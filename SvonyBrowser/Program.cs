using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;
using SvonyBrowser.Helpers;
using SvonyBrowser.Models;

namespace SvonyBrowser
{
    /// <summary>
    /// Application entry point for .NET Framework 4.6.2 with CefSharp 84 (Flash support).
    /// Uses CefFlashBrowser-style asset loading for proper SWF support.
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
    /// │   ├── Plugins/
    /// │   │   └── pepflashplayer.dll
    /// │   ├── SwfPlayer/
    /// │   │   └── swfplayer.html
    /// │   └── EmptyExe/
    /// ├── Cache/
    /// ├── Logs/
    /// ├── config/
    /// └── data/
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            // CRITICAL: Set DLL directory BEFORE any CefSharp references
            // This allows loading CefSharp DLLs from Assets\CefSharp folder
            Win32.SetDllDirectory(GlobalData.CefDllPath);
            
            // Register assembly resolver for CefSharp DLLs
            AppDomain.CurrentDomain.AssemblyResolve += ResolveCefSharpAssembly;

            // Ensure directories exist
            GlobalData.EnsureDirectoriesExist();

            // Write startup log
            GlobalData.LogMessage(string.Format(
                "\n═══════════════════════════════════════════════════════════\n" +
                "Svony Browser v7.0.5 (Flash Edition) starting...\n" +
                "═══════════════════════════════════════════════════════════\n" +
                "Base directory: {0}\n" +
                "Assets path: {1}\n" +
                "CefSharp path: {2}\n" +
                "Plugins path: {3}\n" +
                "Cache path: {4}",
                GlobalData.AppBaseDirectory, GlobalData.AssetsPath, 
                GlobalData.CefDllPath, GlobalData.PluginsPath, GlobalData.CachePath));

            // Validate required assets exist
            string errorMessage;
            if (!GlobalData.ValidateAssets(out errorMessage))
            {
                GlobalData.LogMessage("ERROR: " + errorMessage);
                MessageBox.Show(
                    "Required assets are missing!\n\n" + errorMessage + "\n\n" +
                    "Please ensure the Assets folder is in the same directory as the executable:\n" +
                    "- Assets/CefSharp/ (CefSharp runtime files including libcef.dll)\n" +
                    "- Assets/Plugins/pepflashplayer.dll (Flash plugin)\n" +
                    "- Assets/SwfPlayer/swfplayer.html (SWF player page)\n\n" +
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
                    // Remove black popup window by setting ComSpec to empty exe
                    if (File.Exists(GlobalData.EmptyExePath))
                    {
                        Environment.SetEnvironmentVariable("ComSpec", GlobalData.EmptyExePath);
                    }

                    var settings = new CefSharp.Wpf.CefSettings
                    {
                        CachePath = GlobalData.CachePath,
                        PersistSessionCookies = true,
                        PersistUserPreferences = true,
                        LogSeverity = LogSeverity.Warning,
                        LogFile = GlobalData.CefLogPath,
                        BrowserSubprocessPath = GlobalData.SubprocessPath,
                        LocalesDirPath = GlobalData.LocalesPath,
                        ResourcesDirPath = GlobalData.CefDllPath
                    };

                    // Configure Flash plugin (PPAPI)
                    if (File.Exists(GlobalData.FlashPath))
                    {
                        var flashVersion = GlobalData.GetFlashVersion();
                        settings.CefCommandLineArgs.Add("ppapi-flash-path", GlobalData.FlashPath);
                        settings.CefCommandLineArgs.Add("ppapi-flash-version", flashVersion);
                        settings.CefCommandLineArgs.Add("enable-system-flash", "1");
                        
                        GlobalData.LogMessage(string.Format("Flash plugin configured: {0} (version {1})", 
                            GlobalData.FlashPath, flashVersion));
                    }
                    else
                    {
                        GlobalData.LogMessage(string.Format("WARNING: Flash plugin not found at: {0}", GlobalData.FlashPath));
                    }

                    // GPU and rendering settings for stability
                    settings.CefCommandLineArgs.Add("disable-gpu-compositing", "1");
                    settings.CefCommandLineArgs.Add("disable-gpu-vsync", "1");
                    settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1");
                    
                    // Allow autoplay without user gesture (for Flash content)
                    settings.CefCommandLineArgs.Add("autoplay-policy", "no-user-gesture-required");
                    
                    // Disable features that may cause issues
                    settings.CefCommandLineArgs.Add("disable-extensions", "0");

                    GlobalData.LogMessage("Initializing CefSharp 84...");
                    Cef.Initialize(settings);

                    if (!Cef.IsInitialized)
                    {
                        throw new InvalidOperationException("Cef.Initialize failed");
                    }

                    GlobalData.LogMessage("CefSharp 84 initialized successfully with Flash support!");
                }
            }
            catch (Exception ex)
            {
                GlobalData.LogMessage(string.Format("FATAL: Failed to initialize CefSharp: {0}\n{1}", ex.Message, ex.StackTrace));
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
                App.BasePath = GlobalData.AppBaseDirectory;
                App.CachePath = GlobalData.CachePath;
                App.LogPath = GlobalData.LogsPath;
                App.ConfigPath = GlobalData.ConfigPath;
                App.McpDataPath = GlobalData.McpDataPath;
                App.DataPath = GlobalData.DataPath;

                GlobalData.LogMessage(string.Format("Application started successfully, pid: {0}", Process.GetCurrentProcess().Id));

                var result = app.Run();

                // Cleanup
                GlobalData.LogMessage("Application shutting down...");
                if (Cef.IsInitialized)
                {
                    Cef.Shutdown();
                }
                GlobalData.LogMessage("Shutdown complete");

                return result;
            }
            catch (Exception ex)
            {
                GlobalData.LogMessage(string.Format("FATAL: Application crashed: {0}\n{1}", ex.Message, ex.StackTrace));
                MessageBox.Show(
                    string.Format("Application error:\n\n{0}", ex.Message),
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return 1;
            }
        }

        /// <summary>
        /// Resolves CefSharp assemblies from the Assets\CefSharp folder.
        /// This is critical for loading CefSharp DLLs from a non-standard location.
        /// </summary>
        private static Assembly ResolveCefSharpAssembly(object sender, ResolveEventArgs e)
        {
            string assemblyName = new AssemblyName(e.Name).Name;
            string assemblyPath = Path.Combine(GlobalData.CefDllPath, assemblyName + ".dll");
            
            if (File.Exists(assemblyPath))
            {
                GlobalData.LogMessage(string.Format("Resolved assembly: {0} from {1}", assemblyName, assemblyPath));
                return Assembly.LoadFrom(assemblyPath);
            }
            
            return null;
        }
    }
}
