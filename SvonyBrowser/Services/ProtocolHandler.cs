using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
    using SvonyBrowser.Helpers;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Handles Evony protocol parsing, encoding, and decoding.
    /// Provides AMF3 codec functionality and protocol action lookup.
    /// </summary>
    public sealed class ProtocolHandler : IDisposable
    {
        #region Singleton

        private static readonly Lazy<ProtocolHandler> _lazyInstance =
            new Lazy<ProtocolHandler>(() => new ProtocolHandler(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        public static ProtocolHandler Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly Dictionary<string, ProtocolAction> _protocolActions = new ProtocolAction>();
        private readonly Dictionary<int, string> _commandIdMap = new string>();
        private bool _disposed = false;
        private bool _initialized = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the protocol handler is initialized.
        /// </summary>
        public bool IsInitialized => _initialized;

        /// <summary>
        /// Gets the number of loaded protocol actions.
        /// </summary>
        public int ActionCount => _protocolActions.Count;

        #endregion

        #region Constructor

        private ProtocolHandler()
        {
            App.Logger.Information("ProtocolHandler initialized");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the protocol handler by loading protocol definitions.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_initialized) return;

            App.Logger.Information("Loading protocol definitions...");

            try
            {
                await LoadProtocolDefinitionsAsync();
                _initialized = true;
                App.Logger.Information("Loaded {Count} protocol actions", _protocolActions.Count);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to load protocol definitions");
                throw;
            }
        }

        /// <summary>
        /// Looks up a protocol action by name.
        /// </summary>
        public ProtocolAction? LookupAction(string actionName)
        {
            if (_protocolActions.TryGetValue(actionName, out var action))
            {
                return action;
            }
            return null;
        }

        /// <summary>
        /// Looks up a protocol action by command ID.
        /// </summary>
        public ProtocolAction? LookupByCommandId(int commandId)
        {
            if (_commandIdMap.TryGetValue(commandId, out var actionName))
            {
                return LookupAction(actionName);
            }
            return null;
        }

        /// <summary>
        /// Searches for protocol actions matching a query.
        /// </summary>
        public IEnumerable<ProtocolAction> SearchActions(string query, int limit = 20)
        {
            var results = new List<ProtocolAction>();
            var lowerQuery = query.ToLowerInvariant();

            foreach (var action in _protocolActions.Values)
            {
                if (action.Name.ToLowerInvariant().Contains(lowerQuery) ||
                    action.Description?.ToLowerInvariant().Contains(lowerQuery) == true ||
                    action.Category?.ToLowerInvariant().Contains(lowerQuery) == true)
                {
                    results.Add(action);
                    if (results.Count >= limit) break;
                }
            }

            return results;
        }

        /// <summary>
        /// Gets all actions in a category.
        /// </summary>
        public IEnumerable<ProtocolAction> GetActionsByCategory(string category)
        {
            foreach (var action in _protocolActions.Values)
            {
                if (string.Equals(action.Category, category, StringComparison.OrdinalIgnoreCase))
                {
                    yield return action;
                }
            }
        }

        /// <summary>
        /// Gets all available categories.
        /// </summary>
        public IEnumerable<string> GetCategories()
        {
            var categories = new HashSet<string>();
            foreach (var action in _protocolActions.Values)
            {
                if (!string.IsNullOrEmpty(action.Category))
                {
                    categories.Add(action.Category);
                }
            }
            return categories;
        }

        /// <summary>
        /// Decodes AMF3 binary data.
        /// </summary>
        public JToken DecodeAMF3(byte[] data)
        {
            try
            {
                var decoder = new AMF3Decoder(data);
                return decoder.Decode();
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "AMF3 decode error");
                return null;
            }
        }
    
        /// <summary>
        /// Decodes AMF3 binary data (alias for DecodeAMF3).
        /// </summary>
        public JToken DecodeAmf3(byte[] data) => DecodeAMF3(data);

        /// <summary>
        /// Encodes data to AMF3 binary format.
        /// </summary>
        public byte[]? EncodeAMF3(JToken data)
        {
            try
            {
                var encoder = new AMF3Encoder();
                return encoder.Encode(data);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "AMF3 encode error");
                return null;
            }
        }

        /// <summary>
        /// Parses a captured packet and identifies the action.
        /// </summary>
        public ParsedPacket? ParsePacket(byte[] data, string direction = "request")
        {
            try
            {
                var decoded = DecodeAMF3(data);
                if (decoded == null) return null;

                var packet = new ParsedPacket
                {
                    Direction = direction,
                    RawData = data,
                    DecodedData = decoded,
                    Timestamp = DateTime.UtcNow
                };

                // Try to identify the action
                if (decoded is JObject obj)
                {
                    // Check for command ID
                    if (obj.TryGetValue("cmd", out var cmdToken))
                    {
                        packet.CommandId = cmdToken.Value<int>();
                        packet.Action = LookupByCommandId(packet.CommandId.Value);
                    }

                    // Check for action name
                    if (obj.TryGetValue("action", out var actionToken))
                    {
                        packet.ActionName = actionToken.Value<string>();
                        packet.Action = LookupAction(packet.ActionName ?? "");
                    }

                    // Extract common fields
                    if (obj.TryGetValue("seq", out var seqToken))
                        packet.SequenceId = seqToken.Value<int>();

                    if (obj.TryGetValue("data", out var dataToken))
                        packet.PayloadData = dataToken;
                }

                return packet;
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Packet parse error");
                return null;
            }
        }

        /// <summary>
        /// Builds a packet for a given action.
        /// </summary>
        public byte[]? BuildPacket(string actionName, JObject parameters)
        {
            var action = LookupAction(actionName);
            if (action == null)
            {
                App.Logger.Warning("Unknown action: {Action}", actionName);
                return null;
            }

            try
            {
                var packet = new JObject
                {
                    ["cmd"] = action.CommandId,
                    ["action"] = actionName,
                    ["seq"] = GenerateSequenceId(),
                    ["data"] = parameters
                };

                return EncodeAMF3(packet);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Packet build error");
                return null;
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadProtocolDefinitionsAsync()
        {
            // Load built-in protocol definitions
            LoadBuiltInProtocols();

            // Try to load from external file
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "protocols.json");
            if (File.Exists(configPath))
            {
                try
                {
                    var json = await FileEx.ReadAllTextAsync(configPath);
                    var protocols = JsonConvert.DeserializeObject<List<ProtocolAction>>(json);
                    if (protocols != null)
                    {
                        foreach (var proto in protocols)
                        {
                            _protocolActions[proto.Name] = proto;
                            if (proto.CommandId > 0)
                            {
                                _commandIdMap[proto.CommandId] = proto.Name;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.Warning(ex, "Failed to load external protocol definitions");
                }
            }
        }

        private void LoadBuiltInProtocols()
        {
            // Hero commands
            AddProtocol("hero.hireHero", 10001, "hero", "Hire a hero from the inn",
                new Dictionary<string, string> { ["heroId"] = "int", ["cityId"] = "int" });
            AddProtocol("hero.fireHero", 10002, "hero", "Fire a hero",
                new Dictionary<string, string> { ["heroId"] = "int" });
            AddProtocol("hero.levelUpHero", 10003, "hero", "Level up a hero",
                new Dictionary<string, string> { ["heroId"] = "int", ["itemId"] = "int" });
            AddProtocol("hero.resetHeroPoint", 10004, "hero", "Reset hero attribute points",
                new Dictionary<string, string> { ["heroId"] = "int" });
            AddProtocol("hero.addHeroPoint", 10005, "hero", "Add attribute points to hero",
                new Dictionary<string, string> { ["heroId"] = "int", ["type"] = "int", ["point"] = "int" });
            AddProtocol("hero.refreshHeroList", 10006, "hero", "Refresh inn hero list",
                new Dictionary<string, string> { ["cityId"] = "int" });
            AddProtocol("hero.getHeroList", 10007, "hero", "Get list of heroes",
                new Dictionary<string, string> { ["cityId"] = "int" });

            // Castle commands
            AddProtocol("castle.upgradeBuilding", 20001, "castle", "Upgrade a building",
                new Dictionary<string, string> { ["cityId"] = "int", ["positionId"] = "int" });
            AddProtocol("castle.destructBuilding", 20002, "castle", "Destroy a building",
                new Dictionary<string, string> { ["cityId"] = "int", ["positionId"] = "int" });
            AddProtocol("castle.newBuilding", 20003, "castle", "Construct a new building",
                new Dictionary<string, string> { ["cityId"] = "int", ["positionId"] = "int", ["buildingType"] = "int" });
            AddProtocol("castle.speedUpBuilding", 20004, "castle", "Speed up building construction",
                new Dictionary<string, string> { ["cityId"] = "int", ["positionId"] = "int", ["itemId"] = "int" });
            AddProtocol("castle.cancelBuilding", 20005, "castle", "Cancel building construction",
                new Dictionary<string, string> { ["cityId"] = "int", ["positionId"] = "int" });
            AddProtocol("castle.getCastleInfo", 20006, "castle", "Get castle information",
                new Dictionary<string, string> { ["cityId"] = "int" });

            // Troop commands
            AddProtocol("troop.produceTroop", 30001, "troop", "Train troops",
                new Dictionary<string, string> { ["cityId"] = "int", ["troopType"] = "int", ["num"] = "int" });
            AddProtocol("troop.cancelTroopProduce", 30002, "troop", "Cancel troop training",
                new Dictionary<string, string> { ["cityId"] = "int", ["queueId"] = "int" });
            AddProtocol("troop.speedUpTroopProduce", 30003, "troop", "Speed up troop training",
                new Dictionary<string, string> { ["cityId"] = "int", ["itemId"] = "int" });
            AddProtocol("troop.getTroopList", 30004, "troop", "Get troop list",
                new Dictionary<string, string> { ["cityId"] = "int" });

            // Army commands
            AddProtocol("army.createArmy", 40001, "army", "Create an army",
                new Dictionary<string, string> { ["cityId"] = "int", ["heroId"] = "int", ["troops"] = "object" });
            AddProtocol("army.disbandArmy", 40002, "army", "Disband an army",
                new Dictionary<string, string> { ["armyId"] = "int" });
            AddProtocol("army.marchArmy", 40003, "army", "March army to target",
                new Dictionary<string, string> { ["armyId"] = "int", ["targetX"] = "int", ["targetY"] = "int", ["missionType"] = "int" });
            AddProtocol("army.callBackArmy", 40004, "army", "Recall marching army",
                new Dictionary<string, string> { ["armyId"] = "int" });
            AddProtocol("army.getArmyList", 40005, "army", "Get army list",
                new Dictionary<string, string> { ["cityId"] = "int" });

            // Tech commands
            AddProtocol("tech.research", 50001, "tech", "Start research",
                new Dictionary<string, string> { ["cityId"] = "int", ["techType"] = "int" });
            AddProtocol("tech.cancelResearch", 50002, "tech", "Cancel research",
                new Dictionary<string, string> { ["cityId"] = "int" });
            AddProtocol("tech.speedUpResearch", 50003, "tech", "Speed up research",
                new Dictionary<string, string> { ["cityId"] = "int", ["itemId"] = "int" });
            AddProtocol("tech.getTechList", 50004, "tech", "Get technology list",
                new Dictionary<string, string> { ["cityId"] = "int" });

            // Alliance commands
            AddProtocol("alliance.createAlliance", 60001, "alliance", "Create an alliance",
                new Dictionary<string, string> { ["name"] = "string", ["description"] = "string" });
            AddProtocol("alliance.joinAlliance", 60002, "alliance", "Join an alliance",
                new Dictionary<string, string> { ["allianceId"] = "int" });
            AddProtocol("alliance.leaveAlliance", 60003, "alliance", "Leave alliance",
                new Dictionary<string, string>());
            AddProtocol("alliance.getAllianceInfo", 60004, "alliance", "Get alliance information",
                new Dictionary<string, string> { ["allianceId"] = "int" });

            // Field commands
            AddProtocol("field.getFieldInfo", 70001, "field", "Get field information",
                new Dictionary<string, string> { ["fieldId"] = "int" });
            AddProtocol("field.searchField", 70002, "field", "Search for fields",
                new Dictionary<string, string> { ["x"] = "int", ["y"] = "int", ["radius"] = "int" });
            AddProtocol("field.occupyField", 70003, "field", "Occupy a field",
                new Dictionary<string, string> { ["fieldId"] = "int", ["armyId"] = "int" });

            // Mail commands
            AddProtocol("mail.sendMail", 80001, "mail", "Send mail",
                new Dictionary<string, string> { ["targetName"] = "string", ["title"] = "string", ["content"] = "string" });
            AddProtocol("mail.getMailList", 80002, "mail", "Get mail list",
                new Dictionary<string, string> { ["type"] = "int", ["page"] = "int" });
            AddProtocol("mail.readMail", 80003, "mail", "Read mail",
                new Dictionary<string, string> { ["mailId"] = "int" });
            AddProtocol("mail.deleteMail", 80004, "mail", "Delete mail",
                new Dictionary<string, string> { ["mailIds"] = "array" });

            // Quest commands
            AddProtocol("quest.getQuestList", 90001, "quest", "Get quest list",
                new Dictionary<string, string>());
            AddProtocol("quest.completeQuest", 90002, "quest", "Complete quest",
                new Dictionary<string, string> { ["questId"] = "int" });
            AddProtocol("quest.claimReward", 90003, "quest", "Claim quest reward",
                new Dictionary<string, string> { ["questId"] = "int" });

            // Shop commands
            AddProtocol("shop.buyItem", 100001, "shop", "Buy item from shop",
                new Dictionary<string, string> { ["itemId"] = "int", ["num"] = "int" });
            AddProtocol("shop.useItem", 100002, "shop", "Use an item",
                new Dictionary<string, string> { ["itemId"] = "int", ["num"] = "int", ["targetId"] = "int" });
            AddProtocol("shop.getItemList", 100003, "shop", "Get item list",
                new Dictionary<string, string>());
        }

        private void AddProtocol(string name, int commandId, string category, string description,
            Dictionary<string, string> parameters)
        {
            var action = new ProtocolAction
            {
                Name = name,
                CommandId = commandId,
                Category = category,
                Description = description,
                Parameters = parameters
            };

            _protocolActions[name] = action;
            _commandIdMap[commandId] = name;
        }

        private int _sequenceCounter = 0;
        private int _packetCount = 0;
        private int _errorCount = 0;
        private double _totalLatency = 0;
        private int _latencyCount = 0;
        private int _successCount = 0;
        private int _decodeAttempts = 0;

        private int GenerateSequenceId()
        {
            return System.Threading.Interlocked.Increment(ref _sequenceCounter);
        }

        #endregion

        #region Statistics Methods

        /// <summary>
        /// Gets the current packet rate (packets per second).
        /// </summary>
        public double GetPacketRate() => _packetCount;

        /// <summary>
        /// Gets the average latency in milliseconds.
        /// </summary>
        public double GetAverageLatency() => _latencyCount > 0 ? _totalLatency / _latencyCount : 0;

        /// <summary>
        /// Gets the decode success rate as a percentage.
        /// </summary>
        public double GetDecodeSuccessRate() => _decodeAttempts > 0 ? (_successCount * 100.0 / _decodeAttempts) : 100;

        /// <summary>
        /// Gets the error count.
        /// </summary>
        public int GetErrorCount() => _errorCount;

        /// <summary>
        /// Gets a protocol action by name.
        /// </summary>
        public ProtocolAction? GetProtocolAction(string name) => _protocolActions.GetValueOrDefault(name);

        /// <summary>
        /// Gets all protocol actions.
        /// </summary>
        public IReadOnlyDictionary<string, ProtocolAction> GetAllProtocols() => _protocolActions;

        /// <summary>
        /// Records a packet for statistics.
        /// </summary>
        public void RecordPacket(bool success, double? latencyMs = null)
        {
            _packetCount++;
            _decodeAttempts++;
            if (success) _successCount++;
            else _errorCount++;
            if (latencyMs.HasValue)
            {
                _totalLatency += latencyMs.Value;
                _latencyCount++;
            }
        }

        /// <summary>
        /// Resets statistics counters.
        /// </summary>
        public void ResetStatistics()
        {
            _packetCount = 0;
            _errorCount = 0;
            _totalLatency = 0;
            _latencyCount = 0;
            _successCount = 0;
            _decodeAttempts = 0;
        }

        /// <summary>
        /// Registers a protocol action from JSON data.
        /// </summary>
        public void RegisterProtocol(string name, JObject data)
        {
            var action = new ProtocolAction
            {
                Name = name,
                CommandId = data["commandId"]?.Value<int>() ?? 0,
                Category = data["category"]?.Value<string>(),
                Description = data["description"]?.Value<string>(),
                Parameters = data["parameters"]?.ToObject<Dictionary<string, string>>() ?? new()
            };
            _protocolActions[name] = action;
            if (action.CommandId > 0)
            {
                _commandIdMap[action.CommandId] = name;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _protocolActions.Clear();
            _commandIdMap.Clear();

            App.Logger.Debug("ProtocolHandler disposed");
        }

        #endregion
    }

    /// <summary>
    /// Represents a protocol action definition.
    /// </summary>
    public class ProtocolAction
    {
        public string Name { get; set; } = "";
        public int CommandId { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new string>();
        public Dictionary<string, string> Response { get; set; }
    
        // Properties for PacketAnalysisEngine learned actions
        public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public int Occurrences { get; set; } = 1;
        public string Direction { get; set; } = "request";
        public List<ProtocolParameter> ParameterList { get; set; } = new List<ProtocolParameter>();
    }

    /// <summary>
    /// Represents a protocol parameter.
    /// </summary>
    public class ProtocolParameter
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "string";
        public string Description { get; set; }
        public bool IsRequired { get; set; }
        public object DefaultValue { get; set; }
    }

    /// <summary>
    /// Represents a protocol category.
    /// </summary>
    public class ProtocolCategory
    {
        public string Name { get; set; } = "";
        public string Description { get; set; }
        public List<ProtocolAction> Actions { get; set; } = new List<ProtocolAction>();
    }

    /// <summary>
    /// Protocol database containing all known actions.
    /// </summary>
    public class ProtocolDatabase
    {
        public string Version { get; set; } = "1.0.0";
        public string LastUpdated { get; set; }
        public List<ProtocolCategory> Categories { get; set; } = new List<ProtocolCategory>();
        public List<ProtocolAction> Actions { get; set; } = new List<ProtocolAction>();
    }

    /// <summary>
    /// Represents a parsed packet.
    /// </summary>
    public class ParsedPacket
    {
        public string Direction { get; set; } = "request";
        public byte[]? RawData { get; set; }
        public JToken DecodedData { get; set; }
        public DateTime Timestamp { get; set; }
        public int? CommandId { get; set; }
        public string ActionName { get; set; }
        public int? SequenceId { get; set; }
        public JToken PayloadData { get; set; }
        public ProtocolAction? Action { get; set; }
    }

    /// <summary>
    /// AMF3 decoder implementation.
    /// </summary>
    public class AMF3Decoder
    {
        private readonly byte[] _data;
        private int _pos;
        private readonly List<string> _stringRefs = new List<string>();
        private readonly List<object> _objectRefs = new List<object>();
        private readonly List<AMF3Traits> _traitRefs = new List<AMF3Traits>();

        public AMF3Decoder(byte[] data)
        {
            _data = data;
            _pos = 0;
        }

        public JToken Decode()
        {
            return ReadValue();
        }

        private byte ReadByte()
        {
            if (_pos >= _data.Length)
                throw new InvalidOperationException("Unexpected end of data");
            return _data[_pos++];
        }

        private byte[] ReadBytes(int count)
        {
            if (_pos + count > _data.Length)
                throw new InvalidOperationException("Unexpected end of data");
            var bytes = new byte[count];
            Array.Copy(_data, _pos, bytes, 0, count);
            _pos += count;
            return bytes;
        }

        private int ReadU29()
        {
            int result = 0;
            for (int i = 0; i < 4; i++)
            {
                byte b = ReadByte();
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

        private double ReadDouble()
        {
            var bytes = ReadBytes(8);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }

        private string ReadString()
        {
            int refOrLen = ReadU29();
            if ((refOrLen & 1) == 0)
                return _stringRefs[refOrLen >> 1];

            int length = refOrLen >> 1;
            if (length == 0) return "";

            var bytes = ReadBytes(length);
            var str = Encoding.UTF8.GetString(bytes);
            _stringRefs.Add(str);
            return str;
        }

        private JToken ReadValue()
        {
            byte type = ReadByte();

            return type switch
            {
                0x00 => JValue.CreateUndefined(),
                0x01 => JValue.CreateNull(),
                0x02 => new JValue(false),
                0x03 => new JValue(true),
                0x04 => new JValue(ReadU29()),
                0x05 => new JValue(ReadDouble()),
                0x06 => new JValue(ReadString()),
                0x08 => ReadDate(),
                0x09 => ReadArray(),
                0x0A => ReadObject(),
                0x0C => ReadByteArray(),
                _ => throw new InvalidOperationException($"Unknown AMF3 type: {type}")
            };
        }

        private JToken ReadDate()
        {
            int refOrFlag = ReadU29();
            if ((refOrFlag & 1) == 0)
                return (JToken)_objectRefs[refOrFlag >> 1]!;

            double timestamp = ReadDouble();
            var date = new JValue(DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp).DateTime);
            _objectRefs.Add(date);
            return date;
        }

        private JArray ReadArray()
        {
            int refOrLen = ReadU29();
            if ((refOrLen & 1) == 0)
                return (JArray)_objectRefs[refOrLen >> 1]!;

            int length = refOrLen >> 1;
            var array = new JArray();
            _objectRefs.Add(array);

            // Read associative portion
            string key = ReadString();
            while (!string.IsNullOrEmpty(key))
            {
                array.Add(new JProperty(key, ReadValue()));
                key = ReadString();
            }

            // Read dense portion
            for (int i = 0; i < length; i++)
            {
                array.Add(ReadValue());
            }

            return array;
        }

        private JObject ReadObject()
        {
            int refOrFlag = ReadU29();
            if ((refOrFlag & 1) == 0)
                return (JObject)_objectRefs[refOrFlag >> 1]!;

            AMF3Traits traits;
            if ((refOrFlag & 2) == 0)
            {
                traits = _traitRefs[refOrFlag >> 2];
            }
            else
            {
                traits = new AMF3Traits
                {
                    ClassName = ReadString(),
                    Dynamic = (refOrFlag & 4) != 0,
                    Externalizable = (refOrFlag & 8) != 0
                };

                int propCount = refOrFlag >> 4;
                for (int i = 0; i < propCount; i++)
                {
                    traits.Properties.Add(ReadString());
                }

                _traitRefs.Add(traits);
            }

            var obj = new JObject();
            if (!string.IsNullOrEmpty(traits.ClassName))
            {
                obj["__class__"] = traits.ClassName;
            }
            _objectRefs.Add(obj);

            if (traits.Externalizable)
            {
                obj["__externalized__"] = ReadValue();
            }
            else
            {
                foreach (var prop in traits.Properties)
                {
                    obj[prop] = ReadValue();
                }

                if (traits.Dynamic)
                {
                    string key = ReadString();
                    while (!string.IsNullOrEmpty(key))
                    {
                        obj[key] = ReadValue();
                        key = ReadString();
                    }
                }
            }

            return obj;
        }

        private JToken ReadByteArray()
        {
            int refOrLen = ReadU29();
            if ((refOrLen & 1) == 0)
                return (JToken)_objectRefs[refOrLen >> 1]!;

            int length = refOrLen >> 1;
            var bytes = ReadBytes(length);
            var hex = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

            var result = new JObject
            {
                ["__type__"] = "ByteArray",
                ["length"] = length,
                ["hex"] = hex
            };
            _objectRefs.Add(result);
            return result;
        }
    }

    /// <summary>
    /// AMF3 traits for object serialization.
    /// </summary>
    public class AMF3Traits
    {
        public string ClassName { get; set; } = "";
        public bool Dynamic { get; set; }
        public bool Externalizable { get; set; }
        public List<string> Properties { get; set; } = new List<string>();
    }

    /// <summary>
    /// AMF3 encoder implementation.
    /// </summary>
    public class AMF3Encoder
    {
        private readonly MemoryStream _stream = new MemoryStream();
        private readonly Dictionary<string, int> _stringRefs = new int>();
        private readonly Dictionary<object, int> _objectRefs = new int>();

        public byte[] Encode(JToken value)
        {
            WriteValue(value);
            return _stream.ToArray();
        }

        private void WriteByte(byte b)
        {
            _stream.WriteByte(b);
        }

        private void WriteBytes(byte[] bytes)
        {
            _stream.Write(bytes, 0, bytes.Length);
        }

        private void WriteU29(int value)
        {
            if (value < 0x80)
            {
                WriteByte((byte)value);
            }
            else if (value < 0x4000)
            {
                WriteByte((byte)((value >> 7) | 0x80));
                WriteByte((byte)(value & 0x7F));
            }
            else if (value < 0x200000)
            {
                WriteByte((byte)((value >> 14) | 0x80));
                WriteByte((byte)((value >> 7) | 0x80));
                WriteByte((byte)(value & 0x7F));
            }
            else
            {
                WriteByte((byte)((value >> 22) | 0x80));
                WriteByte((byte)((value >> 15) | 0x80));
                WriteByte((byte)((value >> 8) | 0x80));
                WriteByte((byte)(value & 0xFF));
            }
        }

        private void WriteDouble(double value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            WriteBytes(bytes);
        }

        private void WriteString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                WriteU29(1); // Empty string
                return;
            }

            if (_stringRefs.TryGetValue(value, out int refIndex))
            {
                WriteU29(refIndex << 1);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            WriteU29((bytes.Length << 1) | 1);
            WriteBytes(bytes);
            _stringRefs[value] = _stringRefs.Count;
        }

        private void WriteValue(JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.Undefined:
                    WriteByte(0x00);
                    break;

                case JTokenType.Null:
                    WriteByte(0x01);
                    break;

                case JTokenType.Boolean:
                    WriteByte(value.Value<bool>() ? (byte)0x03 : (byte)0x02);
                    break;

                case JTokenType.Integer:
                    WriteByte(0x04);
                    WriteU29(value.Value<int>());
                    break;

                case JTokenType.Float:
                    WriteByte(0x05);
                    WriteDouble(value.Value<double>());
                    break;

                case JTokenType.String:
                    WriteByte(0x06);
                    WriteString(value.Value<string>() ?? "");
                    break;

                case JTokenType.Array:
                    WriteArray((JArray)value);
                    break;

                case JTokenType.Object:
                    WriteObject((JObject)value);
                    break;

                default:
                    WriteByte(0x01); // Null for unsupported types
                    break;
            }
        }

        private void WriteArray(JArray array)
        {
            WriteByte(0x09);
            WriteU29((array.Count << 1) | 1);
            WriteString(""); // Empty associative portion

            foreach (var item in array)
            {
                WriteValue(item);
            }
        }

        private void WriteObject(JObject obj)
        {
            WriteByte(0x0A);

            // Inline traits, dynamic, not externalizable
            var props = new List<string>();
            foreach (var prop in obj.Properties())
            {
                if (!prop.Name.StartsWith("__"))
                {
                    props.Add(prop.Name);
                }
            }

            int flags = (props.Count << 4) | 0x0B; // Dynamic + inline traits
            WriteU29(flags);
            WriteString(""); // No class name

            // Write property names
            foreach (var prop in props)
            {
                WriteString(prop);
            }

            // Write property values
            foreach (var prop in props)
            {
                WriteValue(obj[prop]!);
            }

            WriteString(""); // End dynamic properties
        }
    }

    // Extension methods for packet sending
    public static class ProtocolHandlerExtensions
    {
        /// <summary>
        /// Sends a packet with the given action and parameters.
        /// This is a placeholder - actual implementation depends on network layer.
        /// </summary>
        public static void SendPacket(this ProtocolHandler handler, string action, JObject parameters)
        {
            var packet = handler.BuildPacket(action, parameters);
            if (packet != null)
            {
                // TODO: Send packet through network layer
                App.Logger.Information("Packet built for action: {Action}", action);
            }
        }
    }

}