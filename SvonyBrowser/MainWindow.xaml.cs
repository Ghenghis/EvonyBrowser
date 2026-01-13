using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Newtonsoft.Json;
using SvonyBrowser.Helpers;
using SvonyBrowser.Services;

namespace SvonyBrowser
{

    /// <summary>
    /// Main window containing dual browser panels for AutoEvony and EvonyClient.
    /// Implements IDisposable pattern for proper resource cleanup.
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        #region Fields

        // Browser controls (using object type to avoid XAML compiler issues with CefSharp)
        private object LeftBrowser;
        private object RightBrowser;

        // Panel state
        private enum PanelMode { Both, LeftOnly, RightOnly }
        private PanelMode _currentMode = PanelMode.Both;
        private bool _panelsSwapped = false;
    
        // Web/SWF mode state
        private enum BrowserMode { Web, Swf }
        private BrowserMode _leftBrowserMode = BrowserMode.Web;
        private BrowserMode _rightBrowserMode = BrowserMode.Web;
    
        // SWF paths
        private readonly string _autoEvonyPath;
        private readonly string _evonyClientPath;
        private string _currentServer = "cc2";
    
        // Services
        private readonly SessionManager _sessionManager;
        private readonly ProxyMonitor _proxyMonitor;
        private readonly McpConnectionManager _mcpManager;
        private readonly ProtocolHandler _protocolHandler;
        private readonly ObservableCollection<McpStatusViewModel> _mcpStatuses = new ObservableCollection<McpStatusViewModel>();
        private readonly DispatcherTimer _statusTimer;
        private bool _sidePanelVisible = true;
        private int _trafficCount = 0;
    
        // Synchronization
        private readonly SynchronizationContext? _syncContext;
        private CancellationTokenSource _cts;
    
        // Disposal tracking
        private bool _disposed = false;
        private bool _isClosing = false;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
        
            // Capture UI thread context for cross-thread operations
            _syncContext = SynchronizationContext.Current;
            _cts = new CancellationTokenSource();
        
            // Initialize services
            _sessionManager = SessionManager.Instance;
            _proxyMonitor = new ProxyMonitor();
            _mcpManager = McpConnectionManager.Instance;
            _protocolHandler = ProtocolHandler.Instance;
        
            // Initialize status timer
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _statusTimer.Tick += StatusTimer_Tick;
        
            // Subscribe to proxy status changes
            _proxyMonitor.ProxyStatusChanged += OnProxyStatusChanged;
        
            // Subscribe to MCP status changes
            _mcpManager.StatusChanged += OnMcpStatusChanged;
        
            // Set SWF paths - prefer settings, fallback to parent directory
            var settings = _sessionManager.Settings;
            _autoEvonyPath = !string.IsNullOrEmpty(settings.AutoEvonySwfPath) 
                ? settings.AutoEvonySwfPath 
                : Path.Combine(App.BasePath, "AutoEvony.swf");
            _evonyClientPath = !string.IsNullOrEmpty(settings.EvonyClientSwfPath) 
                ? settings.EvonyClientSwfPath 
                : Path.Combine(App.BasePath, "EvonyClient1921.swf");
        
            // Create browser controls in code-behind
            InitializeBrowsers();
        
            App.Logger.Information("MainWindow initialized");
            App.Logger.Information("SWF paths configured:");
            App.Logger.Information("  AutoEvony: {Path} (exists: {Exists})", _autoEvonyPath, File.Exists(_autoEvonyPath));
            App.Logger.Information("  EvonyClient: {Path} (exists: {Exists})", _evonyClientPath, File.Exists(_evonyClientPath));
        }
    
        private void InitializeBrowsers()
        {
            // Create browsers using BrowserHelper to avoid XAML compiler issues
            LeftBrowser = BrowserHelper.CreateBrowser("about:blank");
            RightBrowser = BrowserHelper.CreateBrowser("about:blank");
        
            // Set grid rows
            BrowserHelper.SetGridRow(LeftBrowser, 1);
            BrowserHelper.SetGridRow(RightBrowser, 1);
        
            // Subscribe to events
            BrowserHelper.OnFrameLoadEnd(LeftBrowser, (url, isMain) =>
            {
                if (!isMain) return;
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (_disposed) return;
                    LeftStatusText.Text = "Loaded";
                    App.Logger.Information("Left panel (AutoEvony) loaded successfully");
                }));
            });
        
            BrowserHelper.OnFrameLoadEnd(RightBrowser, (url, isMain) =>
            {
                if (!isMain) return;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_disposed) return;
                    RightStatusText.Text = "Loaded";
                    App.Logger.Information("Right panel (EvonyClient) loaded successfully");
                }));
            });
        
            BrowserHelper.OnLoadError(LeftBrowser, (failedUrl, errorText, errorCode) =>
            {
                if (errorCode == -3) return; // Aborted
                App.Logger.Error("Left browser load error: {Error} - URL: {Url}", errorText, failedUrl);
                Dispatcher.BeginInvoke(() =>
                {
                    if (_disposed) return;
                    LeftStatusText.Text = "Error";
                    UpdateStatus($"Load error: {errorText}");
                });
            });
        
            BrowserHelper.OnLoadError(RightBrowser, (failedUrl, errorText, errorCode) =>
            {
                if (errorCode == -3) return; // Aborted
                App.Logger.Error("Right browser load error: {Error} - URL: {Url}", errorText, failedUrl);
                Dispatcher.BeginInvoke(() =>
                {
                    if (_disposed) return;
                    RightStatusText.Text = "Error";
                    UpdateStatus($"Load error: {errorText}");
                });
            });
        
            // Add browsers to containers after Loaded event
            Loaded += (s, e) =>
            {
                BrowserHelper.AddToContainer(LeftBrowserContainer, LeftBrowser!);
                BrowserHelper.AddToContainer(RightBrowserContainer, RightBrowser!);
            };
        }
    
        private void NavigateLeftBrowser(string url)
        {
            BrowserHelper.Navigate(LeftBrowser, url);
        }
    
        private void NavigateRightBrowser(string url)
        {
            BrowserHelper.Navigate(RightBrowser, url);
        }
    
        private void ReloadLeftBrowser()
        {
            BrowserHelper.Reload(LeftBrowser);
            LeftStatusText.Text = "Reloading...";
        }
    
        private void ReloadRightBrowser()
        {
            BrowserHelper.Reload(RightBrowser);
            RightStatusText.Text = "Reloading...";
        }
    
        private async Task ClearBrowserCacheAsync()
        {
            await BrowserHelper.DeleteAllCookiesAsync();
            App.Logger.Information("Cookies cleared");
        
            // Clear SOL files (Flash shared objects)
            var solPath = Path.Combine(App.CachePath, "Pepper Data", "Shockwave Flash");
            if (Directory.Exists(solPath))
            {
                var deletedCount = 0;
                foreach (var file in Directory.GetFiles(solPath, "*.sol", SearchOption.AllDirectories))
                {
                    try 
                    { 
                        File.Delete(file);
                        deletedCount++;
                    } 
                    catch (Exception ex)
                    {
                        App.Logger.Warning(ex, "Failed to delete SOL file: {File}", file);
                    }
                }
                App.Logger.Information("Deleted {Count} SOL files", deletedCount);
            }
        
            // Reload both browsers
            BrowserHelper.Reload(LeftBrowser);
            BrowserHelper.Reload(RightBrowser);
        }
    
        private void ShowErrorInBrowser(object browser, string fileName, string path)
        {
            if (browser == null) return;
        
            var escapedPath = System.Web.HttpUtility.HtmlEncode(path);
            var html = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset=""utf-8"">
        <style>
            body {{ 
                background: #1a1a1a; 
                color: #ff6666; 
                font-family: 'Segoe UI', Tahoma, sans-serif;
                padding: 40px;
                margin: 0;
            }}
            h2 {{ color: #ff4444; margin-top: 0; }}
            code {{ 
                background: #333; 
                padding: 8px 12px; 
                border-radius: 4px;
                display: block;
                margin: 10px 0;
                color: #88ff88;
                word-break: break-all;
            }}
            .tip {{ color: #888; margin-top: 20px; font-size: 14px; }}
            .icon {{ font-size: 48px; margin-bottom: 10px; }}
        </style>
    </head>
    <body>
        <div class=""icon"">⚠️</div>
        <h2>{fileName} Not Found</h2>
        <p>Expected location:</p>
        <code>{escapedPath}</code>
        <p class=""tip"">Place the SWF file in the parent directory of SvonyBrowser:<br>
        <code>{System.Web.HttpUtility.HtmlEncode(App.BasePath)}</code></p>
    </body>
    </html>";
            BrowserHelper.LoadHtml(browser, html);
        }

        #endregion

        #region Window Lifecycle

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize MCP status list
                if (McpStatusList != null)
                    McpStatusList.ItemsSource = _mcpStatuses;
            
                // Connect protocol explorer to chatbot
                if (ProtocolExplorerControl != null)
                    ProtocolExplorerControl.TestInChatRequested += OnTestInChatRequested;
            
                // Check proxy status first
                await CheckProxyStatusAsync();
            
                // Initialize MCP and protocol handler
                await InitializeServicesAsync();
            
                // Load content into browsers (Web mode by default)
                LoadLeftPanelContent();
                LoadRightPanelContent();
            
                // Load saved panel layout if enabled
                if (_sessionManager.Settings.RememberPanelLayout)
                {
                    RestorePanelLayout();
                }
            
                // Start status timer
                _statusTimer.Start();
            
                UpdateStatus("Svony Browser ready. Login in either panel to authenticate both.");
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error during window initialization");
                UpdateStatus($"Initialization error: {ex.Message}");
            }
        }
    
        private async Task InitializeServicesAsync()
        {
            // Initialize protocol handler
            await _protocolHandler.InitializeAsync();
        
            // Connect to MCP servers
            UpdateMcpStatusList();
            await _mcpManager.ConnectAllAsync();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_isClosing) return;
            _isClosing = true;
        
            App.Logger.Information("Window closing, saving state and cleaning up...");
        
            try
            {
                // Stop status timer
                _statusTimer.Stop();
            
                // Save panel layout
                SavePanelLayout();
            
                // Cancel any pending operations
                _cts?.Cancel();
            
                // Disconnect MCP servers
                _mcpManager.DisconnectAllAsync().Wait(TimeSpan.FromSeconds(5));
            
                // Dispose resources
                Dispose();
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error during window closing");
            }
        }

        #endregion

        #region SWF Loading

        private void LoadSwfFiles()
        {
            // Load AutoEvony in left panel
            if (File.Exists(_autoEvonyPath))
            {
                var url = new Uri(_autoEvonyPath).AbsoluteUri;
                NavigateLeftBrowser(url);
                LeftStatusText.Text = "Loading...";
                App.Logger.Information("Loading AutoEvony from: {Url}", url);
            }
            else
            {
                ShowErrorInBrowser(LeftBrowser!, "AutoEvony.swf", _autoEvonyPath);
                LeftStatusText.Text = "Not Found";
                App.Logger.Warning("AutoEvony.swf not found at: {Path}", _autoEvonyPath);
            }

            // Load Evony Client in right panel
            if (File.Exists(_evonyClientPath))
            {
                var url = new Uri(_evonyClientPath).AbsoluteUri;
                NavigateRightBrowser(url);
                RightStatusText.Text = "Loading...";
                App.Logger.Information("Loading EvonyClient from: {Url}", url);
            }
            else
            {
                ShowErrorInBrowser(RightBrowser!, "EvonyClient1921.swf", _evonyClientPath);
                RightStatusText.Text = "Not Found";
                App.Logger.Warning("EvonyClient1921.swf not found at: {Path}", _evonyClientPath);
            }
        }

        #endregion

        #region Panel View Controls

        private void ShowLeftOnly_Click(object sender, RoutedEventArgs e) => SetPanelMode(PanelMode.LeftOnly);
        private void ShowRightOnly_Click(object sender, RoutedEventArgs e) => SetPanelMode(PanelMode.RightOnly);
        private void ShowBoth_Click(object sender, RoutedEventArgs e) => SetPanelMode(PanelMode.Both);

        private void SetPanelMode(PanelMode mode)
        {
            _currentMode = mode;
        
            switch (mode)
            {
                case PanelMode.LeftOnly:
                    LeftColumn.Width = new GridLength(1, GridUnitType.Star);
                    RightColumn.Width = new GridLength(0);
                    LeftPanel.Visibility = Visibility.Visible;
                    RightPanel.Visibility = Visibility.Collapsed;
                    PanelSplitter.Visibility = Visibility.Collapsed;
                    PanelModeText.Text = "Bot Only";
                    break;
                
                case PanelMode.RightOnly:
                    LeftColumn.Width = new GridLength(0);
                    RightColumn.Width = new GridLength(1, GridUnitType.Star);
                    LeftPanel.Visibility = Visibility.Collapsed;
                    RightPanel.Visibility = Visibility.Visible;
                    PanelSplitter.Visibility = Visibility.Collapsed;
                    PanelModeText.Text = "Client Only";
                    break;
                
                case PanelMode.Both:
                default:
                    LeftColumn.Width = new GridLength(1, GridUnitType.Star);
                    RightColumn.Width = new GridLength(1, GridUnitType.Star);
                    LeftPanel.Visibility = Visibility.Visible;
                    RightPanel.Visibility = Visibility.Visible;
                    PanelSplitter.Visibility = Visibility.Visible;
                    PanelModeText.Text = "Both Panels";
                    break;
            }
        
            App.Logger.Information("Panel mode changed to: {Mode}", mode);
        }

        private void SwapPanels_Click(object sender, RoutedEventArgs e)
        {
            _panelsSwapped = !_panelsSwapped;
        
            // Swap browser addresses
            var leftAddr = BrowserHelper.GetAddress(LeftBrowser);
            var rightAddr = BrowserHelper.GetAddress(RightBrowser);
        
            NavigateLeftBrowser(rightAddr);
            NavigateRightBrowser(leftAddr);
        
            UpdateStatus(_panelsSwapped ? "Panels swapped" : "Panels restored to default");
            App.Logger.Information("Panels swapped: {Swapped}", _panelsSwapped);
        }

        private void SavePanelLayout()
        {
            try
            {
                _sessionManager.Settings.LastPanelMode = _currentMode.ToString();
            
                // Calculate width ratio if both panels visible
                if (_currentMode == PanelMode.Both && ContentGrid.ActualWidth > 0)
                {
                    var leftWidth = LeftColumn.ActualWidth;
                    var totalWidth = ContentGrid.ActualWidth - PanelSplitter.ActualWidth;
                    if (totalWidth > 0)
                    {
                        _sessionManager.Settings.LeftPanelWidth = leftWidth / totalWidth;
                    }
                }
            
                _sessionManager.SaveSettings();
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to save panel layout");
            }
        }

        private void RestorePanelLayout()
        {
            try
            {
                // Restore panel mode
                if (Enum.TryParse<PanelMode>(_sessionManager.Settings.LastPanelMode, out var mode))
                {
                    SetPanelMode(mode);
                }
            
                // Restore width ratio (if Both mode)
                if (_currentMode == PanelMode.Both)
                {
                    var ratio = Math.Max(0.1, Math.Min(0.9, _sessionManager.Settings.LeftPanelWidth));
                    LeftColumn.Width = new GridLength(ratio, GridUnitType.Star);
                    RightColumn.Width = new GridLength(1 - ratio, GridUnitType.Star);
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to restore panel layout");
            }
        }

        #endregion

        #region Server Selection

        private void ServerSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServerSelector.SelectedItem is ComboBoxItem item && item.Tag is string server)
            {
                _currentServer = server;
                _sessionManager.SetServer($"{server}.evony.com");
                UpdateStatus($"Server changed to {server}.evony.com");
                App.Logger.Information("Server changed to: {Server}", server);
            }
        }

        #endregion

        #region Quick Actions

        private void ReloadLeft_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReloadLeftBrowser();
                UpdateStatus("Reloading AutoEvony...");
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to reload left browser");
            }
        }

        private void ReloadRight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReloadRightBrowser();
                UpdateStatus("Reloading Evony Client...");
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to reload right browser");
            }
        }

        private async void ClearCache_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Clear browser cache and cookies?\n\nThis will log you out of both panels.",
                "Clear Cache",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes) return;
        
            try
            {
                UpdateStatus("Clearing cache...");
            
                // Clear browser cache using helper method
                await ClearBrowserCacheAsync();
            
                // Reset session state
                _sessionManager.OnLogoutDetected();
            
                UpdateStatus("Cache cleared. Please login again.");
                App.Logger.Information("Cache cleared successfully");
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to clear cache");
                UpdateStatus($"Failed to clear cache: {ex.Message}");
                MessageBox.Show($"Failed to clear cache:\n\n{ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Tools

        private void SolEditor_Click(object sender, RoutedEventArgs e)
        {
            // Try to find SOL editor executable
            var solEditorPaths = new[]
            {
                Path.Combine(App.BasePath, "Assets", "Tools", "SolEditor.exe"),
                Path.Combine(App.BasePath, "tools", "SolEditor.exe"),
                Path.Combine(App.BasePath, "SolEditor.exe"),
            };
        
            foreach (var path in solEditorPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                        UpdateStatus("SOL Editor launched");
                        return;
                    }
                    catch (Exception ex)
                    {
                        App.Logger.Error(ex, "Failed to launch SOL Editor from: {Path}", path);
                    }
                }
            }
        
            // Fallback: Open SOL folder in Explorer
            var solFolder = Path.Combine(App.CachePath, "Pepper Data", "Shockwave Flash");
            if (Directory.Exists(solFolder))
            {
                try
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", solFolder) { UseShellExecute = true });
                    UpdateStatus("Opened SOL folder (no editor found)");
                }
                catch (Exception ex)
                {
                    App.Logger.Error(ex, "Failed to open SOL folder");
                }
            }
            else
            {
                MessageBox.Show(
                    "SOL Editor not found.\n\nYou can use online SOL editors or install Minerva SOL Editor.",
                    "SOL Editor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void OpenFiddler_Click(object sender, RoutedEventArgs e)
        {
            // Check if Fiddler is already running
            var fiddlerProcesses = Process.GetProcessesByName("Fiddler");
            if (fiddlerProcesses.Length > 0)
            {
                UpdateStatus("Fiddler is already running");
            
                // Try to bring it to front (best effort)
                try
                {
                    var hwnd = fiddlerProcesses[0].MainWindowHandle;
                    if (hwnd != IntPtr.Zero)
                    {
                        NativeMethods.SetForegroundWindow(hwnd);
                    }
                }
                catch { /* Ignore */ }
                return;
            }
        
            // Try to find and launch Fiddler
            var fiddlerPaths = new[]
            {
                Path.Combine(App.BasePath, "Fiddler", "Fiddler.exe"),
                @"C:\Users\" + Environment.UserName + @"\AppData\Local\Programs\Fiddler\Fiddler.exe",
                @"C:\Program Files\Fiddler\Fiddler.exe",
                @"C:\Program Files (x86)\Fiddler\Fiddler.exe",
            };
        
            foreach (var path in fiddlerPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                        UpdateStatus("Fiddler launched");
                        return;
                    }
                    catch (Exception ex)
                    {
                        App.Logger.Error(ex, "Failed to launch Fiddler from: {Path}", path);
                    }
                }
            }
        
            MessageBox.Show(
                "Fiddler Classic not found.\n\nPlease install Fiddler Classic and try again.\nDownload from: https://www.telerik.com/fiddler/fiddler-classic",
                "Fiddler Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow(_sessionManager)
                {
                    Owner = this
                };
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to open settings window");
                MessageBox.Show($"Failed to open settings:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Keyboard Shortcuts

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.D1:
                    case Key.NumPad1:
                        SetPanelMode(PanelMode.LeftOnly);
                        e.Handled = true;
                        break;
                    case Key.D2:
                    case Key.NumPad2:
                        SetPanelMode(PanelMode.Both);
                        e.Handled = true;
                        break;
                    case Key.D3:
                    case Key.NumPad3:
                        SetPanelMode(PanelMode.RightOnly);
                        e.Handled = true;
                        break;
                    case Key.S:
                        SwapPanels_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Key.R:
                        // Reload focused panel
                        if (BrowserHelper.IsFocused(LeftBrowser))
                            ReloadLeft_Click(sender, e);
                        else if (BrowserHelper.IsFocused(RightBrowser))
                            ReloadRight_Click(sender, e);
                        e.Handled = true;
                        break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.F5:
                        ReloadLeft_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Key.F6:
                        ReloadRight_Click(sender, e);
                        e.Handled = true;
                        break;
                }
            }
        }

        #endregion

        #region Status & Proxy Monitoring

        private void UpdateStatus(string message)
        {
            if (Dispatcher.CheckAccess())
            {
                StatusText.Text = message;
            }
            else
            {
                Dispatcher.BeginInvoke(() => StatusText.Text = message);
            }
        
            App.Logger.Information("Status: {Message}", message);
        }

        private void OnProxyStatusChanged(bool isAvailable)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (_disposed) return;
                UpdateProxyIndicator(isAvailable);
            });
        }

        private async Task CheckProxyStatusAsync()
        {
            try
            {
                var isProxyReady = await _proxyMonitor.CheckProxyAsync("127.0.0.1", 8888);
                UpdateProxyIndicator(isProxyReady);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error checking proxy status");
                UpdateProxyIndicator(false);
            }
        }

        private void UpdateProxyIndicator(bool isConnected)
        {
            if (isConnected)
            {
                ConnectionIndicator.Fill = new SolidColorBrush(Color.FromRgb(0x44, 0xFF, 0x44));
                ConnectionStatus.Text = "Proxy Connected";
                ProxyStatus.Text = "127.0.0.1:8888 ✓";
                ProxyStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0xFF, 0x88));
            }
            else
            {
                ConnectionIndicator.Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0x88, 0x44));
                ConnectionStatus.Text = "Proxy Not Running";
                ProxyStatus.Text = "127.0.0.1:8888 ✗";
                ProxyStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x88, 0x44));
            
                if (!_isClosing)
                {
                    UpdateStatus("Warning: Fiddler proxy not detected. Traffic won't be captured.");
                }
            }
        }

        #endregion
    
        #region Side Panel
    
        private void TogglePanel_Click(object sender, RoutedEventArgs e)
        {
            _sidePanelVisible = !_sidePanelVisible;
        
            if (_sidePanelVisible)
            {
                SidePanelColumn.Width = new GridLength(400);
                TogglePanelButton.Content = "◀";
            }
            else
            {
                SidePanelColumn.Width = new GridLength(0);
                TogglePanelButton.Content = "▶";
            }
        }
    
        #endregion
    
        #region MCP Status
    
        private void OnMcpStatusChanged(string serverName, McpConnectionStatus status)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateMcpStatusList();
                UpdateMcpIndicator();
            });
        }
    
        private void UpdateMcpStatusList()
        {
            _mcpStatuses.Clear();
        
            var statuses = _mcpManager.GetAllStatuses();
            foreach (var kvp in statuses)
            {
                _mcpStatuses.Add(new McpStatusViewModel
                {
                    Name = kvp.Key,
                    Status = kvp.Value.ToString(),
                    StatusColor = GetStatusBrush(kvp.Value)
                });
            }
        }
    
        private void UpdateMcpIndicator()
        {
            var statuses = _mcpManager.GetAllStatuses();
            var allConnected = true;
            var anyConnected = false;
        
            foreach (var status in statuses.Values)
            {
                if (status == McpConnectionStatus.Connected)
                    anyConnected = true;
                else
                    allConnected = false;
            }
        
            if (allConnected && statuses.Count > 0)
            {
                McpStatusIndicator.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                McpStatusIndicator.ToolTip = "MCP Status: All Connected";
            }
            else if (anyConnected)
            {
                McpStatusIndicator.Background = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                McpStatusIndicator.ToolTip = "MCP Status: Partially Connected";
            }
            else
            {
                McpStatusIndicator.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                McpStatusIndicator.ToolTip = "MCP Status: Disconnected";
            }
        }
    
        private Brush GetStatusBrush(McpConnectionStatus status)
        {
            return status switch
            {
                McpConnectionStatus.Connected => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                McpConnectionStatus.Connecting => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                McpConnectionStatus.Disconnected => new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                McpConnectionStatus.Error => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                _ => new SolidColorBrush(Color.FromRgb(148, 163, 184))
            };
        }
    
        private async void ReconnectMcp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Reconnecting MCP servers...");
                await _mcpManager.DisconnectAllAsync();
                await _mcpManager.ConnectAllAsync();
                UpdateStatus("MCP servers reconnected");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
            }
        }
    
        #endregion
    
        #region Tools Tab
    
        private void DecodeAmf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var input = AmfInputTextBox.Text?.Trim();
                if (string.IsNullOrEmpty(input))
                {
                    AmfOutputTextBox.Text = "Please enter hex data to decode";
                    return;
                }
            
                // Remove spaces and convert to bytes
                var hex = input.Replace(" ", "").Replace("\n", "").Replace("\r", "");
                var bytes = HexToBytes(hex);
            
                // Decode AMF3
                var decoded = _protocolHandler.DecodeAmf3(bytes);
                AmfOutputTextBox.Text = JsonConvert.SerializeObject(decoded, Formatting.Indented);
            }
            catch (Exception ex)
            {
                AmfOutputTextBox.Text = $"Error: {ex.Message}";
            }
        }
    
        private void CalculateTraining_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TroopTypeComboBox.SelectedItem is not ComboBoxItem selectedItem)
                {
                    CalculatorResultText.Text = "Please select a troop type";
                    return;
                }
            
                if (!int.TryParse(TroopAmountTextBox.Text, out int amount) || amount <= 0)
                {
                    CalculatorResultText.Text = "Please enter a valid amount";
                    return;
                }
            
                var troopType = selectedItem.Tag?.ToString() ?? "a";
                var result = CalculateTrainingCost(troopType, amount);
                CalculatorResultText.Text = result;
            }
            catch (Exception ex)
            {
                CalculatorResultText.Text = $"Error: {ex.Message}";
            }
        }
    
        private string CalculateTrainingCost(string troopType, int amount)
        {
            var costs = troopType switch
            {
                "a" => (food: 350, lumber: 450, stone: 0, iron: 100, time: 30),
                "c" => (food: 550, lumber: 100, stone: 0, iron: 350, time: 45),
                "cata" => (food: 650, lumber: 200, stone: 0, iron: 550, time: 60),
                "w" => (food: 200, lumber: 100, stone: 0, iron: 100, time: 15),
                "s" => (food: 100, lumber: 50, stone: 0, iron: 50, time: 10),
                "p" => (food: 300, lumber: 200, stone: 0, iron: 200, time: 25),
                "sw" => (food: 400, lumber: 150, stone: 0, iron: 300, time: 35),
                "b" => (food: 400, lumber: 600, stone: 100, iron: 200, time: 90),
                "r" => (food: 500, lumber: 800, stone: 200, iron: 300, time: 120),
                "cp" => (food: 600, lumber: 1000, stone: 300, iron: 400, time: 150),
                _ => (food: 200, lumber: 100, stone: 0, iron: 100, time: 15)
            };
        
            var totalFood = costs.food * amount;
            var totalLumber = costs.lumber * amount;
            var totalStone = costs.stone * amount;
            var totalIron = costs.iron * amount;
            var totalTime = TimeSpan.FromSeconds(costs.time * amount);
        
            return $"Training {amount:N0} troops:\n\n" +
                   $"Food: {totalFood:N0}\n" +
                   $"Lumber: {totalLumber:N0}\n" +
                   $"Stone: {totalStone:N0}\n" +
                   $"Iron: {totalIron:N0}\n\n" +
                   $"Base Time: {totalTime.Days}d {totalTime.Hours}h {totalTime.Minutes}m";
        }
    
        private static byte[] HexToBytes(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    
        #endregion
    
        #region Web/SWF Toggle Handlers
    
        private void LeftWebToggle_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_leftBrowserMode == BrowserMode.Web) return;
            _leftBrowserMode = BrowserMode.Web;
            UpdateLeftToggleVisuals();
            LoadLeftPanelContent();
        }
    
        private void LeftSwfToggle_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_leftBrowserMode == BrowserMode.Swf) return;
            _leftBrowserMode = BrowserMode.Swf;
            UpdateLeftToggleVisuals();
            LoadLeftPanelContent();
        }
    
        private void RightWebToggle_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_rightBrowserMode == BrowserMode.Web) return;
            _rightBrowserMode = BrowserMode.Web;
            UpdateRightToggleVisuals();
            LoadRightPanelContent();
        }
    
        private void RightSwfToggle_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_rightBrowserMode == BrowserMode.Swf) return;
            _rightBrowserMode = BrowserMode.Swf;
            UpdateRightToggleVisuals();
            LoadRightPanelContent();
        }
    
        private void UpdateLeftToggleVisuals()
        {
            if (_leftBrowserMode == BrowserMode.Web)
            {
                LeftWebToggle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                LeftSwfToggle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D3D"));
                ((TextBlock)LeftWebToggle.Child).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                ((TextBlock)LeftWebToggle.Child).FontWeight = FontWeights.Bold;
                ((TextBlock)LeftSwfToggle.Child).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                ((TextBlock)LeftSwfToggle.Child).FontWeight = FontWeights.Medium;
            }
            else
            {
                LeftWebToggle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D3D"));
                LeftSwfToggle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                ((TextBlock)LeftWebToggle.Child).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                ((TextBlock)LeftWebToggle.Child).FontWeight = FontWeights.Medium;
                ((TextBlock)LeftSwfToggle.Child).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                ((TextBlock)LeftSwfToggle.Child).FontWeight = FontWeights.Bold;
            }
        }
    
        private void UpdateRightToggleVisuals()
        {
            if (_rightBrowserMode == BrowserMode.Web)
            {
                RightWebToggle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                RightSwfToggle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D3D"));
                ((TextBlock)RightWebToggle.Child).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                ((TextBlock)RightWebToggle.Child).FontWeight = FontWeights.Bold;
                ((TextBlock)RightSwfToggle.Child).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                ((TextBlock)RightSwfToggle.Child).FontWeight = FontWeights.Medium;
            }
            else
            {
                RightWebToggle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D3D"));
                RightSwfToggle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                ((TextBlock)RightWebToggle.Child).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                ((TextBlock)RightWebToggle.Child).FontWeight = FontWeights.Medium;
                ((TextBlock)RightSwfToggle.Child).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                ((TextBlock)RightSwfToggle.Child).FontWeight = FontWeights.Bold;
            }
        }
    
        private void LoadLeftPanelContent()
        {
            if (_leftBrowserMode == BrowserMode.Web)
            {
                // Load Evony web game
                var url = $"https://{_currentServer}.evony.com";
                NavigateLeftBrowser(url);
                LeftStatusText.Text = "Web";
                App.Logger.Information("Left panel loading web: {Url}", url);
            }
            else
            {
                // Load SWF file
                if (File.Exists(_autoEvonyPath))
                {
                    var url = new Uri(_autoEvonyPath).AbsoluteUri;
                    NavigateLeftBrowser(url);
                    LeftStatusText.Text = "SWF";
                    App.Logger.Information("Left panel loading SWF: {Url}", url);
                }
                else
                {
                    ShowErrorInBrowser(LeftBrowser!, "AutoEvony.swf", _autoEvonyPath);
                    LeftStatusText.Text = "Not Found";
                    App.Logger.Warning("AutoEvony.swf not found at: {Path}", _autoEvonyPath);
                }
            }
        }
    
        private void LoadRightPanelContent()
        {
            if (_rightBrowserMode == BrowserMode.Web)
            {
                // Load Evony web game
                var url = $"https://{_currentServer}.evony.com";
                NavigateRightBrowser(url);
                RightStatusText.Text = "Web";
                App.Logger.Information("Right panel loading web: {Url}", url);
            }
            else
            {
                // Load SWF file
                if (File.Exists(_evonyClientPath))
                {
                    var url = new Uri(_evonyClientPath).AbsoluteUri;
                    NavigateRightBrowser(url);
                    RightStatusText.Text = "SWF";
                    App.Logger.Information("Right panel loading SWF: {Url}", url);
                }
                else
                {
                    ShowErrorInBrowser(RightBrowser!, "EvonyClient.swf", _evonyClientPath);
                    RightStatusText.Text = "Not Found";
                    App.Logger.Warning("EvonyClient.swf not found at: {Path}", _evonyClientPath);
                }
            }
        }
    
        #endregion
    
        #region Event Handlers
    
        private void OnTestInChatRequested(string message)
        {
            ChatbotPanelControl?.SendMessageAsync(message);
        }
    
        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            // Update traffic count
            if (TrafficCountText != null)
                TrafficCountText.Text = $"Traffic: {_trafficCount}";
        }
    
        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
        
            if (disposing)
            {
                App.Logger.Debug("Disposing MainWindow resources...");
            
                // Unsubscribe from events to prevent memory leaks
                _proxyMonitor.ProxyStatusChanged -= OnProxyStatusChanged;
            
                // Cancel pending operations
                try
                {
                    _cts?.Cancel();
                    _cts?.Dispose();
                    _cts = null;
                }
                catch { /* Ignore */ }
            
                // Dispose proxy monitor
                try
                {
                    _proxyMonitor.Dispose();
                }
                catch { /* Ignore */ }
            
                App.Logger.Debug("MainWindow resources disposed");
            }
        
            _disposed = true;
        }

        ~MainWindow()
        {
            Dispose(false);
        }

        #endregion
    }

    /// <summary>
    /// Native Windows methods for window management.
    /// </summary>
    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);
    }

    /// <summary>
    /// View model for MCP status display.
    /// </summary>
    public class McpStatusViewModel
    {
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public Brush StatusColor { get; set; } = Brushes.Gray;
    }

}