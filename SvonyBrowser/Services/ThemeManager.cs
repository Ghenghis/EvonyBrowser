using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace SvonyBrowser.Services
{
    /// <summary>
    /// Theme management service for Svony Browser v6.0 Borg Edition.
    /// Provides consistent theming, color schemes, and UI polish.
    /// </summary>
    public sealed class ThemeManager : IDisposable
    {
        private static readonly Lazy<ThemeManager> _instance = new Lazy<ThemeManager>(() => new ThemeManager());
        public static ThemeManager Instance => _instance.Value;
        
        private readonly Dictionary<string, Theme> _themes;
        private Theme _currentTheme;
        
        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;
        
        public Theme CurrentTheme => _currentTheme;
        public string CurrentThemeName => _currentTheme.Name;
        
        private ThemeManager()
        {
            _themes = new Dictionary<string, Theme>();
            InitializeThemes();
            _currentTheme = _themes["Borg Dark"];
        }
        
        #region Theme Definitions
        
        private void InitializeThemes()
        {
            // Borg Dark Theme (Default)
            _themes["Borg Dark"] = new Theme
            {
                Name = "Borg Dark",
                IsDark = true,
                Colors = new ThemeColors
                {
                    Background = Color.FromRgb(18, 18, 18),
                    Surface = Color.FromRgb(30, 30, 30),
                    SurfaceVariant = Color.FromRgb(45, 45, 45),
                    Primary = Color.FromRgb(0, 200, 83),
                    PrimaryVariant = Color.FromRgb(0, 150, 62),
                    Secondary = Color.FromRgb(3, 218, 198),
                    SecondaryVariant = Color.FromRgb(2, 164, 149),
                    Accent = Color.FromRgb(187, 134, 252),
                    Error = Color.FromRgb(207, 102, 121),
                    Warning = Color.FromRgb(255, 183, 77),
                    Success = Color.FromRgb(129, 199, 132),
                    Info = Color.FromRgb(100, 181, 246),
                    OnBackground = Color.FromRgb(255, 255, 255),
                    OnSurface = Color.FromRgb(230, 230, 230),
                    OnPrimary = Color.FromRgb(0, 0, 0),
                    OnSecondary = Color.FromRgb(0, 0, 0),
                    Border = Color.FromRgb(60, 60, 60),
                    Divider = Color.FromRgb(50, 50, 50),
                    Disabled = Color.FromRgb(100, 100, 100),
                    Highlight = Color.FromArgb(40, 0, 200, 83)
                }
            };
            
            // Borg Light Theme
            _themes["Borg Light"] = new Theme
            {
                Name = "Borg Light",
                IsDark = false,
                Colors = new ThemeColors
                {
                    Background = Color.FromRgb(250, 250, 250),
                    Surface = Color.FromRgb(255, 255, 255),
                    SurfaceVariant = Color.FromRgb(240, 240, 240),
                    Primary = Color.FromRgb(0, 150, 62),
                    PrimaryVariant = Color.FromRgb(0, 120, 50),
                    Secondary = Color.FromRgb(2, 164, 149),
                    SecondaryVariant = Color.FromRgb(1, 130, 118),
                    Accent = Color.FromRgb(103, 58, 183),
                    Error = Color.FromRgb(176, 0, 32),
                    Warning = Color.FromRgb(245, 124, 0),
                    Success = Color.FromRgb(46, 125, 50),
                    Info = Color.FromRgb(25, 118, 210),
                    OnBackground = Color.FromRgb(0, 0, 0),
                    OnSurface = Color.FromRgb(30, 30, 30),
                    OnPrimary = Color.FromRgb(255, 255, 255),
                    OnSecondary = Color.FromRgb(255, 255, 255),
                    Border = Color.FromRgb(200, 200, 200),
                    Divider = Color.FromRgb(220, 220, 220),
                    Disabled = Color.FromRgb(180, 180, 180),
                    Highlight = Color.FromArgb(40, 0, 150, 62)
                }
            };
            
            // Evony Classic Theme
            _themes["Evony Classic"] = new Theme
            {
                Name = "Evony Classic",
                IsDark = true,
                Colors = new ThemeColors
                {
                    Background = Color.FromRgb(20, 15, 10),
                    Surface = Color.FromRgb(35, 28, 20),
                    SurfaceVariant = Color.FromRgb(50, 40, 30),
                    Primary = Color.FromRgb(218, 165, 32),
                    PrimaryVariant = Color.FromRgb(184, 134, 11),
                    Secondary = Color.FromRgb(139, 90, 43),
                    SecondaryVariant = Color.FromRgb(101, 67, 33),
                    Accent = Color.FromRgb(255, 215, 0),
                    Error = Color.FromRgb(178, 34, 34),
                    Warning = Color.FromRgb(255, 140, 0),
                    Success = Color.FromRgb(34, 139, 34),
                    Info = Color.FromRgb(70, 130, 180),
                    OnBackground = Color.FromRgb(245, 235, 220),
                    OnSurface = Color.FromRgb(235, 225, 210),
                    OnPrimary = Color.FromRgb(0, 0, 0),
                    OnSecondary = Color.FromRgb(255, 255, 255),
                    Border = Color.FromRgb(80, 60, 40),
                    Divider = Color.FromRgb(60, 45, 30),
                    Disabled = Color.FromRgb(100, 80, 60),
                    Highlight = Color.FromArgb(40, 218, 165, 32)
                }
            };
            
            // Cyberpunk Theme
            _themes["Cyberpunk"] = new Theme
            {
                Name = "Cyberpunk",
                IsDark = true,
                Colors = new ThemeColors
                {
                    Background = Color.FromRgb(10, 10, 20),
                    Surface = Color.FromRgb(20, 20, 35),
                    SurfaceVariant = Color.FromRgb(30, 30, 50),
                    Primary = Color.FromRgb(255, 0, 128),
                    PrimaryVariant = Color.FromRgb(200, 0, 100),
                    Secondary = Color.FromRgb(0, 255, 255),
                    SecondaryVariant = Color.FromRgb(0, 200, 200),
                    Accent = Color.FromRgb(255, 255, 0),
                    Error = Color.FromRgb(255, 50, 50),
                    Warning = Color.FromRgb(255, 165, 0),
                    Success = Color.FromRgb(0, 255, 128),
                    Info = Color.FromRgb(100, 150, 255),
                    OnBackground = Color.FromRgb(255, 255, 255),
                    OnSurface = Color.FromRgb(230, 230, 255),
                    OnPrimary = Color.FromRgb(255, 255, 255),
                    OnSecondary = Color.FromRgb(0, 0, 0),
                    Border = Color.FromRgb(80, 80, 120),
                    Divider = Color.FromRgb(50, 50, 80),
                    Disabled = Color.FromRgb(80, 80, 100),
                    Highlight = Color.FromArgb(40, 255, 0, 128)
                }
            };
            
            // High Contrast Theme
            _themes["High Contrast"] = new Theme
            {
                Name = "High Contrast",
                IsDark = true,
                Colors = new ThemeColors
                {
                    Background = Color.FromRgb(0, 0, 0),
                    Surface = Color.FromRgb(0, 0, 0),
                    SurfaceVariant = Color.FromRgb(20, 20, 20),
                    Primary = Color.FromRgb(0, 255, 0),
                    PrimaryVariant = Color.FromRgb(0, 200, 0),
                    Secondary = Color.FromRgb(255, 255, 0),
                    SecondaryVariant = Color.FromRgb(200, 200, 0),
                    Accent = Color.FromRgb(0, 255, 255),
                    Error = Color.FromRgb(255, 0, 0),
                    Warning = Color.FromRgb(255, 165, 0),
                    Success = Color.FromRgb(0, 255, 0),
                    Info = Color.FromRgb(0, 200, 255),
                    OnBackground = Color.FromRgb(255, 255, 255),
                    OnSurface = Color.FromRgb(255, 255, 255),
                    OnPrimary = Color.FromRgb(0, 0, 0),
                    OnSecondary = Color.FromRgb(0, 0, 0),
                    Border = Color.FromRgb(255, 255, 255),
                    Divider = Color.FromRgb(128, 128, 128),
                    Disabled = Color.FromRgb(100, 100, 100),
                    Highlight = Color.FromArgb(60, 0, 255, 0)
                }
            };
        }
        
        #endregion
        
        #region Theme Operations
        
        /// <summary>
        /// Gets all available theme names.
        /// </summary>
        public IEnumerable<string> GetThemeNames()
        {
            return _themes.Keys;
        }
        
        /// <summary>
        /// Gets a theme by name.
        /// </summary>
        public Theme? GetTheme(string name)
        {
            return _themes.TryGetValue(name, out var theme) ? theme : null;
        }
        
        /// <summary>
        /// Applies a theme by name.
        /// </summary>
        public bool ApplyTheme(string themeName)
        {
            if (!_themes.TryGetValue(themeName, out var theme))
            {
                return false;
            }
            
            var oldTheme = _currentTheme;
            _currentTheme = theme;
            
            // Update application resources
            UpdateApplicationResources(theme);
            
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme));
            
            return true;
        }
        
        private void UpdateApplicationResources(Theme theme)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                var resources = Application.Current.Resources;
                
                // Background colors
                resources["BackgroundColor"] = new SolidColorBrush(theme.Colors.Background);
                resources["SurfaceColor"] = new SolidColorBrush(theme.Colors.Surface);
                resources["SurfaceVariantColor"] = new SolidColorBrush(theme.Colors.SurfaceVariant);
                
                // Primary colors
                resources["PrimaryColor"] = new SolidColorBrush(theme.Colors.Primary);
                resources["PrimaryVariantColor"] = new SolidColorBrush(theme.Colors.PrimaryVariant);
                resources["SecondaryColor"] = new SolidColorBrush(theme.Colors.Secondary);
                resources["SecondaryVariantColor"] = new SolidColorBrush(theme.Colors.SecondaryVariant);
                resources["AccentColor"] = new SolidColorBrush(theme.Colors.Accent);
                
                // Status colors
                resources["ErrorColor"] = new SolidColorBrush(theme.Colors.Error);
                resources["WarningColor"] = new SolidColorBrush(theme.Colors.Warning);
                resources["SuccessColor"] = new SolidColorBrush(theme.Colors.Success);
                resources["InfoColor"] = new SolidColorBrush(theme.Colors.Info);
                
                // Text colors
                resources["OnBackgroundColor"] = new SolidColorBrush(theme.Colors.OnBackground);
                resources["OnSurfaceColor"] = new SolidColorBrush(theme.Colors.OnSurface);
                resources["OnPrimaryColor"] = new SolidColorBrush(theme.Colors.OnPrimary);
                resources["OnSecondaryColor"] = new SolidColorBrush(theme.Colors.OnSecondary);
                
                // Border and divider colors
                resources["BorderColor"] = new SolidColorBrush(theme.Colors.Border);
                resources["DividerColor"] = new SolidColorBrush(theme.Colors.Divider);
                resources["DisabledColor"] = new SolidColorBrush(theme.Colors.Disabled);
                resources["HighlightColor"] = new SolidColorBrush(theme.Colors.Highlight);
            });
        }
        
        /// <summary>
        /// Registers a custom theme.
        /// </summary>
        public void RegisterTheme(Theme theme)
        {
            _themes[theme.Name] = theme;
        }
        
        #endregion
        
        #region Helpers
        
        /// <summary>
        /// Gets a brush for the specified color type.
        /// </summary>
        public SolidColorBrush GetBrush(ThemeColorType colorType)
        {
            var color = colorType switch
            {
                ThemeColorType.Background => _currentTheme.Colors.Background,
                ThemeColorType.Surface => _currentTheme.Colors.Surface,
                ThemeColorType.SurfaceVariant => _currentTheme.Colors.SurfaceVariant,
                ThemeColorType.Primary => _currentTheme.Colors.Primary,
                ThemeColorType.PrimaryVariant => _currentTheme.Colors.PrimaryVariant,
                ThemeColorType.Secondary => _currentTheme.Colors.Secondary,
                ThemeColorType.SecondaryVariant => _currentTheme.Colors.SecondaryVariant,
                ThemeColorType.Accent => _currentTheme.Colors.Accent,
                ThemeColorType.Error => _currentTheme.Colors.Error,
                ThemeColorType.Warning => _currentTheme.Colors.Warning,
                ThemeColorType.Success => _currentTheme.Colors.Success,
                ThemeColorType.Info => _currentTheme.Colors.Info,
                ThemeColorType.OnBackground => _currentTheme.Colors.OnBackground,
                ThemeColorType.OnSurface => _currentTheme.Colors.OnSurface,
                ThemeColorType.OnPrimary => _currentTheme.Colors.OnPrimary,
                ThemeColorType.OnSecondary => _currentTheme.Colors.OnSecondary,
                ThemeColorType.Border => _currentTheme.Colors.Border,
                ThemeColorType.Divider => _currentTheme.Colors.Divider,
                ThemeColorType.Disabled => _currentTheme.Colors.Disabled,
                ThemeColorType.Highlight => _currentTheme.Colors.Highlight,
                _ => _currentTheme.Colors.Primary
            };
            
            return new SolidColorBrush(color);
        }
        
        /// <summary>
        /// Gets a color for the specified color type.
        /// </summary>
        public Color GetColor(ThemeColorType colorType)
        {
            return GetBrush(colorType).Color;
        }
        
        #endregion
        #region IDisposable

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }

        #endregion
    }
    
    #region Supporting Classes
    
    public class Theme
    {
        public string Name { get; set; } = "";
        public bool IsDark { get; set; }
        public ThemeColors Colors { get; set; } = new ThemeColors();
    }
    
    public class ThemeColors
    {
        public Color Background { get; set; }
        public Color Surface { get; set; }
        public Color SurfaceVariant { get; set; }
        public Color Primary { get; set; }
        public Color PrimaryVariant { get; set; }
        public Color Secondary { get; set; }
        public Color SecondaryVariant { get; set; }
        public Color Accent { get; set; }
        public Color Error { get; set; }
        public Color Warning { get; set; }
        public Color Success { get; set; }
        public Color Info { get; set; }
        public Color OnBackground { get; set; }
        public Color OnSurface { get; set; }
        public Color OnPrimary { get; set; }
        public Color OnSecondary { get; set; }
        public Color Border { get; set; }
        public Color Divider { get; set; }
        public Color Disabled { get; set; }
        public Color Highlight { get; set; }
    }
    
    public enum ThemeColorType
    {
        Background,
        Surface,
        SurfaceVariant,
        Primary,
        PrimaryVariant,
        Secondary,
        SecondaryVariant,
        Accent,
        Error,
        Warning,
        Success,
        Info,
        OnBackground,
        OnSurface,
        OnPrimary,
        OnSecondary,
        Border,
        Divider,
        Disabled,
        Highlight
    }
    
    public class ThemeChangedEventArgs : EventArgs
    {
        public Theme OldTheme { get; }
        public Theme NewTheme { get; }
        
        public ThemeChangedEventArgs(Theme oldTheme, Theme newTheme)
        {
            OldTheme = oldTheme;
            NewTheme = newTheme;
        }
    }
    
    #endregion
}
