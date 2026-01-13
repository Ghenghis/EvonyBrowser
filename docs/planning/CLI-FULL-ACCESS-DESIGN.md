# Svony Browser CLI - Full Access Design Document

## Gap Analysis

### Current CLI Coverage

The existing CLI (`cli/svony-cli/index.js`) provides:
- Protocol lookup, list, search
- AMF encode/decode
- Traffic list, get, clear, export
- Calculator (training, march)
- Knowledge base search/ask
- Server start/status
- Basic config display

### Missing Features (22 Services Not Accessible)

| Service | CLI Access | MCP Tools | Gap |
|---------|------------|-----------|-----|
| AnalyticsDashboard | ❌ | ❌ | Full |
| AutoPilotService | ❌ | Partial | Commands needed |
| ChatbotService | ❌ | ❌ | Full |
| CombatSimulator | ❌ | Partial | Commands needed |
| ExportImportManager | ❌ | ❌ | Full |
| FiddlerBridge | ❌ | Partial | Commands needed |
| GameStateEngine | ❌ | Partial | Commands needed |
| LlmIntegrationService | ❌ | Partial | Commands needed |
| MapScanner | ❌ | Partial | Commands needed |
| McpConnectionManager | ❌ | ❌ | Full |
| MultiAccountOrchestrator | ❌ | Partial | Commands needed |
| PacketAnalysisEngine | ❌ | Partial | Commands needed |
| PromptTemplateEngine | ❌ | ❌ | Full |
| ProtocolFuzzer | ❌ | Partial | Commands needed |
| ProtocolHandler | Partial | Partial | Enhancement needed |
| ProxyMonitor | ❌ | ❌ | Full |
| SessionManager | ❌ | ❌ | Full |
| SessionRecorder | ❌ | Partial | Commands needed |
| StatusBarManager | ❌ | Partial | Commands needed |
| StrategicAdvisor | ❌ | Partial | Commands needed |
| TrafficPipeClient | ❌ | ❌ | Full |
| WebhookHub | ❌ | Partial | Commands needed |

### Missing UI Control

| Control | CLI Access | Gap |
|---------|------------|-----|
| MainWindow | ❌ | Browser navigation, panel control |
| ChatbotPanel | ❌ | Send messages, get responses |
| PacketBuilder | ❌ | Build and inject packets |
| ProtocolExplorer | ❌ | Browse protocols |
| SettingsWindow | ❌ | All settings |
| StatusBarV4 | ❌ | Widget configuration |
| TrafficViewer | ❌ | Traffic filtering, selection |

---

## Comprehensive CLI Architecture

### Command Structure

```
svony
├── browser          # Browser control
│   ├── navigate     # Navigate to URL
│   ├── back         # Go back
│   ├── forward      # Go forward
│   ├── refresh      # Refresh page
│   ├── panel        # Panel control (left/right/both)
│   ├── screenshot   # Take screenshot
│   └── status       # Get browser status
│
├── session          # Session management
│   ├── list         # List sessions
│   ├── create       # Create new session
│   ├── switch       # Switch active session
│   ├── delete       # Delete session
│   ├── export       # Export session data
│   └── import       # Import session data
│
├── account          # Multi-account management
│   ├── list         # List accounts
│   ├── add          # Add account
│   ├── remove       # Remove account
│   ├── switch       # Switch active account
│   ├── sync         # Sync account data
│   └── coordinate   # Coordinate actions across accounts
│
├── game             # Game state
│   ├── status       # Full game status
│   ├── resources    # Resource levels and rates
│   ├── troops       # Troop counts
│   ├── heroes       # Hero information
│   ├── buildings    # Building status
│   ├── research     # Research status
│   ├── marches      # Active marches
│   ├── alliance     # Alliance info
│   └── events       # Active events
│
├── autopilot        # Automation
│   ├── start        # Start autopilot
│   ├── stop         # Stop autopilot
│   ├── status       # Get autopilot status
│   ├── config       # Configure autopilot
│   ├── tasks        # List/manage tasks
│   ├── schedule     # Schedule actions
│   └── logs         # View automation logs
│
├── combat           # Combat simulation
│   ├── simulate     # Simulate battle
│   ├── optimize     # Optimize composition
│   ├── rally        # Plan rally
│   ├── defense      # Analyze defense
│   └── history      # Combat history
│
├── map              # Map intelligence
│   ├── scan         # Scan area
│   ├── targets      # Find farm targets
│   ├── monsters     # Find monsters
│   ├── resources    # Find resource tiles
│   ├── players      # Track players
│   └── export       # Export map data
│
├── protocol         # Protocol analysis (existing + enhanced)
│   ├── lookup       # Look up action
│   ├── list         # List actions
│   ├── search       # Search actions
│   ├── learn        # Learn new action
│   ├── export       # Export protocol DB
│   └── import       # Import protocol DB
│
├── packet           # Packet operations
│   ├── capture      # Start/stop capture
│   ├── list         # List captured packets
│   ├── decode       # Decode packet
│   ├── encode       # Encode packet
│   ├── inject       # Inject packet
│   ├── replay       # Replay packets
│   ├── compare      # Compare packets
│   ├── search       # Search packets
│   └── export       # Export packets
│
├── fuzz             # Protocol fuzzing
│   ├── start        # Start fuzzing
│   ├── stop         # Stop fuzzing
│   ├── status       # Get fuzzing status
│   ├── results      # Get results
│   ├── config       # Configure fuzzer
│   └── export       # Export discoveries
│
├── fiddler          # Fiddler integration
│   ├── connect      # Connect to Fiddler
│   ├── disconnect   # Disconnect
│   ├── status       # Connection status
│   ├── capture      # Start/stop capture
│   ├── breakpoint   # Set/clear breakpoints
│   ├── inject       # Inject via Fiddler
│   └── export       # Export session
│
├── llm              # LLM integration
│   ├── status       # LLM connection status
│   ├── connect      # Connect to LM Studio
│   ├── disconnect   # Disconnect
│   ├── explain      # Explain packet
│   ├── generate     # Generate script
│   ├── decode       # Decode unknown
│   ├── ask          # Ask question
│   ├── stats        # Inference stats
│   └── config       # Configure LLM
│
├── chat             # Chatbot interface
│   ├── send         # Send message
│   ├── history      # Get chat history
│   ├── clear        # Clear history
│   ├── context      # Set/get context
│   ├── templates    # Manage templates
│   └── export       # Export chat
│
├── analytics        # Analytics dashboard
│   ├── summary      # Get summary
│   ├── power        # Power history
│   ├── resources    # Resource efficiency
│   ├── combat       # Combat stats
│   ├── activity     # Activity patterns
│   └── export       # Export analytics
│
├── webhook          # Webhook notifications
│   ├── list         # List webhooks
│   ├── add          # Add webhook
│   ├── remove       # Remove webhook
│   ├── test         # Test webhook
│   ├── events       # Configure events
│   └── logs         # View webhook logs
│
├── recording        # Session recording
│   ├── start        # Start recording
│   ├── stop         # Stop recording
│   ├── list         # List recordings
│   ├── play         # Play recording
│   ├── export       # Export recording
│   └── import       # Import recording
│
├── statusbar        # Status bar control
│   ├── widgets      # List/configure widgets
│   ├── update       # Update widget value
│   ├── show         # Show widget
│   ├── hide         # Hide widget
│   ├── layout       # Configure layout
│   └── presets      # Save/load presets
│
├── settings         # Application settings
│   ├── get          # Get setting
│   ├── set          # Set setting
│   ├── list         # List all settings
│   ├── reset        # Reset to defaults
│   ├── export       # Export settings
│   └── import       # Import settings
│
├── mcp              # MCP server management
│   ├── status       # All MCP server status
│   ├── connect      # Connect to MCP server
│   ├── disconnect   # Disconnect
│   ├── list         # List available tools
│   ├── call         # Call MCP tool directly
│   └── logs         # View MCP logs
│
├── export           # Data export
│   ├── all          # Export everything
│   ├── protocols    # Export protocols
│   ├── packets      # Export packets
│   ├── sessions     # Export sessions
│   ├── analytics    # Export analytics
│   └── config       # Export configuration
│
├── import           # Data import
│   ├── protocols    # Import protocols
│   ├── packets      # Import packets
│   ├── sessions     # Import sessions
│   └── config       # Import configuration
│
├── server           # Server management (existing)
│   ├── start        # Start WebSocket server
│   ├── stop         # Stop server
│   └── status       # Server status
│
└── config           # Configuration (enhanced)
    ├── show         # Show all config
    ├── set          # Set config value
    ├── reset        # Reset to defaults
    └── validate     # Validate configuration
```

---

## MCP Server Enhancement

### New Unified MCP Server: evony-complete

This server provides **ALL** tools needed for full browser control.

### Tool Categories

#### 1. Browser Control Tools (10)
```javascript
browser_navigate       // Navigate to URL
browser_back           // Go back
browser_forward        // Go forward
browser_refresh        // Refresh page
browser_panel_control  // Control panels (left/right/both/swap)
browser_screenshot     // Take screenshot
browser_get_url        // Get current URL
browser_get_title      // Get page title
browser_execute_js     // Execute JavaScript
browser_get_status     // Get browser status
```

#### 2. Session Tools (8)
```javascript
session_list           // List all sessions
session_create         // Create new session
session_switch         // Switch active session
session_delete         // Delete session
session_get_info       // Get session info
session_export         // Export session
session_import         // Import session
session_sync           // Sync session data
```

#### 3. Account Tools (8)
```javascript
account_list           // List accounts
account_add            // Add account
account_remove         // Remove account
account_switch         // Switch active account
account_get_info       // Get account info
account_sync           // Sync account data
account_coordinate     // Coordinate multi-account actions
account_get_status     // Get all account statuses
```

#### 4. Game State Tools (12)
```javascript
game_get_status        // Full game status
game_get_resources     // Resource levels and rates
game_get_troops        // Troop counts by type
game_get_heroes        // Hero information
game_get_buildings     // Building status
game_get_research      // Research status
game_get_marches       // Active marches
game_get_alliance      // Alliance info
game_get_events        // Active events
game_get_mail          // Mail/messages
game_get_reports       // Battle reports
game_track_changes     // Track state changes
```

#### 5. AutoPilot Tools (10)
```javascript
autopilot_start        // Start autopilot
autopilot_stop         // Stop autopilot
autopilot_pause        // Pause autopilot
autopilot_resume       // Resume autopilot
autopilot_get_status   // Get status
autopilot_get_config   // Get configuration
autopilot_set_config   // Set configuration
autopilot_add_task     // Add task
autopilot_remove_task  // Remove task
autopilot_get_logs     // Get automation logs
```

#### 6. Combat Tools (8)
```javascript
combat_simulate        // Simulate battle
combat_optimize        // Optimize composition
combat_plan_rally      // Plan rally attack
combat_analyze_defense // Analyze defense
combat_get_history     // Get combat history
combat_calculate_power // Calculate power
combat_compare_armies  // Compare armies
combat_get_buffs       // Get active buffs
```

#### 7. Map Tools (8)
```javascript
map_scan_area          // Scan area
map_find_targets       // Find farm targets
map_find_monsters      // Find monsters
map_find_resources     // Find resource tiles
map_track_players      // Track player movements
map_get_tile_info      // Get tile information
map_calculate_distance // Calculate distance
map_export_data        // Export map data
```

#### 8. Packet Tools (12)
```javascript
packet_capture_start   // Start capture
packet_capture_stop    // Stop capture
packet_list            // List captured packets
packet_get             // Get packet details
packet_decode          // Decode packet
packet_encode          // Encode packet
packet_inject          // Inject packet
packet_replay          // Replay packets
packet_compare         // Compare packets
packet_search          // Search packets
packet_export          // Export packets
packet_clear           // Clear captured packets
```

#### 9. Protocol Tools (8)
```javascript
protocol_lookup        // Look up action
protocol_list          // List actions
protocol_search        // Search actions
protocol_learn         // Learn new action
protocol_export        // Export protocol DB
protocol_import        // Import protocol DB
protocol_validate      // Validate action
protocol_generate_docs // Generate documentation
```

#### 10. Fuzzing Tools (6)
```javascript
fuzz_start             // Start fuzzing
fuzz_stop              // Stop fuzzing
fuzz_get_status        // Get status
fuzz_get_results       // Get results
fuzz_set_config        // Set configuration
fuzz_export_discoveries // Export discoveries
```

#### 11. Fiddler Tools (8)
```javascript
fiddler_connect        // Connect to Fiddler
fiddler_disconnect     // Disconnect
fiddler_get_status     // Get status
fiddler_capture_start  // Start capture
fiddler_capture_stop   // Stop capture
fiddler_set_breakpoint // Set breakpoint
fiddler_inject         // Inject packet
fiddler_export_session // Export session
```

#### 12. LLM Tools (10)
```javascript
llm_connect            // Connect to LM Studio
llm_disconnect         // Disconnect
llm_get_status         // Get status
llm_explain_packet     // Explain packet
llm_generate_script    // Generate script
llm_decode_unknown     // Decode unknown structure
llm_ask                // Ask question
llm_get_stats          // Get inference stats
llm_set_config         // Set configuration
llm_list_models        // List available models
```

#### 13. Chat Tools (8)
```javascript
chat_send              // Send message
chat_get_history       // Get history
chat_clear             // Clear history
chat_set_context       // Set context
chat_get_context       // Get context
chat_list_templates    // List templates
chat_use_template      // Use template
chat_export            // Export chat
```

#### 14. Analytics Tools (8)
```javascript
analytics_get_summary  // Get summary
analytics_get_power    // Power history
analytics_get_resources // Resource efficiency
analytics_get_combat   // Combat stats
analytics_get_activity // Activity patterns
analytics_set_period   // Set analysis period
analytics_export       // Export analytics
analytics_reset        // Reset analytics
```

#### 15. Webhook Tools (8)
```javascript
webhook_list           // List webhooks
webhook_add            // Add webhook
webhook_remove         // Remove webhook
webhook_test           // Test webhook
webhook_set_events     // Configure events
webhook_get_logs       // Get logs
webhook_enable         // Enable webhook
webhook_disable        // Disable webhook
```

#### 16. Recording Tools (8)
```javascript
recording_start        // Start recording
recording_stop         // Stop recording
recording_list         // List recordings
recording_get          // Get recording
recording_play         // Play recording
recording_export       // Export recording
recording_import       // Import recording
recording_delete       // Delete recording
```

#### 17. StatusBar Tools (8)
```javascript
statusbar_get_widgets  // Get all widgets
statusbar_update       // Update widget value
statusbar_show         // Show widget
statusbar_hide         // Hide widget
statusbar_set_layout   // Set layout
statusbar_save_preset  // Save preset
statusbar_load_preset  // Load preset
statusbar_reset        // Reset to defaults
```

#### 18. Settings Tools (8)
```javascript
settings_get           // Get setting
settings_set           // Set setting
settings_list          // List all settings
settings_reset         // Reset to defaults
settings_export        // Export settings
settings_import        // Import settings
settings_validate      // Validate settings
settings_get_schema    // Get settings schema
```

#### 19. MCP Management Tools (6)
```javascript
mcp_get_status         // Get all MCP status
mcp_connect            // Connect to server
mcp_disconnect         // Disconnect from server
mcp_list_tools         // List available tools
mcp_call_tool          // Call tool directly
mcp_get_logs           // Get MCP logs
```

#### 20. Export/Import Tools (8)
```javascript
export_all             // Export everything
export_protocols       // Export protocols
export_packets         // Export packets
export_sessions        // Export sessions
import_protocols       // Import protocols
import_packets         // Import packets
import_sessions        // Import sessions
import_validate        // Validate import file
```

---

## Total Tool Count

| Category | Tools |
|----------|-------|
| Browser Control | 10 |
| Session | 8 |
| Account | 8 |
| Game State | 12 |
| AutoPilot | 10 |
| Combat | 8 |
| Map | 8 |
| Packet | 12 |
| Protocol | 8 |
| Fuzzing | 6 |
| Fiddler | 8 |
| LLM | 10 |
| Chat | 8 |
| Analytics | 8 |
| Webhook | 8 |
| Recording | 8 |
| StatusBar | 8 |
| Settings | 8 |
| MCP Management | 6 |
| Export/Import | 8 |
| **TOTAL** | **168** |

---

## IDE Configuration Files

### Claude Desktop (`claude_desktop_config.json`)
```json
{
  "mcpServers": {
    "evony-complete": {
      "command": "node",
      "args": ["/path/to/mcp-servers/evony-complete/index.js"],
      "env": {
        "SVONY_BROWSER_HOST": "localhost",
        "SVONY_BROWSER_PORT": "9876",
        "LM_STUDIO_HOST": "localhost",
        "LM_STUDIO_PORT": "1234"
      }
    }
  }
}
```

### Windsurf IDE (`.windsurf/mcp.json`)
```json
{
  "servers": [
    {
      "name": "evony-complete",
      "transport": "stdio",
      "command": "node",
      "args": ["/path/to/mcp-servers/evony-complete/index.js"],
      "capabilities": ["tools", "resources", "prompts"]
    }
  ]
}
```

### LM Studio Integration
```json
{
  "mcp": {
    "enabled": true,
    "servers": [
      {
        "name": "evony-complete",
        "endpoint": "ws://localhost:9877",
        "tools": "all"
      }
    ]
  }
}
```

---

## Implementation Priority

### Phase 1: Core Browser Control
1. Browser navigation tools
2. Session management tools
3. Settings tools

### Phase 2: Game Integration
4. Game state tools
5. AutoPilot tools
6. Combat tools

### Phase 3: Analysis Tools
7. Packet tools (enhanced)
8. Protocol tools (enhanced)
9. Fuzzing tools

### Phase 4: AI Integration
10. LLM tools
11. Chat tools
12. Analytics tools

### Phase 5: Advanced Features
13. Map tools
14. Webhook tools
15. Recording tools
16. Multi-account tools

### Phase 6: Management
17. StatusBar tools
18. MCP management tools
19. Export/Import tools
