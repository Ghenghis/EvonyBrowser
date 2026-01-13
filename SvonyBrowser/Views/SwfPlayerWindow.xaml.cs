using System;
using System.IO;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;
using Microsoft.Win32;
using SvonyBrowser.Models;

namespace SvonyBrowser.Views
{
    /// <summary>
    /// SWF Player Window - Loads and displays Flash SWF files.
    /// Uses swfplayer.html as the host page and executes JavaScript to load SWF content.
    /// </summary>
    public partial class SwfPlayerWindow : Window
    {
        private string _fileName;
        private bool _isPlayerLoaded;

        /// <summary>
        /// Gets or sets the SWF file path to load.
        /// </summary>
        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                if (_isPlayerLoaded && !string.IsNullOrEmpty(_fileName))
                {
                    LoadSwf(_fileName);
                }
            }
        }

        public SwfPlayerWindow()
        {
            InitializeComponent();
            _isPlayerLoaded = false;
            
            // Load the SWF player HTML page
            LoadSwfPlayer();
        }

        /// <summary>
        /// Loads the swfplayer.html page into the browser.
        /// </summary>
        private void LoadSwfPlayer()
        {
            try
            {
                if (File.Exists(GlobalData.SwfPlayerPath))
                {
                    // Use file:// protocol for local HTML file
                    var playerUri = new Uri(GlobalData.SwfPlayerPath).AbsoluteUri;
                    Browser.Address = playerUri;
                    StatusText.Text = "Loading SWF player...";
                    GlobalData.LogMessage("Loading SWF player from: " + playerUri);
                }
                else
                {
                    StatusText.Text = "Error: swfplayer.html not found";
                    GlobalData.LogMessage("ERROR: SwfPlayer not found at: " + GlobalData.SwfPlayerPath);
                    MessageBox.Show(
                        "SWF Player HTML file not found!\n\nExpected at:\n" + GlobalData.SwfPlayerPath,
                        "Missing File",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error loading player";
                GlobalData.LogMessage("ERROR loading SWF player: " + ex.Message);
            }
        }

        /// <summary>
        /// Called when the browser frame finishes loading.
        /// </summary>
        private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            if (e.Frame.IsMain)
            {
                _isPlayerLoaded = true;
                
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = "SWF Player ready";
                    
                    // If a file was set before the player loaded, load it now
                    if (!string.IsNullOrEmpty(_fileName))
                    {
                        LoadSwf(_fileName);
                    }
                });

                // Enable Flash content automatically - Flash settings handled by CefSettings in Program.cs
            }
        }

        /// <summary>
        /// Loads an SWF file by executing JavaScript in the browser.
        /// </summary>
        private void LoadSwf(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = "Error: File not found";
                        MessageBox.Show("SWF file not found:\n" + filePath, "File Not Found", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                    return;
                }

                // Convert to file:// URI for the browser
                var fileUri = new Uri(filePath).AbsoluteUri;
                
                // Execute JavaScript to load the SWF using Frame - cast to IWebBrowser for extension
                ((CefSharp.IWebBrowser)Browser).GetMainFrame()?.ExecuteJavaScriptAsync($"loadSwf('{fileUri}')");
                
                Dispatcher.Invoke(() =>
                {
                    FilePathText.Text = filePath;
                    Title = "SWF Player - " + Path.GetFileName(filePath);
                    StatusText.Text = "Playing: " + Path.GetFileName(filePath);
                });

                GlobalData.LogMessage("Loaded SWF: " + filePath);
            }
            catch (Exception ex)
            {
                GlobalData.LogMessage("ERROR loading SWF: " + ex.Message);
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = "Error loading SWF";
                });
            }
        }

        /// <summary>
        /// Opens a file dialog to select an SWF file.
        /// </summary>
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "SWF Files (*.swf)|*.swf|All Files (*.*)|*.*",
                Title = "Select SWF File"
            };

            if (dialog.ShowDialog() == true)
            {
                FileName = dialog.FileName;
            }
        }

        /// <summary>
        /// Reloads the current SWF file.
        /// </summary>
        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_fileName))
            {
                LoadSwf(_fileName);
            }
            else
            {
                LoadSwfPlayer();
            }
        }
    }
}
