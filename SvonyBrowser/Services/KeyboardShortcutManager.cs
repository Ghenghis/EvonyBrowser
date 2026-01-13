using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace SvonyBrowser.Services
{
    /// <summary>
    /// Keyboard shortcut management for Svony Browser v6.0 Borg Edition.
    /// Provides customizable keyboard shortcuts for all major actions.
    /// </summary>
    public sealed class KeyboardShortcutManager : IDisposable
    {
        private static readonly Lazy<KeyboardShortcutManager> _instance = new Lazy<KeyboardShortcutManager>(() => new KeyboardShortcutManager());
        public static KeyboardShortcutManager Instance => _instance.Value;
        
        private readonly Dictionary<string, KeyboardShortcut> _shortcuts;
        private readonly Dictionary<string, Action> _actions;
        
        public event EventHandler<ShortcutTriggeredEventArgs> ShortcutTriggered;
        
        private KeyboardShortcutManager()
        {
            _shortcuts = new Dictionary<string, KeyboardShortcut>();
            _actions = new Dictionary<string, Action>();
            InitializeDefaultShortcuts();
        }
        
        #region Default Shortcuts
        
        private void InitializeDefaultShortcuts()
        {
            // Navigation
            RegisterShortcut("Navigate.Back", Key.Left, ModifierKeys.Alt, "Navigate Back");
            RegisterShortcut("Navigate.Forward", Key.Right, ModifierKeys.Alt, "Navigate Forward");
            RegisterShortcut("Navigate.Reload", Key.F5, ModifierKeys.None, "Reload Page");
            RegisterShortcut("Navigate.Home", Key.Home, ModifierKeys.Alt, "Go to Home");
            RegisterShortcut("Navigate.Stop", Key.Escape, ModifierKeys.None, "Stop Loading");
            
            // Browser
            RegisterShortcut("Browser.NewTab", Key.T, ModifierKeys.Control, "New Tab");
            RegisterShortcut("Browser.CloseTab", Key.W, ModifierKeys.Control, "Close Tab");
            RegisterShortcut("Browser.SwitchPanel", Key.Tab, ModifierKeys.Control, "Switch Panel");
            RegisterShortcut("Browser.FullScreen", Key.F11, ModifierKeys.None, "Toggle Full Screen");
            RegisterShortcut("Browser.ZoomIn", Key.OemPlus, ModifierKeys.Control, "Zoom In");
            RegisterShortcut("Browser.ZoomOut", Key.OemMinus, ModifierKeys.Control, "Zoom Out");
            RegisterShortcut("Browser.ZoomReset", Key.D0, ModifierKeys.Control, "Reset Zoom");
            
            // DevTools
            RegisterShortcut("DevTools.Toggle", Key.F12, ModifierKeys.None, "Toggle DevTools");
            RegisterShortcut("DevTools.Console", Key.J, ModifierKeys.Control | ModifierKeys.Shift, "Open Console");
            RegisterShortcut("DevTools.Network", Key.E, ModifierKeys.Control | ModifierKeys.Shift, "Open Network Tab");
            
            // Chatbot
            RegisterShortcut("Chatbot.Toggle", Key.B, ModifierKeys.Control, "Toggle Chatbot Panel");
            RegisterShortcut("Chatbot.Focus", Key.OemQuestion, ModifierKeys.Control, "Focus Chat Input");
            RegisterShortcut("Chatbot.Clear", Key.L, ModifierKeys.Control | ModifierKeys.Shift, "Clear Chat History");
            RegisterShortcut("Chatbot.Send", Key.Enter, ModifierKeys.Control, "Send Message");
            
            // Traffic
            RegisterShortcut("Traffic.Toggle", Key.T, ModifierKeys.Control | ModifierKeys.Shift, "Toggle Traffic Viewer");
            RegisterShortcut("Traffic.Capture", Key.C, ModifierKeys.Control | ModifierKeys.Shift, "Toggle Capture");
            RegisterShortcut("Traffic.Clear", Key.Delete, ModifierKeys.Control | ModifierKeys.Shift, "Clear Traffic");
            RegisterShortcut("Traffic.Export", Key.S, ModifierKeys.Control | ModifierKeys.Shift, "Export Traffic");
            
            // Protocol
            RegisterShortcut("Protocol.Toggle", Key.P, ModifierKeys.Control | ModifierKeys.Shift, "Toggle Protocol Explorer");
            RegisterShortcut("Protocol.Search", Key.F, ModifierKeys.Control, "Search Protocols");
            RegisterShortcut("Protocol.Execute", Key.Enter, ModifierKeys.None, "Execute Selected Action");
            
            // Automation
            RegisterShortcut("Automation.Toggle", Key.A, ModifierKeys.Control | ModifierKeys.Shift, "Toggle AutoPilot");
            RegisterShortcut("Automation.Pause", Key.Space, ModifierKeys.Control, "Pause Automation");
            RegisterShortcut("Automation.Stop", Key.Escape, ModifierKeys.Control, "Stop All Automation");
            
            // MCP
            RegisterShortcut("Mcp.Reconnect", Key.R, ModifierKeys.Control | ModifierKeys.Shift, "Reconnect MCP Servers");
            RegisterShortcut("Mcp.Status", Key.M, ModifierKeys.Control | ModifierKeys.Shift, "Show MCP Status");
            
            // Settings
            RegisterShortcut("Settings.Open", Key.OemComma, ModifierKeys.Control, "Open Settings");
            RegisterShortcut("Settings.Shortcuts", Key.K, ModifierKeys.Control, "Keyboard Shortcuts");
            
            // Window
            RegisterShortcut("Window.Minimize", Key.M, ModifierKeys.Control | ModifierKeys.Alt, "Minimize Window");
            RegisterShortcut("Window.Maximize", Key.X, ModifierKeys.Control | ModifierKeys.Alt, "Maximize Window");
            RegisterShortcut("Window.Close", Key.Q, ModifierKeys.Control, "Close Application");
            
            // Quick Actions
            RegisterShortcut("Quick.Screenshot", Key.PrintScreen, ModifierKeys.None, "Take Screenshot");
            RegisterShortcut("Quick.Record", Key.R, ModifierKeys.Control | ModifierKeys.Alt, "Toggle Recording");
            RegisterShortcut("Quick.Help", Key.F1, ModifierKeys.None, "Show Help");
        }
        
        #endregion
        
        #region Registration
        
        /// <summary>
        /// Registers a keyboard shortcut.
        /// </summary>
        public void RegisterShortcut(string id, Key key, ModifierKeys modifiers, string description)
        {
            _shortcuts[id] = new KeyboardShortcut
            {
                Id = id,
                Key = key,
                Modifiers = modifiers,
                Description = description,
                IsEnabled = true
            };
        }
        
        /// <summary>
        /// Registers an action handler for a shortcut.
        /// </summary>
        public void RegisterAction(string shortcutId, Action action)
        {
            _actions[shortcutId] = action;
        }
        
        /// <summary>
        /// Unregisters a shortcut.
        /// </summary>
        public void UnregisterShortcut(string id)
        {
            _shortcuts.Remove(id);
            _actions.Remove(id);
        }
        
        #endregion
        
        #region Shortcut Handling
        
        /// <summary>
        /// Processes a key event and triggers matching shortcuts.
        /// </summary>
        public bool ProcessKeyDown(Key key, ModifierKeys modifiers)
        {
            foreach (var kvp in _shortcuts)
            {
                var shortcut = kvp.Value;
                
                if (!shortcut.IsEnabled)
                    continue;
                
                if (shortcut.Key == key && shortcut.Modifiers == modifiers)
                {
                    TriggerShortcut(shortcut.Id);
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Triggers a shortcut by ID.
        /// </summary>
        public void TriggerShortcut(string id)
        {
            if (_actions.TryGetValue(id, out var action))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    ErrorHandler.Instance.Handle(ex, $"Shortcut: {id}");
                }
            }
            
            if (_shortcuts.TryGetValue(id, out var shortcut))
            {
                ShortcutTriggered?.Invoke(this, new ShortcutTriggeredEventArgs(shortcut));
            }
        }
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// Gets all registered shortcuts.
        /// </summary>
        public IEnumerable<KeyboardShortcut> GetAllShortcuts()
        {
            return _shortcuts.Values;
        }
        
        /// <summary>
        /// Gets a shortcut by ID.
        /// </summary>
        public KeyboardShortcut? GetShortcut(string id)
        {
            return _shortcuts.TryGetValue(id, out var shortcut) ? shortcut : null;
        }
        
        /// <summary>
        /// Updates a shortcut's key binding.
        /// </summary>
        public bool UpdateShortcut(string id, Key key, ModifierKeys modifiers)
        {
            if (!_shortcuts.TryGetValue(id, out var shortcut))
                return false;
            
            // Check for conflicts
            foreach (var kvp in _shortcuts)
            {
                if (kvp.Key != id && kvp.Value.Key == key && kvp.Value.Modifiers == modifiers)
                {
                    return false; // Conflict
                }
            }
            
            shortcut.Key = key;
            shortcut.Modifiers = modifiers;
            return true;
        }
        
        /// <summary>
        /// Enables or disables a shortcut.
        /// </summary>
        public void SetEnabled(string id, bool enabled)
        {
            if (_shortcuts.TryGetValue(id, out var shortcut))
            {
                shortcut.IsEnabled = enabled;
            }
        }
        
        /// <summary>
        /// Resets all shortcuts to defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            _shortcuts.Clear();
            InitializeDefaultShortcuts();
        }
        
        #endregion
        
        #region Serialization
        
        /// <summary>
        /// Exports shortcuts to a dictionary for saving.
        /// </summary>
        public Dictionary<string, ShortcutBinding> ExportBindings()
        {
            var bindings = new Dictionary<string, ShortcutBinding>();
            
            foreach (var kvp in _shortcuts)
            {
                bindings[kvp.Key] = new ShortcutBinding
                {
                    Key = kvp.Value.Key.ToString(),
                    Modifiers = kvp.Value.Modifiers.ToString(),
                    IsEnabled = kvp.Value.IsEnabled
                };
            }
            
            return bindings;
        }
        
        /// <summary>
        /// Imports shortcuts from a dictionary.
        /// </summary>
        public void ImportBindings(Dictionary<string, ShortcutBinding> bindings)
        {
            foreach (var kvp in bindings)
            {
                if (_shortcuts.TryGetValue(kvp.Key, out var shortcut))
                {
                    if (Enum.TryParse<Key>(kvp.Value.Key, out var key))
                    {
                        shortcut.Key = key;
                    }
                    
                    if (Enum.TryParse<ModifierKeys>(kvp.Value.Modifiers, out var modifiers))
                    {
                        shortcut.Modifiers = modifiers;
                    }
                    
                    shortcut.IsEnabled = kvp.Value.IsEnabled;
                }
            }
        }
        
        #endregion
        
        #region Helpers
        
        /// <summary>
        /// Gets a human-readable string for a shortcut.
        /// </summary>
        public static string FormatShortcut(Key key, ModifierKeys modifiers)
        {
            var parts = new List<string>();
            
            if (modifiers.HasFlag(ModifierKeys.Control))
                parts.Add("Ctrl");
            if (modifiers.HasFlag(ModifierKeys.Alt))
                parts.Add("Alt");
            if (modifiers.HasFlag(ModifierKeys.Shift))
                parts.Add("Shift");
            if (modifiers.HasFlag(ModifierKeys.Windows))
                parts.Add("Win");
            
            parts.Add(FormatKey(key));
            
            return string.Join("+", parts);
        }
        
        private static string FormatKey(Key key)
        {
            return key switch
            {
                Key.OemPlus => "+",
                Key.OemMinus => "-",
                Key.OemComma => ",",
                Key.OemPeriod => ".",
                Key.OemQuestion => "?",
                Key.D0 => "0",
                Key.D1 => "1",
                Key.D2 => "2",
                Key.D3 => "3",
                Key.D4 => "4",
                Key.D5 => "5",
                Key.D6 => "6",
                Key.D7 => "7",
                Key.D8 => "8",
                Key.D9 => "9",
                _ => key.ToString()
            };
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    public class KeyboardShortcut
    {
        public string Id { get; set; } = "";
        public Key Key { get; set; }
        public ModifierKeys Modifiers { get; set; }
        public string Description { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
        
        public string DisplayString => KeyboardShortcutManager.FormatShortcut(Key, Modifiers);
    }
    
    public class ShortcutBinding
    {
        public string Key { get; set; } = "";
        public string Modifiers { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
    }
    
    public class ShortcutTriggeredEventArgs : EventArgs
    {
        public KeyboardShortcut Shortcut { get; }
        public DateTime Timestamp { get; }
        
        public ShortcutTriggeredEventArgs(KeyboardShortcut shortcut)
        {
            Shortcut = shortcut;
            Timestamp = DateTime.Now;
        }
    }
    
    #endregion

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        App.Logger?.Debug("KeyboardShortcutManager disposed");
    }

    #endregion
}
