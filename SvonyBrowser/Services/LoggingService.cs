using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Events;

namespace SvonyBrowser.Services
{
    /// <summary>
    /// Centralized logging service with structured logging, performance tracking,
    /// and audit trail support for Svony Browser.
    /// </summary>
    public sealed class LoggingService : IDisposable
    {
        #region Singleton

        private static readonly Lazy<LoggingService> _lazyInstance =
            new Lazy<LoggingService>(() => new LoggingService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static LoggingService Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly ConcurrentDictionary<string, PerformanceTracker> _performanceTrackers =
            new ConcurrentDictionary<string, PerformanceTracker>();
        
        private readonly ConcurrentQueue<AuditEntry> _auditTrail = new ConcurrentQueue<AuditEntry>();
        private readonly object _auditLock = new object();
        private const int MaxAuditEntries = 10000;

        private ILogger _logger;
        private string _logPath;
        private LogEventLevel _minLevel = LogEventLevel.Information;
        private bool _initialized;

        #endregion

        #region Properties

        /// <summary>Gets the current log level.</summary>
        public LogEventLevel MinimumLevel => _minLevel;

        /// <summary>Gets the log file path.</summary>
        public string LogPath => _logPath;

        /// <summary>Gets whether logging is initialized.</summary>
        public bool IsInitialized => _initialized;

        #endregion

        #region Constructor

        private LoggingService()
        {
            // Will be initialized by Initialize() method
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the logging service with the specified configuration.
        /// </summary>
        public void Initialize(string logPath, string logLevel = "Information")
        {
            if (_initialized) return;

            _logPath = logPath;
            _minLevel = ParseLogLevel(logLevel);

            // Ensure log directory exists
            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            // Configure Serilog
            var logConfig = new LoggerConfiguration()
                .MinimumLevel.Is(_minLevel)
                .Enrich.WithThreadId()
                .Enrich.WithMachineName()
                .Enrich.FromLogContext()
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{ThreadId}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                    fileSizeLimitBytes: 100 * 1024 * 1024, // 100MB
                    rollOnFileSizeLimit: true
                );

            // Add console output in debug mode
            if (_minLevel <= LogEventLevel.Debug)
            {
                logConfig.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                );
            }

            _logger = logConfig.CreateLogger();
            Log.Logger = _logger;

            _initialized = true;
            Info("LoggingService", "Logging initialized", new { LogPath = logPath, Level = logLevel });
        }

        #endregion

        #region Logging Methods

        /// <summary>Logs a verbose message.</summary>
        public void Verbose(string source, string message, object properties = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            LogInternal(LogEventLevel.Verbose, source, message, null, properties, memberName, filePath, lineNumber);
        }

        /// <summary>Logs a debug message.</summary>
        public void Debug(string source, string message, object properties = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            LogInternal(LogEventLevel.Debug, source, message, null, properties, memberName, filePath, lineNumber);
        }

        /// <summary>Logs an information message.</summary>
        public void Info(string source, string message, object properties = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            LogInternal(LogEventLevel.Information, source, message, null, properties, memberName, filePath, lineNumber);
        }

        /// <summary>Logs a warning message.</summary>
        public void Warn(string source, string message, object properties = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            LogInternal(LogEventLevel.Warning, source, message, null, properties, memberName, filePath, lineNumber);
        }

        /// <summary>Logs an error message.</summary>
        public void Error(string source, string message, Exception exception = null, object properties = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            LogInternal(LogEventLevel.Error, source, message, exception, properties, memberName, filePath, lineNumber);
        }

        /// <summary>Logs a fatal error message.</summary>
        public void Fatal(string source, string message, Exception exception = null, object properties = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            LogInternal(LogEventLevel.Fatal, source, message, exception, properties, memberName, filePath, lineNumber);
        }

        private void LogInternal(LogEventLevel level, string source, string message, 
            Exception exception, object properties, string memberName, string filePath, int lineNumber)
        {
            if (!_initialized || _logger == null) return;
            if (level < _minLevel) return;

            var fileName = Path.GetFileName(filePath);
            var contextLogger = _logger.ForContext("SourceContext", source)
                                       .ForContext("Method", memberName)
                                       .ForContext("File", fileName)
                                       .ForContext("Line", lineNumber);

            if (properties != null)
            {
                contextLogger = contextLogger.ForContext("Properties", properties, destructureObjects: true);
            }

            if (exception != null)
            {
                contextLogger.Write(level, exception, message);
            }
            else
            {
                contextLogger.Write(level, message);
            }
        }

        #endregion

        #region Performance Tracking

        /// <summary>
        /// Starts a performance tracking operation.
        /// </summary>
        public PerformanceTracker StartPerformanceTrack(string operationName)
        {
            var tracker = new PerformanceTracker(operationName, this);
            _performanceTrackers[operationName] = tracker;
            return tracker;
        }

        /// <summary>
        /// Logs a performance metric.
        /// </summary>
        public void LogPerformance(string operationName, double durationMs, bool success = true, object properties = null)
        {
            Info("Performance", $"{operationName} completed in {durationMs:F2}ms", new
            {
                Operation = operationName,
                DurationMs = durationMs,
                Success = success,
                Properties = properties
            });
        }

        #endregion

        #region Audit Trail

        /// <summary>
        /// Records an audit entry.
        /// </summary>
        public void Audit(string action, string user, string details, object data = null)
        {
            var entry = new AuditEntry
            {
                Timestamp = DateTime.UtcNow,
                Action = action,
                User = user,
                Details = details,
                Data = data
            };

            _auditTrail.Enqueue(entry);

            // Trim old entries
            while (_auditTrail.Count > MaxAuditEntries)
            {
                _auditTrail.TryDequeue(out _);
            }

            Info("Audit", $"{action}: {details}", new { User = user, Data = data });
        }

        /// <summary>
        /// Gets recent audit entries.
        /// </summary>
        public IReadOnlyList<AuditEntry> GetAuditTrail(int count = 100)
        {
            var entries = _auditTrail.ToArray();
            var startIndex = Math.Max(0, entries.Length - count);
            var result = new AuditEntry[Math.Min(count, entries.Length)];
            Array.Copy(entries, startIndex, result, 0, result.Length);
            return result;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Sets the minimum log level.
        /// </summary>
        public void SetLogLevel(string level)
        {
            _minLevel = ParseLogLevel(level);
            Info("LoggingService", $"Log level changed to {level}");
        }

        private LogEventLevel ParseLogLevel(string level)
        {
            return level?.ToLowerInvariant() switch
            {
                "verbose" => LogEventLevel.Verbose,
                "debug" => LogEventLevel.Debug,
                "information" or "info" => LogEventLevel.Information,
                "warning" or "warn" => LogEventLevel.Warning,
                "error" => LogEventLevel.Error,
                "fatal" => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };
        }

        #endregion

        #region IDisposable

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Info("LoggingService", "Shutting down logging service");
            
            (_logger as IDisposable)?.Dispose();
            Log.CloseAndFlush();
            
            _performanceTrackers.Clear();
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Tracks performance of an operation.
    /// </summary>
    public class PerformanceTracker : IDisposable
    {
        private readonly string _operationName;
        private readonly LoggingService _loggingService;
        private readonly System.Diagnostics.Stopwatch _stopwatch;
        private bool _disposed;

        public PerformanceTracker(string operationName, LoggingService loggingService)
        {
            _operationName = operationName;
            _loggingService = loggingService;
            _stopwatch = System.Diagnostics.Stopwatch.StartNew();
        }

        public double ElapsedMs => _stopwatch.ElapsedMilliseconds;

        public void Complete(bool success = true, object properties = null)
        {
            if (_disposed) return;
            _stopwatch.Stop();
            _loggingService.LogPerformance(_operationName, _stopwatch.Elapsed.TotalMilliseconds, success, properties);
            _disposed = true;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Complete();
            }
        }
    }

    /// <summary>
    /// Represents an audit trail entry.
    /// </summary>
    public class AuditEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
        public string User { get; set; }
        public string Details { get; set; }
        public object Data { get; set; }
    }

    #endregion
}
