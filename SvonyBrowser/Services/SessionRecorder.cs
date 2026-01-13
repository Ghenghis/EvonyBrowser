using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Records, replays, and analyzes game sessions with full packet-level fidelity.
    /// Enables time-travel debugging and session sharing.
    /// </summary>
    public sealed class SessionRecorder : IDisposable
    {
        #region Singleton

        private static readonly Lazy<SessionRecorder> _lazyInstance =
            new Lazy<SessionRecorder>(() => new SessionRecorder(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static SessionRecorder Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly List<RecordedPacket> _packets = new List<RecordedPacket>();
        private readonly object _lock = new object();
        private readonly string _recordingsPath;

        private bool _isRecording = false;
        private bool _isReplaying = false;
        private DateTime _recordingStartTime;
        private string _currentSessionId;
        private CancellationTokenSource _replayCts;
        private bool _disposed = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether recording is active.
        /// </summary>
        public bool IsRecording => _isRecording;

        /// <summary>
        /// Gets whether replay is active.
        /// </summary>
        public bool IsReplaying => _isReplaying;

        /// <summary>
        /// Gets the current session ID.
        /// </summary>
        public string CurrentSessionId => _currentSessionId;

        /// <summary>
        /// Gets the number of recorded packets.
        /// </summary>
        public int PacketCount
        {
            get
            {
                lock (_lock)
                {
                    return _packets.Count;
                }
            }
        }

        /// <summary>
        /// Gets the recording duration.
        /// </summary>
        public TimeSpan RecordingDuration => _isRecording 
            ? DateTime.UtcNow - _recordingStartTime 
            : TimeSpan.Zero;

        #endregion

        #region Events

        /// <summary>
        /// Fired when recording state changes.
        /// </summary>
        public event Action<bool> RecordingStateChanged;

        /// <summary>
        /// Fired when a packet is recorded.
        /// </summary>
        public event Action<RecordedPacket> PacketRecorded;

        /// <summary>
        /// Fired when a packet is replayed.
        /// </summary>
        public event Action<RecordedPacket> PacketReplayed;

        /// <summary>
        /// Fired when replay progress changes.
        /// </summary>
        public event Action<double> ReplayProgressChanged;

        #endregion

        #region Constructor

        private SessionRecorder()
        {
            _recordingsPath = Path.Combine(App.DataPath, "recordings");
            Directory.CreateDirectory(_recordingsPath);

            // Subscribe to traffic events
            TrafficPipeClient.Instance.PacketReceived += OnPacketReceived;

            App.Logger.Information("SessionRecorder initialized");
        }

        #endregion

        #region Public Methods - Recording

        /// <summary>
        /// Starts recording a new session.
        /// </summary>
        public void StartRecording(string sessionName = null)
        {
            if (_isRecording)
            {
                App.Logger.Warning("Recording already in progress");
                return;
            }

            lock (_lock)
            {
                _packets.Clear();
            }

            _currentSessionId = sessionName ?? $"session_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            _recordingStartTime = DateTime.UtcNow;
            _isRecording = true;

            RecordingStateChanged?.Invoke(true);
            App.Logger.Information("Started recording session: {SessionId}", _currentSessionId);
        }

        /// <summary>
        /// Stops recording and saves the session.
        /// </summary>
        public async Task<string> StopRecordingAsync()
        {
            if (!_isRecording)
            {
                App.Logger.Warning("No recording in progress");
                return "";
            }

            _isRecording = false;
            RecordingStateChanged?.Invoke(false);

            // Save the session
            var filePath = await SaveSessionAsync(_currentSessionId!);
        
            App.Logger.Information("Stopped recording. Saved {Count} packets to {Path}", 
                _packets.Count, filePath);

            return filePath;
        }

        /// <summary>
        /// Saves the current session to a file.
        /// </summary>
        public async Task<string> SaveSessionAsync(string sessionId, string format = "json")
        {
            var session = new RecordedSession
            {
                SessionId = sessionId,
                StartTime = _recordingStartTime,
                EndTime = DateTime.UtcNow,
                PacketCount = _packets.Count,
                Packets = _packets.ToList()
            };

            var filePath = Path.Combine(_recordingsPath, $"{sessionId}.{format}");

            switch (format?.ToLower())
            {
                case "json":
                    await SaveAsJsonAsync(session, filePath);
                    break;
                case "har":
                    await SaveAsHarAsync(session, filePath);
                    break;
                case "compressed":
                    await SaveCompressedAsync(session, filePath + ".gz");
                    filePath += ".gz";
                    break;
                default:
                    await SaveAsJsonAsync(session, filePath);
                    break;
            }

            return filePath;
        }

        #endregion

        #region Public Methods - Replay

        /// <summary>
        /// Loads a session from file.
        /// </summary>
        public async Task<RecordedSession?> LoadSessionAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                App.Logger.Error("Session file not found: {Path}", filePath);
                return null;
            }

            try
            {
                string json;
            
                if (filePath.EndsWith(".gz"))
                {
                    var fileStream = File.OpenRead(filePath); // TODO: Add using block for proper disposal
                    var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress); // TODO: Add using block for proper disposal
                    var reader = new StreamReader(gzipStream); // TODO: Add using block for proper disposal
                    json = await reader.ReadToEndAsync();
                }
                else
                {
                    json = await FileEx.ReadAllTextAsync(filePath);
                }

                return JsonConvert.DeserializeObject<RecordedSession>(json);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to load session: {Path}", filePath);
                return null;
            }
        }

        /// <summary>
        /// Starts replaying a session.
        /// </summary>
        public async Task StartReplayAsync(RecordedSession session, double speed = 1.0)
        {
            if (_isReplaying)
            {
                App.Logger.Warning("Replay already in progress");
                return;
            }

            _isReplaying = true;
            _replayCts = new CancellationTokenSource();

            App.Logger.Information("Starting replay of session: {SessionId} at {Speed}x speed", 
                session.SessionId, speed);

            try
            {
                await ReplaySessionAsync(session, speed, _replayCts.Token);
            }
            catch (OperationCanceledException)
            {
                App.Logger.Information("Replay cancelled");
            }
            finally
            {
                _isReplaying = false;
            }
        }

        /// <summary>
        /// Stops the current replay.
        /// </summary>
        public void StopReplay()
        {
            _replayCts?.Cancel();
        }

        /// <summary>
        /// Gets a specific packet by index.
        /// </summary>
        public RecordedPacket? GetPacket(int index)
        {
            lock (_lock)
            {
                if (index >= 0 && index < _packets.Count)
                    return _packets[index];
                return null;
            }
        }

        /// <summary>
        /// Gets packets within a time range.
        /// </summary>
        public List<RecordedPacket> GetPacketsInRange(DateTime start, DateTime end)
        {
            lock (_lock)
            {
                return _packets
                    .Where(p => p.Timestamp >= start && p.Timestamp <= end)
                    .ToList();
            }
        }

        /// <summary>
        /// Searches packets by action or content.
        /// </summary>
        public List<RecordedPacket> SearchPackets(string query)
        {
            lock (_lock)
            {
                return _packets
                    .Where(p => 
                        p.Action.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        p.DecodedJson?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets available session files.
        /// </summary>
        public List<SessionInfo> GetAvailableSessions()
        {
            var sessions = new List<SessionInfo>();

            foreach (var file in Directory.GetFiles(_recordingsPath, "*.json"))
            {
                try
                {
                    var info = new FileInfo(file);
                    sessions.Add(new SessionInfo
                    {
                        SessionId = Path.GetFileNameWithoutExtension(file),
                        FilePath = file,
                        FileSize = info.Length,
                        CreatedAt = info.CreationTimeUtc
                    });
                }
                catch { }
            }

            return sessions.OrderByDescending(s => s.CreatedAt).ToList();
        }

        /// <summary>
        /// Gets a recording by ID.
        /// </summary>
        public RecordedSession? GetRecording(string recordingId)
        {
            var filePath = Path.Combine(_recordingsPath, $"{recordingId}.json");
            if (!File.Exists(filePath)) return null;
        
            try
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<RecordedSession>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets all recordings.
        /// </summary>
        public List<RecordedSession> GetAllRecordings()
        {
            var recordings = new List<RecordedSession>();
        
            foreach (var file in Directory.GetFiles(_recordingsPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var recording = JsonConvert.DeserializeObject<RecordedSession>(json);
                    if (recording != null)
                    {
                        recordings.Add(recording);
                    }
                }
                catch { }
            }
        
            return recordings;
        }

        #endregion

        #region Public Methods - Analysis

        /// <summary>
        /// Compares two sessions and returns differences.
        /// </summary>
        public SessionDiff CompareSession(RecordedSession session1, RecordedSession session2)
        {
            var diff = new SessionDiff
            {
                Session1Id = session1.SessionId,
                Session2Id = session2.SessionId
            };

            // Find unique actions in each session
            var actions1 = session1.Packets.Select(p => p.Action).Distinct().ToHashSet();
            var actions2 = session2.Packets.Select(p => p.Action).Distinct().ToHashSet();

            diff.OnlyInSession1 = actions1.Except(actions2).ToList();
            diff.OnlyInSession2 = actions2.Except(actions1).ToList();
            diff.CommonActions = actions1.Intersect(actions2).ToList();

            // Compare packet counts per action
            var counts1 = session1.Packets.GroupBy(p => p.Action).ToDictionary(g => g.Key, g => g.Count());
            var counts2 = session2.Packets.GroupBy(p => p.Action).ToDictionary(g => g.Key, g => g.Count());

            foreach (var action in diff.CommonActions)
            {
                var count1 = counts1.GetValueOrDefault(action);
                var count2 = counts2.GetValueOrDefault(action);
            
                if (count1 != count2)
                {
                    diff.CountDifferences[action] = (count1, count2);
                }
            }

            return diff;
        }

        /// <summary>
        /// Reconstructs game state at a specific point in the session.
        /// </summary>
        public GameStateSnapshot ReconstructStateAt(RecordedSession session, DateTime timestamp)
        {
            var engine = new GameStateEngine(noSubscription: true);

            foreach (var packet in session.Packets.Where(p => p.Timestamp <= timestamp))
            {
                if (!string.IsNullOrEmpty(packet.DecodedJson))
                {
                    try
                    {
                        var data = JToken.Parse(packet.DecodedJson);
                        engine.ProcessPacket(packet.Action, data, packet.IsResponse);
                    }
                    catch { }
                }
            }

            return engine.GetSnapshot();
        }

        /// <summary>
        /// Generates session statistics.
        /// </summary>
        public SessionStatistics GetStatistics(RecordedSession session)
        {
            var stats = new SessionStatistics
            {
                SessionId = session.SessionId,
                Duration = session.EndTime - session.StartTime,
                TotalPackets = session.PacketCount,
                RequestCount = session.Packets.Count(p => !p.IsResponse),
                ResponseCount = session.Packets.Count(p => p.IsResponse)
            };

            // Action breakdown
            stats.ActionCounts = session.Packets
                .GroupBy(p => p.Action)
                .ToDictionary(g => g.Key, g => g.Count());

            // Category breakdown
            stats.CategoryCounts = session.Packets
                .GroupBy(p => p.Action.Split('.').FirstOrDefault() ?? "unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            // Data size
            stats.TotalDataSize = session.Packets.Sum(p => p.RawData?.Length ?? 0);

            // Timing analysis
            if (session.Packets.Count > 1)
            {
                var intervals = new List<double>();
                for (int i = 1; i < session.Packets.Count; i++)
                {
                    intervals.Add((session.Packets[i].Timestamp - session.Packets[i - 1].Timestamp).TotalMilliseconds);
                }

                stats.AveragePacketInterval = TimeSpan.FromMilliseconds(intervals.Average());
                stats.MinPacketInterval = TimeSpan.FromMilliseconds(intervals.Min());
                stats.MaxPacketInterval = TimeSpan.FromMilliseconds(intervals.Max());
            }

            return stats;
        }

        #endregion

        #region Private Methods

        private void OnPacketReceived(FiddlerTrafficData data)
        {
            if (!_isRecording) return;

            var packet = new RecordedPacket
            {
                Index = _packets.Count,
                Timestamp = DateTime.UtcNow,
                RelativeTime = DateTime.UtcNow - _recordingStartTime,
                Action = data.Action ?? "",
                IsResponse = data.Direction == "response",
                RawData = data.HexData != null ? Convert.FromHexString(data.HexData.Replace(" ", "")) : null,
                DecodedJson = data.Decoded?.ToString()
            };

            lock (_lock)
            {
                _packets.Add(packet);
            }

            PacketRecorded?.Invoke(packet);
        }

        private async Task ReplaySessionAsync(RecordedSession session, double speed, CancellationToken cancellationToken)
        {
            var packets = session.Packets.OrderBy(p => p.Timestamp).ToList();
            var startTime = DateTime.UtcNow;
            var sessionStartTime = packets.First().Timestamp;

            for (int i = 0; i < packets.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var packet = packets[i];
            
                // Calculate when this packet should be replayed
                var relativeTime = packet.Timestamp - sessionStartTime;
                var targetTime = startTime + TimeSpan.FromTicks((long)(relativeTime.Ticks / speed));
            
                // Wait until the right time
                var waitTime = targetTime - DateTime.UtcNow;
                if (waitTime > TimeSpan.Zero)
                {
                    await Task.Delay(waitTime, cancellationToken);
                }

                // Process the packet
                if (!string.IsNullOrEmpty(packet.DecodedJson))
                {
                    try
                    {
                        var data = JToken.Parse(packet.DecodedJson);
                        GameStateEngine.Instance.ProcessPacket(packet.Action, data, packet.IsResponse);
                    }
                    catch { }
                }

                PacketReplayed?.Invoke(packet);
                ReplayProgressChanged?.Invoke((double)(i + 1) / packets.Count);
            }
        }

        private async Task SaveAsJsonAsync(RecordedSession session, string filePath)
        {
            var json = JsonConvert.SerializeObject(session, Formatting.Indented);
            await FileEx.WriteAllTextAsync(filePath, json);
        }

        private async Task SaveAsHarAsync(RecordedSession session, string filePath)
        {
            // Convert to HAR format
            var har = new JObject
            {
                ["log"] = new JObject
                {
                    ["version"] = "1.2",
                    ["creator"] = new JObject
                    {
                        ["name"] = "Svony Browser",
                        ["version"] = "2.0"
                    },
                    ["entries"] = new JArray(session.Packets.Select(p => new JObject
                    {
                        ["startedDateTime"] = p.Timestamp.ToString("o"),
                        ["time"] = p.RelativeTime.TotalMilliseconds,
                        ["request"] = new JObject
                        {
                            ["method"] = p.IsResponse ? "RESPONSE" : "REQUEST",
                            ["url"] = $"amf://{p.Action}"
                        },
                        ["response"] = new JObject
                        {
                            ["content"] = new JObject
                            {
                                ["text"] = p.DecodedJson ?? ""
                            }
                        }
                    }))
                }
            };

            await FileEx.WriteAllTextAsync(filePath, har.ToString(Formatting.Indented));
        }

        private async Task SaveCompressedAsync(RecordedSession session, string filePath)
        {
            var json = JsonConvert.SerializeObject(session);
        
            var fileStream = File.Create(filePath); // TODO: Add using block for proper disposal
            var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal); // TODO: Add using block for proper disposal
            var writer = new StreamWriter(gzipStream); // TODO: Add using block for proper disposal            await writer.WriteAsync(json);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _replayCts?.Cancel();
            _replayCts?.Dispose();

            TrafficPipeClient.Instance.PacketReceived -= OnPacketReceived;

            App.Logger.Information("SessionRecorder disposed");
        }

        #endregion
    }

    #region Models

    public class RecordedSession
    {
        public string SessionId { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int PacketCount { get; set; }
        public List<RecordedPacket> Packets { get; set; } = new List<RecordedPacket>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    public class RecordedPacket
    {
        public int Index { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan RelativeTime { get; set; }
        public string Action { get; set; } = "";
        public bool IsResponse { get; set; }
        public byte[]? RawData { get; set; }
        public string DecodedJson { get; set; }
    }

    public class SessionInfo
    {
        public string SessionId { get; set; } = "";
        public string FilePath { get; set; } = "";
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SessionDiff
    {
        public string Session1Id { get; set; } = "";
        public string Session2Id { get; set; } = "";
        public List<string> OnlyInSession1 { get; set; } = new List<string>();
        public List<string> OnlyInSession2 { get; set; } = new List<string>();
        public List<string> CommonActions { get; set; } = new List<string>();
        public Dictionary<string, (int Count1, int Count2)> CountDifferences { get; set; } = new Dictionary<string, (int Count1, int Count2)>();
    }

    public class SessionStatistics
    {
        public string SessionId { get; set; } = "";
        public TimeSpan Duration { get; set; }
        public int TotalPackets { get; set; }
        public int RequestCount { get; set; }
        public int ResponseCount { get; set; }
        public Dictionary<string, int> ActionCounts { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> CategoryCounts { get; set; } = new Dictionary<string, int>();
        public long TotalDataSize { get; set; }
        public TimeSpan AveragePacketInterval { get; set; }
        public TimeSpan MinPacketInterval { get; set; }
        public TimeSpan MaxPacketInterval { get; set; }
    }

    #endregion

}