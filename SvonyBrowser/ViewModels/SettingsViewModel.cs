using System;
using System.Threading.Tasks;
using SvonyBrowser.Models;
using SvonyBrowser.Services;

namespace SvonyBrowser.ViewModels
{
    /// <summary>
    /// ViewModel for Settings windows - handles all application settings with persistence.
    /// </summary>
    public class SettingsViewModel : ViewModelBase
    {
        #region Fields

        private AppSettings _settings;
        private bool _isDirty;
        private string _validationMessage;
        private bool _isValid = true;

        #endregion

        #region Constructor

        public SettingsViewModel()
        {
            _settings = SettingsManager.Instance.Settings ?? new AppSettings();
        }

        #endregion

        #region General Settings

        public bool StartMinimized
        {
            get => _settings.StartMinimized;
            set { _settings.StartMinimized = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool StartWithWindows
        {
            get => _settings.StartWithWindows;
            set { _settings.StartWithWindows = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool CheckForUpdates
        {
            get => _settings.CheckForUpdates;
            set { _settings.CheckForUpdates = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool ConfirmOnExit
        {
            get => _settings.ConfirmOnExit;
            set { _settings.ConfirmOnExit = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool RememberWindowPosition
        {
            get => _settings.RememberWindowPosition;
            set { _settings.RememberWindowPosition = value; OnPropertyChanged(); MarkDirty(); }
        }

        public string Theme
        {
            get => _settings.Theme;
            set { _settings.Theme = value; OnPropertyChanged(); MarkDirty(); ApplyTheme(); }
        }

        public string Language
        {
            get => _settings.Language;
            set { _settings.Language = value; OnPropertyChanged(); MarkDirty(); }
        }

        #endregion

        #region Browser Settings

        public bool EnableFlash
        {
            get => _settings.EnableFlash;
            set { _settings.EnableFlash = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool EnableGpuAcceleration
        {
            get => _settings.EnableGpuAcceleration;
            set { _settings.EnableGpuAcceleration = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool EnableDevTools
        {
            get => _settings.EnableDevTools;
            set { _settings.EnableDevTools = value; OnPropertyChanged(); MarkDirty(); }
        }

        public string AutoEvonySwfPath
        {
            get => _settings.AutoEvonySwfPath;
            set { _settings.AutoEvonySwfPath = value; OnPropertyChanged(); MarkDirty(); ValidatePath(value, "AutoEvony SWF"); }
        }

        public string EvonyClientSwfPath
        {
            get => _settings.EvonyClientSwfPath;
            set { _settings.EvonyClientSwfPath = value; OnPropertyChanged(); MarkDirty(); ValidatePath(value, "EvonyClient SWF"); }
        }

        public string FlashPluginPath
        {
            get => _settings.FlashPluginPath;
            set { _settings.FlashPluginPath = value; OnPropertyChanged(); MarkDirty(); ValidatePath(value, "Flash Plugin"); }
        }

        public string CachePath
        {
            get => _settings.CachePath;
            set { _settings.CachePath = value; OnPropertyChanged(); MarkDirty(); }
        }

        public int CacheSizeMb
        {
            get => _settings.CacheSizeMb;
            set { _settings.CacheSizeMb = ValidateRange(value, 50, 10000, "Cache Size"); OnPropertyChanged(); MarkDirty(); }
        }

        public double BrowserZoom
        {
            get => _settings.BrowserZoom;
            set { _settings.BrowserZoom = ValidateRange(value, 25, 500, "Browser Zoom"); OnPropertyChanged(); MarkDirty(); }
        }

        #endregion

        #region Proxy Settings

        public bool ProxyEnabled
        {
            get => _settings.ProxyEnabled;
            set { _settings.ProxyEnabled = value; OnPropertyChanged(); MarkDirty(); }
        }

        public string ProxyHost
        {
            get => _settings.ProxyHost;
            set { _settings.ProxyHost = value; OnPropertyChanged(); MarkDirty(); }
        }

        public int ProxyPort
        {
            get => _settings.ProxyPort;
            set { _settings.ProxyPort = ValidatePort(value, "Proxy Port"); OnPropertyChanged(); MarkDirty(); }
        }

        public string ProxyType
        {
            get => _settings.ProxyType;
            set { _settings.ProxyType = value; OnPropertyChanged(); MarkDirty(); }
        }

        #endregion

        #region MCP Settings

        public bool McpEnabled
        {
            get => _settings.McpEnabled;
            set { _settings.McpEnabled = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool McpAutoStart
        {
            get => _settings.McpAutoStart;
            set { _settings.McpAutoStart = value; OnPropertyChanged(); MarkDirty(); }
        }

        public string McpHost
        {
            get => _settings.McpHost;
            set { _settings.McpHost = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool RagEnabled
        {
            get => _settings.RagEnabled;
            set { _settings.RagEnabled = value; OnPropertyChanged(); MarkDirty(); }
        }

        public int RagPort
        {
            get => _settings.RagPort;
            set { _settings.RagPort = ValidatePort(value, "RAG Port"); OnPropertyChanged(); MarkDirty(); }
        }

        public bool RteEnabled
        {
            get => _settings.RteEnabled;
            set { _settings.RteEnabled = value; OnPropertyChanged(); MarkDirty(); }
        }

        public int RtePort
        {
            get => _settings.RtePort;
            set { _settings.RtePort = ValidatePort(value, "RTE Port"); OnPropertyChanged(); MarkDirty(); }
        }

        public int McpHealthCheckIntervalMs
        {
            get => _settings.McpHealthCheckIntervalMs;
            set { _settings.McpHealthCheckIntervalMs = ValidateRange(value, 1000, 60000, "Health Check Interval"); OnPropertyChanged(); MarkDirty(); }
        }

        #endregion

        #region LLM Settings

        public bool LlmEnabled
        {
            get => _settings.LlmEnabled;
            set { _settings.LlmEnabled = value; OnPropertyChanged(); MarkDirty(); }
        }

        public string LlmBackend
        {
            get => _settings.LlmBackend;
            set { _settings.LlmBackend = value; OnPropertyChanged(); MarkDirty(); }
        }

        public string LmStudioUrl
        {
            get => _settings.LmStudioUrl;
            set { _settings.LmStudioUrl = value; OnPropertyChanged(); MarkDirty(); ValidateUrl(value, "LM Studio URL"); }
        }

        public string OllamaUrl
        {
            get => _settings.OllamaUrl;
            set { _settings.OllamaUrl = value; OnPropertyChanged(); MarkDirty(); ValidateUrl(value, "Ollama URL"); }
        }

        public double LlmTemperature
        {
            get => _settings.LlmTemperature;
            set { _settings.LlmTemperature = ValidateRange(value, 0, 2, "Temperature"); OnPropertyChanged(); MarkDirty(); }
        }

        public int LlmMaxTokens
        {
            get => _settings.LlmMaxTokens;
            set { _settings.LlmMaxTokens = ValidateRange(value, 1, 32000, "Max Tokens"); OnPropertyChanged(); MarkDirty(); }
        }

        public bool LlmStreamResponse
        {
            get => _settings.LlmStreamResponse;
            set { _settings.LlmStreamResponse = value; OnPropertyChanged(); MarkDirty(); }
        }

        #endregion

        #region Fiddler Settings

        public bool FiddlerEnabled
        {
            get => _settings.FiddlerEnabled;
            set { _settings.FiddlerEnabled = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool FiddlerAutoStart
        {
            get => _settings.FiddlerAutoStart;
            set { _settings.FiddlerAutoStart = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool FiddlerDecodeAmf
        {
            get => _settings.FiddlerDecodeAmf;
            set { _settings.FiddlerDecodeAmf = value; OnPropertyChanged(); MarkDirty(); }
        }

        public int FiddlerPort
        {
            get => _settings.FiddlerPort;
            set { _settings.FiddlerPort = ValidatePort(value, "Fiddler Port"); OnPropertyChanged(); MarkDirty(); }
        }

        #endregion

        #region Automation Settings

        public bool AutoPilotEnabled
        {
            get => _settings.AutoPilotEnabled;
            set { _settings.AutoPilotEnabled = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool SafetyLimitsEnabled
        {
            get => _settings.SafetyLimitsEnabled;
            set { _settings.SafetyLimitsEnabled = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool RandomizeDelays
        {
            get => _settings.RandomizeDelays;
            set { _settings.RandomizeDelays = value; OnPropertyChanged(); MarkDirty(); }
        }

        public int MaxActionsPerMinute
        {
            get => _settings.MaxActionsPerMinute;
            set { _settings.MaxActionsPerMinute = ValidateRange(value, 1, 60, "Max Actions"); OnPropertyChanged(); MarkDirty(); }
        }

        public int ActionDelayMs
        {
            get => _settings.ActionDelayMs;
            set { _settings.ActionDelayMs = ValidateRange(value, 100, 10000, "Action Delay"); OnPropertyChanged(); MarkDirty(); }
        }

        #endregion

        #region Traffic Settings

        public bool TrafficAnalysisEnabled
        {
            get => _settings.TrafficAnalysisEnabled;
            set { _settings.TrafficAnalysisEnabled = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool PatternDetectionEnabled
        {
            get => _settings.PatternDetectionEnabled;
            set { _settings.PatternDetectionEnabled = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool TrafficAutoScroll
        {
            get => _settings.TrafficAutoScroll;
            set { _settings.TrafficAutoScroll = value; OnPropertyChanged(); MarkDirty(); }
        }

        public int MaxTrafficEntries
        {
            get => _settings.MaxTrafficEntries;
            set { _settings.MaxTrafficEntries = ValidateRange(value, 100, 10000, "Max Traffic Entries"); OnPropertyChanged(); MarkDirty(); }
        }

        #endregion

        #region Chatbot Settings

        public bool ChatbotEnabled
        {
            get => _settings.ChatbotEnabled;
            set { _settings.ChatbotEnabled = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool ChatbotShowTimestamps
        {
            get => _settings.ChatbotShowTimestamps;
            set { _settings.ChatbotShowTimestamps = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool ChatbotMarkdownEnabled
        {
            get => _settings.ChatbotMarkdownEnabled;
            set { _settings.ChatbotMarkdownEnabled = value; OnPropertyChanged(); MarkDirty(); }
        }

        #endregion

        #region StatusBar Settings

        public bool StatusBarEnabled
        {
            get => _settings.StatusBarEnabled;
            set { _settings.StatusBarEnabled = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool ShowRagProgress
        {
            get => _settings.ShowRagProgress;
            set { _settings.ShowRagProgress = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool ShowRteProgress
        {
            get => _settings.ShowRteProgress;
            set { _settings.ShowRteProgress = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool ShowLlmStats
        {
            get => _settings.ShowLlmStats;
            set { _settings.ShowLlmStats = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool ShowGpuTemp
        {
            get => _settings.ShowGpuTemp;
            set { _settings.ShowGpuTemp = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool ShowPacketStats
        {
            get => _settings.ShowPacketStats;
            set { _settings.ShowPacketStats = value; OnPropertyChanged(); MarkDirty(); }
        }

        #endregion

        #region Webhook Settings

        public bool WebhooksEnabled
        {
            get => _settings.WebhooksEnabled;
            set { _settings.WebhooksEnabled = value; OnPropertyChanged(); MarkDirty(); }
        }

        public string DiscordWebhookUrl
        {
            get => _settings.DiscordWebhookUrl;
            set { _settings.DiscordWebhookUrl = value; OnPropertyChanged(); MarkDirty(); ValidateUrl(value, "Discord Webhook"); }
        }

        public string TelegramBotToken
        {
            get => _settings.TelegramBotToken;
            set { _settings.TelegramBotToken = value; OnPropertyChanged(); MarkDirty(); }
        }

        public string TelegramChatId
        {
            get => _settings.TelegramChatId;
            set { _settings.TelegramChatId = value; OnPropertyChanged(); MarkDirty(); }
        }

        #endregion

        #region Advanced Settings

        public bool DebugMode
        {
            get => _settings.DebugMode;
            set { _settings.DebugMode = value; OnPropertyChanged(); MarkDirty(); }
        }

        public bool PerformanceMode
        {
            get => _settings.PerformanceMode;
            set { _settings.PerformanceMode = value; OnPropertyChanged(); MarkDirty(); }
        }

        public string LogLevel
        {
            get => _settings.LogLevel;
            set { _settings.LogLevel = value; OnPropertyChanged(); MarkDirty(); }
        }

        public int LogRetentionDays
        {
            get => _settings.LogRetentionDays;
            set { _settings.LogRetentionDays = ValidateRange(value, 1, 365, "Log Retention"); OnPropertyChanged(); MarkDirty(); }
        }

        public string LogPath
        {
            get => _settings.LogPath;
            set { _settings.LogPath = value; OnPropertyChanged(); MarkDirty(); }
        }

        public int MaxMemoryMb
        {
            get => _settings.MaxMemoryMb;
            set { _settings.MaxMemoryMb = ValidateRange(value, 256, 16384, "Max Memory"); OnPropertyChanged(); MarkDirty(); }
        }

        #endregion

        #region Status Properties

        /// <summary>
        /// Gets whether settings have been modified since last save.
        /// </summary>
        public bool IsDirty
        {
            get => _isDirty;
            private set => SetProperty(ref _isDirty, value);
        }

        /// <summary>
        /// Gets whether all settings are valid.
        /// </summary>
        public bool IsValid
        {
            get => _isValid;
            private set => SetProperty(ref _isValid, value);
        }

        /// <summary>
        /// Gets the current validation message.
        /// </summary>
        public string ValidationMessage
        {
            get => _validationMessage;
            private set => SetProperty(ref _validationMessage, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Saves all settings to disk.
        /// </summary>
        public async Task SaveAsync()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException($"Cannot save invalid settings: {ValidationMessage}");
            }

            try
            {
                await SettingsManager.Instance.SaveAsync();
                IsDirty = false;
                App.Logger.Information("Settings saved successfully");
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to save settings");
                throw;
            }
        }

        /// <summary>
        /// Reloads settings from disk, discarding changes.
        /// </summary>
        public async Task ReloadAsync()
        {
            await SettingsManager.Instance.LoadAsync();
            _settings = SettingsManager.Instance.Settings;
            IsDirty = false;
            
            // Notify all properties changed
            OnPropertyChanged(string.Empty);
        }

        /// <summary>
        /// Resets all settings to defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            _settings = new AppSettings();
            SettingsManager.Instance.Settings = _settings;
            IsDirty = true;
            
            // Notify all properties changed
            OnPropertyChanged(string.Empty);
        }

        /// <summary>
        /// Exports settings to a file.
        /// </summary>
        public async Task ExportAsync(string filePath)
        {
            await SettingsManager.Instance.ExportAsync(filePath);
        }

        /// <summary>
        /// Imports settings from a file.
        /// </summary>
        public async Task ImportAsync(string filePath)
        {
            await SettingsManager.Instance.ImportAsync(filePath);
            _settings = SettingsManager.Instance.Settings;
            IsDirty = true;
            
            // Notify all properties changed
            OnPropertyChanged(string.Empty);
        }

        private void MarkDirty()
        {
            IsDirty = true;
        }

        private void ApplyTheme()
        {
            ThemeManager.Instance.ApplyTheme(Theme);
        }

        #endregion

        #region Validation Helpers

        private int ValidatePort(int value, string fieldName)
        {
            if (value < 1 || value > 65535)
            {
                ValidationMessage = $"{fieldName} must be between 1 and 65535";
                IsValid = false;
                return Math.Clamp(value, 1, 65535);
            }
            ClearValidation();
            return value;
        }

        private int ValidateRange(int value, int min, int max, string fieldName)
        {
            if (value < min || value > max)
            {
                ValidationMessage = $"{fieldName} must be between {min} and {max}";
                IsValid = false;
                return Math.Clamp(value, min, max);
            }
            ClearValidation();
            return value;
        }

        private double ValidateRange(double value, double min, double max, string fieldName)
        {
            if (value < min || value > max)
            {
                ValidationMessage = $"{fieldName} must be between {min} and {max}";
                IsValid = false;
                return Math.Clamp(value, min, max);
            }
            ClearValidation();
            return value;
        }

        private void ValidatePath(string path, string fieldName)
        {
            if (!string.IsNullOrEmpty(path) && !System.IO.File.Exists(path))
            {
                ValidationMessage = $"{fieldName} file not found: {path}";
                IsValid = false;
            }
            else
            {
                ClearValidation();
            }
        }

        private void ValidateUrl(string url, string fieldName)
        {
            if (!string.IsNullOrEmpty(url) && !Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                ValidationMessage = $"{fieldName} is not a valid URL";
                IsValid = false;
            }
            else
            {
                ClearValidation();
            }
        }

        private void ClearValidation()
        {
            ValidationMessage = string.Empty;
            IsValid = true;
        }

        #endregion
    }
}
