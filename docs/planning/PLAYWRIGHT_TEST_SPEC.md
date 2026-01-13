# Svony Browser V7.0 - Playwright Test Specification

## Overview

This document specifies all Playwright tests required to achieve 100% coverage of the 297 identified features in Svony Browser. The test suite is organized by component and feature category.

---

## Test Environment Setup

### Prerequisites

```powershell
# Install Playwright for .NET
dotnet add package Microsoft.Playwright
dotnet add package Microsoft.Playwright.MSTest

# Install browsers
pwsh bin/Debug/net462/playwright.ps1 install
```

### Test Project Structure

```
SvonyBrowser.Tests/
├── PageObjects/
│   ├── MainWindowPage.cs
│   ├── SettingsWindowPage.cs
│   ├── SettingsControlCenterPage.cs
│   ├── ChatbotPanelPage.cs
│   ├── TrafficViewerPage.cs
│   └── ProtocolExplorerPage.cs
├── Fixtures/
│   ├── AppFixture.cs
│   ├── TestData.cs
│   └── MockMcpServer.cs
├── Tests/
│   ├── MainWindowTests.cs
│   ├── SettingsTests.cs
│   ├── ChatbotTests.cs
│   ├── TrafficTests.cs
│   ├── ServiceTests.cs
│   └── IntegrationTests.cs
└── SvonyBrowser.Tests.csproj
```

---

## Test Categories

### Category 1: MainWindow GUI Tests (25 tests)

#### 1.1 Panel Mode Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| MW-001 | ShowLeftOnlyMode | Click "◀ Bot Only" button | Left panel visible, right hidden |
| MW-002 | ShowRightOnlyMode | Click "Client ▶" button | Right panel visible, left hidden |
| MW-003 | ShowBothPanelsMode | Click "◀ Both ▶" button | Both panels visible |
| MW-004 | SwapPanels | Click "⇄ Swap" button | Panel positions swapped |
| MW-005 | PanelModeKeyboardCtrl1 | Press Ctrl+1 | Left only mode activated |
| MW-006 | PanelModeKeyboardCtrl2 | Press Ctrl+2 | Both panels mode activated |
| MW-007 | PanelModeKeyboardCtrl3 | Press Ctrl+3 | Right only mode activated |

#### 1.2 Server Selection Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| MW-008 | SelectServer_cc1 | Select cc1.evony.com | Server changed, browsers reload |
| MW-009 | SelectServer_cc2 | Select cc2.evony.com | Server changed, browsers reload |
| MW-010 | SelectServer_cc3 | Select cc3.evony.com | Server changed, browsers reload |
| MW-011 | SelectServer_cc4 | Select cc4.evony.com | Server changed, browsers reload |
| MW-012 | SelectServer_cc5 | Select cc5.evony.com | Server changed, browsers reload |

#### 1.3 Browser Control Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| MW-013 | ReloadLeftBrowser | Click "🔄 Reload Left" | Left browser reloads |
| MW-014 | ReloadRightBrowser | Click "🔄 Reload Right" | Right browser reloads |
| MW-015 | ClearCache | Click "🗑 Clear Cache" | Cache cleared, confirmation shown |
| MW-016 | ReloadKeyboardF5 | Press F5 | Left browser reloads |
| MW-017 | ReloadKeyboardF6 | Press F6 | Right browser reloads |

#### 1.4 Tool Launch Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| MW-018 | OpenSolEditor | Click "📁 SOL Editor" | SOL editor launches or message shown |
| MW-019 | OpenFiddler | Click "📊 Fiddler" | Fiddler launches or message shown |
| MW-020 | OpenSettings | Click "⚙ Settings" | Settings window opens |

#### 1.5 Status Indicator Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| MW-021 | McpIndicatorConnected | MCP servers connected | Green indicator |
| MW-022 | McpIndicatorDisconnected | MCP servers disconnected | Red indicator |
| MW-023 | ProxyIndicatorConnected | Proxy available | Green indicator |
| MW-024 | ProxyIndicatorDisconnected | Proxy unavailable | Red indicator |
| MW-025 | ConnectionStatusText | Check status text | Shows current connection state |

---

### Category 2: Settings Window Tests (15 tests)

#### 2.1 Settings Load/Save Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SW-001 | LoadDefaultServer | Open settings | Default server combo populated |
| SW-002 | LoadProxySettings | Open settings | Proxy host/port populated |
| SW-003 | LoadSwfPaths | Open settings | SWF paths populated |
| SW-004 | SaveSettings | Change and save | Settings persisted to file |
| SW-005 | CancelSettings | Change and cancel | Changes discarded |

#### 2.2 Browse Dialog Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SW-006 | BrowseAutoEvonySwf | Click Browse for AutoEvony | File dialog opens |
| SW-007 | BrowseEvonyClientSwf | Click Browse for EvonyClient | File dialog opens |
| SW-008 | BrowseFlashPlugin | Click Browse for Flash plugin | File dialog opens |

#### 2.3 Settings Validation Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SW-009 | ValidateProxyPort | Enter invalid port | Validation error shown |
| SW-010 | ValidateSwfPath | Enter non-existent path | Warning shown |
| SW-011 | ValidateFlashPlugin | Enter invalid DLL | Warning shown |

#### 2.4 Checkbox Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SW-012 | ToggleAutoStartFiddler | Toggle checkbox | Setting changes |
| SW-013 | ToggleRememberLayout | Toggle checkbox | Setting changes |
| SW-014 | ToggleEnableLogging | Toggle checkbox | Setting changes |
| SW-015 | PathsReadOnly | Try to edit paths | Paths are read-only |

---

### Category 3: Settings Control Center Tests (50 tests)

#### 3.1 Category Navigation Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SCC-001 | NavigateGeneral | Click General | General panel shown |
| SCC-002 | NavigateBrowser | Click Browser | Browser panel shown |
| SCC-003 | NavigateProxy | Click Proxy | Proxy panel shown |
| SCC-004 | NavigateMcp | Click MCP | MCP panel shown |
| SCC-005 | NavigateLlm | Click LLM | LLM panel shown |
| SCC-006 | NavigateFiddler | Click Fiddler | Fiddler panel shown |
| SCC-007 | NavigateAutomation | Click Automation | Automation panel shown |
| SCC-008 | NavigateTraffic | Click Traffic | Traffic panel shown |
| SCC-009 | NavigateChatbot | Click Chatbot | Chatbot panel shown |
| SCC-010 | NavigateStatusBar | Click Status Bar | Status Bar panel shown |
| SCC-011 | NavigateWebhooks | Click Webhooks | Webhooks panel shown |
| SCC-012 | NavigateAdvanced | Click Advanced | Advanced panel shown |

#### 3.2 General Settings Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SCC-013 | ChangeTheme | Select theme | Theme applied |
| SCC-014 | ChangeLanguage | Select language | Language changed |
| SCC-015 | ToggleStartMinimized | Toggle switch | Setting saved |
| SCC-016 | ToggleStartWithWindows | Toggle switch | Setting saved |
| SCC-017 | ToggleCheckUpdates | Toggle switch | Setting saved |
| SCC-018 | ChangeUpdateChannel | Select channel | Setting saved |
| SCC-019 | ChangeLogLevel | Select level | Setting saved |
| SCC-020 | ChangeLogRetention | Move slider | Setting saved |

#### 3.3 Browser Settings Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SCC-021 | ChangeDefaultServer | Select server | Setting saved |
| SCC-022 | ChangeCacheSize | Move slider | Setting saved |
| SCC-023 | ToggleEnableFlash | Toggle switch | Setting saved |
| SCC-024 | ToggleEnableGpu | Toggle switch | Setting saved |
| SCC-025 | ChangeZoom | Move slider | Setting saved |
| SCC-026 | ToggleDevTools | Toggle switch | Setting saved |

#### 3.4 MCP Settings Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SCC-027 | ToggleMcpEnabled | Toggle switch | MCP connection toggled |
| SCC-028 | ToggleMcpAutoStart | Toggle switch | Setting saved |
| SCC-029 | ChangeRagPort | Enter port | Setting saved |
| SCC-030 | ToggleRagEnabled | Toggle switch | RAG toggled |
| SCC-031 | ChangeRtePort | Enter port | Setting saved |
| SCC-032 | ToggleRteEnabled | Toggle switch | RTE toggled |

#### 3.5 LLM Settings Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SCC-033 | ToggleLlmEnabled | Toggle switch | LLM toggled |
| SCC-034 | ChangeLlmBackend | Select backend | Setting saved |
| SCC-035 | ChangeLmStudioUrl | Enter URL | Setting saved |
| SCC-036 | ChangeTemperature | Move slider | Setting saved |
| SCC-037 | ChangeMaxTokens | Move slider | Setting saved |
| SCC-038 | ToggleStreamResponse | Toggle switch | Setting saved |

#### 3.6 Automation Settings Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SCC-039 | ToggleAutoPilot | Toggle switch | AutoPilot toggled |
| SCC-040 | ToggleSafetyLimits | Toggle switch | Setting saved |
| SCC-041 | ChangeMaxActions | Move slider | Setting saved |
| SCC-042 | ChangeActionDelay | Move slider | Setting saved |
| SCC-043 | ToggleRandomizeDelay | Toggle switch | Setting saved |

#### 3.7 Webhook Settings Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SCC-044 | ToggleWebhooksEnabled | Toggle switch | Webhooks toggled |
| SCC-045 | ChangeDiscordWebhook | Enter URL | Setting saved |
| SCC-046 | ChangeTelegramToken | Enter token | Setting saved |
| SCC-047 | ChangeTelegramChatId | Enter chat ID | Setting saved |

#### 3.8 Advanced Settings Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SCC-048 | ToggleDebugMode | Toggle switch | Debug mode toggled |
| SCC-049 | TogglePerformanceMode | Toggle switch | Performance mode toggled |
| SCC-050 | ChangeMaxMemory | Move slider | Setting saved |

---

### Category 4: Chatbot Panel Tests (15 tests)

#### 4.1 Message Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| CB-001 | SendMessage | Type and send message | Message appears, response received |
| CB-002 | SendMessageEnter | Press Enter to send | Message sent |
| CB-003 | WelcomeMessage | Open chatbot | Welcome message displayed |
| CB-004 | ClearHistory | Clear chat | History cleared, welcome shown |
| CB-005 | ExportChat | Export conversation | File saved |

#### 4.2 Quick Action Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| CB-006 | QuickActionProtocol | Click Protocol Lookup | Input populated |
| CB-007 | QuickActionCalculator | Click Calculator | Input populated |
| CB-008 | QuickActionTraffic | Click Traffic | Traffic query sent |
| CB-009 | QuickActionKnowledge | Click Knowledge | Input populated |
| CB-010 | QuickActionBuildOrder | Click Build Order | Build order generated |

#### 4.3 Status Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| CB-011 | StatusReady | Idle state | "Ready" shown |
| CB-012 | StatusThinking | Processing | "Thinking..." shown |
| CB-013 | StatusError | Error occurs | Error message shown |
| CB-014 | LoadingOverlay | Processing | Loading animation visible |
| CB-015 | McpStatusDisplay | Check MCP status | Connection count shown |

---

### Category 5: Traffic Viewer Tests (12 tests)

#### 5.1 Capture Control Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| TV-001 | StartCapture | Click Start | Capture begins, button disabled |
| TV-002 | StopCapture | Click Stop | Capture stops, button disabled |
| TV-003 | ClearTraffic | Click Clear | Traffic list cleared |
| TV-004 | ExportTraffic | Click Export | File saved |

#### 5.2 Filter Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| TV-005 | FilterByText | Enter filter text | Traffic filtered |
| TV-006 | FilterByDirection | Select direction | Traffic filtered |
| TV-007 | ClearFilter | Clear filter | All traffic shown |

#### 5.3 Display Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| TV-008 | SelectEntry | Click entry | Details shown |
| TV-009 | ShowDecodedAmf | Select AMF entry | Decoded content shown |
| TV-010 | ShowRawHex | Select entry | Raw hex shown |
| TV-011 | ShowHeaders | Select entry | Headers shown |
| TV-012 | TrafficCount | Capture traffic | Count updated |

---

### Category 6: Protocol Explorer Tests (8 tests)

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| PE-001 | SearchProtocol | Enter search term | Results filtered |
| PE-002 | SelectAction | Click action | Details shown |
| PE-003 | ShowParameters | Select action | Parameters listed |
| PE-004 | ShowExample | Select action | Example shown |
| PE-005 | CopyToClipboard | Click Copy | Copied to clipboard |
| PE-006 | TestInChat | Click Test | Opens in chatbot |
| PE-007 | CategoryFilter | Select category | Actions filtered |
| PE-008 | ClearSearch | Clear search | All actions shown |

---

### Category 7: Service Tests (50 tests)

#### 7.1 McpConnectionManager Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SVC-001 | ConnectMcpServer | Connect to server | Status: Connected |
| SVC-002 | DisconnectMcpServer | Disconnect | Status: Disconnected |
| SVC-003 | ReconnectOnFailure | Simulate failure | Auto-reconnect |
| SVC-004 | CallTool_amf_decode | Call amf_decode | Decoded result |
| SVC-005 | CallTool_protocol_lookup | Call protocol_lookup | Protocol info |
| SVC-006 | CallTool_evony_search | Call evony_search | Search results |
| SVC-007 | CallTool_calc_training | Call calc_training | Calculation result |
| SVC-008 | ToolCallTimeout | Simulate timeout | Error handled |
| SVC-009 | GetAllStatuses | Get statuses | All server statuses |
| SVC-010 | HealthCheck | Run health check | Health reported |

#### 7.2 ChatbotService Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SVC-011 | SendMessage | Send user message | Response received |
| SVC-012 | HandleProtocolQuery | Query protocol | Protocol info returned |
| SVC-013 | HandleKnowledgeQuery | Query knowledge | RAG response |
| SVC-014 | HandleCalculation | Request calculation | Result returned |
| SVC-015 | HandleAutomation | Request automation | Build order generated |
| SVC-016 | ClearHistory | Clear conversation | History cleared |
| SVC-017 | UpdateContext | Update game context | Context updated |

#### 7.3 SettingsManager Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SVC-018 | LoadSettings | Load from file | Settings loaded |
| SVC-019 | SaveSettings | Save to file | Settings persisted |
| SVC-020 | EncryptSensitive | Encrypt password | Data encrypted |
| SVC-021 | DecryptSensitive | Decrypt password | Data decrypted |
| SVC-022 | ValidateSettings | Validate settings | Validation result |
| SVC-023 | ResetToDefaults | Reset settings | Defaults applied |

#### 7.4 FiddlerBridge Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SVC-024 | ConnectPipe | Connect to Fiddler | Connected |
| SVC-025 | StartCapture | Start capturing | Capture active |
| SVC-026 | StopCapture | Stop capturing | Capture stopped |
| SVC-027 | ProcessSession | Receive session | Session parsed |
| SVC-028 | DecodeAmf | Decode AMF data | Data decoded |

#### 7.5 GameStateEngine Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SVC-029 | UpdateGameState | Receive update | State updated |
| SVC-030 | GetCurrentCity | Query city | City info returned |
| SVC-031 | GetTroopCounts | Query troops | Troop counts |
| SVC-032 | GetMarchStatus | Query marches | March info |

#### 7.6 AutoPilotService Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SVC-033 | StartAutoPilot | Start automation | AutoPilot running |
| SVC-034 | StopAutoPilot | Stop automation | AutoPilot stopped |
| SVC-035 | ExecuteAction | Execute action | Action completed |
| SVC-036 | SafetyLimitCheck | Exceed limit | Action blocked |

#### 7.7 Other Service Tests

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| SVC-037 | ThemeManager_ApplyTheme | Apply theme | Theme changed |
| SVC-038 | ErrorHandler_LogError | Log error | Error logged |
| SVC-039 | MemoryManager_Cleanup | Run cleanup | Memory freed |
| SVC-040 | SessionRecorder_Record | Record session | Session saved |
| SVC-041 | SessionRecorder_Playback | Play session | Session replayed |
| SVC-042 | WebhookHub_SendDiscord | Send Discord | Webhook sent |
| SVC-043 | WebhookHub_SendTelegram | Send Telegram | Message sent |
| SVC-044 | ExportImportManager_Export | Export data | File created |
| SVC-045 | ExportImportManager_Import | Import data | Data imported |
| SVC-046 | CombatSimulator_Simulate | Run simulation | Results returned |
| SVC-047 | StrategicAdvisor_Advise | Get advice | Advice returned |
| SVC-048 | MapScanner_Scan | Scan map | Scan results |
| SVC-049 | ProtocolFuzzer_Fuzz | Fuzz protocol | Fuzz results |
| SVC-050 | LlmIntegration_Query | Query LLM | Response received |

---

### Category 8: Integration Tests (15 tests)

| Test ID | Test Name | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| INT-001 | AppStartup | Launch application | App ready, no errors |
| INT-002 | LoadSwfFiles | Load SWF in browsers | SWF displayed |
| INT-003 | FullTrafficCapture | Capture and decode | Traffic decoded |
| INT-004 | ChatWithRag | Chat using RAG | Knowledge response |
| INT-005 | SettingsRoundTrip | Save and reload | Settings preserved |
| INT-006 | McpFullWorkflow | Connect, call, disconnect | All steps pass |
| INT-007 | AutomationWorkflow | Configure and run | Automation executes |
| INT-008 | ExportImportCycle | Export then import | Data preserved |
| INT-009 | ThemeSwitch | Switch themes | UI updates |
| INT-010 | MultiPanelWorkflow | Use all panels | All panels work |
| INT-011 | KeyboardNavigation | Navigate with keyboard | All shortcuts work |
| INT-012 | ErrorRecovery | Simulate errors | App recovers |
| INT-013 | MemoryStability | Long running test | No memory leaks |
| INT-014 | ConcurrentOperations | Multiple operations | No race conditions |
| INT-015 | GracefulShutdown | Close application | Clean shutdown |

---

## Test Execution

### Running All Tests

```powershell
dotnet test SvonyBrowser.Tests --logger "console;verbosity=detailed"
```

### Running Specific Category

```powershell
dotnet test SvonyBrowser.Tests --filter "Category=MainWindow"
dotnet test SvonyBrowser.Tests --filter "Category=Settings"
dotnet test SvonyBrowser.Tests --filter "Category=Services"
```

### Generating Coverage Report

```powershell
dotnet test SvonyBrowser.Tests --collect:"XPlat Code Coverage"
reportgenerator -reports:coverage.cobertura.xml -targetdir:coveragereport
```

---

## Test Summary

| Category | Test Count | Priority |
|----------|------------|----------|
| MainWindow GUI | 25 | HIGH |
| Settings Window | 15 | HIGH |
| Settings Control Center | 50 | MEDIUM |
| Chatbot Panel | 15 | HIGH |
| Traffic Viewer | 12 | HIGH |
| Protocol Explorer | 8 | MEDIUM |
| Services | 50 | HIGH |
| Integration | 15 | CRITICAL |
| **TOTAL** | **190** | - |

### Coverage Targets

- GUI Elements: 100%
- Event Handlers: 100%
- Service Methods: 80%+
- Settings: 100%
- Integration Flows: 100%

---

## Notes

1. Tests assume MCP servers are running or mocked
2. Fiddler tests may require Fiddler installation
3. Some tests require actual SWF files
4. Integration tests may take longer to execute
5. Consider parallel test execution for faster results
