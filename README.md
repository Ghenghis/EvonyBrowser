# Evony Browser

A specialized Flash browser for Evony built with CefSharp, featuring advanced game analysis and automation capabilities.

## Features

- **Flash Support** - Full Pepper Flash plugin integration for Evony gameplay
- **Game State Engine** - Real-time game state tracking and analysis
- **Map Scanner** - Automated map scanning and resource detection
- **Combat Simulator** - Battle prediction and strategy planning
- **AutoPilot Service** - Automated gameplay assistance
- **Protocol Analysis** - Network traffic analysis and packet inspection
- **Multi-Account Support** - Orchestrate multiple game sessions
- **LLM Integration** - AI-powered game assistance via chatbot
- **Session Recording** - Record and replay game sessions

## Requirements

- Windows 10/11
- .NET Framework 4.6.2
- Visual Studio 2019+ (for building)

## Quick Start

### Running Pre-built Release
1. Navigate to `release/` folder
2. Run `CefFlashBrowser.exe`

### Building from Source
1. Open `SvonyBrowser/SvonyBrowser.csproj` in Visual Studio
2. Restore NuGet packages
3. Build in Release|x64 configuration
4. Output will be in `SvonyBrowser/bin/x64/Release/`

## Project Structure

```
EvonyBrowser/
├── SvonyBrowser/           # Main WPF application
│   ├── Controls/           # Custom UI controls
│   ├── Services/           # Core services (GameState, MapScanner, etc.)
│   ├── Models/             # Data models
│   └── Helpers/            # Utility classes
├── release/                # Pre-built binaries with Flash assets
│   ├── Assets/
│   │   ├── CefSharp/       # Browser engine
│   │   ├── Plugins/        # Flash plugin (pepflashplayer.dll)
│   │   └── SwfPlayer/      # SWF player
│   └── roboevony_1.60/     # AutoEvony integration
├── FlashBrowser_x86/       # 32-bit build assets
├── cli/                    # Command-line tools
├── config/                 # Configuration files
├── scripts/                # Build and utility scripts
└── docs/                   # Documentation
```

## Key Services

| Service | Description |
|---------|-------------|
| `GameStateEngine` | Tracks and manages game state |
| `MapScanner` | Scans game map for resources/targets |
| `CombatSimulator` | Simulates battle outcomes |
| `AutoPilotService` | Automated gameplay actions |
| `ProtocolHandler` | Game protocol analysis |
| `SessionRecorder` | Records gameplay sessions |
| `ChatbotService` | LLM-powered game assistant |

## Configuration

Copy `.env.example` to `.env` and configure:
```
EVONY_SERVER=https://game.evony.com
LLM_API_KEY=your_api_key
PROXY_ENABLED=false
```

## License

MIT License - See LICENSE file

## Credits

- Based on [CefFlashBrowser](https://github.com/AiDave71/BorgFlashBrowser) by Mzying2001
- Enhanced with Svony features by Ghenghis
- Merged and maintained by AiDave71
