# Svony Browser Services Reference

## Overview

Svony Browser v6.0 includes 49 C# services organized into four categories:
- **Core Services** (7): Essential browser and game functionality
- **Analysis Services** (6): Packet analysis and intelligence
- **Automation Services** (6): Game automation and control
- **Infrastructure Services** (12): System utilities and management

All services use the singleton pattern with lazy initialization for thread safety.

---

## Core Services

### McpConnectionManager

**Purpose:** Manages connections to MCP (Model Context Protocol) servers.

**Key Features:**
- JSON-RPC 2.0 communication
- Automatic reconnection
- Health monitoring
- Tool invocation

**Configuration:**
```json
{
  "servers": [
    { "name": "evony-rag", "port": 3001 },
    { "name": "evony-rte", "port": 3002 }
  ]
}
```

**Usage:**
```csharp
var mcp = McpConnectionManager.Instance;
await mcp.ConnectAllAsync();
var result = await mcp.CallToolAsync("evony-rag", "search_knowledge", args);
```

---

### ProtocolHandler

**Purpose:** Handles AMF3 encoding/decoding and maintains the protocol database.

**Key Features:**
- AMF3 binary serialization
- Protocol action registry
- Packet validation
- Statistics tracking

**Supported Types:**
| AMF3 Type | C# Type |
|-----------|---------|
| Undefined | null |
| Null | null |
| False | bool |
| True | bool |
| Integer | int |
| Double | double |
| String | string |
| Array | List |
| Object | Dictionary |
| ByteArray | byte[] |

**Usage:**
```csharp
var protocol = ProtocolHandler.Instance;
var decoded = protocol.DecodeAmf3(packetBytes);
var action = protocol.GetProtocolAction("march.send");
```

---

### GameStateEngine

**Purpose:** Tracks real-time game state from captured traffic.

**Tracked State:**
- Player profile (name, power, alliance)
- Cities (buildings, resources, troops)
- Heroes (level, equipment, skills)
- Marches (active, queued)
- Research progress
- Build queues

**Events:**
| Event | Description |
|-------|-------------|
| StateChanged | Any state change |
| PacketReceived | New packet captured |
| ResourcesUpdated | Resource values changed |
| MarchStarted | New march initiated |

**Usage:**
```csharp
var game = GameStateEngine.Instance;
var resources = game.GetResources();
var marches = game.GetActiveMarches();
game.StateChanged += (s, e) => UpdateUI(e.ChangeType);
```

---

### ChatbotService

**Purpose:** AI chat interface with RAG integration.

**Key Features:**
- Natural language queries
- Context-aware responses
- RAG knowledge retrieval
- Streaming responses

**Intent Classification:**
| Intent | Description |
|--------|-------------|
| QUERY | Information request |
| COMMAND | Action request |
| ANALYSIS | Data analysis |
| STRATEGY | Strategy advice |

**Usage:**
```csharp
var chat = ChatbotService.Instance;
var response = await chat.SendMessageAsync("How do I upgrade my castle?");
```

---

### SessionManager

**Purpose:** Manages browser session state and settings.

**Managed State:**
- Server selection
- Login credentials
- Panel layout
- Recent history

**Usage:**
```csharp
var session = SessionManager.Instance;
session.SetServer("cc2.evony.com");
session.SavePanelLayout(leftWidth, rightWidth);
```

---

### TrafficPipeClient

**Purpose:** Receives traffic data from Fiddler via named pipe.

**Protocol:**
```
Fiddler → Named Pipe (SvonyFiddlerBridge) → TrafficPipeClient → PacketAnalysisEngine
```

**Message Format:**
```json
{
  "type": "packet",
  "direction": "outbound",
  "timestamp": "2025-01-12T10:30:00Z",
  "data": "base64-encoded-bytes"
}
```

**Usage:**
```csharp
var pipe = TrafficPipeClient.Instance;
await pipe.ConnectAsync();
pipe.TrafficReceived += (s, e) => ProcessPacket(e.Data);
```

---

### ProxyMonitor

**Purpose:** Monitors Fiddler proxy connection status.

**Monitored Metrics:**
- Connection status
- Throughput (KB/s)
- Latency
- Error rate

**Usage:**
```csharp
var proxy = ProxyMonitor.Instance;
bool connected = proxy.IsConnected;
double throughput = proxy.GetThroughputKBps();
proxy.StatusChanged += (s, e) => UpdateStatusBar(e.IsConnected);
```

---

## Analysis Services

### PacketAnalysisEngine

**Purpose:** Deep packet analysis with pattern detection.

**Analysis Features:**
- AMF3 structure parsing
- Field type inference
- Pattern recognition
- Anomaly detection
- Differential comparison

**Usage:**
```csharp
var analyzer = PacketAnalysisEngine.Instance;
var analysis = await analyzer.AnalyzePacketAsync(packet);
var patterns = analyzer.DetectPatterns(packets);
```

---

### ProtocolFuzzer

**Purpose:** Protocol fuzzing for discovering undocumented endpoints.

**Fuzzing Modes:**
| Mode | Description |
|------|-------------|
| Random | Random byte mutations |
| Boundary | Edge case values |
| Format | Type confusion |
| Sequence | Action ordering |

**Safety Features:**
- Rate limiting
- Rollback support
- Safe mode (read-only)

**Usage:**
```csharp
var fuzzer = ProtocolFuzzer.Instance;
await fuzzer.StartFuzzingAsync(new FuzzConfig {
    Mode = FuzzMode.Boundary,
    TargetAction = "building.upgrade",
    MaxIterations = 100
});
```

---

### CombatSimulator

**Purpose:** Battle outcome prediction using Monte Carlo simulation.

**Simulated Factors:**
- Troop types and quantities
- Hero skills and equipment
- Technology bonuses
- Terrain effects
- Luck factor

**Output:**
```csharp
public class BattleResult
{
    public double WinProbability { get; set; }
    public TroopLosses AttackerLosses { get; set; }
    public TroopLosses DefenderLosses { get; set; }
    public int SimulationCount { get; set; }
}
```

**Usage:**
```csharp
var combat = CombatSimulator.Instance;
var result = await combat.SimulateBattleAsync(attackerTroops, defenderTroops);
Console.WriteLine($"Win probability: {result.WinProbability:P}");
```

---

### StrategicAdvisor

**Purpose:** AI-powered strategy recommendations.

**Advice Categories:**
- Growth optimization
- Resource management
- Troop composition
- Building priorities
- Research paths

**Usage:**
```csharp
var advisor = StrategicAdvisor.Instance;
var advice = await advisor.GetAdviceAsync(game.GetSnapshot());
foreach (var recommendation in advice.Recommendations)
{
    Console.WriteLine($"{recommendation.Priority}: {recommendation.Action}");
}
```

---

### MapScanner

**Purpose:** World map discovery and target identification.

**Scan Types:**
| Type | Description |
|------|-------------|
| Farm | Inactive player cities |
| Resource | Gathering spots |
| Monster | NPC enemies |
| Alliance | Alliance members |

**Usage:**
```csharp
var scanner = MapScanner.Instance;
var targets = await scanner.ScanAreaAsync(centerX, centerY, radius);
var farms = targets.Where(t => t.Type == TargetType.Farm);
```

---

### AnalyticsDashboard

**Purpose:** Performance analytics and growth tracking.

**Tracked Metrics:**
- Power growth rate
- Resource efficiency
- Combat win rate
- Activity patterns

**Usage:**
```csharp
var analytics = AnalyticsDashboard.Instance;
var report = analytics.GenerateReport(TimeSpan.FromDays(7));
Console.WriteLine($"Power growth: {report.PowerGrowthRate:+0.0%}");
```

---

## Automation Services

### AutoPilotService

**Purpose:** Intelligent game automation with safety limits.

**Automation Modes:**
| Mode | Description |
|------|-------------|
| Growth | Maximize power growth |
| Farm | Resource gathering |
| Combat | Attack automation |
| Defense | Defensive actions |

**Safety Features:**
- Action rate limiting
- Resource thresholds
- Combat restrictions
- Pause on error

**Usage:**
```csharp
var autopilot = AutoPilotService.Instance;
await autopilot.StartAsync(new AutoPilotConfig {
    Mode = AutoPilotMode.Growth,
    MaxActionsPerHour = 100,
    SafetyLimits = true
});
```

---

### SessionRecorder

**Purpose:** Session recording and replay.

**Recording Format:**
```json
{
  "id": "session-001",
  "startTime": "2025-01-12T10:00:00Z",
  "packets": [
    { "timestamp": 0, "direction": "out", "data": "..." },
    { "timestamp": 100, "direction": "in", "data": "..." }
  ]
}
```

**Usage:**
```csharp
var recorder = SessionRecorder.Instance;
recorder.StartRecording("my-session");
// ... play game ...
var recording = recorder.StopRecording();
await recorder.ReplayAsync(recording.Id);
```

---

### MultiAccountOrchestrator

**Purpose:** Coordinate multiple game accounts.

**Coordination Features:**
- Rally coordination
- Resource transfer
- Reinforcement dispatch
- Synchronized actions

**Usage:**
```csharp
var orchestrator = MultiAccountOrchestrator.Instance;
orchestrator.AddAccount("main", mainSession);
orchestrator.AddAccount("farm1", farm1Session);
await orchestrator.CoordinateRallyAsync(targetId, troops);
```

---

### CdpConnectionService

**Purpose:** Chrome DevTools Protocol for browser automation.

**CDP Features:**
- Page navigation
- Input simulation
- Screenshot capture
- Network interception

**Usage:**
```csharp
var cdp = CdpConnectionService.Instance;
await cdp.ConnectAsync("localhost", 9222);
await cdp.NavigateAsync("https://cc2.evony.com");
await cdp.ClickAsync(100, 200);
```

---

### VisualAutomationService

**Purpose:** Coordinate-based visual automation.

**UI Element Map:**
Pre-defined coordinates for common UI elements:
- Navigation buttons
- Building slots
- Action buttons
- Dialog controls

**Usage:**
```csharp
var visual = VisualAutomationService.Instance;
await visual.ClickElementAsync("castle");
await visual.ClickElementAsync("upgradeButton");
```

---

### PromptTemplateEngine

**Purpose:** AI prompt templates with game context.

**Template Variables:**
| Variable | Description |
|----------|-------------|
| {{gameState}} | Current game snapshot |
| {{resources}} | Resource values |
| {{troops}} | Troop counts |
| {{packet}} | Packet data |

**Usage:**
```csharp
var prompts = PromptTemplateEngine.Instance;
var prompt = prompts.Render("analyze_packet", new {
    packet = packetData,
    gameState = game.GetSnapshot()
});
```

---

## Infrastructure Services

### SettingsManager

**Purpose:** Settings persistence and management.

**Settings Categories:**
- General (10 options)
- Browser (8 options)
- Proxy (6 options)
- MCP (10 options)
- LLM (12 options)
- And more... (164 total)

**Usage:**
```csharp
var settings = SettingsManager.Instance;
settings.Settings.General.Theme = "Borg Dark";
await settings.SaveAsync();
```

---

### MemoryManager

**Purpose:** Memory leak prevention and optimization.

**Features:**
- Memory pressure monitoring
- GC optimization hints
- Object pool management
- Leak detection

**Usage:**
```csharp
var memory = MemoryManager.Instance;
memory.StartMonitoring();
memory.OnMemoryPressure += (s, e) => {
    if (e.PressureLevel > 0.8) ClearCaches();
};
```

---

### ErrorHandler

**Purpose:** Centralized error handling and recovery.

**Error Levels:**
| Level | Action |
|-------|--------|
| Debug | Log only |
| Info | Log only |
| Warning | Log + notify |
| Error | Log + notify + retry |
| Critical | Log + crash report |

**Usage:**
```csharp
var errors = ErrorHandler.Instance;
errors.HandleException(ex, ErrorSeverity.Warning);
errors.EnableCrashRecovery();
```

---

### ConnectionPool

**Purpose:** HTTP and WebSocket connection pooling.

**Features:**
- Connection reuse
- Keep-alive management
- Automatic cleanup
- Load balancing

**Usage:**
```csharp
var pool = ConnectionPool.Instance;
var client = pool.GetHttpClient("evony-api");
var response = await client.GetAsync("/api/status");
```

---

### ThemeManager

**Purpose:** Application theme management.

**Available Themes:**
| Theme | Description |
|-------|-------------|
| Borg Dark | Dark theme with green accents |
| Light | Light theme |
| Evony Classic | Game-inspired theme |
| Cyberpunk | Neon colors |
| High Contrast | Accessibility theme |

**Usage:**
```csharp
var themes = ThemeManager.Instance;
themes.ApplyTheme("Borg Dark");
themes.ThemeChanged += (s, e) => RefreshUI();
```

---

### KeyboardShortcutManager

**Purpose:** Customizable keyboard shortcuts.

**Default Shortcuts:**
| Shortcut | Action |
|----------|--------|
| Ctrl+1 | Show bot panel |
| Ctrl+2 | Show client panel |
| Ctrl+3 | Show both panels |
| Ctrl+S | Open settings |
| F5 | Refresh |

**Usage:**
```csharp
var shortcuts = KeyboardShortcutManager.Instance;
shortcuts.Register("Ctrl+Shift+A", () => autopilot.Toggle());
shortcuts.ShortcutPressed += (s, e) => HandleShortcut(e.ShortcutId);
```

---

### RealDataProvider

**Purpose:** Live data from game traffic (replaces mocked data).

**Data Sources:**
- Captured packets
- Game state engine
- MCP servers
- External APIs

**Usage:**
```csharp
var data = RealDataProvider.Instance;
var resources = await data.GetResourcesAsync();
var troops = await data.GetTroopsAsync();
```

---

### WebhookHub

**Purpose:** Multi-platform webhook notifications.

**Supported Platforms:**
| Platform | Format |
|----------|--------|
| Discord | Embeds |
| Telegram | Markdown |
| Slack | Blocks |
| Teams | Cards |

**Usage:**
```csharp
var webhooks = WebhookHub.Instance;
await webhooks.SendAsync("discord", new WebhookMessage {
    Title = "Rally Alert",
    Description = "Enemy rally detected!",
    Color = "#FF0000"
});
```

---

### ExportImportManager

**Purpose:** Data import/export for portability.

**Export Formats:**
| Format | Use Case |
|--------|----------|
| JSON | Full backup |
| CSV | Spreadsheet analysis |
| Markdown | Documentation |
| Binary | Compact storage |

**Usage:**
```csharp
var exporter = ExportImportManager.Instance;
await exporter.ExportAsync("backup.json", ExportFormat.Json);
await exporter.ImportAsync("backup.json");
```

---

### FiddlerBridge

**Purpose:** Advanced Fiddler integration.

**Features:**
- Named pipe communication
- Packet injection
- Breakpoint support
- Session management

**Usage:**
```csharp
var fiddler = FiddlerBridge.Instance;
await fiddler.ConnectAsync();
await fiddler.InjectPacketAsync(packetBytes);
fiddler.SetBreakpoint("march.send", BreakpointType.Request);
```

---

### LlmIntegrationService

**Purpose:** Local LLM integration (LM Studio, Ollama).

**Supported Backends:**
| Backend | Port |
|---------|------|
| LM Studio | 1234 |
| Ollama | 11434 |
| OpenAI-compatible | Custom |

**Usage:**
```csharp
var llm = LlmIntegrationService.Instance;
await llm.ConnectAsync("http://localhost:1234/v1");
var response = await llm.GenerateAsync("Explain this packet...");
```

---

### StatusBarManager

**Purpose:** Status bar widget management.

**Widget Categories:**
| Category | Widgets |
|----------|---------|
| MCP | RAG progress, RTE progress |
| LLM | Tokens/sec, VRAM, GPU temp |
| Network | Packets/sec, throughput |
| Game | Resources, troops, marches |

**Usage:**
```csharp
var statusBar = StatusBarManager.Instance;
statusBar.UpdateWidget("rag_progress", 75.0);
statusBar.SetWidgetEnabled("gpu_temp", true);
```

---

*Last Updated: v6.0 "Borg Edition"*
