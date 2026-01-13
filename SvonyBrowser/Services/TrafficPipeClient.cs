using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Client for receiving traffic data from Fiddler via named pipe.
    /// Integrates with the RTE MCP server for real-time analysis.
    /// </summary>
    public sealed class TrafficPipeClient : IDisposable
    {
        #region Singleton

        private static readonly Lazy<TrafficPipeClient> _lazyInstance =
            new Lazy<TrafficPipeClient>(() => new TrafficPipeClient(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static TrafficPipeClient Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly string _pipeName;
        private NamedPipeClientStream? _pipeClient;
        private StreamReader _reader;
        private CancellationTokenSource _cts;
        private Task _readTask;
        private bool _disposed = false;
        private bool _isConnected = false;
        private int _reconnectAttempts = 0;
        private const int MaxReconnectAttempts = 5;
        private const int ReconnectDelayMs = 5000;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the pipe is connected.
        /// </summary>
        public bool IsConnected => _isConnected && _pipeClient?.IsConnected == true;

        /// <summary>
        /// Gets the pipe name.
        /// </summary>
        public string PipeName => _pipeName;

        #endregion

        #region Events

        /// <summary>
        /// Fired when traffic data is received.
        /// </summary>
        public event Action<FiddlerTrafficData> TrafficReceived;

        /// <summary>
        /// Alias for TrafficReceived - fired when a packet is received.
        /// </summary>
        public event Action<FiddlerTrafficData> PacketReceived
        {
            add => TrafficReceived += value;
            remove => TrafficReceived -= value;
        }

        /// <summary>
        /// Fired when connection status changes.
        /// </summary>
        public event Action<bool> ConnectionStatusChanged;

        /// <summary>
        /// Fired when an error occurs.
        /// </summary>
        public event Action<string> ErrorOccurred;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new TrafficPipeClient.
        /// </summary>
        /// <param name="pipeName">Name of the named pipe (default: EvonyTraffic)</param>
        public TrafficPipeClient(string pipeName = "EvonyTraffic")
        {
            _pipeName = pipeName;
            App.Logger.Information("TrafficPipeClient created for pipe: {PipeName}", pipeName);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts listening for traffic data.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) return;

            App.Logger.Information("Starting TrafficPipeClient...");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _readTask = ReadLoopAsync(_cts.Token);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Stops listening for traffic data.
        /// </summary>
        public void Stop()
        {
            App.Logger.Information("Stopping TrafficPipeClient...");

            _cts?.Cancel();

            try
            {
                _readTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException) { /* Expected */ }

            Disconnect();
        }

        /// <summary>
        /// Forces a reconnection attempt.
        /// </summary>
        public async Task ReconnectAsync()
        {
            Disconnect();
            _reconnectAttempts = 0;
            await ConnectAsync(_cts?.Token ?? CancellationToken.None);
        }

        #endregion

        #region Private Methods

        private async Task ReadLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !_disposed)
            {
                try
                {
                    if (!IsConnected)
                    {
                        await ConnectAsync(cancellationToken);
                        if (!IsConnected)
                        {
                            await Task.Delay(ReconnectDelayMs, cancellationToken);
                            continue;
                        }
                    }

                    // Read data from pipe
                    // ReadLineAsync with CancellationToken is .NET 7+ only, use Task.Run wrapper
                    var line = await Task.Run(() => _reader!.ReadLineAsync(), cancellationToken);
                    if (line == null)
                    {
                        // Pipe closed
                        App.Logger.Warning("Pipe closed by server");
                        Disconnect();
                        continue;
                    }

                    // Parse and process traffic data
                    try
                    {
                        var trafficData = JsonConvert.DeserializeObject<FiddlerTrafficData>(line);
                        if (trafficData != null)
                        {
                            OnTrafficReceived(trafficData);
                        }
                    }
                    catch (JsonException ex)
                    {
                        App.Logger.Warning("Invalid traffic data received: {Error}", ex.Message);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (IOException ex)
                {
                    App.Logger.Error(ex, "Pipe I/O error");
                    Disconnect();
                    await Task.Delay(ReconnectDelayMs, cancellationToken);
                }
                catch (Exception ex)
                {
                    App.Logger.Error(ex, "Error in read loop");
                    ErrorOccurred?.Invoke(ex.Message);
                    await Task.Delay(ReconnectDelayMs, cancellationToken);
                }
            }
        }

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (_reconnectAttempts >= MaxReconnectAttempts)
            {
                App.Logger.Warning("Max reconnect attempts reached for pipe: {PipeName}", _pipeName);
                return;
            }

            _reconnectAttempts++;
            App.Logger.Information("Connecting to pipe: {PipeName} (attempt {Attempt})", _pipeName, _reconnectAttempts);

            try
            {
                _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.In, PipeOptions.Asynchronous);

                // Try to connect with timeout
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); // TODO: Add using block for proper disposal
                cts.CancelAfter(TimeSpan.FromSeconds(10));

                await _pipeClient.ConnectAsync(cts.Token);

                _reader = new StreamReader(_pipeClient, Encoding.UTF8);
                _isConnected = true;
                _reconnectAttempts = 0;

                App.Logger.Information("Connected to pipe: {PipeName}", _pipeName);
                ConnectionStatusChanged?.Invoke(true);
            }
            catch (OperationCanceledException)
            {
                App.Logger.Debug("Pipe connection cancelled or timed out");
                Disconnect();
            }
            catch (Exception ex)
            {
                App.Logger.Warning("Failed to connect to pipe: {Error}", ex.Message);
                Disconnect();
            }
        }

        private void Disconnect()
        {
            _isConnected = false;

            try
            {
                _reader?.Dispose();
                _reader = null;
            }
            catch { /* Ignore */ }

            try
            {
                _pipeClient?.Dispose();
                _pipeClient = null;
            }
            catch { /* Ignore */ }

            ConnectionStatusChanged?.Invoke(false);
        }

        private void OnTrafficReceived(FiddlerTrafficData data)
        {
            try
            {
                // Forward to RTE server for analysis
                _ = ForwardToRteAsync(data);

                // Notify listeners
                TrafficReceived?.Invoke(data);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error processing traffic data");
            }
        }

        private async Task ForwardToRteAsync(FiddlerTrafficData data)
        {
            try
            {
                var mcpManager = McpConnectionManager.Instance;
                if (mcpManager.GetServerStatus("evony-rte") != McpConnectionStatus.Connected)
                {
                    return;
                }

                await mcpManager.CallToolAsync("evony-rte", "traffic_capture", new JObject
                {
                    ["direction"] = data.Direction,
                    ["url"] = data.Url ?? "",
                    ["data"] = data.HexData ?? "",
                    ["timestamp"] = data.Timestamp
                });
            }
            catch (Exception ex)
            {
                App.Logger.Debug("Failed to forward traffic to RTE: {Error}", ex.Message);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Stop();

            _cts?.Dispose();
            _cts = null;

            App.Logger.Debug("TrafficPipeClient disposed");
        }

        #endregion
    }

    /// <summary>
    /// Traffic data model received from Fiddler.
    /// </summary>
    public class FiddlerTrafficData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("direction")]
        public string Direction { get; set; } = "request";

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        [JsonProperty("contentLength")]
        public int ContentLength { get; set; }

        [JsonProperty("hexData")]
        public string HexData { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("statusCode")]
        public int? StatusCode { get; set; }

        [JsonProperty("headers")]
        public Dictionary<string, string> Headers { get; set; }

        [JsonProperty("decoded")]
        public JObject Decoded { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }
    }

    /// <summary>
    /// Traffic capture session for grouping related requests/responses.
    /// </summary>
    public class TrafficSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        public List<FiddlerTrafficData> Entries { get; set; } = new List<FiddlerTrafficData>();
        public string PlayerName { get; set; }
        public string Server { get; set; }
        public int TotalRequests => Entries.Count(e => e.Direction == "request");
        public int TotalResponses => Entries.Count(e => e.Direction == "response");
        public long TotalBytes => Entries.Sum(e => e.ContentLength);

        public void AddEntry(FiddlerTrafficData data)
        {
            Entries.Add(data);
        }

        public void End()
        {
            EndTime = DateTime.UtcNow;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

}