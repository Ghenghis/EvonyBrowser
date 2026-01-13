using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SvonyBrowser.Services
{
    /// <summary>
    /// Manages Chrome DevTools Protocol (CDP) connections to CefFlashBrowser
    /// for Flash-based Evony game automation
    /// </summary>
    public class CdpConnectionService : IDisposable
    {
        #region Singleton

        private static readonly Lazy<CdpConnectionService> _lazyInstance =
            new Lazy<CdpConnectionService>(() => new CdpConnectionService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static CdpConnectionService Instance => _lazyInstance.Value;

        #endregion

        // Configuration
        private readonly string _cdpHost;
        private readonly int _cdpPort;
        private readonly string _cefBrowserPath;
        
        // Connection state
        private ClientWebSocket _webSocket;
        private Process _browserProcess;
        private bool _isConnected;
        private int _messageId;
        private readonly Dictionary<int, TaskCompletionSource<JToken>> _pendingRequests;
        private CancellationTokenSource _receiveCts;
        
        // Events
        public event EventHandler<CdpConnectionEventArgs> ConnectionStateChanged;
        public event EventHandler<CdpMessageEventArgs> MessageReceived;
        public event EventHandler<CdpEventArgs> EventReceived;
        
        // Properties
        public bool IsConnected => _isConnected;
        public string CdpEndpoint => $"http://{_cdpHost}:{_cdpPort}";
        public string WebSocketEndpoint { get; private set; }
        
        public CdpConnectionService(
            string cdpHost = "localhost",
            int cdpPort = 9222,
            string cefBrowserPath = null)
        {
            _cdpHost = cdpHost;
            _cdpPort = cdpPort;
            _cefBrowserPath = cefBrowserPath ?? FindCefBrowser();
            _pendingRequests = new Dictionary<int, TaskCompletionSource<JToken>>();
        }
        
        #region Browser Launch
        
        /// <summary>
        /// Find CefFlashBrowser executable
        /// </summary>
        private string FindCefBrowser()
        {
            var possiblePaths = new[]
            {
                @"C:\Program Files\CefFlashBrowser\CefFlashBrowser.exe",
                @"C:\Program Files (x86)\CefFlashBrowser\CefFlashBrowser.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "CefFlashBrowser", "CefFlashBrowser.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CefFlashBrowser", "CefFlashBrowser.exe")
            };
            
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }
            
            return null;
        }
        
        /// <summary>
        /// Launch CefFlashBrowser with CDP enabled
        /// </summary>
        public async Task<bool> LaunchBrowserAsync(string startUrl = "https://cc2.evony.com", 
            int width = 1920, int height = 1080)
        {
            if (string.IsNullOrEmpty(_cefBrowserPath) || !File.Exists(_cefBrowserPath))
            {
                throw new FileNotFoundException("CefFlashBrowser executable not found", _cefBrowserPath);
            }
            
            // Kill existing instance if running
            await KillExistingBrowserAsync();
            
            // Build arguments
            var args = new StringBuilder();
            args.Append($"--remote-debugging-port={_cdpPort} ");
            args.Append("--remote-allow-origins=* ");
            args.Append("--disable-gpu-sandbox ");
            args.Append("--enable-logging ");
            args.Append($"--window-size={width},{height} ");
            
            if (!string.IsNullOrEmpty(startUrl))
            {
                args.Append($"\"{startUrl}\"");
            }
            
            // Start process
            var startInfo = new ProcessStartInfo
            {
                FileName = _cefBrowserPath,
                Arguments = args.ToString(),
                UseShellExecute = false,
                CreateNoWindow = false
            };
            
            _browserProcess = Process.Start(startInfo);
            
            if (_browserProcess == null)
            {
                throw new Exception("Failed to start CefFlashBrowser");
            }
            
            // Wait for CDP to be ready
            await WaitForCdpReadyAsync();
            
            return true;
        }
        
        /// <summary>
        /// Kill existing CefFlashBrowser instances
        /// </summary>
        private async Task KillExistingBrowserAsync()
        {
            var processes = Process.GetProcessesByName("CefFlashBrowser");
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    await Task.Delay(500);
                }
                catch { }
            }
        }
        
        /// <summary>
        /// Wait for CDP endpoint to be ready
        /// </summary>
        private async Task WaitForCdpReadyAsync(int maxAttempts = 30)
        {
            var httpClient = new HttpClient(); // TODO: Add using block for proper disposal
            
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    var response = await httpClient.GetStringAsync($"{CdpEndpoint}/json/version");
                    if (!string.IsNullOrEmpty(response))
                    {
                        return;
                    }
                }
                catch
                {
                    await Task.Delay(500);
                }
            }
            
            throw new TimeoutException("CDP endpoint did not become ready");
        }
        
        #endregion
        
        #region CDP Connection
        
        /// <summary>
        /// Connect to CefFlashBrowser via CDP
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                // Get WebSocket endpoint
                var httpClient = new HttpClient(); // TODO: Add using block for proper disposal
                var targetsJson = await httpClient.GetStringAsync($"{CdpEndpoint}/json");
                var targets = JArray.Parse(targetsJson);
                
                // Find page target
                string wsUrl = null;
                foreach (var target in targets)
                {
                    var typeVal = target["type"]?.ToString();
                    if (typeVal == "page")
                    {
                        wsUrl = target["webSocketDebuggerUrl"]?.ToString();
                        if (!string.IsNullOrEmpty(wsUrl))
                            break;
                    }
                }
                
                if (string.IsNullOrEmpty(wsUrl))
                {
                    throw new Exception("No page target found");
                }
                
                WebSocketEndpoint = wsUrl;
                
                // Connect WebSocket
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
                
                _isConnected = true;
                _receiveCts = new CancellationTokenSource();
                
                // Start receiving messages
                _ = ReceiveMessagesAsync(_receiveCts.Token);
                
                ConnectionStateChanged?.Invoke(this, new CdpConnectionEventArgs
                {
                    IsConnected = true,
                    Endpoint = wsUrl
                });
                
                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                ConnectionStateChanged?.Invoke(this, new CdpConnectionEventArgs
                {
                    IsConnected = false,
                    Error = ex.Message
                });
                throw;
            }
        }
        
        /// <summary>
        /// Disconnect from CDP
        /// </summary>
        public async Task DisconnectAsync()
        {
            _receiveCts?.Cancel();
            
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            
            _webSocket?.Dispose();
            _webSocket = null;
            _isConnected = false;
            
            ConnectionStateChanged?.Invoke(this, new CdpConnectionEventArgs
            {
                IsConnected = false
            });
        }
        
        /// <summary>
        /// Receive messages from CDP
        /// </summary>
        private async Task ReceiveMessagesAsync(CancellationToken ct)
        {
            var buffer = new byte[65536];
            var messageBuilder = new StringBuilder();
            
            while (!ct.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await DisconnectAsync();
                        break;
                    }
                    
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    
                    if (result.EndOfMessage)
                    {
                        var message = messageBuilder.ToString();
                        messageBuilder.Clear();
                        
                        ProcessMessage(message);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CDP receive error: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Process received CDP message
        /// </summary>
        private void ProcessMessage(string message)
        {
            try
            {
                var json = JObject.Parse(message);
                
                // Check if it's a response to a request
                var idToken = json["id"];
                if (idToken != null)
                {
                    var id = idToken.Value<int>();
                    
                    if (_pendingRequests.TryGetValue(id, out var tcs))
                    {
                        _pendingRequests.Remove(id);
                        
                        var errorToken = json["error"];
                        if (errorToken != null)
                        {
                            tcs.SetException(new CdpException(errorToken["message"]?.ToString() ?? "Unknown error"));
                        }
                        else
                        {
                            var resultToken = json["result"];
                            tcs.SetResult(resultToken);
                        }
                    }
                }
                // Check if it's an event
                else
                {
                    var methodToken = json["method"];
                    if (methodToken != null)
                    {
                        var method = methodToken.ToString();
                        var paramsToken = json["params"];
                        
                        EventReceived?.Invoke(this, new CdpEventArgs
                        {
                            Method = method,
                            Params = paramsToken
                        });
                    }
                }
                
                MessageReceived?.Invoke(this, new CdpMessageEventArgs
                {
                    RawMessage = message
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CDP message processing error: {ex.Message}");
            }
        }
        
        #endregion
        
        #region CDP Commands
        
        /// <summary>
        /// Send CDP command and wait for response
        /// </summary>
        public async Task<JToken> SendCommandAsync(string method, object parameters = null, 
            int timeoutMs = 30000)
        {
            if (!_isConnected || _webSocket?.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("Not connected to CDP");
            }
            
            var id = Interlocked.Increment(ref _messageId);
            var tcs = new TaskCompletionSource<JToken>();
            _pendingRequests[id] = tcs;
            
            var message = new Dictionary<string, object>
            {
                ["id"] = id,
                ["method"] = method
            };
            
            if (parameters != null)
            {
                message["params"] = parameters;
            }
            
            var json = JsonConvert.SerializeObject(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            
            var cts = new CancellationTokenSource(timeoutMs); // TODO: Add using block for proper disposal
            cts.Token.Register(() =>
            {
                _pendingRequests.Remove(id);
                tcs.TrySetException(new TimeoutException($"CDP command '{method}' timed out"));
            });
            
            return await tcs.Task;
        }
        
        /// <summary>
        /// Navigate to URL
        /// </summary>
        public async Task<string> NavigateAsync(string url)
        {
            var result = await SendCommandAsync("Page.navigate", new { url });
            return result.GetProperty("frameId").GetString();
        }
        
        /// <summary>
        /// Reload page
        /// </summary>
        public async Task ReloadAsync(bool ignoreCache = false)
        {
            await SendCommandAsync("Page.reload", new { ignoreCache });
        }
        
        /// <summary>
        /// Go back
        /// </summary>
        public async Task GoBackAsync()
        {
            var history = await SendCommandAsync("Page.getNavigationHistory");
            var currentIndex = history.GetProperty("currentIndex").GetInt32();
            
            if (currentIndex > 0)
            {
                var entries = history.GetProperty("entries");
                var entryId = entries[currentIndex - 1].GetProperty("id").GetInt32();
                await SendCommandAsync("Page.navigateToHistoryEntry", new { entryId });
            }
        }
        
        /// <summary>
        /// Go forward
        /// </summary>
        public async Task GoForwardAsync()
        {
            var history = await SendCommandAsync("Page.getNavigationHistory");
            var currentIndex = history.GetProperty("currentIndex").GetInt32();
            var entries = history.GetProperty("entries");
            
            if (currentIndex < entries.GetArrayLength() - 1)
            {
                var entryId = entries[currentIndex + 1].GetProperty("id").GetInt32();
                await SendCommandAsync("Page.navigateToHistoryEntry", new { entryId });
            }
        }
        
        /// <summary>
        /// Take screenshot
        /// </summary>
        public async Task<byte[]> TakeScreenshotAsync(string format = "png", int quality = 100,
            int? x = null, int? y = null, int? width = null, int? height = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["format"] = format
            };
            
            if (format == "jpeg")
            {
                parameters["quality"] = quality;
            }
            
            if (x.HasValue && y.HasValue && width.HasValue && height.HasValue)
            {
                parameters["clip"] = new
                {
                    x = x.Value,
                    y = y.Value,
                    width = width.Value,
                    height = height.Value,
                    scale = 1
                };
            }
            
            var result = await SendCommandAsync("Page.captureScreenshot", parameters);
            var base64 = result.GetProperty("data").GetString();
            return Convert.FromBase64String(base64);
        }
        
        /// <summary>
        /// Get page info
        /// </summary>
        public async Task<(string url, string title)> GetPageInfoAsync()
        {
            // Enable Runtime domain if not already enabled
            try
            {
                await SendCommandAsync("Runtime.enable");
            }
            catch { }
            
            var urlResult = await SendCommandAsync("Runtime.evaluate", new
            {
                expression = "window.location.href"
            });
            
            var titleResult = await SendCommandAsync("Runtime.evaluate", new
            {
                expression = "document.title"
            });
            
            var url = urlResult?["result"]?["value"]?.ToString() ?? "";
            var title = titleResult?["result"]?["value"]?.ToString() ?? "";
            
            return (url, title);
        }
        
        /// <summary>
        /// Execute JavaScript
        /// </summary>
        public async Task<JToken> ExecuteScriptAsync(string script)
        {
            var result = await SendCommandAsync("Runtime.evaluate", new
            {
                expression = script,
                returnByValue = true
            });
            
            return result?["result"];
        }
        
        /// <summary>
        /// Get viewport size
        /// </summary>
        public async Task<(int width, int height)> GetViewportSizeAsync()
        {
            var result = await SendCommandAsync("Runtime.evaluate", new
            {
                expression = "JSON.stringify({width: window.innerWidth, height: window.innerHeight})",
                returnByValue = true
            });
            
            var json = result?["value"]?.ToString();
            var size = JObject.Parse(json ?? "{}");
            
            return (size["width"]?.Value<int>() ?? 0, size["height"]?.Value<int>() ?? 0);
        }
        
        #endregion
        
        #region Input Simulation
        
        /// <summary>
        /// Move mouse to coordinates
        /// </summary>
        public async Task MouseMoveAsync(double x, double y)
        {
            await SendCommandAsync("Input.dispatchMouseEvent", new
            {
                type = "mouseMoved",
                x,
                y
            });
        }
        
        /// <summary>
        /// Click at coordinates
        /// </summary>
        public async Task ClickAsync(double x, double y, string button = "left", int clickCount = 1)
        {
            // Move to position
            await MouseMoveAsync(x, y);
            await Task.Delay(10);
            
            // Mouse down
            await SendCommandAsync("Input.dispatchMouseEvent", new
            {
                type = "mousePressed",
                x,
                y,
                button,
                clickCount
            });
            
            await Task.Delay(50);
            
            // Mouse up
            await SendCommandAsync("Input.dispatchMouseEvent", new
            {
                type = "mouseReleased",
                x,
                y,
                button,
                clickCount
            });
        }
        
        /// <summary>
        /// Double click at coordinates
        /// </summary>
        public async Task DoubleClickAsync(double x, double y)
        {
            await ClickAsync(x, y, "left", 2);
        }
        
        /// <summary>
        /// Right click at coordinates
        /// </summary>
        public async Task RightClickAsync(double x, double y)
        {
            await ClickAsync(x, y, "right", 1);
        }
        
        /// <summary>
        /// Drag from one point to another
        /// </summary>
        public async Task DragAsync(double fromX, double fromY, double toX, double toY, int steps = 10)
        {
            // Move to start
            await MouseMoveAsync(fromX, fromY);
            await Task.Delay(10);
            
            // Mouse down
            await SendCommandAsync("Input.dispatchMouseEvent", new
            {
                type = "mousePressed",
                x = fromX,
                y = fromY,
                button = "left",
                clickCount = 1
            });
            
            // Move in steps
            for (int i = 1; i <= steps; i++)
            {
                var progress = (double)i / steps;
                var x = fromX + (toX - fromX) * progress;
                var y = fromY + (toY - fromY) * progress;
                
                await MouseMoveAsync(x, y);
                await Task.Delay(20);
            }
            
            // Mouse up
            await SendCommandAsync("Input.dispatchMouseEvent", new
            {
                type = "mouseReleased",
                x = toX,
                y = toY,
                button = "left",
                clickCount = 1
            });
        }
        
        /// <summary>
        /// Scroll at coordinates
        /// </summary>
        public async Task ScrollAsync(double x, double y, double deltaX = 0, double deltaY = 0)
        {
            await SendCommandAsync("Input.dispatchMouseEvent", new
            {
                type = "mouseWheel",
                x,
                y,
                deltaX,
                deltaY
            });
        }
        
        /// <summary>
        /// Type text
        /// </summary>
        public async Task TypeTextAsync(string text, int delayMs = 50)
        {
            foreach (var c in text)
            {
                await SendCommandAsync("Input.dispatchKeyEvent", new
                {
                    type = "keyDown",
                    text = c.ToString()
                });
                
                await SendCommandAsync("Input.dispatchKeyEvent", new
                {
                    type = "keyUp",
                    text = c.ToString()
                });
                
                await Task.Delay(delayMs);
            }
        }
        
        /// <summary>
        /// Press key
        /// </summary>
        public async Task PressKeyAsync(string key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            var modifiers = 0;
            if (ctrl) modifiers |= 2;
            if (alt) modifiers |= 1;
            if (shift) modifiers |= 8;
            
            await SendCommandAsync("Input.dispatchKeyEvent", new
            {
                type = "keyDown",
                key,
                modifiers
            });
            
            await Task.Delay(50);
            
            await SendCommandAsync("Input.dispatchKeyEvent", new
            {
                type = "keyUp",
                key,
                modifiers
            });
        }
        
        #endregion
        
        #region Cleanup
        
        public void Dispose()
        {
            _receiveCts?.Cancel();
            _webSocket?.Dispose();
            
            if (_browserProcess != null && !_browserProcess.HasExited)
            {
                try
                {
                    _browserProcess.Kill();
                }
                catch { }
            }
        }
        
        #endregion
    }
    
    #region Event Args
    
    public class CdpConnectionEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string Endpoint { get; set; }
        public string Error { get; set; }
    }
    
    public class CdpMessageEventArgs : EventArgs
    {
        public string RawMessage { get; set; }
    }
    
    public class CdpEventArgs : EventArgs
    {
        public string Method { get; set; }
        public JToken? Params { get; set; }
    }
    
    public class CdpException : Exception
    {
        public CdpException(string message) : base(message) { }
    }
    
    #endregion
}
