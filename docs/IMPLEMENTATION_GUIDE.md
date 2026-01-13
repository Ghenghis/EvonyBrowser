# Svony Browser - Implementation Guide

## Overview

This guide provides step-by-step instructions for Claude Desktop to build the Svony Browser application using the existing Fiddler-FlashBrowser infrastructure.

---

## Prerequisites

### Existing Components (Already Available)

| Component           | Location                                             | Status  |
| ------------------- | ---------------------------------------------------- | ------- |
| AutoEvony.swf       | `D:\Fiddler-FlashBrowser\AutoEvony.swf`              | ✅ Ready |
| EvonyClient1921.swf | `D:\Fiddler-FlashBrowser\EvonyClient1921.swf`        | ✅ Ready |
| CefFlashBrowser     | `D:\Fiddler-FlashBrowser\FlashBrowser_x64\`          | ✅ Ready |
| Fiddler Classic     | `D:\Fiddler-FlashBrowser\Fiddler\`                   | ✅ Ready |
| Custom Rules        | `D:\Fiddler-FlashBrowser\config\`                    | ✅ Ready |
| Flash Plugin        | `FlashBrowser_x64\Assets\Plugins\pepflashplayer.dll` | ✅ Ready |

### Development Tools Needed
- Visual Studio 2022 (Community edition is fine)
- .NET 6.0 SDK or .NET Framework 4.7.2+
- Git (optional)

---

## Phase 1: Project Setup

### Step 1.1: Create Solution Structure

```powershell
# Create project directory
mkdir D:\Fiddler-FlashBrowser\SvonyBrowser
cd D:\Fiddler-FlashBrowser\SvonyBrowser

# Create solution (via Visual Studio or dotnet CLI)
dotnet new sln -n SvonyBrowser
dotnet new wpf -n SvonyBrowser -f net6.0-windows
dotnet sln add SvonyBrowser/SvonyBrowser.csproj
```

### Step 1.2: Add NuGet Packages

Edit `SvonyBrowser.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CefSharp.Wpf" Version="119.4.30" />
    <PackageReference Include="CefSharp.Common" Version="119.4.30" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="Serilog" Version="3.1.1" />
    <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
  </ItemGroup>
</Project>
```

### Step 1.3: Initialize CefSharp

Create `App.xaml.cs`:
```csharp
using System;
using System.IO;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;

namespace SvonyBrowser
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            InitializeCef();
        }

        private void InitializeCef()
        {
            var settings = new CefSettings
            {
                // Shared cache for both panels
                CachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache"),
                PersistSessionCookies = true,
                PersistUserPreferences = true,
                LogSeverity = LogSeverity.Warning,
                LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "cef.log")
            };

            // Proxy through Fiddler
            settings.CefCommandLineArgs.Add("proxy-server", "127.0.0.1:8888");
            
            // Flash plugin path
            var flashPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                "..", "FlashBrowser_x64", "Assets", "Plugins", "pepflashplayer.dll"
            );
            
            if (File.Exists(flashPath))
            {
                settings.CefCommandLineArgs.Add("ppapi-flash-path", flashPath);
                settings.CefCommandLineArgs.Add("ppapi-flash-version", "32.0.0.465");
            }

            // Allow plugins
            settings.CefCommandLineArgs.Add("enable-npapi", "1");
            settings.CefCommandLineArgs.Add("allow-outdated-plugins", "1");
            
            // Disable sandbox for Flash compatibility
            settings.CefCommandLineArgs.Add("no-sandbox", "1");
            settings.CefCommandLineArgs.Add("disable-web-security", "1");

            Cef.Initialize(settings);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Cef.Shutdown();
            base.OnExit(e);
        }
    }
}
```

---

## Phase 2: Main Window Layout

### Step 2.1: Create Split Panel XAML

Create `MainWindow.xaml`:
```xml
<Window x:Class="SvonyBrowser.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:cef="clr-namespace:CefSharp.Wpf;assembly=CefSharp.Wpf"
        Title="Svony Browser" 
        Height="800" Width="1400"
        WindowState="Maximized"
        Background="#1e1e1e">
    
    <Window.Resources>
        <Style TargetType="Button" x:Key="ToolbarButton">
            <Setter Property="Background" Value="#333"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Padding" Value="10,5"/>
            <Setter Property="Margin" Value="2"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
    </Window.Resources>
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Toolbar -->
        <Border Grid.Row="0" Background="#2d2d2d" Padding="5">
            <StackPanel Orientation="Horizontal">
                <Button Content="◀ Left Only" Click="ShowLeftOnly_Click" Style="{StaticResource ToolbarButton}"/>
                <Button Content="Both Panels" Click="ShowBoth_Click" Style="{StaticResource ToolbarButton}"/>
                <Button Content="Right Only ▶" Click="ShowRightOnly_Click" Style="{StaticResource ToolbarButton}"/>
                <Separator Width="20"/>
                <Button Content="⇄ Swap Panels" Click="SwapPanels_Click" Style="{StaticResource ToolbarButton}"/>
                <Separator Width="20"/>
                <ComboBox x:Name="ServerSelector" Width="120" SelectedIndex="1">
                    <ComboBoxItem Content="cc1.evony.com"/>
                    <ComboBoxItem Content="cc2.evony.com"/>
                    <ComboBoxItem Content="cc3.evony.com"/>
                </ComboBox>
                <Separator Width="20"/>
                <Button Content="⚙ Settings" Click="Settings_Click" Style="{StaticResource ToolbarButton}"/>
                <Button Content="📁 SOL Editor" Click="SolEditor_Click" Style="{StaticResource ToolbarButton}"/>
            </StackPanel>
        </Border>
        
        <!-- Main Content Area -->
        <Grid Grid.Row="1" x:Name="ContentGrid">
            <Grid.ColumnDefinitions>
                <ColumnDefinition x:Name="LeftColumn" Width="*"/>
                <ColumnDefinition Width="5"/>
                <ColumnDefinition x:Name="RightColumn" Width="*"/>
            </Grid.ColumnDefinitions>
            
            <!-- Left Panel: AutoEvony -->
            <Border Grid.Column="0" x:Name="LeftPanel" Background="#1a1a1a" Margin="2">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    
                    <Border Grid.Row="0" Background="#2a5a2a" Padding="8">
                        <TextBlock Text="AutoEvony Bot" Foreground="White" FontWeight="Bold"/>
                    </Border>
                    
                    <cef:ChromiumWebBrowser Grid.Row="1" 
                                           x:Name="LeftBrowser"
                                           Address="about:blank"/>
                </Grid>
            </Border>
            
            <!-- GridSplitter -->
            <GridSplitter Grid.Column="1" 
                         Width="5" 
                         HorizontalAlignment="Stretch"
                         Background="#444"
                         Cursor="SizeWE"/>
            
            <!-- Right Panel: Evony Client -->
            <Border Grid.Column="2" x:Name="RightPanel" Background="#1a1a1a" Margin="2">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    
                    <Border Grid.Row="0" Background="#2a2a5a" Padding="8">
                        <TextBlock Text="Evony Client" Foreground="White" FontWeight="Bold"/>
                    </Border>
                    
                    <cef:ChromiumWebBrowser Grid.Row="1" 
                                           x:Name="RightBrowser"
                                           Address="about:blank"/>
                </Grid>
            </Border>
        </Grid>
        
        <!-- Status Bar -->
        <Border Grid.Row="2" Background="#2d2d2d" Padding="5">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                
                <TextBlock x:Name="StatusText" 
                          Text="Ready" 
                          Foreground="#888"/>
                <TextBlock Grid.Column="1" 
                          x:Name="SessionStatus" 
                          Text="Session: Disconnected" 
                          Foreground="#f88" 
                          Margin="20,0"/>
                <TextBlock Grid.Column="2" 
                          x:Name="PacketCount" 
                          Text="Packets: 0" 
                          Foreground="#888"/>
            </Grid>
        </Border>
    </Grid>
</Window>
```

### Step 2.2: Implement Code-Behind

Create `MainWindow.xaml.cs`:
```csharp
using System;
using System.IO;
using System.Windows;
using CefSharp;

namespace SvonyBrowser
{
    public partial class MainWindow : Window
    {
        private string _autoEvonyPath;
        private string _evonyClientPath;
        private bool _panelsSwapped = false;

        public MainWindow()
        {
            InitializeComponent();
            
            // Set SWF paths
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _autoEvonyPath = Path.GetFullPath(Path.Combine(baseDir, "..", "AutoEvony.swf"));
            _evonyClientPath = Path.GetFullPath(Path.Combine(baseDir, "..", "EvonyClient1921.swf"));
            
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSwfFiles();
            StatusText.Text = "SWF files loaded. Configure Fiddler proxy to capture traffic.";
        }

        private void LoadSwfFiles()
        {
            // Load AutoEvony in left panel
            if (File.Exists(_autoEvonyPath))
            {
                LeftBrowser.Address = $"file:///{_autoEvonyPath.Replace('\\', '/')}";
            }
            else
            {
                LeftBrowser.LoadHtml($"<html><body style='color:white;background:#1a1a1a;'>" +
                    $"<h2>AutoEvony.swf not found</h2><p>Expected at: {_autoEvonyPath}</p></body></html>");
            }

            // Load Evony Client in right panel
            if (File.Exists(_evonyClientPath))
            {
                RightBrowser.Address = $"file:///{_evonyClientPath.Replace('\\', '/')}";
            }
            else
            {
                RightBrowser.LoadHtml($"<html><body style='color:white;background:#1a1a1a;'>" +
                    $"<h2>EvonyClient1921.swf not found</h2><p>Expected at: {_evonyClientPath}</p></body></html>");
            }
        }

        private void ShowLeftOnly_Click(object sender, RoutedEventArgs e)
        {
            LeftColumn.Width = new GridLength(1, GridUnitType.Star);
            RightColumn.Width = new GridLength(0);
            RightPanel.Visibility = Visibility.Collapsed;
            LeftPanel.Visibility = Visibility.Visible;
        }

        private void ShowRightOnly_Click(object sender, RoutedEventArgs e)
        {
            LeftColumn.Width = new GridLength(0);
            RightColumn.Width = new GridLength(1, GridUnitType.Star);
            LeftPanel.Visibility = Visibility.Collapsed;
            RightPanel.Visibility = Visibility.Visible;
        }

        private void ShowBoth_Click(object sender, RoutedEventArgs e)
        {
            LeftColumn.Width = new GridLength(1, GridUnitType.Star);
            RightColumn.Width = new GridLength(1, GridUnitType.Star);
            LeftPanel.Visibility = Visibility.Visible;
            RightPanel.Visibility = Visibility.Visible;
        }

        private void SwapPanels_Click(object sender, RoutedEventArgs e)
        {
            _panelsSwapped = !_panelsSwapped;
            
            var leftAddr = LeftBrowser.Address;
            var rightAddr = RightBrowser.Address;
            
            LeftBrowser.Address = rightAddr;
            RightBrowser.Address = leftAddr;
            
            StatusText.Text = _panelsSwapped ? "Panels swapped" : "Panels restored";
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings dialog - TODO", "Settings", MessageBoxButton.OK);
        }

        private void SolEditor_Click(object sender, RoutedEventArgs e)
        {
            // Launch the SOL editor from CefFlashBrowser
            var solEditorPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                "..", "FlashBrowser_x64", "SolEditor.exe"
            );
            
            if (File.Exists(solEditorPath))
            {
                System.Diagnostics.Process.Start(solEditorPath);
            }
            else
            {
                MessageBox.Show("SOL Editor not found. Use the built-in browser SOL tools.", 
                    "SOL Editor", MessageBoxButton.OK);
            }
        }
    }
}
```

---

## Phase 3: Session Sharing

### Step 3.1: Create Session Manager

Create `Services/SessionManager.cs`:
```csharp
using System;
using System.IO;
using Newtonsoft.Json;

namespace SvonyBrowser.Services
{
    public class SessionManager
    {
        private static SessionManager? _instance;
        public static SessionManager Instance => _instance ??= new SessionManager();

        public string CachePath { get; private set; }
        public string SolPath { get; private set; }
        public bool IsSessionActive { get; private set; }
        public string? CurrentServer { get; private set; }
        public string? SessionToken { get; private set; }

        public event Action<bool>? SessionStateChanged;

        private SessionManager()
        {
            CachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");
            SolPath = Path.Combine(CachePath, "Pepper Data", "Shockwave Flash");
            
            Directory.CreateDirectory(CachePath);
            Directory.CreateDirectory(SolPath);
        }

        public void SetServer(string server)
        {
            CurrentServer = server;
        }

        public void OnLoginDetected(string token)
        {
            SessionToken = token;
            IsSessionActive = true;
            SessionStateChanged?.Invoke(true);
        }

        public void OnLogoutDetected()
        {
            SessionToken = null;
            IsSessionActive = false;
            SessionStateChanged?.Invoke(false);
        }

        public void ExportSession(string filePath)
        {
            var sessionData = new
            {
                Server = CurrentServer,
                Token = SessionToken,
                Timestamp = DateTime.UtcNow,
                CachePath = CachePath
            };

            File.WriteAllText(filePath, JsonConvert.SerializeObject(sessionData, Formatting.Indented));
        }
    }
}
```

### Step 3.2: Create Message Router

Create `Services/MessageRouter.cs`:
```csharp
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace SvonyBrowser.Services
{
    public class GameCommand
    {
        public string Source { get; set; } = "";
        public string Type { get; set; } = "";
        public string Payload { get; set; } = "";
        public int Priority { get; set; } = 5;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string GetHash() => $"{Type}:{Payload}:{Timestamp.Ticks / 10000000}"; // 1 second dedup window
    }

    public class GameState
    {
        public string? PlayerId { get; set; }
        public string? PlayerName { get; set; }
        public int CastleCount { get; set; }
        public long Gold { get; set; }
        public long Food { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class MessageRouter
    {
        private static MessageRouter? _instance;
        public static MessageRouter Instance => _instance ??= new MessageRouter();

        private readonly ConcurrentDictionary<string, DateTime> _recentCommands = new();
        private readonly ConcurrentQueue<GameCommand> _commandQueue = new();
        private readonly Timer _cleanupTimer;

        public event Action<GameCommand>? OnCommandRouted;
        public event Action<GameState>? OnStateUpdate;

        public GameState CurrentState { get; private set; } = new();

        private MessageRouter()
        {
            // Clean up old commands every 5 seconds
            _cleanupTimer = new Timer(CleanupOldCommands, null, 5000, 5000);
        }

        public bool RouteCommand(GameCommand command)
        {
            var hash = command.GetHash();
            
            // Deduplicate
            if (_recentCommands.ContainsKey(hash))
                return false;

            _recentCommands[hash] = command.Timestamp;
            _commandQueue.Enqueue(command);
            
            OnCommandRouted?.Invoke(command);
            return true;
        }

        public void UpdateState(GameState newState)
        {
            CurrentState = newState;
            CurrentState.LastUpdate = DateTime.UtcNow;
            OnStateUpdate?.Invoke(CurrentState);
        }

        private void CleanupOldCommands(object? state)
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-5);
            
            foreach (var kvp in _recentCommands)
            {
                if (kvp.Value < cutoff)
                    _recentCommands.TryRemove(kvp.Key, out _);
            }
        }
    }
}
```

---

## Phase 4: Fiddler Integration

### Step 4.1: Verify Fiddler Setup

Ensure the custom rules from `D:\Fiddler-FlashBrowser\config\EvonyRE-CustomRules.cs` are installed:

```powershell
# Copy rules to Fiddler scripts directory
Copy-Item "D:\Fiddler-FlashBrowser\config\EvonyRE-CustomRules.cs" `
    "$env:USERPROFILE\Documents\Fiddler2\Scripts\CustomRules.cs" -Force
```

### Step 4.2: Launch Script

Create `Launch-SvonyBrowser.bat`:
```batch
@echo off
echo Starting Svony Browser...

REM Check if Fiddler is running
tasklist /FI "IMAGENAME eq Fiddler.exe" 2>NUL | find /I "Fiddler.exe" >NUL
if "%ERRORLEVEL%"=="1" (
    echo Starting Fiddler...
    start "" "D:\Fiddler-FlashBrowser\Fiddler\Fiddler.exe"
    timeout /t 3 /nobreak >NUL
)

REM Start Svony Browser
echo Starting Svony Browser application...
start "" "D:\Fiddler-FlashBrowser\SvonyBrowser\bin\Release\net6.0-windows\SvonyBrowser.exe"

echo Done!
```

### Step 4.3: PowerShell Launcher

Create `Launch-SvonyBrowser.ps1`:
```powershell
param(
    [string]$Server = "cc2",
    [switch]$NoFiddler
)

$BasePath = "D:\Fiddler-FlashBrowser"
$FiddlerPath = "$BasePath\Fiddler\Fiddler.exe"
$BrowserPath = "$BasePath\SvonyBrowser\bin\Release\net6.0-windows\SvonyBrowser.exe"

# Start Fiddler if not running and not disabled
if (-not $NoFiddler) {
    $fiddlerRunning = Get-Process -Name "Fiddler" -ErrorAction SilentlyContinue
    if (-not $fiddlerRunning) {
        Write-Host "Starting Fiddler proxy..." -ForegroundColor Cyan
        Start-Process $FiddlerPath
        Start-Sleep -Seconds 3
    }
}

# Wait for proxy to be ready
Write-Host "Waiting for proxy (127.0.0.1:8888)..." -ForegroundColor Yellow
$maxWait = 10
$waited = 0
while ($waited -lt $maxWait) {
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $tcpClient.Connect("127.0.0.1", 8888)
        $tcpClient.Close()
        Write-Host "Proxy ready!" -ForegroundColor Green
        break
    } catch {
        Start-Sleep -Seconds 1
        $waited++
    }
}

# Start Svony Browser
Write-Host "Starting Svony Browser..." -ForegroundColor Cyan
Start-Process $BrowserPath -ArgumentList "--server=$Server"

Write-Host "Svony Browser launched!" -ForegroundColor Green
```

---

## Phase 5: Testing

### Step 5.1: Build and Test

```powershell
cd D:\Fiddler-FlashBrowser\SvonyBrowser

# Build the project
dotnet build -c Release

# Run tests
dotnet test
```

### Step 5.2: Manual Testing Checklist

- [ ] Both SWF files load correctly
- [ ] Panel resizing works (drag the splitter)
- [ ] "Left Only", "Both", "Right Only" buttons work
- [ ] "Swap Panels" swaps the content
- [ ] Server selector dropdown functions
- [ ] Fiddler captures traffic from both panels
- [ ] Login in one panel shares session with the other
- [ ] SOL files are shared between panels

### Step 5.3: Verify Session Sharing

1. Start Fiddler and Svony Browser
2. Login via AutoEvony (left panel)
3. Observe Fiddler capturing LOGIN packets
4. Check that Evony Client (right panel) shows logged-in state
5. Perform action in either panel
6. Verify action reflects in both panels

---

## Troubleshooting

### Flash Not Loading
```
1. Verify pepflashplayer.dll exists
2. Check CefSharp flash command line args
3. Try 32-bit version if 64-bit fails
4. Check Windows Event Viewer for errors
```

### Proxy Connection Failed
```
1. Verify Fiddler is running
2. Check port 8888 is not blocked
3. Install Fiddler HTTPS certificate
4. Disable Windows Firewall temporarily
```

### Session Not Sharing
```
1. Verify both browsers use same CachePath
2. Check SOL files in Cache directory
3. Clear cache and re-login
4. Check Fiddler for cookie headers
```

---

## Future Enhancements

1. **Command Palette**: Quick access to bot commands
2. **Script Editor**: Built-in script editing with syntax highlighting
3. **Packet Inspector**: Real-time packet analysis panel
4. **Macro Recorder**: Record and replay actions
5. **Multi-Account**: Tab support for multiple accounts
6. **Dark Mode**: Full dark theme support
7. **Hotkeys**: Keyboard shortcuts for common actions

---

## Files to Create Summary

| File                                      | Purpose                      |
| ----------------------------------------- | ---------------------------- |
| `SvonyBrowser.sln`                        | Solution file                |
| `SvonyBrowser/SvonyBrowser.csproj`        | Project file with NuGet refs |
| `SvonyBrowser/App.xaml`                   | Application definition       |
| `SvonyBrowser/App.xaml.cs`                | CefSharp initialization      |
| `SvonyBrowser/MainWindow.xaml`            | Split panel layout           |
| `SvonyBrowser/MainWindow.xaml.cs`         | Window code-behind           |
| `SvonyBrowser/Services/SessionManager.cs` | Session handling             |
| `SvonyBrowser/Services/MessageRouter.cs`  | Command routing              |
| `Launch-SvonyBrowser.bat`                 | Quick launcher               |
| `Launch-SvonyBrowser.ps1`                 | PowerShell launcher          |

---

## Quick Start for Claude Desktop

1. **Create the WPF project** using Visual Studio 2022
2. **Copy the code** from Phase 1-4 above
3. **Build in Release mode**: `dotnet build -c Release`
4. **Run launcher**: `.\Launch-SvonyBrowser.bat`
5. **Test session sharing** between panels

The key insight is that **both CefSharp browsers share the same cache directory**, which means SOL files and cookies are automatically shared. This allows one login to authenticate both panels!
