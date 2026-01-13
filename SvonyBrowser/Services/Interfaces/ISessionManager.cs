using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SvonyBrowser.Models;

namespace SvonyBrowser.Services.Interfaces
{
    /// <summary>
    /// Interface for managing game sessions and accounts.
    /// </summary>
    public interface ISessionManager : IDisposable
    {
        /// <summary>
        /// Gets the currently active session.
        /// </summary>
        GameSession CurrentSession { get; }

        /// <summary>
        /// Gets all available sessions.
        /// </summary>
        IReadOnlyList<GameSession> Sessions { get; }

        /// <summary>
        /// Gets whether a session is currently active.
        /// </summary>
        bool IsSessionActive { get; }

        /// <summary>
        /// Occurs when the active session changes.
        /// </summary>
        event EventHandler<SessionChangedEventArgs> SessionChanged;

        /// <summary>
        /// Occurs when session state is updated.
        /// </summary>
        event EventHandler<SessionStateEventArgs> SessionStateUpdated;

        /// <summary>
        /// Creates a new session with the specified configuration.
        /// </summary>
        /// <param name="config">Session configuration.</param>
        /// <returns>The created session.</returns>
        Task<GameSession> CreateSessionAsync(SessionConfig config);

        /// <summary>
        /// Loads an existing session by ID.
        /// </summary>
        /// <param name="sessionId">Session identifier.</param>
        /// <returns>The loaded session.</returns>
        Task<GameSession> LoadSessionAsync(string sessionId);

        /// <summary>
        /// Saves the current session state.
        /// </summary>
        Task SaveSessionAsync();

        /// <summary>
        /// Switches to a different session.
        /// </summary>
        /// <param name="sessionId">Session identifier to switch to.</param>
        Task SwitchSessionAsync(string sessionId);

        /// <summary>
        /// Deletes a session by ID.
        /// </summary>
        /// <param name="sessionId">Session identifier to delete.</param>
        Task DeleteSessionAsync(string sessionId);

        /// <summary>
        /// Gets all saved sessions.
        /// </summary>
        Task<IReadOnlyList<GameSession>> GetAllSessionsAsync();
    }

    public class SessionChangedEventArgs : EventArgs
    {
        public GameSession OldSession { get; set; }
        public GameSession NewSession { get; set; }
    }

    public class SessionStateEventArgs : EventArgs
    {
        public GameSession Session { get; set; }
        public string PropertyName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }

    public class SessionConfig
    {
        public string Name { get; set; }
        public string Server { get; set; }
        public string Username { get; set; }
        public bool AutoLogin { get; set; }
        public string SwfPath { get; set; }
    }
}
