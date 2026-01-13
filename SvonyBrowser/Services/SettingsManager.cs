using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using SvonyBrowser.Models;

namespace SvonyBrowser.Services
{
    /// <summary>
    /// Manages application settings persistence for Svony Browser v6.0 Borg Edition.
    /// Handles loading, saving, backup, import/export, and factory reset.
    /// </summary>
    public sealed class SettingsManager : IDisposable
    {
        private static readonly Lazy<SettingsManager> _instance = new Lazy<SettingsManager>(() => new SettingsManager());
        public static SettingsManager Instance => _instance.Value;
        
        private readonly string _settingsPath;
        private readonly string _backupPath;
        private readonly JsonSerializerSettings _jsonOptions;
        private AppSettings _settings;
        private bool _disposed;
        private System.Timers.Timer _autoBackupTimer;
        
        public event EventHandler<SettingsChangedEventArgs> SettingsChanged;
        public event EventHandler<SettingsErrorEventArgs> SettingsError;
        
        public AppSettings Settings => _settings;
        
        private SettingsManager()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SvonyBrowser"
            );
            
            Directory.CreateDirectory(appDataPath);
            
            _settingsPath = Path.Combine(appDataPath, "settings.json");
            _backupPath = Path.Combine(appDataPath, "backups");
            
            Directory.CreateDirectory(_backupPath);
            
            _jsonOptions = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                Converters = { new JsonStringEnumConverter() }
            };
            
            _settings = new AppSettings();
        }
        
        #region Loading & Saving
        
        /// <summary>
        /// Loads settings from disk. Creates default settings if file doesn't exist.
        /// </summary>
        public async Task<bool> LoadAsync()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    _settings = new AppSettings();
                    await SaveAsync();
                    return true;
                }
                
                var json = await FileEx.ReadAllTextAsync(_settingsPath);
                var loaded = JsonConvert.DeserializeObject<AppSettings>(json, _jsonOptions);
                
                if (loaded != null)
                {
                    _settings = loaded;
                    MigrateSettings();
                    StartAutoBackup();
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                OnSettingsError("Failed to load settings", ex);
                _settings = new AppSettings();
                return false;
            }
        }
        
        /// <summary>
        /// Saves current settings to disk.
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_settings, _jsonOptions);
                await FileEx.WriteAllTextAsync(_settingsPath, json);
                OnSettingsChanged("Settings saved");
                return true;
            }
            catch (Exception ex)
            {
                OnSettingsError("Failed to save settings", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Saves settings synchronously (for shutdown scenarios).
        /// </summary>
        public bool Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_settings, _jsonOptions);
                File.WriteAllText(_settingsPath, json);
                return true;
            }
            catch (Exception ex)
            {
                OnSettingsError("Failed to save settings", ex);
                return false;
            }
        }
        
        #endregion
        
        #region Backup & Restore
        
        /// <summary>
        /// Creates a timestamped backup of current settings.
        /// </summary>
        public async Task<string> CreateBackupAsync(string description = null)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupName = $"settings_backup_{timestamp}.json";
                var backupFile = Path.Combine(_backupPath, backupName);
                
                var backup = new SettingsBackup
                {
                    Timestamp = DateTime.Now,
                    Description = description ?? "Manual backup",
                    Settings = _settings
                };
                
                var json = JsonConvert.SerializeObject(backup, _jsonOptions);
                await FileEx.WriteAllTextAsync(backupFile, json);
                
                // Clean old backups (keep last 10)
                await CleanOldBackupsAsync(10);
                
                return backupFile;
            }
            catch (Exception ex)
            {
                OnSettingsError("Failed to create backup", ex);
                return null;
            }
        }
        
        /// <summary>
        /// Restores settings from a backup file.
        /// </summary>
        public async Task<bool> RestoreBackupAsync(string backupFile)
        {
            try
            {
                if (!File.Exists(backupFile))
                {
                    OnSettingsError("Backup file not found", null);
                    return false;
                }
                
                var json = await FileEx.ReadAllTextAsync(backupFile);
                var backup = JsonConvert.DeserializeObject<SettingsBackup>(json, _jsonOptions);
                
                if (backup?.Settings != null)
                {
                    // Create backup of current settings before restore
                    await CreateBackupAsync("Pre-restore backup");
                    
                    _settings = backup.Settings;
                    await SaveAsync();
                    OnSettingsChanged("Settings restored from backup");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                OnSettingsError("Failed to restore backup", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Gets list of available backups.
        /// </summary>
        public string[] GetBackups()
        {
            try
            {
                return Directory.GetFiles(_backupPath, "settings_backup_*.json");
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
        
        private async Task CleanOldBackupsAsync(int keepCount)
        {
            try
            {
                var backups = Directory.GetFiles(_backupPath, "settings_backup_*.json")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .Skip(keepCount)
                    .ToArray();
                
                foreach (var backup in backups)
                {
                    File.Delete(backup);
                }
                
                await Task.CompletedTask;
            }
            catch { }
        }
        
        private void StartAutoBackup()
        {
            if (!_settings.General.BackupEnabled) return;
            
            _autoBackupTimer?.Dispose();
            _autoBackupTimer = new System.Timers.Timer(
                TimeSpan.FromHours(_settings.General.BackupIntervalHours).TotalMilliseconds
            );
            _autoBackupTimer.Elapsed += async (s, e) => await CreateBackupAsync("Auto backup");
            _autoBackupTimer.Start();
        }
        
        #endregion
        
        #region Import & Export
        
        /// <summary>
        /// Exports settings to a file (optionally encrypted).
        /// </summary>
        public async Task<bool> ExportAsync(string filePath, bool encrypt = false, string password = null)
        {
            try
            {
                var json = JsonConvert.SerializeObject(_settings, _jsonOptions);
                
                if (encrypt && !string.IsNullOrEmpty(password))
                {
                    var encrypted = EncryptString(json, password);
                    var export = new SettingsExport
                    {
                        Version = _settings.Version,
                        Encrypted = true,
                        Data = encrypted,
                        ExportedAt = DateTime.Now
                    };
                    json = JsonConvert.SerializeObject(export, _jsonOptions);
                }
                else
                {
                    var export = new SettingsExport
                    {
                        Version = _settings.Version,
                        Encrypted = false,
                        Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(json)),
                        ExportedAt = DateTime.Now
                    };
                    json = JsonConvert.SerializeObject(export, _jsonOptions);
                }
                
                await FileEx.WriteAllTextAsync(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                OnSettingsError("Failed to export settings", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Imports settings from a file.
        /// </summary>
        public async Task<bool> ImportAsync(string filePath, string password = null)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    OnSettingsError("Import file not found", null);
                    return false;
                }
                
                var json = await FileEx.ReadAllTextAsync(filePath);
                var export = JsonConvert.DeserializeObject<SettingsExport>(json, _jsonOptions);
                
                if (export == null)
                {
                    OnSettingsError("Invalid export file", null);
                    return false;
                }
                
                string settingsJson;
                if (export.Encrypted)
                {
                    if (string.IsNullOrEmpty(password))
                    {
                        OnSettingsError("Password required for encrypted settings", null);
                        return false;
                    }
                    settingsJson = DecryptString(export.Data, password);
                }
                else
                {
                    settingsJson = Encoding.UTF8.GetString(Convert.FromBase64String(export.Data));
                }
                
                var imported = JsonConvert.DeserializeObject<AppSettings>(settingsJson, _jsonOptions);
                if (imported != null)
                {
                    // Create backup before import
                    await CreateBackupAsync("Pre-import backup");
                    
                    _settings = imported;
                    MigrateSettings();
                    await SaveAsync();
                    OnSettingsChanged("Settings imported successfully");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                OnSettingsError("Failed to import settings", ex);
                return false;
            }
        }
        
        #endregion
        
        #region Reset & Migration
        
        /// <summary>
        /// Resets all settings to defaults.
        /// </summary>
        public async Task<bool> ResetToDefaultsAsync()
        {
            try
            {
                // Create backup before reset
                await CreateBackupAsync("Pre-reset backup");
                
                _settings = new AppSettings();
                await SaveAsync();
                OnSettingsChanged("Settings reset to defaults");
                return true;
            }
            catch (Exception ex)
            {
                OnSettingsError("Failed to reset settings", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Performs a factory reset, clearing all data.
        /// </summary>
        public async Task<bool> FactoryResetAsync()
        {
            try
            {
                // Delete all backups
                foreach (var backup in GetBackups())
                {
                    File.Delete(backup);
                }
                
                // Delete settings file
                if (File.Exists(_settingsPath))
                {
                    File.Delete(_settingsPath);
                }
                
                // Reset to defaults
                _settings = new AppSettings();
                await SaveAsync();
                
                OnSettingsChanged("Factory reset complete");
                return true;
            }
            catch (Exception ex)
            {
                OnSettingsError("Failed to perform factory reset", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Migrates settings from older versions.
        /// </summary>
        private void MigrateSettings()
        {
            // Version migration logic
            var currentVersion = new Version(_settings.Version);
            var targetVersion = new Version("6.0.0");
            
            if (currentVersion < targetVersion)
            {
                // Perform migrations as needed
                _settings.Version = "6.0.0";
            }
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validates all settings and returns any issues.
        /// </summary>
        public SettingsValidationResult Validate()
        {
            var result = new SettingsValidationResult();
            
            // Validate proxy settings
            if (_settings.Proxy.Enabled)
            {
                if (string.IsNullOrWhiteSpace(_settings.Proxy.Host))
                {
                    result.AddError("Proxy", "Proxy host is required when proxy is enabled");
                }
                if (_settings.Proxy.Port < 1 || _settings.Proxy.Port > 65535)
                {
                    result.AddError("Proxy", "Proxy port must be between 1 and 65535");
                }
            }
            
            // Validate LLM settings
            if (_settings.Llm.Enabled)
            {
                if (_settings.Llm.Temperature < 0 || _settings.Llm.Temperature > 2)
                {
                    result.AddWarning("LLM", "Temperature should be between 0 and 2");
                }
                if (_settings.Llm.MaxTokens < 1)
                {
                    result.AddError("LLM", "Max tokens must be at least 1");
                }
            }
            
            // Validate automation settings
            if (_settings.Automation.AutoPilotEnabled && !_settings.Automation.SafetyLimitsEnabled)
            {
                result.AddWarning("Automation", "Safety limits are disabled - use with caution");
            }
            
            // Validate webhook settings
            if (_settings.Webhooks.Enabled)
            {
                var hasWebhook = !string.IsNullOrWhiteSpace(_settings.Webhooks.DiscordWebhook) ||
                                 !string.IsNullOrWhiteSpace(_settings.Webhooks.SlackWebhook) ||
                                 !string.IsNullOrWhiteSpace(_settings.Webhooks.TeamsWebhook) ||
                                 (!string.IsNullOrWhiteSpace(_settings.Webhooks.TelegramBotToken) &&
                                  !string.IsNullOrWhiteSpace(_settings.Webhooks.TelegramChatId));
                
                if (!hasWebhook)
                {
                    result.AddWarning("Webhooks", "Webhooks enabled but no webhook URLs configured");
                }
            }
            
            return result;
        }
        
        #endregion
        
        #region Encryption Helpers
        
        private static string EncryptString(string plainText, string password)
        {
            var aes = Aes.Create(); // TODO: Add using block for proper disposal
            var key = DeriveKey(password, aes.KeySize / 8);
            aes.Key = key;
            aes.GenerateIV();
            
            var encryptor = aes.CreateEncryptor(); // TODO: Add using block for proper disposal
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
            
            return Convert.ToBase64String(result);
        }
        
        private static string DecryptString(string cipherText, string password)
        {
            var fullCipher = Convert.FromBase64String(cipherText);
            
            var aes = Aes.Create(); // TODO: Add using block for proper disposal
            var key = DeriveKey(password, aes.KeySize / 8);
            aes.Key = key;
            
            var iv = new byte[aes.BlockSize / 8];
            var cipher = new byte[fullCipher.Length - iv.Length];
            
            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);
            
            aes.IV = iv;
            
            var decryptor = aes.CreateDecryptor(); // TODO: Add using block for proper disposal
            var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            
            return Encoding.UTF8.GetString(plainBytes);
        }
        
        private static byte[] DeriveKey(string password, int keySize)
        {
            var salt = Encoding.UTF8.GetBytes("SvonyBrowserBorgEdition");
            // .NET Framework 4.6.2 doesn't support HashAlgorithmName parameter
            // Using default SHA1 for compatibility
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            return pbkdf2.GetBytes(keySize);
        }
        
        #endregion
        
        #region Events
        
        private void OnSettingsChanged(string message)
        {
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(message));
        }
        
        private void OnSettingsError(string message, Exception exception)
        {
            SettingsError?.Invoke(this, new SettingsErrorEventArgs(message, exception));
        }
        
        #endregion
        
        #region IDisposable
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            _autoBackupTimer?.Dispose();
            Save(); // Save on dispose
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    public class SettingsBackup
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = "";
        public AppSettings Settings { get; set; } = new AppSettings();
    }
    
    public class SettingsExport
    {
        public string Version { get; set; } = "";
        public bool Encrypted { get; set; }
        public string Data { get; set; } = "";
        public DateTime ExportedAt { get; set; }
    }
    
    public class SettingsChangedEventArgs : EventArgs
    {
        public string Message { get; }
        public DateTime Timestamp { get; }
        
        public SettingsChangedEventArgs(string message)
        {
            Message = message;
            Timestamp = DateTime.Now;
        }
    }
    
    public class SettingsErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception Exception { get; }
        public DateTime Timestamp { get; }
        
        public SettingsErrorEventArgs(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
            Timestamp = DateTime.Now;
        }
    }
    
    public class SettingsValidationResult
    {
        public List<SettingsValidationIssue> Issues { get; } = new List<SettingsValidationIssue>();
        public bool IsValid => !Issues.Any(i => i.Severity == ValidationSeverity.Error);
        public bool HasWarnings => Issues.Any(i => i.Severity == ValidationSeverity.Warning);
        
        public void AddError(string category, string message)
        {
            Issues.Add(new SettingsValidationIssue(category, message, ValidationSeverity.Error));
        }
        
        public void AddWarning(string category, string message)
        {
            Issues.Add(new SettingsValidationIssue(category, message, ValidationSeverity.Warning));
        }
    }
    
    public class SettingsValidationIssue
    {
        public string Category { get; }
        public string Message { get; }
        public ValidationSeverity Severity { get; }
        
        public SettingsValidationIssue(string category, string message, ValidationSeverity severity)
        {
            Category = category;
            Message = message;
            Severity = severity;
        }
    }
    
    public enum ValidationSeverity
    {
        Warning,
        Error
    }
    
    #endregion
}
