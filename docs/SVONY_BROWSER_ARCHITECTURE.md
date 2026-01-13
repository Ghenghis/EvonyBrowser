# Svony Browser - Architecture Design Document

## Project Overview

**Svony Browser** is a dual-panel Flash browser that allows simultaneous operation of:
- **Left Panel**: AutoEvony.swf (Bot/Automation Interface)
- **Right Panel**: EvonyClient1921.swf (Official Game Client)

Both panels share a **unified connection** through a local proxy, allowing real-time synchronization while Evony's single-connection-per-account limitation is respected.

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           SVONY BROWSER                                      │
├─────────────────────────────────┬───────────────────────────────────────────┤
│      LEFT PANEL                 │           RIGHT PANEL                      │
│   ┌─────────────────────────┐   │   ┌─────────────────────────────────────┐ │
│   │     AutoEvony.swf       │   │   │      EvonyClient1921.swf            │ │
│   │                         │   │   │                                      │ │
│   │  • Bot Controls         │   │   │  • Full Game UI                     │ │
│   │  • Script Execution     │   │   │  • Visual Feedback                  │ │
│   │  • Automation Tasks     │   │   │  • Manual Interaction               │ │
│   │  • Command Queue        │   │   │  • Real-time Updates                │ │
│   │                         │   │   │                                      │ │
│   └───────────┬─────────────┘   │   └──────────────┬──────────────────────┘ │
│               │                 │                   │                        │
│               └─────────────────┴───────────────────┘                        │
│                                 │                                            │
│                    ┌────────────▼────────────┐                               │
│                    │    MESSAGE ROUTER       │                               │
│                    │                         │                               │
│                    │  • Command Dedup        │                               │
│                    │  • State Broadcast      │                               │
│                    │  • Priority Queue       │                               │
│                    └────────────┬────────────┘                               │
│                                 │                                            │
├─────────────────────────────────┼────────────────────────────────────────────┤
│                    ┌────────────▼────────────┐                               │
│                    │   FIDDLER PROXY         │                               │
│                    │   127.0.0.1:8888        │                               │
│                    │                         │                               │
│                    │  • Traffic Capture      │                               │
│                    │  • AMF Decoding         │                               │
│                    │  • SWF Extraction       │                               │
│                    │  • Packet Logging       │                               │
│                    └────────────┬────────────┘                               │
│                                 │                                            │
└─────────────────────────────────┼────────────────────────────────────────────┘
                                  │
                                  ▼
                    ┌─────────────────────────┐
                    │    cc2.evony.com        │
                    │   (Evony Game Server)   │
                    └─────────────────────────┘
```

---

## Core Components

### 1. Split Panel Browser Shell

**Technology**: WPF + CefSharp (based on existing CefFlashBrowser)

```
SvonyBrowser/
├── MainWindow.xaml              # Split panel layout with GridSplitter
├── Panels/
│   ├── LeftPanel.xaml           # AutoEvony container
│   └── RightPanel.xaml          # EvonyClient container
├── Controls/
│   ├── PanelResizer.xaml        # Drag-to-resize control
│   ├── PanelToggle.xaml         # Show/Hide panels
│   └── PanelSwitch.xaml         # Swap panel positions
└── ViewModels/
    ├── MainViewModel.cs         # Panel state management
    └── BrowserViewModel.cs      # CefSharp browser control
```

**Panel Features**:
| Feature | Description                                 |
| ------- | ------------------------------------------- |
| Resize  | GridSplitter allows dragging panel boundary |
| Hide    | Toggle buttons to show/hide either panel    |
| Swap    | Switch left/right panel contents            |
| Single  | Expand one panel to full width              |
| Both    | Show both panels side-by-side               |

### 2. Shared Browser Context

**Key Insight**: Both CefSharp browser instances share the same:
- `CachePath` - Flash Local Shared Objects (.sol files)
- `RequestContext` - Cookies and session data
- `ProxySettings` - Route through Fiddler

```csharp
// Shared settings for both panels
var settings = new CefSettings
{
    CachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache"),
    PersistSessionCookies = true,
    PersistUserPreferences = true
};

// Proxy configuration
settings.CefCommandLineArgs.Add("proxy-server", "127.0.0.1:8888");

// Flash plugin
settings.CefCommandLineArgs.Add("ppapi-flash-path", flashPluginPath);
settings.CefCommandLineArgs.Add("ppapi-flash-version", "32.0.0.465");
```

### 3. Connection Sharing Strategy

**The Single Connection Problem**:
- Evony allows only ONE active socket per account
- Opening a second client disconnects the first
- Both SWFs need to share the same authenticated session

**Solution: Proxy-Based Session Sharing**

```
┌──────────────────────────────────────────────────────────────┐
│                    HOW IT WORKS                               │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  1. User logs in via ONE panel (either AutoEvony or Client)  │
│                                                               │
│  2. Fiddler captures the authentication exchange:            │
│     • Login request with credentials                          │
│     • Session token/cookie in response                        │
│     • Socket handshake completion                             │
│                                                               │
│  3. Session data stored in shared CachePath:                 │
│     • SOL files (Flash Local Shared Objects)                 │
│     • Cookies via shared RequestContext                       │
│                                                               │
│  4. Second panel loads and finds existing session:           │
│     • Reads SOL files from shared cache                       │
│     • Uses same cookies/tokens                                │
│     • Joins existing connection (no re-auth)                  │
│                                                               │
│  5. Both panels now reflect same game state!                 │
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

### 4. Message Router Component

Handles coordination between panels:

```csharp
public class MessageRouter
{
    // Prevent duplicate commands
    private HashSet<string> _recentCommands = new();
    
    // State broadcast to both panels
    public event Action<GameState> OnStateUpdate;
    
    // Command priority queue
    private PriorityQueue<GameCommand, int> _commandQueue;
    
    public void RouteCommand(string source, GameCommand cmd)
    {
        // Deduplicate
        var cmdHash = cmd.GetHash();
        if (_recentCommands.Contains(cmdHash)) return;
        _recentCommands.Add(cmdHash);
        
        // Queue with priority
        _commandQueue.Enqueue(cmd, cmd.Priority);
        
        // Log source
        Logger.Log($"[{source}] {cmd.Type}: {cmd.Details}");
    }
    
    public void BroadcastState(GameState state)
    {
        OnStateUpdate?.Invoke(state);
    }
}
```

---

## Existing Infrastructure (from Fiddler-FlashBrowser)

### Available Components

| Component           | Location                                      | Purpose                 |
| ------------------- | --------------------------------------------- | ----------------------- |
| Fiddler Classic     | `D:\Fiddler-FlashBrowser\Fiddler\`            | Proxy with custom rules |
| CefFlashBrowser     | `D:\Fiddler-FlashBrowser\FlashBrowser_x64\`   | Flash-enabled browser   |
| AutoEvony.swf       | `D:\Fiddler-FlashBrowser\AutoEvony.swf`       | Bot interface           |
| EvonyClient1921.swf | `D:\Fiddler-FlashBrowser\EvonyClient1921.swf` | Game client             |
| Custom Rules        | `config\EvonyRE-CustomRules.cs`               | Fiddler packet handling |
| SOL Editor          | Built into CefFlashBrowser                    | .sol file management    |

### Fiddler Integration Points

From `ARCHITECTURE.md`:
- **Evony Only Mode**: Filter traffic to cc2.evony.com
- **Stealth Mode**: Remove proxy headers
- **AMF Detection**: Identify binary protocol traffic
- **Packet Classification**: LOGIN, MARCH, BATTLE, etc.
- **SWF Extraction**: Auto-save downloaded SWFs

### Packet Type Detection (from CustomRules.cs)

```csharp
// Packet classification by content
| Type     | Detection Pattern                    | Color    |
| -------- | ------------------------------------ | -------- |
| LOGIN    | url/body contains "login"            | Green    |
| MARCH    | url/body contains "march", "troop"   | Orange   |
| BATTLE   | url/body contains "battle", "attack" | Red      |
| RESOURCE | url/body contains "resource", "gold" | Cyan     |
| CHAT     | url/body contains "chat", "message"  | Yellow   |
| BUILD    | url/body contains "build", "upgrade" | Purple   |
| RESEARCH | url/body contains "research", "tech" | Pink     |
| ALLIANCE | url/body contains "alliance"         | Lt Green |
| SWF      | url ends with ".swf"                 | Blue     |
| AMF      | Content-Type: amf OR 0x00 0x03       | Amber    |
```

---

## Data Flow

### Login Flow
```
User                 Svony Browser              Fiddler                Evony
  │                       │                        │                      │
  │ 1. Click Login        │                        │                      │
  ├──────────────────────►│                        │                      │
  │                       │ 2. Send credentials    │                      │
  │                       ├───────────────────────►│                      │
  │                       │                        │ 3. Forward request   │
  │                       │                        ├─────────────────────►│
  │                       │                        │                      │
  │                       │                        │ 4. Auth response     │
  │                       │                        │◄─────────────────────┤
  │                       │ 5. Capture session     │                      │
  │                       │◄───────────────────────┤                      │
  │                       │                        │                      │
  │                       │ 6. Store in shared     │                      │
  │                       │    cache (SOL/cookies) │                      │
  │                       │                        │                      │
  │ 7. Both panels now    │                        │                      │
  │    share session!     │                        │                      │
  │◄──────────────────────┤                        │                      │
```

### Command Flow
```
AutoEvony Panel           Message Router           EvonyClient Panel
      │                         │                         │
      │ 1. User clicks          │                         │
      │    "Attack NPC"         │                         │
      ├────────────────────────►│                         │
      │                         │                         │
      │                         │ 2. Route to server      │
      │                         │    via Fiddler          │
      │                         │                         │
      │                         │ 3. Server responds      │
      │                         │    with new state       │
      │                         │                         │
      │ 4. Broadcast state      │ 4. Broadcast state     │
      │◄────────────────────────┤────────────────────────►│
      │                         │                         │
      │ 5. Both panels show     │ 5. Both panels show    │
      │    updated game state   │    updated game state  │
```

---

## UI Layout Design

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Svony Browser                                              [─] [□] [×]     │
├─────────────────────────────────────────────────────────────────────────────┤
│ [File] [View] [Tools] [Server: cc2 ▼] [◀ Left] [Both] [Right ▶] [Settings] │
├────────────────────────────────┬┬───────────────────────────────────────────┤
│                                ││                                            │
│     AUTOEVONY PANEL            ││     EVONY CLIENT PANEL                    │
│                                ││                                            │
│  ┌──────────────────────────┐  ││  ┌────────────────────────────────────┐   │
│  │                          │  ││  │                                    │   │
│  │   AutoEvony.swf          │  ││  │   EvonyClient1921.swf              │   │
│  │                          │  ││  │                                    │   │
│  │   [Bot Controls]         │  ││  │   [Full Game Interface]            │   │
│  │   [Script List]          │  ││  │   [Map View]                       │   │
│  │   [Command Queue]        │  ││  │   [City View]                      │   │
│  │   [Log Output]           │  ││  │   [Heroes/Troops]                  │   │
│  │                          │  ││  │                                    │   │
│  │                          │◄─┼┼─►│   ← GridSplitter (drag to resize) │   │
│  │                          │  ││  │                                    │   │
│  └──────────────────────────┘  ││  └────────────────────────────────────┘   │
│                                ││                                            │
├────────────────────────────────┴┴───────────────────────────────────────────┤
│ Status: Connected to cc2.evony.com │ Session: Active │ Packets: 1,234      │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## File Structure

```
D:\Fiddler-FlashBrowser\
├── SvonyBrowser/                    # NEW: Main application
│   ├── SvonyBrowser.sln             # Visual Studio solution
│   ├── SvonyBrowser/
│   │   ├── App.xaml                 # Application entry
│   │   ├── MainWindow.xaml          # Split panel layout
│   │   ├── Panels/
│   │   │   ├── BrowserPanel.xaml    # Reusable browser panel
│   │   │   └── PanelControls.xaml   # Resize/toggle controls
│   │   ├── Services/
│   │   │   ├── MessageRouter.cs     # Command coordination
│   │   │   ├── SessionManager.cs    # Shared session handling
│   │   │   └── StateSync.cs         # Panel state synchronization
│   │   └── ViewModels/
│   │       └── MainViewModel.cs     # MVVM view model
│   └── SvonyBrowser.Tests/          # Unit tests
│
├── docs/                            # Documentation
│   ├── SVONY_BROWSER_ARCHITECTURE.md  # This file
│   ├── EVONY_PROTOCOL_REFERENCE.md    # Protocol details
│   └── IMPLEMENTATION_GUIDE.md        # Step-by-step guide
│
├── AutoEvony.swf                    # EXISTING: Bot SWF
├── EvonyClient1921.swf              # EXISTING: Client SWF
├── Fiddler/                         # EXISTING: Proxy
├── FlashBrowser_x64/                # EXISTING: Base browser
└── config/                          # EXISTING: Configuration
```

---

## Dependencies

### Required NuGet Packages
```xml
<PackageReference Include="CefSharp.Wpf" Version="119.4.30" />
<PackageReference Include="CefSharp.Common" Version="119.4.30" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="Serilog" Version="3.1.1" />
```

### System Requirements
- Windows 10/11
- .NET Framework 4.7.2+ or .NET 6.0+
- Visual C++ Redistributable 2019+
- Fiddler Classic (included)
- Flash Player PPAPI (pepflashplayer.dll - included)

---

## Next Steps

See `IMPLEMENTATION_GUIDE.md` for step-by-step build instructions.

See `EVONY_PROTOCOL_REFERENCE.md` for protocol details and API commands.
