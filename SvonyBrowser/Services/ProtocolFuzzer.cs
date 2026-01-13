using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SvonyBrowser.Helpers;

namespace SvonyBrowser.Services
{
    /// <summary>
    /// Protocol Fuzzer for discovering undocumented actions and testing boundaries
    /// </summary>
    public class ProtocolFuzzer : INotifyPropertyChanged, IDisposable
    {
        private static readonly Lazy<ProtocolFuzzer> _lazyInstance =
            new Lazy<ProtocolFuzzer>(() => new ProtocolFuzzer(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static ProtocolFuzzer Instance => _lazyInstance.Value;

        private CancellationTokenSource _fuzzCts;
        private Task _fuzzTask;
        private bool _isFuzzing;
        private bool _isDisposed;
        private int _totalAttempts;
        private int _successfulAttempts;
        private int _errorAttempts;
        private int _discoveredActions;
        private double _progress;
        private readonly ConcurrentBag<FuzzResult> _results = new ConcurrentBag<FuzzResult>();
        private readonly ConcurrentDictionary<string, DiscoveredAction> _discoveries = new ConcurrentDictionary<string, DiscoveredAction>();

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<FuzzResultEventArgs> ResultReceived;
        public event EventHandler<ActionDiscoveredEventArgs> ActionDiscovered;
        public event EventHandler<FuzzCompleteEventArgs> FuzzingComplete;

        // Status properties
        public bool IsFuzzing => _isFuzzing;
        public int TotalAttempts => _totalAttempts;
        public int SuccessfulAttempts => _successfulAttempts;
        public int ErrorAttempts => _errorAttempts;
        public int DiscoveredActions => _discoveredActions;
        public double Progress => _progress;
        public double SuccessRate => _totalAttempts > 0 ? (_successfulAttempts * 100.0 / _totalAttempts) : 0;

        // Known action prefixes from RE analysis
        private readonly string[] _knownPrefixes = new[]
        {
            "user", "city", "hero", "troop", "march", "alliance", "mail",
            "shop", "quest", "event", "buff", "item", "building", "tech",
            "general", "monster", "resource", "pvp", "world", "server",
            "chat", "friend", "rank", "report", "system", "activity"
        };

        // Known action suffixes
        private readonly string[] _knownSuffixes = new[]
        {
            "Info", "List", "Get", "Set", "Create", "Delete", "Update",
            "Start", "Stop", "Cancel", "Complete", "Collect", "Claim",
            "Buy", "Sell", "Use", "Equip", "Unequip", "Upgrade", "Train",
            "Attack", "Defend", "Scout", "Rally", "Reinforce", "Recall",
            "Join", "Leave", "Invite", "Kick", "Promote", "Demote",
            "Send", "Receive", "Accept", "Reject", "Open", "Close"
        };

        private ProtocolFuzzer() { }

        #region Fuzzing Control

        public async Task StartFuzzingAsync(FuzzConfig config)
        {
            if (_isFuzzing) return;

            _isFuzzing = true;
            _totalAttempts = 0;
            _successfulAttempts = 0;
            _errorAttempts = 0;
            _discoveredActions = 0;
            _progress = 0;
            // ConcurrentBag doesn't have Clear() in .NET Framework 4.6.2
            while (_results.TryTake(out _)) { }

            OnPropertyChanged(nameof(IsFuzzing));
            OnPropertyChanged(nameof(Progress));

            _fuzzCts = new CancellationTokenSource();
            _fuzzTask = Task.Run(() => FuzzLoopAsync(config, _fuzzCts.Token));

            await _fuzzTask;
        }

        public void StopFuzzing()
        {
            _fuzzCts?.Cancel();
            _isFuzzing = false;
            OnPropertyChanged(nameof(IsFuzzing));
        }

        private async Task FuzzLoopAsync(FuzzConfig config, CancellationToken ct)
        {
            var actions = GenerateFuzzActions(config);
            var total = actions.Count;
            var processed = 0;

            // Parallel fuzzing with rate limiting
            var semaphore = new SemaphoreSlim(config.Parallelism);
            var tasks = new List<Task>();

            foreach (var action in actions)
            {
                if (ct.IsCancellationRequested) break;

                await semaphore.WaitAsync(ct);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await FuzzActionAsync(action, config, ct);
                    }
                    finally
                    {
                        semaphore.Release();
                        Interlocked.Increment(ref processed);
                        _progress = (processed * 100.0) / total;
                        OnPropertyChanged(nameof(Progress));
                    }
                }, ct));

                // Rate limiting
                await Task.Delay(config.DelayMs, ct);
            }

            await Task.WhenAll(tasks);

            _isFuzzing = false;
            OnPropertyChanged(nameof(IsFuzzing));

            FuzzingComplete?.Invoke(this, new FuzzCompleteEventArgs
            {
                TotalAttempts = _totalAttempts,
                Successful = _successfulAttempts,
                Errors = _errorAttempts,
                Discovered = _discoveredActions,
                Results = _results.ToList()
            });
        }

        private async Task FuzzActionAsync(FuzzAction action, FuzzConfig config, CancellationToken ct)
        {
            Interlocked.Increment(ref _totalAttempts);
            OnPropertyChanged(nameof(TotalAttempts));

            var result = new FuzzResult
            {
                Action = action.Name,
                Parameters = action.Parameters,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                // Build packet
                var packet = BuildFuzzPacket(action);

                // Send via Fiddler bridge or direct
                var response = await SendFuzzPacketAsync(packet, config.TargetUrl, ct);

                result.ResponseSize = response?.Length ?? 0;
                result.ResponseData = response;

                // Analyze response
                var analysis = AnalyzeResponse(response, action);
                result.IsSuccess = analysis.IsValid;
                result.ResponseType = analysis.Type;
                result.Notes = analysis.Notes;

                if (result.IsSuccess)
                {
                    Interlocked.Increment(ref _successfulAttempts);
                    OnPropertyChanged(nameof(SuccessfulAttempts));

                    // Check if this is a new discovery
                    if (analysis.IsNewAction && !_discoveries.ContainsKey(action.Name))
                    {
                        var discovery = new DiscoveredAction
                        {
                            Name = action.Name,
                            Parameters = action.Parameters,
                            ResponseType = analysis.Type,
                            DiscoveredAt = DateTime.UtcNow,
                            SampleResponse = response
                        };

                        if (_discoveries.TryAdd(action.Name, discovery))
                        {
                            Interlocked.Increment(ref _discoveredActions);
                            OnPropertyChanged(nameof(DiscoveredActions));

                            ActionDiscovered?.Invoke(this, new ActionDiscoveredEventArgs
                            {
                                Action = discovery
                            });
                        }
                    }
                }
                else
                {
                    Interlocked.Increment(ref _errorAttempts);
                    OnPropertyChanged(nameof(ErrorAttempts));
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Error = ex.Message;
                Interlocked.Increment(ref _errorAttempts);
                OnPropertyChanged(nameof(ErrorAttempts));
            }

            _results.Add(result);
            ResultReceived?.Invoke(this, new FuzzResultEventArgs { Result = result });
        }

        #endregion

        #region Action Generation

        private List<FuzzAction> GenerateFuzzActions(FuzzConfig config)
        {
            var actions = new List<FuzzAction>();

            switch (config.Mode)
            {
                case FuzzMode.ActionDiscovery:
                    actions.AddRange(GenerateActionDiscoveryFuzz());
                    break;
                case FuzzMode.ParameterBoundary:
                    actions.AddRange(GenerateParameterBoundaryFuzz(config.TargetAction!));
                    break;
                case FuzzMode.TypeConfusion:
                    actions.AddRange(GenerateTypeConfusionFuzz(config.TargetAction!));
                    break;
                case FuzzMode.SequenceBreaking:
                    actions.AddRange(GenerateSequenceBreakingFuzz());
                    break;
                case FuzzMode.Custom:
                    actions.AddRange(config.CustomActions ?? new List<FuzzAction>());
                    break;
            }

            return actions;
        }

        private IEnumerable<FuzzAction> GenerateActionDiscoveryFuzz()
        {
            var actions = new List<FuzzAction>();

            // Generate all prefix.suffix combinations
            foreach (var prefix in _knownPrefixes)
            {
                foreach (var suffix in _knownSuffixes)
                {
                    var actionName = $"{prefix}.{char.ToLower(suffix[0])}{suffix.Substring(1)}";
                    actions.Add(new FuzzAction
                    {
                        Name = actionName,
                        Parameters = new Dictionary<string, object>()
                    });
                }
            }

            // Add numbered variants (action1, action2, etc.)
            foreach (var prefix in _knownPrefixes)
            {
                for (int i = 1; i <= 10; i++)
                {
                    actions.Add(new FuzzAction
                    {
                        Name = $"{prefix}.action{i}",
                        Parameters = new Dictionary<string, object>()
                    });
                }
            }

            // Add internal/debug actions
            var debugPrefixes = new[] { "debug", "admin", "gm", "test", "internal", "dev" };
            foreach (var prefix in debugPrefixes)
            {
                foreach (var suffix in _knownSuffixes)
                {
                    actions.Add(new FuzzAction
                    {
                        Name = $"{prefix}.{char.ToLower(suffix[0])}{suffix.Substring(1)}",
                        Parameters = new Dictionary<string, object>()
                    });
                }
            }

            return actions;
        }

        private IEnumerable<FuzzAction> GenerateParameterBoundaryFuzz(string targetAction)
        {
            var actions = new List<FuzzAction>();

            // Integer boundaries
            var intBoundaries = new object[]
            {
                0, 1, -1, int.MaxValue, int.MinValue,
                long.MaxValue, long.MinValue,
                0x7FFFFFFF, 0x80000000,
                999999999, -999999999
            };

            foreach (var boundary in intBoundaries)
            {
                actions.Add(new FuzzAction
                {
                    Name = targetAction,
                    Parameters = new Dictionary<string, object>
                    {
                        ["id"] = boundary,
                        ["count"] = boundary,
                        ["amount"] = boundary
                    }
                });
            }

            // String boundaries
            var stringBoundaries = new[]
            {
                "", " ", "null", "undefined", "NaN",
                new string('A', 256),
                new string('A', 65536),
                "<script>alert(1)</script>",
                "'; DROP TABLE users; --",
                "../../../etc/passwd",
                "%00%00%00%00"
            };

            foreach (var boundary in stringBoundaries)
            {
                actions.Add(new FuzzAction
                {
                    Name = targetAction,
                    Parameters = new Dictionary<string, object>
                    {
                        ["name"] = boundary,
                        ["message"] = boundary
                    }
                });
            }

            // Array boundaries
            actions.Add(new FuzzAction
            {
                Name = targetAction,
                Parameters = new Dictionary<string, object>
                {
                    ["ids"] = new int[0]
                }
            });

            actions.Add(new FuzzAction
            {
                Name = targetAction,
                Parameters = new Dictionary<string, object>
                {
                    ["ids"] = Enumerable.Range(0, 1000).ToArray()
                }
            });

            return actions;
        }

        private IEnumerable<FuzzAction> GenerateTypeConfusionFuzz(string targetAction)
        {
            var actions = new List<FuzzAction>();

            // Send wrong types for known parameters
            var typeConfusions = new Dictionary<string, object[]>
            {
                ["id"] = new object[] { "string", new[] { 1, 2, 3 }, true, null!, 1.5 },
                ["count"] = new object[] { "many", new[] { 1 }, false, null!, -1.5 },
                ["name"] = new object[] { 12345, new[] { "a", "b" }, true, null!, 0.0 }
            };

            foreach (var param in typeConfusions)
            {
                foreach (var value in param.Value)
                {
                    actions.Add(new FuzzAction
                    {
                        Name = targetAction,
                        Parameters = new Dictionary<string, object>
                        {
                            [param.Key] = value
                        }
                    });
                }
            }

            return actions;
        }

        private IEnumerable<FuzzAction> GenerateSequenceBreakingFuzz()
        {
            var actions = new List<FuzzAction>();

            // Try to skip authentication
            var postAuthActions = new[]
            {
                "city.getCityInfo", "hero.getHeroList", "troop.getTroopInfo",
                "march.createMarch", "alliance.getAllianceInfo"
            };

            foreach (var action in postAuthActions)
            {
                actions.Add(new FuzzAction
                {
                    Name = action,
                    Parameters = new Dictionary<string, object>
                    {
                        ["skipAuth"] = true
                    }
                });
            }

            // Try to complete actions out of order
            var sequences = new[]
            {
                ("march.completeMarch", new Dictionary<string, object> { ["marchId"] = 0 }),
                ("building.completeUpgrade", new Dictionary<string, object> { ["buildingId"] = 0 }),
                ("quest.completeQuest", new Dictionary<string, object> { ["questId"] = 0 })
            };

            foreach (var (actionName, parameters) in sequences)
            {
                actions.Add(new FuzzAction
                {
                    Name = actionName,
                    Parameters = parameters
                });
            }

            return actions;
        }

        #endregion

        #region Packet Building

        private byte[] BuildFuzzPacket(FuzzAction action)
        {
            // Build AMF3 packet
            var ms = new System.IO.MemoryStream();

            // Header
            ms.WriteByte(0x00); // Version
            ms.WriteByte(0x00);
            ms.WriteByte(0x00); // Header count
            ms.WriteByte(0x00);
            ms.WriteByte(0x00); // Message count
            ms.WriteByte(0x01);

            // Target URI (action name)
            WriteAmf0String(ms, action.Name);

            // Response URI
            WriteAmf0String(ms, "/1");

            // Message length placeholder
            var lengthPos = ms.Position;
            ms.Write(new byte[4], 0, 4);

            // AMF3 marker
            ms.WriteByte(0x11);

            // Write parameters as AMF3 object
            WriteAmf3Object(ms, action.Parameters);

            // Update length
            var endPos = ms.Position;
            var length = (int)(endPos - lengthPos - 4);
            ms.Position = lengthPos;
            ms.WriteByte((byte)((length >> 24) & 0xFF));
            ms.WriteByte((byte)((length >> 16) & 0xFF));
            ms.WriteByte((byte)((length >> 8) & 0xFF));
            ms.WriteByte((byte)(length & 0xFF));

            return ms.ToArray();
        }

        private void WriteAmf0String(System.IO.Stream stream, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            stream.WriteByte((byte)((bytes.Length >> 8) & 0xFF));
            stream.WriteByte((byte)(bytes.Length & 0xFF));
            stream.Write(bytes, 0, bytes.Length);
        }

        private void WriteAmf3Object(System.IO.Stream stream, Dictionary<string, object> obj)
        {
            stream.WriteByte(0x0A); // Object marker

            // Dynamic object with no sealed members
            stream.WriteByte(0x0B);

            // Empty class name
            stream.WriteByte(0x01);

            // Write properties
            foreach (var kvp in obj)
            {
                WriteAmf3String(stream, kvp.Key);
                WriteAmf3Value(stream, kvp.Value);
            }

            // End of dynamic properties
            stream.WriteByte(0x01);
        }

        private void WriteAmf3String(System.IO.Stream stream, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var u29 = (bytes.Length << 1) | 1;
            WriteU29(stream, u29);
            stream.Write(bytes, 0, bytes.Length);
        }

        private void WriteAmf3Value(System.IO.Stream stream, object value)
        {
            switch (value)
            {
                case null:
                    stream.WriteByte(0x01); // Null
                    break;
                case bool b:
                    stream.WriteByte(b ? (byte)0x03 : (byte)0x02);
                    break;
                case int i:
                    stream.WriteByte(0x04); // Integer
                    WriteU29(stream, i);
                    break;
                case long l:
                    stream.WriteByte(0x05); // Double
                    WriteDouble(stream, l);
                    break;
                case double d:
                    stream.WriteByte(0x05); // Double
                    WriteDouble(stream, d);
                    break;
                case string s:
                    stream.WriteByte(0x06); // String
                    WriteAmf3String(stream, s);
                    break;
                case int[] arr:
                    stream.WriteByte(0x09); // Array
                    WriteU29(stream, (arr.Length << 1) | 1);
                    stream.WriteByte(0x01); // Empty associative portion
                    foreach (var item in arr)
                    {
                        stream.WriteByte(0x04);
                        WriteU29(stream, item);
                    }
                    break;
                case Dictionary<string, object> dict:
                    WriteAmf3Object(stream, dict);
                    break;
                default:
                    stream.WriteByte(0x01); // Null for unknown types
                    break;
            }
        }

        private void WriteU29(System.IO.Stream stream, int value)
        {
            if (value < 0x80)
            {
                stream.WriteByte((byte)value);
            }
            else if (value < 0x4000)
            {
                stream.WriteByte((byte)((value >> 7) | 0x80));
                stream.WriteByte((byte)(value & 0x7F));
            }
            else if (value < 0x200000)
            {
                stream.WriteByte((byte)((value >> 14) | 0x80));
                stream.WriteByte((byte)((value >> 7) | 0x80));
                stream.WriteByte((byte)(value & 0x7F));
            }
            else
            {
                stream.WriteByte((byte)((value >> 22) | 0x80));
                stream.WriteByte((byte)((value >> 15) | 0x80));
                stream.WriteByte((byte)((value >> 8) | 0x80));
                stream.WriteByte((byte)(value & 0xFF));
            }
        }

        private void WriteDouble(System.IO.Stream stream, double value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            stream.Write(bytes, 0, 8);
        }

        #endregion

        #region Network

        private async Task<byte[]?> SendFuzzPacketAsync(byte[] packet, string targetUrl, CancellationToken ct)
        {
            // Try to send via Fiddler bridge first
            if (FiddlerBridge.Instance.IsConnected)
            {
                var success = await FiddlerBridge.Instance.InjectPacketAsync(packet, targetUrl);
                if (success)
                {
                    // Wait for response via event
                    await Task.Delay(500, ct);
                    return null; // Response comes via event
                }
            }

            // Direct HTTP send
            var client = new System.Net.Http.HttpClient(); // TODO: Add using block for proper disposal
            var httpContent = new System.Net.Http.ByteArrayContent(packet);
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-amf");

            var response = await client.PostAsync(targetUrl, httpContent, ct);
            return await response.Content.ReadAsByteArrayAsync();
        }

        #endregion

        #region Response Analysis

        private ResponseAnalysis AnalyzeResponse(byte[]? response, FuzzAction action)
        {
            var analysis = new ResponseAnalysis();

            if (response == null || response.Length == 0)
            {
                analysis.IsValid = false;
                analysis.Type = ResponseType.NoResponse;
                analysis.Notes = "No response received";
                return analysis;
            }

            // Check for error responses
            var responseStr = Encoding.UTF8.GetString(response);

            if (responseStr.Contains("error") || responseStr.Contains("Error"))
            {
                analysis.IsValid = false;
                analysis.Type = ResponseType.Error;
                analysis.Notes = "Error response";
                return analysis;
            }

            if (responseStr.Contains("invalid") || responseStr.Contains("Invalid"))
            {
                analysis.IsValid = false;
                analysis.Type = ResponseType.InvalidAction;
                analysis.Notes = "Invalid action";
                return analysis;
            }

            if (responseStr.Contains("unauthorized") || responseStr.Contains("Unauthorized"))
            {
                analysis.IsValid = false;
                analysis.Type = ResponseType.Unauthorized;
                analysis.Notes = "Unauthorized";
                return analysis;
            }

            // Check for valid AMF response
            if (response.Length > 10 && (response[0] == 0x00 || response[0] == 0x0A))
            {
                analysis.IsValid = true;
                analysis.Type = ResponseType.ValidAmf;
                analysis.Notes = "Valid AMF response";

                // Check if this is a known action
                analysis.IsNewAction = !IsKnownAction(action.Name);
            }
            else
            {
                analysis.IsValid = true;
                analysis.Type = ResponseType.Unknown;
                analysis.Notes = "Unknown response format";
            }

            return analysis;
        }

        private bool IsKnownAction(string actionName)
        {
            // Check against known protocol database
            return ProtocolHandler.Instance.GetProtocolAction(actionName) != null;
        }

        #endregion

        #region Query Methods

        public IEnumerable<FuzzResult> GetResults()
        {
            return _results.ToList();
        }

        public IEnumerable<DiscoveredAction> GetDiscoveries()
        {
            return _discoveries.Values.ToList();
        }

        public void ClearResults()
        {
            // ConcurrentBag doesn't have Clear() in .NET Framework 4.6.2
            while (_results.TryTake(out _)) { }
            _totalAttempts = 0;
            _successfulAttempts = 0;
            _errorAttempts = 0;
            OnPropertyChanged(nameof(TotalAttempts));
            OnPropertyChanged(nameof(SuccessfulAttempts));
            OnPropertyChanged(nameof(ErrorAttempts));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            StopFuzzing();
            _fuzzCts?.Dispose();
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #region Models

    public class FuzzConfig
    {
        public FuzzMode Mode { get; set; } = FuzzMode.ActionDiscovery;
        public string TargetUrl { get; set; } = "https://cc2.evony.com/gateway.php";
        public string TargetAction { get; set; }
        public int Parallelism { get; set; } = 5;
        public int DelayMs { get; set; } = 100;
        public int TimeoutMs { get; set; } = 5000;
        public List<FuzzAction> CustomActions { get; set; }
    }

    public enum FuzzMode
    {
        ActionDiscovery,
        ParameterBoundary,
        TypeConfusion,
        SequenceBreaking,
        Custom
    }

    public class FuzzAction
    {
        public string Name { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class FuzzResult
    {
        public string Action { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsSuccess { get; set; }
        public ResponseType ResponseType { get; set; }
        public int ResponseSize { get; set; }
        public byte[]? ResponseData { get; set; }
        public string Notes { get; set; }
        public string Error { get; set; }
    }

    public enum ResponseType
    {
        NoResponse,
        Error,
        InvalidAction,
        Unauthorized,
        ValidAmf,
        Unknown
    }

    public class ResponseAnalysis
    {
        public bool IsValid { get; set; }
        public ResponseType Type { get; set; }
        public string Notes { get; set; }
        public bool IsNewAction { get; set; }
    }

    public class DiscoveredAction
    {
        public string Name { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; }
        public ResponseType ResponseType { get; set; }
        public DateTime DiscoveredAt { get; set; }
        public byte[]? SampleResponse { get; set; }
    }

    #endregion

    #region Events

    public class FuzzResultEventArgs : EventArgs
    {
        public FuzzResult Result { get; set; } = null!;
    }

    public class ActionDiscoveredEventArgs : EventArgs
    {
        public DiscoveredAction Action { get; set; } = null!;
    }

    public class FuzzCompleteEventArgs : EventArgs
    {
        public int TotalAttempts { get; set; }
        public int Successful { get; set; }
        public int Errors { get; set; }
        public int Discovered { get; set; }
        public List<FuzzResult> Results { get; set; } = new List<FuzzResult>();
    }

    #endregion
}
