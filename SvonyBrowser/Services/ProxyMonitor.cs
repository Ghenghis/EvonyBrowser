using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Monitors the Fiddler proxy connection status with thread-safe operations.
    /// Implements IDisposable for proper resource cleanup.
    /// </summary>
    public sealed class ProxyMonitor : IDisposable
    {
        #region Singleton

        private static readonly Lazy<ProxyMonitor> _lazyInstance =
            new Lazy<ProxyMonitor>(() => new ProxyMonitor(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static ProxyMonitor Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly object _lock = new object();
        private double _throughputKBps = 0;
        private long _bytesTransferred = 0;
        private DateTime _lastThroughputUpdate = DateTime.UtcNow;
        private Timer _monitorTimer;
        private bool _lastStatus = false;
        private bool _disposed = false;
        private int _checkInProgress = 0; // 0 = not in progress, 1 = in progress (for interlocked)

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the proxy is currently available.
        /// </summary>
        public bool IsProxyAvailable
        {
            get
            {
                lock (_lock)
                {
                    return _lastStatus;
                }
            }
        }

        /// <summary>
        /// Gets the host being monitored.
        /// </summary>
        public string Host { get; } = "127.0.0.1";

        /// <summary>
        /// Gets the port being monitored.
        /// </summary>
        public int Port { get; } = 8888;

        /// <summary>
        /// Gets the monitoring interval in milliseconds.
        /// </summary>
        public int MonitorIntervalMs { get; } = 10000;

        #endregion

        #region Events

        /// <summary>
        /// Fired when proxy status changes. Parameter is true if proxy is available.
        /// </summary>
        public event Action<bool> ProxyStatusChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new ProxyMonitor and starts monitoring.
        /// </summary>
        /// <param name="host">Proxy host (default: 127.0.0.1)</param>
        /// <param name="port">Proxy port (default: 8888)</param>
        /// <param name="intervalMs">Check interval in milliseconds (default: 10000)</param>
        public ProxyMonitor(string host = "127.0.0.1", int port = 8888, int intervalMs = 10000)
        {
            Host = host;
            Port = port;
            MonitorIntervalMs = intervalMs;

            // Start monitoring timer - initial check after 100ms, then every intervalMs
            _monitorTimer = new Timer(
                OnTimerCallback, 
                null, 
                TimeSpan.FromMilliseconds(100), 
                TimeSpan.FromMilliseconds(intervalMs));

            App.Logger.Debug("ProxyMonitor started for {Host}:{Port} with {Interval}ms interval", host, port, intervalMs);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs a single proxy availability check.
        /// </summary>
        /// <param name="host">Host to check</param>
        /// <param name="port">Port to check</param>
        /// <param name="timeoutMs">Connection timeout in milliseconds</param>
        /// <returns>True if proxy is reachable</returns>
        public async Task<bool> CheckProxyAsync(string host, int port, int timeoutMs = 2000)
        {
            if (_disposed) return false;

            try
            {
                var cts = new CancellationTokenSource(timeoutMs); // TODO: Add using block for proper disposal
                var client = new TcpClient(); // TODO: Add using block for proper disposal
            
                // .NET Framework 4.6.2 ConnectAsync doesn't take CancellationToken
                var connectTask = client.ConnectAsync(host, port);
                if (await Task.WhenAny(connectTask, Task.Delay(timeoutMs, cts.Token)) == connectTask)
                {
                    await connectTask; // Propagate any exceptions
                    return client.Connected;
                }
                return false;
            }
            catch (OperationCanceledException)
            {
                // Timeout - proxy not responding
                return false;
            }
            catch (SocketException)
            {
                // Connection refused - proxy not running
                return false;
            }
            catch (Exception ex)
            {
                App.Logger.Debug("Proxy check failed: {Error}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Forces an immediate status check.
        /// </summary>
        public async Task ForceCheckAsync()
        {
            if (_disposed) return;
            await CheckAndNotifyAsync();
        }

        /// <summary>
        /// Pauses monitoring (timer stops).
        /// </summary>
        public void Pause()
        {
            _monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            App.Logger.Debug("ProxyMonitor paused");
        }

        /// <summary>
        /// Resumes monitoring.
        /// </summary>
        public void Resume()
        {
            _monitorTimer?.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(MonitorIntervalMs));
            App.Logger.Debug("ProxyMonitor resumed");
        }

        /// <summary>
        /// Gets the current throughput in KB/s.
        /// </summary>
        public double GetThroughputKBps()
        {
            lock (_lock)
            {
                return _throughputKBps;
            }
        }

        /// <summary>
        /// Records bytes transferred for throughput calculation.
        /// </summary>
        public void RecordBytesTransferred(long bytes)
        {
            lock (_lock)
            {
                _bytesTransferred += bytes;
                var now = DateTime.UtcNow;
                var elapsed = (now - _lastThroughputUpdate).TotalSeconds;
            
                if (elapsed >= 1.0)
                {
                    _throughputKBps = (_bytesTransferred / 1024.0) / elapsed;
                    _bytesTransferred = 0;
                    _lastThroughputUpdate = now;
                }
            }
        }

        #endregion

        #region Private Methods

        private async void OnTimerCallback(object state)
        {
            if (_disposed) return;
        
            // Prevent overlapping checks using interlocked
            if (Interlocked.CompareExchange(ref _checkInProgress, 1, 0) != 0)
            {
                return; // Check already in progress
            }

            try
            {
                await CheckAndNotifyAsync();
            }
            finally
            {
                Interlocked.Exchange(ref _checkInProgress, 0);
            }
        }

        private async Task CheckAndNotifyAsync()
        {
            if (_disposed) return;

            try
            {
                var currentStatus = await CheckProxyAsync(Host, Port);
            
                bool statusChanged;
                lock (_lock)
                {
                    statusChanged = currentStatus != _lastStatus;
                    _lastStatus = currentStatus;
                }

                if (statusChanged)
                {
                    App.Logger.Information("Proxy status changed: {Status}", 
                        currentStatus ? "Connected" : "Disconnected");
                
                    // Fire event on thread pool to avoid blocking timer
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            ProxyStatusChanged?.Invoke(currentStatus);
                        }
                        catch (Exception ex)
                        {
                            App.Logger.Error(ex, "Error in ProxyStatusChanged event handler");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error during proxy status check");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            App.Logger.Debug("Disposing ProxyMonitor...");

            // Stop and dispose timer
            try
            {
                _monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _monitorTimer?.Dispose();
                _monitorTimer = null;
            }
            catch (Exception ex)
            {
                App.Logger.Debug("Error disposing timer: {Error}", ex.Message);
            }

            // Clear event handlers to prevent memory leaks
            ProxyStatusChanged = null;

            App.Logger.Debug("ProxyMonitor disposed");
        }

        #endregion
    }

}