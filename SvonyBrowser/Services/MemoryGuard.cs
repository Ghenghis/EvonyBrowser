using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Memory guard service that monitors and manages memory usage.
    /// Implements automatic GC triggers and memory pressure detection.
    /// </summary>
    public sealed class MemoryGuard : IDisposable
    {
        private static readonly Lazy<MemoryGuard> _instance = new Lazy<MemoryGuard>(() => new MemoryGuard());
        public static MemoryGuard Instance => _instance.Value;

        private readonly Timer _monitorTimer;
        private readonly ConcurrentQueue<MemorySnapshot> _snapshots = new ConcurrentQueue<MemorySnapshot>();
        private readonly int _maxSnapshots = 100;
        private bool _disposed;
    
        private long _warningThresholdMb = 500;
        private long _criticalThresholdMb = 800;
        private long _maxMemoryMb = 1024;
    
        public event EventHandler<MemoryPressureEventArgs> MemoryPressureDetected;
        public event EventHandler<MemorySnapshot> SnapshotTaken;

        public bool IsMonitoring { get; private set; }
        public MemoryPressureLevel CurrentPressure { get; private set; } = MemoryPressureLevel.Normal;
        public long CurrentMemoryMb => Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;

        public long WarningThresholdMb
        {
            get => _warningThresholdMb;
            set => _warningThresholdMb = Math.Max(100, value);
        }

        public long CriticalThresholdMb
        {
            get => _criticalThresholdMb;
            set => _criticalThresholdMb = Math.Max(200, value);
        }

        public long MaxMemoryMb
        {
            get => _maxMemoryMb;
            set => _maxMemoryMb = Math.Max(256, value);
        }

        private MemoryGuard()
        {
            _monitorTimer = new Timer(MonitorCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void StartMonitoring(int intervalMs = 5000)
        {
            if (IsMonitoring) return;
            IsMonitoring = true;
            _monitorTimer.Change(0, intervalMs);
            App.Logger.Information("MemoryGuard monitoring started with {Interval}ms interval", intervalMs);
        }

        public void StopMonitoring()
        {
            if (!IsMonitoring) return;
            IsMonitoring = false;
            _monitorTimer.Change(Timeout.Infinite, Timeout.Infinite);
            App.Logger.Information("MemoryGuard monitoring stopped");
        }

        private void MonitorCallback(object state)
        {
            try
            {
                var snapshot = TakeSnapshot();
                EvaluatePressure(snapshot);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error in MemoryGuard monitor callback");
            }
        }

        public MemorySnapshot TakeSnapshot()
        {
            var process = Process.GetCurrentProcess();
            var snapshot = new MemorySnapshot
            {
                Timestamp = DateTime.UtcNow,
                WorkingSetMb = process.WorkingSet64 / 1024 / 1024,
                PrivateMemoryMb = process.PrivateMemorySize64 / 1024 / 1024,
                VirtualMemoryMb = process.VirtualMemorySize64 / 1024 / 1024,
                GcTotalMemoryMb = GC.GetTotalMemory(false) / 1024 / 1024,
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            };
        
            _snapshots.Enqueue(snapshot);
            while (_snapshots.Count > _maxSnapshots)
                _snapshots.TryDequeue(out _);
        
            SnapshotTaken?.Invoke(this, snapshot);
            return snapshot;
        }

        private void EvaluatePressure(MemorySnapshot snapshot)
        {
            var previousPressure = CurrentPressure;
        
            if (snapshot.WorkingSetMb >= _maxMemoryMb)
                CurrentPressure = MemoryPressureLevel.Critical;
            else if (snapshot.WorkingSetMb >= _criticalThresholdMb)
                CurrentPressure = MemoryPressureLevel.High;
            else if (snapshot.WorkingSetMb >= _warningThresholdMb)
                CurrentPressure = MemoryPressureLevel.Medium;
            else
                CurrentPressure = MemoryPressureLevel.Normal;
        
            if (CurrentPressure != previousPressure && CurrentPressure >= MemoryPressureLevel.Medium)
            {
                var args = new MemoryPressureEventArgs
                {
                    Level = CurrentPressure,
                    Snapshot = snapshot,
                    PreviousLevel = previousPressure
                };
            
                MemoryPressureDetected?.Invoke(this, args);
                App.Logger.Warning("Memory pressure changed: {Previous} -> {Current} ({Memory}MB)",
                    previousPressure, CurrentPressure, snapshot.WorkingSetMb);
            
                if (CurrentPressure >= MemoryPressureLevel.High)
                    TriggerGarbageCollection();
            }
        }

        public void TriggerGarbageCollection(bool aggressive = false)
        {
            var before = GC.GetTotalMemory(false);
        
            if (aggressive)
                GC.Collect(2, GCCollectionMode.Forced, true, true);
            else
                GC.Collect(2, GCCollectionMode.Optimized, false);
        
            GC.WaitForPendingFinalizers();
        
            var after = GC.GetTotalMemory(true);
            var freed = (before - after) / 1024 / 1024;
        
            App.Logger.Information("GC triggered: freed {Freed}MB (aggressive={Aggressive})", freed, aggressive);
        }

        public MemorySnapshot[] GetSnapshots() => _snapshots.ToArray();
    
        public void ClearSnapshots()
        {
            while (_snapshots.TryDequeue(out _)) { }
        }

        public bool IsMemorySafe() => CurrentPressure <= MemoryPressureLevel.Medium;

        public async Task WaitForMemoryReductionAsync(int timeoutMs = 30000, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            while (CurrentPressure >= MemoryPressureLevel.High && sw.ElapsedMilliseconds < timeoutMs)
            {
                TriggerGarbageCollection();
                await Task.Delay(1000, ct);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopMonitoring();
            _monitorTimer.Dispose();
            ClearSnapshots();
        }
    }

    public enum MemoryPressureLevel { Normal, Medium, High, Critical }

    public class MemorySnapshot
    {
        public DateTime Timestamp { get; set; }
        public long WorkingSetMb { get; set; }
        public long PrivateMemoryMb { get; set; }
        public long VirtualMemoryMb { get; set; }
        public long GcTotalMemoryMb { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
    }

    public class MemoryPressureEventArgs : EventArgs
    {
        public MemoryPressureLevel Level { get; set; }
        public MemoryPressureLevel PreviousLevel { get; set; }
        public MemorySnapshot Snapshot { get; set; } = new MemorySnapshot();
    }

}