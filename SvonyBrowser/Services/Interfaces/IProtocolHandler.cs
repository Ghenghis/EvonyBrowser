using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SvonyBrowser.Services.Interfaces
{
    /// <summary>
    /// Interface for handling Evony game protocol encoding/decoding.
    /// </summary>
    public interface IProtocolHandler : IDisposable
    {
        /// <summary>
        /// Gets the list of known protocol actions.
        /// </summary>
        IReadOnlyDictionary<string, ProtocolAction> KnownActions { get; }

        /// <summary>
        /// Gets the protocol version.
        /// </summary>
        string ProtocolVersion { get; }

        /// <summary>
        /// Occurs when a new action is discovered.
        /// </summary>
        event EventHandler<ActionDiscoveredEventArgs> ActionDiscovered;

        /// <summary>
        /// Occurs when protocol data is decoded.
        /// </summary>
        event EventHandler<ProtocolDecodedEventArgs> DataDecoded;

        /// <summary>
        /// Decodes AMF data to a readable format.
        /// </summary>
        /// <param name="amfData">Base64 or hex encoded AMF data.</param>
        /// <returns>Decoded JSON representation.</returns>
        string DecodeAmfData(string amfData);

        /// <summary>
        /// Encodes data to AMF format.
        /// </summary>
        /// <param name="jsonData">JSON data to encode.</param>
        /// <returns>Base64 encoded AMF data.</returns>
        string EncodeAmfData(string jsonData);

        /// <summary>
        /// Parses a protocol message.
        /// </summary>
        /// <param name="rawData">Raw message data.</param>
        /// <returns>Parsed protocol message.</returns>
        ProtocolMessage ParseMessage(byte[] rawData);

        /// <summary>
        /// Builds a protocol message.
        /// </summary>
        /// <param name="action">Action name.</param>
        /// <param name="parameters">Action parameters.</param>
        /// <returns>Encoded message bytes.</returns>
        byte[] BuildMessage(string action, Dictionary<string, object> parameters);

        /// <summary>
        /// Gets information about a specific action.
        /// </summary>
        /// <param name="actionName">Action name.</param>
        /// <returns>Action information or null if unknown.</returns>
        ProtocolAction GetActionInfo(string actionName);

        /// <summary>
        /// Registers a custom action handler.
        /// </summary>
        /// <param name="actionName">Action name.</param>
        /// <param name="handler">Handler function.</param>
        void RegisterActionHandler(string actionName, Func<ProtocolMessage, Task> handler);

        /// <summary>
        /// Validates a protocol message.
        /// </summary>
        /// <param name="message">Message to validate.</param>
        /// <returns>Validation result.</returns>
        ProtocolValidationResult ValidateMessage(ProtocolMessage message);

        /// <summary>
        /// Loads protocol definitions from a file.
        /// </summary>
        /// <param name="filePath">Path to protocol definition file.</param>
        Task LoadDefinitionsAsync(string filePath);

        /// <summary>
        /// Exports known actions to a file.
        /// </summary>
        /// <param name="filePath">Output file path.</param>
        Task ExportActionsAsync(string filePath);
    }

    public class ActionDiscoveredEventArgs : EventArgs
    {
        public ProtocolAction Action { get; set; }
    }

    public class ProtocolDecodedEventArgs : EventArgs
    {
        public ProtocolMessage Message { get; set; }
        public string DecodedJson { get; set; }
    }

    public class ProtocolAction
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public ProtocolParameter[] Parameters { get; set; }
        public string ResponseType { get; set; }
        public bool IsClientToServer { get; set; }
        public bool IsServerToClient { get; set; }
        public int ObservedCount { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class ProtocolParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
        public string Description { get; set; }
        public object DefaultValue { get; set; }
        public string[] PossibleValues { get; set; }
    }

    public class ProtocolMessage
    {
        public string Id { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
        public MessageDirection Direction { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public byte[] RawData { get; set; }
        public string DecodedJson { get; set; }
        public bool IsValid { get; set; }
        public string ValidationError { get; set; }
    }

    public enum MessageDirection
    {
        ClientToServer,
        ServerToClient,
        Unknown
    }

    public class ProtocolValidationResult
    {
        public bool IsValid { get; set; }
        public string[] Errors { get; set; }
        public string[] Warnings { get; set; }
    }
}
