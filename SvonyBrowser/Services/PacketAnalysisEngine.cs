using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
namespace SvonyBrowser.Services
{
    /// <summary>
    /// Deep Packet Analysis Engine for real-time protocol analysis
    /// </summary>
    public class PacketAnalysisEngine : INotifyPropertyChanged, IDisposable
    {
        private static readonly Lazy<PacketAnalysisEngine> _lazyInstance =
            new Lazy<PacketAnalysisEngine>(() => new PacketAnalysisEngine(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static PacketAnalysisEngine Instance => _lazyInstance.Value;

        private readonly ConcurrentQueue<CapturedPacket> _packetQueue = new ConcurrentQueue<CapturedPacket>();
        private readonly ConcurrentDictionary<string, PacketPattern> _patterns = new Dictionary<string, PacketPattern>();
        private readonly ConcurrentDictionary<string, ProtocolAction> _learnedActions = new Dictionary<string, ProtocolAction>();
        private readonly List<PacketBreakpoint> _breakpoints = new List<PacketBreakpoint>();
        private readonly List<CapturedPacket> _packetHistory = new List<CapturedPacket>();
        private readonly object _historyLock = new object();
        
        private CancellationTokenSource _analysisCts;
        private Task _analysisTask;
        private bool _isCapturing;
        private bool _isDisposed;
        private long _totalPackets;
        private long _decodedPackets;
        private long _errorPackets;
        private double _packetsPerSecond;
        private DateTime _lastRateCalculation = DateTime.UtcNow;
        private int _packetsSinceLastCalculation;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<PacketCapturedEventArgs> PacketCaptured;
        public event EventHandler<PacketDecodedEventArgs> PacketDecoded;
        public event EventHandler<BreakpointHitEventArgs> BreakpointHit;
        public event EventHandler<PatternDetectedEventArgs> PatternDetected;
        public event EventHandler<NewActionDiscoveredEventArgs> NewActionDiscovered;

        public bool IsCapturing => _isCapturing;
        public long TotalPackets => _totalPackets;
        public long DecodedPackets => _decodedPackets;
        public long ErrorPackets => _errorPackets;
        public double PacketsPerSecond => _packetsPerSecond;
        public double DecodeSuccessRate => _totalPackets > 0 ? (_decodedPackets * 100.0 / _totalPackets) : 100;
        public int HistoryCount { get { lock (_historyLock) return _packetHistory.Count; } }

        private PacketAnalysisEngine()
        {
            InitializeDefaultPatterns();
        }

        #region Capture Control

        public void StartCapture()
        {
            if (_isCapturing) return;

            _isCapturing = true;
            _analysisCts = new CancellationTokenSource();
            _analysisTask = Task.Run(() => AnalysisLoop(_analysisCts.Token));

            OnPropertyChanged(nameof(IsCapturing));
        }

        public void StopCapture()
        {
            if (!_isCapturing) return;

            _isCapturing = false;
            _analysisCts?.Cancel();
            _analysisTask?.Wait(TimeSpan.FromSeconds(5));

            OnPropertyChanged(nameof(IsCapturing));
        }

        public void ClearHistory()
        {
            lock (_historyLock)
            {
                _packetHistory.Clear();
            }
            _totalPackets = 0;
            _decodedPackets = 0;
            _errorPackets = 0;
        }

        #endregion

        #region Packet Processing

        public void EnqueuePacket(byte[] data, PacketDirection direction, string source = null)
        {
            var packet = new CapturedPacket
            {
                Id = Interlocked.Increment(ref _totalPackets),
                Timestamp = DateTime.UtcNow,
                RawData = data,
                Direction = direction,
                Source = source ?? "Unknown",
                Size = data.Length
            };

            _packetQueue.Enqueue(packet);
            PacketCaptured?.Invoke(this, new PacketCapturedEventArgs { Packet = packet });

            // Update rate calculation
            _packetsSinceLastCalculation++;
            var now = DateTime.UtcNow;
            if ((now - _lastRateCalculation).TotalSeconds >= 1)
            {
                _packetsPerSecond = _packetsSinceLastCalculation / (now - _lastRateCalculation).TotalSeconds;
                _packetsSinceLastCalculation = 0;
                _lastRateCalculation = now;
                OnPropertyChanged(nameof(PacketsPerSecond));
            }
        }

        private async Task AnalysisLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (_packetQueue.TryDequeue(out var packet))
                {
                    await ProcessPacketAsync(packet);
                }
                else
                {
                    await Task.Delay(10, ct);
                }
            }
        }

        private async Task ProcessPacketAsync(CapturedPacket packet)
        {
            try
            {
                // Decode the packet
                var decoded = await DecodePacketAsync(packet);
                packet.DecodedPayload = decoded;
                packet.IsDecoded = decoded != null;

                if (packet.IsDecoded)
                {
                    Interlocked.Increment(ref _decodedPackets);
                    
                    // Extract action name
                    packet.Action = ExtractActionName(decoded!);
                    
                    // Check breakpoints
                    CheckBreakpoints(packet);
                    
                    // Detect patterns
                    DetectPatterns(packet);
                    
                    // Learn new actions
                    LearnAction(packet);

                    PacketDecoded?.Invoke(this, new PacketDecodedEventArgs { Packet = packet });
                }
                else
                {
                    Interlocked.Increment(ref _errorPackets);
                }

                // Add to history
                lock (_historyLock)
                {
                    _packetHistory.Add(packet);
                    
                    // Limit history size
                    while (_packetHistory.Count > 10000)
                    {
                        _packetHistory.RemoveAt(0);
                    }
                }

                OnPropertyChanged(nameof(TotalPackets));
                OnPropertyChanged(nameof(DecodedPackets));
                OnPropertyChanged(nameof(DecodeSuccessRate));
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _errorPackets);
                packet.Error = ex.Message;
            }
        }

        private async Task<Dictionary<string, object>?> DecodePacketAsync(CapturedPacket packet)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Try AMF3 decoding first
                    if (IsAmf3Packet(packet.RawData))
                    {
                        return DecodeAmf3(packet.RawData);
                    }

                    // Try JSON
                    if (IsJsonPacket(packet.RawData))
                    {
                        var json = Encoding.UTF8.GetString(packet.RawData);
                        return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    }

                    // Try binary protocol
                    return DecodeBinaryProtocol(packet.RawData);
                }
                catch
                {
                    return null;
                }
            });
        }

        #endregion

        #region Decoding Methods

        private bool IsAmf3Packet(byte[] data)
        {
            // AMF3 packets typically start with specific markers
            return data.Length > 4 && (data[0] == 0x00 || data[0] == 0x0A);
        }

        private bool IsJsonPacket(byte[] data)
        {
            if (data.Length < 2) return false;
            return data[0] == '{' || data[0] == '[';
        }

        private Dictionary<string, object> DecodeAmf3(byte[] data)
        {
            var result = new Dictionary<string, object>();
            int offset = 0;

            try
            {
                // Read header
                if (data.Length < 10) return null;

                var length = ReadInt32BE(data, ref offset);
                var msgType = data[offset++];
                var seqId = ReadInt32BE(data, ref offset);
                var flags = data[offset++];

                result["_header"] = new Dictionary<string, object>
                {
                    ["length"] = length,
                    ["msgType"] = msgType,
                    ["seqId"] = seqId,
                    ["flags"] = flags
                };

                // Read action string
                if (offset < data.Length && data[offset] == 0x06) // String marker
                {
                    offset++;
                    var actionLength = ReadU29(data, ref offset);
                    if (actionLength > 1)
                    {
                        var strLen = actionLength >> 1;
                        if (offset + strLen <= data.Length)
                        {
                            result["action"] = Encoding.UTF8.GetString(data, offset, strLen);
                            offset += strLen;
                        }
                    }
                }

                // Read parameters
                var parameters = new Dictionary<string, object>();
                while (offset < data.Length)
                {
                    var value = ReadAmf3Value(data, ref offset);
                    if (value != null)
                    {
                        parameters[$"param{parameters.Count}"] = value;
                    }
                    else
                    {
                        break;
                    }
                }
                result["params"] = parameters;

                return result;
            }
            catch
            {
                return null;
            }
        }

        private object ReadAmf3Value(byte[] data, ref int offset)
        {
            if (offset >= data.Length) return null;

            var marker = data[offset++];
            return marker switch
            {
                0x00 => null, // Undefined
                0x01 => null, // Null
                0x02 => false, // False
                0x03 => true, // True
                0x04 => ReadU29(data, ref offset), // Integer
                0x05 => ReadDouble(data, ref offset), // Double
                0x06 => ReadAmf3String(data, ref offset), // String
                0x09 => ReadAmf3Array(data, ref offset), // Array
                0x0A => ReadAmf3Object(data, ref offset), // Object
                _ => null
            };
        }

        private string ReadAmf3String(byte[] data, ref int offset)
        {
            var u29 = ReadU29(data, ref offset);
            if ((u29 & 1) == 0) return null; // Reference

            var length = u29 >> 1;
            if (length == 0 || offset + length > data.Length) return "";

            var str = Encoding.UTF8.GetString(data, offset, length);
            offset += length;
            return str;
        }

        private List<object> ReadAmf3Array(byte[] data, ref int offset)
        {
            var u29 = ReadU29(data, ref offset);
            if ((u29 & 1) == 0) return null; // Reference

            var count = u29 >> 1;
            var result = new List<object>();

            // Skip associative portion
            while (offset < data.Length)
            {
                var key = ReadAmf3String(data, ref offset);
                if (string.IsNullOrEmpty(key)) break;
                ReadAmf3Value(data, ref offset); // Skip value
            }

            // Read dense portion
            for (int i = 0; i < count && offset < data.Length; i++)
            {
                result.Add(ReadAmf3Value(data, ref offset));
            }

            return result;
        }

        private Dictionary<string, object> ReadAmf3Object(byte[] data, ref int offset)
        {
            var u29 = ReadU29(data, ref offset);
            if ((u29 & 1) == 0) return null; // Reference

            var result = new Dictionary<string, object>();

            // Read dynamic properties
            while (offset < data.Length)
            {
                var key = ReadAmf3String(data, ref offset);
                if (string.IsNullOrEmpty(key)) break;
                result[key] = ReadAmf3Value(data, ref offset);
            }

            return result;
        }

        private int ReadU29(byte[] data, ref int offset)
        {
            int result = 0;
            for (int i = 0; i < 4 && offset < data.Length; i++)
            {
                var b = data[offset++];
                if (i < 3)
                {
                    result = (result << 7) | (b & 0x7F);
                    if ((b & 0x80) == 0) break;
                }
                else
                {
                    result = (result << 8) | b;
                }
            }
            return result;
        }

        private int ReadInt32BE(byte[] data, ref int offset)
        {
            var result = (data[offset] << 24) | (data[offset + 1] << 16) | 
                         (data[offset + 2] << 8) | data[offset + 3];
            offset += 4;
            return result;
        }

        private double ReadDouble(byte[] data, ref int offset)
        {
            var bytes = new byte[8];
            Array.Copy(data, offset, bytes, 0, 8);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            offset += 8;
            return BitConverter.ToDouble(bytes, 0);
        }

        private Dictionary<string, object> DecodeBinaryProtocol(byte[] data)
        {
            // Generic binary protocol decoder
            var result = new Dictionary<string, object>
            {
                ["_raw"] = BitConverter.ToString(data).Replace("-", " "),
                ["_length"] = data.Length
            };

            // Try to extract readable strings
            var strings = ExtractStrings(data);
            if (strings.Any())
            {
                result["_strings"] = strings;
            }

            return result;
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

        private string ExtractActionName(Dictionary<string, object> decoded)
        {
            if (decoded.TryGetValue("action", out var action))
            {
                return action?.ToString();
            }
            return null;
        }

        #endregion

        #region Pattern Detection

        private void InitializeDefaultPatterns()
        {
            // Login sequence
            _patterns["login_sequence"] = new PacketPattern
            {
                Name = "Login Sequence",
                Actions = new[] { "user.login", "user.getInfo", "city.getCityInfo" },
                Description = "Standard login flow"
            };

            // Rally attack
            _patterns["rally_attack"] = new PacketPattern
            {
                Name = "Rally Attack",
                Actions = new[] { "march.createRally", "march.joinRally", "march.startRally" },
                Description = "Coordinated rally attack"
            };

            // Resource collection
            _patterns["resource_collect"] = new PacketPattern
            {
                Name = "Resource Collection",
                Actions = new[] { "city.collectResource", "city.collectAllResources" },
                Description = "Resource gathering"
            };

            // Hero upgrade
            _patterns["hero_upgrade"] = new PacketPattern
            {
                Name = "Hero Upgrade",
                Actions = new[] { "hero.upgradeSkill", "hero.addExperience", "hero.levelUp" },
                Description = "Hero improvement actions"
            };
        }

        private void DetectPatterns(CapturedPacket packet)
        {
            if (string.IsNullOrEmpty(packet.Action)) return;

            lock (_historyLock)
            {
                var recentActions = _packetHistory
                    .TakeLast(10)
                    .Where(p => !string.IsNullOrEmpty(p.Action))
                    .Select(p => p.Action!)
                    .ToList();

                foreach (var pattern in _patterns.Values)
                {
                    if (IsPatternMatch(recentActions, pattern.Actions))
                    {
                        PatternDetected?.Invoke(this, new PatternDetectedEventArgs
                        {
                            Pattern = pattern,
                            TriggerPacket = packet
                        });
                    }
                }
            }
        }

        private bool IsPatternMatch(List<string> recent, string[] pattern)
        {
            if (recent.Count < pattern.Length) return false;

            var lastN = recent.TakeLast(pattern.Length).ToArray();
            for (int i = 0; i < pattern.Length; i++)
            {
                if (!lastN[i].Contains(pattern[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        #endregion

        #region Protocol Learning

        private void LearnAction(CapturedPacket packet)
        {
            if (string.IsNullOrEmpty(packet.Action)) return;

            var isNew = !_learnedActions.ContainsKey(packet.Action);

            _learnedActions.AddOrUpdate(packet.Action,
                _ => CreateProtocolAction(packet),
                (_, existing) => UpdateProtocolAction(existing, packet));

            if (isNew)
            {
                NewActionDiscovered?.Invoke(this, new NewActionDiscoveredEventArgs
                {
                    Action = _learnedActions[packet.Action],
                    Packet = packet
                });
            }
        }

        private ProtocolAction CreateProtocolAction(CapturedPacket packet)
        {
            return new ProtocolAction
            {
                Name = packet.Action!,
                Category = ExtractCategory(packet.Action!),
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow,
                Occurrences = 1,
                Direction = packet.Direction.ToString(),
                ParameterList = ExtractParameters(packet.DecodedPayload)
            };
        }

        private ProtocolAction UpdateProtocolAction(ProtocolAction existing, CapturedPacket packet)
        {
            existing.LastSeen = DateTime.UtcNow;
            existing.Occurrences++;
            
            // Merge parameters
            var newParams = ExtractParameters(packet.DecodedPayload);
            foreach (var param in newParams)
            {
                if (!existing.ParameterList.Any(p => p.Name == param.Name))
                {
                    existing.ParameterList.Add(param);
                }
            }

            return existing;
        }

        private string ExtractCategory(string action)
        {
            var parts = action.Split('.');
            return parts.Length > 1 ? parts[0] : "unknown";
        }

        private List<ProtocolParameter> ExtractParameters(Dictionary<string, object> decoded)
        {
            var result = new List<ProtocolParameter>();
            if (decoded == null) return result;

            if (decoded.TryGetValue("params", out var paramsObj) && paramsObj is Dictionary<string, object> parameters)
            {
                foreach (var kvp in parameters)
                {
                    result.Add(new ProtocolParameter
                    {
                        Name = kvp.Key,
                        Type = kvp.Value?.GetType().Name ?? "null",
                        Description = kvp.Value?.ToString(),
                        DefaultValue = kvp.Value
                    });
                }
            }

            return result;
        }

        #endregion

        #region Breakpoints

        public void AddBreakpoint(PacketBreakpoint breakpoint)
        {
            lock (_breakpoints)
            {
                _breakpoints.Add(breakpoint);
            }
        }

        public void RemoveBreakpoint(string id)
        {
            lock (_breakpoints)
            {
                _breakpoints.RemoveAll(b => b.Id == id);
            }
        }

        public void ClearBreakpoints()
        {
            lock (_breakpoints)
            {
                _breakpoints.Clear();
            }
        }

        private void CheckBreakpoints(CapturedPacket packet)
        {
            lock (_breakpoints)
            {
                foreach (var bp in _breakpoints.Where(b => b.IsEnabled))
                {
                    if (EvaluateBreakpoint(bp, packet))
                    {
                        BreakpointHit?.Invoke(this, new BreakpointHitEventArgs
                        {
                            Breakpoint = bp,
                            Packet = packet
                        });
                    }
                }
            }
        }

        private bool EvaluateBreakpoint(PacketBreakpoint bp, CapturedPacket packet)
        {
            return bp.Type switch
            {
                BreakpointType.Action => !string.IsNullOrEmpty(packet.Action) && 
                    packet.Action.Contains(bp.Pattern, StringComparison.OrdinalIgnoreCase),
                BreakpointType.Size => packet.Size >= bp.MinSize && packet.Size <= bp.MaxSize,
                BreakpointType.Direction => packet.Direction == bp.Direction,
                BreakpointType.Conditional => EvaluateCondition(bp.Condition, packet),
                _ => false
            };
        }

        private bool EvaluateCondition(string condition, CapturedPacket packet)
        {
            if (string.IsNullOrEmpty(condition)) return false;
            // Simple condition evaluation - can be expanded
            return packet.DecodedPayload?.ToString()?.Contains(condition) == true;
        }

        #endregion

        #region Query Methods

        public IEnumerable<CapturedPacket> GetHistory(int count = 100)
        {
            lock (_historyLock)
            {
                return _packetHistory.TakeLast(count).ToList();
            }
        }

        public IEnumerable<CapturedPacket> SearchHistory(string query)
        {
            lock (_historyLock)
            {
                return _packetHistory
                    .Where(p => p.Action?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                               p.DecodedPayload?.ToString()?.Contains(query) == true)
                    .ToList();
            }
        }

        public IEnumerable<ProtocolAction> GetLearnedActions()
        {
            return _learnedActions.Values.ToList();
        }

        public CapturedPacket? GetPacketById(long id)
        {
            lock (_historyLock)
            {
                return _packetHistory.FirstOrDefault(p => p.Id == id);
            }
        }

        #endregion

        #region Differential Analysis

        public PacketDiff ComparePackets(CapturedPacket packet1, CapturedPacket packet2)
        {
            var diff = new PacketDiff
            {
                Packet1Id = packet1.Id,
                Packet2Id = packet2.Id,
                Differences = new List<FieldDifference>()
            };

            if (packet1.DecodedPayload != null && packet2.DecodedPayload != null)
            {
                CompareObjects(packet1.DecodedPayload, packet2.DecodedPayload, "", diff.Differences);
            }

            diff.SizeDifference = packet2.Size - packet1.Size;
            diff.TimeDifference = packet2.Timestamp - packet1.Timestamp;

            return diff;
        }

        private void CompareObjects(Dictionary<string, object> obj1, Dictionary<string, object> obj2, 
            string path, List<FieldDifference> differences)
        {
            var allKeys = obj1.Keys.Union(obj2.Keys);

            foreach (var key in allKeys)
            {
                var fullPath = string.IsNullOrEmpty(path) ? key : $"{path}.{key}";
                var has1 = obj1.TryGetValue(key, out var val1);
                var has2 = obj2.TryGetValue(key, out var val2);

                if (!has1)
                {
                    differences.Add(new FieldDifference
                    {
                        Path = fullPath,
                        Type = DifferenceType.Added,
                        NewValue = val2?.ToString()
                    });
                }
                else if (!has2)
                {
                    differences.Add(new FieldDifference
                    {
                        Path = fullPath,
                        Type = DifferenceType.Removed,
                        OldValue = val1?.ToString()
                    });
                }
                else if (!Equals(val1, val2))
                {
                    if (val1 is Dictionary<string, object> dict1 && val2 is Dictionary<string, object> dict2)
                    {
                        CompareObjects(dict1, dict2, fullPath, differences);
                    }
                    else
                    {
                        differences.Add(new FieldDifference
                        {
                            Path = fullPath,
                            Type = DifferenceType.Changed,
                            OldValue = val1?.ToString(),
                            NewValue = val2?.ToString()
                        });
                    }
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            StopCapture();
            _analysisCts?.Dispose();
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #region Models

    public class CapturedPacket
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public byte[] RawData { get; set; } = Array.Empty<byte>();
        public PacketDirection Direction { get; set; }
        public string Source { get; set; } = "";
        public int Size { get; set; }
        public bool IsDecoded { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> DecodedPayload { get; set; }
        public string Error { get; set; }
    }

    public enum PacketDirection
    {
        Inbound,
        Outbound
    }

    public class PacketPattern
    {
        public string Name { get; set; } = "";
        public string[] Actions { get; set; } = Array.Empty<string>();
        public string Description { get; set; } = "";
    }

    // ProtocolParameter is defined in ProtocolHandler.cs

    public class PacketBreakpoint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public BreakpointType Type { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string Pattern { get; set; } = "";
        public string Condition { get; set; }
        public int MinSize { get; set; }
        public int MaxSize { get; set; } = int.MaxValue;
        public PacketDirection? Direction { get; set; }
        public BreakpointAction Action { get; set; } = BreakpointAction.Pause;
    }

    public enum BreakpointType
    {
        Action,
        Size,
        Direction,
        Conditional
    }

    public enum BreakpointAction
    {
        Pause,
        Log,
        Notify
    }

    public class PacketDiff
    {
        public long Packet1Id { get; set; }
        public long Packet2Id { get; set; }
        public int SizeDifference { get; set; }
        public TimeSpan TimeDifference { get; set; }
        public List<FieldDifference> Differences { get; set; } = new List<FieldDifference>();
    }

    public class FieldDifference
    {
        public string Path { get; set; } = "";
        public DifferenceType Type { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }

    public enum DifferenceType
    {
        Added,
        Removed,
        Changed
    }

    #endregion

    #region Events

    public class PacketCapturedEventArgs : EventArgs
    {
        public CapturedPacket Packet { get; set; } = null!;
    }

    public class PacketDecodedEventArgs : EventArgs
    {
        public CapturedPacket Packet { get; set; } = null!;
    }

    public class BreakpointHitEventArgs : EventArgs
    {
        public PacketBreakpoint Breakpoint { get; set; } = null!;
        public CapturedPacket Packet { get; set; } = null!;
    }

    public class PatternDetectedEventArgs : EventArgs
    {
        public PacketPattern Pattern { get; set; } = null!;
        public CapturedPacket TriggerPacket { get; set; } = null!;
    }

    public class NewActionDiscoveredEventArgs : EventArgs
    {
        public ProtocolAction Action { get; set; } = null!;
        public CapturedPacket Packet { get; set; } = null!;
    }

    #endregion
}
