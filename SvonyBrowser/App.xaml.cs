using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Serilog;
using SvonyBrowser.Services;

namespace SvonyBrowser
{
    /// <summary>
    /// Main application class.
    /// CefSharp initialization is handled in Program.cs with self-hosting pattern.
    /// </summary>
    public partial class App : Application
    {
        // Static properties set by Program.cs
        public static string BasePath { get; set; } = "";
        public static string CachePath { get; set; } = "";
        public static string LogPath { get; set; } = "";
        public static string ConfigPath { get; set; } = "";
        public static string McpDataPath { get; set; } = "";
        public static string DataPath { get; set; } = "";
        public static ILogger Logger { get; set; } = null!;

        // Service initialization flag
        private static bool _servicesInitialized;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            // Set up global exception handling
            SetupExceptionHandling();

            Logger?.Information("Application_Startup called - initializing services");

            try
            {
                // Initialize all services in dependency order
                await InitializeServicesAsync();

                Logger?.Information("All services initialized - creating MainWindow");

                // Create and show the main window
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                Logger?.Fatal(ex, "Failed to initialize application");
                MessageBox.Show(
                    $"Failed to start application:\n\n{ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        /// <summary>
        /// Initializes all services in the correct dependency order.
        /// </summary>
        private async Task InitializeServicesAsync()
        {
            if (_servicesInitialized) return;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Layer 1: Core Infrastructure (No Dependencies)
                Logger?.Debug("Initializing Layer 1: Core Infrastructure");
                _ = ErrorHandler.Instance;
                await SettingsManager.Instance.LoadAsync();
                _ = ThemeManager.Instance;

                // Layer 2: Memory & Logging
                Logger?.Debug("Initializing Layer 2: Memory & Logging");
                _ = MemoryManager.Instance;
                _ = MemoryGuard.Instance;
                _ = DebugService.Instance;

                // Layer 3: Network & Connections
                Logger?.Debug("Initializing Layer 3: Network & Connections");
                _ = ConnectionPool.Instance;
                await McpConnectionManager.Instance.InitializeAsync();
                _ = FiddlerBridge.Instance;
                _ = TrafficPipeClient.Instance;
                _ = ProxyMonitor.Instance;

                // Layer 4: Data Providers
                Logger?.Debug("Initializing Layer 4: Data Providers");
                _ = RealDataProvider.Instance;
                _ = LlmIntegrationService.Instance;

                // Layer 5: Game Services
                Logger?.Debug("Initializing Layer 5: Game Services");
                _ = SessionManager.Instance;
                _ = GameStateEngine.Instance;
                _ = ProtocolHandler.Instance;
                _ = PacketAnalysisEngine.Instance;

                // Layer 6: Automation
                Logger?.Debug("Initializing Layer 6: Automation Services");
                _ = AutoPilotService.Instance;
                _ = VisualAutomationService.Instance;
                _ = CombatSimulator.Instance;
                _ = StrategicAdvisor.Instance;

                // Layer 7: UI Services
                Logger?.Debug("Initializing Layer 7: UI Services");
                _ = ChatbotService.Instance;
                _ = StatusBarManager.Instance;
                _ = KeyboardShortcutManager.Instance;

                // Layer 8: Utilities
                Logger?.Debug("Initializing Layer 8: Utility Services");
                _ = SessionRecorder.Instance;
                _ = WebhookHub.Instance;
                _ = ExportImportManager.Instance;
                _ = AnalyticsDashboard.Instance;
                _ = MultiAccountOrchestrator.Instance;
                _ = PromptTemplateEngine.Instance;
                _ = MapScanner.Instance;
                _ = ProtocolFuzzer.Instance;
                _ = CdpConnectionService.Instance;
                _ = FailsafeManager.Instance;

                sw.Stop();
                _servicesInitialized = true;
                Logger?.Information("All {Count} services initialized in {ElapsedMs}ms", 34, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logger?.Error(ex, "Service initialization failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
                throw;
            }
        }

        private void SetupExceptionHandling()
        {
            // Handle UI thread exceptions
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            // Handle non-UI thread exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // Handle task exceptions
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger?.Error(e.Exception, "Unhandled UI exception");

            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true; // Prevent crash, let app continue
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Logger?.Fatal(exception, "Unhandled exception - IsTerminating: {IsTerminating}", e.IsTerminating);
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Logger?.Error(e.Exception, "Unobserved task exception");
            e.SetObserved(); // Prevent crash
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger?.Information("Application OnExit called - disposing services");

            // Dispose services in reverse order
            DisposeServices();

            Logger?.Information("All services disposed");
            base.OnExit(e);
        }

        /// <summary>
        /// Disposes all services in reverse initialization order.
        /// </summary>
        private void DisposeServices()
        {
            try
            {
                // Layer 8: Utilities (reverse order)
                TryDispose(FailsafeManager.Instance);
                TryDispose(CdpConnectionService.Instance);
                TryDispose(ProtocolFuzzer.Instance);
                TryDispose(MapScanner.Instance);
                TryDispose(PromptTemplateEngine.Instance);
                TryDispose(MultiAccountOrchestrator.Instance);
                TryDispose(AnalyticsDashboard.Instance);
                TryDispose(ExportImportManager.Instance);
                TryDispose(WebhookHub.Instance);
                TryDispose(SessionRecorder.Instance);

                // Layer 7: UI Services
                TryDispose(KeyboardShortcutManager.Instance);
                TryDispose(StatusBarManager.Instance);
                TryDispose(ChatbotService.Instance);

                // Layer 6: Automation
                TryDispose(StrategicAdvisor.Instance);
                TryDispose(CombatSimulator.Instance);
                TryDispose(VisualAutomationService.Instance);
                TryDispose(AutoPilotService.Instance);

                // Layer 5: Game Services
                TryDispose(PacketAnalysisEngine.Instance);
                TryDispose(ProtocolHandler.Instance);
                TryDispose(GameStateEngine.Instance);
                TryDispose(SessionManager.Instance);

                // Layer 4: Data Providers
                TryDispose(LlmIntegrationService.Instance);
                TryDispose(RealDataProvider.Instance);

                // Layer 3: Network & Connections
                TryDispose(ProxyMonitor.Instance);
                TryDispose(TrafficPipeClient.Instance);
                TryDispose(FiddlerBridge.Instance);
                TryDispose(McpConnectionManager.Instance);
                TryDispose(ConnectionPool.Instance);

                // Layer 2: Memory & Logging
                TryDispose(DebugService.Instance);
                TryDispose(MemoryGuard.Instance);
                TryDispose(MemoryManager.Instance);

                // Layer 1: Core Infrastructure
                TryDispose(ThemeManager.Instance);
                // SettingsManager - save before dispose
                Task.Run(async () => await SettingsManager.Instance.SaveAsync()).Wait(1000);
                TryDispose(ErrorHandler.Instance);
            }
            catch (Exception ex)
            {
                Logger?.Error(ex, "Error during service disposal");
            }
        }

        private void TryDispose(object service)
        {
            try
            {
                if (service is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger?.Warning(ex, "Error disposing {ServiceType}", service?.GetType().Name ?? "unknown");
            }
        }
    }
}
