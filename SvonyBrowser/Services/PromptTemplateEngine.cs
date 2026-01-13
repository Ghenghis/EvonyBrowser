using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
    using SvonyBrowser.Helpers;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// AI prompt template engine with context injection, variable substitution,
    /// and specialized templates for different game scenarios.
    /// </summary>
    public sealed class PromptTemplateEngine : IDisposable
    {
        #region Singleton

        private static readonly Lazy<PromptTemplateEngine> _lazyInstance =
            new Lazy<PromptTemplateEngine>(() => new PromptTemplateEngine(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static PromptTemplateEngine Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly Dictionary<string, PromptTemplate> _templates = new Dictionary<string, PromptTemplate>();
        private readonly Dictionary<string, Func<GameStateEngine, string>> _dynamicVariables = new Dictionary<string, Func<GameStateEngine, string>>();
        private readonly string _templatesPath;

        #endregion

        #region Constructor

        private PromptTemplateEngine()
        {
            _templatesPath = Path.Combine(App.DataPath, "prompt-templates");
            Directory.CreateDirectory(_templatesPath);

            RegisterDynamicVariables();
            LoadBuiltInTemplates();
            LoadCustomTemplates();

            App.Logger.Information("PromptTemplateEngine initialized with {Count} templates", _templates.Count);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a template by name.
        /// </summary>
        public PromptTemplate? GetTemplate(string name)
        {
            return _templates.TryGetValue(name, out var template) ? template : null;
        }

        /// <summary>
        /// Gets all templates in a category.
        /// </summary>
        public List<PromptTemplate> GetTemplatesByCategory(string category)
        {
            return _templates.Values
                .Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Gets all available templates.
        /// </summary>
        public List<PromptTemplate> GetAllTemplates()
        {
            return _templates.Values.ToList();
        }

        /// <summary>
        /// Renders a template with the current game state.
        /// </summary>
        public string RenderTemplate(string templateName, Dictionary<string, string> additionalVariables = null)
        {
            if (!_templates.TryGetValue(templateName, out var template))
            {
                throw new ArgumentException($"Template not found: {templateName}");
            }

            return RenderTemplate(template, additionalVariables);
        }

        /// <summary>
        /// Renders a template with the current game state.
        /// </summary>
        public string RenderTemplate(PromptTemplate template, Dictionary<string, string> additionalVariables = null)
        {
            var gameState = GameStateEngine.Instance;
            var result = template.Content;

            // Replace dynamic variables
            foreach (var variable in _dynamicVariables)
            {
                var placeholder = $"{{{{{variable.Key}}}}}";
                if (result.Contains(placeholder))
                {
                    try
                    {
                        var value = variable.Value(gameState);
                        result = result.Replace(placeholder, value);
                    }
                    catch (Exception ex)
                    {
                        App.Logger.Warning(ex, "Failed to resolve variable: {Variable}", variable.Key);
                        result = result.Replace(placeholder, "[unavailable]");
                    }
                }
            }

            // Replace additional variables
            if (additionalVariables != null)
            {
                foreach (var variable in additionalVariables)
                {
                    result = result.Replace($"{{{{{variable.Key}}}}}", variable.Value);
                }
            }

            // Replace conditional blocks
            result = ProcessConditionals(result, gameState, additionalVariables);

            // Replace loops
            result = ProcessLoops(result, gameState);

            return result.Trim();
        }

        /// <summary>
        /// Creates a custom template.
        /// </summary>
        public void CreateTemplate(PromptTemplate template)
        {
            template.IsCustom = true;
            _templates[template.Name] = template;
            SaveCustomTemplate(template);
        }

        /// <summary>
        /// Deletes a custom template.
        /// </summary>
        public void DeleteTemplate(string name)
        {
            if (_templates.TryGetValue(name, out var template) && template.IsCustom)
            {
                _templates.Remove(name);
                var filePath = Path.Combine(_templatesPath, $"{name}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        /// <summary>
        /// Generates a context-aware prompt for a specific intent.
        /// </summary>
        public string GenerateContextualPrompt(string intent, string userMessage)
        {
            var gameState = GameStateEngine.Instance;
            var sb = new StringBuilder();

            // System context
            sb.AppendLine("You are an AI assistant specialized in Evony: The King's Return.");
            sb.AppendLine("You have access to the player's current game state and can provide strategic advice.");
            sb.AppendLine();

            // Current game state summary
            sb.AppendLine("## Current Game State");
            sb.AppendLine($"- Player: {gameState.Player.Name}");
            sb.AppendLine($"- Power: {gameState.Player.Power:N0}");
            sb.AppendLine($"- Cities: {gameState.Cities.Count}");
            sb.AppendLine($"- Alliance: {gameState.Player.AllianceName ?? "None"}");
            sb.AppendLine();

            // Intent-specific context
            switch (intent.ToLower())
            {
                case "combat":
                case "attack":
                case "battle":
                    sb.AppendLine("## Combat Context");
                    sb.AppendLine($"- Total Troops: {gameState.GetTotalTroops().Values.Sum():N0}");
                    sb.AppendLine($"- Active Marches: {gameState.Marches.Count(m => m.Value.Status == "marching")}");
                    sb.AppendLine();
                    break;

                case "build":
                case "upgrade":
                case "construction":
                    sb.AppendLine("## Building Context");
                    var mainCity = gameState.Cities.Values.FirstOrDefault();
                    if (mainCity != null)
                    {
                        sb.AppendLine($"- Keep Level: {mainCity.KeepLevel}");
                        sb.AppendLine($"- Active Constructions: {mainCity.Buildings.Count(b => b.Value.IsUpgrading)}");
                    }
                    sb.AppendLine();
                    break;

                case "resource":
                case "economy":
                    sb.AppendLine("## Resource Context");
                    var resources = gameState.GetTotalResources();
                    sb.AppendLine($"- Gold: {resources.Gold:N0}");
                    sb.AppendLine($"- Food: {resources.Food:N0}");
                    sb.AppendLine($"- Lumber: {resources.Lumber:N0}");
                    sb.AppendLine($"- Stone: {resources.Stone:N0}");
                    sb.AppendLine($"- Iron: {resources.Iron:N0}");
                    sb.AppendLine();
                    break;

                case "hero":
                    sb.AppendLine("## Hero Context");
                    foreach (var hero in gameState.Heroes.Values.Take(5))
                    {
                        sb.AppendLine($"- {hero.Name}: Level {hero.Level}, {hero.Quality}");
                    }
                    sb.AppendLine();
                    break;
            }

            // User message
            sb.AppendLine("## User Request");
            sb.AppendLine(userMessage);
            sb.AppendLine();

            sb.AppendLine("Please provide helpful, strategic advice based on the current game state.");

            return sb.ToString();
        }

        #endregion

        #region Private Methods

        private void RegisterDynamicVariables()
        {
            // Player variables
            _dynamicVariables["player.name"] = gs => gs.Player.Name;
            _dynamicVariables["player.power"] = gs => gs.Player.Power.ToString("N0");
            _dynamicVariables["player.level"] = gs => gs.Player.Level.ToString();
            _dynamicVariables["player.alliance"] = gs => gs.Player.AllianceName ?? "None";
            _dynamicVariables["player.vip"] = gs => gs.Player.VipLevel.ToString();

            // Resource variables
            _dynamicVariables["resources.gold"] = gs => gs.GetTotalResources().Gold.ToString("N0");
            _dynamicVariables["resources.food"] = gs => gs.GetTotalResources().Food.ToString("N0");
            _dynamicVariables["resources.lumber"] = gs => gs.GetTotalResources().Lumber.ToString("N0");
            _dynamicVariables["resources.stone"] = gs => gs.GetTotalResources().Stone.ToString("N0");
            _dynamicVariables["resources.iron"] = gs => gs.GetTotalResources().Iron.ToString("N0");
            _dynamicVariables["resources.gems"] = gs => gs.Player.Gems.ToString("N0");

            // Troop variables
            _dynamicVariables["troops.total"] = gs => gs.GetTotalTroops().Values.Sum().ToString("N0");
            _dynamicVariables["troops.cavalry"] = gs => gs.GetTotalTroops().GetValueOrDefault("cavalry").ToString("N0");
            _dynamicVariables["troops.archers"] = gs => gs.GetTotalTroops().GetValueOrDefault("archer").ToString("N0");
            _dynamicVariables["troops.infantry"] = gs => (gs.GetTotalTroops().GetValueOrDefault("warrior") + 
                                                           gs.GetTotalTroops().GetValueOrDefault("pikeman") +
                                                           gs.GetTotalTroops().GetValueOrDefault("swordsman")).ToString("N0");

            // City variables
            _dynamicVariables["cities.count"] = gs => gs.Cities.Count.ToString();
            _dynamicVariables["cities.main.level"] = gs => gs.Cities.Values.FirstOrDefault()?.KeepLevel.ToString() ?? "0";

            // Hero variables
            _dynamicVariables["heroes.count"] = gs => gs.Heroes.Count.ToString();
            _dynamicVariables["heroes.top"] = gs => string.Join(", ", gs.Heroes.Values
                .OrderByDescending(h => h.Power)
                .Take(3)
                .Select(h => $"{h.Name} (L{h.Level})"));

            // March variables
            _dynamicVariables["marches.active"] = gs => gs.Marches.Count(m => m.Value.Status == "marching").ToString();
            _dynamicVariables["marches.available"] = gs => (5 - gs.Marches.Count(m => m.Value.Status == "marching")).ToString();

            // Time variables
            _dynamicVariables["time.now"] = _ => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            _dynamicVariables["time.server"] = _ => DateTime.UtcNow.ToString("HH:mm");
        }

        private void LoadBuiltInTemplates()
        {
            // Combat Analysis Template
            _templates["combat_analysis"] = new PromptTemplate
            {
                Name = "combat_analysis",
                Category = "Combat",
                Description = "Analyze a potential attack target",
                Content = @"Analyze this potential attack target:

    Target Coordinates: {{target.x}}, {{target.y}}
    Target Power: {{target.power}}
    Target Alliance: {{target.alliance}}

    My Current State:
    - Power: {{player.power}}
    - Available Troops: {{troops.total}}
    - Available Marches: {{marches.available}}

    Please analyze:
    1. Win probability
    2. Recommended troop composition
    3. Expected losses
    4. Optimal attack timing
    5. Risk assessment"
            };

            // Resource Optimization Template
            _templates["resource_optimization"] = new PromptTemplate
            {
                Name = "resource_optimization",
                Category = "Economy",
                Description = "Optimize resource production and usage",
                Content = @"Analyze my resource situation and provide optimization recommendations:

    Current Resources:
    - Gold: {{resources.gold}}
    - Food: {{resources.food}}
    - Lumber: {{resources.lumber}}
    - Stone: {{resources.stone}}
    - Iron: {{resources.iron}}
    - Gems: {{resources.gems}}

    Player Info:
    - Power: {{player.power}}
    - Cities: {{cities.count}}
    - Main City Level: {{cities.main.level}}

    Please recommend:
    1. Resource production priorities
    2. Spending priorities
    3. Resource gathering targets
    4. Efficiency improvements"
            };

            // Hero Management Template
            _templates["hero_management"] = new PromptTemplate
            {
                Name = "hero_management",
                Category = "Heroes",
                Description = "Get hero development recommendations",
                Content = @"Analyze my hero roster and provide development recommendations:

    Top Heroes: {{heroes.top}}
    Total Heroes: {{heroes.count}}

    Current Focus:
    - Power: {{player.power}}
    - Troops: {{troops.total}}

    Please recommend:
    1. Which heroes to prioritize for development
    2. Skill book allocation strategy
    3. Equipment recommendations
    4. Hero pairing for different scenarios"
            };

            // Daily Checklist Template
            _templates["daily_checklist"] = new PromptTemplate
            {
                Name = "daily_checklist",
                Category = "General",
                Description = "Generate a daily task checklist",
                Content = @"Generate a prioritized daily checklist based on my current state:

    Player: {{player.name}}
    Power: {{player.power}}
    VIP Level: {{player.vip}}
    Alliance: {{player.alliance}}

    Resources:
    - Gold: {{resources.gold}}
    - Food: {{resources.food}}

    Active Marches: {{marches.active}}
    Available Marches: {{marches.available}}

    Please create a prioritized checklist of:
    1. Essential daily tasks
    2. Resource collection priorities
    3. Training recommendations
    4. Event participation
    5. Alliance activities"
            };

            // Strategic Planning Template
            _templates["strategic_planning"] = new PromptTemplate
            {
                Name = "strategic_planning",
                Category = "Strategy",
                Description = "Long-term strategic planning",
                Content = @"Help me create a strategic development plan:

    Current State:
    - Player: {{player.name}}
    - Power: {{player.power}}
    - Level: {{player.level}}
    - Cities: {{cities.count}}
    - Main City Level: {{cities.main.level}}

    Resources:
    - Gold: {{resources.gold}}
    - Food: {{resources.food}}
    - Gems: {{resources.gems}}

    Military:
    - Total Troops: {{troops.total}}
    - Heroes: {{heroes.count}}

    Please provide:
    1. Short-term goals (1 week)
    2. Medium-term goals (1 month)
    3. Long-term goals (3 months)
    4. Priority focus areas
    5. Resource allocation strategy"
            };

            // Alliance War Template
            _templates["alliance_war"] = new PromptTemplate
            {
                Name = "alliance_war",
                Category = "Combat",
                Description = "Alliance war strategy and coordination",
                Content = @"Help coordinate alliance war strategy:

    My Status:
    - Player: {{player.name}}
    - Power: {{player.power}}
    - Alliance: {{player.alliance}}
    - Troops: {{troops.total}}
    - Available Marches: {{marches.available}}

    Please provide:
    1. Role recommendation (rally leader, filler, reinforcer)
    2. Troop composition for rallies
    3. Timing coordination tips
    4. Defense strategy
    5. Communication templates"
            };

            // Monster Hunt Template
            _templates["monster_hunt"] = new PromptTemplate
            {
                Name = "monster_hunt",
                Category = "PvE",
                Description = "Monster hunting optimization",
                Content = @"Optimize my monster hunting strategy:

    Current State:
    - Power: {{player.power}}
    - Troops: {{troops.total}}
    - Top Heroes: {{heroes.top}}
    - Available Marches: {{marches.available}}

    Please recommend:
    1. Optimal monster levels to target
    2. Hero selection for different monsters
    3. Troop composition
    4. Stamina management
    5. Reward optimization"
            };

            // Troubleshooting Template
            _templates["troubleshooting"] = new PromptTemplate
            {
                Name = "troubleshooting",
                Category = "Support",
                Description = "Troubleshoot game issues",
                Content = @"Help troubleshoot this issue:

    Issue Description: {{issue.description}}

    Current State:
    - Player: {{player.name}}
    - Power: {{player.power}}
    - Time: {{time.now}}

    Please help:
    1. Identify possible causes
    2. Suggest solutions
    3. Provide workarounds
    4. Recommend preventive measures"
            };
        }

        private void LoadCustomTemplates()
        {
            if (!Directory.Exists(_templatesPath))
                return;

            foreach (var file in Directory.GetFiles(_templatesPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var template = JsonConvert.DeserializeObject<PromptTemplate>(json);
                    if (template != null)
                    {
                        template.IsCustom = true;
                        _templates[template.Name] = template;
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.Warning(ex, "Failed to load custom template: {File}", file);
                }
            }
        }

        private void SaveCustomTemplate(PromptTemplate template)
        {
            var filePath = Path.Combine(_templatesPath, $"{template.Name}.json");
            var json = JsonConvert.SerializeObject(template, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        private string ProcessConditionals(string content, GameStateEngine gameState, Dictionary<string, string> additionalVariables)
        {
            // Process {{#if condition}}...{{/if}} blocks
            var ifPattern = @"\{\{#if\s+(\w+\.?\w*)\s*(==|!=|>|<|>=|<=)?\s*([^\}]*)\}\}(.*?)\{\{/if\}\}";
        
            return Regex.Replace(content, ifPattern, match =>
            {
                var variable = match.Groups[1].Value;
                var op = match.Groups[2].Value;
                var compareValue = match.Groups[3].Value.Trim();
                var innerContent = match.Groups[4].Value;

                string value = null;
                if (_dynamicVariables.TryGetValue(variable, out var resolver))
                {
                    try { value = resolver(gameState); } catch { }
                }
                else if (additionalVariables?.TryGetValue(variable, out var addValue) == true)
                {
                    value = addValue;
                }

                if (value == null) return "";

                bool condition = string.IsNullOrEmpty(op) 
                    ? !string.IsNullOrEmpty(value) && value != "0" && value.ToLower() != "false"
                    : EvaluateCondition(value, op, compareValue);

                return condition ? innerContent : "";
            }, RegexOptions.Singleline);
        }

        private bool EvaluateCondition(string left, string op, string right)
        {
            // Try numeric comparison
            if (double.TryParse(left.Replace(",", ""), out var leftNum) && 
                double.TryParse(right.Replace(",", ""), out var rightNum))
            {
                return op switch
                {
                    "==" => Math.Abs(leftNum - rightNum) < 0.001,
                    "!=" => Math.Abs(leftNum - rightNum) >= 0.001,
                    ">" => leftNum > rightNum,
                    "<" => leftNum < rightNum,
                    ">=" => leftNum >= rightNum,
                    "<=" => leftNum <= rightNum,
                    _ => false
                };
            }

            // String comparison
            return op switch
            {
                "==" => left.Equals(right, StringComparison.OrdinalIgnoreCase),
                "!=" => !left.Equals(right, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        private string ProcessLoops(string content, GameStateEngine gameState)
        {
            // Process {{#each collection}}...{{/each}} blocks
            var eachPattern = @"\{\{#each\s+(\w+)\}\}(.*?)\{\{/each\}\}";
        
            return Regex.Replace(content, eachPattern, match =>
            {
                var collection = match.Groups[1].Value;
                var template = match.Groups[2].Value;
                var sb = new StringBuilder();

                IEnumerable<object> items = collection switch
                {
                    "cities" => gameState.Cities.Values.Cast<object>(),
                    "heroes" => gameState.Heroes.Values.Cast<object>(),
                    "marches" => gameState.Marches.Values.Cast<object>(),
                    _ => null
                };

                if (items == null) return "";

                foreach (var item in items)
                {
                    var itemContent = template;
                    var props = item.GetType().GetProperties();
                
                    foreach (var prop in props)
                    {
                        var placeholder = $"{{{{this.{prop.Name.ToLower()}}}}}";
                        var value = prop.GetValue(item)?.ToString() ?? "";
                        itemContent = itemContent.Replace(placeholder, value);
                    }

                    sb.Append(itemContent);
                }

                return sb.ToString();
            }, RegexOptions.Singleline);
        }

        #endregion
    }

    #region Models

    public class PromptTemplate
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "General";
        public string Description { get; set; } = "";
        public string Content { get; set; } = "";
        public bool IsCustom { get; set; }
        public List<string> RequiredVariables { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    }

    #endregion
}
