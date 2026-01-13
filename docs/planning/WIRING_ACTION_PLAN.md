# Svony Browser V7.0 - Complete Wiring Action Plan

## Executive Summary

**Total Features Identified:** 297
**Total Wiring Issues:** 43
**Lines of Code:** 31,048
**Services:** 34
**GUI Components:** 11 XAML files

This document provides a comprehensive, task-by-task action plan to complete all wiring and ensure 100% feature functionality with Playwright testing.

---

## Table of Contents

1. [Critical Infrastructure Wiring](#1-critical-infrastructure-wiring)
2. [Service Initialization](#2-service-initialization)
3. [MCP Connection Implementation](#3-mcp-connection-implementation)
4. [Fiddler Integration](#4-fiddler-integration)
5. [Chatbot & RAG Wiring](#5-chatbot--rag-wiring)
6. [Status Bar Wiring](#6-status-bar-wiring)
7. [Settings Persistence](#7-settings-persistence)
8. [Browser/Flash Integration](#8-browserflash-integration)
9. [Traffic Capture System](#9-traffic-capture-system)
10. [Playwright Test Suite](#10-playwright-test-suite)

---

## 1. Critical Infrastructure Wiring

### 1.1 App.xaml.cs Service Registration

**Status:** ❌ INCOMPLETE
**Priority:** CRITICAL
**Estimated Time:** 2 hours

The following 34 services need to be properly initialized in App.xaml.cs:

```
[ ] Task 1.1.1: Add service initialization in OnStartup
    - SettingsManager.Instance
    - ErrorHandler.Instance
    - ThemeManager.Instance
    - ConnectionPool.Instance
    - MemoryManager.Instance
    - MemoryGuard.Instance

[ ] Task 1.1.2: Add MCP services initialization
    - McpConnectionManager.Instance
    - RealDataProvider.Instance
    - LlmIntegrationService.Instance

[ ] Task 1.1.3: Add traffic/proxy services
    - ProxyMonitor.Instance
    - FiddlerBridge.Instance
    - TrafficPipeClient.Instance

[ ] Task 1.1.4: Add game services
    - SessionManager.Instance
    - GameStateEngine.Instance
    - ProtocolHandler.Instance
    - PacketAnalysisEngine.Instance

[ ] Task 1.1.5: Add automation services
    - AutoPilotService.Instance
    - VisualAutomationService.Instance
    - CombatSimulator.Instance
    - StrategicAdvisor.Instance

[ ] Task 1.1.6: Add utility services
    - ChatbotService.Instance
    - SessionRecorder.Instance
    - WebhookHub.Instance
    - ExportImportManager.Instance
    - StatusBarManager.Instance
    - KeyboardShortcutManager.Instance
    - FailsafeManager.Instance
    - DebugService.Instance
```

### 1.2 Program.cs CefSharp Initialization

**Status:** ✅ IMPLEMENTED (verify)
**Priority:** CRITICAL

```
[x] Task 1.2.1: Verify CefSettings configuration
[x] Task 1.2.2: Verify Flash plugin path (Assets/Plugins/pepflashplayer.dll)
[x] Task 1.2.3: Verify CefSharp runtime path (Assets/CefSharp/)
[ ] Task 1.2.4: Add error handling for missing assets
[ ] Task 1.2.5: Add startup validation dialog
```

---

## 2. Service Initialization

### 2.1 Singleton Pattern Verification

**Status:** ⚠️ PARTIAL
**Priority:** HIGH

```
[ ] Task 2.1.1: Verify all services use thread-safe lazy initialization
[ ] Task 2.1.2: Add IDisposable implementation to all services
[ ] Task 2.1.3: Wire up disposal in App.OnExit
[ ] Task 2.1.4: Add service health checks
```

### 2.2 Service Dependencies

**Status:** ❌ NOT WIRED
**Priority:** HIGH

```
[ ] Task 2.2.1: Create service dependency graph
[ ] Task 2.2.2: Implement initialization order
[ ] Task 2.2.3: Add circular dependency detection
[ ] Task 2.2.4: Add fallback for failed services
```

---

## 3. MCP Connection Implementation

### 3.1 McpConnectionManager Wiring

**Status:** ❌ INCOMPLETE
**Priority:** CRITICAL

The MCP connection manager has no actual network implementation.

```
[ ] Task 3.1.1: Implement WebSocket connection to MCP servers
    - evony-rte (Real-Time Engine)
    - evony-rag (RAG Knowledge Base)
    - evony-tools (Game Tools)

[ ] Task 3.1.2: Add connection retry logic
[ ] Task 3.1.3: Add heartbeat/keepalive
[ ] Task 3.1.4: Implement reconnection on disconnect
[ ] Task 3.1.5: Add connection status events
[ ] Task 3.1.6: Wire status to MainWindow indicators
```

### 3.2 MCP Tool Calls

**Status:** ⚠️ PARTIAL
**Priority:** HIGH

13 MCP tool calls identified:

| Server | Tool | Used In | Status |
|--------|------|---------|--------|
| evony-rte | amf_decode | ChatbotService | ⚠️ |
| evony-rte | protocol_lookup | ChatbotService | ⚠️ |
| evony-rte | protocol_search | ChatbotService | ⚠️ |
| evony-rte | traffic_list | ChatbotService | ⚠️ |
| evony-rte | traffic_capture | TrafficPipeClient | ⚠️ |
| evony-rag | evony_search | ChatbotService | ⚠️ |
| evony-rag | evony_query | ChatbotService | ⚠️ |
| evony-rag | evony_search | StrategicAdvisor | ⚠️ |
| evony-rag | evony_search | AutoPilotService | ⚠️ |
| evony-tools | calc_training | ChatbotService | ⚠️ |
| evony-tools | calc_march | ChatbotService | ⚠️ |
| evony-tools | generate_build_order | ChatbotService | ⚠️ |

```
[ ] Task 3.2.1: Implement CallToolAsync with proper JSON-RPC
[ ] Task 3.2.2: Add tool response parsing
[ ] Task 3.2.3: Add error handling for tool failures
[ ] Task 3.2.4: Add timeout handling
[ ] Task 3.2.5: Add tool result caching
```

---

## 4. Fiddler Integration

### 4.1 FiddlerBridge Wiring

**Status:** ❌ NOT WORKING
**Priority:** HIGH

```
[ ] Task 4.1.1: Verify named pipe name matches Fiddler extension
    - Current: Unknown
    - Required: "SvonyBrowserPipe" or similar

[ ] Task 4.1.2: Create Fiddler extension script
    - FiddlerScript to capture Evony traffic
    - Send to named pipe

[ ] Task 4.1.3: Implement pipe server in FiddlerBridge
[ ] Task 4.1.4: Add AMF decoding for captured traffic
[ ] Task 4.1.5: Wire to TrafficViewer control
[ ] Task 4.1.6: Add traffic filtering
```

### 4.2 Alternative: Direct Proxy

**Status:** ❌ NOT IMPLEMENTED
**Priority:** MEDIUM

```
[ ] Task 4.2.1: Consider implementing direct proxy (no Fiddler)
[ ] Task 4.2.2: Use System.Net.HttpListener or similar
[ ] Task 4.2.3: Add SSL/TLS interception
```

---

## 5. Chatbot & RAG Wiring

### 5.1 ChatbotService Integration

**Status:** ⚠️ PARTIAL
**Priority:** HIGH

```
[ ] Task 5.1.1: Wire ChatbotService to ChatbotPanel
[ ] Task 5.1.2: Implement message streaming
[ ] Task 5.1.3: Add context from GameStateEngine
[ ] Task 5.1.4: Wire quick actions to actual functionality
```

### 5.2 RAG Knowledge Base

**Status:** ❌ NOT CONNECTED
**Priority:** HIGH

```
[ ] Task 5.2.1: Verify evony-rag MCP server is running
[ ] Task 5.2.2: Test evony_search tool
[ ] Task 5.2.3: Test evony_query tool
[ ] Task 5.2.4: Add knowledge base status indicator
[ ] Task 5.2.5: Implement fallback for offline RAG
```

### 5.3 LLM Integration

**Status:** ⚠️ PARTIAL
**Priority:** MEDIUM

```
[ ] Task 5.3.1: Wire LlmIntegrationService to settings
[ ] Task 5.3.2: Support multiple backends (LM Studio, Ollama, OpenAI)
[ ] Task 5.3.3: Add streaming response support
[ ] Task 5.3.4: Add token counting
[ ] Task 5.3.5: Add conversation history management
```

---

## 6. Status Bar Wiring

### 6.1 StatusBar.xaml.cs

**Status:** ❌ NOT CONNECTED
**Priority:** MEDIUM

```
[ ] Task 6.1.1: Subscribe to McpConnectionManager.StatusChanged
[ ] Task 6.1.2: Subscribe to FiddlerBridge.ConnectionChanged
[ ] Task 6.1.3: Subscribe to ProxyMonitor.StatusChanged
[ ] Task 6.1.4: Update indicators on status change
```

### 6.2 StatusBarV4.xaml.cs

**Status:** ❌ NOT CONNECTED
**Priority:** MEDIUM

```
[ ] Task 6.2.1: Wire McpStatusLight to MCP status
[ ] Task 6.2.2: Wire FiddlerStatusLight to Fiddler status
[ ] Task 6.2.3: Wire TroopCountText to GameStateEngine
[ ] Task 6.2.4: Wire MarchCountText to GameStateEngine
[ ] Task 6.2.5: Wire McpToolCount to McpConnectionManager
[ ] Task 6.2.6: Wire McpResourceCount to McpConnectionManager
```

---

## 7. Settings Persistence

### 7.1 SettingsWindow.xaml.cs

**Status:** ❌ NOT PERSISTING
**Priority:** HIGH

```
[ ] Task 7.1.1: Load settings from SettingsManager on window open
[ ] Task 7.1.2: Save settings to SettingsManager on Save click
[ ] Task 7.1.3: Add validation before save
[ ] Task 7.1.4: Add "Apply" button for immediate effect
```

### 7.2 SettingsControlCenter.xaml.cs

**Status:** ⚠️ PARTIAL
**Priority:** HIGH

```
[ ] Task 7.2.1: Wire all toggles to AppSettings model
[ ] Task 7.2.2: Implement auto-save on toggle change
[ ] Task 7.2.3: Add settings categories navigation
[ ] Task 7.2.4: Add reset to defaults button
```

### 7.3 Settings Categories

96 settings identified across these categories:

| Category | Settings Count | Status |
|----------|---------------|--------|
| General | 8 | ⚠️ |
| Browser | 6 | ⚠️ |
| Proxy | 5 | ⚠️ |
| MCP | 6 | ❌ |
| LLM | 6 | ❌ |
| Fiddler | 4 | ❌ |
| Automation | 5 | ❌ |
| Traffic | 3 | ⚠️ |
| Chatbot | 3 | ⚠️ |
| StatusBar | 6 | ❌ |
| Webhooks | 3 | ❌ |
| Advanced | 3 | ❌ |

---

## 8. Browser/Flash Integration

### 8.1 CefSharp Browser Wiring

**Status:** ⚠️ PARTIAL
**Priority:** CRITICAL

```
[ ] Task 8.1.1: Verify browser initialization in MainWindow
[ ] Task 8.1.2: Wire left panel browser (AutoEvony.swf)
[ ] Task 8.1.3: Wire right panel browser (EvonyClient.swf)
[ ] Task 8.1.4: Add SWF loading validation
[ ] Task 8.1.5: Add Flash plugin detection
[ ] Task 8.1.6: Add error handling for missing SWF files
```

### 8.2 Panel Mode Switching

**Status:** ✅ IMPLEMENTED
**Priority:** LOW

```
[x] Task 8.2.1: Left Only mode
[x] Task 8.2.2: Right Only mode
[x] Task 8.2.3: Both Panels mode
[x] Task 8.2.4: Swap Panels
[ ] Task 8.2.5: Persist panel mode in settings
```

### 8.3 Web/SWF Toggle

**Status:** ⚠️ PARTIAL
**Priority:** MEDIUM

```
[ ] Task 8.3.1: Wire LeftWebToggle to load web URL
[ ] Task 8.3.2: Wire LeftSwfToggle to load SWF file
[ ] Task 8.3.3: Wire RightWebToggle to load web URL
[ ] Task 8.3.4: Wire RightSwfToggle to load SWF file
[ ] Task 8.3.5: Add URL/SWF path configuration
```

---

## 9. Traffic Capture System

### 9.1 TrafficViewer Control

**Status:** ⚠️ PARTIAL
**Priority:** HIGH

```
[ ] Task 9.1.1: Wire StartCapture to FiddlerBridge
[ ] Task 9.1.2: Wire StopCapture to FiddlerBridge
[ ] Task 9.1.3: Implement traffic filtering
[ ] Task 9.1.4: Add AMF decoding display
[ ] Task 9.1.5: Add export functionality
```

### 9.2 TrafficPipeClient

**Status:** ❌ NOT CONNECTED
**Priority:** HIGH

```
[ ] Task 9.2.1: Connect to evony-rte traffic_capture tool
[ ] Task 9.2.2: Implement real-time traffic streaming
[ ] Task 9.2.3: Add traffic entry parsing
[ ] Task 9.2.4: Wire to TrafficViewer
```

---

## 10. Playwright Test Suite

### 10.1 Test Infrastructure

**Status:** ❌ NOT CREATED
**Priority:** HIGH

```
[ ] Task 10.1.1: Create Playwright test project
[ ] Task 10.1.2: Set up test fixtures
[ ] Task 10.1.3: Create page object models
[ ] Task 10.1.4: Add test data fixtures
```

### 10.2 GUI Tests (44 buttons, 102 handlers)

```
[ ] Task 10.2.1: MainWindow tests
    - Panel mode switching (3 tests)
    - Server selection (5 tests)
    - Reload buttons (2 tests)
    - Cache clear (1 test)
    - SOL Editor launch (1 test)
    - Fiddler launch (1 test)
    - Settings open (1 test)
    - Keyboard shortcuts (10 tests)

[ ] Task 10.2.2: SettingsWindow tests
    - All settings load (1 test)
    - Settings save (1 test)
    - Browse dialogs (3 tests)
    - Cancel button (1 test)

[ ] Task 10.2.3: SettingsControlCenter tests
    - Category navigation (12 tests)
    - Toggle switches (50+ tests)
    - Slider controls (10+ tests)
    - Text inputs (15+ tests)

[ ] Task 10.2.4: ChatbotPanel tests
    - Send message (1 test)
    - Quick actions (5 tests)
    - Menu options (3 tests)
    - Message display (1 test)

[ ] Task 10.2.5: TrafficViewer tests
    - Start/Stop capture (2 tests)
    - Clear traffic (1 test)
    - Export traffic (1 test)
    - Filter traffic (2 tests)
    - Select entry (1 test)

[ ] Task 10.2.6: ProtocolExplorer tests
    - Search protocols (1 test)
    - Select action (1 test)
    - Copy to clipboard (1 test)
    - Test in chat (1 test)
```

### 10.3 Service Tests (134 async methods)

```
[ ] Task 10.3.1: McpConnectionManager tests
    - Connect/Disconnect (2 tests)
    - Tool calls (13 tests)
    - Status events (2 tests)

[ ] Task 10.3.2: ChatbotService tests
    - Send message (1 test)
    - Protocol query (1 test)
    - Knowledge query (1 test)
    - Calculation (1 test)

[ ] Task 10.3.3: SettingsManager tests
    - Load settings (1 test)
    - Save settings (1 test)
    - Encryption (2 tests)

[ ] Task 10.3.4: FiddlerBridge tests
    - Connect (1 test)
    - Capture start/stop (2 tests)
    - Session handling (2 tests)
```

### 10.4 Integration Tests

```
[ ] Task 10.4.1: Full workflow tests
    - App startup to ready state
    - Load SWF files
    - Capture traffic
    - Decode AMF
    - Chat with RAG

[ ] Task 10.4.2: Error handling tests
    - Missing assets
    - MCP server down
    - Invalid settings
```

### 10.5 Test Coverage Target

| Category | Tests | Target |
|----------|-------|--------|
| GUI Buttons | 44 | 100% |
| Event Handlers | 102 | 100% |
| Service Methods | 134 | 80% |
| Settings | 96 | 100% |
| MCP Tools | 13 | 100% |
| Status Indicators | 26 | 100% |
| **TOTAL** | **415** | **95%+** |

---

## Implementation Priority Order

### Phase 1: Critical Infrastructure (Week 1)
1. App.xaml.cs service registration
2. Program.cs CefSharp verification
3. McpConnectionManager network implementation
4. SettingsManager persistence

### Phase 2: Core Features (Week 2)
5. FiddlerBridge implementation
6. TrafficViewer wiring
7. ChatbotService RAG integration
8. StatusBar wiring

### Phase 3: UI Polish (Week 3)
9. SettingsControlCenter full wiring
10. Browser panel improvements
11. Error handling and validation

### Phase 4: Testing (Week 4)
12. Playwright test infrastructure
13. GUI tests
14. Service tests
15. Integration tests

---

## Files to Modify

| File | Changes Required | Priority |
|------|------------------|----------|
| App.xaml.cs | Service initialization | CRITICAL |
| Program.cs | Asset validation | HIGH |
| MainWindow.xaml.cs | Status indicator wiring | HIGH |
| McpConnectionManager.cs | Network implementation | CRITICAL |
| FiddlerBridge.cs | Pipe implementation | HIGH |
| ChatbotService.cs | RAG tool verification | HIGH |
| SettingsWindow.xaml.cs | SettingsManager integration | HIGH |
| SettingsControlCenter.xaml.cs | Toggle wiring | MEDIUM |
| StatusBar.xaml.cs | Event subscriptions | MEDIUM |
| StatusBarV4.xaml.cs | Event subscriptions | MEDIUM |
| TrafficViewer.xaml.cs | FiddlerBridge wiring | HIGH |

---

## Success Criteria

- [ ] All 34 services properly initialized
- [ ] MCP connections working (evony-rte, evony-rag, evony-tools)
- [ ] Fiddler traffic capture functional
- [ ] Chatbot responding with RAG knowledge
- [ ] All 96 settings persisting correctly
- [ ] All 26 status indicators updating
- [ ] SWF files loading in browser panels
- [ ] Playwright tests: 95%+ pass rate
- [ ] No console errors on startup
- [ ] No unhandled exceptions

---

## Notes

- This is a .NET Framework 4.6.2 project targeting CefSharp 84.4.10 for Flash support
- All services use singleton pattern with lazy initialization
- MCP servers are external processes that need to be running
- Fiddler integration requires Fiddler to be installed separately
