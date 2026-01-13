using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json;
using SvonyBrowser.Services;

namespace SvonyBrowser.Controls
{

    /// <summary>
    /// Interaction logic for TrafficViewer.xaml
    /// </summary>
    public partial class TrafficViewer : UserControl
    {
        #region Fields

        private readonly ObservableCollection<TrafficEntryViewModel> _entries = new ObservableCollection<TrafficEntryViewModel>();
        private readonly ObservableCollection<TrafficEntryViewModel> _filteredEntries = new ObservableCollection<TrafficEntryViewModel>();
        private TrafficPipeClient? _pipeClient;
        private bool _isCapturing = false;
        private string _filterText = "";
        private string _directionFilter = "All";

        #endregion

        #region Constructor

        public TrafficViewer()
        {
            InitializeComponent();
            TrafficDataGrid.ItemsSource = _filteredEntries;
        }

        #endregion

        #region Event Handlers

        private void StartCapture_Click(object sender, RoutedEventArgs e)
        {
            StartCapture();
        }

        private void StopCapture_Click(object sender, RoutedEventArgs e)
        {
            StopCapture();
        }

        private void ClearTraffic_Click(object sender, RoutedEventArgs e)
        {
            _entries.Clear();
            _filteredEntries.Clear();
            UpdateStatus();
            ClearDetails();
        }

        private void ExportTraffic_Click(object sender, RoutedEventArgs e)
        {
            ExportTraffic();
        }

        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _filterText = FilterTextBox.Text ?? "";
            ApplyFilter();
        }

        private void DirectionFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DirectionFilter.SelectedItem is ComboBoxItem item)
            {
                _directionFilter = item.Content?.ToString() ?? "All";
                ApplyFilter();
            }
        }

        private void TrafficDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TrafficDataGrid.SelectedItem is TrafficEntryViewModel entry)
            {
                ShowDetails(entry);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts traffic capture.
        /// </summary>
        public async void StartCapture()
        {
            if (_isCapturing) return;

            try
            {
                _pipeClient = new TrafficPipeClient();
                _pipeClient.TrafficReceived += OnTrafficReceived;
                _pipeClient.ConnectionStatusChanged += OnConnectionStatusChanged;
                _pipeClient.ErrorOccurred += OnErrorOccurred;

                await _pipeClient.StartAsync();

                _isCapturing = true;
                StartCaptureButton.IsEnabled = false;
                StopCaptureButton.IsEnabled = true;
                StatusText.Text = "Capturing...";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start capture: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Stops traffic capture.
        /// </summary>
        public void StopCapture()
        {
            if (!_isCapturing) return;

            _pipeClient?.Stop();
            _pipeClient?.Dispose();
            _pipeClient = null;

            _isCapturing = false;
            StartCaptureButton.IsEnabled = true;
            StopCaptureButton.IsEnabled = false;
            StatusText.Text = "Stopped";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
        }

        /// <summary>
        /// Adds a traffic entry manually.
        /// </summary>
        public void AddEntry(FiddlerTrafficData data)
        {
            Dispatcher.Invoke(() =>
            {
                var entry = new TrafficEntryViewModel(data);
                _entries.Add(entry);

                if (MatchesFilter(entry))
                {
                    _filteredEntries.Add(entry);
                }

                UpdateStatus();

                // Auto-scroll to bottom
                if (_filteredEntries.Count > 0)
                {
                    TrafficDataGrid.ScrollIntoView(_filteredEntries.Last());
                }
            });
        }

        #endregion

        #region Private Methods

        private void OnTrafficReceived(FiddlerTrafficData data)
        {
            AddEntry(data);
        }

        private void OnConnectionStatusChanged(bool connected)
        {
            Dispatcher.Invoke(() =>
            {
                if (connected)
                {
                    StatusText.Text = "Connected";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                }
                else
                {
                    StatusText.Text = "Disconnected";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                }
            });
        }

        private void OnErrorOccurred(string error)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"Error: {error}";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            });
        }

        private void ApplyFilter()
        {
            _filteredEntries.Clear();

            foreach (var entry in _entries)
            {
                if (MatchesFilter(entry))
                {
                    _filteredEntries.Add(entry);
                }
            }

            UpdateStatus();
        }

        private bool MatchesFilter(TrafficEntryViewModel entry)
        {
            // Direction filter
            if (_directionFilter == "Requests" && entry.Direction != "request")
                return false;
            if (_directionFilter == "Responses" && entry.Direction != "response")
                return false;

            // Text filter
            if (!string.IsNullOrEmpty(_filterText))
            {
                var lower = _filterText.ToLowerInvariant();
                if (!entry.Action?.ToLowerInvariant().Contains(lower) == true &&
                    !entry.Url?.ToLowerInvariant().Contains(lower) == true)
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdateStatus()
        {
            CountText.Text = $"{_filteredEntries.Count} / {_entries.Count} entries";
        }

        private void ShowDetails(TrafficEntryViewModel entry)
        {
            // Decoded view
            if (entry.Decoded != null)
            {
                DecodedTextBox.Text = JsonConvert.SerializeObject(entry.Decoded, Formatting.Indented);
            }
            else
            {
                DecodedTextBox.Text = "(Not decoded)";
            }

            // Raw hex view
            RawHexTextBox.Text = FormatHex(entry.HexData ?? "");

            // Headers view
            if (entry.Headers != null)
            {
                var headersText = string.Join("\n", entry.Headers.Select(h => $"{h.Key}: {h.Value}"));
                HeadersTextBox.Text = headersText;
            }
            else
            {
                HeadersTextBox.Text = "(No headers)";
            }
        }

        private void ClearDetails()
        {
            DecodedTextBox.Text = "";
            RawHexTextBox.Text = "";
            HeadersTextBox.Text = "";
        }

        private string FormatHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return "";

            var formatted = new System.Text.StringBuilder();
            var offset = 0;

            for (int i = 0; i < hex.Length; i += 32)
            {
                var chunk = hex.Substring(i, Math.Min(32, hex.Length - i));

                // Add offset
                formatted.Append($"{offset:X8}  ");

                // Add hex bytes
                for (int j = 0; j < chunk.Length; j += 2)
                {
                    formatted.Append(chunk.Substring(j, Math.Min(2, chunk.Length - j)));
                    formatted.Append(" ");
                    if (j == 14) formatted.Append(" ");
                }

                // Pad if needed
                var padding = 32 - chunk.Length;
                formatted.Append(new string(' ', padding / 2 * 3 + (padding > 16 ? 1 : 0)));

                // Add ASCII
                formatted.Append(" |");
                for (int j = 0; j < chunk.Length; j += 2)
                {
                    var byteStr = chunk.Substring(j, Math.Min(2, chunk.Length - j));
                    if (int.TryParse(byteStr, System.Globalization.NumberStyles.HexNumber, null, out int b))
                    {
                        formatted.Append(b >= 32 && b < 127 ? (char)b : '.');
                    }
                }
                formatted.Append("|\n");

                offset += 16;
            }

            return formatted.ToString();
        }

        private void ExportTraffic()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json",
                FileName = $"traffic-{DateTime.Now:yyyyMMdd-HHmmss}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var data = _entries.Select(e => e.ToTrafficData()).ToList();
                    var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    System.IO.File.WriteAllText(dialog.FileName, json);

                    MessageBox.Show($"Exported {_entries.Count} entries to {dialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// View model for traffic entries.
    /// </summary>
    public class TrafficEntryViewModel
    {
        private readonly FiddlerTrafficData _data;

        public TrafficEntryViewModel(FiddlerTrafficData data)
        {
            _data = data;
        }

        public string Id => _data.Id ?? "";
        public string Direction => _data.Direction;
        public string Url => _data.Url;
        public string Action => _data.Action ?? ExtractAction();
        public int? StatusCode => _data.StatusCode;
        public string HexData => _data.HexData;
        public object Decoded => _data.Decoded;
        public System.Collections.Generic.Dictionary<string, string> Headers => _data.Headers;

        public string TimestampFormatted =>
            DateTimeOffset.FromUnixTimeMilliseconds(_data.Timestamp).LocalDateTime.ToString("HH:mm:ss.fff");

        public string DirectionIcon => Direction == "request" ? "→" : "←";

        public Brush DirectionBrush => Direction == "request"
            ? new SolidColorBrush(Color.FromRgb(59, 130, 246))
            : new SolidColorBrush(Color.FromRgb(16, 185, 129));

        public string SizeFormatted
        {
            get
            {
                var bytes = _data.ContentLength;
                if (bytes < 1024) return $"{bytes} B";
                if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
            }
        }

        private string ExtractAction()
        {
            if (_data.Decoded is Newtonsoft.Json.Linq.JObject obj)
            {
                return obj["action"]?.ToString();
            }
            return null;
        }

        public FiddlerTrafficData ToTrafficData() => _data;
    }

}