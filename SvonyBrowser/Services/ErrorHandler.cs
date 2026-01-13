using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
namespace SvonyBrowser.Services
{
    /// <summary>
    /// Centralized error handling service for Svony Browser v6.0 Borg Edition.
    /// Provides consistent error logging, user notification, and crash recovery.
    /// </summary>
    public sealed class ErrorHandler : IDisposable
    {
        private static readonly Lazy<ErrorHandler> _instance = new Lazy<ErrorHandler>(() => new ErrorHandler());
        public static ErrorHandler Instance => _instance.Value;
        
        private readonly ConcurrentQueue<ErrorEntry> _errorLog;
        private readonly string _logDirectory;
        private readonly string _crashDumpDirectory;
        private readonly int _maxLogEntries = 1000;
        private bool _disposed;
        
        public event EventHandler<ErrorEventArgs> ErrorOccurred;
        public event EventHandler<CrashEventArgs> CrashDetected;
        
        private ErrorHandler()
        {
            _errorLog = new ConcurrentQueue<ErrorEntry>();
            
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SvonyBrowser"
            );
            
            _logDirectory = Path.Combine(appDataPath, "logs");
            _crashDumpDirectory = Path.Combine(appDataPath, "crashes");
            
            Directory.CreateDirectory(_logDirectory);
            Directory.CreateDirectory(_crashDumpDirectory);
            
            // Wire up global exception handlers
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }
        
        #region Error Handling
        
        /// <summary>
        /// Handles an exception with optional user notification.
        /// </summary>
        public void Handle(Exception ex, string context = "", bool showUser = false, ErrorSeverity severity = ErrorSeverity.Error)
        {
            var entry = new ErrorEntry
            {
                Timestamp = DateTime.Now,
                Severity = severity,
                Context = context,
                Message = ex.Message,
                StackTrace = ex.StackTrace ?? "",
                ExceptionType = ex.GetType().Name,
                InnerException = ex.InnerException?.Message
            };
            
            // Add to log
            _errorLog.Enqueue(entry);
            
            // Trim log if too large
            while (_errorLog.Count > _maxLogEntries)
            {
                _errorLog.TryDequeue(out _);
            }
            
            // Write to file
            _ = WriteToLogFileAsync(entry);
            
            // Debug output
            Debug.WriteLine($"[{severity}] {context}: {ex.Message}");
            
            // Raise event
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(entry));
            
            // Show user notification if requested
            if (showUser)
            {
                ShowUserError(entry, ex);
            }
        }
        
        /// <summary>
        /// Handles an error message without an exception.
        /// </summary>
        public void HandleMessage(string message, string context = "", bool showUser = false, ErrorSeverity severity = ErrorSeverity.Warning)
        {
            var entry = new ErrorEntry
            {
                Timestamp = DateTime.Now,
                Severity = severity,
                Context = context,
                Message = message,
                StackTrace = Environment.StackTrace,
                ExceptionType = "Message"
            };
            
            _errorLog.Enqueue(entry);
            
            while (_errorLog.Count > _maxLogEntries)
            {
                _errorLog.TryDequeue(out _);
            }
            
            _ = WriteToLogFileAsync(entry);
            
            Debug.WriteLine($"[{severity}] {context}: {message}");
            
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(entry));
            
            if (showUser)
            {
                ShowUserError(entry);
            }
        }
        
        /// <summary>
        /// Executes an action with error handling.
        /// </summary>
        public bool TryExecute(Action action, string context = "", bool showUser = false)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                Handle(ex, context, showUser);
                return false;
            }
        }
        
        /// <summary>
        /// Executes an async action with error handling.
        /// </summary>
        public async Task<bool> TryExecuteAsync(Func<Task> action, string context = "", bool showUser = false)
        {
            try
            {
                await action();
                return true;
            }
            catch (Exception ex)
            {
                Handle(ex, context, showUser);
                return false;
            }
        }
        
        /// <summary>
        /// Executes a function with error handling and returns a default on failure.
        /// </summary>
        public T? TryExecute<T>(Func<T> func, string context = "", T? defaultValue = default, bool showUser = false)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                Handle(ex, context, showUser);
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Executes an async function with error handling and returns a default on failure.
        /// </summary>
        public async Task<T?> TryExecuteAsync<T>(Func<Task<T>> func, string context = "", T? defaultValue = default, bool showUser = false)
        {
            try
            {
                return await func();
            }
            catch (Exception ex)
            {
                Handle(ex, context, showUser);
                return defaultValue;
            }
        }
        
        #endregion
        
        #region User Notification
        
        private void ShowUserError(ErrorEntry entry, Exception originalException = null)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                // Get user-friendly message if we have the original exception
                var displayMessage = entry.Message;
                if (originalException != null)
                {
                    displayMessage = UserFriendlyMessages.GetFriendlyMessage(originalException);
                }
                var icon = entry.Severity switch
                {
                    ErrorSeverity.Info => MessageBoxImage.Information,
                    ErrorSeverity.Warning => MessageBoxImage.Warning,
                    ErrorSeverity.Error => MessageBoxImage.Error,
                    ErrorSeverity.Critical => MessageBoxImage.Error,
                    _ => MessageBoxImage.Error
                };
                
                var title = entry.Severity switch
                {
                    ErrorSeverity.Info => "Information",
                    ErrorSeverity.Warning => "Warning",
                    ErrorSeverity.Error => "Error",
                    ErrorSeverity.Critical => "Critical Error",
                    _ => "Error"
                };
                
                var message = string.IsNullOrEmpty(entry.Context)
                    ? displayMessage
                    : $"{entry.Context}\n\n{displayMessage}";
                
                MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            });
        }
        
        /// <summary>
        /// Shows a user-friendly error dialog with options.
        /// </summary>
        public MessageBoxResult ShowErrorDialog(string message, string title = "Error", 
            MessageBoxButton buttons = MessageBoxButton.OK, bool includeDetails = false, Exception ex = null)
        {
            var result = MessageBoxResult.OK;
            
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                var fullMessage = message;
                
                if (includeDetails && ex != null)
                {
                    fullMessage += $"\n\nDetails: {ex.Message}";
                    
                    if (ex.InnerException != null)
                    {
                        fullMessage += $"\n\nInner: {ex.InnerException.Message}";
                    }
                }
                
                result = MessageBox.Show(fullMessage, title, buttons, MessageBoxImage.Error);
            });
            
            return result;
        }
        
        #endregion
        
        #region Logging
        
        private async Task WriteToLogFileAsync(ErrorEntry entry)
        {
            try
            {
                var logFile = Path.Combine(_logDirectory, $"svony-{DateTime.Now:yyyy-MM-dd}.log");
                var logLine = FormatLogEntry(entry);
                
                await File.AppendAllTextAsync(logFile, logLine + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
        
        private string FormatLogEntry(ErrorEntry entry)
        {
            var sb = new StringBuilder();
            sb.Append($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");
            sb.Append($"[{entry.Severity}] ");
            
            if (!string.IsNullOrEmpty(entry.Context))
            {
                sb.Append($"[{entry.Context}] ");
            }
            
            sb.Append($"[{entry.ExceptionType}] ");
            sb.Append(entry.Message);
            
            if (!string.IsNullOrEmpty(entry.InnerException))
            {
                sb.Append($" | Inner: {entry.InnerException}");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Gets recent error entries.
        /// </summary>
        public ErrorEntry[] GetRecentErrors(int count = 50)
        {
            return _errorLog.ToArray().TakeLast(count).ToArray();
        }
        
        /// <summary>
        /// Clears the in-memory error log.
        /// </summary>
        public void ClearLog()
        {
            while (_errorLog.TryDequeue(out _)) { }
        }
        
        /// <summary>
        /// Gets all log files.
        /// </summary>
        public string[] GetLogFiles()
        {
            return Directory.GetFiles(_logDirectory, "svony-*.log");
        }
        
        /// <summary>
        /// Cleans up old log files.
        /// </summary>
        public void CleanupOldLogs(int keepDays = 7)
        {
            var cutoff = DateTime.Now.AddDays(-keepDays);
            
            foreach (var file in GetLogFiles())
            {
                try
                {
                    if (File.GetCreationTime(file) < cutoff)
                    {
                        File.Delete(file);
                    }
                }
                catch { }
            }
        }
        
        #endregion
        
        #region Crash Handling
        
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            
            if (ex != null)
            {
                Handle(ex, "Unhandled Exception", false, ErrorSeverity.Critical);
                _ = CreateCrashDumpAsync(ex, "unhandled");
            }
            
            CrashDetected?.Invoke(this, new CrashEventArgs(ex, e.IsTerminating));
            
            if (e.IsTerminating)
            {
                // Last chance to save state
                try
                {
                    SettingsManager.Instance.Save();
                }
                catch { }
            }
        }
        
        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Handle(e.Exception, "Unobserved Task Exception", false, ErrorSeverity.Error);
            e.SetObserved(); // Prevent crash
        }
        
        /// <summary>
        /// Creates a crash dump file.
        /// </summary>
        public async Task<string> CreateCrashDumpAsync(Exception ex, string type = "crash")
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"{type}_{timestamp}.txt";
                var filepath = Path.Combine(_crashDumpDirectory, filename);
                
                var sb = new StringBuilder();
                sb.AppendLine("=== SVONY BROWSER CRASH DUMP ===");
                sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine($"Version: 6.0.0 Borg Edition");
                sb.AppendLine($"OS: {Environment.OSVersion}");
                sb.AppendLine($".NET: {Environment.Version}");
                sb.AppendLine($"64-bit: {Environment.Is64BitProcess}");
                sb.AppendLine();
                sb.AppendLine("=== EXCEPTION ===");
                sb.AppendLine($"Type: {ex.GetType().FullName}");
                sb.AppendLine($"Message: {ex.Message}");
                sb.AppendLine($"Source: {ex.Source}");
                sb.AppendLine();
                sb.AppendLine("=== STACK TRACE ===");
                sb.AppendLine(ex.StackTrace);
                
                if (ex.InnerException != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("=== INNER EXCEPTION ===");
                    sb.AppendLine($"Type: {ex.InnerException.GetType().FullName}");
                    sb.AppendLine($"Message: {ex.InnerException.Message}");
                    sb.AppendLine();
                    sb.AppendLine("=== INNER STACK TRACE ===");
                    sb.AppendLine(ex.InnerException.StackTrace);
                }
                
                sb.AppendLine();
                sb.AppendLine("=== MEMORY ===");
                var stats = MemoryManager.Instance.GetStats();
                sb.AppendLine($"Working Set: {stats.WorkingSetMb:F2} MB");
                sb.AppendLine($"Managed: {stats.ManagedMemoryMb:F2} MB");
                sb.AppendLine($"GC Gen0: {stats.Gen0Collections}");
                sb.AppendLine($"GC Gen1: {stats.Gen1Collections}");
                sb.AppendLine($"GC Gen2: {stats.Gen2Collections}");
                
                sb.AppendLine();
                sb.AppendLine("=== RECENT ERRORS ===");
                foreach (var error in GetRecentErrors(10))
                {
                    sb.AppendLine($"[{error.Timestamp:HH:mm:ss}] [{error.Severity}] {error.Context}: {error.Message}");
                }
                
                await FileEx.WriteAllTextAsync(filepath, sb.ToString());
                
                return filepath;
            }
            catch (Exception dumpEx)
            {
                Debug.WriteLine($"Failed to create crash dump: {dumpEx.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets all crash dump files.
        /// </summary>
        public string[] GetCrashDumps()
        {
            return Directory.GetFiles(_crashDumpDirectory, "*.txt");
        }
        
        #endregion
        
        #region IDisposable
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            
            while (_errorLog.TryDequeue(out _)) { }
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
    
    public class ErrorEntry
    {
        public DateTime Timestamp { get; set; }
        public ErrorSeverity Severity { get; set; }
        public string Context { get; set; } = "";
        public string Message { get; set; } = "";
        public string StackTrace { get; set; } = "";
        public string ExceptionType { get; set; } = "";
        public string InnerException { get; set; }
    }
    
    public class ErrorEventArgs : EventArgs
    {
        public ErrorEntry Entry { get; }
        
        public ErrorEventArgs(ErrorEntry entry)
        {
            Entry = entry;
        }
    }
    
    public class CrashEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public bool IsTerminating { get; }
        
        public CrashEventArgs(Exception exception, bool isTerminating)
        {
            Exception = exception;
            IsTerminating = isTerminating;
        }
    }
    
    #endregion
}
