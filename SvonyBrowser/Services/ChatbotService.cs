using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Provides chatbot functionality with RAG-powered knowledge retrieval
    /// and real-time traffic analysis integration.
    /// </summary>
    public sealed class ChatbotService : IDisposable
    {
        #region Singleton

        private static readonly Lazy<ChatbotService> _lazyInstance =
            new Lazy<ChatbotService>(() => new ChatbotService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static ChatbotService Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly McpConnectionManager _mcpManager;
        private readonly List<ChatMessage> _conversationHistory = new List<ChatMessage>();
        private readonly object _historyLock = new object();
        private bool _disposed = false;
        private ChatContext _currentContext = new ChatContext();

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the chatbot is ready (MCP servers connected).
        /// </summary>
        public bool IsReady => _mcpManager.HasConnections;

        /// <summary>
        /// Gets the current conversation history.
        /// </summary>
        public IReadOnlyList<ChatMessage> ConversationHistory
        {
            get
            {
                lock (_historyLock)
                {
                    return _conversationHistory.ToArray();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current context.
        /// </summary>
        public ChatContext CurrentContext
        {
            get => _currentContext;
            set => _currentContext = value ?? new ChatContext();
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired when a new message is added to the conversation.
        /// </summary>
        public event Action<ChatMessage> MessageAdded;

        /// <summary>
        /// Fired when the chatbot is processing.
        /// </summary>
        public event Action<bool> ProcessingStateChanged;

        /// <summary>
        /// Fired when an error occurs.
        /// </summary>
        public event Action<string> ErrorOccurred;

        #endregion

        #region Constructor

        private ChatbotService()
        {
            _mcpManager = McpConnectionManager.Instance;
            App.Logger.Information("ChatbotService initialized");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sends a message to the chatbot and gets a response.
        /// </summary>
        public async Task<ChatMessage?> SendMessageAsync(string userMessage, CancellationToken cancellationToken = default)
        {
            if (_disposed) return null;
            if (string.IsNullOrWhiteSpace(userMessage)) return null;

            App.Logger.Information("Processing user message: {Message}", userMessage.Substring(0, Math.Min(50, userMessage.Length)));

            try
            {
                ProcessingStateChanged?.Invoke(true);

                // Add user message to history
                var userMsg = new ChatMessage
                {
                    Role = ChatRole.User,
                    Content = userMessage,
                    Timestamp = DateTime.UtcNow
                };
                AddMessage(userMsg);

                // Determine intent and route to appropriate handler
                var intent = ClassifyIntent(userMessage);
                ChatMessage response;

                switch (intent)
                {
                    case ChatIntent.ProtocolQuery:
                        response = await HandleProtocolQueryAsync(userMessage, cancellationToken);
                        break;

                    case ChatIntent.KnowledgeQuery:
                        response = await HandleKnowledgeQueryAsync(userMessage, cancellationToken);
                        break;

                    case ChatIntent.TrafficAnalysis:
                        response = await HandleTrafficAnalysisAsync(userMessage, cancellationToken);
                        break;

                    case ChatIntent.Calculation:
                        response = await HandleCalculationAsync(userMessage, cancellationToken);
                        break;

                    case ChatIntent.Automation:
                        response = await HandleAutomationAsync(userMessage, cancellationToken);
                        break;

                    default:
                        response = await HandleGeneralQueryAsync(userMessage, cancellationToken);
                        break;
                }

                AddMessage(response);
                return response;
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error processing message");
                ErrorOccurred?.Invoke(ex.Message);

                var errorMsg = new ChatMessage
                {
                    Role = ChatRole.Assistant,
                    Content = $"I encountered an error: {ex.Message}. Please try again.",
                    Timestamp = DateTime.UtcNow,
                    IsError = true
                };
                AddMessage(errorMsg);
                return errorMsg;
            }
            finally
            {
                ProcessingStateChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Searches the knowledge base.
        /// </summary>
        public async Task<JObject> SearchKnowledgeAsync(string query, int limit = 10, string category = "all", CancellationToken cancellationToken = default)
        {
            return await _mcpManager.CallToolAsync("evony-rag", "evony_search", new JObject
            {
                ["query"] = query,
                ["limit"] = limit,
                ["category"] = category
            }, cancellationToken);
        }

        /// <summary>
        /// Decodes AMF3 data.
        /// </summary>
        public async Task<JObject> DecodeAmfAsync(string hexData, CancellationToken cancellationToken = default)
        {
            return await _mcpManager.CallToolAsync("evony-rte", "amf_decode", new JObject
            {
                ["data"] = hexData,
                ["format"] = "hex"
            }, cancellationToken);
        }

        /// <summary>
        /// Looks up protocol action.
        /// </summary>
        public async Task<JObject> LookupProtocolAsync(string actionName, CancellationToken cancellationToken = default)
        {
            return await _mcpManager.CallToolAsync("evony-rte", "protocol_lookup", new JObject
            {
                ["action"] = actionName
            }, cancellationToken);
        }

        /// <summary>
        /// Calculates troop training.
        /// </summary>
        public async Task<JObject> CalculateTrainingAsync(string troopType, int amount, int barracksLevel = 10, CancellationToken cancellationToken = default)
        {
            return await _mcpManager.CallToolAsync("evony-tools", "calc_training", new JObject
            {
                ["troop_type"] = troopType,
                ["amount"] = amount,
                ["barracks_level"] = barracksLevel
            }, cancellationToken);
        }

        /// <summary>
        /// Clears conversation history.
        /// </summary>
        public void ClearHistory()
        {
            lock (_historyLock)
            {
                _conversationHistory.Clear();
            }
            _currentContext = new ChatContext();
            App.Logger.Information("Conversation history cleared");
        }

        /// <summary>
        /// Updates the current game context.
        /// </summary>
        public void UpdateContext(ChatContext context)
        {
            _currentContext = context ?? new ChatContext();
        }

        #endregion

        #region Private Methods - Intent Classification

        private ChatIntent ClassifyIntent(string message)
        {
            var lower = message.ToLowerInvariant();

            // Protocol-related keywords
            if (ContainsAny(lower, "protocol", "amf", "packet", "decode", "encode", "action", "command", "api"))
                return ChatIntent.ProtocolQuery;

            // Traffic analysis keywords
            if (ContainsAny(lower, "traffic", "capture", "intercept", "request", "response", "hex"))
                return ChatIntent.TrafficAnalysis;

            // Calculation keywords
            if (ContainsAny(lower, "calculate", "how many", "how long", "cost", "time", "march", "train"))
                return ChatIntent.Calculation;

            // Automation keywords
            if (ContainsAny(lower, "automate", "script", "build order", "attack plan", "strategy"))
                return ChatIntent.Automation;

            // Knowledge query (default for game-related questions)
            if (ContainsAny(lower, "hero", "troop", "building", "tech", "research", "alliance", "what is", "how to", "explain"))
                return ChatIntent.KnowledgeQuery;

            return ChatIntent.General;
        }

        private bool ContainsAny(string text, params string[] keywords)
        {
            foreach (var keyword in keywords)
            {
                if (text.Contains(keyword))
                    return true;
            }
            return false;
        }

        #endregion

        #region Private Methods - Intent Handlers

        private async Task<ChatMessage> HandleProtocolQueryAsync(string message, CancellationToken cancellationToken)
        {
            var response = new ChatMessage
            {
                Role = ChatRole.Assistant,
                Timestamp = DateTime.UtcNow,
                Sources = new List<string>()
            };

            try
            {
                // Extract action name if present
                var actionMatch = System.Text.RegularExpressions.Regex.Match(message, @"(\w+\.\w+)");
                if (actionMatch.Success)
                {
                    var result = await LookupProtocolAsync(actionMatch.Value, cancellationToken);
                    if (result != null)
                    {
                        var found = result["result"]?["found"]?.Value<bool>() ?? false;
                        if (found)
                        {
                            var doc = result["result"]?["documentation"]?.ToString() ?? "";
                            response.Content = $"**Protocol Action: {actionMatch.Value}**\n\n{doc}";
                            response.Sources.Add("evony-rte");
                        }
                        else
                        {
                            var similar = result["result"]?["similar_actions"]?.ToObject<string[]>() ?? Array.Empty<string>();
                            response.Content = $"Action `{actionMatch.Value}` not found. Similar actions:\n" +
                                string.Join("\n", similar.Select(s => $"- `{s}`"));
                        }
                        return response;
                    }
                }

                // General protocol search
                var searchResult = await _mcpManager.CallToolAsync("evony-rte", "protocol_search", new JObject
                {
                    ["query"] = message,
                    ["limit"] = 10
                }, cancellationToken);

                if (searchResult != null)
                {
                    var results = searchResult["result"]?["results"]?.ToObject<List<JObject>>() ?? new List<JObject>();
                    if (results.Count > 0)
                    {
                        response.Content = "**Found Protocol Actions:**\n\n" +
                            string.Join("\n", results.Select(r =>
                                $"- `{r["name"]}` ({r["category"]}): {r["description"]}"));
                        response.Sources.Add("evony-rte");
                    }
                    else
                    {
                        response.Content = "No matching protocol actions found. Try searching for specific action names like `hero.hireHero` or `castle.upgradeBuilding`.";
                    }
                }
                else
                {
                    response.Content = "Unable to search protocol database. MCP server may not be connected.";
                    response.IsError = true;
                }
            }
            catch (Exception ex)
            {
                response.Content = $"Error querying protocol: {ex.Message}";
                response.IsError = true;
            }

            return response;
        }

        private async Task<ChatMessage> HandleKnowledgeQueryAsync(string message, CancellationToken cancellationToken)
        {
            var response = new ChatMessage
            {
                Role = ChatRole.Assistant,
                Timestamp = DateTime.UtcNow,
                Sources = new List<string>()
            };

            try
            {
                // Query RAG knowledge base
                var result = await _mcpManager.CallToolAsync("evony-rag", "evony_query", new JObject
                {
                    ["question"] = message,
                    ["include_sources"] = true
                }, cancellationToken);

                if (result != null)
                {
                    var answer = result["result"]?["answer"]?.ToString() ?? "No answer found.";
                    var sources = result["result"]?["sources"]?.ToObject<List<JObject>>() ?? new List<JObject>();

                    response.Content = answer;

                    if (sources.Count > 0)
                    {
                        response.Content += "\n\n**Sources:**\n" +
                            string.Join("\n", sources.Take(3).Select(s =>
                                $"- {s["category"]}: {s["filename"]}"));
                        response.Sources.AddRange(sources.Select(s => s["filename"]?.ToString() ?? "unknown"));
                    }
                }
                else
                {
                    response.Content = "Unable to query knowledge base. RAG server may not be connected.";
                    response.IsError = true;
                }
            }
            catch (Exception ex)
            {
                response.Content = $"Error querying knowledge base: {ex.Message}";
                response.IsError = true;
            }

            return response;
        }

        private async Task<ChatMessage> HandleTrafficAnalysisAsync(string message, CancellationToken cancellationToken)
        {
            var response = new ChatMessage
            {
                Role = ChatRole.Assistant,
                Timestamp = DateTime.UtcNow,
                Sources = new List<string> { "evony-rte" }
            };

            try
            {
                // Check for hex data to decode
                var hexMatch = System.Text.RegularExpressions.Regex.Match(message, @"[0-9a-fA-F]{20,}");
                if (hexMatch.Success)
                {
                    var decoded = await DecodeAmfAsync(hexMatch.Value, cancellationToken);
                    if (decoded != null)
                    {
                        var decodedData = decoded["result"]?["decoded"];
                        response.Content = $"**Decoded AMF3 Data:**\n```json\n{decodedData?.ToString(Newtonsoft.Json.Formatting.Indented)}\n```";
                        return response;
                    }
                }

                // List recent traffic
                var trafficResult = await _mcpManager.CallToolAsync("evony-rte", "traffic_list", new JObject
                {
                    ["limit"] = 20,
                    ["direction"] = "all",
                    ["decoded_only"] = false
                }, cancellationToken);

                if (trafficResult != null)
                {
                    var entries = trafficResult["result"]?["entries"]?.ToObject<List<JObject>>() ?? new List<JObject>();
                    if (entries.Count > 0)
                    {
                        response.Content = $"**Recent Traffic ({entries.Count} entries):**\n\n" +
                            string.Join("\n", entries.Take(10).Select(e =>
                                $"- [{e["direction"]}] {e["timestamp"]} - {e["data_length"]} bytes ({e["decoded"]})"));
                    }
                    else
                    {
                        response.Content = "No traffic captured yet. Make sure Fiddler is running and capturing traffic.";
                    }
                }
                else
                {
                    response.Content = "Unable to retrieve traffic data. RTE server may not be connected.";
                    response.IsError = true;
                }
            }
            catch (Exception ex)
            {
                response.Content = $"Error analyzing traffic: {ex.Message}";
                response.IsError = true;
            }

            return response;
        }

        private async Task<ChatMessage> HandleCalculationAsync(string message, CancellationToken cancellationToken)
        {
            var response = new ChatMessage
            {
                Role = ChatRole.Assistant,
                Timestamp = DateTime.UtcNow,
                Sources = new List<string> { "evony-tools" }
            };

            try
            {
                var lower = message.ToLowerInvariant();

                // Training calculation
                if (lower.Contains("train") || lower.Contains("troop"))
                {
                    // Extract troop type and amount
                    var troopMatch = System.Text.RegularExpressions.Regex.Match(lower, @"(\d+)\s*(archer|cavalry|warrior|pikeman|swordsman|scout|cataphract|ballista|ram|catapult|transporter|worker)s?");
                    if (troopMatch.Success)
                    {
                        var amount = int.Parse(troopMatch.Groups[1].Value);
                        var troopName = troopMatch.Groups[2].Value;
                        var troopCode = GetTroopCode(troopName);

                        var result = await CalculateTrainingAsync(troopCode, amount, 10, cancellationToken);
                        if (result != null)
                        {
                            var costs = result["result"]?["costs"];
                            var time = result["result"]?["training_time"]?["formatted"]?.ToString();
                            var stats = result["result"]?["stats"];

                            response.Content = $"**Training {amount} {troopName}s:**\n\n" +
                                $"**Costs:**\n" +
                                $"- Food: {costs?["food"]}\n" +
                                $"- Lumber: {costs?["lumber"]}\n" +
                                $"- Stone: {costs?["stone"]}\n" +
                                $"- Iron: {costs?["iron"]}\n" +
                                $"- Gold: {costs?["gold"]}\n\n" +
                                $"**Time:** {time}\n\n" +
                                $"**Stats:**\n" +
                                $"- Total Attack: {stats?["total_attack"]}\n" +
                                $"- Total Defense: {stats?["total_defense"]}\n" +
                                $"- Total Load: {stats?["total_load"]}\n" +
                                $"- Food Consumption: {stats?["food_consumption"]}";
                            return response;
                        }
                    }
                }

                // March calculation
                if (lower.Contains("march") || lower.Contains("distance"))
                {
                    var coordMatch = System.Text.RegularExpressions.Regex.Match(message, @"(\d+)[,\s]+(\d+)\s*(?:to|->)\s*(\d+)[,\s]+(\d+)");
                    if (coordMatch.Success)
                    {
                        var fromX = int.Parse(coordMatch.Groups[1].Value);
                        var fromY = int.Parse(coordMatch.Groups[2].Value);
                        var toX = int.Parse(coordMatch.Groups[3].Value);
                        var toY = int.Parse(coordMatch.Groups[4].Value);

                        var result = await _mcpManager.CallToolAsync("evony-tools", "calc_march", new JObject
                        {
                            ["from_x"] = fromX,
                            ["from_y"] = fromY,
                            ["to_x"] = toX,
                            ["to_y"] = toY,
                            ["slowest_troop"] = "a",
                            ["logistics_level"] = 10
                        }, cancellationToken);

                        if (result != null)
                        {
                            var route = result["result"]?["route"];
                            var marchTime = result["result"]?["march_time"]?["formatted"]?.ToString();
                            var distance = route?["distance"]?.ToString();

                            response.Content = $"**March Calculation:**\n\n" +
                                $"- From: ({fromX}, {fromY})\n" +
                                $"- To: ({toX}, {toY})\n" +
                                $"- Distance: {distance} tiles\n" +
                                $"- March Time: {marchTime}\n\n" +
                                $"*(Based on archer speed with Logistics 10)*";
                            return response;
                        }
                    }
                }

                response.Content = "I can help with calculations! Try asking:\n" +
                    "- \"How long to train 10000 archers?\"\n" +
                    "- \"Calculate march time from 100,200 to 300,400\"\n" +
                    "- \"What's the cost of training 5000 cavalry?\"";
            }
            catch (Exception ex)
            {
                response.Content = $"Error performing calculation: {ex.Message}";
                response.IsError = true;
            }

            return response;
        }

        private async Task<ChatMessage> HandleAutomationAsync(string message, CancellationToken cancellationToken)
        {
            var response = new ChatMessage
            {
                Role = ChatRole.Assistant,
                Timestamp = DateTime.UtcNow,
                Sources = new List<string> { "evony-tools" }
            };

            try
            {
                var lower = message.ToLowerInvariant();

                // Build order generation
                if (lower.Contains("build order") || lower.Contains("building order"))
                {
                    var focus = "balanced";
                    if (lower.Contains("military")) focus = "military";
                    else if (lower.Contains("economy")) focus = "economy";
                    else if (lower.Contains("research")) focus = "research";

                    var result = await _mcpManager.CallToolAsync("evony-tools", "generate_build_order", new JObject
                    {
                        ["focus"] = focus,
                        ["target_level"] = 5
                    }, cancellationToken);

                    if (result != null)
                    {
                        var order = result["result"]?["build_order"]?.ToObject<List<JObject>>() ?? new List<JObject>();
                        var tips = result["result"]?["tips"]?.ToString();

                        response.Content = $"**{focus.ToUpper()} Build Order:**\n\n" +
                            string.Join("\n", order.Select(o =>
                                $"{o["step"]}. {o["building"]} → Level {o["level"]}")) +
                            $"\n\n**Tips:** {tips}";
                        return response;
                    }
                }

                // Attack plan generation
                if (lower.Contains("attack plan") || lower.Contains("attack strategy"))
                {
                    response.Content = "To generate an attack plan, I need:\n" +
                        "- Target type (NPC, valley, player, barbarian)\n" +
                        "- Target level\n" +
                        "- Your available troops\n\n" +
                        "Example: \"Generate attack plan for level 10 NPC with 50000 archers and 10000 cavalry\"";
                    return response;
                }

                response.Content = "I can help with automation! Try:\n" +
                    "- \"Generate military build order\"\n" +
                    "- \"Create economy build order\"\n" +
                    "- \"Generate attack plan for level 5 NPC\"";
            }
            catch (Exception ex)
            {
                response.Content = $"Error generating automation: {ex.Message}";
                response.IsError = true;
            }

            return response;
        }

        private async Task<ChatMessage> HandleGeneralQueryAsync(string message, CancellationToken cancellationToken)
        {
            var response = new ChatMessage
            {
                Role = ChatRole.Assistant,
                Timestamp = DateTime.UtcNow,
                Sources = new List<string>()
            };

            // Try knowledge base first
            var kbResponse = await HandleKnowledgeQueryAsync(message, cancellationToken);
            if (!kbResponse.IsError && !string.IsNullOrEmpty(kbResponse.Content))
            {
                return kbResponse;
            }

            // Default response with suggestions
            response.Content = "I'm your Evony co-pilot! I can help with:\n\n" +
                "**🔍 Knowledge Queries**\n" +
                "- \"What are the best heroes for attack?\"\n" +
                "- \"Explain building requirements\"\n\n" +
                "**📡 Protocol Analysis**\n" +
                "- \"Look up hero.hireHero action\"\n" +
                "- \"Search protocol for castle commands\"\n\n" +
                "**📊 Traffic Analysis**\n" +
                "- \"Show recent traffic\"\n" +
                "- \"Decode [hex data]\"\n\n" +
                "**🧮 Calculations**\n" +
                "- \"Calculate training 10000 archers\"\n" +
                "- \"March time from 100,200 to 300,400\"\n\n" +
                "**🤖 Automation**\n" +
                "- \"Generate military build order\"\n" +
                "- \"Create attack plan\"";

            return response;
        }

        private string GetTroopCode(string troopName)
        {
            return troopName.ToLowerInvariant() switch
            {
                "worker" => "wo",
                "warrior" => "w",
                "scout" => "s",
                "pikeman" => "p",
                "swordsman" => "sw",
                "archer" => "a",
                "cavalry" => "c",
                "cataphract" => "cata",
                "transporter" => "t",
                "ballista" => "b",
                "ram" => "r",
                "catapult" => "cp",
                _ => "a"
            };
        }

        private void AddMessage(ChatMessage message)
        {
            lock (_historyLock)
            {
                _conversationHistory.Add(message);
            
                // Keep history manageable
                while (_conversationHistory.Count > 100)
                {
                    _conversationHistory.RemoveAt(0);
                }
            }

            MessageAdded?.Invoke(message);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (_historyLock)
            {
                _conversationHistory.Clear();
            }

            App.Logger.Debug("ChatbotService disposed");
        }

        #endregion
    }

    /// <summary>
    /// Chat intent classification.
    /// </summary>
    public enum ChatIntent
    {
        General,
        ProtocolQuery,
        KnowledgeQuery,
        TrafficAnalysis,
        Calculation,
        Automation
    }

    // Note: ChatMessage, ChatRole, and ChatContext are defined in Models/ChatMessage.cs

}