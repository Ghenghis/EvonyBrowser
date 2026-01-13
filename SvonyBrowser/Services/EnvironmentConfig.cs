using System;
using System.Collections.Generic;
using System.IO;

namespace SvonyBrowser.Services
{
    /// <summary>
    /// Manages environment variables and configuration for Svony Browser.
    /// Supports loading from .env files and system environment variables.
    /// </summary>
    public sealed class EnvironmentConfig : IDisposable
    {
        #region Singleton

        private static readonly Lazy<EnvironmentConfig> _lazyInstance =
            new Lazy<EnvironmentConfig>(() => new EnvironmentConfig(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static EnvironmentConfig Instance => _lazyInstance.Value;

        #endregion

        #region Constants

        /// <summary>Environment variable prefix for Svony Browser settings.</summary>
        public const string ENV_PREFIX = "SVONY_";

        /// <summary>Default .env file name.</summary>
        public const string DEFAULT_ENV_FILE = ".env";

        #endregion

        #region Properties

        /// <summary>Gets the application data path.</summary>
        public string AppDataPath { get; private set; }

        /// <summary>Gets the logs path.</summary>
        public string LogsPath { get; private set; }

        /// <summary>Gets the cache path.</summary>
        public string CachePath { get; private set; }

        /// <summary>Gets the config path.</summary>
        public string ConfigPath { get; private set; }

        /// <summary>Gets the plugins path.</summary>
        public string PluginsPath { get; private set; }

        /// <summary>Gets the assets path.</summary>
        public string AssetsPath { get; private set; }

        /// <summary>Gets whether debug mode is enabled.</summary>
        public bool IsDebugMode { get; private set; }

        /// <summary>Gets the log level.</summary>
        public string LogLevel { get; private set; }

        /// <summary>Gets the MCP server URL.</summary>
        public string McpServerUrl { get; private set; }

        /// <summary>Gets the LLM API endpoint.</summary>
        public string LlmApiEndpoint { get; private set; }

        /// <summary>Gets the LLM API key (if any).</summary>
        public string LlmApiKey { get; private set; }

        /// <summary>Gets the Fiddler proxy port.</summary>
        public int FiddlerProxyPort { get; private set; }

        /// <summary>Gets the Flash plugin path.</summary>
        public string FlashPluginPath { get; private set; }

        /// <summary>Gets the CefSharp path.</summary>
        public string CefSharpPath { get; private set; }

        #endregion

        #region Fields

        private readonly Dictionary<string, string> _envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private bool _initialized;

        #endregion

        #region Constructor

        private EnvironmentConfig()
        {
            Initialize();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the environment configuration.
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;

            // Set up default paths
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            AppDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SvonyBrowser");

            LogsPath = Path.Combine(AppDataPath, "Logs");
            CachePath = Path.Combine(AppDataPath, "Cache");
            ConfigPath = Path.Combine(AppDataPath, "config");
            PluginsPath = Path.Combine(baseDir, "Assets", "Plugins");
            AssetsPath = Path.Combine(baseDir, "Assets");

            // Create directories
            EnsureDirectoryExists(AppDataPath);
            EnsureDirectoryExists(LogsPath);
            EnsureDirectoryExists(CachePath);
            EnsureDirectoryExists(ConfigPath);

            // Load .env file if exists
            var envFilePath = Path.Combine(baseDir, DEFAULT_ENV_FILE);
            if (File.Exists(envFilePath))
            {
                LoadEnvFile(envFilePath);
            }

            // Load configuration from environment variables
            LoadEnvironmentVariables();

            _initialized = true;
            App.Logger?.Info("EnvironmentConfig initialized");
        }

        /// <summary>
        /// Gets an environment variable value.
        /// </summary>
        /// <param name="key">Variable name (without SVONY_ prefix).</param>
        /// <param name="defaultValue">Default value if not found.</param>
        /// <returns>The variable value or default.</returns>
        public string Get(string key, string defaultValue = null)
        {
            var fullKey = ENV_PREFIX + key.ToUpperInvariant();
            
            // Check local cache first
            if (_envVars.TryGetValue(fullKey, out var value))
                return value;

            // Check system environment
            value = Environment.GetEnvironmentVariable(fullKey);
            if (!string.IsNullOrEmpty(value))
            {
                _envVars[fullKey] = value;
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets an environment variable as integer.
        /// </summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            var value = Get(key);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Gets an environment variable as boolean.
        /// </summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            var value = Get(key);
            if (string.IsNullOrEmpty(value)) return defaultValue;
            return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("1", StringComparison.Ordinal) ||
                   value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sets an environment variable.
        /// </summary>
        public void Set(string key, string value)
        {
            var fullKey = ENV_PREFIX + key.ToUpperInvariant();
            _envVars[fullKey] = value;
            Environment.SetEnvironmentVariable(fullKey, value);
        }

        /// <summary>
        /// Loads environment variables from a .env file.
        /// </summary>
        public void LoadEnvFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                App.Logger?.Warn($"Env file not found: {filePath}");
                return;
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    
                    // Skip comments and empty lines
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                        continue;

                    var equalIndex = trimmed.IndexOf('=');
                    if (equalIndex <= 0) continue;

                    var key = trimmed.Substring(0, equalIndex).Trim();
                    var value = trimmed.Substring(equalIndex + 1).Trim();

                    // Remove quotes if present
                    if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                        (value.StartsWith("'") && value.EndsWith("'")))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    _envVars[key] = value;
                    Environment.SetEnvironmentVariable(key, value);
                }

                App.Logger?.Info($"Loaded {_envVars.Count} variables from {filePath}");
            }
            catch (Exception ex)
            {
                App.Logger?.Error($"Failed to load env file: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves current configuration to a .env file.
        /// </summary>
        public void SaveEnvFile(string filePath)
        {
            try
            {
                var lines = new List<string>
                {
                    "# Svony Browser Environment Configuration",
                    $"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    ""
                };

                foreach (var kvp in _envVars)
                {
                    if (kvp.Key.StartsWith(ENV_PREFIX))
                    {
                        var value = kvp.Value.Contains(" ") ? $"\"{kvp.Value}\"" : kvp.Value;
                        lines.Add($"{kvp.Key}={value}");
                    }
                }

                File.WriteAllLines(filePath, lines);
                App.Logger?.Info($"Saved env file: {filePath}");
            }
            catch (Exception ex)
            {
                App.Logger?.Error($"Failed to save env file: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all environment variables as a dictionary.
        /// </summary>
        public IReadOnlyDictionary<string, string> GetAll()
        {
            return _envVars;
        }

        /// <summary>
        /// Reloads configuration from environment.
        /// </summary>
        public void Reload()
        {
            _initialized = false;
            _envVars.Clear();
            Initialize();
        }

        #endregion

        #region Private Methods

        private void LoadEnvironmentVariables()
        {
            // Debug mode
            IsDebugMode = GetBool("DEBUG", false);

            // Log level
            LogLevel = Get("LOG_LEVEL", "Info");

            // MCP configuration
            McpServerUrl = Get("MCP_SERVER_URL", "http://localhost:3000");

            // LLM configuration
            LlmApiEndpoint = Get("LLM_API_ENDPOINT", "http://localhost:11434");
            LlmApiKey = Get("LLM_API_KEY", "");

            // Fiddler configuration
            FiddlerProxyPort = GetInt("FIDDLER_PORT", 8888);

            // Flash plugin path
            FlashPluginPath = Get("FLASH_PLUGIN_PATH", Path.Combine(PluginsPath, "pepflashplayer.dll"));

            // CefSharp path
            CefSharpPath = Get("CEFSHARP_PATH", Path.Combine(AssetsPath, "CefSharp"));
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception ex)
                {
                    App.Logger?.Error($"Failed to create directory {path}: {ex.Message}");
                }
            }
        }

        #endregion

        #region IDisposable

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _envVars.Clear();
            App.Logger?.Debug("EnvironmentConfig disposed");
        }

        #endregion
    }
}
