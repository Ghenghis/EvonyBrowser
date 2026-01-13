using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace SvonyBrowser.Models
{
    /// <summary>
    /// Complete application settings model for Svony Browser v6.0 Borg Edition
    /// Contains 164 configurable options across 12 categories
    /// </summary>
    public class AppSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        
        // Schema version
        [JsonProperty(PropertyName = "$schema")]
        public string Schema { get; set; } = "https://svony-browser.io/schemas/settings.json";
        
        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; } = "6.0.0";
        
        [JsonProperty(PropertyName = "general")]
        public GeneralSettings General { get; set; } = new GeneralSettings();
        
        [JsonProperty(PropertyName = "browser")]
        public BrowserSettings Browser { get; set; } = new BrowserSettings();
        
        [JsonProperty(PropertyName = "proxy")]
        public ProxySettings Proxy { get; set; } = new ProxySettings();
        
        [JsonProperty(PropertyName = "mcp")]
        public McpSettings Mcp { get; set; } = new McpSettings();
        
        [JsonProperty(PropertyName = "llm")]
        public LlmSettings Llm { get; set; } = new LlmSettings();
        
        [JsonProperty(PropertyName = "fiddler")]
        public FiddlerSettings Fiddler { get; set; } = new FiddlerSettings();
        
        [JsonProperty(PropertyName = "automation")]
        public AutomationSettings Automation { get; set; } = new AutomationSettings();
        
        [JsonProperty(PropertyName = "traffic")]
        public TrafficSettings Traffic { get; set; } = new TrafficSettings();
        
        [JsonProperty(PropertyName = "chatbot")]
        public ChatbotSettings Chatbot { get; set; } = new ChatbotSettings();
        
        [JsonProperty(PropertyName = "statusBar")]
        public StatusBarSettings StatusBar { get; set; } = new StatusBarSettings();
        
        [JsonProperty(PropertyName = "webhooks")]
        public WebhookSettings Webhooks { get; set; } = new WebhookSettings();
        
        [JsonProperty(PropertyName = "advanced")]
        public AdvancedSettings Advanced { get; set; } = new AdvancedSettings();
    }
    
    #region General Settings (15 options)
    
    public class GeneralSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private string _theme = "Dark";
        private string _language = "English";
        private bool _startMinimized = false;
        private bool _startWithWindows = false;
        private bool _checkUpdates = true;
        private string _updateChannel = "Stable";
        private bool _telemetryEnabled = false;
        private string _logLevel = "Info";
        private int _logRetentionDays = 7;
        private int _maxLogSizeMB = 100;
        private bool _backupEnabled = true;
        private int _backupIntervalHours = 24;
        private bool _confirmOnExit = true;
        private bool _rememberWindowPosition = true;
        private bool _rememberWindowSize = true;
        
        [JsonProperty(PropertyName = "theme")]
        public string Theme { get => _theme; set { _theme = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Theme))); } }
        
        [JsonProperty(PropertyName = "language")]
        public string Language { get => _language; set { _language = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language))); } }
        
        [JsonProperty(PropertyName = "startMinimized")]
        public bool StartMinimized { get => _startMinimized; set { _startMinimized = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartMinimized))); } }
        
        [JsonProperty(PropertyName = "startWithWindows")]
        public bool StartWithWindows { get => _startWithWindows; set { _startWithWindows = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartWithWindows))); } }
        
        [JsonProperty(PropertyName = "checkUpdates")]
        public bool CheckUpdates { get => _checkUpdates; set { _checkUpdates = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CheckUpdates))); } }
        
        [JsonProperty(PropertyName = "updateChannel")]
        public string UpdateChannel { get => _updateChannel; set { _updateChannel = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdateChannel))); } }
        
        [JsonProperty(PropertyName = "telemetryEnabled")]
        public bool TelemetryEnabled { get => _telemetryEnabled; set { _telemetryEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TelemetryEnabled))); } }
        
        [JsonProperty(PropertyName = "logLevel")]
        public string LogLevel { get => _logLevel; set { _logLevel = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogLevel))); } }
        
        [JsonProperty(PropertyName = "logRetentionDays")]
        public int LogRetentionDays { get => _logRetentionDays; set { _logRetentionDays = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogRetentionDays))); } }
        
        [JsonProperty(PropertyName = "maxLogSizeMB")]
        public int MaxLogSizeMB { get => _maxLogSizeMB; set { _maxLogSizeMB = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxLogSizeMB))); } }
        
        [JsonProperty(PropertyName = "backupEnabled")]
        public bool BackupEnabled { get => _backupEnabled; set { _backupEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BackupEnabled))); } }
        
        [JsonProperty(PropertyName = "backupIntervalHours")]
        public int BackupIntervalHours { get => _backupIntervalHours; set { _backupIntervalHours = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BackupIntervalHours))); } }
        
        [JsonProperty(PropertyName = "confirmOnExit")]
        public bool ConfirmOnExit { get => _confirmOnExit; set { _confirmOnExit = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConfirmOnExit))); } }
        
        [JsonProperty(PropertyName = "rememberWindowPosition")]
        public bool RememberWindowPosition { get => _rememberWindowPosition; set { _rememberWindowPosition = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RememberWindowPosition))); } }
        
        [JsonProperty(PropertyName = "rememberWindowSize")]
        public bool RememberWindowSize { get => _rememberWindowSize; set { _rememberWindowSize = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RememberWindowSize))); } }
    }
    
    #endregion
    
    #region Browser Settings (20 options)
    
    public class BrowserSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private string _defaultServer = "cc2.evony.com";
        private string _customServerUrl = "";
        private string _homePage = "https://cc2.evony.com";
        private string _userAgent = "";
        private int _cacheSizeMB = 500;
        private bool _clearCacheOnExit = false;
        private bool _enableJavaScript = true;
        private bool _enableFlash = true;
        private bool _enableWebGL = true;
        private bool _enableGPU = true;
        private int _zoomLevel = 100;
        private int _defaultZoom = 100;
        private bool _enableDevTools = false;
        private bool _enableContextMenu = true;
        private bool _enableDragDrop = false;
        private string _tabBehavior = "NewTab";
        private int _maxTabs = 10;
        private int _sessionTimeout = 0;
        private bool _autoRefresh = false;
        private int _refreshInterval = 300;
        private string _autoEvonySwfPath = "";
        private string _evonyClientSwfPath = "";
        private string _flashPluginPath = "";
        
        [JsonProperty(PropertyName = "defaultServer")]
        public string DefaultServer { get => _defaultServer; set { _defaultServer = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DefaultServer))); } }
        
        [JsonProperty(PropertyName = "customServerUrl")]
        public string CustomServerUrl { get => _customServerUrl; set { _customServerUrl = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CustomServerUrl))); } }
        
        [JsonProperty(PropertyName = "homePage")]
        public string HomePage { get => _homePage; set { _homePage = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HomePage))); } }
        
        [JsonProperty(PropertyName = "userAgent")]
        public string UserAgent { get => _userAgent; set { _userAgent = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserAgent))); } }
        
        [JsonProperty(PropertyName = "cacheSizeMB")]
        public int CacheSizeMB { get => _cacheSizeMB; set { _cacheSizeMB = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CacheSizeMB))); } }
        
        [JsonProperty(PropertyName = "clearCacheOnExit")]
        public bool ClearCacheOnExit { get => _clearCacheOnExit; set { _clearCacheOnExit = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ClearCacheOnExit))); } }
        
        [JsonProperty(PropertyName = "enableJavaScript")]
        public bool EnableJavaScript { get => _enableJavaScript; set { _enableJavaScript = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableJavaScript))); } }
        
        [JsonProperty(PropertyName = "enableFlash")]
        public bool EnableFlash { get => _enableFlash; set { _enableFlash = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableFlash))); } }
        
        [JsonProperty(PropertyName = "enableWebGL")]
        public bool EnableWebGL { get => _enableWebGL; set { _enableWebGL = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableWebGL))); } }
        
        [JsonProperty(PropertyName = "enableGPU")]
        public bool EnableGPU { get => _enableGPU; set { _enableGPU = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableGPU))); } }
        
        [JsonProperty(PropertyName = "zoomLevel")]
        public int ZoomLevel { get => _zoomLevel; set { _zoomLevel = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZoomLevel))); } }
        
        [JsonProperty(PropertyName = "defaultZoom")]
        public int DefaultZoom { get => _defaultZoom; set { _defaultZoom = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DefaultZoom))); } }
        
        [JsonProperty(PropertyName = "enableDevTools")]
        public bool EnableDevTools { get => _enableDevTools; set { _enableDevTools = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableDevTools))); } }
        
        [JsonProperty(PropertyName = "enableContextMenu")]
        public bool EnableContextMenu { get => _enableContextMenu; set { _enableContextMenu = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableContextMenu))); } }
        
        [JsonProperty(PropertyName = "enableDragDrop")]
        public bool EnableDragDrop { get => _enableDragDrop; set { _enableDragDrop = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableDragDrop))); } }
        
        [JsonProperty(PropertyName = "tabBehavior")]
        public string TabBehavior { get => _tabBehavior; set { _tabBehavior = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TabBehavior))); } }
        
        [JsonProperty(PropertyName = "maxTabs")]
        public int MaxTabs { get => _maxTabs; set { _maxTabs = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxTabs))); } }
        
        [JsonProperty(PropertyName = "sessionTimeout")]
        public int SessionTimeout { get => _sessionTimeout; set { _sessionTimeout = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SessionTimeout))); } }
        
        [JsonProperty(PropertyName = "autoRefresh")]
        public bool AutoRefresh { get => _autoRefresh; set { _autoRefresh = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoRefresh))); } }
        
        [JsonProperty(PropertyName = "refreshInterval")]
        public int RefreshInterval { get => _refreshInterval; set { _refreshInterval = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RefreshInterval))); } }
        
        [JsonProperty(PropertyName = "autoEvonySwfPath")]
        public string AutoEvonySwfPath { get => _autoEvonySwfPath; set { _autoEvonySwfPath = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoEvonySwfPath))); } }
        
        [JsonProperty(PropertyName = "evonyClientSwfPath")]
        public string EvonyClientSwfPath { get => _evonyClientSwfPath; set { _evonyClientSwfPath = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EvonyClientSwfPath))); } }
        
        [JsonProperty(PropertyName = "flashPluginPath")]
        public string FlashPluginPath { get => _flashPluginPath; set { _flashPluginPath = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FlashPluginPath))); } }
    }
    
    #endregion
    
    #region Proxy Settings (12 options)
    
    public class ProxySettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private bool _enabled = true;
        private string _host = "127.0.0.1";
        private int _port = 8888;
        private string _type = "HTTP";
        private bool _authEnabled = false;
        private string _username = "";
        private string _password = "";
        private bool _bypassLocal = true;
        private string _bypassList = "";
        private bool _autoDetect = false;
        private string _pacUrl = "";
        private bool _testOnStartup = true;
        
        [JsonProperty(PropertyName = "enabled")]
        public bool Enabled { get => _enabled; set { _enabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled))); } }
        
        [JsonProperty(PropertyName = "host")]
        public string Host { get => _host; set { _host = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Host))); } }
        
        [JsonProperty(PropertyName = "port")]
        public int Port { get => _port; set { _port = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Port))); } }
        
        [JsonProperty(PropertyName = "type")]
        public string Type { get => _type; set { _type = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Type))); } }
        
        [JsonProperty(PropertyName = "authEnabled")]
        public bool AuthEnabled { get => _authEnabled; set { _authEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AuthEnabled))); } }
        
        [JsonProperty(PropertyName = "username")]
        public string Username { get => _username; set { _username = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Username))); } }
        
        [JsonProperty(PropertyName = "password")]
        public string Password { get => _password; set { _password = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Password))); } }
        
        [JsonProperty(PropertyName = "bypassLocal")]
        public bool BypassLocal { get => _bypassLocal; set { _bypassLocal = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BypassLocal))); } }
        
        [JsonProperty(PropertyName = "bypassList")]
        public string BypassList { get => _bypassList; set { _bypassList = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BypassList))); } }
        
        [JsonProperty(PropertyName = "autoDetect")]
        public bool AutoDetect { get => _autoDetect; set { _autoDetect = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoDetect))); } }
        
        [JsonProperty(PropertyName = "pacUrl")]
        public string PacUrl { get => _pacUrl; set { _pacUrl = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PacUrl))); } }
        
        [JsonProperty(PropertyName = "testOnStartup")]
        public bool TestOnStartup { get => _testOnStartup; set { _testOnStartup = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TestOnStartup))); } }
    }
    
    #endregion
    
    #region MCP Settings (18 options)
    
    public class McpSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private bool _enabled = true;
        private bool _autoStartServers = true;
        private bool _ragServerEnabled = true;
        private int _ragServerPort = 3001;
        private bool _rteServerEnabled = true;
        private int _rteServerPort = 3002;
        private bool _toolsServerEnabled = true;
        private int _toolsServerPort = 3003;
        private bool _cdpServerEnabled = true;
        private int _cdpServerPort = 9222;
        private int _healthCheckInterval = 30;
        private int _maxRetries = 3;
        private int _retryDelay = 5;
        private int _connectionTimeout = 30;
        private bool _logMcpTraffic = false;
        private string _mcpLogLevel = "Info";
        private bool _restartOnCrash = true;
        private int _maxRestarts = 5;
        
        [JsonProperty(PropertyName = "enabled")]
        public bool Enabled { get => _enabled; set { _enabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled))); } }
        
        [JsonProperty(PropertyName = "autoStartServers")]
        public bool AutoStartServers { get => _autoStartServers; set { _autoStartServers = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoStartServers))); } }
        
        [JsonProperty(PropertyName = "ragServerEnabled")]
        public bool RagServerEnabled { get => _ragServerEnabled; set { _ragServerEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RagServerEnabled))); } }
        
        [JsonProperty(PropertyName = "ragServerPort")]
        public int RagServerPort { get => _ragServerPort; set { _ragServerPort = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RagServerPort))); } }
        
        [JsonProperty(PropertyName = "rteServerEnabled")]
        public bool RteServerEnabled { get => _rteServerEnabled; set { _rteServerEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RteServerEnabled))); } }
        
        [JsonProperty(PropertyName = "rteServerPort")]
        public int RteServerPort { get => _rteServerPort; set { _rteServerPort = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RteServerPort))); } }
        
        [JsonProperty(PropertyName = "toolsServerEnabled")]
        public bool ToolsServerEnabled { get => _toolsServerEnabled; set { _toolsServerEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ToolsServerEnabled))); } }
        
        [JsonProperty(PropertyName = "toolsServerPort")]
        public int ToolsServerPort { get => _toolsServerPort; set { _toolsServerPort = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ToolsServerPort))); } }
        
        [JsonProperty(PropertyName = "cdpServerEnabled")]
        public bool CdpServerEnabled { get => _cdpServerEnabled; set { _cdpServerEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CdpServerEnabled))); } }
        
        [JsonProperty(PropertyName = "cdpServerPort")]
        public int CdpServerPort { get => _cdpServerPort; set { _cdpServerPort = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CdpServerPort))); } }
        
        [JsonProperty(PropertyName = "healthCheckInterval")]
        public int HealthCheckInterval { get => _healthCheckInterval; set { _healthCheckInterval = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HealthCheckInterval))); } }
        
        [JsonProperty(PropertyName = "maxRetries")]
        public int MaxRetries { get => _maxRetries; set { _maxRetries = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxRetries))); } }
        
        [JsonProperty(PropertyName = "retryDelay")]
        public int RetryDelay { get => _retryDelay; set { _retryDelay = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RetryDelay))); } }
        
        [JsonProperty(PropertyName = "connectionTimeout")]
        public int ConnectionTimeout { get => _connectionTimeout; set { _connectionTimeout = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConnectionTimeout))); } }
        
        [JsonProperty(PropertyName = "logMcpTraffic")]
        public bool LogMcpTraffic { get => _logMcpTraffic; set { _logMcpTraffic = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogMcpTraffic))); } }
        
        [JsonProperty(PropertyName = "mcpLogLevel")]
        public string McpLogLevel { get => _mcpLogLevel; set { _mcpLogLevel = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(McpLogLevel))); } }
        
        [JsonProperty(PropertyName = "restartOnCrash")]
        public bool RestartOnCrash { get => _restartOnCrash; set { _restartOnCrash = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RestartOnCrash))); } }
        
        [JsonProperty(PropertyName = "maxRestarts")]
        public int MaxRestarts { get => _maxRestarts; set { _maxRestarts = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxRestarts))); } }
    }
    
    #endregion
    
    #region LLM Settings (22 options)
    
    public class LlmSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private bool _enabled = true;
        private string _backend = "LmStudio";
        private string _lmStudioUrl = "http://localhost:1234/v1";
        private string _ollamaUrl = "http://localhost:11434";
        private string _openAiApiKey = "";
        private string _defaultModel = "evony-7b";
        private double _temperature = 0.7;
        private int _maxTokens = 2048;
        private double _topP = 0.9;
        private int _topK = 40;
        private double _repetitionPenalty = 1.1;
        private int _contextLength = 4096;
        private int _gpuLayers = -1;
        private int _threadCount = 0;
        private int _batchSize = 512;
        private bool _flashAttention = true;
        private bool _streamResponse = true;
        private bool _showTokenCount = true;
        private bool _showInferenceTime = true;
        private bool _cachePrompts = true;
        private int _maxCachedPrompts = 100;
        private string _systemPrompt = "";
        
        [JsonProperty(PropertyName = "enabled")]
        public bool Enabled { get => _enabled; set { _enabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled))); } }
        
        [JsonProperty(PropertyName = "backend")]
        public string Backend { get => _backend; set { _backend = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Backend))); } }
        
        [JsonProperty(PropertyName = "lmStudioUrl")]
        public string LmStudioUrl { get => _lmStudioUrl; set { _lmStudioUrl = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LmStudioUrl))); } }
        
        [JsonProperty(PropertyName = "ollamaUrl")]
        public string OllamaUrl { get => _ollamaUrl; set { _ollamaUrl = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OllamaUrl))); } }
        
        [JsonProperty(PropertyName = "openAiApiKey")]
        public string OpenAiApiKey { get => _openAiApiKey; set { _openAiApiKey = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OpenAiApiKey))); } }
        
        [JsonProperty(PropertyName = "defaultModel")]
        public string DefaultModel { get => _defaultModel; set { _defaultModel = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DefaultModel))); } }
        
        [JsonProperty(PropertyName = "temperature")]
        public double Temperature { get => _temperature; set { _temperature = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Temperature))); } }
        
        [JsonProperty(PropertyName = "maxTokens")]
        public int MaxTokens { get => _maxTokens; set { _maxTokens = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxTokens))); } }
        
        [JsonProperty(PropertyName = "topP")]
        public double TopP { get => _topP; set { _topP = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TopP))); } }
        
        [JsonProperty(PropertyName = "topK")]
        public int TopK { get => _topK; set { _topK = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TopK))); } }
        
        [JsonProperty(PropertyName = "repetitionPenalty")]
        public double RepetitionPenalty { get => _repetitionPenalty; set { _repetitionPenalty = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RepetitionPenalty))); } }
        
        [JsonProperty(PropertyName = "contextLength")]
        public int ContextLength { get => _contextLength; set { _contextLength = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ContextLength))); } }
        
        [JsonProperty(PropertyName = "gpuLayers")]
        public int GpuLayers { get => _gpuLayers; set { _gpuLayers = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GpuLayers))); } }
        
        [JsonProperty(PropertyName = "threadCount")]
        public int ThreadCount { get => _threadCount; set { _threadCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ThreadCount))); } }
        
        [JsonProperty(PropertyName = "batchSize")]
        public int BatchSize { get => _batchSize; set { _batchSize = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BatchSize))); } }
        
        [JsonProperty(PropertyName = "flashAttention")]
        public bool FlashAttention { get => _flashAttention; set { _flashAttention = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FlashAttention))); } }
        
        [JsonProperty(PropertyName = "streamResponse")]
        public bool StreamResponse { get => _streamResponse; set { _streamResponse = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StreamResponse))); } }
        
        [JsonProperty(PropertyName = "showTokenCount")]
        public bool ShowTokenCount { get => _showTokenCount; set { _showTokenCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowTokenCount))); } }
        
        [JsonProperty(PropertyName = "showInferenceTime")]
        public bool ShowInferenceTime { get => _showInferenceTime; set { _showInferenceTime = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowInferenceTime))); } }
        
        [JsonProperty(PropertyName = "cachePrompts")]
        public bool CachePrompts { get => _cachePrompts; set { _cachePrompts = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CachePrompts))); } }
        
        [JsonProperty(PropertyName = "maxCachedPrompts")]
        public int MaxCachedPrompts { get => _maxCachedPrompts; set { _maxCachedPrompts = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxCachedPrompts))); } }
        
        [JsonProperty(PropertyName = "systemPrompt")]
        public string SystemPrompt { get => _systemPrompt; set { _systemPrompt = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SystemPrompt))); } }
    }
    
    #endregion
    
    #region Fiddler Settings (10 options)
    
    public class FiddlerSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private bool _enabled = true;
        private bool _autoStartFiddler = false;
        private string _fiddlerPath = "";
        private string _pipeName = "EvonyTrafficPipe";
        private bool _captureTraffic = true;
        private bool _decodeAmf = true;
        private bool _logTraffic = false;
        private int _maxTrafficEntries = 1000;
        private string _filterPattern = "*.evony.com";
        private bool _breakpointsEnabled = false;
        
        [JsonProperty(PropertyName = "enabled")]
        public bool Enabled { get => _enabled; set { _enabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled))); } }
        
        [JsonProperty(PropertyName = "autoStartFiddler")]
        public bool AutoStartFiddler { get => _autoStartFiddler; set { _autoStartFiddler = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoStartFiddler))); } }
        
        [JsonProperty(PropertyName = "fiddlerPath")]
        public string FiddlerPath { get => _fiddlerPath; set { _fiddlerPath = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FiddlerPath))); } }
        
        [JsonProperty(PropertyName = "pipeName")]
        public string PipeName { get => _pipeName; set { _pipeName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PipeName))); } }
        
        [JsonProperty(PropertyName = "captureTraffic")]
        public bool CaptureTraffic { get => _captureTraffic; set { _captureTraffic = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CaptureTraffic))); } }
        
        [JsonProperty(PropertyName = "decodeAmf")]
        public bool DecodeAmf { get => _decodeAmf; set { _decodeAmf = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DecodeAmf))); } }
        
        [JsonProperty(PropertyName = "logTraffic")]
        public bool LogTraffic { get => _logTraffic; set { _logTraffic = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogTraffic))); } }
        
        [JsonProperty(PropertyName = "maxTrafficEntries")]
        public int MaxTrafficEntries { get => _maxTrafficEntries; set { _maxTrafficEntries = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxTrafficEntries))); } }
        
        [JsonProperty(PropertyName = "filterPattern")]
        public string FilterPattern { get => _filterPattern; set { _filterPattern = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilterPattern))); } }
        
        [JsonProperty(PropertyName = "breakpointsEnabled")]
        public bool BreakpointsEnabled { get => _breakpointsEnabled; set { _breakpointsEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BreakpointsEnabled))); } }
    }
    
    #endregion
    
    #region Automation Settings (16 options)
    
    public class AutomationSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private bool _autoPilotEnabled = false;
        private bool _safetyLimitsEnabled = true;
        private int _maxActionsPerMinute = 30;
        private int _maxActionsPerHour = 500;
        private int _actionDelay = 100;
        private bool _randomizeDelay = true;
        private int _delayVariance = 20;
        private bool _pauseOnError = true;
        private int _maxErrors = 5;
        private int _errorCooldown = 60;
        private int _clickPrecision = 5;
        private int _doubleClickDelay = 100;
        private int _typeDelay = 50;
        private int _scrollSpeed = 100;
        private bool _screenshotOnAction = false;
        private bool _logActions = true;
        
        [JsonProperty(PropertyName = "autoPilotEnabled")]
        public bool AutoPilotEnabled { get => _autoPilotEnabled; set { _autoPilotEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoPilotEnabled))); } }
        
        [JsonProperty(PropertyName = "safetyLimitsEnabled")]
        public bool SafetyLimitsEnabled { get => _safetyLimitsEnabled; set { _safetyLimitsEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SafetyLimitsEnabled))); } }
        
        [JsonProperty(PropertyName = "maxActionsPerMinute")]
        public int MaxActionsPerMinute { get => _maxActionsPerMinute; set { _maxActionsPerMinute = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxActionsPerMinute))); } }
        
        [JsonProperty(PropertyName = "maxActionsPerHour")]
        public int MaxActionsPerHour { get => _maxActionsPerHour; set { _maxActionsPerHour = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxActionsPerHour))); } }
        
        [JsonProperty(PropertyName = "actionDelay")]
        public int ActionDelay { get => _actionDelay; set { _actionDelay = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActionDelay))); } }
        
        [JsonProperty(PropertyName = "randomizeDelay")]
        public bool RandomizeDelay { get => _randomizeDelay; set { _randomizeDelay = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RandomizeDelay))); } }
        
        [JsonProperty(PropertyName = "delayVariance")]
        public int DelayVariance { get => _delayVariance; set { _delayVariance = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DelayVariance))); } }
        
        [JsonProperty(PropertyName = "pauseOnError")]
        public bool PauseOnError { get => _pauseOnError; set { _pauseOnError = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PauseOnError))); } }
        
        [JsonProperty(PropertyName = "maxErrors")]
        public int MaxErrors { get => _maxErrors; set { _maxErrors = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxErrors))); } }
        
        [JsonProperty(PropertyName = "errorCooldown")]
        public int ErrorCooldown { get => _errorCooldown; set { _errorCooldown = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorCooldown))); } }
        
        [JsonProperty(PropertyName = "clickPrecision")]
        public int ClickPrecision { get => _clickPrecision; set { _clickPrecision = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ClickPrecision))); } }
        
        [JsonProperty(PropertyName = "doubleClickDelay")]
        public int DoubleClickDelay { get => _doubleClickDelay; set { _doubleClickDelay = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DoubleClickDelay))); } }
        
        [JsonProperty(PropertyName = "typeDelay")]
        public int TypeDelay { get => _typeDelay; set { _typeDelay = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TypeDelay))); } }
        
        [JsonProperty(PropertyName = "scrollSpeed")]
        public int ScrollSpeed { get => _scrollSpeed; set { _scrollSpeed = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ScrollSpeed))); } }
        
        [JsonProperty(PropertyName = "screenshotOnAction")]
        public bool ScreenshotOnAction { get => _screenshotOnAction; set { _screenshotOnAction = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ScreenshotOnAction))); } }
        
        [JsonProperty(PropertyName = "logActions")]
        public bool LogActions { get => _logActions; set { _logActions = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogActions))); } }
    }
    
    #endregion
    
    #region Traffic Settings (12 options)
    
    public class TrafficSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private bool _analysisEnabled = true;
        private bool _autoAnalyze = true;
        private bool _patternDetection = true;
        private bool _anomalyDetection = true;
        private int _maxPacketsInMemory = 5000;
        private string _packetRetention = "Session";
        private string _exportFormat = "JSON";
        private bool _compressExports = true;
        private bool _encryptExports = false;
        private string _exportPassword = "";
        private bool _highlightNew = true;
        private bool _autoScroll = true;
        
        [JsonProperty(PropertyName = "analysisEnabled")]
        public bool AnalysisEnabled { get => _analysisEnabled; set { _analysisEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AnalysisEnabled))); } }
        
        [JsonProperty(PropertyName = "autoAnalyze")]
        public bool AutoAnalyze { get => _autoAnalyze; set { _autoAnalyze = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoAnalyze))); } }
        
        [JsonProperty(PropertyName = "patternDetection")]
        public bool PatternDetection { get => _patternDetection; set { _patternDetection = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PatternDetection))); } }
        
        [JsonProperty(PropertyName = "anomalyDetection")]
        public bool AnomalyDetection { get => _anomalyDetection; set { _anomalyDetection = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AnomalyDetection))); } }
        
        [JsonProperty(PropertyName = "maxPacketsInMemory")]
        public int MaxPacketsInMemory { get => _maxPacketsInMemory; set { _maxPacketsInMemory = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxPacketsInMemory))); } }
        
        [JsonProperty(PropertyName = "packetRetention")]
        public string PacketRetention { get => _packetRetention; set { _packetRetention = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PacketRetention))); } }
        
        [JsonProperty(PropertyName = "exportFormat")]
        public string ExportFormat { get => _exportFormat; set { _exportFormat = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExportFormat))); } }
        
        [JsonProperty(PropertyName = "compressExports")]
        public bool CompressExports { get => _compressExports; set { _compressExports = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompressExports))); } }
        
        [JsonProperty(PropertyName = "encryptExports")]
        public bool EncryptExports { get => _encryptExports; set { _encryptExports = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EncryptExports))); } }
        
        [JsonProperty(PropertyName = "exportPassword")]
        public string ExportPassword { get => _exportPassword; set { _exportPassword = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExportPassword))); } }
        
        [JsonProperty(PropertyName = "highlightNew")]
        public bool HighlightNew { get => _highlightNew; set { _highlightNew = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HighlightNew))); } }
        
        [JsonProperty(PropertyName = "autoScroll")]
        public bool AutoScroll { get => _autoScroll; set { _autoScroll = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoScroll))); } }
    }
    
    #endregion
    
    #region Chatbot Settings (14 options)
    
    public class ChatbotSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private bool _enabled = true;
        private string _welcomeMessage = "Hello! How can I help you today?";
        private bool _showTimestamps = true;
        private bool _showAvatars = true;
        private int _maxHistoryMessages = 100;
        private bool _saveHistory = true;
        private string _historyRetention = "7 days";
        private bool _suggestionsEnabled = true;
        private int _maxSuggestions = 5;
        private bool _codeHighlighting = true;
        private bool _markdownRendering = true;
        private bool _autoCopy = false;
        private bool _soundEnabled = false;
        private bool _notifyOnResponse = true;
        
        [JsonProperty(PropertyName = "enabled")]
        public bool Enabled { get => _enabled; set { _enabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled))); } }
        
        [JsonProperty(PropertyName = "welcomeMessage")]
        public string WelcomeMessage { get => _welcomeMessage; set { _welcomeMessage = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WelcomeMessage))); } }
        
        [JsonProperty(PropertyName = "showTimestamps")]
        public bool ShowTimestamps { get => _showTimestamps; set { _showTimestamps = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowTimestamps))); } }
        
        [JsonProperty(PropertyName = "showAvatars")]
        public bool ShowAvatars { get => _showAvatars; set { _showAvatars = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowAvatars))); } }
        
        [JsonProperty(PropertyName = "maxHistoryMessages")]
        public int MaxHistoryMessages { get => _maxHistoryMessages; set { _maxHistoryMessages = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxHistoryMessages))); } }
        
        [JsonProperty(PropertyName = "saveHistory")]
        public bool SaveHistory { get => _saveHistory; set { _saveHistory = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SaveHistory))); } }
        
        [JsonProperty(PropertyName = "historyRetention")]
        public string HistoryRetention { get => _historyRetention; set { _historyRetention = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HistoryRetention))); } }
        
        [JsonProperty(PropertyName = "suggestionsEnabled")]
        public bool SuggestionsEnabled { get => _suggestionsEnabled; set { _suggestionsEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SuggestionsEnabled))); } }
        
        [JsonProperty(PropertyName = "maxSuggestions")]
        public int MaxSuggestions { get => _maxSuggestions; set { _maxSuggestions = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxSuggestions))); } }
        
        [JsonProperty(PropertyName = "codeHighlighting")]
        public bool CodeHighlighting { get => _codeHighlighting; set { _codeHighlighting = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CodeHighlighting))); } }
        
        [JsonProperty(PropertyName = "markdownRendering")]
        public bool MarkdownRendering { get => _markdownRendering; set { _markdownRendering = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MarkdownRendering))); } }
        
        [JsonProperty(PropertyName = "autoCopy")]
        public bool AutoCopy { get => _autoCopy; set { _autoCopy = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoCopy))); } }
        
        [JsonProperty(PropertyName = "soundEnabled")]
        public bool SoundEnabled { get => _soundEnabled; set { _soundEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SoundEnabled))); } }
        
        [JsonProperty(PropertyName = "notifyOnResponse")]
        public bool NotifyOnResponse { get => _notifyOnResponse; set { _notifyOnResponse = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NotifyOnResponse))); } }
    }
    
    #endregion
    
    #region Status Bar Settings (18 options)
    
    public class StatusBarSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private bool _enabled = true;
        private string _position = "Bottom";
        private int _height = 32;
        private int _widgetSpacing = 8;
        private bool _showRagProgress = true;
        private bool _showRteProgress = true;
        private bool _showLlmTokens = true;
        private bool _showLlmVram = true;
        private bool _showGpuTemp = true;
        private bool _showPacketsPerSec = true;
        private bool _showDecodeRate = true;
        private bool _showFiddlerStatus = true;
        private bool _showResourceBars = true;
        private bool _showTroopCount = false;
        private bool _showMarchCount = true;
        private bool _showQueueStatus = true;
        private bool _showClock = true;
        private bool _showUptime = false;
        private bool _showLlmStats = true;
        
        [JsonProperty(PropertyName = "enabled")]
        public bool Enabled { get => _enabled; set { _enabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled))); } }
        
        [JsonProperty(PropertyName = "position")]
        public string Position { get => _position; set { _position = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Position))); } }
        
        [JsonProperty(PropertyName = "height")]
        public int Height { get => _height; set { _height = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Height))); } }
        
        [JsonProperty(PropertyName = "widgetSpacing")]
        public int WidgetSpacing { get => _widgetSpacing; set { _widgetSpacing = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WidgetSpacing))); } }
        
        [JsonProperty(PropertyName = "showRagProgress")]
        public bool ShowRagProgress { get => _showRagProgress; set { _showRagProgress = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowRagProgress))); } }
        
        [JsonProperty(PropertyName = "showRteProgress")]
        public bool ShowRteProgress { get => _showRteProgress; set { _showRteProgress = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowRteProgress))); } }
        
        [JsonProperty(PropertyName = "showLlmTokens")]
        public bool ShowLlmTokens { get => _showLlmTokens; set { _showLlmTokens = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowLlmTokens))); } }
        
        [JsonProperty(PropertyName = "showLlmVram")]
        public bool ShowLlmVram { get => _showLlmVram; set { _showLlmVram = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowLlmVram))); } }
        
        [JsonProperty(PropertyName = "showGpuTemp")]
        public bool ShowGpuTemp { get => _showGpuTemp; set { _showGpuTemp = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowGpuTemp))); } }
        
        [JsonProperty(PropertyName = "showPacketsPerSec")]
        public bool ShowPacketsPerSec { get => _showPacketsPerSec; set { _showPacketsPerSec = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowPacketsPerSec))); } }
        
        [JsonProperty(PropertyName = "showDecodeRate")]
        public bool ShowDecodeRate { get => _showDecodeRate; set { _showDecodeRate = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowDecodeRate))); } }
        
        [JsonProperty(PropertyName = "showFiddlerStatus")]
        public bool ShowFiddlerStatus { get => _showFiddlerStatus; set { _showFiddlerStatus = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowFiddlerStatus))); } }
        
        [JsonProperty(PropertyName = "showResourceBars")]
        public bool ShowResourceBars { get => _showResourceBars; set { _showResourceBars = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowResourceBars))); } }
        
        [JsonProperty(PropertyName = "showTroopCount")]
        public bool ShowTroopCount { get => _showTroopCount; set { _showTroopCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowTroopCount))); } }
        
        [JsonProperty(PropertyName = "showMarchCount")]
        public bool ShowMarchCount { get => _showMarchCount; set { _showMarchCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowMarchCount))); } }
        
        [JsonProperty(PropertyName = "showQueueStatus")]
        public bool ShowQueueStatus { get => _showQueueStatus; set { _showQueueStatus = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowQueueStatus))); } }
        
        [JsonProperty(PropertyName = "showClock")]
        public bool ShowClock { get => _showClock; set { _showClock = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowClock))); } }
        
        [JsonProperty(PropertyName = "showUptime")]
        public bool ShowUptime { get => _showUptime; set { _showUptime = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowUptime))); } }
        
        [JsonProperty(PropertyName = "showLlmStats")]
        public bool ShowLlmStats { get => _showLlmStats; set { _showLlmStats = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowLlmStats))); } }
    }
    
    #endregion
    
    #region Webhook Settings (12 options)
    
    public class WebhookSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private bool _enabled = false;
        private string _discordWebhook = "";
        private string _telegramBotToken = "";
        private string _telegramChatId = "";
        private string _slackWebhook = "";
        private string _teamsWebhook = "";
        private bool _notifyOnAttack = true;
        private bool _notifyOnScout = true;
        private bool _notifyOnRally = true;
        private bool _notifyOnComplete = true;
        private bool _notifyOnError = true;
        private int _webhookCooldown = 60;
        
        [JsonProperty(PropertyName = "enabled")]
        public bool Enabled { get => _enabled; set { _enabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled))); } }
        
        [JsonProperty(PropertyName = "discordWebhook")]
        public string DiscordWebhook { get => _discordWebhook; set { _discordWebhook = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DiscordWebhook))); } }
        
        [JsonProperty(PropertyName = "telegramBotToken")]
        public string TelegramBotToken { get => _telegramBotToken; set { _telegramBotToken = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TelegramBotToken))); } }
        
        [JsonProperty(PropertyName = "telegramChatId")]
        public string TelegramChatId { get => _telegramChatId; set { _telegramChatId = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TelegramChatId))); } }
        
        [JsonProperty(PropertyName = "slackWebhook")]
        public string SlackWebhook { get => _slackWebhook; set { _slackWebhook = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SlackWebhook))); } }
        
        [JsonProperty(PropertyName = "teamsWebhook")]
        public string TeamsWebhook { get => _teamsWebhook; set { _teamsWebhook = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TeamsWebhook))); } }
        
        [JsonProperty(PropertyName = "notifyOnAttack")]
        public bool NotifyOnAttack { get => _notifyOnAttack; set { _notifyOnAttack = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NotifyOnAttack))); } }
        
        [JsonProperty(PropertyName = "notifyOnScout")]
        public bool NotifyOnScout { get => _notifyOnScout; set { _notifyOnScout = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NotifyOnScout))); } }
        
        [JsonProperty(PropertyName = "notifyOnRally")]
        public bool NotifyOnRally { get => _notifyOnRally; set { _notifyOnRally = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NotifyOnRally))); } }
        
        [JsonProperty(PropertyName = "notifyOnComplete")]
        public bool NotifyOnComplete { get => _notifyOnComplete; set { _notifyOnComplete = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NotifyOnComplete))); } }
        
        [JsonProperty(PropertyName = "notifyOnError")]
        public bool NotifyOnError { get => _notifyOnError; set { _notifyOnError = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NotifyOnError))); } }
        
        [JsonProperty(PropertyName = "webhookCooldown")]
        public int WebhookCooldown { get => _webhookCooldown; set { _webhookCooldown = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WebhookCooldown))); } }
    }
    
    #endregion
    
    #region Advanced Settings (15 options)
    
    public class AdvancedSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private bool _debugMode = false;
        private bool _verboseLogging = false;
        private bool _performanceMode = false;
        private bool _lowMemoryMode = false;
        private int _gcInterval = 60;
        private int _maxMemoryMB = 2048;
        private int _threadPoolSize = 0;
        private bool _enableProfiling = false;
        private int _profileInterval = 60;
        
        [JsonProperty(PropertyName = "debugMode")]
        public bool DebugMode { get => _debugMode; set { _debugMode = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DebugMode))); } }
        
        [JsonProperty(PropertyName = "verboseLogging")]
        public bool VerboseLogging { get => _verboseLogging; set { _verboseLogging = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VerboseLogging))); } }
        
        [JsonProperty(PropertyName = "performanceMode")]
        public bool PerformanceMode { get => _performanceMode; set { _performanceMode = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PerformanceMode))); } }
        
        [JsonProperty(PropertyName = "lowMemoryMode")]
        public bool LowMemoryMode { get => _lowMemoryMode; set { _lowMemoryMode = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LowMemoryMode))); } }
        
        [JsonProperty(PropertyName = "gcInterval")]
        public int GcInterval { get => _gcInterval; set { _gcInterval = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GcInterval))); } }
        
        [JsonProperty(PropertyName = "maxMemoryMB")]
        public int MaxMemoryMB { get => _maxMemoryMB; set { _maxMemoryMB = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxMemoryMB))); } }
        
        [JsonProperty(PropertyName = "threadPoolSize")]
        public int ThreadPoolSize { get => _threadPoolSize; set { _threadPoolSize = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ThreadPoolSize))); } }
        
        [JsonProperty(PropertyName = "enableProfiling")]
        public bool EnableProfiling { get => _enableProfiling; set { _enableProfiling = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableProfiling))); } }
        
        [JsonProperty(PropertyName = "profileInterval")]
        public int ProfileInterval { get => _profileInterval; set { _profileInterval = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfileInterval))); } }
    }
    
    #endregion
}
