using System;
using System.IO;

namespace SvonyBrowser.Models
{
    /// <summary>
    /// Centralized path management for all application assets.
    /// Follows the CefFlashBrowser pattern for proper Flash/SWF support.
    /// </summary>
    public static class GlobalData
    {
        // Base paths
        public static string AppBaseDirectory { get; }
        public static string AssetsPath { get; }
        public static string CachePath { get; }
        public static string LogsPath { get; }
        public static string ConfigPath { get; }
        public static string DataPath { get; }
        public static string McpDataPath { get; }

        // CefSharp paths
        public static string CefDllPath { get; }
        public static string PluginsPath { get; }
        public static string LocalesPath { get; }

        // Critical file paths
        public static string FlashPath { get; }
        public static string SwfPlayerPath { get; }
        public static string SubprocessPath { get; }
        public static string EmptyExePath { get; }
        public static string CefLogPath { get; }
        public static string AppLogPath { get; }

        // Flash version
        public const string FLASH_VERSION = "32.0.0.465";

        static GlobalData()
        {
            // Base directory is where the executable runs from
            AppBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Asset paths
            AssetsPath = Path.Combine(AppBaseDirectory, "Assets");
            CachePath = Path.Combine(AppBaseDirectory, "Cache");
            LogsPath = Path.Combine(AppBaseDirectory, "Logs");
            ConfigPath = Path.Combine(AppBaseDirectory, "config");
            DataPath = Path.Combine(AppBaseDirectory, "data");
            McpDataPath = Path.Combine(AppBaseDirectory, "mcp-data");

            // CefSharp paths - critical for DLL loading
            CefDllPath = Path.Combine(AssetsPath, "CefSharp");
            PluginsPath = Path.Combine(AssetsPath, "Plugins");
            LocalesPath = Path.Combine(CefDllPath, "locales");

            // Critical file paths
            FlashPath = Path.Combine(PluginsPath, "pepflashplayer.dll");
            SwfPlayerPath = Path.Combine(AssetsPath, "SwfPlayer", "swfplayer.html");
            SubprocessPath = Path.Combine(CefDllPath, "CefSharp.BrowserSubprocess.exe");
            EmptyExePath = Path.Combine(AssetsPath, "EmptyExe", "SvonyBrowser.EmptyExe.exe");

            // Log paths
            CefLogPath = Path.Combine(LogsPath, string.Format("cef_{0:yyyyMMdd}.log", DateTime.Now));
            AppLogPath = Path.Combine(LogsPath, string.Format("svony_{0:yyyyMMdd}.log", DateTime.Now));
        }

        /// <summary>
        /// Creates required directories if they don't exist.
        /// </summary>
        public static void EnsureDirectoriesExist()
        {
            CreateDirIfNotExist(CachePath);
            CreateDirIfNotExist(LogsPath);
            CreateDirIfNotExist(ConfigPath);
            CreateDirIfNotExist(DataPath);
            CreateDirIfNotExist(McpDataPath);
        }

        private static void CreateDirIfNotExist(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Validates that all required CefSharp assets are present.
        /// </summary>
        /// <returns>True if all critical files exist.</returns>
        public static bool ValidateAssets(out string errorMessage)
        {
            errorMessage = null;

            // Check Assets folder exists
            if (!Directory.Exists(AssetsPath))
            {
                errorMessage = string.Format("Assets folder not found: {0}", AssetsPath);
                return false;
            }

            // Check CefSharp folder exists
            if (!Directory.Exists(CefDllPath))
            {
                errorMessage = string.Format("CefSharp folder not found: {0}", CefDllPath);
                return false;
            }

            // Check critical CefSharp files
            var criticalFiles = new[]
            {
                new { Path = Path.Combine(CefDllPath, "libcef.dll"), Name = "libcef.dll" },
                new { Path = Path.Combine(CefDllPath, "CefSharp.dll"), Name = "CefSharp.dll" },
                new { Path = Path.Combine(CefDllPath, "CefSharp.Core.dll"), Name = "CefSharp.Core.dll" },
                new { Path = SubprocessPath, Name = "CefSharp.BrowserSubprocess.exe" },
                new { Path = Path.Combine(CefDllPath, "icudtl.dat"), Name = "icudtl.dat" }
            };

            foreach (var file in criticalFiles)
            {
                if (!File.Exists(file.Path))
                {
                    errorMessage = string.Format("Critical file missing: {0}\nExpected at: {1}", file.Name, file.Path);
                    return false;
                }
            }

            // Check Flash plugin (warning only)
            if (!File.Exists(FlashPath))
            {
                // Not a fatal error, but log it
                LogWarning("Flash plugin not found: " + FlashPath);
            }

            // Check SwfPlayer HTML
            if (!File.Exists(SwfPlayerPath))
            {
                LogWarning("SwfPlayer HTML not found: " + SwfPlayerPath);
            }

            return true;
        }

        /// <summary>
        /// Gets the full path to a file in the Assets folder.
        /// </summary>
        public static string GetAssetPath(string relativePath)
        {
            return Path.Combine(AssetsPath, relativePath);
        }

        /// <summary>
        /// Gets the Flash player version from the DLL if it exists.
        /// </summary>
        public static string GetFlashVersion()
        {
            try
            {
                if (File.Exists(FlashPath))
                {
                    var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(FlashPath);
                    return versionInfo.FileVersion?.Replace(',', '.') ?? FLASH_VERSION;
                }
            }
            catch
            {
                // Fall back to default
            }
            return FLASH_VERSION;
        }

        private static void LogWarning(string message)
        {
            try
            {
                var logFile = AppLogPath;
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                File.AppendAllText(logFile, string.Format("[{0}] WARNING: {1}\n", timestamp, message));
            }
            catch
            {
                // Ignore logging errors
            }
        }

        /// <summary>
        /// Logs a message to the application log file.
        /// </summary>
        public static void LogMessage(string message)
        {
            try
            {
                var logFile = AppLogPath;
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
