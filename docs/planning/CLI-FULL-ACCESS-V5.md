# Svony Browser CLI v5.0 - Complete Access Documentation

## Overview

Svony CLI v5.0 provides **168 commands** across **20 categories** for complete control of the Svony Browser from Claude Desktop, Windsurf IDE, and LM Studio.

## Quick Start

```bash
# Install dependencies
cd cli/svony-cli
npm install

# Start the WebSocket server
svony server

# Or start in interactive mode
svony interactive

# Check connection status
svony status
```

## Command Categories

| Category | Commands | Description |
|----------|----------|-------------|
| browser | 10 | Browser navigation and control |
| session | 8 | Session management |
| account | 8 | Multi-account management |
| game | 12 | Game state queries |
| autopilot | 10 | Automation control |
| combat | 8 | Combat simulation |
| map | 8 | Map intelligence |
| packet | 12 | Packet operations |
| protocol | 8 | Protocol analysis |
| fuzz | 6 | Protocol fuzzing |
| fiddler | 8 | Fiddler integration |
| llm | 10 | LLM integration |
| chat | 8 | Chatbot interface |
| analytics | 8 | Analytics queries |
| webhook | 8 | Webhook management |
| recording | 8 | Session recording |
| statusbar | 8 | Status bar control |
| settings | 8 | Settings management |
| mcp | 6 | MCP server management |
| export/import | 8 | Data portability |

## Complete Command Reference

### 1. Browser Commands

```bash
# Navigate to URL
svony browser navigate https://cc2.evony.com -p left

# Go back/forward
svony browser back
svony browser forward

# Refresh page
svony browser refresh --hard

# Control panels
svony browser panel show_both
svony browser panel swap
svony browser panel maximize_left

# Take screenshot
svony browser screenshot --panel full --format png

# Get current URL/title
svony browser url
svony browser title

# Execute JavaScript
svony browser exec "document.title"

# Get full status
svony browser status
```

### 2. Session Commands

```bash
# List sessions
svony session list

# Create session
svony session create "Main Account" --server us1

# Switch session
svony session switch <sessionId>

# Delete session
svony session delete <sessionId>

# Get session info
svony session info

# Export/import session
svony session export --format json
svony session import <data>

# Sync with game
svony session sync
```

### 3. Account Commands

```bash
# List accounts
svony account list

# Add account
svony account add "Farm1" --server us1

# Switch account
svony account switch <accountId>

# Sync account
svony account sync

# Coordinate action across accounts
svony account coordinate rally --accounts id1,id2,id3 --timing simultaneous

# Get all account statuses
svony account status
```

### 4. Game State Commands

```bash
# Get full game status
svony game status

# Get resources
svony game resources --city 1

# Get troops
svony game troops --include-marching

# Get heroes
svony game heroes --id 123

# Get buildings
svony game buildings --type barracks

# Get research
svony game research --category military

# Get active marches
svony game marches --type attack

# Get alliance info
svony game alliance

# Get events
svony game events

# Get mail
svony game mail --folder reports --limit 20

# Get battle reports
svony game reports --type attack --limit 50
```

### 5. Autopilot Commands

```bash
# Start/stop autopilot
svony autopilot start --profile farming
svony autopilot stop

# Pause/resume
svony autopilot pause
svony autopilot resume

# Get status
svony autopilot status

# Get/set config
svony autopilot config
svony autopilot config --set '{"autoGather": true}'

# Add task
svony autopilot add-task gather --priority 8 --params '{"resourceType": "food"}'

# Remove task
svony autopilot remove-task <taskId>

# Get logs
svony autopilot logs --limit 100 --level error
```

### 6. Combat Commands

```bash
# Simulate battle
svony combat simulate \
  --attacker '{"troops": {"t12_cavalry": 100000}}' \
  --defender '{"troops": {"t12_infantry": 50000}}' \
  --iterations 1000

# Optimize composition
svony combat optimize \
  --target '{"power": 500000}' \
  --available '{"t12_cavalry": 200000, "t12_infantry": 100000}' \
  --objective minimize_losses

# Plan rally
svony combat rally 500 600 --size 5 --timing "2024-01-15 20:00"

# Analyze defense
svony combat defense --city 1

# Get combat history
svony combat history --type wins --limit 50

# Calculate power
svony combat power --troops '{"t12_cavalry": 100000}' --heroes 1,2,3

# Get active buffs
svony combat buffs
```

### 7. Map Commands

```bash
# Scan area
svony map scan 500 500 --radius 100

# Find farm targets
svony map targets 500 500 --radius 100 --max-power 100000 --limit 50

# Find monsters
svony map monsters 500 500 --radius 100 --min-level 10 --max-level 20

# Find resource tiles
svony map resources 500 500 --type food --min-level 8

# Get tile info
svony map tile 500 600

# Calculate distance
svony map distance 500 500 600 600

# Export map data
svony map export 500 500 --radius 100 --format csv
```

### 8. Packet Commands

```bash
# Start/stop capture
svony packet capture --start --filter "CastleBean.*"
svony packet capture --stop

# List packets
svony packet list --limit 50 --direction request --filter "upgrade"

# Get packet details
svony packet get <packetId>

# Decode packet
svony packet decode "00 11 22 33..." --format hex

# Encode packet
svony packet encode "CastleBean.upgradeBuilding" --params '{"buildingId": 1}'

# Inject packet
svony packet inject "CastleBean.upgradeBuilding" --params '{"buildingId": 1}'

# Replay packets
svony packet replay id1,id2,id3 --speed 2.0

# Compare packets
svony packet compare <id1> <id2>

# Search packets
svony packet search "upgradeBuilding" --in action

# Export packets
svony packet export --format pcap

# Clear packets
svony packet clear
```

### 9. Protocol Commands

```bash
# Look up action
svony protocol lookup "CastleBean.upgradeBuilding"

# List actions
svony protocol list --category castle

# Search actions
svony protocol search "upgrade"

# Learn from packet
svony protocol learn <packetId> --name "newAction" --description "Does something"

# Export database
svony protocol export --format markdown

# Import database
svony protocol import <data> --merge

# Generate docs
svony protocol docs --category castle --format markdown
```

### 10. Fuzzing Commands

```bash
# Start fuzzing
svony fuzz start \
  --mode action_discovery \
  --target "CastleBean" \
  --parallelism 3 \
  --delay 100

# Stop fuzzing
svony fuzz stop

# Get status
svony fuzz status

# Get results
svony fuzz results --type discoveries

# Export discoveries
svony fuzz export --format markdown
```

### 11. Fiddler Commands

```bash
# Connect to Fiddler
svony fiddler connect --pipe SvonyFiddlerPipe

# Disconnect
svony fiddler disconnect

# Get status
svony fiddler status

# Start/stop capture
svony fiddler capture --start --filter "*evony*"
svony fiddler capture --stop

# Set breakpoint
svony fiddler breakpoint "upgradeBuilding" --type request --action modify

# Inject via Fiddler
svony fiddler inject "CastleBean.upgradeBuilding" --params '{"buildingId": 1}'

# Export session
svony fiddler export --format saz
```

### 12. LLM Commands

```bash
# Connect to LM Studio
svony llm connect --host localhost --port 1234

# Disconnect
svony llm disconnect

# Get status
svony llm status

# Explain packet
svony llm explain <packetId> --detail technical

# Generate script
svony llm generate "auto-gather food tiles" --language python

# Decode unknown
svony llm decode "00 11 22 33..." --context "response from upgradeBuilding"

# Ask question
svony llm ask "How does the combat formula work?"

# Get stats
svony llm stats

# List models
svony llm models
```

### 13. Chat Commands

```bash
# Send message
svony chat send "What are my current resources?"

# Get history
svony chat history --limit 50

# Clear history
svony chat clear

# List templates
svony chat templates --category combat

# Use template
svony chat use-template combat_analysis --vars '{"targetPower": 500000}'

# Export chat
svony chat export --format markdown
```

### 14. Analytics Commands

```bash
# Get summary
svony analytics summary --period week

# Get power history
svony analytics power --period month

# Get resource efficiency
svony analytics resources --period week

# Get combat stats
svony analytics combat --period week

# Get activity patterns
svony analytics activity --period week

# Export analytics
svony analytics export --period all --format xlsx
```

### 15. Webhook Commands

```bash
# List webhooks
svony webhook list

# Add webhook
svony webhook add "Discord Alert" "https://discord.com/api/webhooks/..." \
  --platform discord \
  --events attack_incoming,rally_started

# Remove webhook
svony webhook remove <webhookId>

# Test webhook
svony webhook test <webhookId>

# Configure events
svony webhook events <webhookId> --events attack_incoming,rally_started

# Enable/disable
svony webhook enable <webhookId>
svony webhook disable <webhookId>
```

### 16. Recording Commands

```bash
# Start recording
svony recording start --name "Rally Session" --filter "rally*"

# Stop recording
svony recording stop

# List recordings
svony recording list

# Play recording
svony recording play <recordingId> --speed 2.0 --start-from 10

# Export recording
svony recording export <recordingId> --format pcap

# Delete recording
svony recording delete <recordingId>
```

### 17. Status Bar Commands

```bash
# List widgets
svony statusbar widgets

# Update widget
svony statusbar update rag_progress --value 75 --text "75%"

# Show/hide widget
svony statusbar show llm_tokens
svony statusbar hide gpu_temp

# Save/load preset
svony statusbar preset --save "development"
svony statusbar preset --load "production"

# Reset to defaults
svony statusbar reset
```

### 18. Settings Commands

```bash
# Get setting
svony settings get autopilot.enabled

# Set setting
svony settings set autopilot.enabled true

# List settings
svony settings list --category autopilot

# Reset settings
svony settings reset --category autopilot --yes

# Export settings
svony settings export --format json

# Import settings
svony settings import <data> --merge
```

### 19. MCP Commands

```bash
# Get status
svony mcp status

# Connect to server
svony mcp connect evony-rag

# Disconnect from server
svony mcp disconnect evony-rag

# List tools
svony mcp tools --server evony-complete --category packet

# Call tool directly
svony mcp call packet_list --params '{"limit": 10}'

# Get logs
svony mcp logs --server evony-complete --level error
```

### 20. Export/Import Commands

```bash
# Export all data
svony export all backup.json --no-packets

# Validate import file
svony import validate backup.json --type auto
```

## MCP Server Integration

### evony-complete (168 tools)

The main MCP server providing complete browser control:

```javascript
// Tool categories
- browser_*     (10 tools) - Browser control
- session_*     (8 tools)  - Session management
- account_*     (8 tools)  - Account management
- game_*        (12 tools) - Game state
- autopilot_*   (10 tools) - Automation
- combat_*      (8 tools)  - Combat simulation
- map_*         (8 tools)  - Map intelligence
- packet_*      (12 tools) - Packet operations
- protocol_*    (8 tools)  - Protocol analysis
- fuzz_*        (6 tools)  - Fuzzing
- fiddler_*     (8 tools)  - Fiddler integration
- llm_*         (10 tools) - LLM integration
- chat_*        (8 tools)  - Chat interface
- analytics_*   (8 tools)  - Analytics
- webhook_*     (8 tools)  - Webhooks
- recording_*   (8 tools)  - Recording
- statusbar_*   (8 tools)  - Status bar
- settings_*    (8 tools)  - Settings
- mcp_*         (6 tools)  - MCP management
- export_*/import_* (8 tools) - Data portability
```

### Resources (8 resources)

```
evony://browser/status    - Browser connection status
evony://game/state        - Current game state
evony://packets/recent    - Recent captured packets
evony://protocols/database - Protocol action database
evony://analytics/summary - Analytics summary
evony://settings/all      - All settings
evony://mcp/status        - MCP server status
evony://llm/status        - LLM connection status
```

### Prompts (5 prompts)

```
analyze_packet    - Analyze a captured packet
optimize_army     - Optimize army composition
generate_automation - Generate automation script
explain_protocol  - Explain protocol action
daily_strategy    - Generate daily recommendations
```

## IDE Configuration

### Claude Desktop

1. Copy `cli/claude-desktop-config-v5.json` to:
   - macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - Windows: `%APPDATA%\Claude\claude_desktop_config.json`

2. Set `SVONY_PATH` environment variable

3. Restart Claude Desktop

### Windsurf IDE

1. Copy `cli/windsurf-config-v5.json` to `.windsurf/mcp.json` in your workspace

2. Reload Windsurf

### LM Studio

1. Copy `cli/lm-studio-config-v5.json` to LM Studio's config directory

2. Load the `evony-7b-re` model

3. Enable MCP integration in settings

## RTX 3090 Ti Optimization

The LM Studio configuration is optimized for RTX 3090 Ti:

- **VRAM**: 24GB available, 22GB limit configured
- **GPU Layers**: All layers on GPU (-1)
- **Flash Attention**: Enabled for faster inference
- **FP16**: Enabled for memory efficiency
- **Batch Size**: 512 for optimal throughput
- **Context Length**: 8192 tokens

## Evony RAG Integration

The RAG system is trained on:

- Evony client source code
- AutoEvony source code
- Flash 10 documentation
- Game automation scripts
- Protocol documentation

Progress is shown in the Status Bar with the `rag_progress` widget.

## Evony RTE Integration

Real-time traffic analysis with:

- AMF3 decoding
- Pattern detection
- Differential comparison
- Protocol learning

Progress is shown in the Status Bar with the `rte_progress` widget.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Claude Desktop                            │
│                        Windsurf IDE                              │
│                        LM Studio                                 │
└───────────────────────────┬─────────────────────────────────────┘
                            │ MCP Protocol
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    evony-complete MCP Server                     │
│                    (168 tools, 8 resources, 5 prompts)          │
└───────────────────────────┬─────────────────────────────────────┘
                            │ WebSocket
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Svony Browser (WPF)                         │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐               │
│  │ Left Panel  │ │ Right Panel │ │ Status Bar  │               │
│  │ (cc2.evony) │ │ (Tools)     │ │ (Widgets)   │               │
│  └─────────────┘ └─────────────┘ └─────────────┘               │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐               │
│  │ Chatbot     │ │ Traffic     │ │ Protocol    │               │
│  │ Panel       │ │ Viewer      │ │ Explorer    │               │
│  └─────────────┘ └─────────────┘ └─────────────┘               │
└───────────────────────────┬─────────────────────────────────────┘
                            │ Named Pipe
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Fiddler                                  │
│                    (Traffic Capture)                             │
└─────────────────────────────────────────────────────────────────┘
```

## Version History

- **v5.0.0**: Complete CLI with 168 commands, full MCP integration
- **v4.0.0**: Status bar, packet analysis, LLM integration
- **v3.0.0**: Game-changing features (15 new services)
- **v2.0.0**: MCP server infrastructure
- **v1.0.0**: Initial release
