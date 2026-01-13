using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using Microsoft.Win32;
using SvonyBrowser.Services;
using SvonyBrowser.Models;

namespace SvonyBrowser
{
    /// <summary>
    /// Settings Control Center for Svony Browser v6.0 Borg Edition.
    /// Provides comprehensive settings management with toggles, sliders, and dropdowns.
    /// </summary>
    public partial class SettingsControlCenter : Window
    {
        private readonly SettingsManager _settingsManager;
        private bool _isDirty;
        
        public SettingsControlCenter()
        {
            InitializeComponent();
            _settingsManager = SettingsManager.Instance;
            
            Loaded += async (s, e) => await LoadSettingsAsync();
            Closing += OnClosing;
            
            // Select first category by default
            CategoryList.SelectedIndex = 0;
        }
        
        #region Settings Loading
        
        private async Task LoadSettingsAsync()
        {
            try
            {
                await _settingsManager.LoadAsync();
                BindSettingsToUI();
                _isDirty = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void BindSettingsToUI()
        {
            var s = _settingsManager.Settings;
            
            // General Settings
            SetComboBoxValue(ThemeCombo, s.General.Theme);
            SetComboBoxValue(LanguageCombo, s.General.Language);
            StartMinimizedToggle.IsChecked = s.General.StartMinimized;
            StartWithWindowsToggle.IsChecked = s.General.StartWithWindows;
            CheckUpdatesToggle.IsChecked = s.General.CheckUpdates;
            SetComboBoxValue(UpdateChannelCombo, s.General.UpdateChannel);
            SetComboBoxValue(LogLevelCombo, s.General.LogLevel);
            LogRetentionSlider.Value = s.General.LogRetentionDays;
            ConfirmExitToggle.IsChecked = s.General.ConfirmOnExit;
            RememberPositionToggle.IsChecked = s.General.RememberWindowPosition;
            
            // Browser Settings
            SetComboBoxValue(DefaultServerCombo, s.Browser.DefaultServer);
            CacheSizeSlider.Value = s.Browser.CacheSizeMB;
            EnableFlashToggle.IsChecked = s.Browser.EnableFlash;
            EnableGpuToggle.IsChecked = s.Browser.EnableGPU;
            ZoomSlider.Value = s.Browser.DefaultZoom;
            EnableDevToolsToggle.IsChecked = s.Browser.EnableDevTools;
            
            // Proxy Settings
            ProxyEnabledToggle.IsChecked = s.Proxy.Enabled;
            ProxyHostText.Text = s.Proxy.Host;
            ProxyPortText.Text = s.Proxy.Port.ToString();
            SetComboBoxValue(ProxyTypeCombo, s.Proxy.Type);
            TestProxyToggle.IsChecked = s.Proxy.TestOnStartup;
            
            // MCP Settings
            McpEnabledToggle.IsChecked = s.Mcp.Enabled;
            McpAutoStartToggle.IsChecked = s.Mcp.AutoStartServers;
            RagPortText.Text = s.Mcp.RagServerPort.ToString();
            RagEnabledToggle.IsChecked = s.Mcp.RagServerEnabled;
            RtePortText.Text = s.Mcp.RteServerPort.ToString();
            RteEnabledToggle.IsChecked = s.Mcp.RteServerEnabled;
            HealthCheckSlider.Value = s.Mcp.HealthCheckInterval;
            
            // LLM Settings
            LlmEnabledToggle.IsChecked = s.Llm.Enabled;
            SetComboBoxValue(LlmBackendCombo, s.Llm.Backend);
            LmStudioUrlText.Text = s.Llm.LmStudioUrl;
            TemperatureSlider.Value = s.Llm.Temperature;
            MaxTokensSlider.Value = s.Llm.MaxTokens;
            StreamResponseToggle.IsChecked = s.Llm.StreamResponse;
            
            // Fiddler Settings
            FiddlerEnabledToggle.IsChecked = s.Fiddler.Enabled;
            AutoStartFiddlerToggle.IsChecked = s.Fiddler.AutoStartFiddler;
            DecodeAmfToggle.IsChecked = s.Fiddler.DecodeAmf;
            MaxTrafficSlider.Value = s.Fiddler.MaxTrafficEntries;
            
            // Automation Settings
            AutoPilotToggle.IsChecked = s.Automation.AutoPilotEnabled;
            SafetyLimitsToggle.IsChecked = s.Automation.SafetyLimitsEnabled;
            MaxActionsSlider.Value = s.Automation.MaxActionsPerMinute;
            ActionDelaySlider.Value = s.Automation.ActionDelay;
            RandomizeDelayToggle.IsChecked = s.Automation.RandomizeDelay;
            
            // Traffic Settings
            TrafficAnalysisToggle.IsChecked = s.Traffic.AnalysisEnabled;
            PatternDetectionToggle.IsChecked = s.Traffic.PatternDetection;
            AutoScrollToggle.IsChecked = s.Traffic.AutoScroll;
            
            // Chatbot Settings
            ChatbotEnabledToggle.IsChecked = s.Chatbot.Enabled;
            ShowTimestampsToggle.IsChecked = s.Chatbot.ShowTimestamps;
            MarkdownToggle.IsChecked = s.Chatbot.MarkdownRendering;
            
            // Status Bar Settings
            StatusBarEnabledToggle.IsChecked = s.StatusBar.Enabled;
            ShowRagProgressToggle.IsChecked = s.StatusBar.ShowRagProgress;
            ShowRteProgressToggle.IsChecked = s.StatusBar.ShowRteProgress;
            ShowLlmStatsToggle.IsChecked = s.StatusBar.ShowLlmStats;
            ShowGpuTempToggle.IsChecked = s.StatusBar.ShowGpuTemp;
            ShowPacketsToggle.IsChecked = s.StatusBar.ShowPacketsPerSec;
            
            // Webhooks Settings
            WebhooksEnabledToggle.IsChecked = s.Webhooks.Enabled;
            DiscordWebhookText.Text = s.Webhooks.DiscordWebhook;
            TelegramTokenText.Text = s.Webhooks.TelegramBotToken;
            TelegramChatIdText.Text = s.Webhooks.TelegramChatId;
            
            // Advanced Settings
            DebugModeToggle.IsChecked = s.Advanced.DebugMode;
            PerformanceModeToggle.IsChecked = s.Advanced.PerformanceMode;
            MaxMemorySlider.Value = s.Advanced.MaxMemoryMB;
            
            // Wire up change events
            WireUpChangeEvents();
        }
        
        private void WireUpChangeEvents()
        {
            // Wire up all controls to set dirty flag
            foreach (var control in FindVisualChildren<CheckBox>(this))
            {
                control.Checked += (s, e) => _isDirty = true;
                control.Unchecked += (s, e) => _isDirty = true;
            }
            
            foreach (var control in FindVisualChildren<ComboBox>(this))
            {
                control.SelectionChanged += (s, e) => _isDirty = true;
            }
            
            foreach (var control in FindVisualChildren<Slider>(this))
            {
                control.ValueChanged += (s, e) => _isDirty = true;
            }
            
            foreach (var control in FindVisualChildren<TextBox>(this))
            {
                control.TextChanged += (s, e) => _isDirty = true;
            }
        }
        
        private void SetComboBoxValue(ComboBox combo, string value)
        {
            foreach (ComboBoxItem item in combo.Items)
            {
                if (item.Content?.ToString() == value)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }
        
        private string GetComboBoxValue(ComboBox combo)
        {
            return (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        }
        
        #endregion
        
        #region Settings Saving
        
        private void CollectSettingsFromUI()
        {
            var s = _settingsManager.Settings;
            
            // General Settings
            s.General.Theme = GetComboBoxValue(ThemeCombo);
            s.General.Language = GetComboBoxValue(LanguageCombo);
            s.General.StartMinimized = StartMinimizedToggle.IsChecked ?? false;
            s.General.StartWithWindows = StartWithWindowsToggle.IsChecked ?? false;
            s.General.CheckUpdates = CheckUpdatesToggle.IsChecked ?? true;
            s.General.UpdateChannel = GetComboBoxValue(UpdateChannelCombo);
            s.General.LogLevel = GetComboBoxValue(LogLevelCombo);
            s.General.LogRetentionDays = (int)LogRetentionSlider.Value;
            s.General.ConfirmOnExit = ConfirmExitToggle.IsChecked ?? true;
            s.General.RememberWindowPosition = RememberPositionToggle.IsChecked ?? true;
            
            // Browser Settings
            s.Browser.DefaultServer = GetComboBoxValue(DefaultServerCombo);
            s.Browser.CacheSizeMB = (int)CacheSizeSlider.Value;
            s.Browser.EnableFlash = EnableFlashToggle.IsChecked ?? true;
            s.Browser.EnableGPU = EnableGpuToggle.IsChecked ?? true;
            s.Browser.DefaultZoom = (int)ZoomSlider.Value;
            s.Browser.EnableDevTools = EnableDevToolsToggle.IsChecked ?? false;
            
            // Proxy Settings
            s.Proxy.Enabled = ProxyEnabledToggle.IsChecked ?? true;
            s.Proxy.Host = ProxyHostText.Text;
            if (int.TryParse(ProxyPortText.Text, out int proxyPort))
                s.Proxy.Port = proxyPort;
            s.Proxy.Type = GetComboBoxValue(ProxyTypeCombo);
            s.Proxy.TestOnStartup = TestProxyToggle.IsChecked ?? true;
            
            // MCP Settings
            s.Mcp.Enabled = McpEnabledToggle.IsChecked ?? true;
            s.Mcp.AutoStartServers = McpAutoStartToggle.IsChecked ?? true;
            if (int.TryParse(RagPortText.Text, out int ragPort))
                s.Mcp.RagServerPort = ragPort;
            s.Mcp.RagServerEnabled = RagEnabledToggle.IsChecked ?? true;
            if (int.TryParse(RtePortText.Text, out int rtePort))
                s.Mcp.RteServerPort = rtePort;
            s.Mcp.RteServerEnabled = RteEnabledToggle.IsChecked ?? true;
            s.Mcp.HealthCheckInterval = (int)HealthCheckSlider.Value;
            
            // LLM Settings
            s.Llm.Enabled = LlmEnabledToggle.IsChecked ?? true;
            s.Llm.Backend = GetComboBoxValue(LlmBackendCombo);
            s.Llm.LmStudioUrl = LmStudioUrlText.Text;
            s.Llm.Temperature = TemperatureSlider.Value;
            s.Llm.MaxTokens = (int)MaxTokensSlider.Value;
            s.Llm.StreamResponse = StreamResponseToggle.IsChecked ?? true;
            
            // Fiddler Settings
            s.Fiddler.Enabled = FiddlerEnabledToggle.IsChecked ?? true;
            s.Fiddler.AutoStartFiddler = AutoStartFiddlerToggle.IsChecked ?? false;
            s.Fiddler.DecodeAmf = DecodeAmfToggle.IsChecked ?? true;
            s.Fiddler.MaxTrafficEntries = (int)MaxTrafficSlider.Value;
            
            // Automation Settings
            s.Automation.AutoPilotEnabled = AutoPilotToggle.IsChecked ?? false;
            s.Automation.SafetyLimitsEnabled = SafetyLimitsToggle.IsChecked ?? true;
            s.Automation.MaxActionsPerMinute = (int)MaxActionsSlider.Value;
            s.Automation.ActionDelay = (int)ActionDelaySlider.Value;
            s.Automation.RandomizeDelay = RandomizeDelayToggle.IsChecked ?? true;
            
            // Traffic Settings
            s.Traffic.AnalysisEnabled = TrafficAnalysisToggle.IsChecked ?? true;
            s.Traffic.PatternDetection = PatternDetectionToggle.IsChecked ?? true;
            s.Traffic.AutoScroll = AutoScrollToggle.IsChecked ?? true;
            
            // Chatbot Settings
            s.Chatbot.Enabled = ChatbotEnabledToggle.IsChecked ?? true;
            s.Chatbot.ShowTimestamps = ShowTimestampsToggle.IsChecked ?? true;
            s.Chatbot.MarkdownRendering = MarkdownToggle.IsChecked ?? true;
            
            // Status Bar Settings
            s.StatusBar.Enabled = StatusBarEnabledToggle.IsChecked ?? true;
            s.StatusBar.ShowRagProgress = ShowRagProgressToggle.IsChecked ?? true;
            s.StatusBar.ShowRteProgress = ShowRteProgressToggle.IsChecked ?? true;
            s.StatusBar.ShowLlmStats = ShowLlmStatsToggle.IsChecked ?? true;
            s.StatusBar.ShowGpuTemp = ShowGpuTempToggle.IsChecked ?? true;
            s.StatusBar.ShowPacketsPerSec = ShowPacketsToggle.IsChecked ?? true;
            
            // Webhooks Settings
            s.Webhooks.Enabled = WebhooksEnabledToggle.IsChecked ?? false;
            s.Webhooks.DiscordWebhook = DiscordWebhookText.Text;
            s.Webhooks.TelegramBotToken = TelegramTokenText.Text;
            s.Webhooks.TelegramChatId = TelegramChatIdText.Text;
            
            // Advanced Settings
            s.Advanced.DebugMode = DebugModeToggle.IsChecked ?? false;
            s.Advanced.PerformanceMode = PerformanceModeToggle.IsChecked ?? false;
            s.Advanced.MaxMemoryMB = (int)MaxMemorySlider.Value;
        }
        
        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CollectSettingsFromUI();
                
                // Validate settings
                var validation = _settingsManager.Validate();
                if (!validation.IsValid)
                {
                    var errors = string.Join("\n", validation.Issues
                        .Where(i => i.Severity == ValidationSeverity.Error)
                        .Select(i => $"• {i.Category}: {i.Message}"));
                    
                    MessageBox.Show($"Please fix the following errors:\n\n{errors}", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (validation.HasWarnings)
                {
                    var warnings = string.Join("\n", validation.Issues
                        .Where(i => i.Severity == ValidationSeverity.Warning)
                        .Select(i => $"• {i.Category}: {i.Message}"));
                    
                    var result = MessageBox.Show($"There are some warnings:\n\n{warnings}\n\nSave anyway?", 
                        "Warnings", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result != MessageBoxResult.Yes)
                        return;
                }
                
                await _settingsManager.SaveAsync();
                _isDirty = false;
                
                MessageBox.Show("Settings saved successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show("You have unsaved changes. Discard them?", "Unsaved Changes",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result != MessageBoxResult.Yes)
                    return;
            }
            
            DialogResult = false;
            Close();
        }
        
        #endregion
        
        #region Import/Export
        
        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Settings Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Import Settings"
            };
            
            if (dialog.ShowDialog() == true)
            {
                var password = PromptForPassword("Enter password (leave empty if not encrypted):");
                
                if (await _settingsManager.ImportAsync(dialog.FileName, password))
                {
                    BindSettingsToUI();
                    MessageBox.Show("Settings imported successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        
        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Settings Files (*.json)|*.json",
                Title = "Export Settings",
                FileName = $"svony-settings-{DateTime.Now:yyyyMMdd}.json"
            };
            
            if (dialog.ShowDialog() == true)
            {
                var encrypt = MessageBox.Show("Encrypt the exported settings?", "Encryption",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
                
                string password = null;
                if (encrypt)
                {
                    password = PromptForPassword("Enter encryption password:");
                    if (string.IsNullOrEmpty(password))
                    {
                        MessageBox.Show("Password is required for encryption.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                
                CollectSettingsFromUI();
                
                if (await _settingsManager.ExportAsync(dialog.FileName, encrypt, password))
                {
                    MessageBox.Show("Settings exported successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        
        private string PromptForPassword(string message)
        {
            // Simple password prompt - in production, use a proper dialog
            return Microsoft.VisualBasic.Interaction.InputBox(message, "Password", "");
        }
        
        #endregion
        
        #region Reset
        
        private async void Reset_Click(object sender, RoutedEventArgs e)
        {
            await ResetToDefaults();
        }
        
        private async void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            await ResetToDefaults();
        }
        
        private async Task ResetToDefaults()
        {
            var result = MessageBox.Show(
                "This will reset all settings to their default values. A backup will be created.\n\nContinue?",
                "Reset to Defaults", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                await _settingsManager.ResetToDefaultsAsync();
                BindSettingsToUI();
                _isDirty = false;
                
                MessageBox.Show("Settings have been reset to defaults.", "Reset Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private async void FactoryReset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠ WARNING: This will delete ALL settings, backups, and data.\n\n" +
                "This action cannot be undone!\n\nAre you absolutely sure?",
                "Factory Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                var confirm = MessageBox.Show(
                    "Type 'RESET' to confirm factory reset.",
                    "Confirm Factory Reset", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                
                if (confirm == MessageBoxResult.OK)
                {
                    await _settingsManager.FactoryResetAsync();
                    BindSettingsToUI();
                    _isDirty = false;
                    
                    MessageBox.Show("Factory reset complete.", "Reset Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        
        #endregion
        
        #region Navigation
        
        private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Hide all panels
            GeneralPanel.Visibility = Visibility.Collapsed;
            BrowserPanel.Visibility = Visibility.Collapsed;
            ProxyPanel.Visibility = Visibility.Collapsed;
            McpPanel.Visibility = Visibility.Collapsed;
            LlmPanel.Visibility = Visibility.Collapsed;
            FiddlerPanel.Visibility = Visibility.Collapsed;
            AutomationPanel.Visibility = Visibility.Collapsed;
            TrafficPanel.Visibility = Visibility.Collapsed;
            ChatbotPanel.Visibility = Visibility.Collapsed;
            StatusBarPanel.Visibility = Visibility.Collapsed;
            WebhooksPanel.Visibility = Visibility.Collapsed;
            AdvancedPanel.Visibility = Visibility.Collapsed;
            
            // Show selected panel
            var index = CategoryList.SelectedIndex;
            switch (index)
            {
                case 0: GeneralPanel.Visibility = Visibility.Visible; break;
                case 1: BrowserPanel.Visibility = Visibility.Visible; break;
                case 2: ProxyPanel.Visibility = Visibility.Visible; break;
                case 3: McpPanel.Visibility = Visibility.Visible; break;
                case 4: LlmPanel.Visibility = Visibility.Visible; break;
                case 5: FiddlerPanel.Visibility = Visibility.Visible; break;
                case 6: AutomationPanel.Visibility = Visibility.Visible; break;
                case 7: TrafficPanel.Visibility = Visibility.Visible; break;
                case 8: ChatbotPanel.Visibility = Visibility.Visible; break;
                case 9: StatusBarPanel.Visibility = Visibility.Visible; break;
                case 10: WebhooksPanel.Visibility = Visibility.Visible; break;
                case 11: AdvancedPanel.Visibility = Visibility.Visible; break;
            }
        }
        
        #endregion
        
        #region Window Events
        
        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show("You have unsaved changes. Save before closing?",
                    "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        CollectSettingsFromUI();
                        _settingsManager.Save();
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }
        
        #endregion
        
        #region Helpers
        
        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;
            
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild)
                    yield return typedChild;
                
                foreach (var grandChild in FindVisualChildren<T>(child))
                    yield return grandChild;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Helper class for settings category navigation.
    /// </summary>
    public class SettingsCategory
    {
        public string Icon { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
