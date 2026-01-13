# Evony RE Toolkit - Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        EVONY RE TOOLKIT                                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────────┐          ┌─────────────────────────────────┐     │
│  │   Launch-EvonyRE │──────────▶│        ORCHESTRATION            │     │
│  │      (.bat)      │          │   (PowerShell Scripts)          │     │
│  └──────────────────┘          └────────────┬────────────────────┘     │
│                                              │                          │
│                    ┌─────────────────────────┼─────────────────────┐   │
│                    │                         │                     │   │
│                    ▼                         ▼                     ▼   │
│  ┌─────────────────────┐   ┌─────────────────────┐   ┌────────────────┐│
│  │   FIDDLER CLASSIC   │   │   CEF FLASH BROWSER │   │    CONFIG      ││
│  │                     │   │                     │   │                ││
│  │  • Custom Rules     │   │  • Flash Player     │   │ evony-re-      ││
│  │  • Packet Capture   │◀──│  • CEF Chromium     │   │ config.json    ││
│  │  • Traffic Filter   │   │  • SOL Editor       │   │                ││
│  │  • SWF Extraction   │   │  • Proxy Support    │   │ CustomRules.cs ││
│  │  • Export Tools     │   │                     │   │                ││
│  └──────────┬──────────┘   └─────────────────────┘   └────────────────┘│
│             │ Proxy (127.0.0.1:8888)                                    │
│             │                                                           │
│             ▼                                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                     CAPTURED DATA                                │   │
│  ├──────────────┬──────────────┬──────────────┬───────────────────┤   │
│  │   /logs/     │  /captures/  │/extracted-swf│   Fiddler SAZ     │   │
│  │              │              │              │                    │   │
│  │ Traffic logs │ JSON exports │  SWF files   │  Session archive  │   │
│  │ Daily rotate │ RTE format   │  By date     │  Full packet data │   │
│  └──────────────┴──────────────┴──────────────┴───────────────────┘   │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │       cc2.evony.com           │
                    │     (Evony Game Server)       │
                    └───────────────────────────────┘
```

## Data Flow

```
User Action                     Fiddler                      Evony Server
    │                              │                              │
    │ 1. Click in browser          │                              │
    ▼                              │                              │
┌─────────┐                        │                              │
│ Browser │─────HTTP Request──────▶│                              │
└─────────┘                        │                              │
                                   ▼                              │
                          ┌────────────────┐                      │
                          │ OnBeforeRequest│                      │
                          │                │                      │
                          │ • Filter check │                      │
                          │ • Stealth mode │                      │
                          │ • Log traffic  │                      │
                          │ • Color coding │                      │
                          └───────┬────────┘                      │
                                  │                               │
                                  ▼                               │
                          ┌────────────────┐                      │
                          │  Forward to    │─────────────────────▶│
                          │    Server      │                      │
                          └────────────────┘                      │
                                                                  │
                                                                  ▼
                                                          ┌──────────────┐
                                                          │ Process &    │
                                                          │ Respond      │
                                                          └──────┬───────┘
                                                                 │
                          ┌────────────────┐◀────────────────────┘
                          │OnBeforeResponse│
                          │                │
                          │ • SWF detect   │
                          │ • Auto extract │
                          │ • Error mark   │
                          └───────┬────────┘
                                  │
    ┌─────────┐◀──────────────────┘
    │ Browser │
    └─────────┘
```

## Component Details

### 1. Launcher System

```
Launch-EvonyRE.bat
       │
       ▼
Launch-EvonyRE.ps1
       │
       ├──▶ Check Fiddler running
       │         │
       │         └──▶ Start if not running
       │
       ├──▶ Wait for proxy ready (port 8888)
       │
       ├──▶ Configure browser proxy
       │
       ├──▶ Initialize session logging
       │
       └──▶ Start FlashBrowser
```

### 2. Fiddler Custom Rules

```csharp
namespace Fiddler.Handlers
{
    // Configuration
    TargetServer = "cc2.evony.com"
    
    // Rule Options (toggle via menu)
    m_EvonyOnlyMode      // Only show Evony traffic
    m_StealthMode        // Remove proxy headers
    m_HighlightPackets   // Color by packet type
    m_AutoExtractSWF     // Save SWF files
    m_LogTraffic         // Log all traffic
    m_DetectBots         // Detect AutoEvony
    m_DecodeAMF          // AMF3 decoding
    m_HideNonEvony       // Hide other domains
    
    // Custom Columns
    GetEvonyPacketType() // LOGIN, MARCH, BATTLE, etc.
    GetPacketSize()      // Human-readable size
    
    // Event Handlers
    OnBeforeRequest()    // Filter, stealth, logging
    OnBeforeResponse()   // SWF extraction, error marking
    
    // Menu Actions
    DoExportForRTE()     // Export to JSON
    DoChangeServer()     // Switch servers
    DoOpenSWFFolder()    // Open extraction folder
}
```

### 3. CefFlashBrowser Integration

```
┌────────────────────────────────────────────────────┐
│            CefFlashBrowser.exe                      │
├────────────────────────────────────────────────────┤
│                                                     │
│  ┌─────────────────┐   ┌─────────────────────────┐ │
│  │  CEF Chromium   │   │     Flash Player        │ │
│  │   (Browser)     │   │   (pepflashplayer.dll)  │ │
│  └────────┬────────┘   └────────────┬────────────┘ │
│           │                         │               │
│           └─────────┬───────────────┘               │
│                     │                               │
│                     ▼                               │
│           ┌─────────────────┐                       │
│           │  Proxy Settings │                       │
│           │  127.0.0.1:8888 │──────▶ Fiddler       │
│           └─────────────────┘                       │
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │               SOL Editor                     │   │
│  │  • View/Edit Flash Local Shared Objects     │   │
│  │  • Export/Import game saves                 │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
└────────────────────────────────────────────────────┘
```

## Packet Type Classification

| Type | Detection Pattern | Color |
|------|------------------|-------|
| LOGIN | url/body contains "login" | Green |
| MARCH | url/body contains "march", "troop" | Orange |
| BATTLE | url/body contains "battle", "attack" | Red (bold) |
| RESOURCE | url/body contains "resource", "gold" | Cyan |
| CHAT | url/body contains "chat", "message" | Yellow |
| BUILD | url/body contains "build", "upgrade" | Purple |
| RESEARCH | url/body contains "research", "tech" | Pink |
| ALLIANCE | url/body contains "alliance", "guild" | Light Green |
| SWF | url ends with ".swf" | Blue (bold) |
| AMF | Content-Type: amf OR magic bytes 0x00 0x03 | Amber (italic) |
| BOT | User-Agent contains bot patterns | Magenta (bold) |

## File Storage

```
D:\Fiddler-FlashBrowser\
│
├── logs/
│   ├── launcher-2025-01-11.log       # Launcher activity
│   └── traffic-2025-01-11.log        # Packet log
│       Format: timestamp|type|id|method|url|status|req_size|resp_size
│
├── captures/
│   ├── 20250111-143052/              # Session folder
│   │   └── session.saz               # Fiddler archive
│   └── evony-capture-20250111-143052.json  # RTE export
│
└── extracted-swf/
    ├── 2025-01-11/                   # Daily folder
    │   ├── game.swf
    │   └── tutorial.swf
    ├── tutorial/                     # Special tutorial folder
    │   └── advisor_guide.swf
    └── extraction-log.txt            # Extraction history
```

## Security Considerations

### Stealth Mode Headers Removed
- `Via` - Proxy identification
- `X-Forwarded-For` - Client IP
- `X-Forwarded-Host` - Original host
- `X-Forwarded-Proto` - Protocol
- `Proxy-Connection` - Proxy indicator
- `X-ProxyUser-IP` - User IP

### HTTPS Interception
- Fiddler root certificate required
- Man-in-the-middle for SSL traffic
- Certificate installed to Windows trust store

### Bot Detection Patterns
- `autoevony` in User-Agent
- `yaeb` in User-Agent
- `evony-bot` in User-Agent
- `selenium` in User-Agent
- `headless` in User-Agent
