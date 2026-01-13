# Svony Browser Bug List & Fix Status

## Summary
This document tracks all identified build errors and their fix status during the codebase audit.

**Initial Errors:** ~150+ compile errors  
**Fixed:** ~80 errors  
**Remaining:** 35 unique errors (70 total occurrences)  
**Warnings:** 45+ (nullable reference, unused events/fields)

---

## 📊 Manus-Generated Codebase Overview

### Files Created by Manus (60 C# files):
| Category       | Count | Files                                                                          |
| -------------- | ----- | ------------------------------------------------------------------------------ |
| **Services**   | 28    | GameStateEngine, ProtocolHandler, McpConnectionManager, StatusBarManager, etc. |
| **Controls**   | 8     | ChatbotPanel, PacketBuilder, ProtocolExplorer, TrafficViewer, etc.             |
| **Models**     | 5     | AppSettings, ChatContext, ChatMessage, ProtocolAction, TrafficEntry            |
| **Core**       | 4     | App.xaml.cs, MainWindow.xaml.cs, SettingsControlCenter, GlobalUsings           |
| **Converters** | 1     | Converters.cs                                                                  |

### MCP Servers Created (7 servers):
- evony-complete, evony-rag, evony-rte, evony-tools
- evony-advanced, evony-cdp, evony-v4

### Architecture Issues Introduced by Manus:
1. **Missing method implementations** - Code references methods that don't exist
2. **Type mismatches** - Properties return wrong types expected by callers  
3. **Incomplete model classes** - Missing properties referenced elsewhere
4. **Event signature mismatches** - Delegates don't match handler signatures

---

## ✅ FIXED Issues

### 1. .csproj Duplicate Compile Items (NETSDK1022)
- **File:** `SvonyBrowser/SvonyBrowser.csproj`
- **Error:** Duplicate 'Compile' and 'Page' items were included
- **Fix:** Removed explicit `<Compile Include=...>` and `<Page Include=...>` entries - .NET SDK auto-includes with `ImplicitUsings` and `UseWPF`

### 2. CefSharp Package Reference
- **File:** `SvonyBrowser/SvonyBrowser.csproj`
- **Error:** `MC1000: Could not find assembly 'CefSharp'`
- **Fix:** Changed `CefSharp.Wpf` to `CefSharp.Wpf.NETCore` for .NET 6+ compatibility

### 3. XAML Trigger Target Error (MC4111)
- **File:** `SvonyBrowser/SettingsControlCenter.xaml`
- **Error:** Cannot find Trigger target 'thumbTransform'
- **Fix:** Changed TranslateTransform to direct Margin manipulation for toggle switch animation

### 4. Missing XAML Namespace (MC3000)
- **File:** `SvonyBrowser/SettingsControlCenter.xaml`
- **Error:** 'local' is an undeclared prefix
- **Fix:** Added `xmlns:local="clr-namespace:SvonyBrowser"`

### 5. Duplicate Class Definitions (CS0101)
- **Files:** Multiple service and model files
- **Classes Fixed:**
  - `ChatMessage`, `ChatRole`, `ChatContext` - removed from `ChatbotService.cs`
  - `ProtocolAction` - removed from `ProtocolHandler.cs`
  - `ProtocolDatabase` - renamed to `LocalProtocolDatabase` in `RealDataProvider.cs`
  - `GameStateSnapshot` - removed from `ExportImportManager.cs`
  - `TrafficData` - consolidated in `TrafficPipeClient.cs`
  - `McpConnectionStatus` - removed from `Models/ChatContext.cs`

### 6. Namespace Organization
- **Files:** `Models/ChatMessage.cs`, `Models/ProtocolAction.cs`
- **Fix:** Changed namespace from `SvonyBrowser.Services` to `SvonyBrowser.Models`
- **Updated:** `GlobalUsings.cs` to include `SvonyBrowser.Models`

### 7. Missing App.DataPath Property
- **File:** `App.xaml.cs`
- **Fix:** Added `DataPath` static property and initialization

### 8. Missing Settings Alias Properties
- **File:** `Models/AppSettings.cs`
- **Fix:** Added alias properties for UI compatibility:
  - `CheckForUpdates` → `CheckUpdates`
  - `CacheSizeMb` → `CacheSizeMB`
  - `EnableGpu` → `EnableGPU`
  - `AutoStart`, `RagPort`, `RagEnabled`, `RtePort`, `RteEnabled` → MCP properties
  - `AutoDecodeAmf` → `DecodeAmf`
  - `ActionDelayMs` → `ActionDelay`
  - `ShowLlmStats` → `ShowLlmTokens`
  - `MaxMemoryMb` → `MaxMemoryMB`

### 9. Missing Singleton Properties
- **File:** `TrafficPipeClient.cs`
- **Fix:** Added `Instance` singleton property

### 10. Missing State Properties
- **File:** `GameStateEngine.cs`
- **Properties Added:**
  - `GameStateSnapshot`: `TotalResources`, `TotalTroops`
  - `PlayerState`: `AllianceTag`, `AllianceName`, `VipLevel`
  - `HeroState`: `Defense`, `Leadership`, `Power`, `Quality`
  - `CityState`: `KeepLevel`

### 11. Missing ProtocolAction Properties
- **File:** `Models/ProtocolAction.cs`
- **Properties Added:** `FirstSeen`, `LastSeen`, `Occurrences`, `Direction`

### 12. Missing Stub Methods
- **File:** `ProtocolHandler.cs`
- **Methods Added:** `SendPacket()`, `GetAllProtocols()`, `RegisterProtocol()`

### 13. Missing SessionRecorder Methods
- **File:** `SessionRecorder.cs`
- **Methods Added:** `GetAllRecordings()`, `GetRecording()`, `ImportRecording()`
- **Type Added:** `SessionRecording` class

### 14. ReadLineAsync Parameter Fix
- **Files:** `TrafficPipeClient.cs`, `McpConnectionManager.cs`
- **Fix:** Removed CancellationToken parameter (not supported in .NET 6)

### 15. Type Conversion Fixes
- **File:** `AnalyticsDashboard.cs`
- **Fix:** Added explicit casts from double to long for resource properties

---

## ⚠️ REMAINING Issues (35 unique errors, 70 occurrences)

### Category A: Missing Model Properties (4 errors)
| File                 | Line               | Missing Property                           | Fix                               |
| -------------------- | ------------------ | ------------------------------------------ | --------------------------------- |
| `GameStateEngine.cs` | 315, 316, 329, 330 | `QueueItem.EndTime`, `QueueItem.StartTime` | Add properties to QueueItem class |

### Category B: StatusBarManager Missing Methods (18 errors)
| Missing Method                           | Expected Return           | File Line                                      |
| ---------------------------------------- | ------------------------- | ---------------------------------------------- |
| `double.Percentage`                      | N/A - wrong type expected | 969, 970, 971, 985, 986, 987, 1001, 1002, 1003 |
| `double.TimeRemaining`                   | N/A - wrong type expected | 970                                            |
| `GameStateEngine.GetActiveMarches()`     | int                       | 1014                                           |
| `GameStateEngine.GetMaxMarches()`        | int                       | 1015                                           |
| `GameStateEngine.GetResourceRates()`     | ResourceState             | 1048                                           |
| `ProtocolHandler.GetPacketRate()`        | double                    | 1067                                           |
| `ProtocolHandler.GetAverageLatency()`    | double                    | 1086                                           |
| `ProtocolHandler.GetDecodeSuccessRate()` | double                    | 1101                                           |
| `ProtocolHandler.GetErrorCount()`        | int                       | 1152                                           |
| `AutoPilotService.ActiveTaskCount`       | int                       | 1169                                           |
| `AutoPilotService.QueueDepth`            | int                       | 1184                                           |
| `AutoPilotService.ActionsPerMinute`      | double                    | 1201                                           |
| `ProxyMonitor.Instance`                  | singleton                 | 1132                                           |

### Category C: Type Conversion Issues (6 errors)
| File                      | Line     | Error                  | Fix                     |
| ------------------------- | -------- | ---------------------- | ----------------------- |
| `ExportImportManager.cs`  | 330      | JObject→ProtocolAction | Change method signature |
| `GameStateEngine.cs`      | 618      | Task→MarchState        | Fix TryGetValue call    |
| `PacketAnalysisEngine.cs` | 585      | PacketDirection→string | Add .ToString()         |
| `PacketAnalysisEngine.cs` | 586      | List→Dictionary        | Change property type    |
| `PacketAnalysisEngine.cs` | 599, 601 | KeyValuePair access    | Fix iteration code      |
| `ProtocolHandler.cs`      | 502      | ProtocolAction→string  | Fix assignment          |

### Category D: Missing Service Methods (5 errors)
| File                  | Line | Missing                                | Fix                         |
| --------------------- | ---- | -------------------------------------- | --------------------------- |
| `RealDataProvider.cs` | 285  | `ReadToEndAsync` overload              | Remove CancellationToken    |
| `ProtocolFuzzer.cs`   | 704  | `ProtocolHandler.GetProtocolAction()`  | Add method                  |
| `SessionRecorder.cs`  | 399  | `GameStateEngine()` constructor access | Make public or use Instance |

### Category E: Warnings (45+)
- **Nullable reference warnings:** CdpConnectionService.cs, VisualAutomationService.cs
- **Unused events:** BuildingCompleted, TrainingCompleted, SessionModified, ResponseReceived
- **Unused fields:** _clientPipe, _webSocket
- **Async methods without await:** FiddlerBridge.cs, ExportImportManager.cs

---

## 🔧 Recommended Fix Priority

### Priority 1 - Quick Wins (10 min each):
1. Add `StartTime`/`EndTime` to `QueueItem` class
2. Add `GetActiveMarches()`/`GetMaxMarches()` stubs to GameStateEngine
3. Fix `RealDataProvider.ReadToEndAsync` call
4. Add `ProxyMonitor.Instance` singleton

### Priority 2 - Type Fixes (15 min each):
1. Fix StatusBarManager to expect `double` not progress objects
2. Fix PacketAnalysisEngine type conversions
3. Fix ExportImportManager RegisterProtocol call

### Priority 3 - Stub Methods (20 min each):
1. Add ProtocolHandler metrics methods
2. Add AutoPilotService properties
3. Add GameStateEngine resource methods

---

## 📋 MCP Server Status

### ✅ All Servers Installed Successfully:
| Server         | npm install | Syntax Check |
| -------------- | ----------- | ------------ |
| evony-complete | ✅           | ✅            |
| evony-rag      | ✅           | ✅            |
| evony-rte      | ✅           | ✅            |
| evony-tools    | ✅           | ✅            |
| evony-advanced | ✅           | ✅            |
| evony-cdp      | ✅           | ✅            |
| evony-v4       | ✅           | ✅            |

---

## 📅 Last Updated
- **Date:** 2026-01-12 02:25 UTC-07:00
- **C# Build Status:** 35 unique errors (70 occurrences) + 45 warnings
- **MCP Servers:** All 7 installed and verified
- **Auditor:** Windsurf Cascade

---

## 📝 Audit Notes

### What Manus Did Well:
- Created comprehensive service architecture (28 services)
- Implemented MCP server integration framework
- Built UI controls with WPF best practices
- Structured models for game state tracking
- Created 7 functional MCP servers

### What Manus Did Poorly:
- Referenced methods/properties before implementing them
- Inconsistent type usage (double vs progress objects)
- Duplicate class definitions across files
- Wrong .NET 6 API usage (ReadLineAsync, CefSharp package)
- Event handler signature mismatches
- Left many stub methods unimplemented

### Windsurf Fixes Applied:
1. Fixed .csproj duplicate item inclusions
2. Updated CefSharp package for .NET 6+
3. Fixed XAML trigger targeting
4. Consolidated duplicate class definitions
5. Added missing model properties
6. Added alias properties for UI bindings
7. Fixed ReadLineAsync API calls
8. Added singleton patterns where missing
9. Created stub methods for missing implementations
10. Ran npm install on all MCP servers
