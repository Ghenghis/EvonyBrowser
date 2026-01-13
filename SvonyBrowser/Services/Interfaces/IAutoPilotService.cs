using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SvonyBrowser.Services.Interfaces
{
    /// <summary>
    /// Interface for the AutoPilot automation service.
    /// </summary>
    public interface IAutoPilotService : IDisposable
    {
        /// <summary>
        /// Gets whether AutoPilot is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets whether AutoPilot is paused.
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// Gets the current automation profile.
        /// </summary>
        AutomationProfile CurrentProfile { get; }

        /// <summary>
        /// Gets the list of available profiles.
        /// </summary>
        IReadOnlyList<AutomationProfile> Profiles { get; }

        /// <summary>
        /// Gets the current task queue.
        /// </summary>
        IReadOnlyList<AutomationTask> TaskQueue { get; }

        /// <summary>
        /// Gets the execution statistics.
        /// </summary>
        AutomationStats Statistics { get; }

        /// <summary>
        /// Occurs when a task starts.
        /// </summary>
        event EventHandler<AutomationTaskEventArgs> TaskStarted;

        /// <summary>
        /// Occurs when a task completes.
        /// </summary>
        event EventHandler<AutomationTaskEventArgs> TaskCompleted;

        /// <summary>
        /// Occurs when a task fails.
        /// </summary>
        event EventHandler<AutomationTaskErrorEventArgs> TaskFailed;

        /// <summary>
        /// Occurs when AutoPilot status changes.
        /// </summary>
        event EventHandler<AutoPilotStatusEventArgs> StatusChanged;

        /// <summary>
        /// Starts AutoPilot with the specified profile.
        /// </summary>
        /// <param name="profileId">Profile identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task StartAsync(string profileId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops AutoPilot.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Pauses AutoPilot execution.
        /// </summary>
        void Pause();

        /// <summary>
        /// Resumes AutoPilot execution.
        /// </summary>
        void Resume();

        /// <summary>
        /// Queues a task for execution.
        /// </summary>
        /// <param name="task">Task to queue.</param>
        void QueueTask(AutomationTask task);

        /// <summary>
        /// Removes a task from the queue.
        /// </summary>
        /// <param name="taskId">Task identifier.</param>
        bool RemoveTask(string taskId);

        /// <summary>
        /// Clears the task queue.
        /// </summary>
        void ClearQueue();

        /// <summary>
        /// Loads an automation profile.
        /// </summary>
        /// <param name="profileId">Profile identifier.</param>
        Task<AutomationProfile> LoadProfileAsync(string profileId);

        /// <summary>
        /// Saves an automation profile.
        /// </summary>
        /// <param name="profile">Profile to save.</param>
        Task SaveProfileAsync(AutomationProfile profile);

        /// <summary>
        /// Deletes an automation profile.
        /// </summary>
        /// <param name="profileId">Profile identifier.</param>
        Task DeleteProfileAsync(string profileId);

        /// <summary>
        /// Gets all available profiles.
        /// </summary>
        Task<IReadOnlyList<AutomationProfile>> GetProfilesAsync();

        /// <summary>
        /// Executes a single task immediately.
        /// </summary>
        /// <param name="task">Task to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<AutomationResult> ExecuteTaskAsync(AutomationTask task, CancellationToken cancellationToken = default);
    }

    public class AutomationTaskEventArgs : EventArgs
    {
        public AutomationTask Task { get; set; }
        public AutomationResult Result { get; set; }
    }

    public class AutomationTaskErrorEventArgs : EventArgs
    {
        public AutomationTask Task { get; set; }
        public Exception Exception { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class AutoPilotStatusEventArgs : EventArgs
    {
        public bool IsRunning { get; set; }
        public bool IsPaused { get; set; }
        public string StatusMessage { get; set; }
    }

    public class AutomationProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }
        public List<AutomationTask> Tasks { get; set; }
        public AutomationSchedule Schedule { get; set; }
        public AutomationSettings Settings { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }

    public class AutomationTask
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // "click", "input", "wait", "condition", "loop", "script"
        public Dictionary<string, object> Parameters { get; set; }
        public int Priority { get; set; }
        public int RetryCount { get; set; }
        public int DelayMs { get; set; }
        public string Condition { get; set; }
        public List<AutomationTask> SubTasks { get; set; }
    }

    public class AutomationSchedule
    {
        public bool Enabled { get; set; }
        public string CronExpression { get; set; }
        public TimeSpan? Interval { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? MaxRuns { get; set; }
    }

    public class AutomationSettings
    {
        public int MaxActionsPerMinute { get; set; } = 10;
        public int DefaultDelayMs { get; set; } = 1000;
        public bool RandomizeDelays { get; set; } = true;
        public double RandomDelayFactor { get; set; } = 0.3;
        public bool StopOnError { get; set; } = false;
        public int MaxRetries { get; set; } = 3;
        public bool EnableSafetyLimits { get; set; } = true;
    }

    public class AutomationStats
    {
        public int TotalTasksExecuted { get; set; }
        public int SuccessfulTasks { get; set; }
        public int FailedTasks { get; set; }
        public TimeSpan TotalRunTime { get; set; }
        public DateTime? LastRunTime { get; set; }
        public double AverageTaskDurationMs { get; set; }
    }

    public class AutomationResult
    {
        public bool Success { get; set; }
        public string TaskId { get; set; }
        public object Result { get; set; }
        public string ErrorMessage { get; set; }
        public double DurationMs { get; set; }
        public DateTime CompletedAt { get; set; }
    }
}
