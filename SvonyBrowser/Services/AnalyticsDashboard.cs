using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
    using SvonyBrowser.Helpers;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Comprehensive analytics dashboard with charts, trends, and insights
    /// about gameplay patterns, resource management, and performance.
    /// </summary>
    public sealed class AnalyticsDashboard : IDisposable
    {
        #region Singleton

        private static readonly Lazy<AnalyticsDashboard> _lazyInstance =
            new Lazy<AnalyticsDashboard>(() => new AnalyticsDashboard(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static AnalyticsDashboard Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly GameStateEngine _gameState;
        private readonly List<DataPoint> _resourceHistory = new List<DataPoint>();
        private readonly List<DataPoint> _powerHistory = new List<DataPoint>();
        private readonly List<DataPoint> _troopHistory = new List<DataPoint>();
        private readonly List<ActivityRecord> _activityLog = new List<ActivityRecord>();
        private readonly object _lock = new object();
        private readonly Timer _snapshotTimer;
        private readonly string _dataPath;
        private bool _disposed = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current analytics summary.
        /// </summary>
        public AnalyticsSummary CurrentSummary { get; private set; } = new AnalyticsSummary();

        #endregion

        #region Events

        /// <summary>
        /// Fired when analytics are updated.
        /// </summary>
        public event Action<AnalyticsSummary> AnalyticsUpdated;

        #endregion

        #region Constructor

        private AnalyticsDashboard()
        {
            _gameState = GameStateEngine.Instance;
            _dataPath = Path.Combine(App.DataPath, "analytics");
            Directory.CreateDirectory(_dataPath);

            // Take snapshots every 5 minutes
            _snapshotTimer = new Timer(TakeSnapshot, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));

            // Subscribe to game events
            _gameState.StateChanged += OnStateChanged;

            // Load historical data
            LoadHistoricalData();

            App.Logger.Information("AnalyticsDashboard initialized");
        }

        #endregion

        #region Public Methods - Data Retrieval

        /// <summary>
        /// Gets resource history for a time range.
        /// </summary>
        public List<ResourceDataPoint> GetResourceHistory(DateTime start, DateTime end)
        {
            lock (_lock)
            {
                return _resourceHistory
                    .Where(p => p.Timestamp >= start && p.Timestamp <= end)
                    .Select(p => new ResourceDataPoint
                    {
                        Timestamp = p.Timestamp,
                        Gold = (long)p.Values.GetValueOrDefault("gold"),
                        Food = (long)p.Values.GetValueOrDefault("food"),
                        Lumber = (long)p.Values.GetValueOrDefault("lumber"),
                        Stone = (long)p.Values.GetValueOrDefault("stone"),
                        Iron = (long)p.Values.GetValueOrDefault("iron")
                    })
                    .ToList();
            }
        }

        /// <summary>
        /// Gets power history for a time range.
        /// </summary>
        public List<PowerDataPoint> GetPowerHistory(DateTime start, DateTime end)
        {
            lock (_lock)
            {
                return _powerHistory
                    .Where(p => p.Timestamp >= start && p.Timestamp <= end)
                    .Select(p => new PowerDataPoint
                    {
                        Timestamp = p.Timestamp,
                        TotalPower = (long)p.Values.GetValueOrDefault("total"),
                        MilitaryPower = (long)p.Values.GetValueOrDefault("military"),
                        EconomyPower = (long)p.Values.GetValueOrDefault("economy"),
                        TechPower = (long)p.Values.GetValueOrDefault("tech")
                    })
                    .ToList();
            }
        }

        /// <summary>
        /// Gets troop history for a time range.
        /// </summary>
        public List<TroopDataPoint> GetTroopHistory(DateTime start, DateTime end)
        {
            lock (_lock)
            {
                return _troopHistory
                    .Where(p => p.Timestamp >= start && p.Timestamp <= end)
                    .Select(p => new TroopDataPoint
                    {
                        Timestamp = p.Timestamp,
                        Troops = p.Values.ToDictionary(kv => kv.Key, kv => (int)kv.Value)
                    })
                    .ToList();
            }
        }

        /// <summary>
        /// Gets activity log for a time range.
        /// </summary>
        public List<ActivityRecord> GetActivityLog(DateTime start, DateTime end, string activityType = null)
        {
            lock (_lock)
            {
                var query = _activityLog.Where(a => a.Timestamp >= start && a.Timestamp <= end);
            
                if (!string.IsNullOrEmpty(activityType))
                    query = query.Where(a => a.ActivityType == activityType);

                return query.OrderByDescending(a => a.Timestamp).ToList();
            }
        }

        #endregion

        #region Public Methods - Analytics

        /// <summary>
        /// Calculates resource production efficiency.
        /// </summary>
        public ProductionEfficiency CalculateProductionEfficiency(int days = 7)
        {
            var efficiency = new ProductionEfficiency();
            var start = DateTime.UtcNow.AddDays(-days);

            var history = GetResourceHistory(start, DateTime.UtcNow);
            if (history.Count < 2) return efficiency;

            var first = history.First();
            var last = history.Last();
            var hours = (last.Timestamp - first.Timestamp).TotalHours;

            if (hours > 0)
            {
                efficiency.GoldPerHour = (last.Gold - first.Gold) / hours;
                efficiency.FoodPerHour = (last.Food - first.Food) / hours;
                efficiency.LumberPerHour = (last.Lumber - first.Lumber) / hours;
                efficiency.StonePerHour = (last.Stone - first.Stone) / hours;
                efficiency.IronPerHour = (last.Iron - first.Iron) / hours;
            }

            // Calculate efficiency score (compared to theoretical max)
            var theoreticalMax = _gameState.GetProductionRates();
            if (theoreticalMax.Gold > 0)
                efficiency.GoldEfficiency = efficiency.GoldPerHour / theoreticalMax.Gold;
            if (theoreticalMax.Food > 0)
                efficiency.FoodEfficiency = efficiency.FoodPerHour / theoreticalMax.Food;

            return efficiency;
        }

        /// <summary>
        /// Calculates power growth rate.
        /// </summary>
        public PowerGrowth CalculatePowerGrowth(int days = 7)
        {
            var growth = new PowerGrowth();
            var start = DateTime.UtcNow.AddDays(-days);

            var history = GetPowerHistory(start, DateTime.UtcNow);
            if (history.Count < 2) return growth;

            var first = history.First();
            var last = history.Last();
            var daysDiff = (last.Timestamp - first.Timestamp).TotalDays;

            if (daysDiff > 0)
            {
                growth.TotalGrowth = last.TotalPower - first.TotalPower;
                growth.DailyGrowth = growth.TotalGrowth / daysDiff;
                growth.GrowthRate = first.TotalPower > 0 
                    ? (double)growth.TotalGrowth / first.TotalPower 
                    : 0;
            }

            // Project future power
            growth.ProjectedPower7Days = last.TotalPower + (long)(growth.DailyGrowth * 7);
            growth.ProjectedPower30Days = last.TotalPower + (long)(growth.DailyGrowth * 30);

            return growth;
        }

        /// <summary>
        /// Calculates attack statistics.
        /// </summary>
        public AttackStatistics CalculateAttackStatistics(int days = 7)
        {
            var stats = new AttackStatistics();
            var start = DateTime.UtcNow.AddDays(-days);

            var attacks = GetActivityLog(start, DateTime.UtcNow, "attack");
        
            stats.TotalAttacks = attacks.Count;
            stats.SuccessfulAttacks = attacks.Count(a => a.Success);
            stats.FailedAttacks = stats.TotalAttacks - stats.SuccessfulAttacks;
            stats.SuccessRate = stats.TotalAttacks > 0 
                ? (double)stats.SuccessfulAttacks / stats.TotalAttacks 
                : 0;

            // Calculate loot
            foreach (var attack in attacks.Where(a => a.Success))
            {
                stats.TotalLootGold += attack.LootGold;
                stats.TotalLootFood += attack.LootFood;
                stats.TotalLootLumber += attack.LootLumber;
                stats.TotalLootStone += attack.LootStone;
                stats.TotalLootIron += attack.LootIron;
            }

            // Calculate losses
            foreach (var attack in attacks)
            {
                stats.TotalTroopsLost += attack.TroopsLost;
            }

            return stats;
        }

        /// <summary>
        /// Generates insights based on analytics.
        /// </summary>
        public List<Insight> GenerateInsights()
        {
            var insights = new List<Insight>();

            // Resource insights
            var efficiency = CalculateProductionEfficiency();
            if (efficiency.GoldEfficiency < 0.5)
            {
                insights.Add(new Insight
                {
                    Type = "warning",
                    Category = "resources",
                    Title = "Low Gold Production Efficiency",
                    Description = $"Your gold production is only {efficiency.GoldEfficiency:P0} of theoretical maximum. Consider collecting resources more frequently.",
                    Priority = 2
                });
            }

            // Power growth insights
            var growth = CalculatePowerGrowth();
            if (growth.DailyGrowth < 10000)
            {
                insights.Add(new Insight
                {
                    Type = "info",
                    Category = "growth",
                    Title = "Slow Power Growth",
                    Description = $"Your daily power growth is {growth.DailyGrowth:N0}. Focus on training troops and upgrading buildings to accelerate growth.",
                    Priority = 3
                });
            }
            else if (growth.DailyGrowth > 100000)
            {
                insights.Add(new Insight
                {
                    Type = "success",
                    Category = "growth",
                    Title = "Excellent Power Growth",
                    Description = $"Your daily power growth of {growth.DailyGrowth:N0} is impressive! Keep up the good work.",
                    Priority = 5
                });
            }

            // Attack insights
            var attackStats = CalculateAttackStatistics();
            if (attackStats.SuccessRate < 0.7 && attackStats.TotalAttacks > 5)
            {
                insights.Add(new Insight
                {
                    Type = "warning",
                    Category = "combat",
                    Title = "Low Attack Success Rate",
                    Description = $"Your attack success rate is {attackStats.SuccessRate:P0}. Use the combat simulator to plan attacks better.",
                    Priority = 2
                });
            }

            // Activity insights
            var recentActivity = GetActivityLog(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
            if (recentActivity.Count < 10)
            {
                insights.Add(new Insight
                {
                    Type = "info",
                    Category = "activity",
                    Title = "Low Activity Detected",
                    Description = "You've been less active in the past 24 hours. Consider enabling auto-pilot to maintain progress.",
                    Priority = 4
                });
            }

            return insights.OrderBy(i => i.Priority).ToList();
        }

        /// <summary>
        /// Gets a complete analytics summary.
        /// </summary>
        public AnalyticsSummary GetSummary()
        {
            var summary = new AnalyticsSummary
            {
                Timestamp = DateTime.UtcNow,
            
                // Current state
                CurrentPower = _gameState.Player.Power,
                CurrentResources = _gameState.GetTotalResources(),
                CurrentTroops = _gameState.GetTotalTroops(),
            
                // Production
                ProductionEfficiency = CalculateProductionEfficiency(),
            
                // Growth
                PowerGrowth = CalculatePowerGrowth(),
            
                // Combat
                AttackStatistics = CalculateAttackStatistics(),
            
                // Insights
                Insights = GenerateInsights()
            };

            CurrentSummary = summary;
            return summary;
        }

        #endregion

        #region Public Methods - Comparison

        /// <summary>
        /// Compares current stats with alliance average.
        /// </summary>
        public AllianceComparison CompareWithAlliance()
        {
            var comparison = new AllianceComparison();

            // This would typically fetch alliance data from the game
            // For now, we use placeholder calculations
            var myPower = _gameState.Player.Power;
            var allianceAvgPower = myPower * 0.8; // Placeholder

            comparison.MyPower = myPower;
            comparison.AllianceAveragePower = (long)allianceAvgPower;
            comparison.Rank = 5; // Placeholder
            comparison.PercentileRank = 0.85; // Top 15%

            return comparison;
        }

        #endregion

        #region Private Methods

        private void TakeSnapshot(object state)
        {
            if (_disposed) return;

            try
            {
                var timestamp = DateTime.UtcNow;

                // Resource snapshot
                var resources = _gameState.GetTotalResources();
                lock (_lock)
                {
                    _resourceHistory.Add(new DataPoint
                    {
                        Timestamp = timestamp,
                        Values = new Dictionary<string, double>
                        {
                            ["gold"] = resources.Gold,
                            ["food"] = resources.Food,
                            ["lumber"] = resources.Lumber,
                            ["stone"] = resources.Stone,
                            ["iron"] = resources.Iron
                        }
                    });

                    // Keep only last 30 days of data
                    var cutoff = DateTime.UtcNow.AddDays(-30);
                    _resourceHistory.RemoveAll(p => p.Timestamp < cutoff);
                }

                // Power snapshot
                lock (_lock)
                {
                    _powerHistory.Add(new DataPoint
                    {
                        Timestamp = timestamp,
                        Values = new Dictionary<string, double>
                        {
                            ["total"] = _gameState.Player.Power
                        }
                    });

                    _powerHistory.RemoveAll(p => p.Timestamp < DateTime.UtcNow.AddDays(-30));
                }

                // Troop snapshot
                var troops = _gameState.GetTotalTroops();
                lock (_lock)
                {
                    _troopHistory.Add(new DataPoint
                    {
                        Timestamp = timestamp,
                        Values = troops.ToDictionary(kv => kv.Key, kv => (double)kv.Value)
                    });

                    _troopHistory.RemoveAll(p => p.Timestamp < DateTime.UtcNow.AddDays(-30));
                }

                // Update summary
                var summary = GetSummary();
                AnalyticsUpdated?.Invoke(summary);

                // Periodically save to disk
                if (timestamp.Minute == 0)
                {
                    SaveHistoricalData();
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error taking analytics snapshot");
            }
        }

        private void OnStateChanged(GameStateChange change)
        {
            // Log activity
            var record = new ActivityRecord
            {
                Timestamp = DateTime.UtcNow,
                ActivityType = change.EntityType,
                Description = $"{change.EntityType} {change.ChangeType}",
                EntityId = change.EntityId
            };

            lock (_lock)
            {
                _activityLog.Add(record);
            
                // Keep only last 7 days of activity
                var cutoff = DateTime.UtcNow.AddDays(-7);
                _activityLog.RemoveAll(a => a.Timestamp < cutoff);
            }
        }

        private void LoadHistoricalData()
        {
            try
            {
                var resourceFile = Path.Combine(_dataPath, "resources.json");
                if (File.Exists(resourceFile))
                {
                    var json = File.ReadAllText(resourceFile);
                    var data = JsonConvert.DeserializeObject<List<DataPoint>>(json);
                    if (data != null)
                    {
                        lock (_lock)
                        {
                            _resourceHistory.AddRange(data);
                        }
                    }
                }

                var powerFile = Path.Combine(_dataPath, "power.json");
                if (File.Exists(powerFile))
                {
                    var json = File.ReadAllText(powerFile);
                    var data = JsonConvert.DeserializeObject<List<DataPoint>>(json);
                    if (data != null)
                    {
                        lock (_lock)
                        {
                            _powerHistory.AddRange(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error loading historical analytics data");
            }
        }

        private void SaveHistoricalData()
        {
            try
            {
                lock (_lock)
                {
                    var resourceFile = Path.Combine(_dataPath, "resources.json");
                    File.WriteAllText(resourceFile, JsonConvert.SerializeObject(_resourceHistory));

                    var powerFile = Path.Combine(_dataPath, "power.json");
                    File.WriteAllText(powerFile, JsonConvert.SerializeObject(_powerHistory));
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error saving historical analytics data");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _snapshotTimer?.Dispose();
            _gameState.StateChanged -= OnStateChanged;

            SaveHistoricalData();

            App.Logger.Information("AnalyticsDashboard disposed");
        }

        #endregion
    }

    #region Models

    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, double> Values { get; set; } = new double>();
    }

    public class ResourceDataPoint
    {
        public DateTime Timestamp { get; set; }
        public long Gold { get; set; }
        public long Food { get; set; }
        public long Lumber { get; set; }
        public long Stone { get; set; }
        public long Iron { get; set; }
    }

    public class PowerDataPoint
    {
        public DateTime Timestamp { get; set; }
        public long TotalPower { get; set; }
        public long MilitaryPower { get; set; }
        public long EconomyPower { get; set; }
        public long TechPower { get; set; }
    }

    public class TroopDataPoint
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, int> Troops { get; set; } = new int>();
    }

    public class ActivityRecord
    {
        public DateTime Timestamp { get; set; }
        public string ActivityType { get; set; } = "";
        public string Description { get; set; } = "";
        public int EntityId { get; set; }
        public bool Success { get; set; } = true;
        public long LootGold { get; set; }
        public long LootFood { get; set; }
        public long LootLumber { get; set; }
        public long LootStone { get; set; }
        public long LootIron { get; set; }
        public int TroopsLost { get; set; }
    }

    public class ProductionEfficiency
    {
        public double GoldPerHour { get; set; }
        public double FoodPerHour { get; set; }
        public double LumberPerHour { get; set; }
        public double StonePerHour { get; set; }
        public double IronPerHour { get; set; }
        public double GoldEfficiency { get; set; }
        public double FoodEfficiency { get; set; }
        public double LumberEfficiency { get; set; }
        public double StoneEfficiency { get; set; }
        public double IronEfficiency { get; set; }
    }

    public class PowerGrowth
    {
        public long TotalGrowth { get; set; }
        public double DailyGrowth { get; set; }
        public double GrowthRate { get; set; }
        public long ProjectedPower7Days { get; set; }
        public long ProjectedPower30Days { get; set; }
    }

    public class AttackStatistics
    {
        public int TotalAttacks { get; set; }
        public int SuccessfulAttacks { get; set; }
        public int FailedAttacks { get; set; }
        public double SuccessRate { get; set; }
        public long TotalLootGold { get; set; }
        public long TotalLootFood { get; set; }
        public long TotalLootLumber { get; set; }
        public long TotalLootStone { get; set; }
        public long TotalLootIron { get; set; }
        public int TotalTroopsLost { get; set; }
    }

    public class Insight
    {
        public string Type { get; set; } = ""; // info, warning, success, error
        public string Category { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int Priority { get; set; }
    }

    public class AnalyticsSummary
    {
        public DateTime Timestamp { get; set; }
        public long CurrentPower { get; set; }
        public ResourceState CurrentResources { get; set; } = new ResourceState();
        public Dictionary<string, int> CurrentTroops { get; set; } = new int>();
        public ProductionEfficiency ProductionEfficiency { get; set; } = new ProductionEfficiency();
        public PowerGrowth PowerGrowth { get; set; } = new PowerGrowth();
        public AttackStatistics AttackStatistics { get; set; } = new AttackStatistics();
        public List<Insight> Insights { get; set; } = new List<Insight>();
    }

    public class AllianceComparison
    {
        public long MyPower { get; set; }
        public long AllianceAveragePower { get; set; }
        public int Rank { get; set; }
        public double PercentileRank { get; set; }
    }

    #endregion

}