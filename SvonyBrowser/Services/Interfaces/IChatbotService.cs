using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SvonyBrowser.Services.Interfaces
{
    /// <summary>
    /// Interface for the chatbot service with RAG integration.
    /// </summary>
    public interface IChatbotService : IDisposable
    {
        /// <summary>
        /// Gets whether the chatbot is ready to receive messages.
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Gets whether a response is currently being generated.
        /// </summary>
        bool IsGenerating { get; }

        /// <summary>
        /// Gets the conversation history.
        /// </summary>
        IReadOnlyList<ChatMessage> ConversationHistory { get; }

        /// <summary>
        /// Gets the current context sources (RAG documents).
        /// </summary>
        IReadOnlyList<ContextSource> ContextSources { get; }

        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        event EventHandler<ChatMessageEventArgs> MessageReceived;

        /// <summary>
        /// Occurs when a streaming token is received.
        /// </summary>
        event EventHandler<ChatStreamEventArgs> StreamTokenReceived;

        /// <summary>
        /// Occurs when context sources are updated.
        /// </summary>
        event EventHandler<ContextUpdatedEventArgs> ContextUpdated;

        /// <summary>
        /// Occurs when an error occurs.
        /// </summary>
        event EventHandler<ChatErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// Sends a message to the chatbot.
        /// </summary>
        /// <param name="message">User message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The assistant's response.</returns>
        Task<ChatMessage> SendMessageAsync(string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a message with streaming response.
        /// </summary>
        /// <param name="message">User message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async enumerable of response chunks.</returns>
        IAsyncEnumerable<string> SendMessageStreamAsync(string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears the conversation history.
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// Sets the system prompt.
        /// </summary>
        /// <param name="systemPrompt">System prompt text.</param>
        void SetSystemPrompt(string systemPrompt);

        /// <summary>
        /// Adds context from a document.
        /// </summary>
        /// <param name="document">Document to add to context.</param>
        Task AddContextAsync(ContextDocument document);

        /// <summary>
        /// Removes context by source ID.
        /// </summary>
        /// <param name="sourceId">Source identifier.</param>
        void RemoveContext(string sourceId);

        /// <summary>
        /// Clears all context sources.
        /// </summary>
        void ClearContext();

        /// <summary>
        /// Queries the RAG knowledge base.
        /// </summary>
        /// <param name="query">Search query.</param>
        /// <param name="maxResults">Maximum results to return.</param>
        /// <returns>Relevant context sources.</returns>
        Task<IReadOnlyList<ContextSource>> QueryKnowledgeBaseAsync(string query, int maxResults = 5);

        /// <summary>
        /// Cancels any ongoing generation.
        /// </summary>
        void CancelGeneration();

        /// <summary>
        /// Exports the conversation history.
        /// </summary>
        /// <param name="format">Export format (json, markdown, text).</param>
        string ExportHistory(string format = "markdown");
    }

    public class ChatMessageEventArgs : EventArgs
    {
        public ChatMessage Message { get; set; }
    }

    public class ChatStreamEventArgs : EventArgs
    {
        public string Token { get; set; }
        public bool IsComplete { get; set; }
    }

    public class ContextUpdatedEventArgs : EventArgs
    {
        public IReadOnlyList<ContextSource> Sources { get; set; }
    }

    public class ChatErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }

    public class ChatMessage
    {
        public string Id { get; set; }
        public string Role { get; set; } // "user", "assistant", "system"
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public int TokenCount { get; set; }
        public IReadOnlyList<ContextSource> UsedSources { get; set; }
    }

    public class ContextSource
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Source { get; set; }
        public double Relevance { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }

    public class ContextDocument
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Type { get; set; } // "text", "markdown", "code", "json"
        public Dictionary<string, string> Metadata { get; set; }
    }
}
