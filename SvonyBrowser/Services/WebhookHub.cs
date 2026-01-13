using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Webhook and external integration hub for connecting Svony Browser
    /// to Discord, Telegram, Slack, and custom webhooks.
    /// </summary>
    public sealed class WebhookHub : IDisposable
    {
        #region Singleton

        private static readonly Lazy<WebhookHub> _lazyInstance =
            new Lazy<WebhookHub>(() => new WebhookHub(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static WebhookHub Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly HttpClient _httpClient;
        private readonly List<WebhookConfig> _webhooks = new List<WebhookConfig>();
        private readonly Dictionary<string, DateTime> _lastNotifications = new Dictionary<string, DateTime>();
        private readonly object _lock = new object();
        private bool _disposed = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the configured webhooks.
        /// </summary>
        public IReadOnlyList<WebhookConfig> Webhooks
        {
            get
            {
                lock (_lock)
                {
                    return _webhooks.ToArray();
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired when a webhook is triggered.
        /// </summary>
        public event Action<string, string> WebhookTriggered;

        /// <summary>
        /// Fired when a webhook fails.
        /// </summary>
        public event Action<string, string> WebhookFailed;

        #endregion

        #region Constructor

        private WebhookHub()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            // Subscribe to game events
            GameStateEngine.Instance.UnderAttack += OnUnderAttack;
            GameStateEngine.Instance.ArmyArrived += OnArmyArrived;
            GameStateEngine.Instance.BuildingCompleted += OnBuildingCompleted;
            GameStateEngine.Instance.HeroLeveledUp += OnHeroLeveledUp;
            GameStateEngine.Instance.TrainingCompleted += OnTrainingCompleted;

            LoadWebhooksFromConfig();

            App.Logger.Information("WebhookHub initialized");
        }

        #endregion

        #region Public Methods - Configuration

        /// <summary>
        /// Adds a new webhook configuration.
        /// </summary>
        public void AddWebhook(WebhookConfig config)
        {
            lock (_lock)
            {
                _webhooks.Add(config);
            }
            SaveWebhooksToConfig();
            App.Logger.Information("Added webhook: {Name}", config.Name);
        }

        /// <summary>
        /// Removes a webhook configuration.
        /// </summary>
        public void RemoveWebhook(string name)
        {
            lock (_lock)
            {
                _webhooks.RemoveAll(w => w.Name == name);
            }
            SaveWebhooksToConfig();
            App.Logger.Information("Removed webhook: {Name}", name);
        }

        /// <summary>
        /// Updates a webhook configuration.
        /// </summary>
        public void UpdateWebhook(string name, WebhookConfig newConfig)
        {
            lock (_lock)
            {
                var index = _webhooks.FindIndex(w => w.Name == name);
                if (index >= 0)
                {
                    _webhooks[index] = newConfig;
                }
            }
            SaveWebhooksToConfig();
        }

        /// <summary>
        /// Tests a webhook by sending a test message.
        /// </summary>
        public async Task<bool> TestWebhookAsync(WebhookConfig config)
        {
            try
            {
                await SendNotificationAsync(config, "test", new JObject
                {
                    ["message"] = "Test notification from Svony Browser",
                    ["timestamp"] = DateTime.UtcNow.ToString("o")
                });
                return true;
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Webhook test failed: {Name}", config.Name);
                return false;
            }
        }

        #endregion

        #region Public Methods - Manual Triggers

        /// <summary>
        /// Manually triggers a notification for all webhooks subscribed to an event.
        /// </summary>
        public async Task TriggerEventAsync(string eventType, JObject data)
        {
            var webhooks = GetWebhooksForEvent(eventType);
        
            foreach (var webhook in webhooks)
            {
                await SendNotificationAsync(webhook, eventType, data);
            }
        }

        /// <summary>
        /// Sends a custom message to a specific webhook.
        /// </summary>
        public async Task SendCustomMessageAsync(string webhookName, string message)
        {
            var webhook = _webhooks.FirstOrDefault(w => w.Name == webhookName);
            if (webhook == null)
            {
                App.Logger.Warning("Webhook not found: {Name}", webhookName);
                return;
            }

            await SendNotificationAsync(webhook, "custom", new JObject
            {
                ["message"] = message,
                ["timestamp"] = DateTime.UtcNow.ToString("o")
            });
        }

        #endregion

        #region Private Methods - Event Handlers

        private async void OnUnderAttack(AttackAlert alert)
        {
            await TriggerEventAsync("under_attack", JObject.FromObject(new
            {
                alert.AttackerId,
                alert.AttackerName,
                alert.TargetCityId,
                alert.ArrivalTime,
                alert.TroopPower,
                message = $"⚔️ UNDER ATTACK! {alert.AttackerName} is attacking your city! ETA: {(alert.ArrivalTime - DateTime.UtcNow).TotalMinutes:F1} minutes"
            }));
        }

        private async void OnArmyArrived(MarchState march)
        {
            await TriggerEventAsync("march_complete", JObject.FromObject(new
            {
                march.MarchId,
                march.TargetX,
                march.TargetY,
                march.MissionType,
                message = $"🏃 March completed! Army arrived at ({march.TargetX}, {march.TargetY})"
            }));
        }

        private async void OnBuildingCompleted(int cityId, BuildingState building)
        {
            await TriggerEventAsync("building_done", JObject.FromObject(new
            {
                cityId,
                building.Name,
                building.Level,
                message = $"🏗️ Building complete! {building.Name} is now level {building.Level}"
            }));
        }

        private async void OnHeroLeveledUp(HeroState hero)
        {
            await TriggerEventAsync("hero_level_up", JObject.FromObject(new
            {
                hero.HeroId,
                hero.Name,
                hero.Level,
                message = $"⬆️ Hero leveled up! {hero.Name} is now level {hero.Level}"
            }));
        }

        private async void OnTrainingCompleted(int cityId, string troopType, int amount)
        {
            await TriggerEventAsync("training_done", JObject.FromObject(new
            {
                cityId,
                troopType,
                amount,
                message = $"⚔️ Training complete! {amount} {troopType} ready for battle"
            }));
        }

        #endregion

        #region Private Methods - Notification Sending

        private async Task SendNotificationAsync(WebhookConfig webhook, string eventType, JObject data)
        {
            // Check rate limiting
            var key = $"{webhook.Name}_{eventType}";
            if (_lastNotifications.TryGetValue(key, out var lastTime))
            {
                if ((DateTime.UtcNow - lastTime).TotalSeconds < webhook.RateLimitSeconds)
                {
                    App.Logger.Debug("Rate limited: {Key}", key);
                    return;
                }
            }
            _lastNotifications[key] = DateTime.UtcNow;

            try
            {
                var payload = FormatPayload(webhook, eventType, data);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(webhook.Url, content);
            
                if (response.IsSuccessStatusCode)
                {
                    WebhookTriggered?.Invoke(webhook.Name, eventType);
                    App.Logger.Information("Webhook sent: {Name} - {Event}", webhook.Name, eventType);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    WebhookFailed?.Invoke(webhook.Name, error);
                    App.Logger.Warning("Webhook failed: {Name} - {Status}", webhook.Name, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                WebhookFailed?.Invoke(webhook.Name, ex.Message);
                App.Logger.Error(ex, "Webhook error: {Name}", webhook.Name);
            }
        }

        private string FormatPayload(WebhookConfig webhook, string eventType, JObject data)
        {
            var message = data["message"]?.Value<string>() ?? eventType;

            return webhook.Format.ToLower() switch
            {
                "discord" => FormatDiscordPayload(webhook, eventType, message, data),
                "telegram" => FormatTelegramPayload(webhook, message),
                "slack" => FormatSlackPayload(webhook, eventType, message, data),
                "teams" => FormatTeamsPayload(webhook, eventType, message, data),
                _ => FormatGenericPayload(eventType, data)
            };
        }

        private string FormatDiscordPayload(WebhookConfig webhook, string eventType, string message, JObject data)
        {
            var color = eventType switch
            {
                "under_attack" => 15158332, // Red
                "march_complete" => 3066993, // Green
                "building_done" => 3447003, // Blue
                "hero_level_up" => 10181046, // Purple
                "training_done" => 15844367, // Gold
                _ => 9807270 // Gray
            };

            var embed = new JObject
            {
                ["title"] = $"Svony Browser - {eventType.Replace("_", " ").ToUpper()}",
                ["description"] = message,
                ["color"] = color,
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["footer"] = new JObject
                {
                    ["text"] = "Svony Browser Notifications"
                }
            };

            // Add fields for additional data
            var fields = new JArray();
            foreach (var prop in data.Properties().Where(p => p.Name != "message"))
            {
                fields.Add(new JObject
                {
                    ["name"] = prop.Name,
                    ["value"] = prop.Value.ToString(),
                    ["inline"] = true
                });
            }
        
            if (fields.Count > 0)
                embed["fields"] = fields;

            return new JObject
            {
                ["username"] = webhook.Username ?? "Svony Browser",
                ["avatar_url"] = webhook.AvatarUrl ?? "",
                ["embeds"] = new JArray { embed }
            }.ToString();
        }

        private string FormatTelegramPayload(WebhookConfig webhook, string message)
        {
            return new JObject
            {
                ["chat_id"] = webhook.ChatId ?? "",
                ["text"] = message,
                ["parse_mode"] = "HTML"
            }.ToString();
        }

        private string FormatSlackPayload(WebhookConfig webhook, string eventType, string message, JObject data)
        {
            var color = eventType switch
            {
                "under_attack" => "danger",
                "march_complete" => "good",
                "building_done" => "#3AA3E3",
                _ => "#808080"
            };

            return new JObject
            {
                ["attachments"] = new JArray
                {
                    new JObject
                    {
                        ["fallback"] = message,
                        ["color"] = color,
                        ["title"] = $"Svony Browser - {eventType}",
                        ["text"] = message,
                        ["ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    }
                }
            }.ToString();
        }

        private string FormatTeamsPayload(WebhookConfig webhook, string eventType, string message, JObject data)
        {
            var color = eventType switch
            {
                "under_attack" => "FF0000",
                "march_complete" => "00FF00",
                "building_done" => "0000FF",
                _ => "808080"
            };

            return new JObject
            {
                ["@type"] = "MessageCard",
                ["@context"] = "http://schema.org/extensions",
                ["themeColor"] = color,
                ["summary"] = message,
                ["sections"] = new JArray
                {
                    new JObject
                    {
                        ["activityTitle"] = $"Svony Browser - {eventType}",
                        ["activitySubtitle"] = DateTime.UtcNow.ToString("g"),
                        ["text"] = message
                    }
                }
            }.ToString();
        }

        private string FormatGenericPayload(string eventType, JObject data)
        {
            data["event"] = eventType;
            data["source"] = "Svony Browser";
            data["timestamp"] = DateTime.UtcNow.ToString("o");
            return data.ToString();
        }

        #endregion

        #region Private Methods - Configuration

        private List<WebhookConfig> GetWebhooksForEvent(string eventType)
        {
            lock (_lock)
            {
                return _webhooks
                    .Where(w => w.Enabled && w.Events.Contains(eventType))
                    .ToList();
            }
        }

        private void LoadWebhooksFromConfig()
        {
            try
            {
                var configPath = Path.Combine(App.ConfigPath, "webhooks.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var configs = JsonConvert.DeserializeObject<List<WebhookConfig>>(json);
                    if (configs != null)
                    {
                        lock (_lock)
                        {
                            _webhooks.Clear();
                            _webhooks.AddRange(configs);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to load webhooks config");
            }
        }

        private void SaveWebhooksToConfig()
        {
            try
            {
                var configPath = Path.Combine(App.ConfigPath, "webhooks.json");
                string json;
                lock (_lock)
                {
                    json = JsonConvert.SerializeObject(_webhooks, Formatting.Indented);
                }
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to save webhooks config");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _httpClient.Dispose();

            GameStateEngine.Instance.UnderAttack -= OnUnderAttack;
            GameStateEngine.Instance.ArmyArrived -= OnArmyArrived;
            GameStateEngine.Instance.BuildingCompleted -= OnBuildingCompleted;
            GameStateEngine.Instance.HeroLeveledUp -= OnHeroLeveledUp;
            GameStateEngine.Instance.TrainingCompleted -= OnTrainingCompleted;

            App.Logger.Information("WebhookHub disposed");
        }

        #endregion
    }

    #region Models

    public class WebhookConfig
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string Format { get; set; } = "generic"; // discord, telegram, slack, teams, generic
        public List<string> Events { get; set; } = new List<string>();
        public bool Enabled { get; set; } = true;
        public int RateLimitSeconds { get; set; } = 30;
    
        // Discord-specific
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
    
        // Telegram-specific
        public string ChatId { get; set; }
    
        // Custom headers
        public Dictionary<string, string> Headers { get; set; }
    }

    #endregion

}