using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SvonyBrowser.Services;

namespace SvonyBrowser
{

    /// <summary>
    /// Settings dialog for configuring application preferences.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SessionManager _sessionManager;
        private bool _isDirty = false;

        public SettingsWindow(SessionManager sessionManager)
        {
            InitializeComponent();
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        
            // Load current settings into UI
            LoadSettings();
        
            // Track changes
            AttachChangeHandlers();
        }

        #region Settings Loading

        private void LoadSettings()
        {
            try
            {
                var settings = _sessionManager.Settings;

                // Server selection
                SelectComboBoxItem(DefaultServerCombo, settings.DefaultServer);

                // Proxy settings
                ProxyHostText.Text = settings.ProxyHost ?? "127.0.0.1";
                ProxyPortText.Text = settings.ProxyPort.ToString();
                AutoStartFiddlerCheck.IsChecked = settings.AutoStartFiddler;

                // UI settings
                RememberLayoutCheck.IsChecked = settings.RememberPanelLayout;
                EnableLoggingCheck.IsChecked = settings.EnableLogging;

                // SWF paths
                AutoEvonySwfPathText.Text = settings.AutoEvonySwfPath ?? "";
                EvonyClientSwfPathText.Text = settings.EvonyClientSwfPath ?? "";
                FlashPluginPathText.Text = settings.FlashPluginPath ?? "";

                // Paths (read-only info)
                CachePathText.Text = _sessionManager.CachePath;
                LogPathText.Text = App.LogPath;
            
                _isDirty = false;
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to load settings into dialog");
                MessageBox.Show(
                    $"Failed to load settings:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SelectComboBoxItem(ComboBox comboBox, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }

            // If not found, select first item
            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }
        }

        #endregion

        #region Change Tracking

        private void AttachChangeHandlers()
        {
            DefaultServerCombo.SelectionChanged += (s, e) => _isDirty = true;
            ProxyHostText.TextChanged += (s, e) => _isDirty = true;
            ProxyPortText.TextChanged += (s, e) => _isDirty = true;
            AutoStartFiddlerCheck.Checked += (s, e) => _isDirty = true;
            AutoStartFiddlerCheck.Unchecked += (s, e) => _isDirty = true;
            RememberLayoutCheck.Checked += (s, e) => _isDirty = true;
            RememberLayoutCheck.Unchecked += (s, e) => _isDirty = true;
            EnableLoggingCheck.Checked += (s, e) => _isDirty = true;
            EnableLoggingCheck.Unchecked += (s, e) => _isDirty = true;
            AutoEvonySwfPathText.TextChanged += (s, e) => _isDirty = true;
            EvonyClientSwfPathText.TextChanged += (s, e) => _isDirty = true;
            FlashPluginPathText.TextChanged += (s, e) => _isDirty = true;
        }

        #endregion

        #region Validation

        private bool ValidateSettings(out string errorMessage)
        {
            errorMessage = null;

            // Validate proxy host
            var proxyHost = ProxyHostText.Text?.Trim();
            if (string.IsNullOrWhiteSpace(proxyHost))
            {
                errorMessage = "Proxy host cannot be empty.";
                ProxyHostText.Focus();
                return false;
            }

            // Validate proxy port
            var portText = ProxyPortText.Text?.Trim();
            if (string.IsNullOrWhiteSpace(portText))
            {
                errorMessage = "Proxy port cannot be empty.";
                ProxyPortText.Focus();
                return false;
            }

            if (!int.TryParse(portText, out int port))
            {
                errorMessage = "Proxy port must be a valid number.";
                ProxyPortText.Focus();
                return false;
            }

            if (port < 1 || port > 65535)
            {
                errorMessage = "Proxy port must be between 1 and 65535.";
                ProxyPortText.Focus();
                return false;
            }

            // Validate SWF paths if provided
            var autoEvonyPath = AutoEvonySwfPathText.Text?.Trim();
            if (!string.IsNullOrEmpty(autoEvonyPath) && !File.Exists(autoEvonyPath))
            {
                errorMessage = $"AutoEvony.swf file not found at:\n{autoEvonyPath}";
                AutoEvonySwfPathText.Focus();
                return false;
            }

            var evonyClientPath = EvonyClientSwfPathText.Text?.Trim();
            if (!string.IsNullOrEmpty(evonyClientPath) && !File.Exists(evonyClientPath))
            {
                errorMessage = $"EvonyClient.swf file not found at:\n{evonyClientPath}";
                EvonyClientSwfPathText.Focus();
                return false;
            }

            var flashPluginPath = FlashPluginPathText.Text?.Trim();
            if (!string.IsNullOrEmpty(flashPluginPath) && !File.Exists(flashPluginPath))
            {
                errorMessage = $"Flash plugin file not found at:\n{flashPluginPath}";
                FlashPluginPathText.Focus();
                return false;
            }

            return true;
        }

        #endregion

        #region Browse Button Handlers

        private void BrowseAutoEvony_Click(object sender, RoutedEventArgs e)
        {
            var path = BrowseForSwfFile("Select AutoEvony.swf", "AutoEvony.swf");
            if (!string.IsNullOrEmpty(path))
            {
                AutoEvonySwfPathText.Text = path;
                _isDirty = true;
            }
        }

        private void BrowseEvonyClient_Click(object sender, RoutedEventArgs e)
        {
            var path = BrowseForSwfFile("Select EvonyClient.swf", "EvonyClient1921.swf");
            if (!string.IsNullOrEmpty(path))
            {
                EvonyClientSwfPathText.Text = path;
                _isDirty = true;
            }
        }

        private void BrowseFlashPlugin_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Flash Plugin",
                Filter = "Flash Plugin (*.dll)|*.dll|All Files (*.*)|*.*",
                FileName = "pepflashplayer.dll"
            };

            // Start in Assets/Plugins folder (where pepflashplayer.dll should be)
            var assetsPluginsPath = Path.Combine(App.BasePath, "Assets", "Plugins");
            if (Directory.Exists(assetsPluginsPath))
            {
                dialog.InitialDirectory = assetsPluginsPath;
            }
            else
            {
                // Fallback to common Flash plugin locations
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var flashPath = Path.Combine(programFiles, "Adobe", "Flash Player", "NPAPI");
                if (Directory.Exists(flashPath))
                {
                    dialog.InitialDirectory = flashPath;
                }
            }

            if (dialog.ShowDialog() == true)
            {
                FlashPluginPathText.Text = dialog.FileName;
                _isDirty = true;
            }
        }

        private string BrowseForSwfFile(string title, string defaultFileName)
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = "SWF Files (*.swf)|*.swf|All Files (*.*)|*.*",
                FileName = defaultFileName
            };

            // Start in Downloads folder
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(downloads))
            {
                dialog.InitialDirectory = downloads;
            }

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        }

        #endregion

        #region Event Handlers

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate input
            if (!ValidateSettings(out var errorMessage))
            {
                MessageBox.Show(
                    errorMessage,
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                var settings = _sessionManager.Settings;

                // Server
                if (DefaultServerCombo.SelectedItem is ComboBoxItem serverItem)
                {
                    settings.DefaultServer = serverItem.Content?.ToString() ?? "cc2.evony.com";
                }

                // Proxy
                settings.ProxyHost = ProxyHostText.Text.Trim();
                settings.ProxyPort = int.Parse(ProxyPortText.Text.Trim());
                settings.AutoStartFiddler = AutoStartFiddlerCheck.IsChecked ?? true;

                // UI
                settings.RememberPanelLayout = RememberLayoutCheck.IsChecked ?? true;
                settings.EnableLogging = EnableLoggingCheck.IsChecked ?? true;

                // SWF paths
                settings.AutoEvonySwfPath = AutoEvonySwfPathText.Text?.Trim() ?? "";
                settings.EvonyClientSwfPath = EvonyClientSwfPathText.Text?.Trim() ?? "";
                settings.FlashPluginPath = FlashPluginPathText.Text?.Trim() ?? "";

                // Save to file
                _sessionManager.SaveSettings();
            
                App.Logger.Information("Settings saved successfully");
                App.Logger.Information("SWF paths configured:");
                App.Logger.Information("  AutoEvony: {Path}", settings.AutoEvonySwfPath);
                App.Logger.Information("  EvonyClient: {Path}", settings.EvonyClientSwfPath);
                App.Logger.Information("  FlashPlugin: {Path}", settings.FlashPluginPath);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to save settings");
                MessageBox.Show(
                    $"Failed to save settings:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Check for unsaved changes
            if (_isDirty)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Discard them?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            DialogResult = false;
            Close();
        }

        #endregion
    }

}