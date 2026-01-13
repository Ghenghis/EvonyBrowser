using System;
using Newtonsoft.Json.Linq;
using SvonyBrowser.Services;

namespace SvonyBrowser
{

    /// <summary>
    /// Represents a captured traffic entry.
    /// </summary>
    public class TrafficEntry
    {
        /// <summary>
        /// Unique identifier for the entry.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp when the traffic was captured.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// HTTP method (GET, POST, etc.).
        /// </summary>
        public string Method { get; set; } = "GET";

        /// <summary>
        /// Request URL.
        /// </summary>
        public string Url { get; set; } = "";

        /// <summary>
        /// Request path (extracted from URL).
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// Host name.
        /// </summary>
        public string Host { get; set; } = "";

        /// <summary>
        /// HTTP status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Response status text.
        /// </summary>
        public string StatusText { get; set; } = "";

        /// <summary>
        /// Content type of the response.
        /// </summary>
        public string ContentType { get; set; } = "";

        /// <summary>
        /// Request size in bytes.
        /// </summary>
        public long RequestSize { get; set; }

        /// <summary>
        /// Response size in bytes.
        /// </summary>
        public long ResponseSize { get; set; }

        /// <summary>
        /// Request duration in milliseconds.
        /// </summary>
        public long Duration { get; set; }

        /// <summary>
        /// Raw request body.
        /// </summary>
        public byte[]? RequestBody { get; set; }

        /// <summary>
        /// Raw response body.
        /// </summary>
        public byte[]? ResponseBody { get; set; }

        /// <summary>
        /// Decoded request data (if AMF3).
        /// </summary>
        public JToken DecodedRequest { get; set; }

        /// <summary>
        /// Decoded response data (if AMF3).
        /// </summary>
        public JToken DecodedResponse { get; set; }

        /// <summary>
        /// Whether this is an AMF3 request.
        /// </summary>
        public bool IsAmf3 { get; set; }

        /// <summary>
        /// Detected packet type.
        /// </summary>
        public string PacketType { get; set; } = "Unknown";

        /// <summary>
        /// Protocol action name (if identified).
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// Command ID (if identified).
        /// </summary>
        public int? CommandId { get; set; }

        /// <summary>
        /// Whether this entry has an error.
        /// </summary>
        public bool HasError => StatusCode >= 400;

        /// <summary>
        /// Whether this entry is a redirect.
        /// </summary>
        public bool IsRedirect => StatusCode >= 300 && StatusCode < 400;

        /// <summary>
        /// Whether this entry is successful.
        /// </summary>
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;

        /// <summary>
        /// Gets a display-friendly summary of the entry.
        /// </summary>
        public string Summary
        {
            get
            {
                if (!string.IsNullOrEmpty(ActionName))
                {
                    return $"{ActionName} ({StatusCode})";
                }
                return $"{Method} {Path} ({StatusCode})";
            }
        }

        /// <summary>
        /// Gets the formatted timestamp.
        /// </summary>
        public string FormattedTimestamp => Timestamp.ToLocalTime().ToString("HH:mm:ss.fff");

        /// <summary>
        /// Gets the formatted duration.
        /// </summary>
        public string FormattedDuration => $"{Duration}ms";

        /// <summary>
        /// Gets the formatted size.
        /// </summary>
        public string FormattedSize
        {
            get
            {
                var totalSize = RequestSize + ResponseSize;
                if (totalSize < 1024) return $"{totalSize} B";
                if (totalSize < 1024 * 1024) return $"{totalSize / 1024.0:F1} KB";
                return $"{totalSize / (1024.0 * 1024.0):F1} MB";
            }
        }

        /// <summary>
        /// Creates a copy of this entry.
        /// </summary>
        public TrafficEntry Clone()
        {
            return new TrafficEntry
            {
                Id = Id,
                Timestamp = Timestamp,
                Method = Method,
                Url = Url,
                Path = Path,
                Host = Host,
                StatusCode = StatusCode,
                StatusText = StatusText,
                ContentType = ContentType,
                RequestSize = RequestSize,
                ResponseSize = ResponseSize,
                Duration = Duration,
                RequestBody = RequestBody,
                ResponseBody = ResponseBody,
                DecodedRequest = DecodedRequest,
                DecodedResponse = DecodedResponse,
                IsAmf3 = IsAmf3,
                PacketType = PacketType,
                ActionName = ActionName,
                CommandId = CommandId
            };
        }
    }

    /// <summary>
    /// Represents a parsed protocol packet.
    /// </summary>
    public class ParsedPacket
    {
        /// <summary>
        /// Direction of the packet (request/response).
        /// </summary>
        public string Direction { get; set; } = "request";

        /// <summary>
        /// Raw binary data.
        /// </summary>
        public byte[]? RawData { get; set; }

        /// <summary>
        /// Decoded data as JSON.
        /// </summary>
        public JToken DecodedData { get; set; }

        /// <summary>
        /// Timestamp when the packet was captured.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Command ID.
        /// </summary>
        public int? CommandId { get; set; }

        /// <summary>
        /// Action name.
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// Sequence ID.
        /// </summary>
        public int? SequenceId { get; set; }

        /// <summary>
        /// Payload data.
        /// </summary>
        public JToken PayloadData { get; set; }

        /// <summary>
        /// Associated protocol action.
        /// </summary>
        public ProtocolAction? Action { get; set; }
    }

}