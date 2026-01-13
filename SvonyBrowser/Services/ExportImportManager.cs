using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SvonyBrowser.Helpers;
using SvonyBrowser.Services;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Manages export and import of game data, configurations, templates,
    /// and session recordings for backup and sharing.
    /// </summary>
    public sealed class ExportImportManager : IDisposable
    {
        #region Singleton

        private static readonly Lazy<ExportImportManager> _lazyInstance =
            new Lazy<ExportImportManager>(() => new ExportImportManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static ExportImportManager Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly string _exportPath;

        #endregion

        #region Constructor

        private ExportImportManager()
        {
            _exportPath = Path.Combine(App.DataPath, "exports");
            Directory.CreateDirectory(_exportPath);

            App.Logger.Information("ExportImportManager initialized");
        }

        #endregion

        #region Public Methods - Export

        /// <summary>
        /// Exports all data to a compressed archive.
        /// </summary>
        public async Task<string> ExportAllAsync(string outputPath = null)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"svony_export_{timestamp}.svx";
            var filePath = outputPath ?? Path.Combine(_exportPath, fileName);

            var archive = ZipFile.Open(filePath, ZipArchiveMode.Create); // TODO: Add using block for proper disposal

            // Export configuration
            await AddJsonToArchiveAsync(archive, "config/mcp-config.json", 
                await LoadConfigAsync("mcp-config.json"));
            await AddJsonToArchiveAsync(archive, "config/webhooks.json", 
                await LoadConfigAsync("webhooks.json"));
            await AddJsonToArchiveAsync(archive, "config/accounts.json", 
                await LoadConfigAsync("accounts.json"));

            // Export prompt templates
            var templates = PromptTemplateEngine.Instance.GetAllTemplates()
                .Where(t => t.IsCustom)
                .ToList();
            await AddJsonToArchiveAsync(archive, "templates/custom-templates.json", templates);

            // Export packet templates
            var packetTemplatesPath = Path.Combine(App.DataPath, "packet-templates");
            if (Directory.Exists(packetTemplatesPath))
            {
                foreach (var file in Directory.GetFiles(packetTemplatesPath, "*.pkt"))
                {
                    var entry = archive.CreateEntry($"packets/{Path.GetFileName(file)}");
                    var stream = entry.Open(); // TODO: Add using block for proper disposal
                    var fileStream = File.OpenRead(file); // TODO: Add using block for proper disposal
                    await fileStream.CopyToAsync(stream);
                }
            }

            // Export session recordings
            var recordings = SessionRecorder.Instance.GetAllRecordings();
            await AddJsonToArchiveAsync(archive, "recordings/index.json", recordings);

            // Export analytics data
            var analytics = AnalyticsDashboard.Instance.GetSummary();
            await AddJsonToArchiveAsync(archive, "analytics/summary.json", analytics);

            // Export map cache
            var mapStats = MapScanner.Instance.GetStatistics();
            await AddJsonToArchiveAsync(archive, "map/statistics.json", mapStats);

            // Export metadata
            var metadata = new ExportMetadata
            {
                Version = "1.0",
                ExportDate = DateTime.UtcNow,
                PlayerName = GameStateEngine.Instance.Player.Name,
                PlayerPower = GameStateEngine.Instance.Player.Power,
                Contents = new List<string>
                {
                    "config", "templates", "packets", "recordings", "analytics", "map"
                }
            };
            await AddJsonToArchiveAsync(archive, "metadata.json", metadata);

            App.Logger.Information("Exported all data to {Path}", filePath);
            return filePath;
        }

        /// <summary>
        /// Exports game state snapshot.
        /// </summary>
        public async Task<string> ExportGameStateAsync(string outputPath = null)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"gamestate_{timestamp}.json";
            var filePath = outputPath ?? Path.Combine(_exportPath, fileName);

            var gameState = GameStateEngine.Instance;
            var snapshot = new GameStateSnapshot
            {
                Timestamp = DateTime.UtcNow,
                Player = gameState.Player,
                Cities = gameState.Cities.Values.ToList(),
                Heroes = gameState.Heroes.Values.ToList(),
                Armies = gameState.Armies.Values.ToList(),
                Marches = gameState.Marches.Values.ToList()
            };

            var json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
            await FileEx.WriteAllTextAsync(filePath, json);

            App.Logger.Information("Exported game state to {Path}", filePath);
            return filePath;
        }

        /// <summary>
        /// Exports session recording.
        /// </summary>
        public async Task<string> ExportRecordingAsync(string recordingId, string outputPath = null)
        {
            var recording = SessionRecorder.Instance.GetRecording(recordingId);
            if (recording == null)
                throw new ArgumentException($"Recording not found: {recordingId}");

            var fileName = $"recording_{recordingId}.svr";
            var filePath = outputPath ?? Path.Combine(_exportPath, fileName);

            var json = JsonConvert.SerializeObject(recording, Formatting.Indented);
            await FileEx.WriteAllTextAsync(filePath, json);

            App.Logger.Information("Exported recording {Id} to {Path}", recordingId, filePath);
            return filePath;
        }

        /// <summary>
        /// Exports to CSV format for spreadsheet analysis.
        /// </summary>
        public async Task<string> ExportToCsvAsync(string dataType, string outputPath = null)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{dataType}_{timestamp}.csv";
            var filePath = outputPath ?? Path.Combine(_exportPath, fileName);

            var csv = dataType.ToLower() switch
            {
                "troops" => GenerateTroopsCsv(),
                "heroes" => GenerateHeroesCsv(),
                "resources" => GenerateResourcesCsv(),
                "buildings" => GenerateBuildingsCsv(),
                "analytics" => GenerateAnalyticsCsv(),
                _ => throw new ArgumentException($"Unknown data type: {dataType}")
            };

            await FileEx.WriteAllTextAsync(filePath, csv);

            App.Logger.Information("Exported {Type} to CSV: {Path}", dataType, filePath);
            return filePath;
        }

        /// <summary>
        /// Exports protocol database.
        /// </summary>
        public async Task<string> ExportProtocolDbAsync(string outputPath = null)
        {
            var fileName = "protocol-db-export.json";
            var filePath = outputPath ?? Path.Combine(_exportPath, fileName);

            var dbPath = Path.Combine(App.DataPath, "protocol-db.json");
            if (File.Exists(dbPath))
            {
                File.Copy(dbPath, filePath, true);
            }
            else
            {
                // Generate from ProtocolHandler
                var protocols = ProtocolHandler.Instance.GetAllProtocols();
                var json = JsonConvert.SerializeObject(protocols, Formatting.Indented);
                await FileEx.WriteAllTextAsync(filePath, json);
            }

            App.Logger.Information("Exported protocol database to {Path}", filePath);
            return filePath;
        }

        #endregion

        #region Public Methods - Import

        /// <summary>
        /// Imports data from a compressed archive.
        /// </summary>
        public async Task<ImportResult> ImportAllAsync(string archivePath)
        {
            var result = new ImportResult();

            if (!File.Exists(archivePath))
            {
                result.Success = false;
                result.Error = "Archive file not found";
                return result;
            }

            try
            {
                var archive = ZipFile.OpenRead(archivePath); // TODO: Add using block for proper disposal

                // Read metadata
                var metadataEntry = archive.GetEntry("metadata.json");
                if (metadataEntry != null)
                {
                    var stream = metadataEntry.Open(); // TODO: Add using block for proper disposal
                    var reader = new StreamReader(stream); // TODO: Add using block for proper disposal
                    var json = await reader.ReadToEndAsync();
                    result.Metadata = JsonConvert.DeserializeObject<ExportMetadata>(json);
                }

                // Import configuration
                foreach (var entry in archive.Entries.Where(e => e.FullName.StartsWith("config/")))
                {
                    await ImportConfigEntryAsync(entry);
                    result.ConfigsImported++;
                }

                // Import templates
                var templatesEntry = archive.GetEntry("templates/custom-templates.json");
                if (templatesEntry != null)
                {
                    var stream = templatesEntry.Open(); // TODO: Add using block for proper disposal
                    var reader = new StreamReader(stream); // TODO: Add using block for proper disposal
                    var json = await reader.ReadToEndAsync();
                    var templates = JsonConvert.DeserializeObject<List<PromptTemplate>>(json);
                    if (templates != null)
                    {
                        foreach (var template in templates)
                        {
                            PromptTemplateEngine.Instance.CreateTemplate(template);
                            result.TemplatesImported++;
                        }
                    }
                }

                // Import packet templates
                foreach (var entry in archive.Entries.Where(e => e.FullName.StartsWith("packets/")))
                {
                    var targetPath = Path.Combine(App.DataPath, "packet-templates", entry.Name);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                    entry.ExtractToFile(targetPath, true);
                    result.PacketTemplatesImported++;
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                App.Logger.Error(ex, "Import failed");
            }

            return result;
        }

        /// <summary>
        /// Imports a session recording.
        /// </summary>
        public async Task<bool> ImportRecordingAsync(string filePath)
        {
            try
            {
                var json = await FileEx.ReadAllTextAsync(filePath);
                var recording = JsonConvert.DeserializeObject<RecordedSession>(json);
            
                if (recording != null)
                {
                    // Recording imported - save to recordings path
                    var destPath = Path.Combine(App.DataPath, "recordings", $"{recording.SessionId}.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                    File.Copy(filePath, destPath, true);
                    App.Logger.Information("Imported recording: {Id}", recording.SessionId);
                    return true;
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to import recording");
            }

            return false;
        }

        /// <summary>
        /// Imports protocol definitions.
        /// </summary>
        public async Task<int> ImportProtocolsAsync(string filePath)
        {
            try
            {
                var json = await FileEx.ReadAllTextAsync(filePath);
                var protocols = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(json);
            
                if (protocols != null)
                {
                    var count = 0;
                    foreach (var protocol in protocols)
                    {
                        ProtocolHandler.Instance.RegisterProtocol(protocol.Key, protocol.Value);
                        count++;
                    }
                    App.Logger.Information("Imported {Count} protocols", count);
                    return count;
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to import protocols");
            }

            return 0;
        }

        #endregion

        #region Public Methods - Sharing

        /// <summary>
        /// Generates a shareable link for a recording.
        /// </summary>
        public async Task<string> GenerateShareLinkAsync(string recordingId)
        {
            var recording = SessionRecorder.Instance.GetRecording(recordingId);
            if (recording == null)
                throw new ArgumentException($"Recording not found: {recordingId}");

            // In a real implementation, this would upload to a sharing service
            // For now, we generate a base64-encoded data URL
            var json = JsonConvert.SerializeObject(recording);
            var compressed = CompressString(json);
            var base64 = Convert.ToBase64String(compressed);

            // Generate a short hash for the link
            var hash = Convert.ToBase64String(
                System.Security.Cryptography.HashEx.SHA256(
                    Encoding.UTF8.GetBytes(recordingId + DateTime.UtcNow.Ticks)
                )
            ).Substring(0, 8).Replace("/", "_").Replace("+", "-");

            return $"svony://share/{hash}";
        }

        /// <summary>
        /// Exports a shareable strategy guide.
        /// </summary>
        public async Task<string> ExportStrategyGuideAsync(StrategyGuideOptions options)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# Evony Strategy Guide");
            sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            sb.AppendLine();

            if (options.IncludeHeroSetups)
            {
                sb.AppendLine("## Hero Setups");
                foreach (var hero in GameStateEngine.Instance.Heroes.Values.Take(10))
                {
                    sb.AppendLine($"### {hero.Name}");
                    sb.AppendLine($"- Level: {hero.Level}");
                    sb.AppendLine($"- Quality: {hero.Quality}");
                    sb.AppendLine($"- Power: {hero.Power:N0}");
                    sb.AppendLine();
                }
            }

            if (options.IncludeTroopCompositions)
            {
                sb.AppendLine("## Troop Compositions");
                var troops = GameStateEngine.Instance.GetTotalTroops();
                foreach (var troop in troops)
                {
                    sb.AppendLine($"- {troop.Key}: {troop.Value:N0}");
                }
                sb.AppendLine();
            }

            if (options.IncludeBuildingPriorities)
            {
                sb.AppendLine("## Building Priorities");
                var city = GameStateEngine.Instance.Cities.Values.FirstOrDefault();
                if (city != null)
                {
                    foreach (var building in city.Buildings.Values.OrderByDescending(b => b.Level))
                    {
                        sb.AppendLine($"- {building.Name}: Level {building.Level}");
                    }
                }
                sb.AppendLine();
            }

            var fileName = $"strategy_guide_{DateTime.Now:yyyyMMdd}.md";
            var filePath = Path.Combine(_exportPath, fileName);
            await FileEx.WriteAllTextAsync(filePath, sb.ToString());

            return filePath;
        }

        #endregion

        #region Private Methods

        private async Task<JObject> LoadConfigAsync(string fileName)
        {
            var path = Path.Combine(App.ConfigPath, fileName);
            if (File.Exists(path))
            {
                var json = await FileEx.ReadAllTextAsync(path);
                return JObject.Parse(json);
            }
            return null;
        }

        private async Task AddJsonToArchiveAsync(ZipArchive archive, string entryName, object data)
        {
            if (data == null) return;

            var entry = archive.CreateEntry(entryName);
            var stream = entry.Open(); // TODO: Add using block for proper disposal
            var writer = new StreamWriter(stream); // TODO: Add using block for proper disposal
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            await writer.WriteAsync(json);
        }

        private async Task ImportConfigEntryAsync(ZipArchiveEntry entry)
        {
            var fileName = Path.GetFileName(entry.FullName);
            var targetPath = Path.Combine(App.ConfigPath, fileName);

            var stream = entry.Open(); // TODO: Add using block for proper disposal
            var fileStream = File.Create(targetPath); // TODO: Add using block for proper disposal
            await stream.CopyToAsync(fileStream);
        }

        private string GenerateTroopsCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Troop Type,Count,Power");

            var troops = GameStateEngine.Instance.GetTotalTroops();
            foreach (var troop in troops)
            {
                sb.AppendLine($"{troop.Key},{troop.Value},{troop.Value * 10}");
            }

            return sb.ToString();
        }

        private string GenerateHeroesCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name,Level,Quality,Power,Attack,Defense,Politics,Leadership");

            foreach (var hero in GameStateEngine.Instance.Heroes.Values)
            {
                sb.AppendLine($"{hero.Name},{hero.Level},{hero.Quality},{hero.Power}," +
                             $"{hero.Attack},{hero.Defense},{hero.Politics},{hero.Leadership}");
            }

            return sb.ToString();
        }

        private string GenerateResourcesCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Resource,Amount,Production Rate");

            var resources = GameStateEngine.Instance.GetTotalResources();
            var rates = GameStateEngine.Instance.GetProductionRates();

            sb.AppendLine($"Gold,{resources.Gold},{rates.Gold}");
            sb.AppendLine($"Food,{resources.Food},{rates.Food}");
            sb.AppendLine($"Lumber,{resources.Lumber},{rates.Lumber}");
            sb.AppendLine($"Stone,{resources.Stone},{rates.Stone}");
            sb.AppendLine($"Iron,{resources.Iron},{rates.Iron}");

            return sb.ToString();
        }

        private string GenerateBuildingsCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("City,Building,Level,Position,Is Upgrading");

            foreach (var city in GameStateEngine.Instance.Cities.Values)
            {
                foreach (var building in city.Buildings.Values)
                {
                    sb.AppendLine($"{city.Name},{building.Name},{building.Level}," +
                                 $"{building.PositionId},{building.IsUpgrading}");
                }
            }

            return sb.ToString();
        }

        private string GenerateAnalyticsCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Metric,Value");

            var summary = AnalyticsDashboard.Instance.GetSummary();
        
            sb.AppendLine($"Current Power,{summary.CurrentPower}");
            sb.AppendLine($"Daily Power Growth,{summary.PowerGrowth.DailyGrowth:F0}");
            sb.AppendLine($"Gold Per Hour,{summary.ProductionEfficiency.GoldPerHour:F0}");
            sb.AppendLine($"Food Per Hour,{summary.ProductionEfficiency.FoodPerHour:F0}");
            sb.AppendLine($"Attack Success Rate,{summary.AttackStatistics.SuccessRate:P0}");
            sb.AppendLine($"Total Attacks,{summary.AttackStatistics.TotalAttacks}");

            return sb.ToString();
        }

        private byte[] CompressString(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var output = new MemoryStream(); // TODO: Add using block for proper disposal
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }
            return output.ToArray();
        }

        private string DecompressString(byte[] compressed)
        {
            var input = new MemoryStream(compressed); // TODO: Add using block for proper disposal
            var gzip = new GZipStream(input, CompressionMode.Decompress); // TODO: Add using block for proper disposal
            var output = new MemoryStream(); // TODO: Add using block for proper disposal
            gzip.CopyTo(output);
            return Encoding.UTF8.GetString(output.ToArray());
        }

    #endregion

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        App.Logger?.Debug("ExportImportManager disposed");
    }

    #endregion
}

    #region Models

    public class ExportMetadata
    {
        public string Version { get; set; } = "1.0";
        public DateTime ExportDate { get; set; }
        public string PlayerName { get; set; } = "";
        public long PlayerPower { get; set; }
        public List<string> Contents { get; set; } = new List<string>();
    }

    public class ImportResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public ExportMetadata? Metadata { get; set; }
        public int ConfigsImported { get; set; }
        public int TemplatesImported { get; set; }
        public int PacketTemplatesImported { get; set; }
        public int RecordingsImported { get; set; }
    }

    // GameStateSnapshot is defined in GameStateEngine.cs

    public class StrategyGuideOptions
    {
        public bool IncludeHeroSetups { get; set; } = true;
        public bool IncludeTroopCompositions { get; set; } = true;
        public bool IncludeBuildingPriorities { get; set; } = true;
        public bool IncludeResourceManagement { get; set; } = true;
        public bool IncludeCombatStrategies { get; set; } = true;
    }

    #endregion

}