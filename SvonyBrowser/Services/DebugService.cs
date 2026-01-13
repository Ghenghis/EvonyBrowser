using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
    using SvonyBrowser.Helpers;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Comprehensive debugging and profiling service for Svony Browser.
    /// Provides performance monitoring, diagnostics, and troubleshooting capabilities.
    /// </summary>
    public sealed class DebugService : IDisposable
    {
        private static readonly Lazy<DebugService> _instance = new Lazy<DebugService>(() => new DebugService());
        public static DebugService Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, Stopwatch> _timers = new Dictionary<string, Stopwatch>();
        private readonly ConcurrentQueue<DiagnosticEvent> _eventLog = new ConcurrentQueue<DiagnosticEvent>();
        private readonly ConcurrentDictionary<string, long> _metrics = new Dictionary<string, long>();
        private readonly int _maxEventLogSize = 10000;
        private bool _isEnabled = true;
        private bool _disposed;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public int EventCount => _eventLog.Count;

        private DebugService()
        {
            InitializeDefaultCounters();
        }

        private void InitializeDefaultCounters()
        {
            _metrics["requests_total"] = 0;
            _metrics["errors_total"] = 0;
            _metrics["warnings_total"] = 0;
            _metrics["gc_collections_gen0"] = 0;
            _metrics["gc_collections_gen1"] = 0;
            _metrics["gc_collections_gen2"] = 0;
        }

        public void StartTimer(string name)
        {
            if (!_isEnabled) return;
            var timer = new Stopwatch();
            timer.Start();
            _timers[name] = timer;
            LogEvent(DiagnosticLevel.Debug, $"Timer started: {name}");
        }

        public long StopTimer(string name)
        {
            if (!_isEnabled) return 0;
            if (_timers.TryRemove(name, out var timer))
            {
                timer.Stop();
                var elapsed = timer.ElapsedMilliseconds;
                LogEvent(DiagnosticLevel.Debug, $"Timer stopped: {name} - {elapsed}ms");
                return elapsed;
            }
            return 0;
        }

        public long GetTimerElapsed(string name)
        {
            return _timers.TryGetValue(name, out var timer) ? timer.ElapsedMilliseconds : 0;
        }

        public void IncrementMetric(string name, long value = 1)
        {
            if (!_isEnabled) return;
            _metrics.AddOrUpdate(name, value, (_, existing) => existing + value);
        }

        public long GetMetric(string name)
        {
            return _metrics.TryGetValue(name, out var value) ? value : 0;
        }

        public Dictionary<string, long> GetAllMetrics()
        {
            return new Dictionary<string, long>(_metrics);
        }

        public void LogEvent(DiagnosticLevel level, string message,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!_isEnabled) return;
        
            var evt = new DiagnosticEvent
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message,
                Caller = caller,
                File = Path.GetFileName(file),
                Line = line
            };
        
            _eventLog.Enqueue(evt);
        
            while (_eventLog.Count > _maxEventLogSize)
                _eventLog.TryDequeue(out _);
        
            switch (level)
            {
                case DiagnosticLevel.Error:
                    IncrementMetric("errors_total");
                    break;
                case DiagnosticLevel.Warning:
                    IncrementMetric("warnings_total");
                    break;
            }
        }

        public IEnumerable<DiagnosticEvent> GetRecentEvents(int count = 100)
        {
            return _eventLog.TakeLast(count).ToList();
        }

        public IEnumerable<DiagnosticEvent> GetEventsByLevel(DiagnosticLevel level, int count = 100)
        {
            return _eventLog.Where(e => e.Level == level).TakeLast(count).ToList();
        }

        public void ClearEvents()
        {
            while (_eventLog.TryDequeue(out _)) { }
        }

        public MemoryDiagnostics GetMemoryDiagnostics()
        {
            var process = Process.GetCurrentProcess();
            return new MemoryDiagnostics
            {
                WorkingSet = process.WorkingSet64,
                PrivateMemory = process.PrivateMemorySize64,
                VirtualMemory = process.VirtualMemorySize64,
                GcTotalMemory = GC.GetTotalMemory(false),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            };
        }

        public ProcessDiagnostics GetProcessDiagnostics()
        {
            var process = Process.GetCurrentProcess();
            return new ProcessDiagnostics
            {
                ProcessId = process.Id,
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                TotalProcessorTime = process.TotalProcessorTime,
                UserProcessorTime = process.UserProcessorTime,
                StartTime = process.StartTime,
                Uptime = DateTime.Now - process.StartTime
            };
        }

        public string GenerateReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine("              SVONY BROWSER DIAGNOSTIC REPORT              ");
            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
        
            var mem = GetMemoryDiagnostics();
            sb.AppendLine("MEMORY:");
            sb.AppendLine($"  Working Set:    {mem.WorkingSet / 1024 / 1024:N0} MB");
            sb.AppendLine($"  Private Memory: {mem.PrivateMemory / 1024 / 1024:N0} MB");
            sb.AppendLine($"  GC Total:       {mem.GcTotalMemory / 1024 / 1024:N0} MB");
            sb.AppendLine();
        
            var proc = GetProcessDiagnostics();
            sb.AppendLine("PROCESS:");
            sb.AppendLine($"  PID:            {proc.ProcessId}");
            sb.AppendLine($"  Threads:        {proc.ThreadCount}");
            sb.AppendLine($"  Uptime:         {proc.Uptime}");
            sb.AppendLine();
        
            sb.AppendLine("METRICS:");
            foreach (var metric in _metrics.OrderBy(m => m.Key))
                sb.AppendLine($"  {metric.Key}: {metric.Value}");
        
            sb.AppendLine("═══════════════════════════════════════════════════════════");
            return sb.ToString();
        }

        public async Task SaveReportAsync(string path)
        {
            var report = GenerateReport();
            await FileEx.WriteAllTextAsync(path, report);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timers.Clear();
            ClearEvents();
        }
    }

    public enum DiagnosticLevel { Debug, Info, Warning, Error, Critical }

    public class DiagnosticEvent
    {
        public DateTime Timestamp { get; set; }
        public DiagnosticLevel Level { get; set; }
        public string Message { get; set; } = "";
        public string Caller { get; set; } = "";
        public string File { get; set; } = "";
        public int Line { get; set; }
    }

    public class MemoryDiagnostics
    {
        public long WorkingSet { get; set; }
        public long PrivateMemory { get; set; }
        public long VirtualMemory { get; set; }
        public long GcTotalMemory { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
    }

    public class ProcessDiagnostics
    {
        public int ProcessId { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public TimeSpan TotalProcessorTime { get; set; }
        public TimeSpan UserProcessorTime { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Uptime { get; set; }
    }

}