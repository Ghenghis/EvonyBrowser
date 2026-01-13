using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Thread-safe singleton that manages shared session state between browser panels.
    /// The key architectural insight is that both CefSharp browsers share the same CachePath,
    /// which means SOL files and cookies are automatically shared for session synchronization.
    /// </summary>
    public sealed class SessionManager : IDisposable
    {
        #region Singleton

        private static readonly Lazy<SessionManager> _lazyInstance = 
            new Lazy<SessionManager>(() => new SessionManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of SessionManager.
        /// </summary>
        public static SessionManager Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly object _sessionLock = new object();
        private readonly object _settingsLock = new object();
    
        // Session state (protected by _sessionLock)
        private bool _isSessionActive;
        private string _currentServer;
        private string _sessionToken;
        private string _playerName;
        private DateTime? _loginTime;

        #endregion

        #region Properties - Paths

        /// <summary>
        /// Path to the shared browser cache directory.
        /// </summary>
        public string CachePath { get; }

        /// <summary>
        /// Path to Flash SOL files (shared objects).
        /// </summary>
        public string SolPath { get; }

        /// <summary>
        /// Path to configuration files.
        /// </summary>
        public string ConfigPath { get; }

        #endregion

        #region Properties - Session State

        /// <summary>
        /// Gets whether an active session exists.
        /// </summary>
        public bool IsSessionActive
        {
            get { lock (_sessionLock) { return _isSessionActive; } }
        }

        /// <summary>
        /// Gets the current server hostname.
        /// </summary>
        public string CurrentServer
        {
            get { lock (_sessionLock) { return _currentServer; } }
        }

        /// <summary>
        /// Gets the current session token (if any).
        /// </summary>
        public string SessionToken
        {
            get { lock (_sessionLock) { return _sessionToken; } }
        }

        /// <summary>
        /// Gets the current player name (if logged in).
        /// </summary>
        public string PlayerName
        {
            get { lock (_sessionLock) { return _playerName; } }
        }

        /// <summary>
        /// Gets the login timestamp (if logged in).
        /// </summary>
        public DateTime? LoginTime
        {
            get { lock (_sessionLock) { return _loginTime; } }
        }

        /// <summary>
        /// Gets the application settings.
        /// </summary>
        public SvonySettings Settings { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Fired when session state changes. Parameter is true if session is active.
        /// </summary>
        public event Action<bool> SessionStateChanged;

        /// <summary>
        /// Fired when server is changed. Parameter is the new server hostname.
        /// </summary>
        public event Action<string> ServerChanged;

        #endregion

        #region Constructor

        private SessionManager()
        {
            // Initialize paths
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            CachePath = Path.Combine(baseDir, "Cache");
            SolPath = Path.Combine(CachePath, "Pepper Data", "Shockwave Flash");
            ConfigPath = Path.Combine(baseDir, "config");

            // Ensure directories exist
            EnsureDirectoriesExist();

            // Load settings
            Settings = LoadSettingsInternal();
            _currentServer = Settings.DefaultServer;

            App.Logger.Information("SessionManager initialized");
            App.Logger.Debug("  CachePath: {Path}", CachePath);
            App.Logger.Debug("  SolPath: {Path}", SolPath);
            App.Logger.Debug("  ConfigPath: {Path}", ConfigPath);
        }

        private void EnsureDirectoriesExist()
        {
            try
            {
                Directory.CreateDirectory(CachePath);
                Directory.CreateDirectory(SolPath);
                Directory.CreateDirectory(ConfigPath);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to create session directories");
            }
        }

        #endregion

        #region Public Methods - Session Management

        /// <summary>
        /// Sets the current server.
        /// </summary>
        /// <param name="server">Server hostname (e.g., "cc2.evony.com")</param>
        public void SetServer(string server)
        {
            if (string.IsNullOrWhiteSpace(server))
            {
                throw new ArgumentException("Server cannot be null or empty", nameof(server));
            }

            lock (_sessionLock)
            {
                _currentServer = server;
            }

            App.Logger.Information("Server set to: {Server}", server);
        
            // Fire event outside lock to prevent deadlocks
            try
            {
                ServerChanged?.Invoke(server);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error in ServerChanged event handler");
            }
        }

        /// <summary>
        /// Called when a login is detected in either browser panel.
        /// </summary>
        /// <param name="token">Session token</param>
        /// <param name="playerName">Optional player name</param>
        public void OnLoginDetected(string token, string playerName = null)
        {
            lock (_sessionLock)
            {
                _sessionToken = token;
                _playerName = playerName;
                _loginTime = DateTime.UtcNow;
                _isSessionActive = true;
            }

            App.Logger.Information("Login detected - Player: {Player}, Time: {Time}", 
                playerName ?? "Unknown", DateTime.UtcNow);

            try
            {
                SessionStateChanged?.Invoke(true);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error in SessionStateChanged event handler");
            }
        }

        /// <summary>
        /// Called when a logout is detected or session is cleared.
        /// </summary>
        public void OnLogoutDetected()
        {
            lock (_sessionLock)
            {
                _sessionToken = null;
                _playerName = null;
                _loginTime = null;
                _isSessionActive = false;
            }

            App.Logger.Information("Logout detected");

            try
            {
                SessionStateChanged?.Invoke(false);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error in SessionStateChanged event handler");
            }
        }

        /// <summary>
        /// Exports current session information to a JSON file.
        /// </summary>
        /// <param name="filePath">Path to export file</param>
        public void ExportSession(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }

            object sessionData;
            lock (_sessionLock)
            {
                sessionData = new
                {
                    Server = _currentServer,
                    Player = _playerName,
                    LoginTime = _loginTime,
                    ExportTime = DateTime.UtcNow,
                    CachePath,
                    IsActive = _isSessionActive
                };
            }

            try
            {
                var json = JsonConvert.SerializeObject(sessionData, Formatting.Indented);
                File.WriteAllText(filePath, json);
                App.Logger.Information("Session exported to: {Path}", filePath);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to export session to: {Path}", filePath);
                throw;
            }
        }

        #endregion

        #region Settings Management

        private SvonySettings LoadSettingsInternal()
        {
            var settingsFile = Path.Combine(ConfigPath, "svony-settings.json");

            if (!File.Exists(settingsFile))
            {
                App.Logger.Information("Settings file not found, using defaults");
                return new SvonySettings();
            }

            try
            {
                var json = File.ReadAllText(settingsFile);
                var settings = JsonConvert.DeserializeObject<SvonySettings>(json);
            
                if (settings != null)
                {
                    App.Logger.Information("Settings loaded from: {Path}", settingsFile);
                    return settings;
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to load settings from: {Path}", settingsFile);
            }

            return new SvonySettings();
        }

        /// <summary>
        /// Saves current settings to disk.
        /// </summary>
        public void SaveSettings()
        {
            var settingsFile = Path.Combine(ConfigPath, "svony-settings.json");

            lock (_settingsLock)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                
                    // Write to temp file first, then move (atomic operation)
                    var tempFile = settingsFile + ".tmp";
                    File.WriteAllText(tempFile, json);
                
                    if (File.Exists(settingsFile))
                    {
                        File.Delete(settingsFile);
                    }
                    File.Move(tempFile, settingsFile);
                
                    App.Logger.Information("Settings saved to: {Path}", settingsFile);
                }
                catch (Exception ex)
                {
                    App.Logger.Error(ex, "Failed to save settings");
                    throw;
                }
            }
        }

        /// <summary>
        /// Reloads settings from disk.
        /// </summary>
        public void ReloadSettings()
        {
            lock (_settingsLock)
            {
                Settings = LoadSettingsInternal();
            }
        }

        #endregion

        #region IDisposable

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }

        #endregion
    }

    /// <summary>
    /// Application settings stored in config/svony-settings.json
    /// </summary>
    public sealed class SvonySettings
    {
        /// <summary>
        /// Default server to connect to.
        /// </summary>
        public string DefaultServer { get; set; } = "cc2.evony.com";

        /// <summary>
        /// Proxy host address.
        /// </summary>
        public string ProxyHost { get; set; } = "127.0.0.1";

        /// <summary>
        /// Proxy port number.
        /// </summary>
        public int ProxyPort { get; set; } = 8888;

        /// <summary>
        /// Whether to automatically start Fiddler on launch.
        /// </summary>
        public bool AutoStartFiddler { get; set; } = true;

        /// <summary>
        /// Whether to remember panel layout between sessions.
        /// </summary>
        public bool RememberPanelLayout { get; set; } = true;

        /// <summary>
        /// Left panel width as a ratio (0.0-1.0).
        /// </summary>
        public double LeftPanelWidth { get; set; } = 0.5;

        /// <summary>
        /// Last used panel mode (Both, LeftOnly, RightOnly).
        /// </summary>
        public string LastPanelMode { get; set; } = "Both";

        /// <summary>
        /// Whether to enable verbose logging.
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// UI theme (Dark, Light).
        /// </summary>
        public string Theme { get; set; } = "Dark";

        /// <summary>
        /// Path to AutoEvony.swf file.
        /// </summary>
        public string AutoEvonySwfPath { get; set; } = "";

        /// <summary>
        /// Path to EvonyClient.swf file.
        /// </summary>
        public string EvonyClientSwfPath { get; set; } = "";

        /// <summary>
        /// Path to Flash plugin (pepflashplayer.dll).
        /// </summary>
        public string FlashPluginPath { get; set; } = "";
    }
}
