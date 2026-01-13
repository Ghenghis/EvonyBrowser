using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// World map scanner that discovers targets, resources, monsters,
    /// and provides strategic overlay information.
    /// </summary>
    public sealed class MapScanner : IDisposable
    {
        #region Singleton

        private static readonly Lazy<MapScanner> _lazyInstance =
            new Lazy<MapScanner>(() => new MapScanner(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static MapScanner Instance => _lazyInstance.Value;

        #endregion

        #region Constants

        private const int MAP_SIZE = 1200; // Map is 1200x1200
        private const int SCAN_CHUNK_SIZE = 50; // Scan 50x50 chunks

        #endregion

        #region Fields

        private readonly ConcurrentDictionary<string, MapTile> _mapData = new MapTile>();
        private readonly ConcurrentDictionary<string, PlayerBase> _playerBases = new PlayerBase>();
        private readonly ConcurrentDictionary<string, ResourceTile> _resourceTiles = new ResourceTile>();
        private readonly ConcurrentDictionary<string, MonsterTile> _monsters = new MonsterTile>();
        private readonly List<ScanResult> _scanHistory = new List<ScanResult>();
        private readonly string _dataPath;
        private CancellationTokenSource _scanCts;
        private bool _isScanning = false;
        private bool _disposed = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether a scan is currently in progress.
        /// </summary>
        public bool IsScanning => _isScanning;

        /// <summary>
        /// Gets the current scan progress (0-100).
        /// </summary>
        public int ScanProgress { get; private set; }

        /// <summary>
        /// Gets the total discovered tiles.
        /// </summary>
        public int DiscoveredTiles => _mapData.Count;

        #endregion

        #region Events

        /// <summary>
        /// Fired when scan progress updates.
        /// </summary>
        public event Action<int> ScanProgressChanged;

        /// <summary>
        /// Fired when a scan completes.
        /// </summary>
        public event Action<ScanResult> ScanCompleted;

        /// <summary>
        /// Fired when a high-value target is discovered.
        /// </summary>
        public event Action<TargetAlert> TargetDiscovered;

        /// <summary>
        /// Fired when map data is updated.
        /// </summary>
        public event Action MapDataUpdated;

        #endregion

        #region Constructor

        private MapScanner()
        {
            _dataPath = Path.Combine(App.DataPath, "map-cache");
            Directory.CreateDirectory(_dataPath);

            LoadCachedData();

            App.Logger.Information("MapScanner initialized");
        }

        #endregion

        #region Public Methods - Scanning

        /// <summary>
        /// Starts a full map scan.
        /// </summary>
        public async Task StartFullScanAsync()
        {
            if (_isScanning)
            {
                App.Logger.Warning("Scan already in progress");
                return;
            }

            _isScanning = true;
            _scanCts = new CancellationTokenSource();
            ScanProgress = 0;

            var result = new ScanResult
            {
                StartTime = DateTime.UtcNow,
                ScanType = "full"
            };

            try
            {
                var totalChunks = (MAP_SIZE / SCAN_CHUNK_SIZE) * (MAP_SIZE / SCAN_CHUNK_SIZE);
                var processedChunks = 0;

                for (int x = 0; x < MAP_SIZE; x += SCAN_CHUNK_SIZE)
                {
                    for (int y = 0; y < MAP_SIZE; y += SCAN_CHUNK_SIZE)
                    {
                        if (_scanCts.Token.IsCancellationRequested)
                            break;

                        await ScanChunkAsync(x, y, SCAN_CHUNK_SIZE, result);
                    
                        processedChunks++;
                        ScanProgress = (processedChunks * 100) / totalChunks;
                        ScanProgressChanged?.Invoke(ScanProgress);

                        // Small delay to prevent overwhelming the server
                        await Task.Delay(100, _scanCts.Token);
                    }

                    if (_scanCts.Token.IsCancellationRequested)
                        break;
                }

                result.EndTime = DateTime.UtcNow;
                result.Success = !_scanCts.Token.IsCancellationRequested;
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.EndTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                result.EndTime = DateTime.UtcNow;
                App.Logger.Error(ex, "Map scan failed");
            }
            finally
            {
                _isScanning = false;
                ScanProgress = 100;

                _scanHistory.Add(result);
                SaveCachedData();
            
                ScanCompleted?.Invoke(result);
                MapDataUpdated?.Invoke();
            }
        }

        /// <summary>
        /// Scans a specific area around coordinates.
        /// </summary>
        public async Task<ScanResult> ScanAreaAsync(int centerX, int centerY, int radius)
        {
            var result = new ScanResult
            {
                StartTime = DateTime.UtcNow,
                ScanType = "area",
                CenterX = centerX,
                CenterY = centerY,
                Radius = radius
            };

            try
            {
                var startX = Math.Max(0, centerX - radius);
                var startY = Math.Max(0, centerY - radius);
                var endX = Math.Min(MAP_SIZE, centerX + radius);
                var endY = Math.Min(MAP_SIZE, centerY + radius);

                for (int x = startX; x < endX; x += SCAN_CHUNK_SIZE)
                {
                    for (int y = startY; y < endY; y += SCAN_CHUNK_SIZE)
                    {
                        var chunkSize = Math.Min(SCAN_CHUNK_SIZE, Math.Min(endX - x, endY - y));
                        await ScanChunkAsync(x, y, chunkSize, result);
                        await Task.Delay(50);
                    }
                }

                result.Success = true;
                result.EndTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                result.EndTime = DateTime.UtcNow;
                App.Logger.Error(ex, "Area scan failed");
            }

            _scanHistory.Add(result);
            MapDataUpdated?.Invoke();

            return result;
        }

        /// <summary>
        /// Stops the current scan.
        /// </summary>
        public void StopScan()
        {
            _scanCts?.Cancel();
        }

        #endregion

        #region Public Methods - Data Retrieval

        /// <summary>
        /// Gets all player bases in an area.
        /// </summary>
        public List<PlayerBase> GetPlayerBasesInArea(int x, int y, int radius)
        {
            return _playerBases.Values
                .Where(b => Math.Abs(b.X - x) <= radius && Math.Abs(b.Y - y) <= radius)
                .ToList();
        }

        /// <summary>
        /// Gets all resource tiles in an area.
        /// </summary>
        public List<ResourceTile> GetResourceTilesInArea(int x, int y, int radius, string resourceType = null)
        {
            var query = _resourceTiles.Values
                .Where(r => Math.Abs(r.X - x) <= radius && Math.Abs(r.Y - y) <= radius);

            if (!string.IsNullOrEmpty(resourceType))
                query = query.Where(r => r.ResourceType == resourceType);

            return query.ToList();
        }

        /// <summary>
        /// Gets all monsters in an area.
        /// </summary>
        public List<MonsterTile> GetMonstersInArea(int x, int y, int radius, int? minLevel = null, int? maxLevel = null)
        {
            var query = _monsters.Values
                .Where(m => Math.Abs(m.X - x) <= radius && Math.Abs(m.Y - y) <= radius);

            if (minLevel.HasValue)
                query = query.Where(m => m.Level >= minLevel.Value);
            if (maxLevel.HasValue)
                query = query.Where(m => m.Level <= maxLevel.Value);

            return query.ToList();
        }

        /// <summary>
        /// Finds optimal farm targets based on criteria.
        /// </summary>
        public List<FarmTarget> FindFarmTargets(FarmTargetCriteria criteria)
        {
            var targets = new List<FarmTarget>();
            var myPower = GameStateEngine.Instance.Player.Power;

            foreach (var player in _playerBases.Values)
            {
                // Skip alliance members
                if (player.AllianceTag == GameStateEngine.Instance.Player.AllianceTag)
                    continue;

                // Check power ratio
                var powerRatio = (double)player.Power / myPower;
                if (powerRatio > criteria.MaxPowerRatio)
                    continue;

                // Check distance
                var distance = CalculateDistance(criteria.FromX, criteria.FromY, player.X, player.Y);
                if (distance > criteria.MaxDistance)
                    continue;

                // Check bubble status
                if (criteria.SkipBubbled && player.IsBubbled)
                    continue;

                // Calculate score
                var score = CalculateFarmScore(player, criteria, distance);

                targets.Add(new FarmTarget
                {
                    X = player.X,
                    Y = player.Y,
                    PlayerName = player.PlayerName,
                    Power = player.Power,
                    AllianceTag = player.AllianceTag,
                    Distance = distance,
                    Score = score,
                    IsBubbled = player.IsBubbled,
                    LastSeen = player.LastSeen
                });
            }

            return targets.OrderByDescending(t => t.Score).Take(criteria.MaxResults).ToList();
        }

        /// <summary>
        /// Finds optimal resource gathering spots.
        /// </summary>
        public List<GatheringSpot> FindGatheringSpots(GatheringCriteria criteria)
        {
            var spots = new List<GatheringSpot>();

            foreach (var resource in _resourceTiles.Values)
            {
                if (!string.IsNullOrEmpty(criteria.ResourceType) && resource.ResourceType != criteria.ResourceType)
                    continue;

                if (resource.Level < criteria.MinLevel)
                    continue;

                var distance = CalculateDistance(criteria.FromX, criteria.FromY, resource.X, resource.Y);
                if (distance > criteria.MaxDistance)
                    continue;

                // Check if occupied
                if (criteria.SkipOccupied && resource.IsOccupied)
                    continue;

                var efficiency = resource.Amount / (distance + 1);

                spots.Add(new GatheringSpot
                {
                    X = resource.X,
                    Y = resource.Y,
                    ResourceType = resource.ResourceType,
                    Level = resource.Level,
                    Amount = resource.Amount,
                    Distance = distance,
                    Efficiency = efficiency,
                    IsOccupied = resource.IsOccupied
                });
            }

            return spots.OrderByDescending(s => s.Efficiency).Take(criteria.MaxResults).ToList();
        }

        /// <summary>
        /// Gets map statistics.
        /// </summary>
        public MapStatistics GetStatistics()
        {
            return new MapStatistics
            {
                TotalTilesScanned = _mapData.Count,
                PlayerBasesFound = _playerBases.Count,
                ResourceTilesFound = _resourceTiles.Count,
                MonstersFound = _monsters.Count,
                LastFullScan = _scanHistory.LastOrDefault(s => s.ScanType == "full")?.EndTime,
                ScanCoverage = (double)_mapData.Count / (MAP_SIZE * MAP_SIZE),
                PlayersByAlliance = _playerBases.Values
                    .GroupBy(p => p.AllianceTag ?? "None")
                    .ToDictionary(g => g.Key, g => g.Count()),
                ResourcesByType = _resourceTiles.Values
                    .GroupBy(r => r.ResourceType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                MonstersByLevel = _monsters.Values
                    .GroupBy(m => m.Level)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        #endregion

        #region Public Methods - Overlay Data

        /// <summary>
        /// Gets overlay data for rendering on the map.
        /// </summary>
        public MapOverlayData GetOverlayData(int viewX, int viewY, int viewWidth, int viewHeight, OverlaySettings settings)
        {
            var overlay = new MapOverlayData();

            // Get visible bounds
            var minX = viewX;
            var maxX = viewX + viewWidth;
            var minY = viewY;
            var maxY = viewY + viewHeight;

            // Add player markers
            if (settings.ShowPlayers)
            {
                foreach (var player in _playerBases.Values)
                {
                    if (player.X >= minX && player.X <= maxX && player.Y >= minY && player.Y <= maxY)
                    {
                        var color = GetPlayerMarkerColor(player, settings);
                        overlay.PlayerMarkers.Add(new MapMarker
                        {
                            X = player.X,
                            Y = player.Y,
                            Label = player.PlayerName,
                            Color = color,
                            Size = GetMarkerSize(player.Power),
                            Tooltip = $"{player.PlayerName}\nPower: {player.Power:N0}\nAlliance: {player.AllianceTag ?? "None"}"
                        });
                    }
                }
            }

            // Add resource markers
            if (settings.ShowResources)
            {
                foreach (var resource in _resourceTiles.Values)
                {
                    if (resource.X >= minX && resource.X <= maxX && resource.Y >= minY && resource.Y <= maxY)
                    {
                        overlay.ResourceMarkers.Add(new MapMarker
                        {
                            X = resource.X,
                            Y = resource.Y,
                            Label = $"L{resource.Level}",
                            Color = GetResourceColor(resource.ResourceType),
                            Size = 8,
                            Tooltip = $"{resource.ResourceType} L{resource.Level}\nAmount: {resource.Amount:N0}"
                        });
                    }
                }
            }

            // Add monster markers
            if (settings.ShowMonsters)
            {
                foreach (var monster in _monsters.Values)
                {
                    if (monster.X >= minX && monster.X <= maxX && monster.Y >= minY && monster.Y <= maxY)
                    {
                        overlay.MonsterMarkers.Add(new MapMarker
                        {
                            X = monster.X,
                            Y = monster.Y,
                            Label = $"L{monster.Level}",
                            Color = GetMonsterColor(monster.Level),
                            Size = 10,
                            Tooltip = $"{monster.MonsterType} L{monster.Level}"
                        });
                    }
                }
            }

            // Add heatmap data
            if (settings.ShowHeatmap)
            {
                overlay.HeatmapData = GenerateHeatmap(minX, minY, maxX, maxY, settings.HeatmapType);
            }

            return overlay;
        }

        #endregion

        #region Private Methods

        private async Task ScanChunkAsync(int startX, int startY, int size, ScanResult result)
        {
            // In a real implementation, this would send requests to the game server
            // For now, we simulate the scan with cached/mock data
        
            for (int x = startX; x < startX + size && x < MAP_SIZE; x++)
            {
                for (int y = startY; y < startY + size && y < MAP_SIZE; y++)
                {
                    var key = $"{x}_{y}";
                
                    // Check if we already have recent data for this tile
                    if (_mapData.TryGetValue(key, out var existing) && 
                        (DateTime.UtcNow - existing.LastScanned).TotalHours < 1)
                    {
                        continue;
                    }

                    // Simulate tile discovery
                    var tile = await DiscoverTileAsync(x, y);
                    if (tile != null)
                    {
                        _mapData[key] = tile;
                        ProcessDiscoveredTile(tile, result);
                    }
                }
            }

            await Task.CompletedTask;
        }

        private async Task<MapTile?> DiscoverTileAsync(int x, int y)
        {
            // This would normally query the game server
            // For simulation, we create mock data
        
            await Task.Delay(1); // Simulate network delay

            var tile = new MapTile
            {
                X = x,
                Y = y,
                LastScanned = DateTime.UtcNow
            };

            // Randomly generate tile content for simulation
            var random = new Random(x * 10000 + y);
            var roll = random.NextDouble();

            if (roll < 0.05) // 5% chance of player base
            {
                tile.TileType = "player";
                tile.PlayerId = random.Next(100000, 999999);
            }
            else if (roll < 0.15) // 10% chance of resource
            {
                tile.TileType = "resource";
                tile.ResourceType = new[] { "gold", "food", "lumber", "stone", "iron" }[random.Next(5)];
                tile.Level = random.Next(1, 11);
            }
            else if (roll < 0.20) // 5% chance of monster
            {
                tile.TileType = "monster";
                tile.MonsterType = new[] { "Ymir", "Hydra", "Cerberus", "Gorgon" }[random.Next(4)];
                tile.Level = random.Next(1, 16);
            }
            else
            {
                tile.TileType = "empty";
            }

            return tile;
        }

        private void ProcessDiscoveredTile(MapTile tile, ScanResult result)
        {
            var key = $"{tile.X}_{tile.Y}";

            switch (tile.TileType)
            {
                case "player":
                    var playerBase = new PlayerBase
                    {
                        X = tile.X,
                        Y = tile.Y,
                        PlayerId = tile.PlayerId ?? 0,
                        PlayerName = $"Player_{tile.PlayerId}",
                        Power = new Random().Next(100000, 10000000),
                        LastSeen = DateTime.UtcNow
                    };
                    _playerBases[key] = playerBase;
                    result.PlayersFound++;

                    // Check for high-value target
                    if (playerBase.Power > 1000000 && !playerBase.IsBubbled)
                    {
                        TargetDiscovered?.Invoke(new TargetAlert
                        {
                            X = tile.X,
                            Y = tile.Y,
                            TargetType = "player",
                            Description = $"High-value target: {playerBase.PlayerName} ({playerBase.Power:N0} power)"
                        });
                    }
                    break;

                case "resource":
                    _resourceTiles[key] = new ResourceTile
                    {
                        X = tile.X,
                        Y = tile.Y,
                        ResourceType = tile.ResourceType ?? "unknown",
                        Level = tile.Level ?? 1,
                        Amount = (tile.Level ?? 1) * 100000,
                        LastSeen = DateTime.UtcNow
                    };
                    result.ResourcesFound++;
                    break;

                case "monster":
                    _monsters[key] = new MonsterTile
                    {
                        X = tile.X,
                        Y = tile.Y,
                        MonsterType = tile.MonsterType ?? "Unknown",
                        Level = tile.Level ?? 1,
                        LastSeen = DateTime.UtcNow
                    };
                    result.MonstersFound++;
                    break;
            }
        }

        private double CalculateDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        private double CalculateFarmScore(PlayerBase player, FarmTargetCriteria criteria, double distance)
        {
            // Higher score = better target
            var score = 100.0;

            // Power factor (lower power = higher score)
            score += (1 - (double)player.Power / GameStateEngine.Instance.Player.Power) * 50;

            // Distance factor (closer = higher score)
            score -= distance * 0.5;

            // Bubble penalty
            if (player.IsBubbled)
                score -= 100;

            // Freshness bonus (recently seen = higher score)
            var hoursSinceLastSeen = (DateTime.UtcNow - player.LastSeen).TotalHours;
            score -= hoursSinceLastSeen * 2;

            return Math.Max(0, score);
        }

        private string GetPlayerMarkerColor(PlayerBase player, OverlaySettings settings)
        {
            // Alliance members = green
            if (player.AllianceTag == GameStateEngine.Instance.Player.AllianceTag)
                return "#00FF00";

            // NAP alliances = blue
            if (settings.NapAlliances?.Contains(player.AllianceTag ?? "") == true)
                return "#0088FF";

            // Enemy alliances = red
            if (settings.EnemyAlliances?.Contains(player.AllianceTag ?? "") == true)
                return "#FF0000";

            // Bubbled = gray
            if (player.IsBubbled)
                return "#808080";

            // Default = yellow
            return "#FFFF00";
        }

        private string GetResourceColor(string resourceType)
        {
            return resourceType switch
            {
                "gold" => "#FFD700",
                "food" => "#32CD32",
                "lumber" => "#8B4513",
                "stone" => "#808080",
                "iron" => "#4169E1",
                _ => "#FFFFFF"
            };
        }

        private string GetMonsterColor(int level)
        {
            return level switch
            {
                <= 5 => "#00FF00",
                <= 10 => "#FFFF00",
                <= 15 => "#FF8800",
                _ => "#FF0000"
            };
        }

        private int GetMarkerSize(long power)
        {
            return power switch
            {
                < 100000 => 6,
                < 1000000 => 8,
                < 10000000 => 10,
                < 100000000 => 12,
                _ => 14
            };
        }

        private double[,] GenerateHeatmap(int minX, int minY, int maxX, int maxY, string heatmapType)
        {
            var width = maxX - minX;
            var height = maxY - minY;
            var heatmap = new double[width, height];

            switch (heatmapType)
            {
                case "power":
                    foreach (var player in _playerBases.Values)
                    {
                        if (player.X >= minX && player.X < maxX && player.Y >= minY && player.Y < maxY)
                        {
                            var x = player.X - minX;
                            var y = player.Y - minY;
                            heatmap[x, y] = Math.Log10(player.Power + 1);
                        }
                    }
                    break;

                case "activity":
                    // Generate activity heatmap based on recent scans
                    break;

                case "resources":
                    foreach (var resource in _resourceTiles.Values)
                    {
                        if (resource.X >= minX && resource.X < maxX && resource.Y >= minY && resource.Y < maxY)
                        {
                            var x = resource.X - minX;
                            var y = resource.Y - minY;
                            heatmap[x, y] = resource.Level;
                        }
                    }
                    break;
            }

            return heatmap;
        }

        private void LoadCachedData()
        {
            try
            {
                var cacheFile = Path.Combine(_dataPath, "map-cache.json");
                if (File.Exists(cacheFile))
                {
                    var json = File.ReadAllText(cacheFile);
                    var cache = JsonConvert.DeserializeObject<MapCache>(json);
                    if (cache != null)
                    {
                        foreach (var player in cache.PlayerBases)
                            _playerBases[player.Key] = player.Value;
                        foreach (var resource in cache.ResourceTiles)
                            _resourceTiles[resource.Key] = resource.Value;
                        foreach (var monster in cache.Monsters)
                            _monsters[monster.Key] = monster.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.Warning(ex, "Failed to load map cache");
            }
        }

        private void SaveCachedData()
        {
            try
            {
                var cache = new MapCache
                {
                    PlayerBases = _playerBases.ToDictionary(kv => kv.Key, kv => kv.Value),
                    ResourceTiles = _resourceTiles.ToDictionary(kv => kv.Key, kv => kv.Value),
                    Monsters = _monsters.ToDictionary(kv => kv.Key, kv => kv.Value),
                    LastSaved = DateTime.UtcNow
                };

                var cacheFile = Path.Combine(_dataPath, "map-cache.json");
                File.WriteAllText(cacheFile, JsonConvert.SerializeObject(cache));
            }
            catch (Exception ex)
            {
                App.Logger.Warning(ex, "Failed to save map cache");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _scanCts?.Cancel();
            _scanCts?.Dispose();

            SaveCachedData();

            App.Logger.Information("MapScanner disposed");
        }

        #endregion
    }

    #region Models

    public class MapTile
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string TileType { get; set; } = "empty";
        public int? PlayerId { get; set; }
        public string ResourceType { get; set; }
        public string MonsterType { get; set; }
        public int? Level { get; set; }
        public DateTime LastScanned { get; set; }
    }

    public class PlayerBase
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = "";
        public string AllianceTag { get; set; }
        public long Power { get; set; }
        public bool IsBubbled { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class ResourceTile
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string ResourceType { get; set; } = "";
        public int Level { get; set; }
        public long Amount { get; set; }
        public bool IsOccupied { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class MonsterTile
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string MonsterType { get; set; } = "";
        public int Level { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class ScanResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string ScanType { get; set; } = "";
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int Radius { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public int PlayersFound { get; set; }
        public int ResourcesFound { get; set; }
        public int MonstersFound { get; set; }
    }

    public class TargetAlert
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string TargetType { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class FarmTargetCriteria
    {
        public int FromX { get; set; }
        public int FromY { get; set; }
        public double MaxPowerRatio { get; set; } = 0.5;
        public int MaxDistance { get; set; } = 100;
        public bool SkipBubbled { get; set; } = true;
        public int MaxResults { get; set; } = 20;
    }

    public class FarmTarget
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string PlayerName { get; set; } = "";
        public long Power { get; set; }
        public string AllianceTag { get; set; }
        public double Distance { get; set; }
        public double Score { get; set; }
        public bool IsBubbled { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class GatheringCriteria
    {
        public int FromX { get; set; }
        public int FromY { get; set; }
        public string ResourceType { get; set; }
        public int MinLevel { get; set; } = 1;
        public int MaxDistance { get; set; } = 50;
        public bool SkipOccupied { get; set; } = true;
        public int MaxResults { get; set; } = 20;
    }

    public class GatheringSpot
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string ResourceType { get; set; } = "";
        public int Level { get; set; }
        public long Amount { get; set; }
        public double Distance { get; set; }
        public double Efficiency { get; set; }
        public bool IsOccupied { get; set; }
    }

    public class MapStatistics
    {
        public int TotalTilesScanned { get; set; }
        public int PlayerBasesFound { get; set; }
        public int ResourceTilesFound { get; set; }
        public int MonstersFound { get; set; }
        public DateTime? LastFullScan { get; set; }
        public double ScanCoverage { get; set; }
        public Dictionary<string, int> PlayersByAlliance { get; set; } = new int>();
        public Dictionary<string, int> ResourcesByType { get; set; } = new int>();
        public Dictionary<int, int> MonstersByLevel { get; set; } = new int>();
    }

    public class OverlaySettings
    {
        public bool ShowPlayers { get; set; } = true;
        public bool ShowResources { get; set; } = true;
        public bool ShowMonsters { get; set; } = true;
        public bool ShowHeatmap { get; set; } = false;
        public string HeatmapType { get; set; } = "power";
        public List<string> NapAlliances { get; set; }
        public List<string> EnemyAlliances { get; set; }
    }

    public class MapOverlayData
    {
        public List<MapMarker> PlayerMarkers { get; set; } = new List<MapMarker>();
        public List<MapMarker> ResourceMarkers { get; set; } = new List<MapMarker>();
        public List<MapMarker> MonsterMarkers { get; set; } = new List<MapMarker>();
        public double[,]? HeatmapData { get; set; }
    }

    public class MapMarker
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Label { get; set; } = "";
        public string Color { get; set; } = "#FFFFFF";
        public int Size { get; set; } = 8;
        public string Tooltip { get; set; } = "";
    }

    public class MapCache
    {
        public Dictionary<string, PlayerBase> PlayerBases { get; set; } = new PlayerBase>();
        public Dictionary<string, ResourceTile> ResourceTiles { get; set; } = new ResourceTile>();
        public Dictionary<string, MonsterTile> Monsters { get; set; } = new MonsterTile>();
        public DateTime LastSaved { get; set; }
    }

    #endregion

}