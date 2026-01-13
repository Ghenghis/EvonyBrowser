using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SvonyBrowser.Services
{
    /// <summary>
    /// Manages the customizable Status Bar system with real-time widgets
    /// </summary>
    public class StatusBarManager : INotifyPropertyChanged, IDisposable
    {
        private static readonly Lazy<StatusBarManager> _lazyInstance =
            new Lazy<StatusBarManager>(() => new StatusBarManager(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static StatusBarManager Instance => _lazyInstance.Value;

        private readonly Dictionary<string, StatusBarWidget> _widgets = new StatusBarWidget>();
        private readonly Dictionary<string, IWidgetDataProvider> _dataProviders = new IWidgetDataProvider>();
        private readonly List<StatusBarRow> _rows = new List<StatusBarRow>();
        private readonly DispatcherTimer _updateTimer;
        private readonly object _lock = new object();
        private bool _isDisposed;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<WidgetUpdatedEventArgs> WidgetUpdated;
        public event EventHandler<StatusBarConfigChangedEventArgs> ConfigChanged;

        public ObservableCollection<StatusBarWidget> ActiveWidgets { get; } = new ObservableCollection<StatusBarWidget>();
        public int RowCount => _rows.Count;
        public bool IsRunning { get; private set; }

        private StatusBarManager()
        {
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // Base update rate
            };
            _updateTimer.Tick += OnUpdateTick;

            InitializeDefaultWidgets();
            InitializeRows(3); // Default 3 rows
        }

        #region Initialization

        private void InitializeDefaultWidgets()
        {
            // MCP Server Widgets
            RegisterWidget(new StatusBarWidget
            {
                Id = "rag-progress",
                Title = "RAG",
                Type = WidgetType.ProgressBar,
                Category = WidgetCategory.MCP,
                RefreshRateMs = 500,
                ColorScheme = "purple",
                DefaultRow = 0,
                DefaultPosition = 0
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "rte-progress",
                Title = "RTE",
                Type = WidgetType.ProgressBar,
                Category = WidgetCategory.MCP,
                RefreshRateMs = 100,
                ColorScheme = "blue",
                DefaultRow = 0,
                DefaultPosition = 1
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "mcp-status",
                Title = "MCP",
                Type = WidgetType.TrafficLight,
                Category = WidgetCategory.MCP,
                RefreshRateMs = 1000,
                DefaultRow = 0,
                DefaultPosition = 2
            });

            // LLM Widgets
            RegisterWidget(new StatusBarWidget
            {
                Id = "llm-inference",
                Title = "LLM",
                Type = WidgetType.ProgressBar,
                Category = WidgetCategory.LLM,
                RefreshRateMs = 100,
                ColorScheme = "green",
                DefaultRow = 0,
                DefaultPosition = 3
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "gpu-temp",
                Title = "GPU",
                Type = WidgetType.Temperature,
                Category = WidgetCategory.LLM,
                RefreshRateMs = 2000,
                DefaultRow = 0,
                DefaultPosition = 4
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "vram-usage",
                Title = "VRAM",
                Type = WidgetType.ProgressBar,
                Category = WidgetCategory.LLM,
                RefreshRateMs = 1000,
                ColorScheme = "orange",
                DefaultRow = 0,
                DefaultPosition = 5
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "token-rate",
                Title = "Tokens/s",
                Type = WidgetType.Sparkline,
                Category = WidgetCategory.LLM,
                RefreshRateMs = 500,
                DefaultRow = 0,
                DefaultPosition = 6
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "model-name",
                Title = "Model",
                Type = WidgetType.Label,
                Category = WidgetCategory.LLM,
                RefreshRateMs = 5000,
                DefaultRow = 0,
                DefaultPosition = 7
            });

            // Game State Widgets
            RegisterWidget(new StatusBarWidget
            {
                Id = "build-progress",
                Title = "Build",
                Type = WidgetType.ProgressBar,
                Category = WidgetCategory.GameState,
                RefreshRateMs = 1000,
                ColorScheme = "yellow",
                DefaultRow = 1,
                DefaultPosition = 0
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "train-progress",
                Title = "Train",
                Type = WidgetType.ProgressBar,
                Category = WidgetCategory.GameState,
                RefreshRateMs = 1000,
                ColorScheme = "red",
                DefaultRow = 1,
                DefaultPosition = 1
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "research-progress",
                Title = "Research",
                Type = WidgetType.ProgressBar,
                Category = WidgetCategory.GameState,
                RefreshRateMs = 1000,
                ColorScheme = "cyan",
                DefaultRow = 1,
                DefaultPosition = 2
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "march-count",
                Title = "Marches",
                Type = WidgetType.Counter,
                Category = WidgetCategory.GameState,
                RefreshRateMs = 500,
                DefaultRow = 1,
                DefaultPosition = 3
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "stamina",
                Title = "Stamina",
                Type = WidgetType.ProgressBar,
                Category = WidgetCategory.GameState,
                RefreshRateMs = 1000,
                ColorScheme = "green",
                DefaultRow = 1,
                DefaultPosition = 4
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "resource-rate",
                Title = "Income/hr",
                Type = WidgetType.Label,
                Category = WidgetCategory.GameState,
                RefreshRateMs = 5000,
                DefaultRow = 1,
                DefaultPosition = 5
            });

            // Network & Packet Widgets
            RegisterWidget(new StatusBarWidget
            {
                Id = "packet-rate",
                Title = "Packets/s",
                Type = WidgetType.Sparkline,
                Category = WidgetCategory.Network,
                RefreshRateMs = 100,
                DefaultRow = 2,
                DefaultPosition = 0
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "latency",
                Title = "Latency",
                Type = WidgetType.Label,
                Category = WidgetCategory.Network,
                RefreshRateMs = 500,
                DefaultRow = 2,
                DefaultPosition = 1
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "decode-rate",
                Title = "Decoded",
                Type = WidgetType.Label,
                Category = WidgetCategory.Network,
                RefreshRateMs = 1000,
                DefaultRow = 2,
                DefaultPosition = 2
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "fiddler-status",
                Title = "Fiddler",
                Type = WidgetType.TrafficLight,
                Category = WidgetCategory.Network,
                RefreshRateMs = 2000,
                DefaultRow = 2,
                DefaultPosition = 3
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "proxy-throughput",
                Title = "Proxy KB/s",
                Type = WidgetType.Sparkline,
                Category = WidgetCategory.Network,
                RefreshRateMs = 500,
                DefaultRow = 2,
                DefaultPosition = 4
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "error-count",
                Title = "Errors",
                Type = WidgetType.Counter,
                Category = WidgetCategory.Network,
                RefreshRateMs = 1000,
                DefaultRow = 2,
                DefaultPosition = 5
            });

            // Automation Widgets
            RegisterWidget(new StatusBarWidget
            {
                Id = "autopilot-status",
                Title = "AutoPilot",
                Type = WidgetType.Label,
                Category = WidgetCategory.Automation,
                RefreshRateMs = 1000,
                DefaultRow = 2,
                DefaultPosition = 6
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "queue-depth",
                Title = "Queue",
                Type = WidgetType.Counter,
                Category = WidgetCategory.Automation,
                RefreshRateMs = 500,
                DefaultRow = 2,
                DefaultPosition = 7
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "action-rate",
                Title = "Actions/min",
                Type = WidgetType.Sparkline,
                Category = WidgetCategory.Automation,
                RefreshRateMs = 1000,
                DefaultRow = 2,
                DefaultPosition = 8
            });

            RegisterWidget(new StatusBarWidget
            {
                Id = "session-time",
                Title = "Session",
                Type = WidgetType.Timer,
                Category = WidgetCategory.Automation,
                RefreshRateMs = 1000,
                DefaultRow = 2,
                DefaultPosition = 9
            });

            // System Widgets
            RegisterWidget(new StatusBarWidget
            {
                Id = "memory-usage",
                Title = "Memory",
                Type = WidgetType.ProgressBar,
                Category = WidgetCategory.System,
                RefreshRateMs = 2000,
                ColorScheme = "gray",
                DefaultRow = 2,
                DefaultPosition = 10
            });
        }

        private void InitializeRows(int count)
        {
            _rows.Clear();
            for (int i = 0; i < count; i++)
            {
                _rows.Add(new StatusBarRow { Index = i, Widgets = new List<StatusBarWidget>() });
            }
        }

        #endregion

        #region Widget Management

        public void RegisterWidget(StatusBarWidget widget)
        {
            lock (_lock)
            {
                _widgets[widget.Id] = widget;
            }
        }

        public void RegisterDataProvider(string widgetId, IWidgetDataProvider provider)
        {
            lock (_lock)
            {
                _dataProviders[widgetId] = provider;
            }
        }

        public void ActivateWidget(string widgetId, int row, int position)
        {
            lock (_lock)
            {
                if (!_widgets.TryGetValue(widgetId, out var widget))
                    return;

                widget.Row = row;
                widget.Position = position;
                widget.IsActive = true;

                if (!ActiveWidgets.Contains(widget))
                {
                    ActiveWidgets.Add(widget);
                }

                if (row < _rows.Count)
                {
                    _rows[row].Widgets.Add(widget);
                    _rows[row].Widgets = _rows[row].Widgets.OrderBy(w => w.Position).ToList();
                }
            }

            ConfigChanged?.Invoke(this, new StatusBarConfigChangedEventArgs { WidgetId = widgetId, Action = "activate" });
        }

        public void DeactivateWidget(string widgetId)
        {
            lock (_lock)
            {
                if (!_widgets.TryGetValue(widgetId, out var widget))
                    return;

                widget.IsActive = false;
                ActiveWidgets.Remove(widget);

                foreach (var row in _rows)
                {
                    row.Widgets.Remove(widget);
                }
            }

            ConfigChanged?.Invoke(this, new StatusBarConfigChangedEventArgs { WidgetId = widgetId, Action = "deactivate" });
        }

        public void MoveWidget(string widgetId, int newRow, int newPosition)
        {
            lock (_lock)
            {
                if (!_widgets.TryGetValue(widgetId, out var widget))
                    return;

                // Remove from current row
                foreach (var row in _rows)
                {
                    row.Widgets.Remove(widget);
                }

                // Add to new row
                widget.Row = newRow;
                widget.Position = newPosition;

                if (newRow < _rows.Count)
                {
                    _rows[newRow].Widgets.Add(widget);
                    _rows[newRow].Widgets = _rows[newRow].Widgets.OrderBy(w => w.Position).ToList();
                }
            }

            ConfigChanged?.Invoke(this, new StatusBarConfigChangedEventArgs { WidgetId = widgetId, Action = "move" });
        }

        public IEnumerable<StatusBarWidget> GetWidgetsForRow(int row)
        {
            lock (_lock)
            {
                if (row < _rows.Count)
                {
                    return _rows[row].Widgets.ToList();
                }
                return Enumerable.Empty<StatusBarWidget>();
            }
        }

        public IEnumerable<StatusBarWidget> GetAvailableWidgets()
        {
            lock (_lock)
            {
                return _widgets.Values.ToList();
            }
        }

        public IEnumerable<StatusBarWidget> GetWidgetsByCategory(WidgetCategory category)
        {
            lock (_lock)
            {
                return _widgets.Values.Where(w => w.Category == category).ToList();
            }
        }

        #endregion

        #region Update Loop

        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            _updateTimer.Start();
        }

        public void Stop()
        {
            IsRunning = false;
            _updateTimer.Stop();
        }

        private void OnUpdateTick(object sender, EventArgs e)
        {
            var now = DateTime.UtcNow;

            lock (_lock)
            {
                foreach (var widget in ActiveWidgets)
                {
                    if ((now - widget.LastUpdate).TotalMilliseconds >= widget.RefreshRateMs)
                    {
                        UpdateWidget(widget);
                        widget.LastUpdate = now;
                    }
                }
            }
        }

        private void UpdateWidget(StatusBarWidget widget)
        {
            if (_dataProviders.TryGetValue(widget.Id, out var provider))
            {
                try
                {
                    var data = provider.GetData();
                    widget.CurrentValue = data.Value;
                    widget.DisplayText = data.DisplayText;
                    widget.Progress = data.Progress;
                    widget.Status = data.Status;
                    widget.SparklineData = data.SparklineData;

                    WidgetUpdated?.Invoke(this, new WidgetUpdatedEventArgs { Widget = widget, Data = data });
                }
                catch (Exception ex)
                {
                    widget.Status = WidgetStatus.Error;
                    widget.DisplayText = "Error";
                }
            }
        }

        #endregion

        #region Configuration

        public void LoadConfiguration(string configPath)
        {
            try
            {
                var json = System.IO.File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<StatusBarConfiguration>(json);

                if (config != null)
                {
                    InitializeRows(config.RowCount);

                    foreach (var widgetConfig in config.Widgets)
                    {
                        if (_widgets.TryGetValue(widgetConfig.Id, out var widget))
                        {
                            widget.RefreshRateMs = widgetConfig.RefreshRateMs;
                            widget.ColorScheme = widgetConfig.ColorScheme;
                            widget.ShowLabel = widgetConfig.ShowLabel;
                            widget.Width = widgetConfig.Width;

                            if (widgetConfig.IsActive)
                            {
                                ActivateWidget(widgetConfig.Id, widgetConfig.Row, widgetConfig.Position);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Load defaults on error
                LoadDefaultConfiguration();
            }
        }

        public void SaveConfiguration(string configPath)
        {
            var config = new StatusBarConfiguration
            {
                RowCount = _rows.Count,
                Widgets = ActiveWidgets.Select(w => new WidgetConfiguration
                {
                    Id = w.Id,
                    Row = w.Row,
                    Position = w.Position,
                    RefreshRateMs = w.RefreshRateMs,
                    ColorScheme = w.ColorScheme,
                    ShowLabel = w.ShowLabel,
                    Width = w.Width,
                    IsActive = w.IsActive
                }).ToList()
            };

            var json = JsonConvert.SerializeObject(config, new JsonSerializerSettings { Formatting = Formatting.Indented });
            System.IO.File.WriteAllText(configPath, json);
        }

        public void LoadDefaultConfiguration()
        {
            // Activate default widgets
            foreach (var widget in _widgets.Values)
            {
                ActivateWidget(widget.Id, widget.DefaultRow, widget.DefaultPosition);
            }
        }

        #endregion

        #region Data Provider Registration

        public void RegisterMcpDataProviders()
        {
            RegisterDataProvider("rag-progress", new RagProgressProvider());
            RegisterDataProvider("rte-progress", new RteProgressProvider());
            RegisterDataProvider("mcp-status", new McpStatusProvider());
        }

        public void RegisterLlmDataProviders()
        {
            RegisterDataProvider("llm-inference", new LlmInferenceProvider());
            RegisterDataProvider("gpu-temp", new GpuTemperatureProvider());
            RegisterDataProvider("vram-usage", new VramUsageProvider());
            RegisterDataProvider("token-rate", new TokenRateProvider());
            RegisterDataProvider("model-name", new ModelNameProvider());
        }

        public void RegisterGameStateDataProviders()
        {
            RegisterDataProvider("build-progress", new BuildProgressProvider());
            RegisterDataProvider("train-progress", new TrainProgressProvider());
            RegisterDataProvider("research-progress", new ResearchProgressProvider());
            RegisterDataProvider("march-count", new MarchCountProvider());
            RegisterDataProvider("stamina", new StaminaProvider());
            RegisterDataProvider("resource-rate", new ResourceRateProvider());
        }

        public void RegisterNetworkDataProviders()
        {
            RegisterDataProvider("packet-rate", new PacketRateProvider());
            RegisterDataProvider("latency", new LatencyProvider());
            RegisterDataProvider("decode-rate", new DecodeRateProvider());
            RegisterDataProvider("fiddler-status", new FiddlerStatusProvider());
            RegisterDataProvider("proxy-throughput", new ProxyThroughputProvider());
            RegisterDataProvider("error-count", new ErrorCountProvider());
        }

        public void RegisterAutomationDataProviders()
        {
            RegisterDataProvider("autopilot-status", new AutopilotStatusProvider());
            RegisterDataProvider("queue-depth", new QueueDepthProvider());
            RegisterDataProvider("action-rate", new ActionRateProvider());
            RegisterDataProvider("session-time", new SessionTimeProvider());
        }

        public void RegisterSystemDataProviders()
        {
            RegisterDataProvider("memory-usage", new MemoryUsageProvider());
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            Stop();
            _updateTimer.Stop();
            _widgets.Clear();
            _dataProviders.Clear();
            ActiveWidgets.Clear();
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #region Models

    public class StatusBarWidget : INotifyPropertyChanged
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public WidgetType Type { get; set; }
        public WidgetCategory Category { get; set; }
        public int RefreshRateMs { get; set; } = 1000;
        public string ColorScheme { get; set; } = "default";
        public bool ShowLabel { get; set; } = true;
        public string Width { get; set; } = "auto";
        public int Row { get; set; }
        public int Position { get; set; }
        public int DefaultRow { get; set; }
        public int DefaultPosition { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastUpdate { get; set; }

        // Current data
        private object _currentValue;
        public object CurrentValue
        {
            get => _currentValue;
            set { _currentValue = value; OnPropertyChanged(); }
        }

        private string _displayText = "";
        public string DisplayText
        {
            get => _displayText;
            set { _displayText = value; OnPropertyChanged(); }
        }

        private double _progress;
        public double Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); }
        }

        private WidgetStatus _status;
        public WidgetStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        private List<double> _sparklineData;
        public List<double> SparklineData
        {
            get => _sparklineData;
            set { _sparklineData = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StatusBarRow
    {
        public int Index { get; set; }
        public List<StatusBarWidget> Widgets { get; set; } = new List<StatusBarWidget>();
    }

    public enum WidgetType
    {
        ProgressBar,
        Label,
        Sparkline,
        TrafficLight,
        Counter,
        Timer,
        Temperature,
        Custom
    }

    public enum WidgetCategory
    {
        MCP,
        LLM,
        GameState,
        Network,
        Automation,
        System
    }

    public enum WidgetStatus
    {
        Normal,
        Warning,
        Error,
        Inactive
    }

    public class WidgetData
    {
        public object Value { get; set; }
        public string DisplayText { get; set; } = "";
        public double Progress { get; set; }
        public WidgetStatus Status { get; set; }
        public List<double> SparklineData { get; set; }
    }

    public class StatusBarConfiguration
    {
        public int RowCount { get; set; } = 3;
        public List<WidgetConfiguration> Widgets { get; set; } = new List<WidgetConfiguration>();
    }

    public class WidgetConfiguration
    {
        public string Id { get; set; } = "";
        public int Row { get; set; }
        public int Position { get; set; }
        public int RefreshRateMs { get; set; }
        public string ColorScheme { get; set; } = "default";
        public bool ShowLabel { get; set; } = true;
        public string Width { get; set; } = "auto";
        public bool IsActive { get; set; }
    }

    #endregion

    #region Events

    public class WidgetUpdatedEventArgs : EventArgs
    {
        public StatusBarWidget Widget { get; set; } = null!;
        public WidgetData Data { get; set; } = null!;
    }

    public class StatusBarConfigChangedEventArgs : EventArgs
    {
        public string WidgetId { get; set; } = "";
        public string Action { get; set; } = "";
    }

    #endregion

    #region Data Provider Interface

    public interface IWidgetDataProvider
    {
        WidgetData GetData();
    }

    #endregion

    #region Data Providers

    // MCP Providers
    public class RagProgressProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            try
            {
                var progress = McpConnectionManager.Instance.GetRagProgress();
                return new WidgetData
                {
                    Value = progress,
                    DisplayText = $"{progress:F0}%",
                    Progress = progress / 100.0,
                    Status = progress >= 100 ? WidgetStatus.Normal : WidgetStatus.Warning
                };
            }
            catch
            {
                return new WidgetData { Value = 0, DisplayText = "N/A", Status = WidgetStatus.Error };
            }
        }
    }

    public class RteProgressProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            try
            {
                var progress = McpConnectionManager.Instance.GetRteProgress();
                return new WidgetData
                {
                    Value = progress,
                    DisplayText = $"{progress:F0}%",
                    Progress = progress / 100.0,
                    Status = progress >= 100 ? WidgetStatus.Normal : WidgetStatus.Warning
                };
            }
            catch
            {
                return new WidgetData { Value = 0, DisplayText = "N/A", Status = WidgetStatus.Error };
            }
        }
    }

    public class McpStatusProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            var manager = McpConnectionManager.Instance;
            var connected = manager.IsConnected;
            return new WidgetData
            {
                Value = connected,
                DisplayText = connected ? "Connected" : "Disconnected",
                Status = connected ? WidgetStatus.Normal : WidgetStatus.Error
            };
        }
    }

    // LLM Providers
    public class LlmInferenceProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            try
            {
                var llm = LlmIntegrationService.Instance;
                var progress = llm.GetInferenceProgress();
                return new WidgetData
                {
                    Value = progress,
                    DisplayText = $"{progress:F0}%",
                    Progress = progress / 100.0,
                    Status = llm.IsConnected ? WidgetStatus.Normal : WidgetStatus.Warning
                };
            }
            catch
            {
                return new WidgetData { Value = 0, DisplayText = "N/A", Status = WidgetStatus.Error };
            }
        }
    }

    public class GpuTemperatureProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            try
            {
                var llm = LlmIntegrationService.Instance;
                var temp = (int)llm.GetGpuTemperature();
                return new WidgetData
                {
                    Value = temp,
                    DisplayText = $"{temp}°C",
                    Status = temp > 80 ? WidgetStatus.Warning : (temp > 90 ? WidgetStatus.Error : WidgetStatus.Normal)
                };
            }
            catch
            {
                return new WidgetData { Value = 0, DisplayText = "N/A", Status = WidgetStatus.Error };
            }
        }
    }

    public class VramUsageProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            try
            {
                var llm = LlmIntegrationService.Instance;
                var usedGb = llm.GetVramUsage();
                var totalGb = 24.0; // RTX 3090 Ti
                return new WidgetData
                {
                    Value = usedGb,
                    DisplayText = $"{usedGb:F1}/{totalGb:F0}GB",
                    Progress = usedGb / totalGb,
                    Status = usedGb / totalGb > 0.9 ? WidgetStatus.Warning : WidgetStatus.Normal
                };
            }
            catch
            {
                return new WidgetData { Value = 0, DisplayText = "N/A", Status = WidgetStatus.Error };
            }
        }
    }

    public class TokenRateProvider : IWidgetDataProvider
    {
        private readonly Queue<double> _history = new Queue<double>();

        public WidgetData GetData()
        {
            try
            {
                var llm = LlmIntegrationService.Instance;
                var rate = llm.GetTokensPerSecond();
                _history.Enqueue(rate);
                if (_history.Count > 20) _history.Dequeue();

                return new WidgetData
                {
                    Value = rate,
                    DisplayText = $"{rate:F0} t/s",
                    SparklineData = _history.ToList(),
                    Status = WidgetStatus.Normal
                };
            }
            catch
            {
                return new WidgetData { Value = 0, DisplayText = "N/A", Status = WidgetStatus.Error };
            }
        }
    }

    public class ModelNameProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            return new WidgetData
            {
                Value = "evony-re-7b",
                DisplayText = "evony-re-7b",
                Status = WidgetStatus.Normal
            };
        }
    }

    // Game State Providers
    public class BuildProgressProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            var engine = GameStateEngine.Instance;
            var progress = engine.GetBuildProgress();
            var timeStr = progress.TimeRemaining.TotalSeconds > 0 
                ? $"{progress.TimeRemaining:hh\\:mm\\:ss}" 
                : "Complete";
            return new WidgetData
            {
                Value = progress.Percentage,
                DisplayText = $"{progress.Percentage:F0}% ({timeStr})",
                Progress = progress.Percentage / 100.0,
                Status = WidgetStatus.Normal
            };
        }
    }

    public class TrainProgressProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            var engine = GameStateEngine.Instance;
            var progress = engine.GetTrainProgress();
            return new WidgetData
            {
                Value = progress.Percentage,
                DisplayText = $"{progress.Percentage:F0}%",
                Progress = progress.Percentage / 100.0,
                Status = WidgetStatus.Normal
            };
        }
    }

    public class ResearchProgressProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            var engine = GameStateEngine.Instance;
            var progress = engine.GetResearchProgress();
            return new WidgetData
            {
                Value = progress.Percentage,
                DisplayText = $"{progress.Percentage:F0}%",
                Progress = progress.Percentage / 100.0,
                Status = WidgetStatus.Normal
            };
        }
    }

    public class MarchCountProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            var engine = GameStateEngine.Instance;
            var marches = engine.GetActiveMarches();
            var maxMarches = engine.GetMaxMarches();
            return new WidgetData
            {
                Value = marches.Count,
                DisplayText = $"{marches.Count}/{maxMarches}",
                Progress = (double)marches.Count / maxMarches,
                Status = marches.Count >= maxMarches ? WidgetStatus.Warning : WidgetStatus.Normal
            };
        }
    }

    public class StaminaProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            var engine = GameStateEngine.Instance;
            var stamina = engine.GetHeroStamina();
            var maxStamina = engine.GetMaxStamina();
            return new WidgetData
            {
                Value = stamina,
                DisplayText = $"{stamina}/{maxStamina}",
                Progress = (double)stamina / maxStamina,
                Status = stamina < 50 ? WidgetStatus.Warning : WidgetStatus.Normal
            };
        }
    }

    public class ResourceRateProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            var engine = GameStateEngine.Instance;
            var rates = engine.GetResourceRates();
            var totalRate = rates.Values.Values.Sum();
            return new WidgetData
            {
                Value = totalRate,
                DisplayText = $"{totalRate:N0}/hr",
                Status = WidgetStatus.Normal
            };
        }
    }

    // Network Providers
    public class PacketRateProvider : IWidgetDataProvider
    {
        private readonly Queue<double> _history = new Queue<double>();

        public WidgetData GetData()
        {
            var handler = ProtocolHandler.Instance;
            var rate = handler.GetPacketRate();
            _history.Enqueue(rate);
            if (_history.Count > 20) _history.Dequeue();

            return new WidgetData
            {
                Value = rate,
                DisplayText = $"{rate:F0}/s",
                SparklineData = _history.ToList(),
                Status = WidgetStatus.Normal
            };
        }
    }

    public class LatencyProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            var handler = ProtocolHandler.Instance;
            var latency = handler.GetAverageLatency();
            return new WidgetData
            {
                Value = latency,
                DisplayText = $"{latency:F0}ms",
                Status = latency > 200 ? WidgetStatus.Warning : WidgetStatus.Normal
            };
        }
    }

    public class DecodeRateProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            var handler = ProtocolHandler.Instance;
            var rate = handler.GetDecodeSuccessRate();
            return new WidgetData
            {
                Value = rate,
                DisplayText = $"{rate:F1}%",
                Status = rate < 95 ? WidgetStatus.Warning : WidgetStatus.Normal
            };
        }
    }

    public class FiddlerStatusProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            var client = TrafficPipeClient.Instance;
            var connected = client.IsConnected;
            return new WidgetData
            {
                Value = connected,
                DisplayText = connected ? "Connected" : "Disconnected",
                Status = connected ? WidgetStatus.Normal : WidgetStatus.Inactive
            };
        }
    }

    public class ProxyThroughputProvider : IWidgetDataProvider
    {
        private readonly Queue<double> _history = new Queue<double>();

        public WidgetData GetData()
        {
            var monitor = ProxyMonitor.Instance;
            var throughput = monitor.GetThroughputKBps();
            _history.Enqueue(throughput);
            if (_history.Count > 20) _history.Dequeue();

            return new WidgetData
            {
                Value = throughput,
                DisplayText = $"{throughput:F1} KB/s",
                SparklineData = _history.ToList(),
                Status = WidgetStatus.Normal
            };
        }
    }

    public class ErrorCountProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            var handler = ProtocolHandler.Instance;
            var errors = handler.GetErrorCount();
            return new WidgetData
            {
                Value = errors,
                DisplayText = errors.ToString(),
                Status = errors > 0 ? WidgetStatus.Warning : WidgetStatus.Normal
            };
        }
    }

    // Automation Providers
    public class AutopilotStatusProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            var autopilot = AutoPilotService.Instance;
            var status = autopilot.IsRunning ? "Running" : "Stopped";
            var taskCount = autopilot.ActiveTaskCount;
            return new WidgetData
            {
                Value = autopilot.IsRunning,
                DisplayText = $"{status} ({taskCount})",
                Status = autopilot.IsRunning ? WidgetStatus.Normal : WidgetStatus.Inactive
            };
        }
    }

    public class QueueDepthProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            var autopilot = AutoPilotService.Instance;
            var depth = autopilot.QueueDepth;
            return new WidgetData
            {
                Value = depth,
                DisplayText = depth.ToString(),
                Status = depth > 100 ? WidgetStatus.Warning : WidgetStatus.Normal
            };
        }
    }

    public class ActionRateProvider : IWidgetDataProvider
    {
        private readonly Queue<double> _history = new Queue<double>();

        public WidgetData GetData()
        {
            var autopilot = AutoPilotService.Instance;
            var rate = autopilot.ActionsPerMinute;
            _history.Enqueue(rate);
            if (_history.Count > 20) _history.Dequeue();

            return new WidgetData
            {
                Value = rate,
                DisplayText = $"{rate:F0}/min",
                SparklineData = _history.ToList(),
                Status = WidgetStatus.Normal
            };
        }
    }

    public class SessionTimeProvider : IWidgetDataProvider
    {
        private readonly DateTime _sessionStart = DateTime.UtcNow;

        public WidgetData GetData()
        {
            var duration = DateTime.UtcNow - _sessionStart;
            return new WidgetData
            {
                Value = duration.TotalSeconds,
                DisplayText = $"{duration:hh\\:mm\\:ss}",
                Status = WidgetStatus.Normal
            };
        }
    }

    // System Providers
    public class MemoryUsageProvider : IWidgetDataProvider
    {
        public WidgetData GetData()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var usedMb = process.WorkingSet64 / 1024.0 / 1024.0;
            var maxMb = 2048.0; // Assume 2GB max
            return new WidgetData
            {
                Value = usedMb,
                DisplayText = $"{usedMb:F0}MB",
                Progress = usedMb / maxMb,
                Status = usedMb > 1500 ? WidgetStatus.Warning : WidgetStatus.Normal
            };
        }
    }

    #endregion
}
