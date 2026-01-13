using System;
using System.IO;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;

namespace SvonyBrowser
{
    /// <summary>
    /// Application entry point for .NET Framework 4.6.2 with CefSharp 84 (Flash support).
    /// 
    /// Expected folder structure:
    /// [AppDirectory]/
    /// ├── SvonyBrowser.exe
    /// ├── Assets/
    /// │   ├── CefSharp/
    /// │   │   ├── libcef.dll
    /// │   │   ├── CefSharp.dll
    /// │   │   ├── CefSharp.Core.dll
    /// │   │   ├── CefSharp.BrowserSubprocess.exe
    /// │   │   ├── icudtl.dat
    /// │   │   ├── locales/
    /// │   │   └── ...
    /// │   └── Plugins/
    /// │       └── pepflashplayer.dll
    /// ├── Cache/
    /// ├── Logs/
    /// ├── config/
    /// └── data/
    /// </summary>
    public static class Program
    {
        // Asset paths relative to application directory
        private const string ASSETS_FOLDER = "Assets";
        private const string CEFSHARP_FOLDER = "Assets\\CefSharp";
        private const string PLUGINS_FOLDER = "Assets\\Plugins";
        private const string FLASH_PLUGIN = "pepflashplayer.dll";
        private const string FLASH_VERSION = "32.0.0.465";

        [STAThread]
        public static int Main(string[] args)
        {
            // Base directory is where the executable runs from
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Define all paths
            var assetsPath = Path.Combine(baseDir, ASSETS_FOLDER);
            var cefSharpPath = Path.Combine(baseDir, CEFSHARP_FOLDER);
            var pluginsPath = Path.Combine(baseDir, PLUGINS_FOLDER);
            var cachePath = Path.Combine(baseDir, "Cache");
            var logPath = Path.Combine(baseDir, "Logs");
            var configPath = Path.Combine(baseDir, "config");
            var mcpDataPath = Path.Combine(baseDir, "mcp-data");
            var dataPath = Path.Combine(baseDir, "data");

            // Ensure directories exist
            try
            {
                Directory.CreateDirectory(cachePath);
                Directory.CreateDirectory(logPath);
                Directory.CreateDirectory(configPath);
                Directory.CreateDirectory(mcpDataPath);
                Directory.CreateDirectory(dataPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Failed to create application directories:\n\n{0}", ex.Message),
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            // Write startup log
            LogMessage(logPath, string.Format(
                "\n═══════════════════════════════════════════════════════════\n" +
                "Svony Browser v7.0.5 (Flash Edition) starting...\n" +
                "═══════════════════════════════════════════════════════════\n" +
                "Base directory: {0}\n" +
                "Assets path: {1}\n" +
                "CefSharp path: {2}\n" +
                "Plugins path: {3}\n" +
                "Cache path: {4}",
                baseDir, assetsPath, cefSharpPath, pluginsPath, cachePath));

            // Validate required assets exist
            if (!ValidateAssets(baseDir, logPath))
            {
                MessageBox.Show(
                    "Required assets are missing!\n\n" +
                    "Please ensure the Assets folder is in the same directory as the executable:\n" +
                    "- Assets/CefSharp/ (CefSharp runtime files)\n" +
                    "- Assets/Plugins/pepflashplayer.dll (Flash plugin)\n\n" +
                    "The application will now exit.",
                    "Missing Assets",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return 1;
            }

            // Initialize CefSharp with Flash support
            try
            {
                if (!Cef.IsInitialized)
                {
                    // Set the path to CefSharp subprocess
                    var browserSubprocessPath = Path.Combine(cefSharpPath, "CefSharp.BrowserSubprocess.exe");
                    
                    var settings = new CefSettings
                    {
                        CachePath = cachePath,
                        PersistSessionCookies = true,
                        PersistUserPreferences = true,
                        WindowlessRenderingEnabled = false,
                        LogSeverity = LogSeverity.Warning,
                        LogFile = Path.Combine(logPath, "cef.log"),
                        BrowserSubprocessPath = browserSubprocessPath,
                        ResourcesDirPath = cefSharpPath,
                        LocalesDirPath = Path.Combine(cefSharpPath, "locales")
                    };

                    // Configure Flash plugin
                    var flashPath = Path.Combine(pluginsPath, FLASH_PLUGIN);
                    if (File.Exists(flashPath))
                    {
                        settings.CefCommandLineArgs.Add("ppapi-flash-path", flashPath);
                        settings.CefCommandLineArgs.Add("ppapi-flash-version", FLASH_VERSION);
                        settings.CefCommandLineArgs.Add("allow-outdated-plugins", "1");
                        settings.CefCommandLineArgs.Add("enable-npapi", "1");
                        settings.CefCommandLineArgs.Add("enable-system-flash", "1");
                        
                        LogMessage(logPath, string.Format("Flash plugin configured: {0}", flashPath));
                    }
                    else
                    {
                        LogMessage(logPath, string.Format("WARNING: Flash plugin not found at: {0}", flashPath));
                    }

                    // GPU and rendering settings for stability
                    settings.CefCommandLineArgs.Add("disable-gpu-compositing", "1");
                    settings.CefCommandLineArgs.Add("disable-gpu-vsync", "1");
                    settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1");
                    
                    // Disable features that may cause issues
                    settings.CefCommandLineArgs.Add("disable-extensions", "0");

                    LogMessage(logPath, "Initializing CefSharp 84...");
                    var success = Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

                    if (!success)
                    {
                        throw new InvalidOperationException("Cef.Initialize returned false");
                    }

                    LogMessage(logPath, "CefSharp 84 initialized successfully with Flash support!");
                }
            }
            catch (Exception ex)
            {
                LogMessage(logPath, string.Format("FATAL: Failed to initialize CefSharp: {0}\n{1}", ex.Message, ex.StackTrace));
                MessageBox.Show(
                    string.Format("Failed to initialize browser engine:\n\n{0}\n\nThe application will now exit.", ex.Message),
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return 1;
            }

            // Start the WPF application
            try
            {
                var app = new App();
                app.InitializeComponent();

                // Set static properties for App - these are used throughout the application
                App.BasePath = baseDir;
                App.CachePath = cachePath;
                App.LogPath = logPath;
                App.ConfigPath = configPath;
                App.McpDataPath = mcpDataPath;
                App.DataPath = dataPath;

                var result = app.Run();

                // Cleanup
                LogMessage(logPath, "Application shutting down...");
                if (Cef.IsInitialized)
                {
                    Cef.Shutdown();
                }
                LogMessage(logPath, "Shutdown complete");

                return result;
            }
            catch (Exception ex)
            {
                LogMessage(logPath, string.Format("FATAL: Application crashed: {0}\n{1}", ex.Message, ex.StackTrace));
                MessageBox.Show(
                    string.Format("Application error:\n\n{0}", ex.Message),
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return 1;
            }
        }

        /// <summary>
        /// Validates that all required assets are present.
        /// </summary>
        private static bool ValidateAssets(string baseDir, string logPath)
        {
            var assetsPath = Path.Combine(baseDir, ASSETS_FOLDER);
            var cefSharpPath = Path.Combine(baseDir, CEFSHARP_FOLDER);
            var pluginsPath = Path.Combine(baseDir, PLUGINS_FOLDER);

            // Check Assets folder exists
            if (!Directory.Exists(assetsPath))
            {
                LogMessage(logPath, string.Format("ERROR: Assets folder not found: {0}", assetsPath));
                return false;
            }

            // Check CefSharp folder exists
            if (!Directory.Exists(cefSharpPath))
            {
                LogMessage(logPath, string.Format("ERROR: CefSharp folder not found: {0}", cefSharpPath));
                return false;
            }

            // Check critical CefSharp files
            var criticalFiles = new[]
            {
                Path.Combine(cefSharpPath, "libcef.dll"),
                Path.Combine(cefSharpPath, "CefSharp.dll"),
                Path.Combine(cefSharpPath, "CefSharp.Core.dll"),
                Path.Combine(cefSharpPath, "CefSharp.BrowserSubprocess.exe"),
                Path.Combine(cefSharpPath, "icudtl.dat")
            };

            foreach (var file in criticalFiles)
            {
                if (!File.Exists(file))
                {
                    LogMessage(logPath, string.Format("ERROR: Critical file missing: {0}", file));
                    return false;
                }
            }

            // Check Flash plugin (warning only, not critical)
            var flashPath = Path.Combine(pluginsPath, FLASH_PLUGIN);
            if (!File.Exists(flashPath))
            {
                LogMessage(logPath, string.Format("WARNING: Flash plugin not found: {0}", flashPath));
                LogMessage(logPath, "Flash/SWF content will not work without pepflashplayer.dll");
            }

            LogMessage(logPath, "Asset validation passed");
            return true;
        }

        /// <summary>
        /// Writes a message to the log file.
        /// </summary>
        private static void LogMessage(string logPath, string message)
        {
            try
            {
                var logFile = Path.Combine(logPath, string.Format("svony-{0:yyyy-MM-dd}.log", DateTime.Now));
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                File.AppendAllText(logFile, string.Format("[{0}] {1}\n", timestamp, message));
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
}
