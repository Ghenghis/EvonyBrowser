using System;
using System.Collections.Generic;

namespace SvonyBrowser
{

    /// <summary>
    /// Represents a chat message in the co-pilot conversation.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Unique identifier for the message.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The role of the message sender.
        /// </summary>
        public ChatRole Role { get; set; }

        /// <summary>
        /// The content of the message.
        /// </summary>
        public string Content { get; set; } = "";

        /// <summary>
        /// When the message was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Sources referenced in the response.
        /// </summary>
        public List<string> Sources { get; set; }

        /// <summary>
        /// Whether this message represents an error.
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// Additional metadata for the message.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }
    }

    // Note: ChatRole and ChatContext are defined in ChatContext.cs
    // Note: TrafficData is defined in TrafficEntry.cs

    /// <summary>
    /// Represents captured traffic data for chat context.
    /// </summary>
    public class ChatTrafficData
    {
        /// <summary>
        /// Unique identifier for the traffic entry.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Direction of the traffic (request/response).
        /// </summary>
        public string Direction { get; set; } = "request";

        /// <summary>
        /// The URL of the request.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The action/command name.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// HTTP status code (for responses).
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// Timestamp in milliseconds.
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Content length in bytes.
        /// </summary>
        public int ContentLength { get; set; }

        /// <summary>
        /// Raw hex data.
        /// </summary>
        public string HexData { get; set; }

        /// <summary>
        /// Decoded data object.
        /// </summary>
        public object Decoded { get; set; }

        /// <summary>
        /// HTTP headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }
    }

}