using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SvonyBrowser.Services
{

    /// <summary>
    /// Manages and coordinates multiple game accounts from a single interface
    /// with synchronized actions, resource sharing, and coordinated attacks.
    /// </summary>
    public sealed class MultiAccountOrchestrator : IDisposable
    {
        #region Singleton

        private static readonly Lazy<MultiAccountOrchestrator> _lazyInstance =
            new Lazy<MultiAccountOrchestrator>(() => new MultiAccountOrchestrator(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static MultiAccountOrchestrator Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly ConcurrentDictionary<string, AccountSession> _accounts = new AccountSession>();
        private readonly List<CoordinatedAction> _pendingActions = new List<CoordinatedAction>();
        private readonly object _lock = new object();
        private string _activeAccountId;
        private bool _disposed = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets all registered accounts.
        /// </summary>
        public IReadOnlyDictionary<string, AccountSession> Accounts => _accounts;

        /// <summary>
        /// Gets the currently active account ID.
        /// </summary>
        public string ActiveAccountId => _activeAccountId;

        /// <summary>
        /// Gets the currently active account.
        /// </summary>
        public AccountSession? ActiveAccount => 
            _activeAccountId != null && _accounts.TryGetValue(_activeAccountId, out var account) 
                ? account : null;

        /// <summary>
        /// Gets the total power across all accounts.
        /// </summary>
        public long TotalPower => _accounts.Values.Sum(a => a.Power);

        /// <summary>
        /// Gets the total resources across all accounts.
        /// </summary>
        public ResourceState TotalResources
        {
            get
            {
                var total = new ResourceState();
                foreach (var account in _accounts.Values)
                {
                    total.Gold += account.Resources.Gold;
                    total.Food += account.Resources.Food;
                    total.Lumber += account.Resources.Lumber;
                    total.Stone += account.Resources.Stone;
                    total.Iron += account.Resources.Iron;
                }
                return total;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired when the active account changes.
        /// </summary>
        public event Action<AccountSession?> ActiveAccountChanged;

        /// <summary>
        /// Fired when an account is added.
        /// </summary>
        public event Action<AccountSession> AccountAdded;

        /// <summary>
        /// Fired when an account is removed.
        /// </summary>
        public event Action<string> AccountRemoved;

        /// <summary>
        /// Fired when an account's status changes.
        /// </summary>
        public event Action<AccountSession> AccountStatusChanged;

        /// <summary>
        /// Fired when a coordinated action completes.
        /// </summary>
        public event Action<CoordinatedAction> CoordinatedActionCompleted;

        #endregion

        #region Constructor

        private MultiAccountOrchestrator()
        {
            LoadAccounts();
            App.Logger.Information("MultiAccountOrchestrator initialized");
        }

        #endregion

        #region Public Methods - Account Management

        /// <summary>
        /// Adds a new account.
        /// </summary>
        public void AddAccount(AccountConfig config)
        {
            var session = new AccountSession
            {
                AccountId = config.AccountId,
                AccountName = config.AccountName,
                ServerUrl = config.ServerUrl,
                Username = config.Username,
                AccountType = config.AccountType,
                Status = AccountStatus.Offline,
                LastActivity = DateTime.UtcNow
            };

            if (_accounts.TryAdd(config.AccountId, session))
            {
                AccountAdded?.Invoke(session);
                SaveAccounts();
                App.Logger.Information("Added account: {Name}", config.AccountName);
            }
        }

        /// <summary>
        /// Removes an account.
        /// </summary>
        public void RemoveAccount(string accountId)
        {
            if (_accounts.TryRemove(accountId, out _))
            {
                if (_activeAccountId == accountId)
                {
                    _activeAccountId = _accounts.Keys.FirstOrDefault();
                    ActiveAccountChanged?.Invoke(ActiveAccount);
                }

                AccountRemoved?.Invoke(accountId);
                SaveAccounts();
                App.Logger.Information("Removed account: {Id}", accountId);
            }
        }

        /// <summary>
        /// Switches to a different account.
        /// </summary>
        public async Task SwitchAccountAsync(string accountId)
        {
            if (!_accounts.TryGetValue(accountId, out var account))
            {
                App.Logger.Warning("Account not found: {Id}", accountId);
                return;
            }

            // Mark previous account as idle
            if (_activeAccountId != null && _accounts.TryGetValue(_activeAccountId, out var previousAccount))
            {
                previousAccount.Status = AccountStatus.Idle;
                AccountStatusChanged?.Invoke(previousAccount);
            }

            _activeAccountId = accountId;
            account.Status = AccountStatus.Active;
            account.LastActivity = DateTime.UtcNow;

            AccountStatusChanged?.Invoke(account);
            ActiveAccountChanged?.Invoke(account);

            // Trigger browser navigation to account
            await NavigateToAccountAsync(account);

            App.Logger.Information("Switched to account: {Name}", account.AccountName);
        }

        /// <summary>
        /// Updates account status and data.
        /// </summary>
        public void UpdateAccountData(string accountId, AccountData data)
        {
            if (!_accounts.TryGetValue(accountId, out var account))
                return;

            account.Power = data.Power;
            account.Resources = data.Resources;
            account.CityCount = data.CityCount;
            account.LastActivity = DateTime.UtcNow;

            AccountStatusChanged?.Invoke(account);
        }

        #endregion

        #region Public Methods - Coordinated Actions

        /// <summary>
        /// Initiates a coordinated rally attack from multiple accounts.
        /// </summary>
        public async Task<CoordinatedAction> StartRallyAttackAsync(
            int targetX, int targetY,
            List<string> participatingAccounts,
            DateTime rallyTime)
        {
            var action = new CoordinatedAction
            {
                ActionId = Guid.NewGuid().ToString(),
                ActionType = "rally_attack",
                TargetX = targetX,
                TargetY = targetY,
                ParticipatingAccounts = participatingAccounts,
                ScheduledTime = rallyTime,
                Status = "pending"
            };

            lock (_lock)
            {
                _pendingActions.Add(action);
            }

            // Schedule the rally
            var delay = rallyTime - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                _ = Task.Delay(delay).ContinueWith(async _ =>
                {
                    await ExecuteRallyAttackAsync(action);
                });
            }
            else
            {
                await ExecuteRallyAttackAsync(action);
            }

            return action;
        }

        /// <summary>
        /// Initiates a mass resource transfer from farm accounts to main.
        /// </summary>
        public async Task<CoordinatedAction> StartMassResourceTransferAsync(
            string targetAccountId,
            List<string> sourceAccounts)
        {
            var action = new CoordinatedAction
            {
                ActionId = Guid.NewGuid().ToString(),
                ActionType = "resource_transfer",
                TargetAccountId = targetAccountId,
                ParticipatingAccounts = sourceAccounts,
                ScheduledTime = DateTime.UtcNow,
                Status = "executing"
            };

            lock (_lock)
            {
                _pendingActions.Add(action);
            }

            await ExecuteResourceTransferAsync(action);

            return action;
        }

        /// <summary>
        /// Synchronizes training across all accounts.
        /// </summary>
        public async Task<CoordinatedAction> StartSynchronizedTrainingAsync(
            string troopType,
            int amountPerAccount)
        {
            var action = new CoordinatedAction
            {
                ActionId = Guid.NewGuid().ToString(),
                ActionType = "sync_training",
                TroopType = troopType,
                Amount = amountPerAccount,
                ParticipatingAccounts = _accounts.Keys.ToList(),
                ScheduledTime = DateTime.UtcNow,
                Status = "executing"
            };

            lock (_lock)
            {
                _pendingActions.Add(action);
            }

            await ExecuteSynchronizedTrainingAsync(action);

            return action;
        }

        /// <summary>
        /// Gets pending coordinated actions.
        /// </summary>
        public List<CoordinatedAction> GetPendingActions()
        {
            lock (_lock)
            {
                return _pendingActions.Where(a => a.Status == "pending" || a.Status == "executing").ToList();
            }
        }

        /// <summary>
        /// Cancels a pending coordinated action.
        /// </summary>
        public void CancelAction(string actionId)
        {
            lock (_lock)
            {
                var action = _pendingActions.FirstOrDefault(a => a.ActionId == actionId);
                if (action != null)
                {
                    action.Status = "cancelled";
                }
            }
        }

        #endregion

        #region Public Methods - Auto-Management

        /// <summary>
        /// Starts auto-pilot on all farm accounts.
        /// </summary>
        public async Task StartFarmAutoPilotAsync()
        {
            var farmAccounts = _accounts.Values
                .Where(a => a.AccountType == "farm")
                .ToList();

            foreach (var account in farmAccounts)
            {
                account.AutoPilotEnabled = true;
                account.Status = AccountStatus.AutoPilot;
                AccountStatusChanged?.Invoke(account);
            }

            App.Logger.Information("Started auto-pilot on {Count} farm accounts", farmAccounts.Count);
        }

        /// <summary>
        /// Stops auto-pilot on all accounts.
        /// </summary>
        public void StopAllAutoPilot()
        {
            foreach (var account in _accounts.Values)
            {
                account.AutoPilotEnabled = false;
                if (account.Status == AccountStatus.AutoPilot)
                {
                    account.Status = AccountStatus.Idle;
                    AccountStatusChanged?.Invoke(account);
                }
            }

            App.Logger.Information("Stopped auto-pilot on all accounts");
        }

        /// <summary>
        /// Gets account recommendations based on current state.
        /// </summary>
        public List<AccountRecommendation> GetRecommendations()
        {
            var recommendations = new List<AccountRecommendation>();

            // Check for idle farm accounts with high resources
            foreach (var account in _accounts.Values.Where(a => a.AccountType == "farm"))
            {
                var totalResources = account.Resources.Gold + account.Resources.Food + 
                                   account.Resources.Lumber + account.Resources.Stone + account.Resources.Iron;
            
                if (totalResources > 5000000)
                {
                    recommendations.Add(new AccountRecommendation
                    {
                        AccountId = account.AccountId,
                        AccountName = account.AccountName,
                        RecommendationType = "transfer_resources",
                        Description = $"Farm account has {totalResources:N0} resources. Consider transferring to main account.",
                        Priority = 1
                    });
                }
            }

            // Check for accounts that haven't been active
            foreach (var account in _accounts.Values)
            {
                if ((DateTime.UtcNow - account.LastActivity).TotalHours > 24)
                {
                    recommendations.Add(new AccountRecommendation
                    {
                        AccountId = account.AccountId,
                        AccountName = account.AccountName,
                        RecommendationType = "check_account",
                        Description = $"Account hasn't been active for {(DateTime.UtcNow - account.LastActivity).TotalHours:F0} hours.",
                        Priority = 2
                    });
                }
            }

            return recommendations.OrderBy(r => r.Priority).ToList();
        }

        #endregion

        #region Private Methods

        private async Task NavigateToAccountAsync(AccountSession account)
        {
            // This would integrate with the browser to switch accounts
            // For now, we just log the action
            App.Logger.Information("Navigating to account: {Name} at {Url}", 
                account.AccountName, account.ServerUrl);
        
            await Task.Delay(100);
        }

        private async Task ExecuteRallyAttackAsync(CoordinatedAction action)
        {
            action.Status = "executing";

            try
            {
                foreach (var accountId in action.ParticipatingAccounts)
                {
                    if (!_accounts.TryGetValue(accountId, out var account))
                        continue;

                    // Switch to account and send troops
                    await SwitchAccountAsync(accountId);
                
                    // Send attack command
                    App.Logger.Information("Sending rally troops from {Account} to ({X}, {Y})",
                        account.AccountName, action.TargetX, action.TargetY);

                    await Task.Delay(500); // Simulate action
                }

                action.Status = "completed";
                action.CompletedTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                action.Status = "failed";
                action.Error = ex.Message;
                App.Logger.Error(ex, "Rally attack failed");
            }

            CoordinatedActionCompleted?.Invoke(action);
        }

        private async Task ExecuteResourceTransferAsync(CoordinatedAction action)
        {
            try
            {
                foreach (var sourceAccountId in action.ParticipatingAccounts)
                {
                    if (!_accounts.TryGetValue(sourceAccountId, out var sourceAccount))
                        continue;

                    // Switch to source account
                    await SwitchAccountAsync(sourceAccountId);

                    // Send resources to target
                    App.Logger.Information("Transferring resources from {Source} to {Target}",
                        sourceAccount.AccountName, action.TargetAccountId);

                    await Task.Delay(500); // Simulate action
                }

                action.Status = "completed";
                action.CompletedTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                action.Status = "failed";
                action.Error = ex.Message;
                App.Logger.Error(ex, "Resource transfer failed");
            }

            CoordinatedActionCompleted?.Invoke(action);
        }

        private async Task ExecuteSynchronizedTrainingAsync(CoordinatedAction action)
        {
            try
            {
                foreach (var accountId in action.ParticipatingAccounts)
                {
                    if (!_accounts.TryGetValue(accountId, out var account))
                        continue;

                    // Switch to account
                    await SwitchAccountAsync(accountId);

                    // Train troops
                    App.Logger.Information("Training {Amount} {Type} on {Account}",
                        action.Amount, action.TroopType, account.AccountName);

                    await Task.Delay(300); // Simulate action
                }

                action.Status = "completed";
                action.CompletedTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                action.Status = "failed";
                action.Error = ex.Message;
                App.Logger.Error(ex, "Synchronized training failed");
            }

            CoordinatedActionCompleted?.Invoke(action);
        }

        private void LoadAccounts()
        {
            try
            {
                var configPath = Path.Combine(App.ConfigPath, "accounts.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var accounts = JsonConvert.DeserializeObject<List<AccountSession>>(json);
                    if (accounts != null)
                    {
                        foreach (var account in accounts)
                        {
                            account.Status = AccountStatus.Offline;
                            _accounts[account.AccountId] = account;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to load accounts");
            }
        }

        private void SaveAccounts()
        {
            try
            {
                var configPath = Path.Combine(App.ConfigPath, "accounts.json");
                var json = JsonConvert.SerializeObject(_accounts.Values.ToList(), Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Failed to save accounts");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            SaveAccounts();
            App.Logger.Information("MultiAccountOrchestrator disposed");
        }

        #endregion
    }

    #region Models

    public class AccountConfig
    {
        public string AccountId { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string ServerUrl { get; set; } = "";
        public string Username { get; set; } = "";
        public string AccountType { get; set; } = "main"; // main, farm, alt
    }

    public class AccountSession
    {
        public string AccountId { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string ServerUrl { get; set; } = "";
        public string Username { get; set; } = "";
        public string AccountType { get; set; } = "main";
        public AccountStatus Status { get; set; } = AccountStatus.Offline;
        public long Power { get; set; }
        public ResourceState Resources { get; set; } = new ResourceState();
        public int CityCount { get; set; }
        public DateTime LastActivity { get; set; }
        public bool AutoPilotEnabled { get; set; }
    }

    public class AccountData
    {
        public long Power { get; set; }
        public ResourceState Resources { get; set; } = new ResourceState();
        public int CityCount { get; set; }
    }

    public enum AccountStatus
    {
        Offline,
        Online,
        Active,
        Idle,
        AutoPilot
    }

    public class CoordinatedAction
    {
        public string ActionId { get; set; } = "";
        public string ActionType { get; set; } = "";
        public List<string> ParticipatingAccounts { get; set; } = new List<string>();
        public string TargetAccountId { get; set; }
        public int TargetX { get; set; }
        public int TargetY { get; set; }
        public string TroopType { get; set; }
        public int Amount { get; set; }
        public DateTime ScheduledTime { get; set; }
        public DateTime? CompletedTime { get; set; }
        public string Status { get; set; } = "pending";
        public string Error { get; set; }
    }

    public class AccountRecommendation
    {
        public string AccountId { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string RecommendationType { get; set; } = "";
        public string Description { get; set; } = "";
        public int Priority { get; set; }
    }

    #endregion

}