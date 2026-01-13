using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SvonyBrowser.Services.Interfaces
{
    /// <summary>
    /// Interface for Fiddler proxy bridge communication.
    /// </summary>
    public interface IFiddlerBridge : IDisposable
    {
        /// <summary>
        /// Gets whether the bridge is connected to Fiddler.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets whether traffic capture is active.
        /// </summary>
        bool IsCapturing { get; }

        /// <summary>
        /// Gets the current traffic filter pattern.
        /// </summary>
        string CurrentFilter { get; }

        /// <summary>
        /// Gets the number of active sessions.
        /// </summary>
        int ActiveSessionCount { get; }

        /// <summary>
        /// Gets the current throughput in KB/s.
        /// </summary>
        double ThroughputKBps { get; }

        /// <summary>
        /// Occurs when a session is captured.
        /// </summary>
        event EventHandler<FiddlerSessionEventArgs> SessionCaptured;

        /// <summary>
        /// Occurs when a session is modified.
        /// </summary>
        event EventHandler<FiddlerSessionEventArgs> SessionModified;

        /// <summary>
        /// Occurs when a breakpoint is hit.
        /// </summary>
        event EventHandler<FiddlerBreakpointEventArgs> BreakpointHit;

        /// <summary>
        /// Occurs when an error occurs.
        /// </summary>
        event EventHandler<FiddlerErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// Connects to the Fiddler proxy.
        /// </summary>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Disconnects from the Fiddler proxy.
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Starts capturing traffic.
        /// </summary>
        /// <param name="filter">Optional URL filter pattern.</param>
        Task StartCaptureAsync(string filter = null);

        /// <summary>
        /// Stops capturing traffic.
        /// </summary>
        Task StopCaptureAsync();

        /// <summary>
        /// Sets a breakpoint for traffic interception.
        /// </summary>
        /// <param name="breakpoint">Breakpoint configuration.</param>
        Task SetBreakpointAsync(FiddlerBreakpoint breakpoint);

        /// <summary>
        /// Removes a breakpoint.
        /// </summary>
        /// <param name="breakpointId">Breakpoint identifier.</param>
        Task RemoveBreakpointAsync(string breakpointId);

        /// <summary>
        /// Injects a request into the traffic stream.
        /// </summary>
        /// <param name="request">Request to inject.</param>
        Task InjectRequestAsync(FiddlerRequest request);

        /// <summary>
        /// Replays a captured session.
        /// </summary>
        /// <param name="sessionId">Session identifier to replay.</param>
        Task ReplaySessionAsync(string sessionId);

        /// <summary>
        /// Gets captured sessions.
        /// </summary>
        /// <param name="count">Maximum number of sessions to return.</param>
        IReadOnlyList<FiddlerSession> GetCapturedSessions(int count = 100);

        /// <summary>
        /// Clears all captured sessions.
        /// </summary>
        void ClearSessions();
    }

    public class FiddlerSessionEventArgs : EventArgs
    {
        public FiddlerSession Session { get; set; }
    }

    public class FiddlerBreakpointEventArgs : EventArgs
    {
        public FiddlerBreakpoint Breakpoint { get; set; }
        public FiddlerSession Session { get; set; }
    }

    public class FiddlerErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }

    public class FiddlerSession
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Method { get; set; }
        public string Url { get; set; }
        public int StatusCode { get; set; }
        public string ContentType { get; set; }
        public long RequestSize { get; set; }
        public long ResponseSize { get; set; }
        public double DurationMs { get; set; }
        public Dictionary<string, string> RequestHeaders { get; set; }
        public Dictionary<string, string> ResponseHeaders { get; set; }
        public byte[] RequestBody { get; set; }
        public byte[] ResponseBody { get; set; }
    }

    public class FiddlerBreakpoint
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string UrlPattern { get; set; }
        public string Method { get; set; }
        public bool BreakOnRequest { get; set; }
        public bool BreakOnResponse { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class FiddlerRequest
    {
        public string Method { get; set; }
        public string Url { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public byte[] Body { get; set; }
    }
}
