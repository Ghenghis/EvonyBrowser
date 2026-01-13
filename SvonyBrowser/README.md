# Svony Browser

**Dual-panel Flash browser for running AutoEvony bot and EvonyClient side-by-side with shared session authentication.**

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows%20x64-lightgrey)
![.NET](https://img.shields.io/badge/.NET-6.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

| Feature | Description |
|---------|-------------|
| **Dual Panel View** | Run AutoEvony and EvonyClient simultaneously |
| **Session Sharing** | Login once, both panels authenticate automatically |
| **Fiddler Integration** | All traffic routed through Fiddler for inspection |
| **Panel Controls** | Show/hide panels, swap positions, resize with splitter |
| **Keyboard Shortcuts** | Quick access to common actions |
| **Dark Theme** | Modern dark UI for extended use |
| **Settings Persistence** | Remember panel layout between sessions |

## Quick Start

### Prerequisites

1. **.NET 6.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/6.0)
2. **Fiddler Classic** - [Download](https://www.telerik.com/fiddler/fiddler-classic)
3. **Flash SWF Files**:
   - `AutoEvony.swf` - AutoEvony bot
   - `EvonyClient1921.swf` - Official Evony client

### Installation

```powershell
# 1. Clone or download to D:\Fiddler-FlashBrowser\

# 2. Ensure SWF files are in place:
#    D:\Fiddler-FlashBrowser\AutoEvony.swf
#    D:\Fiddler-FlashBrowser\EvonyClient1921.swf

# 3. Build the application
cd D:\Fiddler-FlashBrowser
.\Build-SvonyBrowser.bat

# 4. Launch
.\Launch-SvonyBrowser.bat
```

### Manual Build

```powershell
cd D:\Fiddler-FlashBrowser\SvonyBrowser
dotnet restore
dotnet build -c Release
```

## How Session Sharing Works

```
┌─────────────────────────────────────────────────────────────┐
│                      Svony Browser                          │
├─────────────────────┬───────────────────────────────────────┤
│   AutoEvony Panel   │          EvonyClient Panel            │
│   (Left Browser)    │          (Right Browser)              │
│                     │                                       │
│   ┌─────────────┐   │   ┌─────────────┐                     │
│   │ CefSharp    │   │   │ CefSharp    │                     │
│   │ Browser     │   │   │ Browser     │                     │
│   └──────┬──────┘   │   └──────┬──────┘                     │
│          │          │          │                            │
│          └──────────┴──────────┘                            │
│                     │                                       │
│              ┌──────▼──────┐                                │
│              │ SHARED CACHE │  ← Session sync happens here  │
│              │ - Cookies    │                               │
│              │ - SOL files  │                               │
│              │ - Preferences│                               │
│              └──────┬──────┘                                │
│                     │                                       │
│              ┌──────▼──────┐                                │
│              │   Fiddler   │  ← Traffic capture             │
│              │   Proxy     │                                │
│              │ 127.0.0.1:  │                                │
│              │    8888     │                                │
│              └──────┬──────┘                                │
│                     │                                       │
└─────────────────────┼───────────────────────────────────────┘
                      │
               ┌──────▼──────┐
               │ Evony Server│
               │ cc1-cc5     │
               └─────────────┘
```

**Key Insight:** Both browser panels use the same `CachePath`. When you login via either panel, the session token is stored in shared SOL files. The other panel automatically reads this session and authenticates without requiring a separate login.

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+1` | Show Bot panel only |
| `Ctrl+2` | Show both panels |
| `Ctrl+3` | Show Client panel only |
| `Ctrl+S` | Swap panel positions |
| `Ctrl+R` | Reload focused panel |
| `F5` | Reload left panel |
| `F6` | Reload right panel |

## Project Structure

```
D:\Fiddler-FlashBrowser\
├── SvonyBrowser/                  # Main application
│   ├── App.xaml(.cs)              # Application entry point, CefSharp init
│   ├── MainWindow.xaml(.cs)       # Main UI with dual browser panels
│   ├── SettingsWindow.xaml(.cs)   # Settings dialog
│   ├── GlobalUsings.cs            # Global using directives
│   ├── app.manifest               # UAC and DPI settings
│   ├── SvonyBrowser.csproj        # Project file
│   ├── Services/
│   │   ├── SessionManager.cs      # Session state management
│   │   └── ProxyMonitor.cs        # Fiddler proxy monitoring
│   ├── docs/
│   │   ├── ARCHITECTURE.md        # Technical architecture
│   │   ├── diagrams/              # SVG diagrams
│   │   └── ...
│   └── bin/Release/               # Build output
│       └── net6.0-windows/
│           ├── SvonyBrowser.exe   # Main executable
│           ├── Cache/             # Shared browser cache
│           ├── Logs/              # Application logs
│           ├── config/            # Settings storage
│           └── Plugins/           # Flash plugin (copied from FlashBrowser_x64)
├── FlashBrowser_x64/              # Flash plugin source
│   └── Assets/Plugins/
│       └── pepflashplayer.dll     # Flash PPAPI plugin
├── Fiddler/                       # Fiddler Classic installation
│   └── Fiddler.exe
├── AutoEvony.swf                  # AutoEvony bot SWF
├── EvonyClient1921.swf            # Evony client SWF
├── Build-SvonyBrowser.bat         # Build script
├── Launch-SvonyBrowser.bat        # Launch script
└── Launch-SvonyBrowser.ps1        # PowerShell launch script
```

## Configuration

Settings are stored in `config/svony-settings.json`:

```json
{
  "DefaultServer": "cc2.evony.com",
  "ProxyHost": "127.0.0.1",
  "ProxyPort": 8888,
  "AutoStartFiddler": true,
  "RememberPanelLayout": true,
  "LeftPanelWidth": 0.5,
  "LastPanelMode": "Both",
  "EnableLogging": true,
  "Theme": "Dark"
}
```

## Troubleshooting

### Flash Not Loading

1. Ensure `pepflashplayer.dll` exists in one of:
   - `FlashBrowser_x64\Assets\Plugins\`
   - `SvonyBrowser\bin\Release\net6.0-windows\Plugins\`

2. Run the build script to copy the plugin:
   ```powershell
   .\Build-SvonyBrowser.bat
   ```

### Proxy Connection Failed

1. Ensure Fiddler Classic is running
2. Check Fiddler is listening on `127.0.0.1:8888`
3. In Fiddler: Tools → Options → Connections → "Allow remote computers to connect" 

### SWF Files Not Found

Place the following files in `D:\Fiddler-FlashBrowser\`:
- `AutoEvony.swf`
- `EvonyClient1921.swf`

### Build Errors

```powershell
# Clean and rebuild
cd D:\Fiddler-FlashBrowser\SvonyBrowser
dotnet clean
dotnet restore
dotnet build -c Release
```

### High Memory Usage

CefSharp creates multiple processes. This is normal behavior for Chromium-based browsers. To minimize:
- Close unused panels (Ctrl+1 or Ctrl+3)
- Clear cache periodically (toolbar button)

## Technology Stack

| Component | Version | Purpose |
|-----------|---------|---------|
| .NET | 6.0 | Runtime framework |
| WPF | - | UI framework |
| CefSharp | 119.4.30 | Chromium browser control |
| Flash PPAPI | 32.0.0.465 | Flash player plugin |
| Serilog | 3.1.1 | Logging |
| Newtonsoft.Json | 13.0.3 | JSON serialization |

## Logs

Application logs are written to `Logs/svony-{date}.log`:
- Rolling daily logs
- 7-day retention
- 10MB max per file

## License

MIT License - See LICENSE file for details.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## Acknowledgments

- CefSharp team for the Chromium .NET bindings
- Fiddler/Telerik for the proxy debugging tool
- Evony community for protocol documentation
