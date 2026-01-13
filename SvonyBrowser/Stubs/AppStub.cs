#if VALIDATION_BUILD
using Serilog;

namespace SvonyBrowser
{

    /// <summary>
    /// Stub App class for validation builds without WPF.
    /// </summary>
    public static class App
    {
        /// <summary>
        /// Logger instance.
        /// </summary>
        public static ILogger Logger { get; } = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        /// <summary>
        /// Configuration path.
        /// </summary>
        public static string ConfigPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SvonyBrowser");

        /// <summary>
        /// MCP data path.
        /// </summary>
        public static string McpDataPath { get; } = Path.Combine(ConfigPath, "mcp-data");

        /// <summary>
        /// Data path.
        /// </summary>
        public static string DataPath { get; } = Path.Combine(ConfigPath, "data");

        /// <summary>
        /// Settings manager.
        /// </summary>
        public static SettingsManager Settings { get; } = SettingsManager.Instance;
    }
}
#endif