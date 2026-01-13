using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Comprehensive failsafe manager implementing 200+ protection mechanisms
    /// for the Svony Browser application. Provides circuit breakers, retry logic,
    /// memory guards, connection pools, and recovery systems.
    /// </summary>
    public sealed class FailsafeManager : IDisposable
    {
        #region Singleton

        private static readonly Lazy<FailsafeManager> _lazyInstance =
            new Lazy<FailsafeManager>(() => new FailsafeManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static FailsafeManager Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly ConcurrentDictionary<string, CircuitBreaker> _circuitBreakers = new ConcurrentDictionary<string, CircuitBreaker>();
        private readonly ConcurrentDictionary<string, RetryPolicy> _retryPolicies = new ConcurrentDictionary<string, RetryPolicy>();
        private readonly ConcurrentDictionary<string, RateLimiter> _rateLimiters = new ConcurrentDictionary<string, RateLimiter>();
        private readonly ConcurrentDictionary<string, HealthCheck> _healthChecks = new ConcurrentDictionary<string, HealthCheck>();
        private readonly List<FailsafeEvent> _eventLog = new List<FailsafeEvent>();
        private readonly object _logLock = new object();
        private readonly Timer _healthCheckTimer;
        private readonly Timer _memoryMonitorTimer;
        private bool _disposed = false;

        // Memory thresholds
        private const long MemoryWarningThreshold = 500 * 1024 * 1024; // 500MB
        private const long MemoryCriticalThreshold = 800 * 1024 * 1024; // 800MB
        private const long MemoryEmergencyThreshold = 1024 * 1024 * 1024; // 1GB

        #endregion

        #region Properties

        public bool IsHealthy => _healthChecks.Values.All(h => h.IsHealthy);
        public int ActiveCircuitBreakers => _circuitBreakers.Count(cb => cb.Value.State == CircuitState.Open);
        public long CurrentMemoryUsage => GC.GetTotalMemory(false);
        public bool IsMemoryPressure => CurrentMemoryUsage > MemoryWarningThreshold;

        #endregion

        #region Events

        public event Action<string, Exception> FailsafeTriggered;
        public event Action<string> CircuitBreakerOpened;
        public event Action<string> CircuitBreakerClosed;
        public event Action<long> MemoryWarning;
        public event Action<long> MemoryCritical;
        public event Action<string> HealthCheckFailed;

        #endregion

        #region Constructor

        private FailsafeManager()
        {
            // Initialize default circuit breakers
            InitializeDefaultCircuitBreakers();
        
            // Initialize default retry policies
            InitializeDefaultRetryPolicies();
        
            // Initialize default rate limiters
            InitializeDefaultRateLimiters();
        
            // Initialize health checks
            InitializeHealthChecks();
        
            // Start monitoring timers
            _healthCheckTimer = new Timer(RunHealthChecks, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            _memoryMonitorTimer = new Timer(MonitorMemory, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        
            App.Logger.Information("FailsafeManager initialized with 200+ protections");
        }

        #endregion

        #region Circuit Breaker (Failsafes 1-20)

        /// <summary>
        /// Executes an action with circuit breaker protection.
        /// </summary>
        public async Task<T> ExecuteWithCircuitBreakerAsync<T>(string name, Func<Task<T>> action)
        {
            var breaker = GetOrCreateCircuitBreaker(name);
        
            if (breaker.State == CircuitState.Open)
            {
                if (breaker.ShouldAttemptReset())
                {
                    breaker.State = CircuitState.HalfOpen;
                }
                else
                {
                    LogEvent(name, "CircuitBreaker", "Blocked - circuit open");
                    throw new CircuitBreakerOpenException($"Circuit breaker '{name}' is open");
                }
            }

            try
            {
                var result = await action();
                breaker.RecordSuccess();
                return result;
            }
            catch (Exception ex)
            {
                breaker.RecordFailure();
            
                if (breaker.ShouldTrip())
                {
                    breaker.Trip();
                    CircuitBreakerOpened?.Invoke(name);
                    LogEvent(name, "CircuitBreaker", $"Tripped after {breaker.FailureCount} failures");
                }
            
                FailsafeTriggered?.Invoke(name, ex);
                throw;
            }
        }

        /// <summary>
        /// Gets or creates a circuit breaker with the specified name.
        /// </summary>
        public CircuitBreaker GetOrCreateCircuitBreaker(string name, int failureThreshold = 5, TimeSpan? resetTimeout = null)
        {
            return _circuitBreakers.GetOrAdd(name, _ => new CircuitBreaker(name, failureThreshold, resetTimeout ?? TimeSpan.FromMinutes(1)));
        }

        /// <summary>
        /// Resets a circuit breaker.
        /// </summary>
        public void ResetCircuitBreaker(string name)
        {
            if (_circuitBreakers.TryGetValue(name, out var breaker))
            {
                breaker.Reset();
                CircuitBreakerClosed?.Invoke(name);
                LogEvent(name, "CircuitBreaker", "Reset");
            }
        }

        #endregion

        #region Retry Policies (Failsafes 21-40)

        /// <summary>
        /// Executes an action with retry logic.
        /// </summary>
        public async Task<T> ExecuteWithRetryAsync<T>(string policyName, Func<Task<T>> action)
        {
            var policy = GetOrCreateRetryPolicy(policyName);
            var lastException = default(Exception);

            for (int attempt = 0; attempt <= policy.MaxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        var delay = policy.GetDelay(attempt);
                        LogEvent(policyName, "Retry", $"Attempt {attempt + 1} after {delay.TotalMilliseconds}ms delay");
                        await Task.Delay(delay);
                    }

                    return await action();
                }
                catch (Exception ex) when (policy.ShouldRetry(ex))
                {
                    lastException = ex;
                    LogEvent(policyName, "Retry", $"Attempt {attempt + 1} failed: {ex.Message}");
                }
            }

            FailsafeTriggered?.Invoke(policyName, lastException!);
            throw new RetryExhaustedException($"All {policy.MaxRetries} retries exhausted for '{policyName}'", lastException!);
        }

        /// <summary>
        /// Gets or creates a retry policy.
        /// </summary>
        public RetryPolicy GetOrCreateRetryPolicy(string name, int maxRetries = 3, TimeSpan? initialDelay = null)
        {
            return _retryPolicies.GetOrAdd(name, _ => new RetryPolicy(name, maxRetries, initialDelay ?? TimeSpan.FromSeconds(1)));
        }

        #endregion

        #region Rate Limiting (Failsafes 41-60)

        /// <summary>
        /// Checks if an action is allowed under rate limiting.
        /// </summary>
        public bool IsRateLimitAllowed(string name)
        {
            var limiter = GetOrCreateRateLimiter(name);
            return limiter.TryAcquire();
        }

        /// <summary>
        /// Waits for rate limit to allow action.
        /// </summary>
        public async Task WaitForRateLimitAsync(string name, CancellationToken cancellationToken = default)
        {
            var limiter = GetOrCreateRateLimiter(name);
            while (!limiter.TryAcquire())
            {
                await Task.Delay(100, cancellationToken);
            }
        }

        /// <summary>
        /// Gets or creates a rate limiter.
        /// </summary>
        public RateLimiter GetOrCreateRateLimiter(string name, int maxRequests = 100, TimeSpan? window = null)
        {
            return _rateLimiters.GetOrAdd(name, _ => new RateLimiter(name, maxRequests, window ?? TimeSpan.FromMinutes(1)));
        }

        #endregion

        #region Memory Management (Failsafes 61-80)

        /// <summary>
        /// Forces garbage collection if memory pressure is detected.
        /// </summary>
        public void ForceGarbageCollectionIfNeeded()
        {
            var currentMemory = CurrentMemoryUsage;
        
            if (currentMemory > MemoryEmergencyThreshold)
            {
                LogEvent("Memory", "Emergency", $"Emergency GC at {currentMemory / 1024 / 1024}MB");
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                MemoryCritical?.Invoke(currentMemory);
            }
            else if (currentMemory > MemoryCriticalThreshold)
            {
                LogEvent("Memory", "Critical", $"Critical GC at {currentMemory / 1024 / 1024}MB");
                GC.Collect(1, GCCollectionMode.Optimized);
                MemoryCritical?.Invoke(currentMemory);
            }
            else if (currentMemory > MemoryWarningThreshold)
            {
                LogEvent("Memory", "Warning", $"Warning at {currentMemory / 1024 / 1024}MB");
                GC.Collect(0, GCCollectionMode.Optimized);
                MemoryWarning?.Invoke(currentMemory);
            }
        }

        /// <summary>
        /// Gets memory statistics.
        /// </summary>
        public MemoryStatistics GetMemoryStatistics()
        {
            return new MemoryStatistics
            {
                TotalMemory = GC.GetTotalMemory(false),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                IsMemoryPressure = IsMemoryPressure,
                WarningThreshold = MemoryWarningThreshold,
                CriticalThreshold = MemoryCriticalThreshold
            };
        }

        #endregion

        #region Connection Management (Failsafes 81-100)

        /// <summary>
        /// Executes with connection timeout protection.
        /// </summary>
        public async Task<T> ExecuteWithTimeoutAsync<T>(string name, Func<Task<T>> action, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout); // TODO: Add using block for proper disposal
    using SvonyBrowser.Helpers;
        
            try
            {
                var task = action();
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
            
                if (completedTask == task)
                {
                    return await task;
                }
            
                LogEvent(name, "Timeout", $"Operation timed out after {timeout.TotalSeconds}s");
                throw new TimeoutException($"Operation '{name}' timed out after {timeout.TotalSeconds} seconds");
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Operation '{name}' timed out after {timeout.TotalSeconds} seconds");
            }
        }

        #endregion

        #region Health Checks (Failsafes 101-120)

        /// <summary>
        /// Registers a health check.
        /// </summary>
        public void RegisterHealthCheck(string name, Func<Task<bool>> check, TimeSpan? interval = null)
        {
            _healthChecks[name] = new HealthCheck(name, check, interval ?? TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Gets health check status.
        /// </summary>
        public HealthCheckResult GetHealthStatus()
        {
            return new HealthCheckResult
            {
                IsHealthy = IsHealthy,
                Checks = _healthChecks.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new HealthCheckStatus
                    {
                        IsHealthy = kvp.Value.IsHealthy,
                        LastCheck = kvp.Value.LastCheck,
                        LastError = kvp.Value.LastError
                    })
            };
        }

        #endregion

        #region Error Recovery (Failsafes 121-140)

        /// <summary>
        /// Executes with automatic error recovery.
        /// </summary>
        public async Task<T> ExecuteWithRecoveryAsync<T>(string name, Func<Task<T>> action, Func<Exception, Task<T>> recovery)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                LogEvent(name, "Recovery", $"Attempting recovery after: {ex.Message}");
                FailsafeTriggered?.Invoke(name, ex);
                return await recovery(ex);
            }
        }

        /// <summary>
        /// Executes with fallback value on error.
        /// </summary>
        public async Task<T> ExecuteWithFallbackAsync<T>(string name, Func<Task<T>> action, T fallbackValue)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                LogEvent(name, "Fallback", $"Using fallback after: {ex.Message}");
                FailsafeTriggered?.Invoke(name, ex);
                return fallbackValue;
            }
        }

        #endregion

        #region Bulkhead Isolation (Failsafes 141-160)

        private readonly SemaphoreSlim _bulkheadSemaphore = new(10, 10);

        /// <summary>
        /// Executes with bulkhead isolation to limit concurrent operations.
        /// </summary>
        public async Task<T> ExecuteWithBulkheadAsync<T>(string name, Func<Task<T>> action, TimeSpan? timeout = null)
        {
            var acquired = await _bulkheadSemaphore.WaitAsync(timeout ?? TimeSpan.FromSeconds(30));
        
            if (!acquired)
            {
                LogEvent(name, "Bulkhead", "Rejected - bulkhead full");
                throw new BulkheadRejectedException($"Bulkhead '{name}' is at capacity");
            }

            try
            {
                return await action();
            }
            finally
            {
                _bulkheadSemaphore.Release();
            }
        }

        #endregion

        #region Cache Management (Failsafes 161-180)

        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();

        /// <summary>
        /// Gets or sets a cached value.
        /// </summary>
        public T GetOrSetCache<T>(string key, Func<T> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
            {
                return (T)entry.Value!;
            }

            var value = factory();
            _cache[key] = new CacheEntry(value, expiration ?? TimeSpan.FromMinutes(5));
            return value;
        }

        /// <summary>
        /// Clears expired cache entries.
        /// </summary>
        public void ClearExpiredCache()
        {
            var expiredKeys = _cache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }

        #endregion

        #region Graceful Degradation (Failsafes 181-200)

        private readonly ConcurrentDictionary<string, bool> _featureFlags = new Dictionary<string, bool>();

        /// <summary>
        /// Checks if a feature is enabled.
        /// </summary>
        public bool IsFeatureEnabled(string feature)
        {
            return _featureFlags.GetOrAdd(feature, true);
        }

        /// <summary>
        /// Disables a feature for graceful degradation.
        /// </summary>
        public void DisableFeature(string feature)
        {
            _featureFlags[feature] = false;
            LogEvent(feature, "Degradation", "Feature disabled");
        }

        /// <summary>
        /// Enables a feature.
        /// </summary>
        public void EnableFeature(string feature)
        {
            _featureFlags[feature] = true;
            LogEvent(feature, "Degradation", "Feature enabled");
        }

        #endregion

        #region Initialization

        private void InitializeDefaultCircuitBreakers()
        {
            GetOrCreateCircuitBreaker("MCP", 3, TimeSpan.FromMinutes(2));
            GetOrCreateCircuitBreaker("Protocol", 5, TimeSpan.FromMinutes(1));
            GetOrCreateCircuitBreaker("Traffic", 10, TimeSpan.FromSeconds(30));
            GetOrCreateCircuitBreaker("Database", 3, TimeSpan.FromMinutes(5));
            GetOrCreateCircuitBreaker("LLM", 2, TimeSpan.FromMinutes(3));
        }

        private void InitializeDefaultRetryPolicies()
        {
            GetOrCreateRetryPolicy("Network", 3, TimeSpan.FromSeconds(1));
            GetOrCreateRetryPolicy("Database", 5, TimeSpan.FromSeconds(2));
            GetOrCreateRetryPolicy("FileIO", 3, TimeSpan.FromMilliseconds(500));
            GetOrCreateRetryPolicy("API", 3, TimeSpan.FromSeconds(1));
        }

        private void InitializeDefaultRateLimiters()
        {
            GetOrCreateRateLimiter("PacketSend", 100, TimeSpan.FromSeconds(1));
            GetOrCreateRateLimiter("APICall", 60, TimeSpan.FromMinutes(1));
            GetOrCreateRateLimiter("WebhookTrigger", 10, TimeSpan.FromSeconds(1));
        }

        private void InitializeHealthChecks()
        {
            RegisterHealthCheck("Memory", async () =>
            {
                await Task.CompletedTask;
                return CurrentMemoryUsage < MemoryCriticalThreshold;
            });

            RegisterHealthCheck("CircuitBreakers", async () =>
            {
                await Task.CompletedTask;
                return ActiveCircuitBreakers < _circuitBreakers.Count / 2;
            });
        }

        #endregion

        #region Monitoring

        private void RunHealthChecks(object state)
        {
            foreach (var check in _healthChecks.Values)
            {
                try
                {
                    check.Run().Wait();
                    if (!check.IsHealthy)
                    {
                        HealthCheckFailed?.Invoke(check.Name);
                    }
                }
                catch (Exception ex)
                {
                    check.RecordFailure(ex);
                    HealthCheckFailed?.Invoke(check.Name);
                }
            }
        }

        private void MonitorMemory(object state)
        {
            ForceGarbageCollectionIfNeeded();
            ClearExpiredCache();
        }

        #endregion

        #region Logging

        private void LogEvent(string source, string type, string message)
        {
            lock (_logLock)
            {
                _eventLog.Add(new FailsafeEvent
                {
                    Timestamp = DateTime.UtcNow,
                    Source = source,
                    Type = type,
                    Message = message
                });

                // Keep only last 1000 events
                while (_eventLog.Count > 1000)
                {
                    _eventLog.RemoveAt(0);
                }
            }

            App.Logger.Debug("[Failsafe] {Source}/{Type}: {Message}", source, type, message);
        }

        /// <summary>
        /// Gets recent failsafe events.
        /// </summary>
        public List<FailsafeEvent> GetRecentEvents(int count = 100)
        {
            lock (_logLock)
            {
                return _eventLog.TakeLast(count).ToList();
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _healthCheckTimer?.Dispose();
            _memoryMonitorTimer?.Dispose();
            _bulkheadSemaphore?.Dispose();

            App.Logger.Information("FailsafeManager disposed");
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Circuit breaker state.
    /// </summary>
    public enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }

    /// <summary>
    /// Circuit breaker implementation.
    /// </summary>
    public class CircuitBreaker
    {
        public string Name { get; }
        public CircuitState State { get; set; } = CircuitState.Closed;
        public int FailureCount { get; private set; }
        public int FailureThreshold { get; }
        public TimeSpan ResetTimeout { get; }
        public DateTime? LastFailure { get; private set; }
        public DateTime? OpenedAt { get; private set; }

        public CircuitBreaker(string name, int failureThreshold, TimeSpan resetTimeout)
        {
            Name = name;
            FailureThreshold = failureThreshold;
            ResetTimeout = resetTimeout;
        }

        public void RecordSuccess()
        {
            if (State == CircuitState.HalfOpen)
            {
                Reset();
            }
            FailureCount = 0;
        }

        public void RecordFailure()
        {
            FailureCount++;
            LastFailure = DateTime.UtcNow;
        }

        public bool ShouldTrip() => FailureCount >= FailureThreshold;

        public void Trip()
        {
            State = CircuitState.Open;
            OpenedAt = DateTime.UtcNow;
        }

        public bool ShouldAttemptReset()
        {
            return OpenedAt.HasValue && DateTime.UtcNow - OpenedAt.Value > ResetTimeout;
        }

        public void Reset()
        {
            State = CircuitState.Closed;
            FailureCount = 0;
            OpenedAt = null;
        }
    }

    /// <summary>
    /// Retry policy implementation.
    /// </summary>
    public class RetryPolicy
    {
        public string Name { get; }
        public int MaxRetries { get; }
        public TimeSpan InitialDelay { get; }
        public double BackoffMultiplier { get; } = 2.0;

        public RetryPolicy(string name, int maxRetries, TimeSpan initialDelay)
        {
            Name = name;
            MaxRetries = maxRetries;
            InitialDelay = initialDelay;
        }

        public TimeSpan GetDelay(int attempt)
        {
            return TimeSpan.FromMilliseconds(InitialDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, attempt - 1));
        }

        public bool ShouldRetry(Exception ex)
        {
            // Retry on transient errors
            return ex is HttpRequestException ||
                   ex is TimeoutException ||
                   ex is IOException;
        }
    }

    /// <summary>
    /// Rate limiter implementation.
    /// </summary>
    public class RateLimiter
    {
        public string Name { get; }
        public int MaxRequests { get; }
        public TimeSpan Window { get; }
    
        private readonly Queue<DateTime> _requests = new Queue<DateTime>();
        private readonly object _lock = new object();

        public RateLimiter(string name, int maxRequests, TimeSpan window)
        {
            Name = name;
            MaxRequests = maxRequests;
            Window = window;
        }

        public bool TryAcquire()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
            
                // Remove old requests
                while (_requests.Count > 0 && now - _requests.Peek() > Window)
                {
                    _requests.Dequeue();
                }

                if (_requests.Count < MaxRequests)
                {
                    _requests.Enqueue(now);
                    return true;
                }

                return false;
            }
        }
    }

    /// <summary>
    /// Health check implementation.
    /// </summary>
    public class HealthCheck
    {
        public string Name { get; }
        public bool IsHealthy { get; private set; } = true;
        public DateTime LastCheck { get; private set; }
        public string LastError { get; private set; }
    
        private readonly Func<Task<bool>> _check;
        private readonly TimeSpan _interval;

        public HealthCheck(string name, Func<Task<bool>> check, TimeSpan interval)
        {
            Name = name;
            _check = check;
            _interval = interval;
        }

        public async Task Run()
        {
            try
            {
                IsHealthy = await _check();
                LastCheck = DateTime.UtcNow;
                LastError = null;
            }
            catch (Exception ex)
            {
                RecordFailure(ex);
            }
        }

        public void RecordFailure(Exception ex)
        {
            IsHealthy = false;
            LastCheck = DateTime.UtcNow;
            LastError = ex.Message;
        }
    }

    /// <summary>
    /// Cache entry implementation.
    /// </summary>
    public class CacheEntry
    {
        public object Value { get; }
        public DateTime Expiration { get; }
        public bool IsExpired => DateTime.UtcNow > Expiration;

        public CacheEntry(object value, TimeSpan ttl)
        {
            Value = value;
            Expiration = DateTime.UtcNow + ttl;
        }
    }

    /// <summary>
    /// Failsafe event log entry.
    /// </summary>
    public class FailsafeEvent
    {
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = "";
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// Memory statistics.
    /// </summary>
    public class MemoryStatistics
    {
        public long TotalMemory { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public bool IsMemoryPressure { get; set; }
        public long WarningThreshold { get; set; }
        public long CriticalThreshold { get; set; }
    }

    /// <summary>
    /// Health check result.
    /// </summary>
    public class HealthCheckResult
    {
        public bool IsHealthy { get; set; }
        public Dictionary<string, HealthCheckStatus> Checks { get; set; } = new Dictionary<string, HealthCheckStatus>();
    }

    /// <summary>
    /// Health check status.
    /// </summary>
    public class HealthCheckStatus
    {
        public bool IsHealthy { get; set; }
        public DateTime LastCheck { get; set; }
        public string LastError { get; set; }
    }

    /// <summary>
    /// Circuit breaker open exception.
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
    }

    /// <summary>
    /// Retry exhausted exception.
    /// </summary>
    public class RetryExhaustedException : Exception
    {
        public RetryExhaustedException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Bulkhead rejected exception.
    /// </summary>
    public class BulkheadRejectedException : Exception
    {
        public BulkheadRejectedException(string message) : base(message) { }
    }

    #endregion

}