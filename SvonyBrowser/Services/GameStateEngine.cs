using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
    using SvonyBrowser.Helpers;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Real-time game state synchronization engine that maintains a complete
    /// in-memory model of the player's game state, updated from intercepted traffic.
    /// </summary>
    public sealed class GameStateEngine : IDisposable
    {
        #region Singleton

        private static readonly Lazy<GameStateEngine> _lazyInstance =
            new Lazy<GameStateEngine>(() => new GameStateEngine(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static GameStateEngine Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly ConcurrentDictionary<int, CityState> _cities = new Dictionary<int, CityState>();
        private readonly ConcurrentDictionary<int, HeroState> _heroes = new Dictionary<int, HeroState>();
        private readonly ConcurrentDictionary<int, ArmyState> _armies = new Dictionary<int, ArmyState>();
        private readonly ConcurrentDictionary<int, MarchState> _marches = new Dictionary<int, MarchState>();
        private readonly List<GameEvent> _eventHistory = new List<GameEvent>();
        private readonly object _historyLock = new object();
        private readonly Timer _updateTimer;
        private bool _disposed = false;

        private PlayerState _player = new PlayerState();
        private AllianceState _alliance = new AllianceState();
        private DateTime _lastUpdate = DateTime.UtcNow;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current player state.
        /// </summary>
        public PlayerState Player => _player;

        /// <summary>
        /// Gets the current alliance state.
        /// </summary>
        public AllianceState Alliance => _alliance;

        /// <summary>
        /// Gets all cities.
        /// </summary>
        public IReadOnlyDictionary<int, CityState> Cities => _cities;

        /// <summary>
        /// Gets all heroes.
        /// </summary>
        public IReadOnlyDictionary<int, HeroState> Heroes => _heroes;

        /// <summary>
        /// Gets all armies.
        /// </summary>
        public IReadOnlyDictionary<int, ArmyState> Armies => _armies;

        /// <summary>
        /// Gets all active marches.
        /// </summary>
        public IReadOnlyDictionary<int, MarchState> Marches => _marches;

        /// <summary>
        /// Gets the last update timestamp.
        /// </summary>
        public DateTime LastUpdate => _lastUpdate;

        /// <summary>
        /// Gets whether the state is considered fresh (updated within 30 seconds).
        /// </summary>
        public bool IsFresh => (DateTime.UtcNow - _lastUpdate).TotalSeconds < 30;

        #endregion

        #region Events

        /// <summary>
        /// Fired when any state changes.
        /// </summary>
        public event Action<GameStateChange> StateChanged;

        /// <summary>
        /// Fired when resources are updated.
        /// </summary>
        public event Action<int, ResourceState> ResourcesUpdated;

        /// <summary>
        /// Fired when a building completes.
        /// </summary>
        public event Action<int, BuildingState> BuildingCompleted;

        /// <summary>
        /// Fired when a hero levels up.
        /// </summary>
        public event Action<HeroState> HeroLeveledUp;

        /// <summary>
        /// Fired when an army arrives.
        /// </summary>
        public event Action<MarchState> ArmyArrived;

        /// <summary>
        /// Fired when under attack.
        /// </summary>
        public event Action<AttackAlert> UnderAttack;

        /// <summary>
        /// Fired when training completes.
        /// </summary>
        public event Action<int, string, int> TrainingCompleted;

        #endregion

        #region Constructor

        private GameStateEngine()
        {
            // Update timer for calculated values (resource production, march ETAs)
            _updateTimer = new Timer(UpdateCalculatedValues, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        
            // Subscribe to traffic events
            TrafficPipeClient.Instance.PacketReceived += OnPacketReceived;
        
            App.Logger.Information("GameStateEngine initialized");
        }

        /// <summary>
        /// Internal constructor for state reconstruction (no event subscription).
        /// </summary>
        internal GameStateEngine(bool noSubscription)
        {
            // No timer or event subscription for reconstruction
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a complete snapshot of the current game state.
        /// </summary>
        public GameStateSnapshot GetSnapshot()
        {
            return new GameStateSnapshot
            {
                Timestamp = DateTime.UtcNow,
                Player = _player.Clone(),
                Alliance = _alliance.Clone(),
                Cities = _cities.Values.Select(c => c.Clone()).ToList(),
                Heroes = _heroes.Values.Select(h => h.Clone()).ToList(),
                Armies = _armies.Values.Select(a => a.Clone()).ToList(),
                Marches = _marches.Values.Select(m => m.Clone()).ToList()
            };
        }

        /// <summary>
        /// Gets the state history for a specific entity.
        /// </summary>
        public List<GameEvent> GetHistory(string entityType, int? entityId = null, int limit = 100)
        {
            lock (_historyLock)
            {
                var query = _eventHistory.AsEnumerable();
            
                if (!string.IsNullOrEmpty(entityType))
                    query = query.Where(e => e.EntityType == entityType);
            
                if (entityId.HasValue)
                    query = query.Where(e => e.EntityId == entityId.Value);
            
                return query.OrderByDescending(e => e.Timestamp).Take(limit).ToList();
            }
        }

        /// <summary>
        /// Calculates total resources across all cities.
        /// </summary>
        public ResourceState GetTotalResources()
        {
            var total = new ResourceState();
            foreach (var city in _cities.Values)
            {
                total.Gold += city.Resources.Gold;
                total.Food += city.Resources.Food;
                total.Lumber += city.Resources.Lumber;
                total.Stone += city.Resources.Stone;
                total.Iron += city.Resources.Iron;
            }
            return total;
        }

        /// <summary>
        /// Calculates total troops across all cities.
        /// </summary>
        public Dictionary<string, int> GetTotalTroops()
        {
            var total = new Dictionary<string, int>();
            foreach (var city in _cities.Values)
            {
                foreach (var troop in city.Troops)
                {
                    if (!total.ContainsKey(troop.Key))
                        total[troop.Key] = 0;
                    total[troop.Key] += troop.Value;
                }
            }
            return total;
        }

        /// <summary>
        /// Gets resource production rates per hour.
        /// </summary>
        public ResourceState GetProductionRates()
        {
            var rates = new ResourceState();
            foreach (var city in _cities.Values)
            {
                rates.Gold += city.ProductionRates.Gold;
                rates.Food += city.ProductionRates.Food;
                rates.Lumber += city.ProductionRates.Lumber;
                rates.Stone += city.ProductionRates.Stone;
                rates.Iron += city.ProductionRates.Iron;
            }
            return rates;
        }

        /// <summary>
        /// Processes a decoded packet and updates state.
        /// </summary>
        public void ProcessPacket(string action, JToken data, bool isResponse)
        {
            try
            {
                _lastUpdate = DateTime.UtcNow;

                // Route to appropriate handler based on action
                var category = action.Split('.')[0];
            
                switch (category)
                {
                    case "castle":
                        ProcessCastlePacket(action, data, isResponse);
                        break;
                    case "hero":
                        ProcessHeroPacket(action, data, isResponse);
                        break;
                    case "troop":
                        ProcessTroopPacket(action, data, isResponse);
                        break;
                    case "army":
                        ProcessArmyPacket(action, data, isResponse);
                        break;
                    case "march":
                        ProcessMarchPacket(action, data, isResponse);
                        break;
                    case "alliance":
                        ProcessAlliancePacket(action, data, isResponse);
                        break;
                    case "player":
                        ProcessPlayerPacket(action, data, isResponse);
                        break;
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error processing packet: {Action}", action);
            }
        }

        /// <summary>
        /// Manually updates a city's state.
        /// </summary>
        public void UpdateCity(CityState city)
        {
            var oldState = _cities.GetValueOrDefault(city.CityId);
            _cities[city.CityId] = city;
        
            RecordEvent("city", city.CityId, "updated", oldState, city);
            StateChanged?.Invoke(new GameStateChange("city", city.CityId, "updated"));
        }

        /// <summary>
        /// Clears all state data.
        /// </summary>
        public void ClearState()
        {
            _cities.Clear();
            _heroes.Clear();
            _armies.Clear();
            _marches.Clear();
            _player = new PlayerState();
            _alliance = new AllianceState();
        
            lock (_historyLock)
            {
                _eventHistory.Clear();
            }
        
            App.Logger.Information("Game state cleared");
        }

        #endregion

        #region Private Methods - Packet Processing

        private void OnPacketReceived(FiddlerTrafficData data)
        {
            if (data.Decoded != null && !string.IsNullOrEmpty(data.Action))
            {
                var decoded = data.Decoded is JToken jt ? jt : JToken.FromObject(data.Decoded);
                ProcessPacket(data.Action, decoded, data.Direction == "response");
            }
        }

        private void ProcessCastlePacket(string action, JToken data, bool isResponse)
        {
            if (!isResponse) return;

            switch (action)
            {
                case "castle.getCastleInfo":
                case "castle.getFullCastleInfo":
                    UpdateCityFromPacket(data);
                    break;
                case "castle.upgradeBuilding":
                    UpdateBuildingQueue(data);
                    break;
                case "castle.collectResource":
                case "castle.collectAllResources":
                    UpdateResources(data);
                    break;
            }
        }

        private void ProcessHeroPacket(string action, JToken data, bool isResponse)
        {
            if (!isResponse) return;

            switch (action)
            {
                case "hero.getHeroList":
                case "hero.getAllHeroLevel":
                    UpdateHeroList(data);
                    break;
                case "hero.hireHero":
                    AddHero(data);
                    break;
                case "hero.levelUpHero":
                    UpdateHeroLevel(data);
                    break;
            }
        }

        private void ProcessTroopPacket(string action, JToken data, bool isResponse)
        {
            if (!isResponse) return;

            switch (action)
            {
                case "troop.getTroopList":
                    UpdateTroopList(data);
                    break;
                case "troop.trainTroop":
                    UpdateTrainingQueue(data);
                    break;
            }
        }

        private void ProcessArmyPacket(string action, JToken data, bool isResponse)
        {
            if (!isResponse) return;

            switch (action)
            {
                case "army.getArmyList":
                    UpdateArmyList(data);
                    break;
                case "army.createArmy":
                    AddArmy(data);
                    break;
                case "army.disbandArmy":
                    RemoveArmy(data);
                    break;
            }
        }

        private void ProcessMarchPacket(string action, JToken data, bool isResponse)
        {
            switch (action)
            {
                case "march.startMarch":
                    if (isResponse) AddMarch(data);
                    break;
                case "march.recallMarch":
                    if (isResponse) UpdateMarchRecall(data);
                    break;
                case "march.marchArrived":
                    CompleteMarch(data);
                    break;
            }

            // Check for incoming attacks
            if (action == "march.incomingAttack" || action == "march.underAttack")
            {
                ProcessIncomingAttack(data);
            }
        }

        private void ProcessAlliancePacket(string action, JToken data, bool isResponse)
        {
            if (!isResponse) return;

            switch (action)
            {
                case "alliance.getAllianceInfo":
                    UpdateAllianceInfo(data);
                    break;
                case "alliance.getMemberList":
                    UpdateAllianceMembers(data);
                    break;
            }
        }

        private void ProcessPlayerPacket(string action, JToken data, bool isResponse)
        {
            if (!isResponse) return;

            switch (action)
            {
                case "player.getPlayerInfo":
                    UpdatePlayerInfo(data);
                    break;
            }
        }

        #endregion

        #region Private Methods - State Updates

        private void UpdateCityFromPacket(JToken data)
        {
            var cityId = data["cityId"]?.Value<int>() ?? 0;
            if (cityId == 0) return;

            var city = _cities.GetOrAdd(cityId, _ => new CityState { CityId = cityId });
        
            city.Name = data["name"]?.Value<string>() ?? city.Name;
            city.Level = data["level"]?.Value<int>() ?? city.Level;
            city.X = data["x"]?.Value<int>() ?? city.X;
            city.Y = data["y"]?.Value<int>() ?? city.Y;

            // Update resources
            if (data["resources"] is JObject resources)
            {
                city.Resources.Gold = resources["gold"]?.Value<long>() ?? city.Resources.Gold;
                city.Resources.Food = resources["food"]?.Value<long>() ?? city.Resources.Food;
                city.Resources.Lumber = resources["lumber"]?.Value<long>() ?? city.Resources.Lumber;
                city.Resources.Stone = resources["stone"]?.Value<long>() ?? city.Resources.Stone;
                city.Resources.Iron = resources["iron"]?.Value<long>() ?? city.Resources.Iron;
            }

            // Update buildings
            if (data["buildings"] is JArray buildings)
            {
                city.Buildings.Clear();
                foreach (var b in buildings)
                {
                    var building = new BuildingState
                    {
                        PositionId = b["positionId"]?.Value<int>() ?? 0,
                        TypeId = b["typeId"]?.Value<int>() ?? 0,
                        Level = b["level"]?.Value<int>() ?? 0,
                        Name = b["name"]?.Value<string>() ?? ""
                    };
                    city.Buildings[building.PositionId] = building;
                }
            }

            city.LastUpdated = DateTime.UtcNow;
            ResourcesUpdated?.Invoke(cityId, city.Resources);
            StateChanged?.Invoke(new GameStateChange("city", cityId, "updated"));
        }

        private void UpdateHeroList(JToken data)
        {
            if (data["heroes"] is not JArray heroes) return;

            foreach (var h in heroes)
            {
                var heroId = h["heroId"]?.Value<int>() ?? 0;
                if (heroId == 0) continue;

                var hero = _heroes.GetOrAdd(heroId, _ => new HeroState { HeroId = heroId });
            
                hero.Name = h["name"]?.Value<string>() ?? hero.Name;
                hero.Level = h["level"]?.Value<int>() ?? hero.Level;
                hero.Experience = h["exp"]?.Value<long>() ?? hero.Experience;
                hero.Politics = h["politics"]?.Value<int>() ?? hero.Politics;
                hero.Attack = h["attack"]?.Value<int>() ?? hero.Attack;
                hero.Intelligence = h["intelligence"]?.Value<int>() ?? hero.Intelligence;
                hero.CityId = h["cityId"]?.Value<int>() ?? hero.CityId;
                hero.Status = h["status"]?.Value<string>() ?? hero.Status;
                hero.LastUpdated = DateTime.UtcNow;
            }

            StateChanged?.Invoke(new GameStateChange("hero", 0, "list_updated"));
        }

        private void UpdateHeroLevel(JToken data)
        {
            var heroId = data["heroId"]?.Value<int>() ?? 0;
            if (heroId == 0 || !_heroes.TryGetValue(heroId, out var hero)) return;

            var oldLevel = hero.Level;
            hero.Level = data["newLevel"]?.Value<int>() ?? hero.Level;
            hero.Experience = data["exp"]?.Value<long>() ?? hero.Experience;
            hero.LastUpdated = DateTime.UtcNow;

            if (hero.Level > oldLevel)
            {
                HeroLeveledUp?.Invoke(hero);
                RecordEvent("hero", heroId, "leveled_up", oldLevel, hero.Level);
            }
        }

        private void AddMarch(JToken data)
        {
            var marchId = data["marchId"]?.Value<int>() ?? 0;
            if (marchId == 0) return;

            var march = new MarchState
            {
                MarchId = marchId,
                ArmyId = data["armyId"]?.Value<int>() ?? 0,
                SourceCityId = data["sourceCityId"]?.Value<int>() ?? 0,
                TargetX = data["targetX"]?.Value<int>() ?? 0,
                TargetY = data["targetY"]?.Value<int>() ?? 0,
                MissionType = data["missionType"]?.Value<string>() ?? "attack",
                StartTime = DateTime.UtcNow,
                ArrivalTime = DateTime.UtcNow.AddSeconds(data["duration"]?.Value<int>() ?? 0),
                Status = "marching"
            };

            _marches[marchId] = march;
            StateChanged?.Invoke(new GameStateChange("march", marchId, "started"));
        }

        private void CompleteMarch(JToken data)
        {
            var marchId = data["marchId"]?.Value<int>() ?? 0;
            if (marchId == 0 || !_marches.TryGetValue(marchId, out var march)) return;

            march.Status = "arrived";
            march.ArrivalTime = DateTime.UtcNow;

            ArmyArrived?.Invoke(march);
            RecordEvent("march", marchId, "arrived", null, march);

            // Remove completed march after a delay
            Task.Delay(5000).ContinueWith(_ => { _marches.TryRemove(marchId, out MarchState? _); });
        }

        private void ProcessIncomingAttack(JToken data)
        {
            var alert = new AttackAlert
            {
                AttackerId = data["attackerId"]?.Value<int>() ?? 0,
                AttackerName = data["attackerName"]?.Value<string>() ?? "Unknown",
                TargetCityId = data["targetCityId"]?.Value<int>() ?? 0,
                ArrivalTime = DateTime.UtcNow.AddSeconds(data["eta"]?.Value<int>() ?? 0),
                TroopPower = data["power"]?.Value<long>() ?? 0
            };

            UnderAttack?.Invoke(alert);
            RecordEvent("attack", alert.TargetCityId, "incoming", null, alert);
        }

        private void UpdateResources(JToken data)
        {
            var cityId = data["cityId"]?.Value<int>() ?? 0;
            if (cityId == 0 || !_cities.TryGetValue(cityId, out var city)) return;

            if (data["resources"] is JObject resources)
            {
                city.Resources.Gold = resources["gold"]?.Value<long>() ?? city.Resources.Gold;
                city.Resources.Food = resources["food"]?.Value<long>() ?? city.Resources.Food;
                city.Resources.Lumber = resources["lumber"]?.Value<long>() ?? city.Resources.Lumber;
                city.Resources.Stone = resources["stone"]?.Value<long>() ?? city.Resources.Stone;
                city.Resources.Iron = resources["iron"]?.Value<long>() ?? city.Resources.Iron;
            }

            city.LastUpdated = DateTime.UtcNow;
            ResourcesUpdated?.Invoke(cityId, city.Resources);
        }

        private void UpdateBuildingQueue(JToken data) { /* Implementation */ }
        private void UpdateTroopList(JToken data) { /* Implementation */ }
        private void UpdateTrainingQueue(JToken data) { /* Implementation */ }
        private void UpdateArmyList(JToken data) { /* Implementation */ }
        private void AddArmy(JToken data) { /* Implementation */ }
        private void RemoveArmy(JToken data) { /* Implementation */ }
        private void UpdateMarchRecall(JToken data) { /* Implementation */ }
        private void AddHero(JToken data) { /* Implementation */ }
        private void UpdateAllianceInfo(JToken data) { /* Implementation */ }
        private void UpdateAllianceMembers(JToken data) { /* Implementation */ }
        private void UpdatePlayerInfo(JToken data) { /* Implementation */ }

        #endregion

        #region Private Methods - Utilities

        private void UpdateCalculatedValues(object state)
        {
            if (_disposed) return;

            // Update resource projections based on production rates
            foreach (var city in _cities.Values)
            {
                var elapsed = (DateTime.UtcNow - city.LastUpdated).TotalHours;
                if (elapsed > 0 && elapsed < 1)
                {
                    // Project current resources based on production rates
                    city.ProjectedResources.Gold = city.Resources.Gold + (long)(city.ProductionRates.Gold * elapsed);
                    city.ProjectedResources.Food = city.Resources.Food + (long)(city.ProductionRates.Food * elapsed);
                    city.ProjectedResources.Lumber = city.Resources.Lumber + (long)(city.ProductionRates.Lumber * elapsed);
                    city.ProjectedResources.Stone = city.Resources.Stone + (long)(city.ProductionRates.Stone * elapsed);
                    city.ProjectedResources.Iron = city.Resources.Iron + (long)(city.ProductionRates.Iron * elapsed);
                }
            }

            // Update march ETAs
            foreach (var march in _marches.Values)
            {
                march.TimeRemaining = march.ArrivalTime - DateTime.UtcNow;
                if (march.TimeRemaining < TimeSpan.Zero)
                    march.TimeRemaining = TimeSpan.Zero;
            }
        }

        private void RecordEvent(string entityType, int entityId, string eventType, object oldValue, object newValue)
        {
            var evt = new GameEvent
            {
                Timestamp = DateTime.UtcNow,
                EntityType = entityType,
                EntityId = entityId,
                EventType = eventType,
                OldValue = oldValue != null ? JsonConvert.SerializeObject(oldValue) : null,
                NewValue = newValue != null ? JsonConvert.SerializeObject(newValue) : null
            };

            lock (_historyLock)
            {
                _eventHistory.Add(evt);
            
                // Keep only last 10000 events
                if (_eventHistory.Count > 10000)
                {
                    _eventHistory.RemoveRange(0, _eventHistory.Count - 10000);
                }
            }
        }

        #endregion

        #region Progress Methods

        /// <summary>
        /// Gets the current build progress as a percentage.
        /// </summary>
        public ProgressInfo GetBuildProgress()
        {
            var city = _cities.Values.FirstOrDefault();
            if (city?.BuildQueue?.Count > 0)
            {
                var queue = city.BuildQueue[0];
                var total = (queue.EndTime - queue.StartTime).TotalSeconds;
                var remaining = (queue.EndTime - DateTime.UtcNow).TotalSeconds;
                var percentage = total > 0 ? Math.Max(0, Math.Min(100, (1 - remaining / total) * 100)) : 100;
                return new ProgressInfo { Percentage = percentage, TimeRemaining = TimeSpan.FromSeconds(Math.Max(0, remaining)) };
            }
            return new ProgressInfo { Percentage = 100, TimeRemaining = TimeSpan.Zero };
        }

        /// <summary>
        /// Gets the current training progress as a percentage.
        /// </summary>
        public ProgressInfo GetTrainProgress()
        {
            var city = _cities.Values.FirstOrDefault();
            if (city?.TrainingQueue?.Count > 0)
            {
                var queue = city.TrainingQueue[0];
                var total = (queue.EndTime - queue.StartTime).TotalSeconds;
                var remaining = (queue.EndTime - DateTime.UtcNow).TotalSeconds;
                var percentage = total > 0 ? Math.Max(0, Math.Min(100, (1 - remaining / total) * 100)) : 100;
                return new ProgressInfo { Percentage = percentage, TimeRemaining = TimeSpan.FromSeconds(Math.Max(0, remaining)) };
            }
            return new ProgressInfo { Percentage = 100, TimeRemaining = TimeSpan.Zero };
        }

        /// <summary>
        /// Gets the current research progress as a percentage.
        /// </summary>
        public ProgressInfo GetResearchProgress()
        {
            var city = _cities.Values.FirstOrDefault();
            if (city?.ResearchQueue?.Count > 0)
            {
                var queue = city.ResearchQueue[0];
                var total = (queue.EndTime - queue.StartTime).TotalSeconds;
                var remaining = (queue.EndTime - DateTime.UtcNow).TotalSeconds;
                var percentage = total > 0 ? Math.Max(0, Math.Min(100, (1 - remaining / total) * 100)) : 100;
                return new ProgressInfo { Percentage = percentage, TimeRemaining = TimeSpan.FromSeconds(Math.Max(0, remaining)) };
            }
            return new ProgressInfo { Percentage = 100, TimeRemaining = TimeSpan.Zero };
        }

        /// <summary>
        /// Gets the list of active marches.
        /// </summary>
        public List<MarchState> GetActiveMarches() => _marches.Values.ToList();

        /// <summary>
        /// Gets the maximum number of marches allowed.
        /// </summary>
        public int GetMaxMarches() => _player.MaxMarches > 0 ? _player.MaxMarches : 5;

        /// <summary>
        /// Gets the resource production rates.
        /// </summary>
        public ResourceState GetResourceRates() => _cities.Values.FirstOrDefault()?.ProductionRates ?? new ResourceState();

        /// <summary>
        /// Gets the total hero stamina.
        /// </summary>
        public int GetHeroStamina() => _heroes.Values.Sum(h => h.Stamina);

        /// <summary>
        /// Gets the maximum hero stamina.
        /// </summary>
        public int GetMaxStamina() => _heroes.Values.Sum(h => h.MaxStamina);

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _updateTimer?.Dispose();
            TrafficPipeClient.Instance.PacketReceived -= OnPacketReceived;

            App.Logger.Information("GameStateEngine disposed");
        }

        #endregion
    }

    #region State Models

    /// <summary>
    /// Complete game state snapshot.
    /// </summary>
    public class GameStateSnapshot
    {
        public DateTime Timestamp { get; set; }
        public PlayerState Player { get; set; } = new PlayerState();
        public AllianceState Alliance { get; set; } = new AllianceState();
        public List<CityState> Cities { get; set; } = new List<CityState>();
        public List<HeroState> Heroes { get; set; } = new List<HeroState>();
        public List<ArmyState> Armies { get; set; } = new List<ArmyState>();
        public List<MarchState> Marches { get; set; } = new List<MarchState>();
    }

    /// <summary>
    /// Player state.
    /// </summary>
    public class PlayerState
    {
        public int PlayerId { get; set; }
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public long Power { get; set; }
        public int Prestige { get; set; }
        public int Honor { get; set; }
        public int Gems { get; set; }
        public int MaxMarches { get; set; } = 5;
        public int VipLevel { get; set; }
        public string AllianceName { get; set; } = "";
        public string AllianceTag { get; set; } = "";
        public DateTime LastUpdated { get; set; }

        public PlayerState Clone() => (PlayerState)MemberwiseClone();
    }

    /// <summary>
    /// Alliance state.
    /// </summary>
    public class AllianceState
    {
        public int AllianceId { get; set; }
        public string Name { get; set; } = "";
        public string Tag { get; set; } = "";
        public int Level { get; set; }
        public int MemberCount { get; set; }
        public long TotalPower { get; set; }
        public string LeaderName { get; set; } = "";
        public DateTime LastUpdated { get; set; }

        public AllianceState Clone() => (AllianceState)MemberwiseClone();
    }

    /// <summary>
    /// City state.
    /// </summary>
    public class CityState
    {
        public int CityId { get; set; }
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public int KeepLevel { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public ResourceState Resources { get; set; } = new ResourceState();
        public ResourceState ProductionRates { get; set; } = new ResourceState();
        public ResourceState ProjectedResources { get; set; } = new ResourceState();
        public Dictionary<int, BuildingState> Buildings { get; set; } = new Dictionary<int, BuildingState>();
        public Dictionary<string, int> Troops { get; set; } = new Dictionary<string, int>();
        public List<QueueItem> BuildQueue { get; set; } = new List<QueueItem>();
        public List<QueueItem> TrainQueue { get; set; } = new List<QueueItem>();
        public List<QueueItem> TrainingQueue { get; set; } = new List<QueueItem>();
        public List<QueueItem> ResearchQueue { get; set; } = new List<QueueItem>();
        public DateTime LastUpdated { get; set; }

        public CityState Clone()
        {
            var clone = (CityState)MemberwiseClone();
            clone.Resources = new ResourceState
            {
                Gold = Resources.Gold,
                Food = Resources.Food,
                Lumber = Resources.Lumber,
                Stone = Resources.Stone,
                Iron = Resources.Iron
            };
            clone.Buildings = new Dictionary<int, BuildingState>(Buildings);
            clone.Troops = new Dictionary<string, int>(Troops);
            return clone;
        }
    }

    /// <summary>
    /// Resource state.
    /// </summary>
    public class ResourceState
    {
        public long Gold { get; set; }
        public long Food { get; set; }
        public long Lumber { get; set; }
        public long Stone { get; set; }
        public long Iron { get; set; }
    
        /// <summary>
        /// Get all resource values as a dictionary.
        /// </summary>
        public Dictionary<string, long> Values => new()
        {
            ["gold"] = Gold,
            ["food"] = Food,
            ["lumber"] = Lumber,
            ["stone"] = Stone,
            ["iron"] = Iron
        };
    }

    /// <summary>
    /// Building state.
    /// </summary>
    public class BuildingState
    {
        public int PositionId { get; set; }
        public int TypeId { get; set; }
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public bool IsUpgrading { get; set; }
        public DateTime? CompletionTime { get; set; }
    }

    /// <summary>
    /// Hero state.
    /// </summary>
    public class HeroState
    {
        public int HeroId { get; set; }
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public long Experience { get; set; }
        public int Politics { get; set; }
        public int Attack { get; set; }
        public int Intelligence { get; set; }
        public int Defense { get; set; }
        public int Leadership { get; set; }
        public string Quality { get; set; } = "Common";
        public int CityId { get; set; }
        public string Status { get; set; } = "";
        public int Stamina { get; set; } = 100;
        public int MaxStamina { get; set; } = 100;
        public long Power { get; set; }
        public Dictionary<int, int> Equipment { get; set; } = new Dictionary<int, int>();
        public List<int> Skills { get; set; } = new List<int>();
        public DateTime LastUpdated { get; set; }

        public HeroState Clone() => (HeroState)MemberwiseClone();
    }

    /// <summary>
    /// Army state.
    /// </summary>
    public class ArmyState
    {
        public int ArmyId { get; set; }
        public int CityId { get; set; }
        public int HeroId { get; set; }
        public string HeroName { get; set; } = "";
        public Dictionary<string, int> Troops { get; set; } = new Dictionary<string, int>();
        public string Status { get; set; } = "";
        public DateTime LastUpdated { get; set; }

        public ArmyState Clone() => (ArmyState)MemberwiseClone();
    }

    /// <summary>
    /// March state.
    /// </summary>
    public class MarchState
    {
        public int MarchId { get; set; }
        public int ArmyId { get; set; }
        public int SourceCityId { get; set; }
        public int TargetX { get; set; }
        public int TargetY { get; set; }
        public string MissionType { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public string Status { get; set; } = "";

        public MarchState Clone() => (MarchState)MemberwiseClone();
    }

    /// <summary>
    /// Queue item (building/training).
    /// </summary>
    public class QueueItem
    {
        public int ItemId { get; set; }
        public string ItemType { get; set; } = "";
        public int Amount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime CompletionTime { get; set; }
    
        /// <summary>
        /// Alias for CompletionTime.
        /// </summary>
        public DateTime EndTime
        {
            get => CompletionTime;
            set => CompletionTime = value;
        }
    }

    /// <summary>
    /// Game state change event.
    /// </summary>
    public class GameStateChange
    {
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public string ChangeType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public GameStateChange(string entityType, int entityId, string changeType)
        {
            EntityType = entityType;
            EntityId = entityId;
            ChangeType = changeType;
        }
    }

    /// <summary>
    /// Game event for history.
    /// </summary>
    public class GameEvent
    {
        public DateTime Timestamp { get; set; }
        public string EntityType { get; set; } = "";
        public int EntityId { get; set; }
        public string EventType { get; set; } = "";
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }

    /// <summary>
    /// Attack alert.
    /// </summary>
    public class AttackAlert
    {
        public int AttackerId { get; set; }
        public string AttackerName { get; set; } = "";
        public int TargetCityId { get; set; }
        public DateTime ArrivalTime { get; set; }
        public long TroopPower { get; set; }
    }

    /// <summary>
    /// Progress information for queues.
    /// </summary>
    public class ProgressInfo
    {
        public double Percentage { get; set; }
        public TimeSpan TimeRemaining { get; set; }
    }

    /// <summary>
    /// Packet event args for traffic processing.
    /// </summary>
    public class PacketEventArgs : EventArgs
    {
        public string Action { get; set; } = "";
        public JToken DecodedData { get; set; }
        public bool IsResponse { get; set; }
        public byte[]? RawData { get; set; }
    }

    #endregion

}