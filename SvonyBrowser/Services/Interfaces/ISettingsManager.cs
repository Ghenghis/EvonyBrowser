using System;
using System.Threading.Tasks;
using SvonyBrowser.Models;

namespace SvonyBrowser.Services.Interfaces
{
    /// <summary>
    /// Interface for managing application settings with persistence.
    /// </summary>
    public interface ISettingsManager : IDisposable
    {
        /// <summary>
        /// Gets or sets the current application settings.
        /// </summary>
        AppSettings Settings { get; set; }

        /// <summary>
        /// Gets whether settings have been modified since last save.
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Gets the settings file path.
        /// </summary>
        string SettingsPath { get; }

        /// <summary>
        /// Occurs when settings are changed.
        /// </summary>
        event EventHandler<SettingsChangedEventArgs> SettingsChanged;

        /// <summary>
        /// Occurs when settings are saved.
        /// </summary>
        event EventHandler SettingsSaved;

        /// <summary>
        /// Occurs when settings are loaded.
        /// </summary>
        event EventHandler SettingsLoaded;

        /// <summary>
        /// Loads settings from disk.
        /// </summary>
        Task LoadAsync();

        /// <summary>
        /// Saves settings to disk.
        /// </summary>
        Task SaveAsync();

        /// <summary>
        /// Resets settings to defaults.
        /// </summary>
        void ResetToDefaults();

        /// <summary>
        /// Gets a setting value by key.
        /// </summary>
        /// <typeparam name="T">Type of the setting value.</typeparam>
        /// <param name="key">Setting key.</param>
        /// <param name="defaultValue">Default value if not found.</param>
        /// <returns>Setting value.</returns>
        T Get<T>(string key, T defaultValue = default);

        /// <summary>
        /// Sets a setting value by key.
        /// </summary>
        /// <typeparam name="T">Type of the setting value.</typeparam>
        /// <param name="key">Setting key.</param>
        /// <param name="value">Setting value.</param>
        void Set<T>(string key, T value);

        /// <summary>
        /// Exports settings to a file.
        /// </summary>
        /// <param name="filePath">Export file path.</param>
        Task ExportAsync(string filePath);

        /// <summary>
        /// Imports settings from a file.
        /// </summary>
        /// <param name="filePath">Import file path.</param>
        Task ImportAsync(string filePath);

        /// <summary>
        /// Creates a backup of current settings.
        /// </summary>
        Task BackupAsync();

        /// <summary>
        /// Restores settings from the latest backup.
        /// </summary>
        Task RestoreFromBackupAsync();

        /// <summary>
        /// Validates current settings.
        /// </summary>
        /// <returns>Validation result.</returns>
        SettingsValidationResult Validate();
    }

    public class SettingsChangedEventArgs : EventArgs
    {
        public string PropertyName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }

    public class SettingsValidationResult
    {
        public bool IsValid { get; set; }
        public string[] Errors { get; set; }
        public string[] Warnings { get; set; }
    }
}
