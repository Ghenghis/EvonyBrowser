using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SvonyBrowser.Services.Interfaces
{
    /// <summary>
    /// Interface for LLM (Large Language Model) integration services.
    /// </summary>
    public interface ILlmIntegrationService : IDisposable
    {
        /// <summary>
        /// Gets whether the service is connected to an LLM backend.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets whether inference is currently in progress.
        /// </summary>
        bool IsInferring { get; }

        /// <summary>
        /// Gets the current model name.
        /// </summary>
        string CurrentModel { get; }

        /// <summary>
        /// Gets the current tokens per second rate.
        /// </summary>
        double TokensPerSecond { get; }

        /// <summary>
        /// Gets the context length.
        /// </summary>
        int ContextLength { get; }

        /// <summary>
        /// Gets the temperature setting.
        /// </summary>
        double Temperature { get; }

        /// <summary>
        /// Gets the GPU temperature in Celsius.
        /// </summary>
        double GpuTemperature { get; }

        /// <summary>
        /// Gets the VRAM usage in GB.
        /// </summary>
        double VramUsageGb { get; }

        /// <summary>
        /// Gets the total VRAM in GB.
        /// </summary>
        double VramTotalGb { get; }

        /// <summary>
        /// Occurs when a response is received.
        /// </summary>
        event EventHandler<LlmResponseEventArgs> ResponseReceived;

        /// <summary>
        /// Occurs when a streaming token is received.
        /// </summary>
        event EventHandler<LlmStreamEventArgs> StreamTokenReceived;

        /// <summary>
        /// Occurs when an error occurs.
        /// </summary>
        event EventHandler<LlmErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// Connects to the LLM backend.
        /// </summary>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Disconnects from the LLM backend.
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Sends a completion request to the LLM.
        /// </summary>
        /// <param name="prompt">The prompt text.</param>
        /// <param name="options">Optional generation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated response.</returns>
        Task<LlmResponse> CompleteAsync(string prompt, LlmOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a chat completion request to the LLM.
        /// </summary>
        /// <param name="messages">Chat messages.</param>
        /// <param name="options">Optional generation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated response.</returns>
        Task<LlmResponse> ChatAsync(IEnumerable<LlmMessage> messages, LlmOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams a completion response from the LLM.
        /// </summary>
        /// <param name="prompt">The prompt text.</param>
        /// <param name="options">Optional generation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async enumerable of response chunks.</returns>
        IAsyncEnumerable<string> StreamCompleteAsync(string prompt, LlmOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of available models.
        /// </summary>
        Task<IReadOnlyList<LlmModel>> GetModelsAsync();

        /// <summary>
        /// Sets the current model.
        /// </summary>
        /// <param name="modelName">Model name to use.</param>
        Task SetModelAsync(string modelName);

        /// <summary>
        /// Cancels any ongoing inference.
        /// </summary>
        void CancelInference();
    }

    public class LlmResponseEventArgs : EventArgs
    {
        public string Response { get; set; }
        public int TokenCount { get; set; }
        public double DurationMs { get; set; }
    }

    public class LlmStreamEventArgs : EventArgs
    {
        public string Token { get; set; }
        public bool IsComplete { get; set; }
    }

    public class LlmErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }

    public class LlmOptions
    {
        public double? Temperature { get; set; }
        public int? MaxTokens { get; set; }
        public double? TopP { get; set; }
        public double? TopK { get; set; }
        public string[] StopSequences { get; set; }
        public bool Stream { get; set; }
    }

    public class LlmMessage
    {
        public string Role { get; set; } // "system", "user", "assistant"
        public string Content { get; set; }
    }

    public class LlmResponse
    {
        public string Content { get; set; }
        public string Model { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public double DurationMs { get; set; }
        public string FinishReason { get; set; }
    }

    public class LlmModel
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public long SizeBytes { get; set; }
        public int ContextLength { get; set; }
        public string[] Capabilities { get; set; }
    }
}
