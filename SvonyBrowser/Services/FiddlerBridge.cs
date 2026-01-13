using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SvonyBrowser.Services
{
    /// <summary>
    /// Advanced Fiddler Bridge for bidirectional communication with Fiddler Classic/Everywhere
    /// Supports packet capture, modification, injection, and replay
    /// </summary>
    public class FiddlerBridge : INotifyPropertyChanged, IDisposable
    {
        private static readonly Lazy<FiddlerBridge> _lazyInstance =
            new Lazy<FiddlerBridge>(() => new FiddlerBridge(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static FiddlerBridge Instance => _lazyInstance.Value;

        private NamedPipeServerStream? _serverPipe;
        private NamedPipeClientStream? _clientPipe;
        private StreamReader _reader;
        private StreamWriter _writer;
        private CancellationTokenSource _connectionCts;
        private Task _receiveTask;
        private bool _isConnected;
        private bool _isCapturing;
        private bool _isDisposed;
        private string _currentFilter = "*.evony.com";
        private readonly ConcurrentQueue<FiddlerCommand> _commandQueue = new ConcurrentQueue<FiddlerCommand>();
        private readonly ConcurrentDictionary<string, FiddlerSession> _activeSessions = new Dictionary<string, FiddlerSession>();
        private readonly List<FiddlerBreakpoint> _breakpoints = new List<FiddlerBreakpoint>();
        private readonly object _breakpointLock = new object();

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<FiddlerSessionEventArgs> SessionCaptured;
        public event EventHandler<FiddlerSessionEventArgs> SessionModified;
        public event EventHandler<FiddlerBreakpointEventArgs> BreakpointHit;
        public event EventHandler<FiddlerErrorEventArgs> ErrorOccurred;

        // Connection settings
        public string PipeName { get; set; } = "SvonyFiddlerBridge";
        public string FiddlerHost { get; set; } = "127.0.0.1";
        public int FiddlerPort { get; set; } = 8888;

        // Status properties
        public bool IsConnected => _isConnected;
        public bool IsCapturing => _isCapturing;
        public string CurrentFilter => _currentFilter;
        public int ActiveSessionCount => _activeSessions.Count;
        public double ThroughputKBps { get; private set; }

        private FiddlerBridge() { }

        #region Connection Management

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _connectionCts = new CancellationTokenSource();

                // Create named pipe server
                _serverPipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                // Wait for Fiddler script to connect
                await _serverPipe.WaitForConnectionAsync(_connectionCts.Token);

                _reader = new StreamReader(_serverPipe, Encoding.UTF8);
                _writer = new StreamWriter(_serverPipe, Encoding.UTF8) { AutoFlush = true };

                _isConnected = true;
                OnPropertyChanged(nameof(IsConnected));

                // Start receive loop
                _receiveTask = Task.Run(() => ReceiveLoopAsync(_connectionCts.Token));

                // Send initial configuration
                await SendCommandAsync(new FiddlerCommand
                {
                    Type = FiddlerCommandType.Configure,
                    Data = new Dictionary<string, object>
                    {
                        ["filter"] = _currentFilter,
                        ["captureEnabled"] = _isCapturing
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new FiddlerErrorEventArgs { Error = ex.Message });
                return false;
            }
        }

        public void Disconnect()
        {
            _connectionCts?.Cancel();
            _receiveTask?.Wait(TimeSpan.FromSeconds(5));

            _reader?.Dispose();
            _writer?.Dispose();
            _serverPipe?.Dispose();
            _clientPipe?.Dispose();

            _isConnected = false;
            OnPropertyChanged(nameof(IsConnected));
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _reader != null)
                {
                    var line = await _reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;

                    await ProcessMessageAsync(line);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new FiddlerErrorEventArgs { Error = ex.Message });
            }
        }

        private async Task ProcessMessageAsync(string message)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<FiddlerMessage>(message);
                if (msg == null) return;

                switch (msg.Type)
                {
                    case "session":
                        await HandleSessionAsync(msg);
                        break;
                    case "response":
                        HandleResponse(msg);
                        break;
                    case "error":
                        HandleError(msg);
                        break;
                    case "status":
                        HandleStatus(msg);
                        break;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new FiddlerErrorEventArgs { Error = $"Parse error: {ex.Message}" });
            }
        }

        #endregion

        #region Capture Control

        public async Task StartCaptureAsync(string filter = null)
        {
            if (!_isConnected) return;

            if (filter != null)
            {
                _currentFilter = filter;
                OnPropertyChanged(nameof(CurrentFilter));
            }

            await SendCommandAsync(new FiddlerCommand
            {
                Type = FiddlerCommandType.CaptureStart,
                Data = new Dictionary<string, object>
                {
                    ["filter"] = _currentFilter
                }
            });

            _isCapturing = true;
            OnPropertyChanged(nameof(IsCapturing));
        }

        public async Task StopCaptureAsync()
        {
            if (!_isConnected) return;

            await SendCommandAsync(new FiddlerCommand
            {
                Type = FiddlerCommandType.CaptureStop
            });

            _isCapturing = false;
            OnPropertyChanged(nameof(IsCapturing));
        }

        public async Task SetFilterAsync(string filter)
        {
            _currentFilter = filter;
            OnPropertyChanged(nameof(CurrentFilter));

            if (_isConnected)
            {
                await SendCommandAsync(new FiddlerCommand
                {
                    Type = FiddlerCommandType.FilterSet,
                    Data = new Dictionary<string, object>
                    {
                        ["filter"] = filter
                    }
                });
            }
        }

        #endregion

        #region Packet Operations

        public async Task<bool> InjectPacketAsync(byte[] data, string targetUrl)
        {
            if (!_isConnected) return false;

            var command = new FiddlerCommand
            {
                Type = FiddlerCommandType.PacketInject,
                Data = new Dictionary<string, object>
                {
                    ["url"] = targetUrl,
                    ["body"] = Convert.ToBase64String(data),
                    ["contentType"] = "application/x-amf"
                }
            };

            return await SendCommandAsync(command);
        }

        public async Task<bool> ModifyPacketAsync(string sessionId, Dictionary<string, object> modifications)
        {
            if (!_isConnected) return false;

            var command = new FiddlerCommand
            {
                Type = FiddlerCommandType.PacketModify,
                Data = new Dictionary<string, object>
                {
                    ["sessionId"] = sessionId,
                    ["modifications"] = modifications
                }
            };

            return await SendCommandAsync(command);
        }

        public async Task<bool> DropPacketAsync(string sessionId)
        {
            if (!_isConnected) return false;

            var command = new FiddlerCommand
            {
                Type = FiddlerCommandType.PacketDrop,
                Data = new Dictionary<string, object>
                {
                    ["sessionId"] = sessionId
                }
            };

            return await SendCommandAsync(command);
        }

        public async Task<bool> ReplaySessionAsync(string sessionId)
        {
            if (!_isConnected || !_activeSessions.TryGetValue(sessionId, out var session))
                return false;

            var command = new FiddlerCommand
            {
                Type = FiddlerCommandType.SessionReplay,
                Data = new Dictionary<string, object>
                {
                    ["sessionId"] = sessionId,
                    ["request"] = session.RequestData
                }
            };

            return await SendCommandAsync(command);
        }

        #endregion

        #region Breakpoints

        public void AddBreakpoint(FiddlerBreakpoint breakpoint)
        {
            lock (_breakpointLock)
            {
                _breakpoints.Add(breakpoint);
            }

            // Sync with Fiddler
            _ = SendCommandAsync(new FiddlerCommand
            {
                Type = FiddlerCommandType.BreakpointSet,
                Data = new Dictionary<string, object>
                {
                    ["id"] = breakpoint.Id,
                    ["type"] = breakpoint.Type.ToString(),
                    ["pattern"] = breakpoint.Pattern,
                    ["enabled"] = breakpoint.IsEnabled
                }
            });
        }

        public void RemoveBreakpoint(string id)
        {
            lock (_breakpointLock)
            {
                _breakpoints.RemoveAll(b => b.Id == id);
            }

            _ = SendCommandAsync(new FiddlerCommand
            {
                Type = FiddlerCommandType.BreakpointRemove,
                Data = new Dictionary<string, object>
                {
                    ["id"] = id
                }
            });
        }

        public void ClearBreakpoints()
        {
            lock (_breakpointLock)
            {
                _breakpoints.Clear();
            }

            _ = SendCommandAsync(new FiddlerCommand
            {
                Type = FiddlerCommandType.BreakpointClear
            });
        }

        #endregion

        #region Auto-Response Rules

        public async Task AddAutoResponseRuleAsync(AutoResponseRule rule)
        {
            await SendCommandAsync(new FiddlerCommand
            {
                Type = FiddlerCommandType.AutoResponseAdd,
                Data = new Dictionary<string, object>
                {
                    ["id"] = rule.Id,
                    ["pattern"] = rule.UrlPattern,
                    ["responseFile"] = rule.ResponseFile ?? "",
                    ["responseBody"] = rule.ResponseBody ?? "",
                    ["statusCode"] = rule.StatusCode,
                    ["headers"] = rule.Headers ?? new Dictionary<string, string>()
                }
            });
        }

        public async Task RemoveAutoResponseRuleAsync(string ruleId)
        {
            await SendCommandAsync(new FiddlerCommand
            {
                Type = FiddlerCommandType.AutoResponseRemove,
                Data = new Dictionary<string, object>
                {
                    ["id"] = ruleId
                }
            });
        }

        #endregion

        #region Export/Import

        public async Task ExportSessionAsync(string sessionId, string filePath, ExportFormat format)
        {
            await SendCommandAsync(new FiddlerCommand
            {
                Type = FiddlerCommandType.ExportSession,
                Data = new Dictionary<string, object>
                {
                    ["sessionId"] = sessionId,
                    ["filePath"] = filePath,
                    ["format"] = format.ToString()
                }
            });
        }

        public async Task ExportAllSessionsAsync(string filePath, ExportFormat format)
        {
            await SendCommandAsync(new FiddlerCommand
            {
                Type = FiddlerCommandType.ExportAll,
                Data = new Dictionary<string, object>
                {
                    ["filePath"] = filePath,
                    ["format"] = format.ToString()
                }
            });
        }

        public async Task ImportSessionAsync(string filePath)
        {
            await SendCommandAsync(new FiddlerCommand
            {
                Type = FiddlerCommandType.ImportSession,
                Data = new Dictionary<string, object>
                {
                    ["filePath"] = filePath
                }
            });
        }

        #endregion

        #region Message Handlers

        private async Task HandleSessionAsync(FiddlerMessage msg)
        {
            if (msg.Data == null) return;

            var session = new FiddlerSession
            {
                Id = msg.Data.TryGetValue("id", out var id) ? id.ToString()! : Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Url = msg.Data.TryGetValue("url", out var url) ? url.ToString()! : "",
                Method = msg.Data.TryGetValue("method", out var method) ? method.ToString()! : "GET",
                StatusCode = msg.Data.TryGetValue("statusCode", out var status) ? Convert.ToInt32(status) : 0,
                RequestSize = msg.Data.TryGetValue("requestSize", out var reqSize) ? Convert.ToInt32(reqSize) : 0,
                ResponseSize = msg.Data.TryGetValue("responseSize", out var resSize) ? Convert.ToInt32(resSize) : 0,
                Duration = msg.Data.TryGetValue("duration", out var dur) ? Convert.ToDouble(dur) : 0
            };

            if (msg.Data.TryGetValue("requestBody", out var reqBody))
            {
                session.RequestData = Convert.FromBase64String(reqBody.ToString()!);
            }

            if (msg.Data.TryGetValue("responseBody", out var resBody))
            {
                session.ResponseData = Convert.FromBase64String(resBody.ToString()!);
            }

            _activeSessions[session.Id] = session;
            OnPropertyChanged(nameof(ActiveSessionCount));

            // Check breakpoints
            await CheckBreakpointsAsync(session);

            // Forward to packet analysis engine
            if (session.RequestData != null)
            {
                PacketAnalysisEngine.Instance.EnqueuePacket(
                    session.RequestData,
                    PacketDirection.Outbound,
                    "Fiddler");
            }

            if (session.ResponseData != null)
            {
                PacketAnalysisEngine.Instance.EnqueuePacket(
                    session.ResponseData,
                    PacketDirection.Inbound,
                    "Fiddler");
            }

            SessionCaptured?.Invoke(this, new FiddlerSessionEventArgs { Session = session });
        }

        private async Task CheckBreakpointsAsync(FiddlerSession session)
        {
            lock (_breakpointLock)
            {
                foreach (var bp in _breakpoints.Where(b => b.IsEnabled))
                {
                    bool matches = bp.Type switch
                    {
                        FiddlerBreakpointType.Url => session.Url.Contains(bp.Pattern, StringComparison.OrdinalIgnoreCase),
                        FiddlerBreakpointType.Method => session.Method.Equals(bp.Pattern, StringComparison.OrdinalIgnoreCase),
                        FiddlerBreakpointType.StatusCode => session.StatusCode.ToString() == bp.Pattern,
                        FiddlerBreakpointType.RequestBody => session.RequestData != null &&
                            Encoding.UTF8.GetString(session.RequestData).Contains(bp.Pattern),
                        FiddlerBreakpointType.ResponseBody => session.ResponseData != null &&
                            Encoding.UTF8.GetString(session.ResponseData).Contains(bp.Pattern),
                        _ => false
                    };

                    if (matches)
                    {
                        BreakpointHit?.Invoke(this, new FiddlerBreakpointEventArgs
                        {
                            Breakpoint = bp,
                            Session = session
                        });
                    }
                }
            }
        }

        private void HandleResponse(FiddlerMessage msg)
        {
            // Handle command responses
        }

        private void HandleError(FiddlerMessage msg)
        {
            var error = msg.Data?.TryGetValue("message", out var errMsg) == true ? errMsg.ToString()! : "Unknown error";
            ErrorOccurred?.Invoke(this, new FiddlerErrorEventArgs { Error = error });
        }

        private void HandleStatus(FiddlerMessage msg)
        {
            if (msg.Data?.TryGetValue("throughput", out var throughput) == true)
            {
                ThroughputKBps = Convert.ToDouble(throughput);
                OnPropertyChanged(nameof(ThroughputKBps));
            }
        }

        #endregion

        #region Command Sending

        private async Task<bool> SendCommandAsync(FiddlerCommand command)
        {
            if (!_isConnected || _writer == null) return false;

            try
            {
                var json = JsonConvert.SerializeObject(command);
                await _writer.WriteLineAsync(json);
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new FiddlerErrorEventArgs { Error = ex.Message });
                return false;
            }
        }

        #endregion

        #region Query Methods

        public FiddlerSession? GetSession(string sessionId)
        {
            return _activeSessions.TryGetValue(sessionId, out var session) ? session : null;
        }

        public IEnumerable<FiddlerSession> GetAllSessions()
        {
            return _activeSessions.Values.ToList();
        }

        public IEnumerable<FiddlerSession> SearchSessions(string query)
        {
            return _activeSessions.Values
                .Where(s => s.Url.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public void ClearSessions()
        {
            _activeSessions.Clear();
            OnPropertyChanged(nameof(ActiveSessionCount));
        }

        /// <summary>
        /// Gets the current throughput in KB/s.
        /// </summary>
        public double GetThroughput() => ThroughputKBps;

        /// <summary>
        /// Gets the active session count.
        /// </summary>
        public int GetSessionCount() => _activeSessions.Count;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            Disconnect();
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #region Models

    public class FiddlerCommand
    {
        public FiddlerCommandType Type { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    public enum FiddlerCommandType
    {
        Configure,
        CaptureStart,
        CaptureStop,
        FilterSet,
        PacketInject,
        PacketModify,
        PacketDrop,
        SessionReplay,
        BreakpointSet,
        BreakpointRemove,
        BreakpointClear,
        AutoResponseAdd,
        AutoResponseRemove,
        ExportSession,
        ExportAll,
        ImportSession
    }

    public class FiddlerMessage
    {
        public string Type { get; set; } = "";
        public Dictionary<string, object> Data { get; set; }
    }

    public class FiddlerSession
    {
        public string Id { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Url { get; set; } = "";
        public string Method { get; set; } = "";
        public int StatusCode { get; set; }
        public int RequestSize { get; set; }
        public int ResponseSize { get; set; }
        public double Duration { get; set; }
        public byte[]? RequestData { get; set; }
        public byte[]? ResponseData { get; set; }
        public Dictionary<string, string> RequestHeaders { get; set; }
        public Dictionary<string, string> ResponseHeaders { get; set; }
    }

    public class FiddlerBreakpoint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public FiddlerBreakpointType Type { get; set; }
        public string Pattern { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
        public BreakpointAction Action { get; set; } = BreakpointAction.Pause;
    }

    public enum FiddlerBreakpointType
    {
        Url,
        Method,
        StatusCode,
        RequestBody,
        ResponseBody
    }

    public class AutoResponseRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UrlPattern { get; set; } = "";
        public string ResponseFile { get; set; }
        public string ResponseBody { get; set; }
        public int StatusCode { get; set; } = 200;
        public Dictionary<string, string> Headers { get; set; }
    }

    public enum ExportFormat
    {
        HAR,
        SAZ,
        JSON,
        PCAP
    }

    #endregion

    #region Events

    public class FiddlerSessionEventArgs : EventArgs
    {
        public FiddlerSession Session { get; set; } = null!;
    }

    public class FiddlerBreakpointEventArgs : EventArgs
    {
        public FiddlerBreakpoint Breakpoint { get; set; } = null!;
        public FiddlerSession Session { get; set; } = null!;
    }

    public class FiddlerErrorEventArgs : EventArgs
    {
        public string Error { get; set; } = "";
    }

    #endregion
}
