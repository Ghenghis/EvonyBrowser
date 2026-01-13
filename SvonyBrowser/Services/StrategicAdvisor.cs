using System;
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
    /// AI-powered strategic advisor that analyzes game state and provides
    /// real-time recommendations using RAG-enhanced LLM reasoning.
    /// </summary>
    public sealed class StrategicAdvisor : IDisposable
    {
        #region Singleton

        private static readonly Lazy<StrategicAdvisor> _lazyInstance =
            new Lazy<StrategicAdvisor>(() => new StrategicAdvisor(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static StrategicAdvisor Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly GameStateEngine _gameState;
        private readonly McpConnectionManager _mcpManager;
        private readonly ChatbotService _chatbot;
        private bool _disposed = false;

        // Combat coefficients (from game data)
        private static readonly Dictionary<string, TroopStats> TroopData = new()
        {
            ["worker"] = new TroopStats { Attack = 5, Defense = 5, Life = 20, Speed = 100, Load = 20 },
            ["warrior"] = new TroopStats { Attack = 50, Defense = 50, Life = 200, Speed = 180, Load = 10 },
            ["scout"] = new TroopStats { Attack = 20, Defense = 20, Life = 50, Speed = 3000, Load = 5 },
            ["pikeman"] = new TroopStats { Attack = 150, Defense = 150, Life = 300, Speed = 300, Load = 15 },
            ["swordsman"] = new TroopStats { Attack = 100, Defense = 250, Life = 400, Speed = 275, Load = 20 },
            ["archer"] = new TroopStats { Attack = 120, Defense = 50, Life = 250, Speed = 250, Load = 10 },
            ["cavalry"] = new TroopStats { Attack = 250, Defense = 180, Life = 500, Speed = 1000, Load = 35 },
            ["cataphract"] = new TroopStats { Attack = 350, Defense = 350, Life = 1000, Speed = 750, Load = 45 },
            ["transporter"] = new TroopStats { Attack = 10, Defense = 10, Life = 100, Speed = 150, Load = 200 },
            ["ballista"] = new TroopStats { Attack = 450, Defense = 160, Life = 320, Speed = 100, Load = 20 },
            ["ram"] = new TroopStats { Attack = 250, Defense = 160, Life = 500, Speed = 120, Load = 30 },
            ["catapult"] = new TroopStats { Attack = 600, Defense = 200, Life = 480, Speed = 80, Load = 25 }
        };

        #endregion

        #region Constructor

        private StrategicAdvisor()
        {
            _gameState = GameStateEngine.Instance;
            _mcpManager = McpConnectionManager.Instance;
            _chatbot = ChatbotService.Instance;

            App.Logger.Information("StrategicAdvisor initialized");
        }

        #endregion

        #region Public Methods - Attack Analysis

        /// <summary>
        /// Analyzes a potential attack target and provides recommendations.
        /// </summary>
        public async Task<AttackAnalysis> AnalyzeAttackTargetAsync(
            int targetX, int targetY, 
            CancellationToken cancellationToken = default)
        {
            var analysis = new AttackAnalysis
            {
                TargetX = targetX,
                TargetY = targetY,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                // Get target information from RAG
                var targetInfo = await GetTargetInfoAsync(targetX, targetY, cancellationToken);
                analysis.TargetName = targetInfo?.Value<string>("name") ?? "Unknown";
                analysis.TargetPower = targetInfo?.Value<long>("power") ?? 0;
                analysis.TargetAlliance = targetInfo?.Value<string>("alliance") ?? "";
                analysis.IsOnline = targetInfo?.Value<bool>("online") ?? false;

                // Get our available troops
                var ourTroops = _gameState.GetTotalTroops();
                var ourPower = CalculateTroopPower(ourTroops);
                analysis.OurPower = ourPower;

                // Calculate distance and march time
                var nearestCity = GetNearestCity(targetX, targetY);
                if (nearestCity != null)
                {
                    analysis.Distance = CalculateDistance(nearestCity.X, nearestCity.Y, targetX, targetY);
                    analysis.MarchTime = CalculateMarchTime(analysis.Distance, "cavalry");
                    analysis.SourceCityId = nearestCity.CityId;
                }

                // Simulate combat
                var simulation = SimulateCombat(ourTroops, analysis.TargetPower);
                analysis.WinProbability = simulation.WinProbability;
                analysis.ExpectedLosses = simulation.AttackerLosses;
                analysis.ExpectedLoot = simulation.LootEstimate;

                // Generate recommendations
                analysis.Recommendations = GenerateAttackRecommendations(analysis, ourTroops);
            
                // Risk assessment
                analysis.RiskLevel = AssessRisk(analysis);

                // Final recommendation
                analysis.ShouldAttack = analysis.WinProbability >= 0.75 && analysis.RiskLevel != "HIGH";
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error analyzing attack target");
                analysis.Error = ex.Message;
            }

            return analysis;
        }

        /// <summary>
        /// Gets optimal troop composition for an attack.
        /// </summary>
        public TroopComposition GetOptimalTroopComposition(long targetPower, string missionType = "attack")
        {
            var composition = new TroopComposition();
            var availableTroops = _gameState.GetTotalTroops();

            // Calculate required power (1.5x target for safety)
            var requiredPower = (long)(targetPower * 1.5);

            switch (missionType.ToLower())
            {
                case "attack":
                    // Prioritize cavalry and archers for attacks
                    composition = CalculateAttackComposition(availableTroops, requiredPower);
                    break;
                case "scout":
                    // Use scouts only
                    composition.Troops["scout"] = Math.Min(availableTroops.GetValueOrDefault("scout"), 10000);
                    break;
                case "reinforce":
                    // Balanced defensive composition
                    composition = CalculateDefenseComposition(availableTroops, requiredPower);
                    break;
                case "transport":
                    // Maximize load capacity
                    composition = CalculateTransportComposition(availableTroops);
                    break;
            }

            composition.TotalPower = CalculateTroopPower(composition.Troops);
            return composition;
        }

        #endregion

        #region Public Methods - Build Order

        /// <summary>
        /// Generates an optimal build order for a city.
        /// </summary>
        public async Task<BuildOrder> GenerateBuildOrderAsync(
            int cityId, 
            string focus = "balanced",
            CancellationToken cancellationToken = default)
        {
            var buildOrder = new BuildOrder
            {
                CityId = cityId,
                Focus = focus,
                Timestamp = DateTime.UtcNow
            };

            if (!_gameState.Cities.TryGetValue(cityId, out var city))
            {
                buildOrder.Error = "City not found";
                return buildOrder;
            }

            try
            {
                // Analyze current city state
                var townHallLevel = city.Buildings.Values
                    .FirstOrDefault(b => b.TypeId == 1)?.Level ?? 1;

                // Generate build queue based on focus
                var queue = focus.ToLower() switch
                {
                    "military" => GenerateMilitaryBuildOrder(city, townHallLevel),
                    "economy" => GenerateEconomyBuildOrder(city, townHallLevel),
                    "research" => GenerateResearchBuildOrder(city, townHallLevel),
                    "defense" => GenerateDefenseBuildOrder(city, townHallLevel),
                    _ => GenerateBalancedBuildOrder(city, townHallLevel)
                };

                buildOrder.Queue = queue;

                // Calculate total time and resources
                foreach (var item in queue)
                {
                    buildOrder.TotalTime += item.Duration;
                    buildOrder.TotalGold += item.GoldCost;
                    buildOrder.TotalFood += item.FoodCost;
                    buildOrder.TotalLumber += item.LumberCost;
                    buildOrder.TotalStone += item.StoneCost;
                    buildOrder.TotalIron += item.IronCost;
                }

                // Add AI recommendations
                buildOrder.Recommendations = await GetBuildRecommendationsAsync(city, focus, cancellationToken);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error generating build order");
                buildOrder.Error = ex.Message;
            }

            return buildOrder;
        }

        #endregion

        #region Public Methods - Hero Skills

        /// <summary>
        /// Gets optimal skill point allocation for a hero.
        /// </summary>
        public HeroSkillRecommendation GetHeroSkillRecommendation(int heroId, string role = "auto")
        {
            var recommendation = new HeroSkillRecommendation
            {
                HeroId = heroId,
                Timestamp = DateTime.UtcNow
            };

            if (!_gameState.Heroes.TryGetValue(heroId, out var hero))
            {
                recommendation.Error = "Hero not found";
                return recommendation;
            }

            recommendation.HeroName = hero.Name;
            recommendation.CurrentLevel = hero.Level;

            // Determine role if auto
            if (role == "auto")
            {
                role = DetermineHeroRole(hero);
            }
            recommendation.RecommendedRole = role;

            // Calculate available points
            var totalPoints = hero.Level * 5; // 5 points per level
            var usedPoints = hero.Politics + hero.Attack + hero.Intelligence - 15; // Base stats
            var availablePoints = totalPoints - usedPoints;
            recommendation.AvailablePoints = availablePoints;

            // Generate allocation based on role
            switch (role.ToLower())
            {
                case "mayor":
                    recommendation.PoliticsAllocation = (int)(availablePoints * 0.7);
                    recommendation.AttackAllocation = (int)(availablePoints * 0.1);
                    recommendation.IntelligenceAllocation = (int)(availablePoints * 0.2);
                    recommendation.Explanation = "Mayors benefit most from Politics for resource production and population growth.";
                    break;
                case "attacker":
                    recommendation.PoliticsAllocation = (int)(availablePoints * 0.1);
                    recommendation.AttackAllocation = (int)(availablePoints * 0.8);
                    recommendation.IntelligenceAllocation = (int)(availablePoints * 0.1);
                    recommendation.Explanation = "Attack heroes should maximize Attack for combat effectiveness.";
                    break;
                case "defender":
                    recommendation.PoliticsAllocation = (int)(availablePoints * 0.1);
                    recommendation.AttackAllocation = (int)(availablePoints * 0.4);
                    recommendation.IntelligenceAllocation = (int)(availablePoints * 0.5);
                    recommendation.Explanation = "Defense heroes benefit from Intelligence for trap damage and wall defense.";
                    break;
                default:
                    recommendation.PoliticsAllocation = (int)(availablePoints * 0.33);
                    recommendation.AttackAllocation = (int)(availablePoints * 0.34);
                    recommendation.IntelligenceAllocation = (int)(availablePoints * 0.33);
                    recommendation.Explanation = "Balanced allocation for versatile use.";
                    break;
            }

            return recommendation;
        }

        #endregion

        #region Public Methods - Resource Management

        /// <summary>
        /// Generates a resource management plan.
        /// </summary>
        public ResourcePlan GenerateResourcePlan(int daysAhead = 7)
        {
            var plan = new ResourcePlan
            {
                DaysAhead = daysAhead,
                Timestamp = DateTime.UtcNow
            };

            var currentResources = _gameState.GetTotalResources();
            var productionRates = _gameState.GetProductionRates();

            // Current state
            plan.CurrentGold = currentResources.Gold;
            plan.CurrentFood = currentResources.Food;
            plan.CurrentLumber = currentResources.Lumber;
            plan.CurrentStone = currentResources.Stone;
            plan.CurrentIron = currentResources.Iron;

            // Daily production
            plan.DailyGoldProduction = productionRates.Gold * 24;
            plan.DailyFoodProduction = productionRates.Food * 24;
            plan.DailyLumberProduction = productionRates.Lumber * 24;
            plan.DailyStoneProduction = productionRates.Stone * 24;
            plan.DailyIronProduction = productionRates.Iron * 24;

            // Projected resources
            plan.ProjectedGold = currentResources.Gold + (plan.DailyGoldProduction * daysAhead);
            plan.ProjectedFood = currentResources.Food + (plan.DailyFoodProduction * daysAhead);
            plan.ProjectedLumber = currentResources.Lumber + (plan.DailyLumberProduction * daysAhead);
            plan.ProjectedStone = currentResources.Stone + (plan.DailyStoneProduction * daysAhead);
            plan.ProjectedIron = currentResources.Iron + (plan.DailyIronProduction * daysAhead);

            // Generate recommendations
            plan.Recommendations = new List<string>();

            // Check for imbalances
            var avgProduction = (plan.DailyFoodProduction + plan.DailyLumberProduction + 
                               plan.DailyStoneProduction + plan.DailyIronProduction) / 4;

            if (plan.DailyFoodProduction < avgProduction * 0.7)
                plan.Recommendations.Add("Food production is low. Consider upgrading farms.");
            if (plan.DailyLumberProduction < avgProduction * 0.7)
                plan.Recommendations.Add("Lumber production is low. Consider upgrading sawmills.");
            if (plan.DailyStoneProduction < avgProduction * 0.7)
                plan.Recommendations.Add("Stone production is low. Consider upgrading quarries.");
            if (plan.DailyIronProduction < avgProduction * 0.7)
                plan.Recommendations.Add("Iron production is low. Consider upgrading iron mines.");

            // Check for excess resources (attack risk)
            var totalResources = currentResources.Gold + currentResources.Food + 
                               currentResources.Lumber + currentResources.Stone + currentResources.Iron;
            if (totalResources > 10000000)
                plan.Recommendations.Add("High resource stockpile detected. Consider spending or shielding to avoid losses from attacks.");

            return plan;
        }

        #endregion

        #region Private Methods - Combat Simulation

        private CombatSimulation SimulateCombat(Dictionary<string, int> attackerTroops, long defenderPower)
        {
            var simulation = new CombatSimulation();

            var attackerPower = CalculateTroopPower(attackerTroops);
            var powerRatio = (double)attackerPower / Math.Max(defenderPower, 1);

            // Win probability based on power ratio
            simulation.WinProbability = Math.Min(0.99, powerRatio / (powerRatio + 1));

            // Expected losses (simplified model)
            var lossRatio = 1.0 / (powerRatio + 1);
            simulation.AttackerLosses = new Dictionary<string, int>();
        
            foreach (var troop in attackerTroops)
            {
                simulation.AttackerLosses[troop.Key] = (int)(troop.Value * lossRatio * 0.5);
            }

            // Loot estimate (based on defender power)
            simulation.LootEstimate = new ResourceState
            {
                Gold = (long)(defenderPower * 0.1 * simulation.WinProbability),
                Food = (long)(defenderPower * 0.2 * simulation.WinProbability),
                Lumber = (long)(defenderPower * 0.15 * simulation.WinProbability),
                Stone = (long)(defenderPower * 0.1 * simulation.WinProbability),
                Iron = (long)(defenderPower * 0.1 * simulation.WinProbability)
            };

            return simulation;
        }

        private long CalculateTroopPower(Dictionary<string, int> troops)
        {
            long power = 0;
            foreach (var troop in troops)
            {
                if (TroopData.TryGetValue(troop.Key.ToLower(), out var stats))
                {
                    power += (long)troop.Value * (stats.Attack + stats.Defense + stats.Life / 10);
                }
            }
            return power;
        }

        private TroopComposition CalculateAttackComposition(Dictionary<string, int> available, long requiredPower)
        {
            var composition = new TroopComposition();
            var currentPower = 0L;

            // Priority: Cavalry > Cataphract > Archer > Swordsman
            var priorities = new[] { "cavalry", "cataphract", "archer", "swordsman", "pikeman" };

            foreach (var troopType in priorities)
            {
                if (currentPower >= requiredPower) break;
            
                var availableCount = available.GetValueOrDefault(troopType);
                if (availableCount == 0) continue;

                var stats = TroopData[troopType];
                var powerPerTroop = stats.Attack + stats.Defense + stats.Life / 10;
                var neededTroops = (int)Math.Ceiling((requiredPower - currentPower) / (double)powerPerTroop);
                var troopsToUse = Math.Min(availableCount, neededTroops);

                composition.Troops[troopType] = troopsToUse;
                currentPower += troopsToUse * powerPerTroop;
            }

            return composition;
        }

        private TroopComposition CalculateDefenseComposition(Dictionary<string, int> available, long requiredPower)
        {
            var composition = new TroopComposition();
            // Defense prioritizes high-defense troops
            var priorities = new[] { "swordsman", "cataphract", "pikeman", "archer" };
        
            // Similar logic to attack composition
            var currentPower = 0L;
            foreach (var troopType in priorities)
            {
                if (currentPower >= requiredPower) break;
                var availableCount = available.GetValueOrDefault(troopType);
                if (availableCount == 0) continue;

                var stats = TroopData[troopType];
                var powerPerTroop = stats.Attack + stats.Defense + stats.Life / 10;
                var neededTroops = (int)Math.Ceiling((requiredPower - currentPower) / (double)powerPerTroop);
                var troopsToUse = Math.Min(availableCount, neededTroops);

                composition.Troops[troopType] = troopsToUse;
                currentPower += troopsToUse * powerPerTroop;
            }

            return composition;
        }

        private TroopComposition CalculateTransportComposition(Dictionary<string, int> available)
        {
            var composition = new TroopComposition();
            // Maximize load capacity
            composition.Troops["transporter"] = available.GetValueOrDefault("transporter");
            composition.Troops["cavalry"] = available.GetValueOrDefault("cavalry"); // Good load + speed
            return composition;
        }

        #endregion

        #region Private Methods - Utilities

        private async Task<JObject> GetTargetInfoAsync(int x, int y, CancellationToken cancellationToken)
        {
            return await _mcpManager.CallToolAsync("evony-rag", "evony_search", new JObject
            {
                ["query"] = $"player at coordinates {x},{y}",
                ["limit"] = 1
            }, cancellationToken);
        }

        private CityState? GetNearestCity(int targetX, int targetY)
        {
            return _gameState.Cities.Values
                .OrderBy(c => CalculateDistance(c.X, c.Y, targetX, targetY))
                .FirstOrDefault();
        }

        private double CalculateDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        private TimeSpan CalculateMarchTime(double distance, string troopType)
        {
            var speed = TroopData.GetValueOrDefault(troopType)?.Speed ?? 250;
            var seconds = distance * 6000 / speed; // Simplified formula
            return TimeSpan.FromSeconds(seconds);
        }

        private List<string> GenerateAttackRecommendations(AttackAnalysis analysis, Dictionary<string, int> ourTroops)
        {
            var recommendations = new List<string>();

            if (analysis.WinProbability < 0.5)
                recommendations.Add("Low win probability. Consider training more troops before attacking.");
            else if (analysis.WinProbability < 0.75)
                recommendations.Add("Moderate win probability. Add more cavalry for better odds.");
            else if (analysis.WinProbability < 0.9)
                recommendations.Add("Good win probability. Attack is recommended.");
            else
                recommendations.Add("Excellent win probability. Easy victory expected.");

            if (analysis.IsOnline)
                recommendations.Add("Target is currently ONLINE. They may reinforce or shield.");
            else
                recommendations.Add("Target appears OFFLINE. Good time to attack.");

            if (!string.IsNullOrEmpty(analysis.TargetAlliance))
                recommendations.Add($"Target is in alliance [{analysis.TargetAlliance}]. Expect possible reinforcements.");

            if (analysis.MarchTime > TimeSpan.FromMinutes(30))
                recommendations.Add("Long march time. Consider using speed items.");

            return recommendations;
        }

        private string AssessRisk(AttackAnalysis analysis)
        {
            if (analysis.WinProbability < 0.5) return "HIGH";
            if (analysis.IsOnline) return "MEDIUM";
            if (!string.IsNullOrEmpty(analysis.TargetAlliance)) return "MEDIUM";
            if (analysis.WinProbability >= 0.85) return "LOW";
            return "MEDIUM";
        }

        private string DetermineHeroRole(HeroState hero)
        {
            // Determine role based on current stats
            var maxStat = Math.Max(hero.Politics, Math.Max(hero.Attack, hero.Intelligence));
        
            if (hero.Politics == maxStat) return "mayor";
            if (hero.Attack == maxStat) return "attacker";
            return "defender";
        }

        private List<BuildQueueItem> GenerateBalancedBuildOrder(CityState city, int townHallLevel)
        {
            var queue = new List<BuildQueueItem>();
            // Add balanced build items
            queue.Add(new BuildQueueItem { BuildingType = "Town Hall", TargetLevel = townHallLevel + 1, Priority = 1 });
            queue.Add(new BuildQueueItem { BuildingType = "Barracks", TargetLevel = townHallLevel, Priority = 2 });
            queue.Add(new BuildQueueItem { BuildingType = "Academy", TargetLevel = townHallLevel, Priority = 3 });
            return queue;
        }

        private List<BuildQueueItem> GenerateMilitaryBuildOrder(CityState city, int townHallLevel)
        {
            var queue = new List<BuildQueueItem>();
            queue.Add(new BuildQueueItem { BuildingType = "Barracks", TargetLevel = townHallLevel + 1, Priority = 1 });
            queue.Add(new BuildQueueItem { BuildingType = "Stable", TargetLevel = townHallLevel, Priority = 2 });
            queue.Add(new BuildQueueItem { BuildingType = "Workshop", TargetLevel = townHallLevel, Priority = 3 });
            return queue;
        }

        private List<BuildQueueItem> GenerateEconomyBuildOrder(CityState city, int townHallLevel)
        {
            var queue = new List<BuildQueueItem>();
            queue.Add(new BuildQueueItem { BuildingType = "Farm", TargetLevel = townHallLevel, Priority = 1 });
            queue.Add(new BuildQueueItem { BuildingType = "Sawmill", TargetLevel = townHallLevel, Priority = 2 });
            queue.Add(new BuildQueueItem { BuildingType = "Cottage", TargetLevel = townHallLevel, Priority = 3 });
            return queue;
        }

        private List<BuildQueueItem> GenerateResearchBuildOrder(CityState city, int townHallLevel)
        {
            var queue = new List<BuildQueueItem>();
            queue.Add(new BuildQueueItem { BuildingType = "Academy", TargetLevel = townHallLevel + 1, Priority = 1 });
            return queue;
        }

        private List<BuildQueueItem> GenerateDefenseBuildOrder(CityState city, int townHallLevel)
        {
            var queue = new List<BuildQueueItem>();
            queue.Add(new BuildQueueItem { BuildingType = "Walls", TargetLevel = townHallLevel + 1, Priority = 1 });
            queue.Add(new BuildQueueItem { BuildingType = "Beacon Tower", TargetLevel = townHallLevel, Priority = 2 });
            return queue;
        }

        private async Task<List<string>> GetBuildRecommendationsAsync(CityState city, string focus, CancellationToken cancellationToken)
        {
            var recommendations = new List<string>();
        
            // Query RAG for build recommendations
            var result = await _mcpManager.CallToolAsync("evony-rag", "evony_search", new JObject
            {
                ["query"] = $"optimal build order for {focus} focus",
                ["limit"] = 5
            }, cancellationToken);

            if (result?["results"] is JArray results)
            {
                foreach (var r in results.Take(3))
                {
                    var text = r["text"]?.Value<string>();
                    if (!string.IsNullOrEmpty(text))
                        recommendations.Add(text);
                }
            }

            return recommendations;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            App.Logger.Information("StrategicAdvisor disposed");
        }

        #endregion
    }

    #region Models

    public class TroopStats
    {
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Life { get; set; }
        public int Speed { get; set; }
        public int Load { get; set; }
    }

    public class AttackAnalysis
    {
        public int TargetX { get; set; }
        public int TargetY { get; set; }
        public string TargetName { get; set; } = "";
        public long TargetPower { get; set; }
        public string TargetAlliance { get; set; } = "";
        public bool IsOnline { get; set; }
        public long OurPower { get; set; }
        public double Distance { get; set; }
        public TimeSpan MarchTime { get; set; }
        public int SourceCityId { get; set; }
        public double WinProbability { get; set; }
        public Dictionary<string, int> ExpectedLosses { get; set; } = new Dictionary<string, int>();
        public ResourceState ExpectedLoot { get; set; } = new ResourceState();
        public List<string> Recommendations { get; set; } = new List<string>();
        public string RiskLevel { get; set; } = "MEDIUM";
        public bool ShouldAttack { get; set; }
        public DateTime Timestamp { get; set; }
        public string Error { get; set; }
    }

    public class TroopComposition
    {
        public Dictionary<string, int> Troops { get; set; } = new Dictionary<string, int>();
        public long TotalPower { get; set; }
    }

    public class CombatSimulation
    {
        public double WinProbability { get; set; }
        public Dictionary<string, int> AttackerLosses { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DefenderLosses { get; set; } = new Dictionary<string, int>();
        public ResourceState LootEstimate { get; set; } = new ResourceState();
    }

    public class BuildOrder
    {
        public int CityId { get; set; }
        public string Focus { get; set; } = "";
        public List<BuildQueueItem> Queue { get; set; } = new List<BuildQueueItem>();
        public TimeSpan TotalTime { get; set; }
        public long TotalGold { get; set; }
        public long TotalFood { get; set; }
        public long TotalLumber { get; set; }
        public long TotalStone { get; set; }
        public long TotalIron { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; }
        public string Error { get; set; }
    }

    public class BuildQueueItem
    {
        public string BuildingType { get; set; } = "";
        public int TargetLevel { get; set; }
        public int Priority { get; set; }
        public TimeSpan Duration { get; set; }
        public long GoldCost { get; set; }
        public long FoodCost { get; set; }
        public long LumberCost { get; set; }
        public long StoneCost { get; set; }
        public long IronCost { get; set; }
    }

    public class HeroSkillRecommendation
    {
        public int HeroId { get; set; }
        public string HeroName { get; set; } = "";
        public int CurrentLevel { get; set; }
        public string RecommendedRole { get; set; } = "";
        public int AvailablePoints { get; set; }
        public int PoliticsAllocation { get; set; }
        public int AttackAllocation { get; set; }
        public int IntelligenceAllocation { get; set; }
        public string Explanation { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Error { get; set; }
    }

    public class ResourcePlan
    {
        public int DaysAhead { get; set; }
        public long CurrentGold { get; set; }
        public long CurrentFood { get; set; }
        public long CurrentLumber { get; set; }
        public long CurrentStone { get; set; }
        public long CurrentIron { get; set; }
        public long DailyGoldProduction { get; set; }
        public long DailyFoodProduction { get; set; }
        public long DailyLumberProduction { get; set; }
        public long DailyStoneProduction { get; set; }
        public long DailyIronProduction { get; set; }
        public long ProjectedGold { get; set; }
        public long ProjectedFood { get; set; }
        public long ProjectedLumber { get; set; }
        public long ProjectedStone { get; set; }
        public long ProjectedIron { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; }
    }

    #endregion

}