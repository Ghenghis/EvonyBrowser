using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;

namespace SvonyBrowser.Services
{
    /// <summary>
    /// Converts technical error messages to user-friendly messages.
    /// Maps common exceptions and error patterns to helpful, actionable messages.
    /// </summary>
    public static class UserFriendlyMessages
    {
        #region Message Mappings

        /// <summary>
        /// Maps exception types to user-friendly messages.
        /// </summary>
        private static readonly Dictionary<Type, Func<Exception, string>> ExceptionMessages = new()
        {
            // Network Errors
            [typeof(SocketException)] = ex => GetSocketErrorMessage((SocketException)ex),
            [typeof(WebException)] = ex => GetWebExceptionMessage((WebException)ex),
            [typeof(HttpRequestException)] = ex => "Unable to connect to the server. Please check your internet connection and try again.",
            
            // File Errors
            [typeof(FileNotFoundException)] = ex => $"The required file was not found: {Path.GetFileName(((FileNotFoundException)ex).FileName ?? "unknown")}. Please verify the installation is complete.",
            [typeof(DirectoryNotFoundException)] = ex => "A required folder is missing. Please verify the installation is complete or reinstall the application.",
            [typeof(UnauthorizedAccessException)] = ex => "Access denied. Please run the application as administrator or check file permissions.",
            [typeof(IOException)] = ex => GetIOExceptionMessage((IOException)ex),
            
            // Configuration Errors
            [typeof(InvalidOperationException)] = ex => "An operation could not be completed. Please check your settings and try again.",
            [typeof(ArgumentException)] = ex => "Invalid input provided. Please check your settings and try again.",
            [typeof(ArgumentNullException)] = ex => "A required value is missing. Please check your settings and try again.",
            [typeof(FormatException)] = ex => "Invalid format. Please check the input values and try again.",
            
            // Timeout Errors
            [typeof(TimeoutException)] = ex => "The operation timed out. The server may be busy or unreachable. Please try again later.",
            [typeof(TaskCanceledException)] = ex => "The operation was cancelled or timed out. Please try again.",
            [typeof(OperationCanceledException)] = ex => "The operation was cancelled.",
            
            // Memory Errors
            [typeof(OutOfMemoryException)] = ex => "The application is running low on memory. Please close some browser tabs or restart the application.",
            [typeof(StackOverflowException)] = ex => "A critical error occurred. Please restart the application.",
            
            // Security Errors
            [typeof(System.Security.SecurityException)] = ex => "A security error occurred. Please check your permissions and try again.",
            [typeof(System.Security.Cryptography.CryptographicException)] = ex => "An encryption error occurred. Your settings file may be corrupted. Try resetting to defaults.",
        };

        /// <summary>
        /// Maps error message patterns to user-friendly messages.
        /// </summary>
        private static readonly List<(Regex Pattern, string FriendlyMessage)> MessagePatterns = new()
        {
            // Connection errors
            (new Regex(@"connection.*refused", RegexOptions.IgnoreCase), 
                "Connection refused. The server may be offline or blocked by a firewall."),
            (new Regex(@"connection.*reset", RegexOptions.IgnoreCase), 
                "Connection was reset. Please check your network and try again."),
            (new Regex(@"connection.*timed?\s*out", RegexOptions.IgnoreCase), 
                "Connection timed out. The server is not responding."),
            (new Regex(@"host.*not.*found|dns.*failed|name.*resolution", RegexOptions.IgnoreCase), 
                "Server not found. Please check the server address and your internet connection."),
            (new Regex(@"ssl|tls|certificate", RegexOptions.IgnoreCase), 
                "Secure connection failed. There may be a problem with the server's security certificate."),
            
            // Flash/CefSharp errors
            (new Regex(@"flash.*plugin|pepflashplayer", RegexOptions.IgnoreCase), 
                "Flash plugin error. Please verify the Flash plugin is installed in the Assets/Plugins folder."),
            (new Regex(@"cefsharp|chromium|browser.*subprocess", RegexOptions.IgnoreCase), 
                "Browser component error. Please verify CefSharp files are in the Assets/CefSharp folder."),
            (new Regex(@"libcef|cef_.*\.dll", RegexOptions.IgnoreCase), 
                "Browser library missing. Please ensure all CefSharp runtime files are present."),
            
            // MCP errors
            (new Regex(@"mcp.*server|mcp.*connection", RegexOptions.IgnoreCase), 
                "MCP server connection failed. Please check that the MCP servers are running."),
            (new Regex(@"rag.*server|knowledge.*base", RegexOptions.IgnoreCase), 
                "RAG server unavailable. The knowledge base features will be limited."),
            (new Regex(@"rte.*server|real.*time.*engine", RegexOptions.IgnoreCase), 
                "Real-time engine unavailable. Some automation features may not work."),
            
            // LLM errors
            (new Regex(@"llm|language.*model|ollama|lmstudio", RegexOptions.IgnoreCase), 
                "AI assistant unavailable. Please check that your LLM backend is running."),
            (new Regex(@"inference|model.*load|gpu.*memory", RegexOptions.IgnoreCase), 
                "AI model error. The model may require more GPU memory or a restart."),
            
            // Fiddler errors
            (new Regex(@"fiddler|proxy.*port|8888", RegexOptions.IgnoreCase), 
                "Proxy connection failed. Please check Fiddler settings and ensure the port is available."),
            (new Regex(@"traffic.*capture|packet.*intercept", RegexOptions.IgnoreCase), 
                "Traffic capture error. Please check proxy settings and permissions."),
            
            // Settings errors
            (new Regex(@"settings.*file|config.*corrupt|json.*parse", RegexOptions.IgnoreCase), 
                "Settings file error. Your settings may be corrupted. Try resetting to defaults."),
            (new Regex(@"serialize|deserialize", RegexOptions.IgnoreCase), 
                "Data format error. Please try resetting the affected settings."),
            
            // Game-specific errors
            (new Regex(@"evony|game.*server|cc2\.evony", RegexOptions.IgnoreCase), 
                "Game server connection issue. Please check your server selection and try again."),
            (new Regex(@"swf.*load|flash.*content", RegexOptions.IgnoreCase), 
                "Game content failed to load. Please verify your SWF file paths in settings."),
            (new Regex(@"amf.*decode|protocol.*error", RegexOptions.IgnoreCase), 
                "Game protocol error. The traffic data may be corrupted or in an unexpected format."),
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a user-friendly message for an exception.
        /// </summary>
        public static string GetFriendlyMessage(Exception ex)
        {
            if (ex == null) return "An unknown error occurred.";

            // Check for specific exception type
            var exType = ex.GetType();
            if (ExceptionMessages.TryGetValue(exType, out var messageFunc))
            {
                return messageFunc(ex);
            }

            // Check for pattern matches in message
            var originalMessage = ex.Message ?? "";
            foreach (var (pattern, friendlyMessage) in MessagePatterns)
            {
                if (pattern.IsMatch(originalMessage))
                {
                    return friendlyMessage;
                }
            }

            // Check inner exception
            if (ex.InnerException != null)
            {
                var innerMessage = GetFriendlyMessage(ex.InnerException);
                if (innerMessage != ex.InnerException.Message)
                {
                    return innerMessage;
                }
            }

            // Return a generic but helpful message
            return GetGenericFriendlyMessage(ex);
        }

        /// <summary>
        /// Gets a user-friendly message with suggested actions.
        /// </summary>
        public static (string Message, string[] Actions) GetFriendlyMessageWithActions(Exception ex)
        {
            var message = GetFriendlyMessage(ex);
            var actions = GetSuggestedActions(ex);
            return (message, actions);
        }

        /// <summary>
        /// Gets suggested actions for an exception.
        /// </summary>
        public static string[] GetSuggestedActions(Exception ex)
        {
            var actions = new List<string>();

            switch (ex)
            {
                case SocketException or WebException or HttpRequestException:
                    actions.Add("Check your internet connection");
                    actions.Add("Verify the server address is correct");
                    actions.Add("Check if a firewall is blocking the connection");
                    break;

                case FileNotFoundException or DirectoryNotFoundException:
                    actions.Add("Verify the installation is complete");
                    actions.Add("Reinstall the application if files are missing");
                    actions.Add("Check the file paths in settings");
                    break;

                case UnauthorizedAccessException:
                    actions.Add("Run the application as administrator");
                    actions.Add("Check file and folder permissions");
                    actions.Add("Ensure antivirus is not blocking access");
                    break;

                case TimeoutException or TaskCanceledException:
                    actions.Add("Wait a moment and try again");
                    actions.Add("Check if the server is responding");
                    actions.Add("Increase timeout settings if available");
                    break;

                case OutOfMemoryException:
                    actions.Add("Close unused browser tabs");
                    actions.Add("Restart the application");
                    actions.Add("Close other memory-intensive applications");
                    break;

                default:
                    actions.Add("Try the operation again");
                    actions.Add("Restart the application if the problem persists");
                    actions.Add("Check the logs for more details");
                    break;
            }

            return actions.ToArray();
        }

        #endregion

        #region Private Methods

        private static string GetSocketErrorMessage(SocketException ex)
        {
            return ex.SocketErrorCode switch
            {
                SocketError.ConnectionRefused => "Connection refused. The server may be offline or the port may be blocked.",
                SocketError.ConnectionReset => "Connection was reset by the server. Please try again.",
                SocketError.TimedOut => "Connection timed out. The server is not responding.",
                SocketError.HostNotFound => "Server not found. Please check the address and try again.",
                SocketError.HostUnreachable => "Server is unreachable. Please check your network connection.",
                SocketError.NetworkUnreachable => "Network is unreachable. Please check your internet connection.",
                SocketError.AddressAlreadyInUse => "The port is already in use. Please close other applications using this port.",
                SocketError.AccessDenied => "Network access denied. Please check your firewall settings.",
                _ => $"Network error: {ex.SocketErrorCode}. Please check your connection and try again."
            };
        }

        private static string GetWebExceptionMessage(WebException ex)
        {
            return ex.Status switch
            {
                WebExceptionStatus.ConnectFailure => "Unable to connect to the server. Please check your internet connection.",
                WebExceptionStatus.Timeout => "The request timed out. The server may be busy or unreachable.",
                WebExceptionStatus.ProtocolError => GetHttpStatusMessage(ex),
                WebExceptionStatus.NameResolutionFailure => "Server not found. Please check the address and your DNS settings.",
                WebExceptionStatus.SecureChannelFailure => "Secure connection failed. There may be a certificate issue.",
                WebExceptionStatus.TrustFailure => "The server's security certificate is not trusted.",
                WebExceptionStatus.ServerProtocolViolation => "The server sent an invalid response.",
                _ => "A network error occurred. Please check your connection and try again."
            };
        }

        private static string GetHttpStatusMessage(WebException ex)
        {
            if (ex.Response is HttpWebResponse response)
            {
                return response.StatusCode switch
                {
                    HttpStatusCode.NotFound => "The requested resource was not found (404).",
                    HttpStatusCode.Unauthorized => "Authentication required. Please check your credentials.",
                    HttpStatusCode.Forbidden => "Access forbidden. You don't have permission to access this resource.",
                    HttpStatusCode.InternalServerError => "Server error. Please try again later.",
                    HttpStatusCode.BadGateway => "Server is temporarily unavailable. Please try again later.",
                    HttpStatusCode.ServiceUnavailable => "Service unavailable. The server may be under maintenance.",
                    HttpStatusCode.GatewayTimeout => "Server timeout. Please try again later.",
                    _ => $"Server returned error: {(int)response.StatusCode} {response.StatusDescription}"
                };
            }
            return "A server error occurred. Please try again.";
        }

        private static string GetIOExceptionMessage(IOException ex)
        {
            var message = ex.Message.ToLowerInvariant();
            
            if (message.Contains("disk full") || message.Contains("no space"))
                return "Disk is full. Please free up some space and try again.";
            
            if (message.Contains("sharing violation") || message.Contains("in use"))
                return "The file is in use by another program. Please close it and try again.";
            
            if (message.Contains("path too long"))
                return "The file path is too long. Please use a shorter path.";
            
            return "A file operation failed. Please check file permissions and try again.";
        }

        private static string GetGenericFriendlyMessage(Exception ex)
        {
            // Try to make the message more readable
            var message = ex.Message;
            
            // Remove common technical prefixes
            message = Regex.Replace(message, @"^(Exception|Error|Failed|Unable):\s*", "", RegexOptions.IgnoreCase);
            
            // Capitalize first letter
            if (!string.IsNullOrEmpty(message))
            {
                message = char.ToUpper(message[0]) + message.Substring(1);
            }
            
            // Add period if missing
            if (!message.EndsWith(".") && !message.EndsWith("!") && !message.EndsWith("?"))
            {
                message += ".";
            }
            
            return message;
        }

        #endregion
    }
}
