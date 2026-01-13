# Svony Browser v5.0 Action Plan

## Executive Summary

Version 5.0 represents the **AI-Native Evolution** of Svony Browser, transforming it from a tool collection into an intelligent, self-learning game automation platform. This version focuses on deep AI integration, computer vision, autonomous decision-making, and seamless multi-platform orchestration.

## Current State (v4.x)

| Category | Count | Status |
|----------|-------|--------|
| C# Services | 24 | ✅ Complete |
| XAML Controls | 10 | ✅ Complete |
| MCP Servers | 8 | ✅ Complete |
| CLI Tools | 5 | ✅ Complete |
| Fiddler Scripts | 6 | ✅ Complete |
| Documentation | 35 | ✅ Complete |
| **Total Files** | **127** | **Production Ready** |

### What We Have

1. **CDP Integration** - Control CefFlashBrowser via Chrome DevTools Protocol
2. **MCP Servers** - 200+ tools for Claude Desktop, Windsurf, LM Studio
3. **Status Bar** - 25+ customizable widgets with real-time progress
4. **Packet Analysis** - Deep AMF3 decoding and protocol fuzzing
5. **LLM Integration** - RTX 3090 Ti optimized local model support
6. **Visual Automation** - Coordinate-based clicking with UI element map

---

## v5.0 Vision: AI-Native Game Automation

### Core Philosophy

> **"From Tools to Intelligence"** - v5.0 transforms Svony Browser from a collection of automation tools into an intelligent agent that understands, learns, and adapts to the game.

---

## Phase 1: Computer Vision Engine (Week 1-2)

### 1.1 Screenshot Analysis with Vision LLM

**Goal**: Enable AI to "see" and understand game state from screenshots.

```
┌─────────────────────────────────────────────────────────────┐
│                    Vision Pipeline                           │
│                                                              │
│  Screenshot → Pre-process → Vision LLM → Structured Data    │
│                                                              │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐ │
│  │ Capture  │ → │ Crop/    │ → │ GPT-4V / │ → │ JSON     │ │
│  │ Screen   │   │ Enhance  │   │ LLaVA    │   │ Output   │ │
│  └──────────┘   └──────────┘   └──────────┘   └──────────┘ │
└─────────────────────────────────────────────────────────────┘
```

**Components to Build**:

| Component | File | Description |
|-----------|------|-------------|
| VisionService.cs | Services/ | Screenshot capture and pre-processing |
| VisionLlmClient.cs | Services/ | Integration with GPT-4V, LLaVA, Qwen-VL |
| ScreenParser.cs | Services/ | Parse vision output to structured data |
| evony-vision MCP | mcp-servers/ | Vision tools for Claude/Windsurf |

**New MCP Tools**:
- `vision_analyze_screen` - Full screen analysis
- `vision_find_element` - Find UI element by description
- `vision_read_text` - OCR for resource counts, timers
- `vision_detect_state` - Detect game state (city/map/dialog)
- `vision_compare_screenshots` - Detect changes between frames

### 1.2 Template Matching Engine

**Goal**: Fast, reliable detection of known UI elements without LLM.

**Components**:

| Component | Description |
|-----------|-------------|
| TemplateLibrary | 500+ pre-captured UI element templates |
| MatchingEngine | OpenCV-based template matching |
| ConfidenceScoring | Multi-scale matching with confidence |
| AutoCapture | Tool to capture new templates |

**Template Categories**:
- Buttons (Confirm, Cancel, Upgrade, Train, etc.)
- Icons (Resources, Troops, Buildings)
- Dialogs (Build menu, Research tree, Hero panel)
- Notifications (Attack warning, Resource full)
- Map elements (Cities, Monsters, Resources)

---

## Phase 2: Autonomous Agent Framework (Week 3-4)

### 2.1 Goal-Oriented Agent

**Goal**: AI that can achieve high-level objectives autonomously.

```
┌─────────────────────────────────────────────────────────────┐
│                    Agent Architecture                        │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │                    Goal Planner                       │   │
│  │  "Maximize power growth while staying safe"           │   │
│  └─────────────────────────┬────────────────────────────┘   │
│                            │                                 │
│  ┌─────────────────────────▼────────────────────────────┐   │
│  │                   Task Decomposer                     │   │
│  │  Goal → Sub-goals → Tasks → Actions                   │   │
│  └─────────────────────────┬────────────────────────────┘   │
│                            │                                 │
│  ┌─────────────────────────▼────────────────────────────┐   │
│  │                   Action Executor                     │   │
│  │  CDP clicks, packet injection, API calls              │   │
│  └─────────────────────────┬────────────────────────────┘   │
│                            │                                 │
│  ┌─────────────────────────▼────────────────────────────┐   │
│  │                   Feedback Loop                       │   │
│  │  Monitor results, adjust strategy, learn              │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

**Components to Build**:

| Component | File | Description |
|-----------|------|-------------|
| AgentCore.cs | Services/ | Main agent loop and state machine |
| GoalPlanner.cs | Services/ | High-level goal decomposition |
| TaskQueue.cs | Services/ | Priority queue for pending tasks |
| ActionExecutor.cs | Services/ | Execute actions via CDP/packets |
| FeedbackAnalyzer.cs | Services/ | Analyze results and learn |

**Pre-Built Goals**:
- `maximize_power` - Optimize for power growth
- `farm_resources` - Automated resource gathering
- `defend_city` - Keep city safe from attacks
- `complete_events` - Auto-complete daily events
- `train_troops` - Maintain troop production
- `upgrade_buildings` - Optimize build queue
- `research_tech` - Optimize research queue

### 2.2 Behavior Trees

**Goal**: Visual behavior definition for complex automation.

```
[Selector: Main Loop]
├── [Sequence: Handle Emergency]
│   ├── [Condition: Under Attack?]
│   ├── [Action: Recall Marches]
│   └── [Action: Activate Shield]
├── [Sequence: Daily Tasks]
│   ├── [Condition: Daily Reset?]
│   ├── [Action: Collect Rewards]
│   └── [Action: Start Events]
└── [Sequence: Growth Loop]
    ├── [Action: Check Build Queue]
    ├── [Action: Check Research Queue]
    └── [Action: Send Farm Marches]
```

**Components**:

| Component | Description |
|-----------|-------------|
| BehaviorTreeEngine | Execute behavior trees |
| BehaviorTreeEditor | Visual editor (XAML control) |
| NodeLibrary | Pre-built behavior nodes |
| TreeSerializer | Save/load trees as JSON |

---

## Phase 3: Learning & Adaptation (Week 5-6)

### 3.1 Play Pattern Learning

**Goal**: Learn from human gameplay to improve automation.

**Data Collection**:
- Record all clicks, timings, decisions
- Capture game state before/after actions
- Track outcomes (success/failure)
- Build action-outcome correlation

**Learning Pipeline**:
```
Human Play → Record → Analyze → Extract Patterns → Apply to Agent
```

**Components**:

| Component | Description |
|-----------|-------------|
| PlayRecorder | Record human gameplay sessions |
| PatternExtractor | Extract common patterns |
| TimingAnalyzer | Learn optimal timing |
| DecisionLearner | Learn decision criteria |

### 3.2 Reinforcement Learning Integration

**Goal**: Agent that improves through trial and error.

**Reward Signals**:
- Power increase → Positive
- Resource gain → Positive
- Troop loss → Negative
- Time wasted → Negative
- Goal achieved → Large positive

**Components**:

| Component | Description |
|-----------|-------------|
| RewardCalculator | Calculate reward from game state |
| PolicyNetwork | Neural network for decisions |
| ExperienceBuffer | Store experiences for training |
| RLTrainer | Train policy from experiences |

---

## Phase 4: Multi-Account Swarm (Week 7-8)

### 4.1 Swarm Intelligence

**Goal**: Coordinate multiple accounts as a unified swarm.

```
┌─────────────────────────────────────────────────────────────┐
│                    Swarm Controller                          │
│                                                              │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │ Account1 │  │ Account2 │  │ Account3 │  │ Account4 │    │
│  │ (Main)   │  │ (Farm)   │  │ (Farm)   │  │ (Scout)  │    │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘    │
│       │             │             │             │           │
│       └─────────────┴─────────────┴─────────────┘           │
│                           │                                  │
│                    Coordination Layer                        │
│       ┌───────────────────┴───────────────────┐             │
│       │  - Rally coordination                  │             │
│       │  - Resource transfer                   │             │
│       │  - Reinforcement dispatch              │             │
│       │  - Scouting network                    │             │
│       └───────────────────────────────────────┘             │
└─────────────────────────────────────────────────────────────┘
```

**Components**:

| Component | Description |
|-----------|-------------|
| SwarmController | Central coordination |
| AccountAgent | Per-account agent instance |
| TaskDistributor | Distribute tasks across accounts |
| ResourceBalancer | Balance resources between accounts |
| RallyCoordinator | Coordinate rally attacks |

### 4.2 Distributed Execution

**Goal**: Run agents across multiple machines.

**Architecture**:
```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Machine1  │     │   Machine2  │     │   Machine3  │
│  (Control)  │────▶│  (Worker)   │────▶│  (Worker)   │
│             │     │             │     │             │
│  - Planner  │     │  - Agent1   │     │  - Agent3   │
│  - Coord    │     │  - Agent2   │     │  - Agent4   │
└─────────────┘     └─────────────┘     └─────────────┘
```

**Components**:

| Component | Description |
|-----------|-------------|
| DistributedBus | Message bus for coordination |
| WorkerNode | Remote agent execution |
| TaskRouter | Route tasks to workers |
| StateSync | Synchronize state across nodes |

---

## Phase 5: Advanced Protocol Engineering (Week 9-10)

### 5.1 Protocol Synthesis

**Goal**: Generate valid packets from high-level intent.

```
Intent: "Train 1000 cavalry"
    ↓
Protocol Lookup: TrainTroop action
    ↓
Parameter Fill: { troopId: 4, count: 1000, buildingId: ... }
    ↓
AMF3 Encode: Binary packet
    ↓
Inject via Fiddler/CDP
```

**Components**:

| Component | Description |
|-----------|-------------|
| IntentParser | Parse natural language to intent |
| ProtocolSynthesizer | Generate packets from intent |
| ParameterResolver | Resolve dynamic parameters |
| PacketValidator | Validate before injection |

### 5.2 Protocol Reverse Engineering Assistant

**Goal**: AI-assisted discovery of new protocol actions.

**Workflow**:
1. Capture unknown packet
2. Analyze structure with LLM
3. Hypothesize purpose
4. Test hypothesis
5. Document if confirmed

**Components**:

| Component | Description |
|-----------|-------------|
| UnknownPacketAnalyzer | Analyze unknown packets |
| HypothesisGenerator | Generate purpose hypotheses |
| HypothesisTester | Test hypotheses safely |
| ProtocolDocumenter | Auto-document discoveries |

---

## Phase 6: Enhanced UI & Visualization (Week 11-12)

### 6.1 Real-Time Game Overlay

**Goal**: Transparent overlay showing AI insights on game.

**Features**:
- Resource projections
- Optimal click targets highlighted
- Threat indicators
- Task queue visualization
- Agent decision explanations

**Components**:

| Component | Description |
|-----------|-------------|
| OverlayWindow | Transparent WPF window |
| OverlayRenderer | Draw overlay elements |
| InsightEngine | Generate insights to display |
| PositionMapper | Map game coords to screen |

### 6.2 3D World Map Visualization

**Goal**: Interactive 3D view of game world.

**Features**:
- 3D terrain with cities
- Troop movement visualization
- Alliance territory coloring
- Resource spot highlighting
- Historical replay

**Components**:

| Component | Description |
|-----------|-------------|
| WorldMap3D.xaml | 3D viewport control |
| TerrainRenderer | Render terrain mesh |
| EntityRenderer | Render cities, troops |
| CameraController | Pan, zoom, rotate |

---

## Phase 7: Plugin Architecture (Week 13-14)

### 7.1 Plugin System

**Goal**: Allow community extensions.

**Plugin Types**:
- **Strategy Plugins** - Custom automation strategies
- **Vision Plugins** - Custom element detectors
- **Protocol Plugins** - Custom packet handlers
- **UI Plugins** - Custom panels and widgets

**Components**:

| Component | Description |
|-----------|-------------|
| PluginLoader | Load plugins from directory |
| PluginSandbox | Isolated execution environment |
| PluginAPI | API for plugin development |
| PluginMarketplace | Browse/install plugins |

### 7.2 Scripting Engine

**Goal**: User-defined automation scripts.

**Language**: Python or Lua embedded in C#

**Example Script**:
```python
@on_event("daily_reset")
def handle_daily_reset():
    go_to_city()
    collect_all_rewards()
    start_daily_events()
    
@on_condition(lambda: resources.food < 1000000)
def low_food_handler():
    send_gather_march("food")
```

**Components**:

| Component | Description |
|-----------|-------------|
| ScriptEngine | Python/Lua interpreter |
| ScriptAPI | Expose game functions to scripts |
| ScriptEditor | In-app script editor |
| ScriptDebugger | Debug running scripts |

---

## Implementation Priority

### High Priority (Must Have)

| Feature | Week | Impact |
|---------|------|--------|
| Vision LLM Integration | 1-2 | 🔥🔥🔥🔥🔥 |
| Autonomous Agent Core | 3-4 | 🔥🔥🔥🔥🔥 |
| Behavior Tree Engine | 4 | 🔥🔥🔥🔥 |
| Template Matching | 2 | 🔥🔥🔥🔥 |

### Medium Priority (Should Have)

| Feature | Week | Impact |
|---------|------|--------|
| Play Pattern Learning | 5-6 | 🔥🔥🔥 |
| Swarm Controller | 7-8 | 🔥🔥🔥 |
| Protocol Synthesis | 9 | 🔥🔥🔥 |
| Game Overlay | 11 | 🔥🔥🔥 |

### Lower Priority (Nice to Have)

| Feature | Week | Impact |
|---------|------|--------|
| RL Integration | 6 | 🔥🔥 |
| Distributed Execution | 8 | 🔥🔥 |
| 3D World Map | 12 | 🔥🔥 |
| Plugin System | 13-14 | 🔥🔥 |

---

## New MCP Tools for v5.0

### Vision Tools (15)
```
vision_analyze_screen
vision_find_element
vision_read_text
vision_detect_state
vision_compare_screenshots
vision_capture_template
vision_match_template
vision_track_element
vision_get_colors
vision_detect_animation
vision_read_timer
vision_read_resources
vision_detect_dialog
vision_find_button
vision_get_minimap
```

### Agent Tools (20)
```
agent_set_goal
agent_get_status
agent_pause
agent_resume
agent_add_task
agent_remove_task
agent_get_queue
agent_set_priority
agent_explain_decision
agent_override_action
behavior_load_tree
behavior_save_tree
behavior_start
behavior_stop
behavior_get_state
learning_start_recording
learning_stop_recording
learning_get_patterns
learning_apply_pattern
learning_reset
```

### Swarm Tools (12)
```
swarm_add_account
swarm_remove_account
swarm_get_accounts
swarm_set_role
swarm_coordinate_rally
swarm_transfer_resources
swarm_dispatch_reinforcement
swarm_sync_state
swarm_broadcast_command
swarm_get_status
swarm_set_leader
swarm_emergency_recall
```

### Protocol Tools (8)
```
protocol_synthesize
protocol_parse_intent
protocol_discover
protocol_test_hypothesis
protocol_document
protocol_get_unknown
protocol_analyze_unknown
protocol_validate
```

---

## File Structure for v5.0

```
SvonyBrowser/
├── Services/
│   ├── Vision/
│   │   ├── VisionService.cs
│   │   ├── VisionLlmClient.cs
│   │   ├── ScreenParser.cs
│   │   ├── TemplateLibrary.cs
│   │   └── TemplateMatcher.cs
│   ├── Agent/
│   │   ├── AgentCore.cs
│   │   ├── GoalPlanner.cs
│   │   ├── TaskQueue.cs
│   │   ├── ActionExecutor.cs
│   │   └── FeedbackAnalyzer.cs
│   ├── BehaviorTree/
│   │   ├── BehaviorTreeEngine.cs
│   │   ├── NodeLibrary.cs
│   │   └── TreeSerializer.cs
│   ├── Learning/
│   │   ├── PlayRecorder.cs
│   │   ├── PatternExtractor.cs
│   │   └── DecisionLearner.cs
│   ├── Swarm/
│   │   ├── SwarmController.cs
│   │   ├── AccountAgent.cs
│   │   ├── TaskDistributor.cs
│   │   └── RallyCoordinator.cs
│   └── Protocol/
│       ├── ProtocolSynthesizer.cs
│       ├── IntentParser.cs
│       └── ProtocolDiscovery.cs
├── Controls/
│   ├── BehaviorTreeEditor.xaml
│   ├── AgentDashboard.xaml
│   ├── SwarmMonitor.xaml
│   ├── VisionDebugger.xaml
│   └── GameOverlay.xaml
└── mcp-servers/
    ├── evony-vision/
    ├── evony-agent/
    ├── evony-swarm/
    └── evony-protocol/
```

---

## Success Metrics

| Metric | Target |
|--------|--------|
| Vision accuracy | >95% element detection |
| Agent autonomy | 8+ hours unattended |
| Task completion | >90% success rate |
| Multi-account sync | <100ms latency |
| Protocol synthesis | >80% valid packets |
| User satisfaction | 4.5+ stars |

---

## Timeline Summary

| Week | Phase | Deliverables |
|------|-------|--------------|
| 1-2 | Vision Engine | VisionService, TemplateMatcher, 15 MCP tools |
| 3-4 | Agent Framework | AgentCore, GoalPlanner, BehaviorTree, 20 MCP tools |
| 5-6 | Learning | PlayRecorder, PatternExtractor, DecisionLearner |
| 7-8 | Swarm | SwarmController, RallyCoordinator, 12 MCP tools |
| 9-10 | Protocol | ProtocolSynthesizer, Discovery, 8 MCP tools |
| 11-12 | UI | GameOverlay, 3D WorldMap, AgentDashboard |
| 13-14 | Plugins | PluginSystem, ScriptEngine |

---

## Getting Started with v5.0 Development

### Prerequisites

1. Complete v4.x implementation (✅ Done)
2. Install OpenCV for template matching
3. Set up vision LLM API access (GPT-4V or local LLaVA)
4. Prepare template image library

### First Steps

1. **Week 1, Day 1**: Create Vision service skeleton
2. **Week 1, Day 2**: Implement screenshot capture pipeline
3. **Week 1, Day 3**: Integrate GPT-4V for screen analysis
4. **Week 1, Day 4**: Build template matching engine
5. **Week 1, Day 5**: Create evony-vision MCP server

### Development Commands

```bash
# Create v5.0 branch
git checkout -b v5.0-development

# Install OpenCV
dotnet add package Emgu.CV

# Install Python.NET for scripting
dotnet add package Python.Runtime

# Run tests
dotnet test
```

---

## Conclusion

v5.0 transforms Svony Browser from a powerful tool collection into an **intelligent, autonomous game automation platform**. The combination of computer vision, goal-oriented agents, behavior trees, and swarm coordination creates a system that can play the game better than most humans while remaining fully controllable through Claude Desktop, Windsurf IDE, and LM Studio.

**Total New Components**: 40+ services, 10+ controls, 55+ MCP tools
**Estimated Development Time**: 14 weeks
**Expected Impact**: Game-changing autonomous automation
