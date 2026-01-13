using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Manages connections to MCP (Model Context Protocol) servers.
    /// Handles process lifecycle, communication, and health monitoring.
    /// </summary>
    public sealed class McpConnectionManager : IDisposable
    {
        #region Singleton

        private static readonly Lazy<McpConnectionManager> _lazyInstance =
            new Lazy<McpConnectionManager>(() => new McpConnectionManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static McpConnectionManager Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly ConcurrentDictionary<string, McpServerConnection> _connections = new Dictionary<string, McpServerConnection>();
        private readonly object _configLock = new object();
        private McpConfig? _config;
        private bool _disposed = false;
        private Timer _healthCheckTimer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the configuration path.
        /// </summary>
        public string ConfigPath { get; }

        /// <summary>
        /// Gets whether any servers are connected.
        /// </summary>
        public bool HasConnections => !_connections.IsEmpty;

        /// <summary>
        /// Gets whether the manager is connected to any server.
        /// </summary>
        public bool IsConnected => HasConnections;

        /// <summary>
        /// Gets the names of all connected servers.
        /// </summary>
        public IEnumerable<string> ConnectedServers => _connections.Keys;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a server connection status changes.
        /// </summary>
        public event Action<string, McpConnectionStatus> ConnectionStatusChanged;
    
        /// <summary>
        /// Alias for ConnectionStatusChanged for compatibility.
        /// </summary>
        public event Action<string, McpConnectionStatus> StatusChanged
        {
            add => ConnectionStatusChanged += value;
            remove => ConnectionStatusChanged -= value;
        }

        /// <summary>
        /// Fired when a server sends a message.
        /// </summary>
        public event Action<string, JObject> MessageReceived;

        /// <summary>
        /// Fired when a server error occurs.
        /// </summary>
        public event Action<string, string> ServerError;

        #endregion

        #region Constructor

        private McpConnectionManager()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            ConfigPath = Path.Combine(baseDir, "config", "mcp-config.json");
        
            App.Logger.Information("McpConnectionManager initialized");
            App.Logger.Debug("Config path: {Path}", ConfigPath);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads configuration and starts all auto-connect servers.
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            App.Logger.Information("Initializing MCP connections...");
        
            try
            {
                LoadConfiguration();
            
                if (_config?.McpServers == null || _config.McpServers.Count == 0)
                {
                    App.Logger.Warning("No MCP servers configured");
                    return;
                }
            
                // Start auto-connect servers
                var tasks = new List<Task>();
                foreach (var (name, serverConfig) in _config.McpServers)
                {
                    if (serverConfig.AutoConnect)
                    {
                        tasks.Add(ConnectServerAsync(name, cancellationToken));
                    }
                }
            
                await Task.WhenAll(tasks);
            
                // Start health check timer
                StartHealthCheckTimer();
            
                App.Logger.Information("MCP initialization complete. Connected: {Count}", _connections.Count);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to initialize MCP connections");
                throw;
            }
        }

        /// <summary>
        /// Connects to a specific MCP server.
        /// </summary>
        public async Task<bool> ConnectServerAsync(string serverName, CancellationToken cancellationToken = default)
        {
            if (_disposed) return false;
        
            App.Logger.Information("Connecting to MCP server: {Server}", serverName);
        
            try
            {
                if (_config?.McpServers == null || !_config.McpServers.TryGetValue(serverName, out var serverConfig))
                {
                    App.Logger.Error("Server configuration not found: {Server}", serverName);
                    return false;
                }
            
                // Disconnect existing connection if any
                if (_connections.TryRemove(serverName, out var existing))
                {
                    existing.Dispose();
                }
            
                // Create new connection
                var connection = new McpServerConnection(serverName, serverConfig);
                connection.MessageReceived += (msg) => OnMessageReceived(serverName, msg);
                connection.StatusChanged += (status) => OnConnectionStatusChanged(serverName, status);
                connection.ErrorOccurred += (error) => OnServerError(serverName, error);
            
                // Start the server process
                var success = await connection.StartAsync(cancellationToken);
            
                if (success)
                {
                    _connections[serverName] = connection;
                    App.Logger.Information("Connected to MCP server: {Server}", serverName);
                }
                else
                {
                    App.Logger.Error("Failed to connect to MCP server: {Server}", serverName);
                    connection.Dispose();
                }
            
                return success;
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error connecting to MCP server: {Server}", serverName);
                return false;
            }
        }

        /// <summary>
        /// Disconnects from a specific MCP server.
        /// </summary>
        public void DisconnectServer(string serverName)
        {
            if (_connections.TryRemove(serverName, out var connection))
            {
                connection.Dispose();
                App.Logger.Information("Disconnected from MCP server: {Server}", serverName);
            }
        }
    
        /// <summary>
        /// Connects to all configured MCP servers.
        /// </summary>
        public async Task ConnectAllAsync(CancellationToken cancellationToken = default)
        {
            LoadConfiguration();
        
            if (_config?.McpServers == null || _config.McpServers.Count == 0)
            {
                App.Logger.Warning("No MCP servers configured");
                return;
            }
        
            var tasks = new List<Task>();
            foreach (var serverName in _config.McpServers.Keys)
            {
                tasks.Add(ConnectServerAsync(serverName, cancellationToken));
            }
        
            await Task.WhenAll(tasks);
            App.Logger.Information("Connected to {Count} MCP servers", _connections.Count);
        }
    
        /// <summary>
        /// Disconnects from all MCP servers.
        /// </summary>
        public async Task DisconnectAllAsync()
        {
            var serverNames = _connections.Keys.ToList();
            foreach (var serverName in serverNames)
            {
                DisconnectServer(serverName);
            }
        
            await Task.CompletedTask;
            App.Logger.Information("Disconnected from all MCP servers");
        }

        /// <summary>
        /// Sends a tool call request to a server.
        /// </summary>
        public async Task<JObject> CallToolAsync(string serverName, string toolName, JObject parameters, CancellationToken cancellationToken = default)
        {
            if (!_connections.TryGetValue(serverName, out var connection))
            {
                App.Logger.Warning("Server not connected: {Server}", serverName);
                return null;
            }
        
            try
            {
                var request = new JObject
                {
                    ["jsonrpc"] = "2.0",
                    ["id"] = Guid.NewGuid().ToString(),
                    ["method"] = "tools/call",
                    ["params"] = new JObject
                    {
                        ["name"] = toolName,
                        ["arguments"] = parameters
                    }
                };
            
                return await connection.SendRequestAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error calling tool {Tool} on {Server}", toolName, serverName);
                return null;
            }
        }

        /// <summary>
        /// Gets a resource from a server.
        /// </summary>
        public async Task<JObject> GetResourceAsync(string serverName, string resourceUri, CancellationToken cancellationToken = default)
        {
            if (!_connections.TryGetValue(serverName, out var connection))
            {
                App.Logger.Warning("Server not connected: {Server}", serverName);
                return null;
            }
        
            try
            {
                var request = new JObject
                {
                    ["jsonrpc"] = "2.0",
                    ["id"] = Guid.NewGuid().ToString(),
                    ["method"] = "resources/read",
                    ["params"] = new JObject
                    {
                        ["uri"] = resourceUri
                    }
                };
            
                return await connection.SendRequestAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error getting resource {Uri} from {Server}", resourceUri, serverName);
                return null;
            }
        }

        /// <summary>
        /// Lists available tools from a server.
        /// </summary>
        public async Task<JArray> ListToolsAsync(string serverName, CancellationToken cancellationToken = default)
        {
            if (!_connections.TryGetValue(serverName, out var connection))
            {
                App.Logger.Warning("Server not connected: {Server}", serverName);
                return null;
            }
        
            try
            {
                var request = new JObject
                {
                    ["jsonrpc"] = "2.0",
                    ["id"] = Guid.NewGuid().ToString(),
                    ["method"] = "tools/list",
                    ["params"] = new JObject()
                };
            
                var response = await connection.SendRequestAsync(request, cancellationToken);
                return response?["result"]?["tools"] as JArray;
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error listing tools from {Server}", serverName);
                return null;
            }
        }

        /// <summary>
        /// Gets the status of a server connection.
        /// </summary>
        public McpConnectionStatus GetServerStatus(string serverName)
        {
            if (_connections.TryGetValue(serverName, out var connection))
            {
                return connection.Status;
            }
            return McpConnectionStatus.Disconnected;
        }

        /// <summary>
        /// Gets all server statuses.
        /// </summary>
        public Dictionary<string, McpConnectionStatus> GetAllStatuses()
        {
            var result = new Dictionary<string, McpConnectionStatus>();
        
            if (_config?.McpServers != null)
            {
                foreach (var serverName in _config.McpServers.Keys)
                {
                    result[serverName] = GetServerStatus(serverName);
                }
            }
        
            return result;
        }

        /// <summary>
        /// Gets the RAG server progress (0-100).
        /// </summary>
        public double GetRagProgress()
        {
            if (_connections.TryGetValue("evony-rag", out var connection))
            {
                return connection.Status == McpConnectionStatus.Connected ? 100 : 0;
            }
            return 0;
        }

        /// <summary>
        /// Gets the RTE server progress (0-100).
        /// </summary>
        public double GetRteProgress()
        {
            if (_connections.TryGetValue("evony-rte", out var connection))
            {
                return connection.Status == McpConnectionStatus.Connected ? 100 : 0;
            }
            return 0;
        }

        #endregion

        #region Private Methods

        private void LoadConfiguration()
        {
            lock (_configLock)
            {
                if (!File.Exists(ConfigPath))
                {
                    App.Logger.Warning("MCP config file not found: {Path}", ConfigPath);
                    _config = new McpConfig();
                    return;
                }
            
                try
                {
                    var json = File.ReadAllText(ConfigPath);
                    _config = JsonConvert.DeserializeObject<McpConfig>(json) ?? new McpConfig();
                    App.Logger.Information("Loaded MCP configuration with {Count} servers", _config.McpServers?.Count ?? 0);
                }
                catch (Exception ex)
                {
                    App.Logger.Error(ex, "Failed to load MCP configuration");
                    _config = new McpConfig();
                }
            }
        }

        private void StartHealthCheckTimer()
        {
            var interval = _config?.Connection?.HealthCheckIntervalMs ?? 30000;
        
            _healthCheckTimer = new Timer(
                async _ => await PerformHealthChecksAsync(),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMilliseconds(interval));
        }

        private async Task PerformHealthChecksAsync()
        {
            if (_disposed) return;
        
            foreach (var (name, connection) in _connections)
            {
                try
                {
                    var isHealthy = await connection.CheckHealthAsync();
                    if (!isHealthy)
                    {
                        App.Logger.Warning("Health check failed for {Server}, attempting reconnect", name);
                        await ConnectServerAsync(name);
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.Error(ex, "Health check error for {Server}", name);
                }
            }
        }

        private void OnMessageReceived(string serverName, JObject message)
        {
            try
            {
                MessageReceived?.Invoke(serverName, message);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error in MessageReceived handler");
            }
        }

        private void OnConnectionStatusChanged(string serverName, McpConnectionStatus status)
        {
            try
            {
                ConnectionStatusChanged?.Invoke(serverName, status);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error in ConnectionStatusChanged handler");
            }
        }

        private void OnServerError(string serverName, string error)
        {
            try
            {
                ServerError?.Invoke(serverName, error);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error in ServerError handler");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        
            App.Logger.Debug("Disposing McpConnectionManager...");
        
            _healthCheckTimer?.Dispose();
            _healthCheckTimer = null;
        
            foreach (var connection in _connections.Values)
            {
                try
                {
                    connection.Dispose();
                }
                catch { /* Ignore */ }
            }
            _connections.Clear();
        
            App.Logger.Debug("McpConnectionManager disposed");
        }

        #endregion
    }

    /// <summary>
    /// Represents a connection to a single MCP server.
    /// </summary>
    public sealed class McpServerConnection : IDisposable
    {
        private readonly string _name;
        private readonly McpServerConfig _config;
        private Process _process;
        private StreamWriter _stdin;
        private StreamReader _stdout;
        private CancellationTokenSource _readCts;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>> _pendingRequests = new TaskCompletionSource<JObject>>();
        private bool _disposed = false;
        private int _requestId = 0;

        public McpConnectionStatus Status { get; private set; } = McpConnectionStatus.Disconnected;
    
        public event Action<JObject> MessageReceived;
        public event Action<McpConnectionStatus> StatusChanged;
        public event Action<string> ErrorOccurred;

        public McpServerConnection(string name, McpServerConfig config)
        {
            _name = name;
            _config = config;
        }

        public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Status = McpConnectionStatus.Connecting;
                StatusChanged?.Invoke(Status);
            
                var startInfo = new ProcessStartInfo
                {
                    FileName = _config.Command,
                    Arguments = string.Join(" ", _config.Args ?? Array.Empty<string>()),
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };
            
                // Add environment variables
                if (_config.Env != null)
                {
                    foreach (var (key, value) in _config.Env)
                    {
                        startInfo.EnvironmentVariables[key] = value;
                    }
                }
            
                _process = new Process { StartInfo = startInfo };
                _process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        App.Logger.Debug("[{Server}] stderr: {Data}", _name, e.Data);
                    }
                };
            
                _process.Start();
                _process.BeginErrorReadLine();
            
                _stdin = _process.StandardInput;
                _stdout = _process.StandardOutput;
            
                // Start reading responses
                _readCts = new CancellationTokenSource();
                _ = ReadResponsesAsync(_readCts.Token);
            
                // Send initialize request
                var initResponse = await SendRequestAsync(new JObject
                {
                    ["jsonrpc"] = "2.0",
                    ["id"] = Interlocked.Increment(ref _requestId).ToString(),
                    ["method"] = "initialize",
                    ["params"] = new JObject
                    {
                        ["protocolVersion"] = "2024-11-05",
                        ["capabilities"] = new JObject(),
                        ["clientInfo"] = new JObject
                        {
                            ["name"] = "SvonyBrowser",
                            ["version"] = "1.0.0"
                        }
                    }
                }, cancellationToken);
            
                if (initResponse != null)
                {
                    // Send initialized notification
                    await SendNotificationAsync(new JObject
                    {
                        ["jsonrpc"] = "2.0",
                        ["method"] = "notifications/initialized"
                    });
                
                    Status = McpConnectionStatus.Connected;
                    StatusChanged?.Invoke(Status);
                    return true;
                }
            
                Status = McpConnectionStatus.Error;
                StatusChanged?.Invoke(Status);
                return false;
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to start MCP server: {Name}", _name);
                Status = McpConnectionStatus.Error;
                StatusChanged?.Invoke(Status);
                ErrorOccurred?.Invoke(ex.Message);
                return false;
            }
        }

        public async Task<JObject> SendRequestAsync(JObject request, CancellationToken cancellationToken = default)
        {
            if (_disposed || _stdin == null) return null;
        
            var id = request["id"]?.ToString() ?? Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<JObject>();
            _pendingRequests[id] = tcs;
        
            try
            {
                var json = request.ToString(Formatting.None);
                await _stdin.WriteLineAsync(json);
                await _stdin.FlushAsync();
            
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); // TODO: Add using block for proper disposal
    using SvonyBrowser.Helpers;
                cts.CancelAfter(TimeSpan.FromSeconds(30));
            
                var response = await tcs.Task.WaitAsync(cts.Token);
                return response;
            }
            catch (OperationCanceledException)
            {
                App.Logger.Warning("Request timed out: {Id}", id);
                return null;
            }
            finally
            {
                _pendingRequests.TryRemove(id, out _);
            }
        }

        public async Task SendNotificationAsync(JObject notification)
        {
            if (_disposed || _stdin == null) return;
        
            var json = notification.ToString(Formatting.None);
            await _stdin.WriteLineAsync(json);
            await _stdin.FlushAsync();
        }

        public async Task<bool> CheckHealthAsync()
        {
            if (_disposed || _process == null || _process.HasExited)
            {
                return false;
            }
        
            try
            {
                var response = await SendRequestAsync(new JObject
                {
                    ["jsonrpc"] = "2.0",
                    ["id"] = Interlocked.Increment(ref _requestId).ToString(),
                    ["method"] = "ping"
                });
            
                return response != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task ReadResponsesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _stdout != null)
                {
                    // ReadLineAsync with CancellationToken is .NET 7+ only, use Task.Run wrapper
                    var line = await Task.Run(() => _stdout.ReadLineAsync(), cancellationToken);
                    if (line == null) break;
                
                    try
                    {
                        var message = JObject.Parse(line);
                    
                        // Check if this is a response to a pending request
                        var id = message["id"]?.ToString();
                        if (id != null && _pendingRequests.TryRemove(id, out var tcs))
                        {
                            tcs.TrySetResult(message);
                        }
                        else
                        {
                            // It's a notification or unsolicited message
                            MessageReceived?.Invoke(message);
                        }
                    }
                    catch (JsonReaderException ex)
                    {
                        App.Logger.Warning("Invalid JSON from {Server}: {Error}", _name, ex.Message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when disposing
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error reading from {Server}", _name);
                ErrorOccurred?.Invoke(ex.Message);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        
            Status = McpConnectionStatus.Disconnected;
            StatusChanged?.Invoke(Status);
        
            _readCts?.Cancel();
            _readCts?.Dispose();
        
            try
            {
                _stdin?.Dispose();
                _stdout?.Dispose();
            
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill();
                    _process.WaitForExit(5000);
                }
                _process?.Dispose();
            }
            catch { /* Ignore */ }
        
            // Complete any pending requests with null
            foreach (var tcs in _pendingRequests.Values)
            {
                tcs.TrySetResult(null!);
            }
            _pendingRequests.Clear();
        }
    }

    /// <summary>
    /// MCP connection status.
    /// </summary>
    public enum McpConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    /// <summary>
    /// MCP configuration model.
    /// </summary>
    public class McpConfig
    {
        [JsonProperty("mcpServers")]
        public Dictionary<string, McpServerConfig> McpServers { get; set; }
    
        [JsonProperty("connection")]
        public McpConnectionConfig? Connection { get; set; }
    
        [JsonProperty("logging")]
        public McpLoggingConfig? Logging { get; set; }
    }

    public class McpServerConfig
    {
        [JsonProperty("command")]
        public string Command { get; set; } = "node";
    
        [JsonProperty("args")]
        public string[]? Args { get; set; }
    
        [JsonProperty("env")]
        public Dictionary<string, string> Env { get; set; }
    
        [JsonProperty("description")]
        public string Description { get; set; }
    
        [JsonProperty("autoConnect")]
        public bool AutoConnect { get; set; } = true;
    
        [JsonProperty("healthCheck")]
        public McpHealthCheckConfig? HealthCheck { get; set; }
    }

    public class McpConnectionConfig
    {
        [JsonProperty("maxRetries")]
        public int MaxRetries { get; set; } = 3;
    
        [JsonProperty("retryDelayMs")]
        public int RetryDelayMs { get; set; } = 5000;
    
        [JsonProperty("timeoutMs")]
        public int TimeoutMs { get; set; } = 30000;
    
        [JsonProperty("healthCheckIntervalMs")]
        public int HealthCheckIntervalMs { get; set; } = 30000;
    }

    public class McpHealthCheckConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;
    
        [JsonProperty("intervalMs")]
        public int IntervalMs { get; set; } = 30000;
    }

    public class McpLoggingConfig
    {
        [JsonProperty("level")]
        public string Level { get; set; } = "info";
    
        [JsonProperty("file")]
        public string File { get; set; }
    }

}