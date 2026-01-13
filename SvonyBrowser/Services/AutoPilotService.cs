using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
    using SvonyBrowser.Helpers;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Intelligent automation system that can play the game autonomously
    /// based on user-defined strategies and AI recommendations.
    /// </summary>
    public sealed class AutoPilotService : IDisposable
    {
        #region Singleton

        private static readonly Lazy<AutoPilotService> _lazyInstance =
            new Lazy<AutoPilotService>(() => new AutoPilotService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static AutoPilotService Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly GameStateEngine _gameState;
        private readonly StrategicAdvisor _advisor;
        private readonly McpConnectionManager _mcpManager;
        private readonly ConcurrentQueue<AutoPilotTask> _taskQueue = new ConcurrentQueue<AutoPilotTask>();
        private readonly List<AutoPilotLog> _logs = new List<AutoPilotLog>();
        private readonly object _logLock = new object();

        private CancellationTokenSource _cts;
        private Task _runningTask;
        private AutoPilotStrategy _currentStrategy = new AutoPilotStrategy();
        private bool _disposed = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether auto-pilot is currently running.
        /// </summary>
        public bool IsRunning => _runningTask != null && !_runningTask.IsCompleted;

        /// <summary>
        /// Gets the current strategy.
        /// </summary>
        public AutoPilotStrategy CurrentStrategy => _currentStrategy;

        /// <summary>
        /// Gets the current status.
        /// </summary>
        public AutoPilotStatus Status { get; private set; } = new AutoPilotStatus();

        /// <summary>
        /// Gets recent logs.
        /// </summary>
        public IReadOnlyList<AutoPilotLog> Logs
        {
            get
            {
                lock (_logLock)
                {
                    return _logs.ToArray();
                }
            }
        }

        /// <summary>
        /// Gets the number of active tasks.
        /// </summary>
        public int ActiveTaskCount => IsRunning ? 1 : 0;

        /// <summary>
        /// Gets the queue depth.
        /// </summary>
        public int QueueDepth => _taskQueue.Count;

        /// <summary>
        /// Gets the actions per minute rate.
        /// </summary>
        public double ActionsPerMinute => Status.ActionsPerformed > 0 && Status.RunTime.TotalMinutes > 0 
            ? Status.ActionsPerformed / Status.RunTime.TotalMinutes 
            : 0;

        #endregion

        #region Events

        /// <summary>
        /// Fired when status changes.
        /// </summary>
        public event Action<AutoPilotStatus> StatusChanged;

        /// <summary>
        /// Fired when a task is completed.
        /// </summary>
        public event Action<AutoPilotTask> TaskCompleted;

        /// <summary>
        /// Fired when an action is performed.
        /// </summary>
        public event Action<string> ActionPerformed;

        /// <summary>
        /// Fired when an error occurs.
        /// </summary>
        public event Action<string> ErrorOccurred;

        #endregion

        #region Constructor

        private AutoPilotService()
        {
            _gameState = GameStateEngine.Instance;
            _advisor = StrategicAdvisor.Instance;
            _mcpManager = McpConnectionManager.Instance;

            // Subscribe to game state events
            _gameState.UnderAttack += OnUnderAttack;
            _gameState.ResourcesUpdated += OnResourcesUpdated;

            App.Logger.Information("AutoPilotService initialized");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts auto-pilot with the specified strategy.
        /// </summary>
        public async Task StartAsync(AutoPilotStrategy strategy)
        {
            if (IsRunning)
            {
                Log("Auto-pilot is already running", LogLevel.Warning);
                return;
            }

            _currentStrategy = strategy;
            _cts = new CancellationTokenSource();

            Status = new AutoPilotStatus
            {
                IsRunning = true,
                StartTime = DateTime.UtcNow,
                StrategyName = strategy.Name
            };
            StatusChanged?.Invoke(Status);

            Log($"Auto-pilot started with strategy: {strategy.Name}", LogLevel.Info);

            _runningTask = RunAutoPilotLoopAsync(_cts.Token);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Stops auto-pilot.
        /// </summary>
        public async Task StopAsync()
        {
            if (!IsRunning)
            {
                Log("Auto-pilot is not running", LogLevel.Warning);
                return;
            }

            _cts?.Cancel();

            if (_runningTask != null)
            {
                try
                {
                    await _runningTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }

            Status.IsRunning = false;
            Status.StopTime = DateTime.UtcNow;
            StatusChanged?.Invoke(Status);

            Log("Auto-pilot stopped", LogLevel.Info);
        }

        /// <summary>
        /// Pauses auto-pilot.
        /// </summary>
        public void Pause()
        {
            if (!IsRunning) return;
            Status.IsPaused = true;
            StatusChanged?.Invoke(Status);
            Log("Auto-pilot paused", LogLevel.Info);
        }

        /// <summary>
        /// Resumes auto-pilot.
        /// </summary>
        public void Resume()
        {
            if (!IsRunning) return;
            Status.IsPaused = false;
            StatusChanged?.Invoke(Status);
            Log("Auto-pilot resumed", LogLevel.Info);
        }

        /// <summary>
        /// Updates the current strategy.
        /// </summary>
        public void UpdateStrategy(AutoPilotStrategy strategy)
        {
            _currentStrategy = strategy;
            Log($"Strategy updated to: {strategy.Name}", LogLevel.Info);
        }

        /// <summary>
        /// Gets predefined strategy profiles.
        /// </summary>
        public static List<AutoPilotStrategy> GetPredefinedStrategies()
        {
            return new List<AutoPilotStrategy>
            {
                new AutoPilotStrategy
                {
                    Name = "Aggressive",
                    Description = "Focus on attacking and expanding",
                    Tasks = new List<AutoPilotTaskConfig>
                    {
                        new() { TaskType = "attack_npcs", Interval = TimeSpan.FromMinutes(5), Priority = 1 },
                        new() { TaskType = "train_troops", TroopType = "cavalry", Continuous = true, Priority = 2 },
                        new() { TaskType = "collect_resources", Interval = TimeSpan.FromMinutes(10), Priority = 3 }
                    },
                    Safety = new SafetyConfig { ShieldWhenAttacked = true, MinTroopReserve = 10000 }
                },
                new AutoPilotStrategy
                {
                    Name = "Defensive",
                    Description = "Focus on defense and resource protection",
                    Tasks = new List<AutoPilotTaskConfig>
                    {
                        new() { TaskType = "collect_resources", Interval = TimeSpan.FromMinutes(5), Priority = 1 },
                        new() { TaskType = "train_troops", TroopType = "swordsman", Continuous = true, Priority = 2 },
                        new() { TaskType = "upgrade_walls", Priority = 3 }
                    },
                    Safety = new SafetyConfig { ShieldWhenAttacked = true, MinTroopReserve = 50000, MaxResourceExposure = 1000000 }
                },
                new AutoPilotStrategy
                {
                    Name = "Farming",
                    Description = "Focus on resource gathering and growth",
                    Tasks = new List<AutoPilotTaskConfig>
                    {
                        new() { TaskType = "collect_resources", Interval = TimeSpan.FromMinutes(3), Priority = 1 },
                        new() { TaskType = "attack_npcs", NpcLevel = "5-7", Interval = TimeSpan.FromMinutes(10), Priority = 2 },
                        new() { TaskType = "upgrade_resource_buildings", Priority = 3 },
                        new() { TaskType = "train_troops", TroopType = "transporter", Continuous = true, Priority = 4 }
                    },
                    Safety = new SafetyConfig { ShieldWhenAttacked = true, MinTroopReserve = 5000 }
                },
                new AutoPilotStrategy
                {
                    Name = "Balanced",
                    Description = "Balanced approach to all activities",
                    Tasks = new List<AutoPilotTaskConfig>
                    {
                        new() { TaskType = "collect_resources", Interval = TimeSpan.FromMinutes(5), Priority = 1 },
                        new() { TaskType = "train_troops", TroopType = "auto", Continuous = true, Priority = 2 },
                        new() { TaskType = "upgrade_buildings", Focus = "balanced", Priority = 3 },
                        new() { TaskType = "attack_npcs", NpcLevel = "auto", Interval = TimeSpan.FromMinutes(15), Priority = 4 }
                    },
                    Safety = new SafetyConfig { ShieldWhenAttacked = true, MinTroopReserve = 20000, MaxResourceExposure = 5000000 }
                }
            };
        }

        #endregion

        #region Private Methods - Main Loop

        private async Task RunAutoPilotLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check if paused
                    if (Status.IsPaused)
                    {
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }

                    // Check safety conditions
                    if (!await CheckSafetyConditionsAsync(cancellationToken))
                    {
                        await Task.Delay(5000, cancellationToken);
                        continue;
                    }

                    // Process task queue
                    if (_taskQueue.TryDequeue(out var task))
                    {
                        await ExecuteTaskAsync(task, cancellationToken);
                    }
                    else
                    {
                        // Generate new tasks based on strategy
                        await GenerateTasksAsync(cancellationToken);
                    }

                    // Update status
                    Status.LastActionTime = DateTime.UtcNow;
                    Status.TotalActions++;
                    StatusChanged?.Invoke(Status);

                    // Human-like delay
                    var delay = GetHumanLikeDelay();
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"Error in auto-pilot loop: {ex.Message}", LogLevel.Error);
                    ErrorOccurred?.Invoke(ex.Message);
                    await Task.Delay(5000, cancellationToken);
                }
            }
        }

        private async Task<bool> CheckSafetyConditionsAsync(CancellationToken cancellationToken)
        {
            var safety = _currentStrategy.Safety;

            // Check troop reserve
            var totalTroops = _gameState.GetTotalTroops().Values.Sum();
            if (totalTroops < safety.MinTroopReserve)
            {
                Log($"Troop reserve below minimum ({totalTroops} < {safety.MinTroopReserve})", LogLevel.Warning);
                Status.CurrentTask = "Waiting for troops";
                return false;
            }

            // Check resource exposure
            var totalResources = _gameState.GetTotalResources();
            var resourceValue = totalResources.Gold + totalResources.Food + totalResources.Lumber + 
                              totalResources.Stone + totalResources.Iron;
        
            if (safety.MaxResourceExposure > 0 && resourceValue > safety.MaxResourceExposure)
            {
                Log($"Resource exposure above maximum ({resourceValue} > {safety.MaxResourceExposure})", LogLevel.Warning);
                // Consider spending resources or shielding
                await SpendExcessResourcesAsync(cancellationToken);
            }

            return true;
        }

        private async Task GenerateTasksAsync(CancellationToken cancellationToken)
        {
            foreach (var taskConfig in _currentStrategy.Tasks.OrderBy(t => t.Priority))
            {
                if (ShouldExecuteTask(taskConfig))
                {
                    var task = CreateTask(taskConfig);
                    _taskQueue.Enqueue(task);
                }
            }

            await Task.CompletedTask;
        }

        private bool ShouldExecuteTask(AutoPilotTaskConfig config)
        {
            // Check if enough time has passed since last execution
            if (config.LastExecuted.HasValue && config.Interval.HasValue)
            {
                var elapsed = DateTime.UtcNow - config.LastExecuted.Value;
                if (elapsed < config.Interval.Value)
                    return false;
            }

            // Check conditions
            if (!string.IsNullOrEmpty(config.Condition))
            {
                return EvaluateCondition(config.Condition);
            }

            return true;
        }

        private AutoPilotTask CreateTask(AutoPilotTaskConfig config)
        {
            return new AutoPilotTask
            {
                Id = Guid.NewGuid().ToString(),
                TaskType = config.TaskType,
                Parameters = new Dictionary<string, object>
                {
                    ["troopType"] = config.TroopType ?? "auto",
                    ["npcLevel"] = config.NpcLevel ?? "auto",
                    ["focus"] = config.Focus ?? "balanced",
                    ["continuous"] = config.Continuous
                },
                Priority = config.Priority,
                CreatedAt = DateTime.UtcNow
            };
        }

        private async Task ExecuteTaskAsync(AutoPilotTask task, CancellationToken cancellationToken)
        {
            Status.CurrentTask = task.TaskType;
            Log($"Executing task: {task.TaskType}", LogLevel.Info);

            try
            {
                switch (task.TaskType)
                {
                    case "collect_resources":
                        await ExecuteCollectResourcesAsync(cancellationToken);
                        break;
                    case "train_troops":
                        await ExecuteTrainTroopsAsync(task.Parameters, cancellationToken);
                        break;
                    case "attack_npcs":
                        await ExecuteAttackNpcsAsync(task.Parameters, cancellationToken);
                        break;
                    case "upgrade_buildings":
                        await ExecuteUpgradeBuildingsAsync(task.Parameters, cancellationToken);
                        break;
                    case "upgrade_resource_buildings":
                        await ExecuteUpgradeResourceBuildingsAsync(cancellationToken);
                        break;
                    case "upgrade_walls":
                        await ExecuteUpgradeWallsAsync(cancellationToken);
                        break;
                    default:
                        Log($"Unknown task type: {task.TaskType}", LogLevel.Warning);
                        break;
                }

                task.CompletedAt = DateTime.UtcNow;
                task.Status = "completed";
                TaskCompleted?.Invoke(task);
            }
            catch (Exception ex)
            {
                task.Status = "failed";
                task.Error = ex.Message;
                Log($"Task failed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Private Methods - Task Execution

        private async Task ExecuteCollectResourcesAsync(CancellationToken cancellationToken)
        {
            foreach (var city in _gameState.Cities.Values)
            {
                // Send collect all resources command
                await SendGameCommandAsync("castle.collectAllResources", new JObject
                {
                    ["castleId"] = city.CityId
                }, cancellationToken);

                ActionPerformed?.Invoke($"Collected resources in {city.Name}");
                await Task.Delay(GetHumanLikeDelay(), cancellationToken);
            }
        }

        private async Task ExecuteTrainTroopsAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var troopType = parameters.GetValueOrDefault("troopType")?.ToString() ?? "cavalry";
        
            if (troopType == "auto")
            {
                // Get recommendation from advisor
                var composition = _advisor.GetOptimalTroopComposition(0, "attack");
                troopType = composition.Troops.Keys.FirstOrDefault() ?? "cavalry";
            }

            foreach (var city in _gameState.Cities.Values)
            {
                // Check if barracks is available
                var barracks = city.Buildings.Values.FirstOrDefault(b => b.TypeId == 4);
                if (barracks == null) continue;

                // Calculate how many troops to train based on resources
                var amount = CalculateTrainableAmount(city, troopType);
                if (amount <= 0) continue;

                await SendGameCommandAsync("troop.trainTroop", new JObject
                {
                    ["castleId"] = city.CityId,
                    ["troopType"] = troopType,
                    ["amount"] = amount
                }, cancellationToken);

                ActionPerformed?.Invoke($"Training {amount} {troopType} in {city.Name}");
                await Task.Delay(GetHumanLikeDelay(), cancellationToken);
            }
        }

        private async Task ExecuteAttackNpcsAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var npcLevel = parameters.GetValueOrDefault("npcLevel")?.ToString() ?? "5-7";
        
            // Parse level range
            var levels = npcLevel.Split('-');
            var minLevel = int.Parse(levels[0]);
            var maxLevel = levels.Length > 1 ? int.Parse(levels[1]) : minLevel;

            // Find suitable NPC targets
            var targets = await FindNpcTargetsAsync(minLevel, maxLevel, cancellationToken);
        
            foreach (var target in targets.Take(3)) // Attack up to 3 NPCs
            {
                var analysis = await _advisor.AnalyzeAttackTargetAsync(target.X, target.Y, cancellationToken);
            
                if (analysis.ShouldAttack)
                {
                    await SendGameCommandAsync("army.sendArmy", new JObject
                    {
                        ["armyId"] = GetAvailableArmyId(),
                        ["targetX"] = target.X,
                        ["targetY"] = target.Y,
                        ["missionType"] = "attack"
                    }, cancellationToken);

                    ActionPerformed?.Invoke($"Attacking NPC at ({target.X}, {target.Y})");
                    await Task.Delay(GetHumanLikeDelay(), cancellationToken);
                }
            }
        }

        private async Task ExecuteUpgradeBuildingsAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var focus = parameters.GetValueOrDefault("focus")?.ToString() ?? "balanced";

            foreach (var city in _gameState.Cities.Values)
            {
                var buildOrder = await _advisor.GenerateBuildOrderAsync(city.CityId, focus, cancellationToken);
            
                if (buildOrder.Queue.Count > 0)
                {
                    var nextBuilding = buildOrder.Queue.First();
                
                    // Find the building position
                    var building = city.Buildings.Values
                        .FirstOrDefault(b => b.Name == nextBuilding.BuildingType);
                
                    if (building != null)
                    {
                        await SendGameCommandAsync("castle.upgradeBuilding", new JObject
                        {
                            ["castleId"] = city.CityId,
                            ["positionId"] = building.PositionId
                        }, cancellationToken);

                        ActionPerformed?.Invoke($"Upgrading {nextBuilding.BuildingType} in {city.Name}");
                    }
                }

                await Task.Delay(GetHumanLikeDelay(), cancellationToken);
            }
        }

        private async Task ExecuteUpgradeResourceBuildingsAsync(CancellationToken cancellationToken)
        {
            var resourceBuildingTypes = new[] { 20, 21, 22, 23 }; // Farm, Sawmill, Quarry, Ironmine

            foreach (var city in _gameState.Cities.Values)
            {
                var lowestBuilding = city.Buildings.Values
                    .Where(b => resourceBuildingTypes.Contains(b.TypeId))
                    .OrderBy(b => b.Level)
                    .FirstOrDefault();

                if (lowestBuilding != null)
                {
                    await SendGameCommandAsync("castle.upgradeBuilding", new JObject
                    {
                        ["castleId"] = city.CityId,
                        ["positionId"] = lowestBuilding.PositionId
                    }, cancellationToken);

                    ActionPerformed?.Invoke($"Upgrading {lowestBuilding.Name} in {city.Name}");
                }

                await Task.Delay(GetHumanLikeDelay(), cancellationToken);
            }
        }

        private async Task ExecuteUpgradeWallsAsync(CancellationToken cancellationToken)
        {
            foreach (var city in _gameState.Cities.Values)
            {
                var walls = city.Buildings.Values.FirstOrDefault(b => b.TypeId == 16);
            
                if (walls != null)
                {
                    await SendGameCommandAsync("castle.upgradeBuilding", new JObject
                    {
                        ["castleId"] = city.CityId,
                        ["positionId"] = walls.PositionId
                    }, cancellationToken);

                    ActionPerformed?.Invoke($"Upgrading walls in {city.Name}");
                }

                await Task.Delay(GetHumanLikeDelay(), cancellationToken);
            }
        }

        private async Task SpendExcessResourcesAsync(CancellationToken cancellationToken)
        {
            // Train troops to spend resources
            await ExecuteTrainTroopsAsync(new Dictionary<string, object> { ["troopType"] = "auto" }, cancellationToken);
        }

        #endregion

        #region Private Methods - Utilities

        private async Task SendGameCommandAsync(string action, JObject parameters, CancellationToken cancellationToken)
        {
            // This would integrate with the actual game communication
            // For now, we log the action
            Log($"Sending command: {action} with params: {parameters}", LogLevel.Debug);
        
            // Simulate command execution
            await Task.Delay(100, cancellationToken);
        }

        private async Task<List<(int X, int Y)>> FindNpcTargetsAsync(int minLevel, int maxLevel, CancellationToken cancellationToken)
        {
            // Query for NPC targets
            var result = await _mcpManager.CallToolAsync("evony-rag", "evony_search", new JObject
            {
                ["query"] = $"NPC level {minLevel} to {maxLevel} near player",
                ["limit"] = 10
            }, cancellationToken);

            // Parse results (simplified)
            return new List<(int, int)>
            {
                (100, 100), (105, 110), (95, 105) // Placeholder coordinates
            };
        }

        private int CalculateTrainableAmount(CityState city, string troopType)
        {
            // Calculate based on resources and barracks capacity
            var resources = city.Resources;
            var maxByFood = resources.Food / 50; // Simplified cost
            var maxByGold = resources.Gold / 20;
        
            return (int)Math.Min(maxByFood, Math.Min(maxByGold, 10000));
        }

        private int GetAvailableArmyId()
        {
            // Get first available army
            return _gameState.Armies.Values.FirstOrDefault()?.ArmyId ?? 1;
        }

        private bool EvaluateCondition(string condition)
        {
            // Simple condition evaluation
            // Format: "troops > 50000" or "resources < 1000000"
            var parts = condition.Split(' ');
            if (parts.Length != 3) return true;

            var variable = parts[0];
            var op = parts[1];
            var value = long.Parse(parts[2]);

            long actualValue = variable switch
            {
                "troops" => _gameState.GetTotalTroops().Values.Sum(),
                "resources" => _gameState.GetTotalResources().Gold + _gameState.GetTotalResources().Food,
                _ => 0
            };

            return op switch
            {
                ">" => actualValue > value,
                "<" => actualValue < value,
                ">=" => actualValue >= value,
                "<=" => actualValue <= value,
                "==" => actualValue == value,
                _ => true
            };
        }

        private TimeSpan GetHumanLikeDelay()
        {
            // Random delay between 1-5 seconds to mimic human behavior
            var random = new Random();
            return TimeSpan.FromMilliseconds(1000 + random.Next(4000));
        }

        private void OnUnderAttack(AttackAlert alert)
        {
            if (_currentStrategy.Safety.ShieldWhenAttacked)
            {
                Log($"Under attack! Activating shield for city {alert.TargetCityId}", LogLevel.Warning);
                // Queue shield activation task
                _taskQueue.Enqueue(new AutoPilotTask
                {
                    Id = Guid.NewGuid().ToString(),
                    TaskType = "activate_shield",
                    Parameters = new Dictionary<string, object> { ["cityId"] = alert.TargetCityId },
                    Priority = 0 // Highest priority
                });
            }
        }

        private void OnResourcesUpdated(int cityId, ResourceState resources)
        {
            // Update status
            Status.TotalResourcesCollected += 1000; // Simplified tracking
        }

        private void Log(string message, LogLevel level)
        {
            var log = new AutoPilotLog
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message
            };

            lock (_logLock)
            {
                _logs.Add(log);
                if (_logs.Count > 1000)
                    _logs.RemoveRange(0, 100);
            }

            App.Logger.Information("[AutoPilot] {Message}", message);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts?.Cancel();
            _cts?.Dispose();

            _gameState.UnderAttack -= OnUnderAttack;
            _gameState.ResourcesUpdated -= OnResourcesUpdated;

            App.Logger.Information("AutoPilotService disposed");
        }

        #endregion
    }

    #region Models

    public class AutoPilotStrategy
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<AutoPilotTaskConfig> Tasks { get; set; } = new List<AutoPilotTaskConfig>();
        public SafetyConfig Safety { get; set; } = new SafetyConfig();
    }

    public class AutoPilotTaskConfig
    {
        public string TaskType { get; set; } = "";
        public int Priority { get; set; }
        public TimeSpan? Interval { get; set; }
        public bool Continuous { get; set; }
        public string TroopType { get; set; }
        public string NpcLevel { get; set; }
        public string Focus { get; set; }
        public string Condition { get; set; }
        public DateTime? LastExecuted { get; set; }
    }

    public class SafetyConfig
    {
        public bool ShieldWhenAttacked { get; set; } = true;
        public int MinTroopReserve { get; set; } = 10000;
        public long MaxResourceExposure { get; set; } = 5000000;
    }

    public class AutoPilotTask
    {
        public string Id { get; set; } = "";
        public string TaskType { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new object>();
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "pending";
        public string Error { get; set; }
    }

    public class AutoPilotStatus
    {
        public bool IsRunning { get; set; }
        public bool IsPaused { get; set; }
        public string StrategyName { get; set; } = "";
        public string CurrentTask { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime? StopTime { get; set; }
        public DateTime LastActionTime { get; set; }
        public int TotalActions { get; set; }
        public long TotalResourcesCollected { get; set; }
        public int TotalAttacks { get; set; }
        public int TotalTroopsTrained { get; set; }
        public int ActionsPerformed { get; set; }
        public TimeSpan RunTime => IsRunning ? DateTime.Now - StartTime : (StopTime ?? DateTime.Now) - StartTime;
    }

    public class AutoPilotLog
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = "";
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    #endregion

}