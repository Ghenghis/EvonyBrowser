using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SvonyBrowser.Services.Interfaces
{
    /// <summary>
    /// Interface for tracking and managing game state.
    /// </summary>
    public interface IGameStateEngine : IDisposable
    {
        /// <summary>
        /// Gets the current game state.
        /// </summary>
        GameState CurrentState { get; }

        /// <summary>
        /// Gets whether the game is currently connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets the current player information.
        /// </summary>
        PlayerInfo Player { get; }

        /// <summary>
        /// Gets the current city information.
        /// </summary>
        CityInfo CurrentCity { get; }

        /// <summary>
        /// Gets all cities owned by the player.
        /// </summary>
        IReadOnlyList<CityInfo> Cities { get; }

        /// <summary>
        /// Occurs when game state changes.
        /// </summary>
        event EventHandler<GameStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Occurs when resources are updated.
        /// </summary>
        event EventHandler<ResourcesUpdatedEventArgs> ResourcesUpdated;

        /// <summary>
        /// Occurs when a city is updated.
        /// </summary>
        event EventHandler<CityUpdatedEventArgs> CityUpdated;

        /// <summary>
        /// Occurs when troops are updated.
        /// </summary>
        event EventHandler<TroopsUpdatedEventArgs> TroopsUpdated;

        /// <summary>
        /// Initializes the game state engine.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Updates the game state from protocol data.
        /// </summary>
        /// <param name="action">Protocol action name.</param>
        /// <param name="data">Protocol data.</param>
        void ProcessProtocolData(string action, Dictionary<string, object> data);

        /// <summary>
        /// Gets the current resources.
        /// </summary>
        Resources GetResources();

        /// <summary>
        /// Gets troops for a specific city.
        /// </summary>
        /// <param name="cityId">City identifier.</param>
        TroopInfo GetTroops(string cityId = null);

        /// <summary>
        /// Gets building information for a city.
        /// </summary>
        /// <param name="cityId">City identifier.</param>
        IReadOnlyList<BuildingInfo> GetBuildings(string cityId = null);

        /// <summary>
        /// Gets active marches.
        /// </summary>
        IReadOnlyList<MarchInfo> GetActiveMarches();

        /// <summary>
        /// Gets the build queue for a city.
        /// </summary>
        /// <param name="cityId">City identifier.</param>
        IReadOnlyList<QueueItem> GetBuildQueue(string cityId = null);

        /// <summary>
        /// Gets the research queue.
        /// </summary>
        IReadOnlyList<QueueItem> GetResearchQueue();

        /// <summary>
        /// Gets the training queue for a city.
        /// </summary>
        /// <param name="cityId">City identifier.</param>
        IReadOnlyList<QueueItem> GetTrainingQueue(string cityId = null);

        /// <summary>
        /// Exports the current state to JSON.
        /// </summary>
        string ExportState();

        /// <summary>
        /// Imports state from JSON.
        /// </summary>
        /// <param name="json">JSON state data.</param>
        void ImportState(string json);
    }

    public class GameStateChangedEventArgs : EventArgs
    {
        public GameState OldState { get; set; }
        public GameState NewState { get; set; }
        public string ChangedProperty { get; set; }
    }

    public class ResourcesUpdatedEventArgs : EventArgs
    {
        public Resources Resources { get; set; }
        public string CityId { get; set; }
    }

    public class CityUpdatedEventArgs : EventArgs
    {
        public CityInfo City { get; set; }
        public string UpdateType { get; set; }
    }

    public class TroopsUpdatedEventArgs : EventArgs
    {
        public TroopInfo Troops { get; set; }
        public string CityId { get; set; }
    }

    public enum GameState
    {
        Disconnected,
        Connecting,
        Loading,
        InGame,
        InBattle,
        Error
    }

    public class PlayerInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public long Experience { get; set; }
        public string AllianceId { get; set; }
        public string AllianceName { get; set; }
        public string Title { get; set; }
        public int Prestige { get; set; }
        public int Honor { get; set; }
    }

    public class CityInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsCapital { get; set; }
        public int Population { get; set; }
        public int MaxPopulation { get; set; }
        public Resources Resources { get; set; }
        public Resources Production { get; set; }
    }

    public class Resources
    {
        public long Gold { get; set; }
        public long Food { get; set; }
        public long Wood { get; set; }
        public long Stone { get; set; }
        public long Iron { get; set; }
        public int Gems { get; set; }
    }

    public class TroopInfo
    {
        public Dictionary<string, int> Troops { get; set; }
        public Dictionary<string, int> Wounded { get; set; }
        public int TotalTroops { get; set; }
        public int TotalWounded { get; set; }
        public int Capacity { get; set; }
    }

    public class BuildingInfo
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public int Level { get; set; }
        public int Position { get; set; }
        public bool IsUpgrading { get; set; }
        public DateTime? UpgradeCompleteTime { get; set; }
    }

    public class MarchInfo
    {
        public string Id { get; set; }
        public string Type { get; set; } // "attack", "reinforce", "transport", "scout"
        public string TargetX { get; set; }
        public string TargetY { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime? ReturnTime { get; set; }
        public TroopInfo Troops { get; set; }
        public Resources Cargo { get; set; }
    }

    public class QueueItem
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int Quantity { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime CompleteTime { get; set; }
        public double ProgressPercent { get; set; }
    }
}
