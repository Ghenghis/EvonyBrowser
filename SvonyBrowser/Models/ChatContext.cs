using System;
using System.Collections.Generic;

namespace SvonyBrowser
{

    /// <summary>
    /// Represents the chat context with game state information.
    /// </summary>
    public class ChatContext
    {
        /// <summary>
        /// Current server name (e.g., cc1, cc2).
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Current player name.
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// Current alliance name.
        /// </summary>
        public string AllianceName { get; set; }

        /// <summary>
        /// Current city information.
        /// </summary>
        public CityContext? CurrentCity { get; set; }

        /// <summary>
        /// List of all cities.
        /// </summary>
        public List<CityContext> Cities { get; set; } = new List<CityContext>();

        /// <summary>
        /// Recent traffic entries for context.
        /// </summary>
        public List<TrafficEntry> RecentTraffic { get; set; } = new List<TrafficEntry>();

        /// <summary>
        /// Current session token.
        /// </summary>
        public string SessionToken { get; set; }

        /// <summary>
        /// Whether the player is currently online.
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// Last activity timestamp.
        /// </summary>
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Custom context data.
        /// </summary>
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents city context information.
    /// </summary>
    public class CityContext
    {
        /// <summary>
        /// City ID.
        /// </summary>
        public int CityId { get; set; }

        /// <summary>
        /// City name.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// City coordinates.
        /// </summary>
        public (int X, int Y) Coordinates { get; set; }

        /// <summary>
        /// City level (Town Hall level).
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Current resources.
        /// </summary>
        public ResourceInfo Resources { get; set; } = new ResourceInfo();

        /// <summary>
        /// Current troops.
        /// </summary>
        public TroopInfo Troops { get; set; } = new TroopInfo();
    }

    /// <summary>
    /// Represents resource information.
    /// </summary>
    public class ResourceInfo
    {
        public long Gold { get; set; }
        public long Food { get; set; }
        public long Lumber { get; set; }
        public long Stone { get; set; }
        public long Iron { get; set; }
    }

    /// <summary>
    /// Represents troop information.
    /// </summary>
    public class TroopInfo
    {
        public int Workers { get; set; }
        public int Warriors { get; set; }
        public int Scouts { get; set; }
        public int Pikemen { get; set; }
        public int Swordsmen { get; set; }
        public int Archers { get; set; }
        public int Cavalry { get; set; }
        public int Cataphracts { get; set; }
        public int Transporters { get; set; }
        public int Ballistas { get; set; }
        public int BatteringRams { get; set; }
        public int Catapults { get; set; }
    }

    /// <summary>
    /// Represents the role in a chat conversation.
    /// </summary>
    public enum ChatRole
    {
        /// <summary>
        /// System message.
        /// </summary>
        System,

        /// <summary>
        /// User message.
        /// </summary>
        User,

        /// <summary>
        /// Assistant/AI message.
        /// </summary>
        Assistant,

        /// <summary>
        /// Tool/function result.
        /// </summary>
        Tool
    }

    // McpConnectionStatus is defined in SvonyBrowser.Services.McpConnectionManager

}