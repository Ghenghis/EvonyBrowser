using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace SvonyBrowser.Services
{
    /// <summary>
    /// Memory management service for Svony Browser v6.0 Borg Edition.
    /// Prevents memory leaks, monitors usage, and performs automatic cleanup.
    /// </summary>
    public sealed class MemoryManager : IDisposable
    {
        private static readonly Lazy<MemoryManager> _instance = new Lazy<MemoryManager>(() => new MemoryManager());
        public static MemoryManager Instance => _instance.Value;
        
        private readonly System.Timers.Timer _monitorTimer;
        private readonly System.Timers.Timer _cleanupTimer;
        private readonly ConcurrentDictionary<string, WeakReference> _trackedObjects;
        private readonly ConcurrentQueue<Action> _cleanupActions;
        private readonly object _lock = new object();
        
        private long _maxMemoryBytes;
        private long _warningThresholdBytes;
        private bool _disposed;
        
        public event EventHandler<MemoryWarningEventArgs> MemoryWarning;
        public event EventHandler<MemoryStatsEventArgs> StatsUpdated;
        
        public MemoryStats CurrentStats { get; private set; }
        
        private MemoryManager()
        {
            _trackedObjects = new ConcurrentDictionary<string, WeakReference>();
            _cleanupActions = new ConcurrentQueue<Action>();
            CurrentStats = new MemoryStats();
            
            // Default limits (can be configured)
            _maxMemoryBytes = 2L * 1024 * 1024 * 1024; // 2 GB
            _warningThresholdBytes = (long)(_maxMemoryBytes * 0.8); // 80%
            
            // Monitor memory every 5 seconds
            _monitorTimer = new System.Timers.Timer(5000);
            _monitorTimer.Elapsed += OnMonitorTick;
            _monitorTimer.AutoReset = true;
            
            // Cleanup every 30 seconds
            _cleanupTimer = new System.Timers.Timer(30000);
            _cleanupTimer.Elapsed += OnCleanupTick;
            _cleanupTimer.AutoReset = true;
        }
        
        #region Configuration
        
        /// <summary>
        /// Sets the maximum memory limit in megabytes.
        /// </summary>
        public void SetMaxMemory(int megabytes)
        {
            _maxMemoryBytes = (long)megabytes * 1024 * 1024;
            _warningThresholdBytes = (long)(_maxMemoryBytes * 0.8);
        }
        
        /// <summary>
        /// Starts memory monitoring.
        /// </summary>
        public void Start()
        {
            _monitorTimer.Start();
            _cleanupTimer.Start();
            UpdateStats();
        }
        
        /// <summary>
        /// Stops memory monitoring.
        /// </summary>
        public void Stop()
        {
            _monitorTimer.Stop();
            _cleanupTimer.Stop();
        }
        
        #endregion
        
        #region Object Tracking
        
        /// <summary>
        /// Tracks an object for memory management.
        /// </summary>
        public void Track(string key, object obj)
        {
            _trackedObjects[key] = new WeakReference(obj);
        }
        
        /// <summary>
        /// Untracks an object.
        /// </summary>
        public void Untrack(string key)
        {
            _trackedObjects.TryRemove(key, out _);
        }
        
        /// <summary>
        /// Checks if a tracked object is still alive.
        /// </summary>
        public bool IsAlive(string key)
        {
            return _trackedObjects.TryGetValue(key, out var weakRef) && weakRef.IsAlive;
        }
        
        /// <summary>
        /// Gets the count of tracked objects that are still alive.
        /// </summary>
        public int GetAliveCount()
        {
            int count = 0;
            foreach (var kvp in _trackedObjects)
            {
                if (kvp.Value.IsAlive)
                    count++;
            }
            return count;
        }
        
        #endregion
        
        #region Cleanup Registration
        
        /// <summary>
        /// Registers a cleanup action to be executed during memory pressure.
        /// </summary>
        public void RegisterCleanupAction(Action action)
        {
            _cleanupActions.Enqueue(action);
        }
        
        /// <summary>
        /// Executes all registered cleanup actions.
        /// </summary>
        public void ExecuteCleanupActions()
        {
            while (_cleanupActions.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Cleanup action failed: {ex.Message}");
                }
            }
        }
        
        #endregion
        
        #region Memory Operations
        
        /// <summary>
        /// Forces garbage collection.
        /// </summary>
        public void ForceGC()
        {
            lock (_lock)
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, true, true);
            }
        }
        
        /// <summary>
        /// Performs aggressive memory cleanup.
        /// </summary>
        public async Task AggressiveCleanupAsync()
        {
            await Task.Run(() =>
            {
                // Execute registered cleanup actions
                ExecuteCleanupActions();
                
                // Clean up dead weak references
                var deadKeys = new System.Collections.Generic.List<string>();
                foreach (var kvp in _trackedObjects)
                {
                    if (!kvp.Value.IsAlive)
                        deadKeys.Add(kvp.Key);
                }
                
                foreach (var key in deadKeys)
                {
                    _trackedObjects.TryRemove(key, out _);
                }
                
                // Force GC
                ForceGC();
                
                // Update stats
                UpdateStats();
            });
        }
        
        /// <summary>
        /// Trims the working set to reduce memory footprint.
        /// </summary>
        public void TrimWorkingSet()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                process.MinWorkingSet = (IntPtr)((long)process.MinWorkingSet);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to trim working set: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Statistics
        
        private void UpdateStats()
        {
            var process = Process.GetCurrentProcess();
            
            CurrentStats = new MemoryStats
            {
                WorkingSetMb = process.WorkingSet64 / (1024.0 * 1024.0),
                PrivateMemoryMb = process.PrivateMemorySize64 / (1024.0 * 1024.0),
                ManagedMemoryMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                TrackedObjectsAlive = GetAliveCount(),
                MaxMemoryMb = _maxMemoryBytes / (1024.0 * 1024.0),
                UsagePercent = (process.WorkingSet64 * 100.0) / _maxMemoryBytes,
                Timestamp = DateTime.Now
            };
            
            StatsUpdated?.Invoke(this, new MemoryStatsEventArgs(CurrentStats));
        }
        
        /// <summary>
        /// Gets current memory statistics.
        /// </summary>
        public MemoryStats GetStats()
        {
            UpdateStats();
            return CurrentStats;
        }
        
        #endregion
        
        #region Timer Handlers
        
        private void OnMonitorTick(object sender, ElapsedEventArgs e)
        {
            UpdateStats();
            
            var process = Process.GetCurrentProcess();
            var currentMemory = process.WorkingSet64;
            
            // Check for memory warning
            if (currentMemory > _warningThresholdBytes)
            {
                MemoryWarning?.Invoke(this, new MemoryWarningEventArgs(
                    MemoryWarningLevel.Warning,
                    $"Memory usage at {CurrentStats.UsagePercent:F1}% ({CurrentStats.WorkingSetMb:F0} MB)"
                ));
            }
            
            // Check for critical memory
            if (currentMemory > _maxMemoryBytes)
            {
                MemoryWarning?.Invoke(this, new MemoryWarningEventArgs(
                    MemoryWarningLevel.Critical,
                    $"Memory limit exceeded! ({CurrentStats.WorkingSetMb:F0} MB / {CurrentStats.MaxMemoryMb:F0} MB)"
                ));
                
                // Trigger aggressive cleanup
                _ = AggressiveCleanupAsync();
            }
        }
        
        private void OnCleanupTick(object sender, ElapsedEventArgs e)
        {
            // Periodic cleanup of dead weak references
            var deadKeys = new System.Collections.Generic.List<string>();
            foreach (var kvp in _trackedObjects)
            {
                if (!kvp.Value.IsAlive)
                    deadKeys.Add(kvp.Key);
            }
            
            foreach (var key in deadKeys)
            {
                _trackedObjects.TryRemove(key, out _);
            }
            
            // Light GC if memory is getting high
            var process = Process.GetCurrentProcess();
            if (process.WorkingSet64 > _warningThresholdBytes * 0.7)
            {
                GC.Collect(0, GCCollectionMode.Optimized);
            }
        }
        
        #endregion
        
        #region IDisposable
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            _monitorTimer?.Stop();
            _monitorTimer?.Dispose();
            _cleanupTimer?.Stop();
            _cleanupTimer?.Dispose();
            
            _trackedObjects.Clear();
            
            while (_cleanupActions.TryDequeue(out _)) { }
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    public class MemoryStats
    {
        public double WorkingSetMb { get; set; }
        public double PrivateMemoryMb { get; set; }
        public double ManagedMemoryMb { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public int TrackedObjectsAlive { get; set; }
        public double MaxMemoryMb { get; set; }
        public double UsagePercent { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public enum MemoryWarningLevel
    {
        Info,
        Warning,
        Critical
    }
    
    public class MemoryWarningEventArgs : EventArgs
    {
        public MemoryWarningLevel Level { get; }
        public string Message { get; }
        public DateTime Timestamp { get; }
        
        public MemoryWarningEventArgs(MemoryWarningLevel level, string message)
        {
            Level = level;
            Message = message;
            Timestamp = DateTime.Now;
        }
    }
    
    public class MemoryStatsEventArgs : EventArgs
    {
        public MemoryStats Stats { get; }
        
        public MemoryStatsEventArgs(MemoryStats stats)
        {
            Stats = stats;
        }
    }
    
    #endregion
}
