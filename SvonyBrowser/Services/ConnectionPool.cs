using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SvonyBrowser.Helpers;

namespace SvonyBrowser.Services
{
    /// <summary>
    /// Connection pool service for Svony Browser v6.0 Borg Edition.
    /// Manages HTTP clients, WebSocket connections, and other network resources efficiently.
    /// </summary>
    public sealed class ConnectionPool : IDisposable
    {
        private static readonly Lazy<ConnectionPool> _instance = new Lazy<ConnectionPool>(() => new ConnectionPool());
        public static ConnectionPool Instance => _instance.Value;
        
        private readonly ConcurrentDictionary<string, PooledHttpClient> _httpClients;
        private readonly ConcurrentDictionary<string, PooledWebSocket> _webSockets;
        private readonly SemaphoreSlim _httpSemaphore;
        private readonly SemaphoreSlim _wsSemaphore;
        private readonly System.Timers.Timer _cleanupTimer;
        
        private readonly int _maxHttpClients = 10;
        private readonly int _maxWebSockets = 20;
        private readonly TimeSpan _idleTimeout = TimeSpan.FromMinutes(5);
        private bool _disposed;
        
        public event EventHandler<ConnectionEventArgs> ConnectionCreated;
        public event EventHandler<ConnectionEventArgs> ConnectionClosed;
        
        private ConnectionPool()
        {
            _httpClients = new ConcurrentDictionary<string, PooledHttpClient>();
            _webSockets = new ConcurrentDictionary<string, PooledWebSocket>();
            _httpSemaphore = new SemaphoreSlim(_maxHttpClients);
            _wsSemaphore = new SemaphoreSlim(_maxWebSockets);
            
            // Cleanup idle connections every minute
            _cleanupTimer = new System.Timers.Timer(60000);
            _cleanupTimer.Elapsed += (s, e) => CleanupIdleConnections();
            _cleanupTimer.AutoReset = true;
            _cleanupTimer.Start();
        }
        
        #region HTTP Client Pool
        
        /// <summary>
        /// Gets or creates an HTTP client for the specified base URL.
        /// </summary>
        public async Task<HttpClient> GetHttpClientAsync(string baseUrl, CancellationToken cancellationToken = default)
        {
            var key = new Uri(baseUrl).Host;
            
            // Try to get existing client
            if (_httpClients.TryGetValue(key, out var pooled) && !pooled.IsExpired)
            {
                pooled.LastUsed = DateTime.Now;
                return pooled.Client;
            }
            
            // Wait for available slot
            await _httpSemaphore.WaitAsync(cancellationToken);
            
            try
            {
                // Double-check after acquiring semaphore
                if (_httpClients.TryGetValue(key, out pooled) && !pooled.IsExpired)
                {
                    pooled.LastUsed = DateTime.Now;
                    _httpSemaphore.Release();
                    return pooled.Client;
                }
                
                // Create new client
                var handler = new HttpClientHandler
                {
                    // .NET Framework 4.6.2 compatible settings
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                };
                
                var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri(baseUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                };
                
                client.DefaultRequestHeaders.Add("User-Agent", "SvonyBrowser/6.0 Borg Edition");
                
                pooled = new PooledHttpClient
                {
                    Client = client,
                    Handler = handler,
                    Key = key,
                    Created = DateTime.Now,
                    LastUsed = DateTime.Now
                };
                
                _httpClients[key] = pooled;
                
                ConnectionCreated?.Invoke(this, new ConnectionEventArgs("HTTP", key));
                
                return client;
            }
            catch
            {
                _httpSemaphore.Release();
                throw;
            }
        }
        
        /// <summary>
        /// Releases an HTTP client back to the pool.
        /// </summary>
        public void ReleaseHttpClient(string baseUrl)
        {
            var key = new Uri(baseUrl).Host;
            
            if (_httpClients.TryGetValue(key, out var pooled))
            {
                pooled.LastUsed = DateTime.Now;
            }
            
            _httpSemaphore.Release();
        }
        
        #endregion
        
        #region WebSocket Pool
        
        /// <summary>
        /// Gets or creates a WebSocket connection for the specified URL.
        /// </summary>
        public async Task<ClientWebSocket> GetWebSocketAsync(string url, CancellationToken cancellationToken = default)
        {
            var key = url;
            
            // Try to get existing connection
            if (_webSockets.TryGetValue(key, out var pooled) && 
                pooled.Socket.State == WebSocketState.Open)
            {
                pooled.LastUsed = DateTime.Now;
                return pooled.Socket;
            }
            
            // Wait for available slot
            await _wsSemaphore.WaitAsync(cancellationToken);
            
            try
            {
                // Double-check after acquiring semaphore
                if (_webSockets.TryGetValue(key, out pooled) && 
                    pooled.Socket.State == WebSocketState.Open)
                {
                    pooled.LastUsed = DateTime.Now;
                    _wsSemaphore.Release();
                    return pooled.Socket;
                }
                
                // Close existing dead connection
                if (pooled != null)
                {
                    await CloseWebSocketAsync(pooled);
                    _webSockets.TryRemove(key, out _);
                }
                
                // Create new connection
                var socket = new ClientWebSocket();
                socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
                
                await socket.ConnectAsync(new Uri(url), cancellationToken);
                
                pooled = new PooledWebSocket
                {
                    Socket = socket,
                    Key = key,
                    Created = DateTime.Now,
                    LastUsed = DateTime.Now
                };
                
                _webSockets[key] = pooled;
                
                ConnectionCreated?.Invoke(this, new ConnectionEventArgs("WebSocket", key));
                
                return socket;
            }
            catch
            {
                _wsSemaphore.Release();
                throw;
            }
        }
        
        /// <summary>
        /// Releases a WebSocket connection back to the pool.
        /// </summary>
        public void ReleaseWebSocket(string url)
        {
            if (_webSockets.TryGetValue(url, out var pooled))
            {
                pooled.LastUsed = DateTime.Now;
            }
            
            _wsSemaphore.Release();
        }
        
        /// <summary>
        /// Closes and removes a WebSocket connection.
        /// </summary>
        public async Task CloseWebSocketAsync(string url)
        {
            if (_webSockets.TryRemove(url, out var pooled))
            {
                await CloseWebSocketAsync(pooled);
                ConnectionClosed?.Invoke(this, new ConnectionEventArgs("WebSocket", url));
            }
        }
        
        private async Task CloseWebSocketAsync(PooledWebSocket pooled)
        {
            try
            {
                if (pooled.Socket.State == WebSocketState.Open)
                {
                    await pooled.Socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Pool cleanup",
                        CancellationToken.None
                    );
                }
                
                pooled.Socket.Dispose();
            }
            catch { }
        }
        
        #endregion
        
        #region Cleanup
        
        private void CleanupIdleConnections()
        {
            var now = DateTime.Now;
            
            // Cleanup idle HTTP clients
            foreach (var kvp in _httpClients)
            {
                if (now - kvp.Value.LastUsed > _idleTimeout)
                {
                    if (_httpClients.TryRemove(kvp.Key, out var pooled))
                    {
                        pooled.Client.Dispose();
                        pooled.Handler.Dispose();
                        ConnectionClosed?.Invoke(this, new ConnectionEventArgs("HTTP", kvp.Key));
                    }
                }
            }
            
            // Cleanup dead WebSockets
            foreach (var kvp in _webSockets)
            {
                if (kvp.Value.Socket.State != WebSocketState.Open ||
                    now - kvp.Value.LastUsed > _idleTimeout)
                {
                    if (_webSockets.TryRemove(kvp.Key, out var pooled))
                    {
                        _ = CloseWebSocketAsync(pooled);
                        ConnectionClosed?.Invoke(this, new ConnectionEventArgs("WebSocket", kvp.Key));
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets connection pool statistics.
        /// </summary>
        public ConnectionPoolStats GetStats()
        {
            return new ConnectionPoolStats
            {
                ActiveHttpClients = _httpClients.Count,
                ActiveWebSockets = _webSockets.Count,
                MaxHttpClients = _maxHttpClients,
                MaxWebSockets = _maxWebSockets,
                AvailableHttpSlots = _httpSemaphore.CurrentCount,
                AvailableWebSocketSlots = _wsSemaphore.CurrentCount
            };
        }
        
        /// <summary>
        /// Closes all connections.
        /// </summary>
        public async Task CloseAllAsync()
        {
            // Close all HTTP clients
            foreach (var kvp in _httpClients)
            {
                if (_httpClients.TryRemove(kvp.Key, out var pooled))
                {
                    pooled.Client.Dispose();
                    pooled.Handler.Dispose();
                }
            }
            
            // Close all WebSockets
            foreach (var kvp in _webSockets)
            {
                if (_webSockets.TryRemove(kvp.Key, out var pooled))
                {
                    await CloseWebSocketAsync(pooled);
                }
            }
        }
        
        #endregion
        
        #region IDisposable
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            _cleanupTimer?.Stop();
            _cleanupTimer?.Dispose();
            
            // Synchronously close all connections
            foreach (var kvp in _httpClients)
            {
                kvp.Value.Client.Dispose();
                kvp.Value.Handler.Dispose();
            }
            
            foreach (var kvp in _webSockets)
            {
                try
                {
                    kvp.Value.Socket.Dispose();
                }
                catch { }
            }
            
            _httpClients.Clear();
            _webSockets.Clear();
            
            _httpSemaphore.Dispose();
            _wsSemaphore.Dispose();
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    internal class PooledHttpClient
    {
        public HttpClient Client { get; set; } = null!;
        public HttpClientHandler Handler { get; set; } = null!;
        public string Key { get; set; } = "";
        public DateTime Created { get; set; }
        public DateTime LastUsed { get; set; }
        
        public bool IsExpired => DateTime.Now - Created > TimeSpan.FromMinutes(30);
    }
    
    internal class PooledWebSocket
    {
        public ClientWebSocket Socket { get; set; } = null!;
        public string Key { get; set; } = "";
        public DateTime Created { get; set; }
        public DateTime LastUsed { get; set; }
    }
    
    public class ConnectionPoolStats
    {
        public int ActiveHttpClients { get; set; }
        public int ActiveWebSockets { get; set; }
        public int MaxHttpClients { get; set; }
        public int MaxWebSockets { get; set; }
        public int AvailableHttpSlots { get; set; }
        public int AvailableWebSocketSlots { get; set; }
    }
    
    public class ConnectionEventArgs : EventArgs
    {
        public string Type { get; }
        public string Key { get; }
        public DateTime Timestamp { get; }
        
        public ConnectionEventArgs(string type, string key)
        {
            Type = type;
            Key = key;
            Timestamp = DateTime.Now;
        }
    }
    
    #endregion
}
