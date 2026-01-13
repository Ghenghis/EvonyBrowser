using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
    using SvonyBrowser.Helpers;

namespace SvonyBrowser.Controls
{
    /// <summary>
    /// StatusBarV4 - Advanced customizable status bar with real-time widgets
    /// Supports MCP progress, LLM stats, network monitoring, game state, and automation
    /// </summary>
    public partial class StatusBarV4 : UserControl, INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler ConfigureRequested;
        #endregion

        #region Private Fields
        private readonly DispatcherTimer _updateTimer;
        private readonly Queue<double> _packetHistory = new Queue<double>();
        private readonly int _sparklinePoints = 20;
        private DateTime _startTime;
        private int _totalPackets;
        private int _decodedPackets;
        private int _learnedActions;
        private CancellationTokenSource _updateCts;
        #endregion

        #region Widget State
        private readonly Dictionary<string, WidgetState> _widgetStates = new Dictionary<string, WidgetState>();
        #endregion

        public StatusBarV4()
        {
            InitializeComponent();
            
            _startTime = DateTime.Now;
            
            // Initialize sparkline history
            for (int i = 0; i < _sparklinePoints; i++)
            {
                _packetHistory.Enqueue(0);
            }
            
            // Initialize widget states
            InitializeWidgetStates();
            
            // Setup update timer (100ms for smooth animations)
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
            
            // Start background updates
            StartBackgroundUpdates();
        }

        #region Initialization
        private void InitializeWidgetStates()
        {
            // MCP Widgets
            _widgetStates["rag_progress"] = new WidgetState { Value = 0, MaxValue = 100, IsVisible = true };
            _widgetStates["rte_progress"] = new WidgetState { Value = 0, MaxValue = 100, IsVisible = true };
            _widgetStates["mcp_status"] = new WidgetState { Status = ConnectionStatus.Connecting, IsVisible = true };
            
            // LLM Widgets
            _widgetStates["llm_tokens"] = new WidgetState { Value = 0, IsVisible = true };
            _widgetStates["llm_vram"] = new WidgetState { Value = 0, MaxValue = 24, IsVisible = true };
            _widgetStates["gpu_temp"] = new WidgetState { Value = 0, IsVisible = true };
            _widgetStates["inference"] = new WidgetState { Value = 0, MaxValue = 100, IsVisible = true };
            
            // Network Widgets
            _widgetStates["packets_sec"] = new WidgetState { Value = 0, IsVisible = true };
            _widgetStates["decode_rate"] = new WidgetState { Value = 100, MaxValue = 100, IsVisible = true };
            _widgetStates["fiddler_status"] = new WidgetState { Status = ConnectionStatus.Connecting, IsVisible = true };
            _widgetStates["throughput_down"] = new WidgetState { Value = 0, IsVisible = true };
            _widgetStates["throughput_up"] = new WidgetState { Value = 0, IsVisible = true };
            
            // Game State Widgets
            _widgetStates["food_rate"] = new WidgetState { Value = 0, IsVisible = true };
            _widgetStates["wood_rate"] = new WidgetState { Value = 0, IsVisible = true };
            _widgetStates["stone_rate"] = new WidgetState { Value = 0, IsVisible = true };
            _widgetStates["iron_rate"] = new WidgetState { Value = 0, IsVisible = true };
            _widgetStates["troop_count"] = new WidgetState { Value = 0, IsVisible = true };
            _widgetStates["march_count"] = new WidgetState { Value = 0, MaxValue = 8, IsVisible = true };
            _widgetStates["power"] = new WidgetState { Value = 0, IsVisible = true };
            _widgetStates["build_progress"] = new WidgetState { Value = 0, MaxValue = 100, IsVisible = true };
            _widgetStates["research_progress"] = new WidgetState { Value = 0, MaxValue = 100, IsVisible = true };
            _widgetStates["training_progress"] = new WidgetState { Value = 0, MaxValue = 100, IsVisible = true };
            
            // Automation Widgets
            _widgetStates["autopilot"] = new WidgetState { Status = ConnectionStatus.Disconnected, IsVisible = true };
            _widgetStates["queue_size"] = new WidgetState { Value = 0, IsVisible = true };
            _widgetStates["fuzz_progress"] = new WidgetState { Value = 0, MaxValue = 100, IsVisible = true };
        }

        private void StartBackgroundUpdates()
        {
            _updateCts = new CancellationTokenSource();
            Task.Run(async () => await BackgroundUpdateLoop(_updateCts.Token));
        }

        private async Task BackgroundUpdateLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Update uptime
                    var uptime = DateTime.Now - _startTime;
                    await Dispatcher.InvokeAsync(() =>
                    {
                        UptimeText.Text = $"Uptime: {uptime:hh\\:mm\\:ss}";
                    });
                    
                    await Task.Delay(1000, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Ignore errors in background loop
                }
            }
        }
        #endregion

        #region Public Update Methods
        
        /// <summary>
        /// Update RAG progress bar
        /// </summary>
        public void UpdateRagProgress(double progress, string text = null)
        {
            Dispatcher.Invoke(() =>
            {
                RagProgressBar.Value = MathEx.Clamp(progress, 0, 100);
                RagProgressText.Text = text ?? $"{progress:F0}%";
                _widgetStates["rag_progress"].Value = progress;
            });
        }

        /// <summary>
        /// Update RTE progress bar
        /// </summary>
        public void UpdateRteProgress(double progress, string text = null)
        {
            Dispatcher.Invoke(() =>
            {
                RteProgressBar.Value = MathEx.Clamp(progress, 0, 100);
                RteProgressText.Text = text ?? $"{progress:F0}%";
                _widgetStates["rte_progress"].Value = progress;
            });
        }

        /// <summary>
        /// Update MCP connection status
        /// </summary>
        public void UpdateMcpStatus(ConnectionStatus status, string text = null)
        {
            Dispatcher.Invoke(() =>
            {
                McpStatusLight.Fill = GetStatusBrush(status);
                McpStatusText.Text = text ?? status.ToString();
                _widgetStates["mcp_status"].Status = status;
            });
        }

        /// <summary>
        /// Update LLM tokens per second
        /// </summary>
        public void UpdateTokensPerSec(double tokensPerSec)
        {
            Dispatcher.Invoke(() =>
            {
                TokensPerSecText.Text = tokensPerSec.ToString("F1");
                _widgetStates["llm_tokens"].Value = tokensPerSec;
            });
        }

        /// <summary>
        /// Update VRAM usage
        /// </summary>
        public void UpdateVramUsage(double usedGb, double totalGb = 24.0)
        {
            Dispatcher.Invoke(() =>
            {
                var percentage = (usedGb / totalGb) * 100;
                VramProgressBar.Value = percentage;
                VramText.Text = $"{usedGb:F1}GB";
                _widgetStates["llm_vram"].Value = usedGb;
                
                // Change color based on usage
                if (percentage > 90)
                    VramProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                else if (percentage > 75)
                    VramProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Yellow
                else
                    VramProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(6, 182, 212)); // Cyan
            });
        }

        /// <summary>
        /// Update GPU temperature
        /// </summary>
        public void UpdateGpuTemp(int tempCelsius)
        {
            Dispatcher.Invoke(() =>
            {
                GpuTempText.Text = $"{tempCelsius}°C";
                _widgetStates["gpu_temp"].Value = tempCelsius;
                
                // Change color based on temperature
                if (tempCelsius > 85)
                    GpuTempText.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                else if (tempCelsius > 75)
                    GpuTempText.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Yellow
                else
                    GpuTempText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
            });
        }

        /// <summary>
        /// Update inference progress
        /// </summary>
        public void UpdateInferenceProgress(double progress, string status = null)
        {
            Dispatcher.Invoke(() =>
            {
                InferenceProgressBar.Value = MathEx.Clamp(progress, 0, 100);
                InferenceText.Text = status ?? (progress > 0 ? $"{progress:F0}%" : "Idle");
                _widgetStates["inference"].Value = progress;
            });
        }

        /// <summary>
        /// Update packets per second with sparkline
        /// </summary>
        public void UpdatePacketsPerSec(double packetsPerSec)
        {
            Dispatcher.Invoke(() =>
            {
                PacketsPerSecText.Text = packetsPerSec.ToString("F0");
                _widgetStates["packets_sec"].Value = packetsPerSec;
                
                // Update sparkline
                _packetHistory.Dequeue();
                _packetHistory.Enqueue(packetsPerSec);
                DrawSparkline();
            });
        }

        /// <summary>
        /// Update decode rate
        /// </summary>
        public void UpdateDecodeRate(double rate)
        {
            Dispatcher.Invoke(() =>
            {
                DecodeRateBar.Value = MathEx.Clamp(rate, 0, 100);
                DecodeRateText.Text = $"{rate:F0}%";
                _widgetStates["decode_rate"].Value = rate;
                
                // Change color based on rate
                if (rate < 80)
                    DecodeRateBar.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                else if (rate < 95)
                    DecodeRateBar.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Yellow
                else
                    DecodeRateBar.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
            });
        }

        /// <summary>
        /// Update Fiddler status
        /// </summary>
        public void UpdateFiddlerStatus(ConnectionStatus status, string text = null)
        {
            Dispatcher.Invoke(() =>
            {
                FiddlerStatusLight.Fill = GetStatusBrush(status);
                FiddlerStatusText.Text = text ?? "Fiddler";
                _widgetStates["fiddler_status"].Status = status;
            });
        }

        /// <summary>
        /// Update network throughput
        /// </summary>
        public void UpdateThroughput(double downloadKbps, double uploadKbps)
        {
            Dispatcher.Invoke(() =>
            {
                DownloadText.Text = FormatThroughput(downloadKbps);
                UploadText.Text = FormatThroughput(uploadKbps);
                _widgetStates["throughput_down"].Value = downloadKbps;
                _widgetStates["throughput_up"].Value = uploadKbps;
            });
        }

        /// <summary>
        /// Update resource rates
        /// </summary>
        public void UpdateResourceRates(long food, long wood, long stone, long iron)
        {
            Dispatcher.Invoke(() =>
            {
                FoodRateText.Text = FormatRate(food);
                WoodRateText.Text = FormatRate(wood);
                StoneRateText.Text = FormatRate(stone);
                IronRateText.Text = FormatRate(iron);
            });
        }

        /// <summary>
        /// Update troop count
        /// </summary>
        public void UpdateTroopCount(long count)
        {
            Dispatcher.Invoke(() =>
            {
                TroopCountText.Text = FormatNumber(count);
                _widgetStates["troop_count"].Value = count;
            });
        }

        /// <summary>
        /// Update march count
        /// </summary>
        public void UpdateMarchCount(int current, int max)
        {
            Dispatcher.Invoke(() =>
            {
                MarchCountText.Text = $"{current}/{max}";
                _widgetStates["march_count"].Value = current;
                _widgetStates["march_count"].MaxValue = max;
            });
        }

        /// <summary>
        /// Update power
        /// </summary>
        public void UpdatePower(long power)
        {
            Dispatcher.Invoke(() =>
            {
                PowerText.Text = FormatNumber(power);
                _widgetStates["power"].Value = power;
            });
        }

        /// <summary>
        /// Update build progress
        /// </summary>
        public void UpdateBuildProgress(double progress, TimeSpan? remaining = null)
        {
            Dispatcher.Invoke(() =>
            {
                BuildProgressBar.Value = MathEx.Clamp(progress, 0, 100);
                BuildTimeText.Text = remaining.HasValue ? FormatTime(remaining.Value) : "--:--";
                _widgetStates["build_progress"].Value = progress;
            });
        }

        /// <summary>
        /// Update research progress
        /// </summary>
        public void UpdateResearchProgress(double progress, TimeSpan? remaining = null)
        {
            Dispatcher.Invoke(() =>
            {
                ResearchProgressBar.Value = MathEx.Clamp(progress, 0, 100);
                ResearchTimeText.Text = remaining.HasValue ? FormatTime(remaining.Value) : "--:--";
                _widgetStates["research_progress"].Value = progress;
            });
        }

        /// <summary>
        /// Update training progress
        /// </summary>
        public void UpdateTrainingProgress(double progress, TimeSpan? remaining = null)
        {
            Dispatcher.Invoke(() =>
            {
                TrainingProgressBar.Value = MathEx.Clamp(progress, 0, 100);
                TrainingTimeText.Text = remaining.HasValue ? FormatTime(remaining.Value) : "--:--";
                _widgetStates["training_progress"].Value = progress;
            });
        }

        /// <summary>
        /// Update autopilot status
        /// </summary>
        public void UpdateAutoPilotStatus(ConnectionStatus status, string text = null)
        {
            Dispatcher.Invoke(() =>
            {
                AutoPilotLight.Fill = GetStatusBrush(status);
                AutoPilotText.Text = text ?? (status == ConnectionStatus.Connected ? "On" : "Off");
                _widgetStates["autopilot"].Status = status;
            });
        }

        /// <summary>
        /// Update automation queue size
        /// </summary>
        public void UpdateQueueSize(int size)
        {
            Dispatcher.Invoke(() =>
            {
                QueueSizeText.Text = size.ToString();
                _widgetStates["queue_size"].Value = size;
            });
        }

        /// <summary>
        /// Update fuzzing progress
        /// </summary>
        public void UpdateFuzzProgress(double progress, string text = null)
        {
            Dispatcher.Invoke(() =>
            {
                FuzzProgressBar.Value = MathEx.Clamp(progress, 0, 100);
                FuzzProgressText.Text = text ?? $"{progress:F0}%";
                _widgetStates["fuzz_progress"].Value = progress;
            });
        }

        /// <summary>
        /// Update packet statistics
        /// </summary>
        public void UpdatePacketStats(int total, int decoded, int learned)
        {
            _totalPackets = total;
            _decodedPackets = decoded;
            _learnedActions = learned;
            
            Dispatcher.Invoke(() =>
            {
                TotalPacketsText.Text = $"Total Packets: {total:N0}";
                DecodedPacketsText.Text = $"Decoded: {decoded:N0}";
                LearnedActionsText.Text = $"Learned Actions: {learned}";
            });
        }

        /// <summary>
        /// Update session info
        /// </summary>
        public void UpdateSessionInfo(string sessionId, DateTime? lastActivity = null)
        {
            Dispatcher.Invoke(() =>
            {
                SessionIdText.Text = $"Session: {sessionId}";
                LastActivityText.Text = lastActivity.HasValue 
                    ? $"Last Activity: {lastActivity.Value:HH:mm:ss}" 
                    : "Last Activity: --";
            });
        }

        /// <summary>
        /// Toggle detail row visibility
        /// </summary>
        public void ToggleDetailRow()
        {
            Dispatcher.Invoke(() =>
            {
                DetailRow.Visibility = DetailRow.Visibility == Visibility.Visible 
                    ? Visibility.Collapsed 
                    : Visibility.Visible;
            });
        }

        #endregion

        #region Private Methods
        
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            // Smooth animations and periodic updates
        }

        private void DrawSparkline()
        {
            PacketSparkline.Children.Clear();
            
            var points = _packetHistory.ToArray();
            var maxValue = Math.Max(points.Max(), 1);
            var width = PacketSparkline.Width;
            var height = PacketSparkline.Height;
            var stepX = width / (_sparklinePoints - 1);
            
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                StrokeThickness = 1.5,
                StrokeLineJoin = PenLineJoin.Round
            };
            
            for (int i = 0; i < points.Length; i++)
            {
                var x = i * stepX;
                var y = height - (points[i] / maxValue * height);
                polyline.Points.Add(new Point(x, y));
            }
            
            PacketSparkline.Children.Add(polyline);
        }

        private SolidColorBrush GetStatusBrush(ConnectionStatus status)
        {
            return status switch
            {
                ConnectionStatus.Connected => new SolidColorBrush(Color.FromRgb(16, 185, 129)),    // Green
                ConnectionStatus.Connecting => new SolidColorBrush(Color.FromRgb(245, 158, 11)),  // Yellow
                ConnectionStatus.Disconnected => new SolidColorBrush(Color.FromRgb(239, 68, 68)), // Red
                ConnectionStatus.Error => new SolidColorBrush(Color.FromRgb(239, 68, 68)),        // Red
                _ => new SolidColorBrush(Color.FromRgb(100, 116, 139))                            // Gray
            };
        }

        private string FormatThroughput(double kbps)
        {
            if (kbps >= 1000)
                return $"{kbps / 1000:F1}M";
            return $"{kbps:F0}";
        }

        private string FormatRate(long rate)
        {
            var prefix = rate >= 0 ? "+" : "";
            if (Math.Abs(rate) >= 1000000)
                return $"{prefix}{rate / 1000000.0:F1}M";
            if (Math.Abs(rate) >= 1000)
                return $"{prefix}{rate / 1000.0:F1}K";
            return $"{prefix}{rate}";
        }

        private string FormatNumber(long number)
        {
            if (number >= 1000000000)
                return $"{number / 1000000000.0:F1}B";
            if (number >= 1000000)
                return $"{number / 1000000.0:F1}M";
            if (number >= 1000)
                return $"{number / 1000.0:F1}K";
            return number.ToString();
        }

        private string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
            return $"{time.Minutes:D2}:{time.Seconds:D2}";
        }

        private void ConfigureStatusBar_Click(object sender, RoutedEventArgs e)
        {
            ConfigureRequested?.Invoke(this, EventArgs.Empty);
            
            // Toggle detail row as a quick action
            ToggleDetailRow();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Cleanup
        
        public void Dispose()
        {
            _updateTimer.Stop();
            _updateCts?.Cancel();
            _updateCts?.Dispose();
        }

        #endregion
    }

    #region Supporting Types

    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    public class WidgetState
    {
        public double Value { get; set; }
        public double MaxValue { get; set; } = 100;
        public ConnectionStatus Status { get; set; }
        public bool IsVisible { get; set; } = true;
        public string DisplayText { get; set; }
    }

    #endregion
}
