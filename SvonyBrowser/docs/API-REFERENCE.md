# Svony Browser API Reference

## Service APIs

### McpConnectionManager

Manages connections to MCP servers.

```csharp
// Singleton access
var mcp = McpConnectionManager.Instance;

// Connect to all servers
await mcp.ConnectAllAsync();

// Disconnect from all servers
await mcp.DisconnectAllAsync();

// Call a tool
var result = await mcp.CallToolAsync("evony-rag", "search_knowledge", new {
    query = "how to upgrade castle",
    limit = 5
});

// Get server status
var status = mcp.GetServerStatus("evony-rag");

// Get RAG progress
double ragProgress = mcp.GetRagProgress();

// Get RTE progress
double rteProgress = mcp.GetRteProgress();

// Events
mcp.ConnectionStatusChanged += (s, e) => { /* handle */ };
mcp.ToolResultReceived += (s, e) => { /* handle */ };
```

### GameStateEngine

Tracks real-time game state from traffic.

```csharp
// Singleton access
var game = GameStateEngine.Instance;

// Initialize
await game.InitializeAsync();

// Get player state
var player = game.GetPlayerState();

// Get city state
var city = game.GetCityState(cityId);

// Get all cities
var cities = game.GetAllCities();

// Get hero state
var hero = game.GetHeroState(heroId);

// Get all heroes
var heroes = game.GetAllHeroes();

// Get resources
var resources = game.GetResources();

// Get resource rates
var rates = game.GetResourceRates();

// Get active marches
var marches = game.GetActiveMarches();

// Get max marches
int maxMarches = game.GetMaxMarches();

// Get build progress
var buildProgress = game.GetBuildProgress();

// Get train progress
var trainProgress = game.GetTrainProgress();

// Get research progress
var researchProgress = game.GetResearchProgress();

// Get hero stamina
int stamina = game.GetHeroStamina(heroId);
int maxStamina = game.GetMaxStamina(heroId);

// Get snapshot
var snapshot = game.GetSnapshot();

// Events
game.StateChanged += (s, e) => { /* handle */ };
game.PacketReceived += (s, e) => { /* handle */ };
```

### ProtocolHandler

Handles AMF3 encoding/decoding and protocol database.

```csharp
// Singleton access
var protocol = ProtocolHandler.Instance;

// Initialize
await protocol.InitializeAsync();

// Decode AMF3 packet
var decoded = protocol.DecodeAmf3(packetBytes);

// Encode AMF3 packet
var encoded = protocol.EncodeAmf3(data);

// Get protocol action
var action = protocol.GetProtocolAction(actionId);

// Get all actions
var actions = protocol.GetAllActions();

// Search actions
var results = protocol.SearchActions("march");

// Register new protocol
protocol.RegisterProtocol(new ProtocolAction {
    Id = "custom.action",
    Name = "Custom Action",
    Direction = "outbound"
});

// Get statistics
int packetRate = protocol.GetPacketRate();
double latency = protocol.GetAverageLatency();
double successRate = protocol.GetDecodeSuccessRate();
int errorCount = protocol.GetErrorCount();
```

### ChatbotService

AI chat with RAG integration.

```csharp
// Singleton access
var chat = ChatbotService.Instance;

// Initialize
await chat.InitializeAsync();

// Send message
var response = await chat.SendMessageAsync("How do I upgrade my castle?");

// Send with context
var response = await chat.SendMessageAsync("What troops should I train?", new ChatContext {
    GameState = game.GetSnapshot(),
    RecentPackets = protocol.GetRecentPackets(10)
});

// Clear history
chat.ClearHistory();

// Events
chat.ResponseReceived += (s, e) => { /* handle */ };
chat.StreamingUpdate += (s, e) => { /* handle */ };
```

### AutoPilotService

Intelligent game automation.

```csharp
// Singleton access
var autopilot = AutoPilotService.Instance;

// Start autopilot
await autopilot.StartAsync(new AutoPilotConfig {
    Mode = AutoPilotMode.Growth,
    SafetyLimits = true,
    MaxActionsPerHour = 100
});

// Stop autopilot
await autopilot.StopAsync();

// Get status
var status = autopilot.GetStatus();
bool isRunning = autopilot.IsRunning;

// Get statistics
int activeTaskCount = autopilot.ActiveTaskCount;
int queueDepth = autopilot.QueueDepth;
double actionsPerMinute = autopilot.ActionsPerMinute;

// Events
autopilot.ActionExecuted += (s, e) => { /* handle */ };
autopilot.ErrorOccurred += (s, e) => { /* handle */ };
```

### CombatSimulator

Battle outcome prediction.

```csharp
// Singleton access
var combat = CombatSimulator.Instance;

// Simulate battle
var result = await combat.SimulateBattleAsync(new BattleConfig {
    Attacker = attackerTroops,
    Defender = defenderTroops,
    AttackerHero = attackerHero,
    DefenderHero = defenderHero
});

// Find optimal composition
var optimal = await combat.FindOptimalCompositionAsync(targetPower, budget);

// Analyze rally
var analysis = await combat.AnalyzeRallyAsync(rallyId);
```

### SessionRecorder

Session recording and replay.

```csharp
// Singleton access
var recorder = SessionRecorder.Instance;

// Start recording
recorder.StartRecording("my-session");

// Stop recording
var recording = recorder.StopRecording();

// Get recording
var recording = recorder.GetRecording(recordingId);

// Get all recordings
var recordings = recorder.GetAllRecordings();

// Replay recording
await recorder.ReplayAsync(recordingId, new ReplayOptions {
    Speed = 1.0,
    SkipDelays = false
});

// Events
recorder.PacketRecorded += (s, e) => { /* handle */ };
recorder.ReplayProgress += (s, e) => { /* handle */ };
```

### LlmIntegrationService

Local LLM integration.

```csharp
// Singleton access
var llm = LlmIntegrationService.Instance;

// Connect to LM Studio
await llm.ConnectAsync("http://localhost:1234/v1");

// Generate completion
var response = await llm.GenerateAsync("Explain this packet: ...");

// Stream completion
await foreach (var token in llm.StreamAsync("Explain this packet: ..."))
{
    Console.Write(token);
}

// Get statistics
bool isConnected = llm.IsConnected;
double tokensPerSecond = llm.GetTokensPerSecond();
double vramUsage = llm.GetVramUsage();
double gpuTemp = llm.GetGpuTemperature();
double inferenceProgress = llm.GetInferenceProgress();
```

### FiddlerBridge

Advanced Fiddler integration.

```csharp
// Singleton access
var fiddler = FiddlerBridge.Instance;

// Connect
await fiddler.ConnectAsync();

// Inject packet
await fiddler.InjectPacketAsync(packetBytes);

// Set breakpoint
fiddler.SetBreakpoint("march.send", BreakpointType.Request);

// Clear breakpoint
fiddler.ClearBreakpoint("march.send");

// Get statistics
bool isConnected = fiddler.IsConnected;
double throughput = fiddler.GetThroughput();
int sessionCount = fiddler.GetSessionCount();

// Events
fiddler.PacketCaptured += (s, e) => { /* handle */ };
fiddler.BreakpointHit += (s, e) => { /* handle */ };
```

### SettingsManager

Settings persistence.

```csharp
// Singleton access
var settings = SettingsManager.Instance;

// Get settings
var appSettings = settings.Settings;

// Update setting
settings.Settings.General.Theme = "Borg Dark";

// Save settings
await settings.SaveAsync();

// Reset to defaults
settings.ResetToDefaults();

// Export settings
await settings.ExportAsync("backup.json");

// Import settings
await settings.ImportAsync("backup.json");
```

### StatusBarManager

Status bar widget management.

```csharp
// Singleton access
var statusBar = StatusBarManager.Instance;

// Initialize
await statusBar.InitializeAsync();

// Get all widgets
var widgets = statusBar.GetAllWidgets();

// Update widget
statusBar.UpdateWidget("rag_progress", 75.0);

// Enable/disable widget
statusBar.SetWidgetEnabled("gpu_temp", true);

// Reorder widgets
statusBar.ReorderWidgets(new[] { "rag_progress", "rte_progress", "packets_sec" });
```

---

## MCP Tool APIs

### evony-rag (Port 3001)

```javascript
// Search knowledge base
{
  "tool": "search_knowledge",
  "arguments": {
    "query": "castle upgrade requirements",
    "limit": 10
  }
}

// Get protocol info
{
  "tool": "get_protocol_info",
  "arguments": {
    "action_id": "march.send"
  }
}

// Get game data
{
  "tool": "get_game_data",
  "arguments": {
    "category": "troops",
    "filter": "cavalry"
  }
}
```

### evony-rte (Port 3002)

```javascript
// Decode packet
{
  "tool": "decode_packet",
  "arguments": {
    "hex": "00 03 00 00 00 01..."
  }
}

// Analyze traffic
{
  "tool": "analyze_traffic",
  "arguments": {
    "duration_seconds": 60
  }
}

// Get recent packets
{
  "tool": "get_recent_packets",
  "arguments": {
    "limit": 50,
    "filter": "march"
  }
}
```

### evony-complete (Port 3006)

```javascript
// Browser control
{
  "tool": "browser_navigate",
  "arguments": {
    "url": "https://cc2.evony.com"
  }
}

// Game state
{
  "tool": "game_get_resources",
  "arguments": {}
}

// Autopilot
{
  "tool": "autopilot_start",
  "arguments": {
    "mode": "growth",
    "safety_limits": true
  }
}

// Status bar
{
  "tool": "statusbar_update",
  "arguments": {
    "widget": "rag_progress",
    "value": 75
  }
}
```

---

## Event Reference

### Connection Events

| Event | Source | Args |
|-------|--------|------|
| ConnectionStatusChanged | McpConnectionManager | ServerName, Status |
| Connected | FiddlerBridge | - |
| Disconnected | FiddlerBridge | Reason |

### Data Events

| Event | Source | Args |
|-------|--------|------|
| PacketReceived | GameStateEngine | Packet |
| StateChanged | GameStateEngine | ChangeType, Data |
| ResponseReceived | ChatbotService | Response |

### Automation Events

| Event | Source | Args |
|-------|--------|------|
| ActionExecuted | AutoPilotService | Action, Result |
| ErrorOccurred | AutoPilotService | Error |
| PacketRecorded | SessionRecorder | Packet |

### UI Events

| Event | Source | Args |
|-------|--------|------|
| WidgetUpdated | StatusBarManager | WidgetId, Value |
| ThemeChanged | ThemeManager | ThemeName |
| ShortcutPressed | KeyboardShortcutManager | ShortcutId |

---

## Error Codes

| Code | Description |
|------|-------------|
| E001 | MCP connection failed |
| E002 | Tool execution failed |
| E003 | AMF3 decode error |
| E004 | Fiddler not connected |
| E005 | LLM not available |
| E006 | Invalid packet format |
| E007 | Rate limit exceeded |
| E008 | Authentication required |
| E009 | Resource not found |
| E010 | Operation timeout |

---

*Last Updated: v6.0 "Borg Edition"*
