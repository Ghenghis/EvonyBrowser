using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SvonyBrowser.Services.Interfaces
{
    /// <summary>
    /// Interface for managing MCP (Model Context Protocol) server connections.
    /// </summary>
    public interface IMcpConnectionManager : IDisposable
    {
        /// <summary>
        /// Gets whether the manager is connected to any MCP servers.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets the list of connected MCP servers.
        /// </summary>
        IReadOnlyList<McpServerInfo> ConnectedServers { get; }

        /// <summary>
        /// Gets the list of available tools from all connected servers.
        /// </summary>
        IReadOnlyList<McpTool> AvailableTools { get; }

        /// <summary>
        /// Occurs when connection status changes.
        /// </summary>
        event EventHandler<McpConnectionStatusEventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Occurs when a tool response is received.
        /// </summary>
        event EventHandler<McpToolResponseEventArgs> ToolResponseReceived;

        /// <summary>
        /// Occurs when an error occurs.
        /// </summary>
        event EventHandler<McpErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// Initializes the connection manager.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Connects to an MCP server.
        /// </summary>
        /// <param name="serverConfig">Server configuration.</param>
        Task<bool> ConnectAsync(McpServerConfig serverConfig);

        /// <summary>
        /// Disconnects from an MCP server.
        /// </summary>
        /// <param name="serverId">Server identifier.</param>
        Task DisconnectAsync(string serverId);

        /// <summary>
        /// Disconnects from all servers.
        /// </summary>
        Task DisconnectAllAsync();

        /// <summary>
        /// Invokes a tool on an MCP server.
        /// </summary>
        /// <param name="toolName">Name of the tool to invoke.</param>
        /// <param name="parameters">Tool parameters.</param>
        /// <returns>Tool response.</returns>
        Task<McpToolResponse> InvokeToolAsync(string toolName, Dictionary<string, object> parameters);

        /// <summary>
        /// Gets the status of a specific server.
        /// </summary>
        /// <param name="serverId">Server identifier.</param>
        McpServerStatus GetServerStatus(string serverId);

        /// <summary>
        /// Performs a health check on all connected servers.
        /// </summary>
        Task<Dictionary<string, bool>> HealthCheckAsync();

        /// <summary>
        /// Refreshes the list of available tools from all servers.
        /// </summary>
        Task RefreshToolsAsync();
    }

    public class McpConnectionStatusEventArgs : EventArgs
    {
        public string ServerId { get; set; }
        public bool IsConnected { get; set; }
        public string Message { get; set; }
    }

    public class McpToolResponseEventArgs : EventArgs
    {
        public string ToolName { get; set; }
        public McpToolResponse Response { get; set; }
    }

    public class McpErrorEventArgs : EventArgs
    {
        public string ServerId { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }

    public class McpServerConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Protocol { get; set; } = "http";
        public bool AutoReconnect { get; set; } = true;
        public int TimeoutMs { get; set; } = 30000;
        public Dictionary<string, string> Headers { get; set; }
    }

    public class McpServerInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string[] Capabilities { get; set; }
        public McpServerStatus Status { get; set; }
        public DateTime ConnectedAt { get; set; }
    }

    public enum McpServerStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error,
        Reconnecting
    }

    public class McpTool
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ServerId { get; set; }
        public McpToolParameter[] Parameters { get; set; }
    }

    public class McpToolParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public bool Required { get; set; }
        public object DefaultValue { get; set; }
    }

    public class McpToolResponse
    {
        public bool Success { get; set; }
        public object Result { get; set; }
        public string Error { get; set; }
        public double DurationMs { get; set; }
    }
}
