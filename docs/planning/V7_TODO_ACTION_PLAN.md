# Svony Browser V7.0 - Complete Action Plan

## Project Status: 99.99% NON-FUNCTIONAL

The GUI exists but almost nothing actually works. This document lists ALL issues that must be fixed to make the project usable.

---

## CRITICAL PRIORITY (App Won't Function Without These)

### 1. SWF Loading - COMPLETELY BROKEN
- [ ] AutoEvony.swf does not load at startup
- [ ] EvonyClient.swf does not load at startup
- [ ] Flash plugin (pepflashplayer.dll) not integrated
- [ ] CefSharp Flash support not configured
- [ ] SWF path settings don't actually load the files
- [ ] No fallback when SWF files are missing
- [ ] Browser panels show blank/empty content

### 2. Browser Panels - NON-FUNCTIONAL
- [ ] Left panel (AutoEvony Bot) shows nothing
- [ ] Right panel (Evony Client) shows nothing
- [ ] Navigation doesn't work
- [ ] Page loading doesn't work
- [ ] JavaScript execution doesn't work
- [ ] Cookie/session management doesn't work

### 3. Fiddler Integration - NOT CONNECTED
- [ ] ProxyMonitor detects Fiddler but doesn't capture traffic
- [ ] FiddlerBridge.cs has code but no actual Fiddler API integration
- [ ] Traffic capture doesn't actually capture anything
- [ ] No HTTPS decryption configured
- [ ] Proxy settings not applied to CefSharp

---

## HIGH PRIORITY (Core Features)

### 4. Traffic Viewer - EMPTY/NON-FUNCTIONAL
- [ ] Start Capture button does nothing useful
- [ ] No actual traffic is captured
- [ ] Traffic list always empty
- [ ] Export exports nothing
- [ ] Filter doesn't filter anything
- [ ] Details panel shows nothing

### 5. Protocol Handler - INCOMPLETE
- [ ] AMF decoding not connected to traffic
- [ ] Packet parsing doesn't receive real packets
- [ ] TODO at line 1040: "Send packet through network layer"
- [ ] No actual network layer implementation
- [ ] Packet injection doesn't inject anything

### 6. Chatbot / Co-Pilot - NOT CONNECTED
- [ ] Shows "Connecting..." forever
- [ ] LM Studio connection not established
- [ ] No actual LLM responses
- [ ] Quick actions don't do anything useful
- [ ] Chat history not persisted
- [ ] RAG integration not working
- [ ] RTE integration not working

### 7. MCP Servers - NOT STARTING
- [ ] evony-complete server doesn't start
- [ ] evony-v4 server doesn't start
- [ ] evony-cdp server doesn't start
- [ ] evony-advanced server doesn't start
- [ ] evony-rte server doesn't start
- [ ] evony-tools server doesn't start
- [ ] evony-rag server doesn't start
- [ ] Node.js not bundled/available
- [ ] Server processes not spawned
- [ ] No IPC communication established

---

## MEDIUM PRIORITY (Secondary Features)

### 8. Settings Window - PARTIALLY WORKING
- [ ] Server dropdown doesn't populate servers
- [ ] Proxy settings don't apply to browser
- [ ] Auto-start Fiddler doesn't work
- [ ] SWF path settings don't load files
- [ ] Settings may not persist correctly

### 9. SOL Editor - NOT FUNCTIONAL
- [ ] SOL Editor executable not bundled
- [ ] Fallback to folder doesn't help
- [ ] No way to edit Flash cookies

### 10. Protocol Explorer - EMPTY
- [ ] Action list is empty
- [ ] No protocol definitions loaded
- [ ] Search doesn't find anything
- [ ] Copy to clipboard copies nothing useful

### 11. Packet Builder - NON-FUNCTIONAL
- [ ] Can't build packets without protocol definitions
- [ ] Validate does nothing useful
- [ ] Preview shows nothing
- [ ] Inject doesn't inject
- [ ] Templates don't exist

### 12. Status Bar - SHOWS WRONG DATA
- [ ] Proxy status always "Not Running"
- [ ] Traffic count always 0
- [ ] Session status wrong
- [ ] Widgets show placeholder data

### 13. Combat Simulator - NOT CONNECTED
- [ ] No game data to simulate
- [ ] Calculations based on nothing
- [ ] Results meaningless

### 14. Strategic Advisor - NOT CONNECTED
- [ ] No game state to analyze
- [ ] Advice based on nothing
- [ ] Recommendations meaningless

### 15. Game State Engine - EMPTY
- [ ] No game data received
- [ ] State always empty
- [ ] Events never fire
- [ ] Snapshots contain nothing

---

## LOW PRIORITY (Nice to Have)

### 16. Map Scanner - NOT FUNCTIONAL
- [ ] No map data to scan
- [ ] Scanning does nothing

### 17. Session Recorder - NOT FUNCTIONAL
- [ ] No sessions to record
- [ ] Recording captures nothing

### 18. Multi-Account Orchestrator - NOT FUNCTIONAL
- [ ] No accounts to orchestrate
- [ ] Switching does nothing

### 19. AutoPilot Service - NOT FUNCTIONAL
- [ ] No game connection to automate
- [ ] Automation does nothing

### 20. Visual Automation - NOT FUNCTIONAL
- [ ] No UI elements to automate
- [ ] Clicks do nothing

### 21. Analytics Dashboard - EMPTY
- [ ] No data to analyze
- [ ] Charts show nothing

---

## TESTING (Currently Useless)

### 22. Unit Tests - TESTING NOTHING
- [ ] Tests pass but test mock data
- [ ] No real functionality tested
- [ ] Coverage meaningless

### 23. Playwright E2E Tests - CAN'T RUN
- [ ] App doesn't function to test
- [ ] Tests would all fail
- [ ] No meaningful E2E possible

---

## DOCUMENTATION (Severely Outdated)

### 24. README.md - WRONG VERSION
- [ ] Says v3.0, we're at v7.0
- [ ] Missing all v4-v7 features
- [ ] Installation instructions outdated
- [ ] Screenshots missing
- [ ] No 3D ASCII art

### 25. API Documentation - MISSING
- [ ] No API docs
- [ ] No protocol docs
- [ ] No MCP server docs

---

## ROOT CAUSE ANALYSIS

The project has:
1. **GUI Shell** - XAML files define UI ✓
2. **Click Handlers** - Methods exist ✓
3. **Service Classes** - Code files exist ✓
4. **BUT NO ACTUAL INTEGRATION** - Nothing is wired together ✗

### What's Missing:
1. **CefSharp → Flash → SWF** pipeline not working
2. **CefSharp → Proxy → Fiddler** pipeline not working
3. **Fiddler → Traffic → Protocol Handler** pipeline not working
4. **Protocol Handler → Game State** pipeline not working
5. **Game State → UI Updates** pipeline not working
6. **MCP Servers → Node.js** runtime not available
7. **Chatbot → LLM** connection not established

---

## ESTIMATED EFFORT

| Category | Issues | Effort |
|----------|--------|--------|
| Critical | 7 | 40+ hours |
| High | 14 | 60+ hours |
| Medium | 21 | 40+ hours |
| Low | 12 | 20+ hours |
| Testing | 4 | 10+ hours |
| Docs | 4 | 10+ hours |
| **TOTAL** | **62** | **180+ hours** |

---

## RECOMMENDED APPROACH

### Option A: Fix Incrementally
1. Get SWF loading working first
2. Then Fiddler traffic capture
3. Then protocol parsing
4. Then game state
5. Then UI updates
6. Then MCP servers
7. Then chatbot

### Option B: Start Fresh
Given the scope of issues, it may be faster to:
1. Keep the UI designs
2. Rewrite the service layer from scratch
3. Properly integrate each component
4. Test as you go

### Option C: Have Claude Code Finish
Claude Code has better context on:
- What was actually intended
- How components should connect
- What the working state should be

---

## FILES THAT NEED MAJOR WORK

1. `SvonyBrowser/MainWindow.xaml.cs` - Browser initialization
2. `SvonyBrowser/Program.cs` - CefSharp/Flash setup
3. `SvonyBrowser/Services/FiddlerBridge.cs` - Actual Fiddler integration
4. `SvonyBrowser/Services/ProtocolHandler.cs` - Network layer
5. `SvonyBrowser/Services/McpConnectionManager.cs` - Server spawning
6. `SvonyBrowser/Services/LlmIntegrationService.cs` - LLM connection
7. `SvonyBrowser/Services/GameStateEngine.cs` - Data flow
8. `SvonyBrowser/Controls/ChatbotPanel.xaml.cs` - LLM UI

---

## CONCLUSION

This project requires approximately **180+ hours** of development work to become functional. The GUI exists but is essentially a non-working prototype.

**Recommendation:** Have Claude Code finish the project as they have better context on the intended functionality and integration points.
