using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
namespace SvonyBrowser.Services
{

    /// <summary>
    /// Full combat simulator that predicts battle outcomes with AI-powered analysis
    /// of optimal strategies, including hero skills, technology, and terrain effects.
    /// </summary>
    public sealed class CombatSimulator : IDisposable
    {
        #region Singleton

        private static readonly Lazy<CombatSimulator> _lazyInstance =
            new Lazy<CombatSimulator>(() => new CombatSimulator(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static CombatSimulator Instance => _lazyInstance.Value;

        #endregion

        #region Constants - Troop Data

        private static readonly Dictionary<string, TroopDefinition> TroopDefinitions = new()
        {
            ["worker"] = new TroopDefinition
            {
                Name = "Worker", Code = "wo", Tier = 1,
                Attack = 5, Defense = 5, Life = 20, Speed = 100, Load = 20,
                FoodCost = 10, GoldCost = 50, TrainTime = 15,
                StrongAgainst = new[] { "none" }, WeakAgainst = new[] { "all" }
            },
            ["warrior"] = new TroopDefinition
            {
                Name = "Warrior", Code = "w", Tier = 1,
                Attack = 50, Defense = 50, Life = 200, Speed = 180, Load = 10,
                FoodCost = 30, GoldCost = 100, TrainTime = 30,
                StrongAgainst = new[] { "scout" }, WeakAgainst = new[] { "cavalry" }
            },
            ["scout"] = new TroopDefinition
            {
                Name = "Scout", Code = "s", Tier = 1,
                Attack = 20, Defense = 20, Life = 50, Speed = 3000, Load = 5,
                FoodCost = 20, GoldCost = 80, TrainTime = 20,
                StrongAgainst = new[] { "none" }, WeakAgainst = new[] { "warrior" }
            },
            ["pikeman"] = new TroopDefinition
            {
                Name = "Pikeman", Code = "p", Tier = 2,
                Attack = 150, Defense = 150, Life = 300, Speed = 300, Load = 15,
                FoodCost = 100, GoldCost = 300, TrainTime = 60,
                StrongAgainst = new[] { "cavalry", "cataphract" }, WeakAgainst = new[] { "archer" }
            },
            ["swordsman"] = new TroopDefinition
            {
                Name = "Swordsman", Code = "sw", Tier = 2,
                Attack = 100, Defense = 250, Life = 400, Speed = 275, Load = 20,
                FoodCost = 120, GoldCost = 350, TrainTime = 75,
                StrongAgainst = new[] { "archer" }, WeakAgainst = new[] { "cavalry" }
            },
            ["archer"] = new TroopDefinition
            {
                Name = "Archer", Code = "a", Tier = 2,
                Attack = 120, Defense = 50, Life = 250, Speed = 250, Load = 10,
                FoodCost = 80, GoldCost = 250, TrainTime = 50,
                StrongAgainst = new[] { "pikeman", "swordsman" }, WeakAgainst = new[] { "cavalry" }
            },
            ["cavalry"] = new TroopDefinition
            {
                Name = "Cavalry", Code = "c", Tier = 3,
                Attack = 250, Defense = 180, Life = 500, Speed = 1000, Load = 35,
                FoodCost = 200, GoldCost = 600, TrainTime = 120,
                StrongAgainst = new[] { "archer", "warrior", "swordsman" }, WeakAgainst = new[] { "pikeman" }
            },
            ["cataphract"] = new TroopDefinition
            {
                Name = "Cataphract", Code = "cata", Tier = 3,
                Attack = 350, Defense = 350, Life = 1000, Speed = 750, Load = 45,
                FoodCost = 300, GoldCost = 900, TrainTime = 180,
                StrongAgainst = new[] { "archer", "cavalry" }, WeakAgainst = new[] { "pikeman" }
            },
            ["transporter"] = new TroopDefinition
            {
                Name = "Transporter", Code = "t", Tier = 2,
                Attack = 10, Defense = 10, Life = 100, Speed = 150, Load = 200,
                FoodCost = 50, GoldCost = 150, TrainTime = 40,
                StrongAgainst = new[] { "none" }, WeakAgainst = new[] { "all" }
            },
            ["ballista"] = new TroopDefinition
            {
                Name = "Ballista", Code = "b", Tier = 3,
                Attack = 450, Defense = 160, Life = 320, Speed = 100, Load = 20,
                FoodCost = 250, GoldCost = 750, TrainTime = 150,
                StrongAgainst = new[] { "ram", "catapult" }, WeakAgainst = new[] { "cavalry" }
            },
            ["ram"] = new TroopDefinition
            {
                Name = "Battering Ram", Code = "r", Tier = 3,
                Attack = 250, Defense = 160, Life = 500, Speed = 120, Load = 30,
                FoodCost = 200, GoldCost = 600, TrainTime = 130,
                StrongAgainst = new[] { "fortifications" }, WeakAgainst = new[] { "ballista" }
            },
            ["catapult"] = new TroopDefinition
            {
                Name = "Catapult", Code = "cp", Tier = 4,
                Attack = 600, Defense = 200, Life = 480, Speed = 80, Load = 25,
                FoodCost = 350, GoldCost = 1000, TrainTime = 200,
                StrongAgainst = new[] { "fortifications", "all" }, WeakAgainst = new[] { "cavalry", "ballista" }
            }
        };

        // Technology bonuses
        private static readonly Dictionary<string, double> TechBonuses = new()
        {
            ["military_science_1"] = 0.05,
            ["military_science_2"] = 0.10,
            ["military_science_3"] = 0.15,
            ["military_science_4"] = 0.20,
            ["military_science_5"] = 0.25,
            ["iron_working_1"] = 0.05,
            ["iron_working_2"] = 0.10,
            ["iron_working_3"] = 0.15,
            ["medicine_1"] = 0.05,
            ["medicine_2"] = 0.10
        };

        #endregion

        #region Constructor

        private CombatSimulator()
        {
            App.Logger.Information("CombatSimulator initialized");
        }

        #endregion

        #region Public Methods - Simulation

        /// <summary>
        /// Simulates a battle between attacker and defender.
        /// </summary>
        public BattleResult SimulateBattle(BattleSetup setup)
        {
            var result = new BattleResult
            {
                Timestamp = DateTime.UtcNow,
                AttackerName = setup.AttackerName,
                DefenderName = setup.DefenderName
            };

            try
            {
                // Calculate effective stats with bonuses
                var attackerStats = CalculateArmyStats(setup.AttackerTroops, setup.AttackerHero, setup.AttackerTech);
                var defenderStats = CalculateArmyStats(setup.DefenderTroops, setup.DefenderHero, setup.DefenderTech);

                // Apply wall bonus to defender
                if (setup.WallLevel > 0)
                {
                    defenderStats.Defense *= (1 + setup.WallLevel * 0.05);
                }

                // Apply terrain modifiers
                ApplyTerrainModifiers(ref attackerStats, ref defenderStats, setup.Terrain);

                // Run combat rounds
                var rounds = SimulateCombatRounds(
                    setup.AttackerTroops.ToDictionary(kv => kv.Key, kv => kv.Value),
                    setup.DefenderTroops.ToDictionary(kv => kv.Key, kv => kv.Value),
                    attackerStats, defenderStats);

                result.Rounds = rounds;
                result.TotalRounds = rounds.Count;

                // Calculate final results
                var lastRound = rounds.Last();
                result.AttackerSurvivors = lastRound.AttackerRemaining;
                result.DefenderSurvivors = lastRound.DefenderRemaining;

                result.AttackerLosses = CalculateLosses(setup.AttackerTroops, result.AttackerSurvivors);
                result.DefenderLosses = CalculateLosses(setup.DefenderTroops, result.DefenderSurvivors);

                // Determine winner
                var attackerRemaining = result.AttackerSurvivors.Values.Sum();
                var defenderRemaining = result.DefenderSurvivors.Values.Sum();

                if (attackerRemaining > 0 && defenderRemaining == 0)
                {
                    result.Winner = "attacker";
                    result.WinProbability = 1.0;
                }
                else if (defenderRemaining > 0 && attackerRemaining == 0)
                {
                    result.Winner = "defender";
                    result.WinProbability = 0.0;
                }
                else
                {
                    // Partial victory - calculate based on remaining power
                    var attackerPower = CalculatePower(result.AttackerSurvivors);
                    var defenderPower = CalculatePower(result.DefenderSurvivors);
                    result.WinProbability = attackerPower / (attackerPower + defenderPower + 1);
                    result.Winner = result.WinProbability > 0.5 ? "attacker" : "defender";
                }

                // Calculate loot if attacker wins
                if (result.Winner == "attacker")
                {
                    result.LootCaptured = CalculateLoot(result.AttackerSurvivors, setup.DefenderResources);
                }

                // Generate analysis
                result.Analysis = GenerateBattleAnalysis(setup, result);
                result.Recommendations = GenerateRecommendations(setup, result);
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                App.Logger.Error(ex, "Combat simulation error");
            }

            return result;
        }

        /// <summary>
        /// Runs multiple simulations with random variance for probability analysis.
        /// </summary>
        public BattleProbabilityAnalysis RunProbabilityAnalysis(BattleSetup setup, int iterations = 100)
        {
            var analysis = new BattleProbabilityAnalysis
            {
                Iterations = iterations,
                Setup = setup
            };

            var wins = 0;
            var losses = new List<Dictionary<string, int>>();
            var random = new Random();

            for (int i = 0; i < iterations; i++)
            {
                // Add random variance (±10%)
                var variedSetup = ApplyVariance(setup, random);
                var result = SimulateBattle(variedSetup);

                if (result.Winner == "attacker")
                    wins++;

                losses.Add(result.AttackerLosses);
            }

            analysis.WinRate = (double)wins / iterations;
            analysis.LossRate = 1 - analysis.WinRate;

            // Calculate average losses
            analysis.AverageLosses = new Dictionary<string, int>();
            foreach (var troopType in setup.AttackerTroops.Keys)
            {
                var avgLoss = losses.Where(l => l.ContainsKey(troopType))
                                   .Select(l => l[troopType])
                                   .DefaultIfEmpty(0)
                                   .Average();
                analysis.AverageLosses[troopType] = (int)avgLoss;
            }

            // Confidence intervals
            analysis.WinRateConfidence = CalculateConfidenceInterval(wins, iterations);

            return analysis;
        }

        /// <summary>
        /// Finds the optimal troop composition for a given target.
        /// </summary>
        public OptimalComposition FindOptimalComposition(
            Dictionary<string, int> availableTroops,
            Dictionary<string, int> defenderTroops,
            double targetWinRate = 0.9)
        {
            var optimal = new OptimalComposition
            {
                TargetWinRate = targetWinRate
            };

            // Start with minimum viable force
            var composition = new Dictionary<string, int>();
            var defenderPower = CalculatePower(defenderTroops);

            // Priority order for troop selection
            var priorities = new[] { "cavalry", "cataphract", "archer", "swordsman", "pikeman", "ballista" };

            foreach (var troopType in priorities)
            {
                if (!availableTroops.ContainsKey(troopType) || availableTroops[troopType] == 0)
                    continue;

                // Binary search for optimal count
                var minCount = 0;
                var maxCount = availableTroops[troopType];

                while (minCount < maxCount)
                {
                    var midCount = (minCount + maxCount) / 2;
                    composition[troopType] = midCount;

                    var setup = new BattleSetup
                    {
                        AttackerTroops = composition,
                        DefenderTroops = defenderTroops
                    };

                    var analysis = RunProbabilityAnalysis(setup, 20);

                    if (analysis.WinRate >= targetWinRate)
                    {
                        maxCount = midCount;
                    }
                    else
                    {
                        minCount = midCount + 1;
                    }
                }

                composition[troopType] = minCount;

                // Check if we've reached target
                var finalSetup = new BattleSetup
                {
                    AttackerTroops = composition,
                    DefenderTroops = defenderTroops
                };
                var finalAnalysis = RunProbabilityAnalysis(finalSetup, 50);

                if (finalAnalysis.WinRate >= targetWinRate)
                {
                    optimal.Troops = composition;
                    optimal.AchievedWinRate = finalAnalysis.WinRate;
                    optimal.ExpectedLosses = finalAnalysis.AverageLosses;
                    optimal.TotalPower = CalculatePower(composition);
                    break;
                }
            }

            return optimal;
        }

        #endregion

        #region Public Methods - Analysis

        /// <summary>
        /// Analyzes troop matchups and returns effectiveness matrix.
        /// </summary>
        public Dictionary<string, Dictionary<string, double>> GetEffectivenessMatrix()
        {
            var matrix = new Dictionary<string, Dictionary<string, double>>();

            foreach (var attacker in TroopDefinitions.Keys)
            {
                matrix[attacker] = new Dictionary<string, double>();
                var attackerDef = TroopDefinitions[attacker];

                foreach (var defender in TroopDefinitions.Keys)
                {
                    var defenderDef = TroopDefinitions[defender];
                    var effectiveness = 1.0;

                    if (attackerDef.StrongAgainst.Contains(defender))
                        effectiveness = 1.5;
                    else if (attackerDef.WeakAgainst.Contains(defender))
                        effectiveness = 0.5;

                    matrix[attacker][defender] = effectiveness;
                }
            }

            return matrix;
        }

        /// <summary>
        /// Calculates the power of a troop composition.
        /// </summary>
        public long CalculatePower(Dictionary<string, int> troops)
        {
            long power = 0;
            foreach (var troop in troops)
            {
                if (TroopDefinitions.TryGetValue(troop.Key.ToLower(), out var def))
                {
                    power += (long)troop.Value * (def.Attack + def.Defense + def.Life / 10);
                }
            }
            return power;
        }

        #endregion

        #region Private Methods - Combat Logic

        private ArmyStats CalculateArmyStats(
            Dictionary<string, int> troops,
            HeroStats? hero,
            List<string> techs)
        {
            var stats = new ArmyStats();

            foreach (var troop in troops)
            {
                if (TroopDefinitions.TryGetValue(troop.Key.ToLower(), out var def))
                {
                    stats.Attack += def.Attack * troop.Value;
                    stats.Defense += def.Defense * troop.Value;
                    stats.Life += def.Life * troop.Value;
                }
            }

            // Apply hero bonuses
            if (hero != null)
            {
                stats.Attack *= (1 + hero.AttackBonus);
                stats.Defense *= (1 + hero.DefenseBonus);
            }

            // Apply tech bonuses
            if (techs != null)
            {
                foreach (var tech in techs)
                {
                    if (TechBonuses.TryGetValue(tech, out var bonus))
                    {
                        stats.Attack *= (1 + bonus);
                        stats.Defense *= (1 + bonus);
                    }
                }
            }

            return stats;
        }

        private void ApplyTerrainModifiers(ref ArmyStats attacker, ref ArmyStats defender, string terrain)
        {
            switch (terrain?.ToLower())
            {
                case "plains":
                    // Cavalry bonus
                    attacker.Attack *= 1.1;
                    break;
                case "forest":
                    // Archer bonus, cavalry penalty
                    defender.Defense *= 1.1;
                    break;
                case "mountain":
                    // Defender bonus
                    defender.Defense *= 1.2;
                    break;
                case "swamp":
                    // Speed penalty (affects siege)
                    attacker.Attack *= 0.9;
                    break;
            }
        }

        private List<CombatRound> SimulateCombatRounds(
            Dictionary<string, int> attackerTroops,
            Dictionary<string, int> defenderTroops,
            ArmyStats attackerStats,
            ArmyStats defenderStats)
        {
            var rounds = new List<CombatRound>();
            var maxRounds = 10;

            for (int i = 0; i < maxRounds; i++)
            {
                var round = new CombatRound { RoundNumber = i + 1 };

                // Calculate damage
                var attackerDamage = attackerStats.Attack * (1 - defenderStats.Defense / (defenderStats.Defense + 1000));
                var defenderDamage = defenderStats.Attack * (1 - attackerStats.Defense / (attackerStats.Defense + 1000));

                // Apply damage to troops
                ApplyDamage(defenderTroops, attackerDamage, round.DefenderCasualties);
                ApplyDamage(attackerTroops, defenderDamage, round.AttackerCasualties);

                round.AttackerRemaining = attackerTroops.ToDictionary(kv => kv.Key, kv => kv.Value);
                round.DefenderRemaining = defenderTroops.ToDictionary(kv => kv.Key, kv => kv.Value);

                rounds.Add(round);

                // Check for battle end
                if (attackerTroops.Values.Sum() == 0 || defenderTroops.Values.Sum() == 0)
                    break;

                // Recalculate stats for next round
                attackerStats = RecalculateStats(attackerTroops, attackerStats);
                defenderStats = RecalculateStats(defenderTroops, defenderStats);
            }

            return rounds;
        }

        private void ApplyDamage(Dictionary<string, int> troops, double damage, Dictionary<string, int> casualties)
        {
            var totalLife = troops.Sum(t => 
                TroopDefinitions.TryGetValue(t.Key.ToLower(), out var def) ? def.Life * t.Value : 0);

            if (totalLife == 0) return;

            foreach (var troopType in troops.Keys.ToList())
            {
                if (!TroopDefinitions.TryGetValue(troopType.ToLower(), out var def))
                    continue;

                var troopLife = def.Life * troops[troopType];
                var damageShare = damage * (troopLife / totalLife);
                var killed = (int)Math.Min(troops[troopType], damageShare / def.Life);

                troops[troopType] -= killed;
                casualties[troopType] = killed;
            }
        }

        private ArmyStats RecalculateStats(Dictionary<string, int> troops, ArmyStats baseStats)
        {
            var newStats = new ArmyStats();
        
            foreach (var troop in troops)
            {
                if (TroopDefinitions.TryGetValue(troop.Key.ToLower(), out var def))
                {
                    newStats.Attack += def.Attack * troop.Value;
                    newStats.Defense += def.Defense * troop.Value;
                    newStats.Life += def.Life * troop.Value;
                }
            }

            return newStats;
        }

        private Dictionary<string, int> CalculateLosses(
            Dictionary<string, int> original,
            Dictionary<string, int> remaining)
        {
            var losses = new Dictionary<string, int>();
        
            foreach (var troop in original)
            {
                var survived = remaining.GetValueOrDefault(troop.Key);
                losses[troop.Key] = troop.Value - survived;
            }

            return losses;
        }

        private ResourceState CalculateLoot(
            Dictionary<string, int> survivors,
            ResourceState? defenderResources)
        {
            if (defenderResources == null)
                return new ResourceState();

            // Calculate total load capacity
            var loadCapacity = survivors.Sum(t =>
                TroopDefinitions.TryGetValue(t.Key.ToLower(), out var def) ? def.Load * t.Value : 0);

            // Distribute load across resources
            var totalResources = defenderResources.Gold + defenderResources.Food + 
                               defenderResources.Lumber + defenderResources.Stone + defenderResources.Iron;

            if (totalResources == 0) return new ResourceState();

            var lootRatio = Math.Min(1.0, (double)loadCapacity / totalResources);

            return new ResourceState
            {
                Gold = (long)(defenderResources.Gold * lootRatio),
                Food = (long)(defenderResources.Food * lootRatio),
                Lumber = (long)(defenderResources.Lumber * lootRatio),
                Stone = (long)(defenderResources.Stone * lootRatio),
                Iron = (long)(defenderResources.Iron * lootRatio)
            };
        }

        private BattleSetup ApplyVariance(BattleSetup setup, Random random)
        {
            var varied = new BattleSetup
            {
                AttackerName = setup.AttackerName,
                DefenderName = setup.DefenderName,
                AttackerTroops = setup.AttackerTroops.ToDictionary(
                    kv => kv.Key,
                    kv => (int)(kv.Value * (0.9 + random.NextDouble() * 0.2))),
                DefenderTroops = setup.DefenderTroops.ToDictionary(
                    kv => kv.Key,
                    kv => (int)(kv.Value * (0.9 + random.NextDouble() * 0.2))),
                AttackerHero = setup.AttackerHero,
                DefenderHero = setup.DefenderHero,
                AttackerTech = setup.AttackerTech,
                DefenderTech = setup.DefenderTech,
                WallLevel = setup.WallLevel,
                Terrain = setup.Terrain,
                DefenderResources = setup.DefenderResources
            };

            return varied;
        }

        private (double Lower, double Upper) CalculateConfidenceInterval(int successes, int total)
        {
            var p = (double)successes / total;
            var z = 1.96; // 95% confidence
            var margin = z * Math.Sqrt(p * (1 - p) / total);
            return (Math.Max(0, p - margin), Math.Min(1, p + margin));
        }

        private string GenerateBattleAnalysis(BattleSetup setup, BattleResult result)
        {
            var analysis = new System.Text.StringBuilder();

            analysis.AppendLine($"Battle Analysis: {setup.AttackerName} vs {setup.DefenderName}");
            analysis.AppendLine($"Result: {result.Winner.ToUpper()} WINS");
            analysis.AppendLine($"Win Probability: {result.WinProbability:P0}");
            analysis.AppendLine($"Combat Rounds: {result.TotalRounds}");
            analysis.AppendLine();
            analysis.AppendLine("Attacker Losses:");
            foreach (var loss in result.AttackerLosses.Where(l => l.Value > 0))
            {
                analysis.AppendLine($"  - {loss.Key}: {loss.Value:N0}");
            }
            analysis.AppendLine();
            analysis.AppendLine("Defender Losses:");
            foreach (var loss in result.DefenderLosses.Where(l => l.Value > 0))
            {
                analysis.AppendLine($"  - {loss.Key}: {loss.Value:N0}");
            }

            return analysis.ToString();
        }

        private List<string> GenerateRecommendations(BattleSetup setup, BattleResult result)
        {
            var recommendations = new List<string>();

            if (result.Winner == "defender")
            {
                recommendations.Add("Consider adding more troops before attacking.");
            
                // Check for counter compositions
                var defenderTypes = setup.DefenderTroops.Keys.ToList();
                if (defenderTypes.Contains("cavalry"))
                    recommendations.Add("Add pikemen to counter enemy cavalry.");
                if (defenderTypes.Contains("archer"))
                    recommendations.Add("Add cavalry to counter enemy archers.");
            }
            else
            {
                var lossRatio = result.AttackerLosses.Values.Sum() / 
                               (double)setup.AttackerTroops.Values.Sum();
            
                if (lossRatio > 0.3)
                    recommendations.Add("High losses detected. Consider a larger force for safer victory.");
                else if (lossRatio < 0.1)
                    recommendations.Add("Overwhelming victory! You could use fewer troops.");
            }

            return recommendations;
        }

    #endregion

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // No resources to dispose currently
        App.Logger?.Debug("CombatSimulator disposed");
    }

    #endregion
}

    #region Models

    public class TroopDefinition
    {
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public int Tier { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Life { get; set; }
        public int Speed { get; set; }
        public int Load { get; set; }
        public int FoodCost { get; set; }
        public int GoldCost { get; set; }
        public int TrainTime { get; set; }
        public string[] StrongAgainst { get; set; } = Array.Empty<string>();
        public string[] WeakAgainst { get; set; } = Array.Empty<string>();
    }

    public class ArmyStats
    {
        public double Attack { get; set; }
        public double Defense { get; set; }
        public double Life { get; set; }
    }

    public class HeroStats
    {
        public int HeroId { get; set; }
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public double AttackBonus { get; set; }
        public double DefenseBonus { get; set; }
    }

    public class BattleSetup
    {
        public string AttackerName { get; set; } = "Attacker";
        public string DefenderName { get; set; } = "Defender";
        public Dictionary<string, int> AttackerTroops { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DefenderTroops { get; set; } = new Dictionary<string, int>();
        public HeroStats? AttackerHero { get; set; }
        public HeroStats? DefenderHero { get; set; }
        public List<string> AttackerTech { get; set; }
        public List<string> DefenderTech { get; set; }
        public int WallLevel { get; set; }
        public string Terrain { get; set; }
        public ResourceState? DefenderResources { get; set; }
    }

    public class BattleResult
    {
        public DateTime Timestamp { get; set; }
        public string AttackerName { get; set; } = "";
        public string DefenderName { get; set; } = "";
        public string Winner { get; set; } = "";
        public double WinProbability { get; set; }
        public int TotalRounds { get; set; }
        public List<CombatRound> Rounds { get; set; } = new List<CombatRound>();
        public Dictionary<string, int> AttackerLosses { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DefenderLosses { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> AttackerSurvivors { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DefenderSurvivors { get; set; } = new Dictionary<string, int>();
        public ResourceState LootCaptured { get; set; } = new ResourceState();
        public string Analysis { get; set; } = "";
        public List<string> Recommendations { get; set; } = new List<string>();
        public string Error { get; set; }
    }

    public class CombatRound
    {
        public int RoundNumber { get; set; }
        public Dictionary<string, int> AttackerCasualties { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DefenderCasualties { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> AttackerRemaining { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DefenderRemaining { get; set; } = new Dictionary<string, int>();
    }

    public class BattleProbabilityAnalysis
    {
        public int Iterations { get; set; }
        public BattleSetup Setup { get; set; } = new BattleSetup();
        public double WinRate { get; set; }
        public double LossRate { get; set; }
        public Dictionary<string, int> AverageLosses { get; set; } = new Dictionary<string, int>();
        public (double Lower, double Upper) WinRateConfidence { get; set; }
    }

    public class OptimalComposition
    {
        public double TargetWinRate { get; set; }
        public double AchievedWinRate { get; set; }
        public Dictionary<string, int> Troops { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ExpectedLosses { get; set; } = new Dictionary<string, int>();
        public long TotalPower { get; set; }
    }

    #endregion

}