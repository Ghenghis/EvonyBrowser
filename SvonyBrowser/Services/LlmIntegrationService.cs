using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;
using SvonyBrowser.Helpers;

namespace SvonyBrowser.Services
{
    /// <summary>
    /// LLM Integration Service for local model support (LM Studio, Ollama)
    /// Optimized for RTX 3090 Ti with 7B parameter models
    /// </summary>
    public class LlmIntegrationService : INotifyPropertyChanged, IDisposable
    {
        private static readonly Lazy<LlmIntegrationService> _lazyInstance =
            new Lazy<LlmIntegrationService>(() => new LlmIntegrationService(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static LlmIntegrationService Instance => _lazyInstance.Value;

        private readonly HttpClient _httpClient;
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource _connectionCts;
        private bool _isConnected;
        private bool _isInferring;
        private bool _isDisposed;
        private string _currentModel = "evony-re-7b";
        private double _tokensPerSecond;
        private int _contextLength = 8192;
        private double _temperature = 0.7;
        private double _gpuTemperature;
        private double _vramUsageGb;
        private double _vramTotalGb = 24.0; // RTX 3090 Ti
        private readonly Queue<double> _tokenRateHistory = new Queue<double>();

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<LlmResponseEventArgs> ResponseReceived;
        public event EventHandler<LlmStreamEventArgs> StreamTokenReceived;
        public event EventHandler<LlmErrorEventArgs> ErrorOccurred;

        // Connection settings
        public string LmStudioUrl { get; set; } = "http://localhost:1234/v1";
        public string OllamaUrl { get; set; } = "http://localhost:11434";
        public LlmBackend Backend { get; set; } = LlmBackend.LmStudio;

        // Status properties
        public bool IsConnected => _isConnected;
        public bool IsInferring => _isInferring;
        public string CurrentModel => _currentModel;
        public double TokensPerSecond => _tokensPerSecond;
        public int ContextLength => _contextLength;
        public double Temperature => _temperature;
        public double GpuTemperature => _gpuTemperature;
        public double VramUsageGb => _vramUsageGb;
        public double VramTotalGb => _vramTotalGb;
        public double VramUsagePercent => _vramTotalGb > 0 ? (_vramUsageGb / _vramTotalGb) * 100 : 0;
        public double InferenceProgress { get; private set; }
        public IReadOnlyCollection<double> TokenRateHistory => _tokenRateHistory;

        // Prompt templates for Evony RE tasks
        private readonly Dictionary<string, string> _promptTemplates = new()
        {
            ["explain_packet"] = @"You are an expert reverse engineer specializing in the Evony game protocol.
Analyze the following packet and explain what it does:

Packet Data:
{packet_data}

Provide:
1. Action type and purpose
2. Parameter breakdown
3. Expected server response
4. Related game mechanics",

            ["generate_script"] = @"You are an expert in Evony automation and scripting.
Generate a script to accomplish the following task:

Task: {task_description}

Current Game State:
{game_state}

Requirements:
- Use the Evony protocol actions available
- Include error handling
- Add appropriate delays to avoid detection
- Return the script in Python format",

            ["analyze_pattern"] = @"You are an expert in protocol analysis and pattern detection.
Analyze the following sequence of packets and identify patterns:

Packet Sequence:
{packet_sequence}

Identify:
1. Recurring patterns
2. Action sequences
3. Timing patterns
4. Potential automation opportunities",

            ["decode_unknown"] = @"You are an expert reverse engineer trained on EvonyClient and AutoEvony source code.
Attempt to decode the following unknown packet structure:

Raw Hex: {hex_data}
Extracted Strings: {strings}
Context: {context}

Provide your best analysis of:
1. Packet structure
2. Field types and meanings
3. Confidence level
4. Similar known packets",

            ["strategic_advice"] = @"You are an expert Evony strategist with deep knowledge of game mechanics.
Provide strategic advice based on the current situation:

Player State:
{player_state}

Question: {question}

Provide actionable advice with specific steps and timing."
        };

        private LlmIntegrationService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
        }

        #region Connection Management

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _connectionCts = new CancellationTokenSource();

                // Test connection based on backend
                var url = Backend == LlmBackend.LmStudio ? LmStudioUrl : OllamaUrl;
                var testEndpoint = Backend == LlmBackend.LmStudio ? "/models" : "/api/tags";

                var response = await _httpClient.GetAsync(url + testEndpoint, _connectionCts.Token);
                
                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    OnPropertyChanged(nameof(IsConnected));

                    // Get available models
                    await RefreshModelsAsync();

                    // Start GPU monitoring
                    _ = MonitorGpuAsync(_connectionCts.Token);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new LlmErrorEventArgs { Error = ex.Message });
                return false;
            }
        }

        public void Disconnect()
        {
            _connectionCts?.Cancel();
            _webSocket?.Dispose();
            _isConnected = false;
            OnPropertyChanged(nameof(IsConnected));
        }

        private async Task RefreshModelsAsync()
        {
            try
            {
                var url = Backend == LlmBackend.LmStudio ? $"{LmStudioUrl}/models" : $"{OllamaUrl}/api/tags";
                var response = await _httpClient.GetStringAsync(url);
                
                // Parse and set current model
                // For now, use default
                _currentModel = "evony-re-7b";
                OnPropertyChanged(nameof(CurrentModel));
            }
            catch { }
        }

        #endregion

        #region Inference Methods

        public async Task<string> GenerateAsync(string prompt, LlmOptions? options = null)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to LLM backend");

            options ??= new LlmOptions();
            _isInferring = true;
            InferenceProgress = 0;
            OnPropertyChanged(nameof(IsInferring));
            OnPropertyChanged(nameof(InferenceProgress));

            try
            {
                var startTime = DateTime.UtcNow;
                var tokenCount = 0;

                if (Backend == LlmBackend.LmStudio)
                {
                    return await GenerateLmStudioAsync(prompt, options, (tokens) =>
                    {
                        tokenCount += tokens;
                        UpdateTokenRate(tokenCount, startTime);
                    });
                }
                else
                {
                    return await GenerateOllamaAsync(prompt, options, (tokens) =>
                    {
                        tokenCount += tokens;
                        UpdateTokenRate(tokenCount, startTime);
                    });
                }
            }
            finally
            {
                _isInferring = false;
                InferenceProgress = 100;
                OnPropertyChanged(nameof(IsInferring));
                OnPropertyChanged(nameof(InferenceProgress));
            }
        }

        public async Task GenerateStreamAsync(string prompt, Action<string> onToken, LlmOptions options = null)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to LLM backend");

            options = options ?? new LlmOptions();
            _isInferring = true;
            InferenceProgress = 0;
            OnPropertyChanged(nameof(IsInferring));

            var startTime = DateTime.UtcNow;
            var tokenCount = 0;

            try
            {
                Action<string> tokenHandler = (token) =>
                {
                    tokenCount++;
                    UpdateTokenRate(tokenCount, startTime);
                    StreamTokenReceived?.Invoke(this, new LlmStreamEventArgs { Token = token });
                    onToken?.Invoke(token);
                };

                if (Backend == LlmBackend.LmStudio)
                {
                    await StreamLmStudioAsync(prompt, options, tokenHandler);
                }
                else
                {
                    await StreamOllamaAsync(prompt, options, tokenHandler);
                }
            }
            finally
            {
                _isInferring = false;
                InferenceProgress = 100;
                OnPropertyChanged(nameof(IsInferring));
            }
        }

        private async Task<string> GenerateLmStudioAsync(string prompt, LlmOptions options, Action<int> onTokens)
        {
            var request = new
            {
                model = _currentModel,
                messages = new[]
                {
                    new { role = "system", content = GetSystemPrompt() },
                    new { role = "user", content = prompt }
                },
                temperature = options.Temperature ?? _temperature,
                max_tokens = options.MaxTokens ?? 2048,
                stream = false
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{LmStudioUrl}/chat/completions", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            var doc = JObject.Parse(responseJson);
            var choices = doc["choices"] as JArray;
            var message = choices?[0]?["message"]?["content"]?.ToString() ?? "";

            var usage = doc["usage"];
            var totalTokens = usage?["total_tokens"]?.Value<int>() ?? 0;
            onTokens(totalTokens);

            return message;
        }

        private async Task<string> GenerateOllamaAsync(string prompt, LlmOptions options, Action<int> onTokens)
        {
            var request = new
            {
                model = _currentModel,
                prompt = $"{GetSystemPrompt()}\n\nUser: {prompt}\n\nAssistant:",
                stream = false,
                options = new
                {
                    temperature = options.Temperature ?? _temperature,
                    num_predict = options.MaxTokens ?? 2048
                }
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{OllamaUrl}/api/generate", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            var doc = JObject.Parse(responseJson);
            var responseText = doc["response"]?.ToString() ?? "";

            var evalCount = doc["eval_count"]?.Value<int>();
            if (evalCount.HasValue)
            {
                onTokens(evalCount.Value);
            }

            return responseText;
        }

        private async Task StreamLmStudioAsync(string prompt, LlmOptions options, Action<string> onToken)
        {
            var request = new
            {
                model = _currentModel,
                messages = new[]
                {
                    new { role = "system", content = GetSystemPrompt() },
                    new { role = "user", content = prompt }
                },
                temperature = options.Temperature ?? _temperature,
                max_tokens = options.MaxTokens ?? 2048,
                stream = true
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{LmStudioUrl}/chat/completions")
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var reader = new System.IO.StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;

                    var data = line.Substring(6);
                    if (data == "[DONE]") break;

                    var doc = JObject.Parse(data);
                    var choices = doc["choices"] as JArray;
                    if (choices != null && choices.Count > 0)
                    {
                        var delta = choices[0]["delta"];
                        var tokenContent = delta?["content"]?.ToString();
                        if (!string.IsNullOrEmpty(tokenContent))
                        {
                            onToken(tokenContent);
                        }
                    }
                }
            }
        }

        private async Task StreamOllamaAsync(string prompt, LlmOptions options, Action<string> onToken)
        {
            var request = new
            {
                model = _currentModel,
                prompt = $"{GetSystemPrompt()}\n\nUser: {prompt}\n\nAssistant:",
                stream = true,
                options = new
                {
                    temperature = options.Temperature ?? _temperature,
                    num_predict = options.MaxTokens ?? 2048
                }
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{OllamaUrl}/api/generate")
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var reader = new System.IO.StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;

                    var doc = JObject.Parse(line);
                    var tokenContent = doc["response"]?.ToString();
                    if (!string.IsNullOrEmpty(tokenContent))
                    {
                        onToken(tokenContent);
                    }

                    var done = doc["done"]?.Value<bool>() ?? false;
                    if (done) break;
                }
            }
        }

        #endregion

        #region Template Methods

        public async Task<string> ExplainPacketAsync(CapturedPacket packet)
        {
            var template = _promptTemplates["explain_packet"];
            var packetData = JsonConvert.SerializeObject(packet.DecodedPayload ?? new Dictionary<string, object>
            {
                ["raw"] = BitConverter.ToString(packet.RawData),
                ["action"] = packet.Action ?? "unknown"
            }, new JsonSerializerSettings { Formatting = Formatting.Indented });

            var prompt = template.Replace("{packet_data}", packetData);
            return await GenerateAsync(prompt);
        }

        public async Task<string> GenerateScriptAsync(string taskDescription, Dictionary<string, object> gameState = null)
        {
            var template = _promptTemplates["generate_script"];
            var stateJson = JsonConvert.SerializeObject(gameState ?? new Dictionary<string, object>(), 
                new JsonSerializerSettings { Formatting = Formatting.Indented });

            var prompt = template
                .Replace("{task_description}", taskDescription)
                .Replace("{game_state}", stateJson);

            return await GenerateAsync(prompt, new LlmOptions { MaxTokens = 4096 });
        }

        public async Task<string> AnalyzePatternAsync(IEnumerable<CapturedPacket> packets)
        {
            var template = _promptTemplates["analyze_pattern"];
            var sequence = string.Join("\n", packets.Select(p => 
                $"[{p.Timestamp:HH:mm:ss.fff}] {p.Direction} {p.Action ?? "unknown"} ({p.Size} bytes)"));

            var prompt = template.Replace("{packet_sequence}", sequence);
            return await GenerateAsync(prompt);
        }

        public async Task<string> DecodeUnknownPacketAsync(byte[] data, string context = null)
        {
            var template = _promptTemplates["decode_unknown"];
            var hexData = BitConverter.ToString(data).Replace("-", " ");
            var strings = ExtractStrings(data);

            var prompt = template
                .Replace("{hex_data}", hexData)
                .Replace("{strings}", string.Join(", ", strings))
                .Replace("{context}", context ?? "No additional context");

            return await GenerateAsync(prompt);
        }

        public async Task<string> GetStrategicAdviceAsync(string question, Dictionary<string, object> playerState = null)
        {
            var template = _promptTemplates["strategic_advice"];
            var stateJson = JsonConvert.SerializeObject(playerState ?? new Dictionary<string, object>(),
                new JsonSerializerSettings { Formatting = Formatting.Indented });

            var prompt = template
                .Replace("{player_state}", stateJson)
                .Replace("{question}", question);

            return await GenerateAsync(prompt);
        }

        private List<string> ExtractStrings(byte[] data)
        {
            var strings = new List<string>();
            var current = new StringBuilder();

            foreach (var b in data)
            {
                if (b >= 32 && b < 127)
                {
                    current.Append((char)b);
                }
                else
                {
                    if (current.Length >= 4)
                    {
                        strings.Add(current.ToString());
                    }
                    current.Clear();
                }
            }

            if (current.Length >= 4)
            {
                strings.Add(current.ToString());
            }

            return strings;
        }

        #endregion

        #region GPU Monitoring

        private async Task MonitorGpuAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await UpdateGpuStatsAsync();
                    await Task.Delay(2000, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch { }
            }
        }

        private async Task UpdateGpuStatsAsync()
        {
            try
            {
                // Try nvidia-smi for GPU stats
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "nvidia-smi",
                        Arguments = "--query-gpu=temperature.gpu,memory.used,memory.total --format=csv,noheader,nounits",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                var parts = output.Trim().Split(',');
                if (parts.Length >= 3)
                {
                    _gpuTemperature = double.Parse(parts[0].Trim());
                    _vramUsageGb = double.Parse(parts[1].Trim()) / 1024.0;
                    _vramTotalGb = double.Parse(parts[2].Trim()) / 1024.0;

                    OnPropertyChanged(nameof(GpuTemperature));
                    OnPropertyChanged(nameof(VramUsageGb));
                    OnPropertyChanged(nameof(VramTotalGb));
                    OnPropertyChanged(nameof(VramUsagePercent));
                }
            }
            catch
            {
                // nvidia-smi not available, use defaults
            }
        }

        private void UpdateTokenRate(int tokenCount, DateTime startTime)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
            if (elapsed > 0)
            {
                _tokensPerSecond = tokenCount / elapsed;
                _tokenRateHistory.Enqueue(_tokensPerSecond);
                while (_tokenRateHistory.Count > 20)
                {
                    _tokenRateHistory.Dequeue();
                }
                OnPropertyChanged(nameof(TokensPerSecond));
            }

            // Update inference progress (estimate based on typical response length)
            InferenceProgress = Math.Min(95, tokenCount / 20.0);
            OnPropertyChanged(nameof(InferenceProgress));
        }

        #endregion

        #region Configuration

        public void SetModel(string modelName)
        {
            _currentModel = modelName;
            OnPropertyChanged(nameof(CurrentModel));
        }

        public void SetTemperature(double temperature)
        {
            _temperature = MathEx.Clamp(temperature, 0, 2);
            OnPropertyChanged(nameof(Temperature));
        }

        public void SetContextLength(int length)
        {
            _contextLength = MathEx.Clamp(length, 512, 32768);
            OnPropertyChanged(nameof(ContextLength));
        }

        private string GetSystemPrompt()
        {
            return @"You are an expert reverse engineer and game analyst specializing in Evony: The King's Return.
You have been trained on:
- EvonyClient source code (Flash/AS3)
- AutoEvony automation framework
- Flash 10 runtime internals
- AMF3 protocol specifications
- Game mechanics and strategies

You understand:
- AMF3 encoding/decoding
- Network packet structures
- Game state management
- Automation patterns and anti-detection
- Strategic gameplay optimization

Provide accurate, technical responses focused on reverse engineering and game analysis.
When analyzing packets, be precise about data types, field meanings, and protocol patterns.
When generating scripts, include proper error handling and timing considerations.";
        }

        #endregion

        #region Status Methods

        /// <summary>
        /// Gets the current inference progress (0-100).
        /// </summary>
        public double GetInferenceProgress() => InferenceProgress;

        /// <summary>
        /// Gets the current GPU temperature.
        /// </summary>
        public double GetGpuTemperature() => _gpuTemperature;

        /// <summary>
        /// Gets the current VRAM usage in GB.
        /// </summary>
        public double GetVramUsage() => _vramUsageGb;

        /// <summary>
        /// Gets the current tokens per second rate.
        /// </summary>
        public double GetTokensPerSecond() => _tokensPerSecond;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            Disconnect();
            _httpClient.Dispose();
            _webSocket?.Dispose();
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #region Models

    public enum LlmBackend
    {
        LmStudio,
        Ollama
    }

    public class LlmOptions
    {
        public double? Temperature { get; set; }
        public int? MaxTokens { get; set; }
        public double? TopP { get; set; }
        public int? TopK { get; set; }
        public string[]? StopSequences { get; set; }
    }

    #endregion

    #region Events

    public class LlmResponseEventArgs : EventArgs
    {
        public string Response { get; set; } = "";
        public int TokenCount { get; set; }
        public double Duration { get; set; }
    }

    public class LlmStreamEventArgs : EventArgs
    {
        public string Token { get; set; } = "";
    }

    public class LlmErrorEventArgs : EventArgs
    {
        public string Error { get; set; } = "";
    }

    #endregion
}
