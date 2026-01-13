# Svony Browser V7.0 - Best Practices Gap Analysis Report

**Author:** Manus AI  
**Date:** January 12, 2026  
**Version:** 1.0

---

## Executive Summary

This report provides a comprehensive 1:1 gap analysis of the Svony Browser codebase against software engineering best practices. The audit reveals significant gaps in UI component wiring, event handler implementation, and validation coverage that must be addressed before the application can be considered production-ready.

| Category | Compliance | Status |
|----------|------------|--------|
| UI Components | 7% | 🔴 CRITICAL |
| State Management | 85% | 🟡 NEEDS WORK |
| Event Handlers | 59% | 🟡 NEEDS WORK |
| Validation | 26% | 🔴 CRITICAL |
| Error Handling | 51% | 🟡 NEEDS WORK |
| Backend Services | 79% | 🟢 GOOD |
| Infrastructure | 75% | 🟢 GOOD |
| Quality Assurance | 40% | 🟡 NEEDS WORK |

**Overall Compliance: 53%**

---

## 1. UI Components Analysis

### 1.1 Buttons (0/46 wired = 0%)

The audit identified **46 buttons** across all XAML files. Currently, **0 buttons** have properly wired Click handlers that match the code-behind implementation.

| File | Buttons | Wired | Missing |
|------|---------|-------|---------|
| MainWindow.xaml | 12 | 0 | 12 |
| SettingsWindow.xaml | 4 | 0 | 4 |
| SettingsControlCenter.xaml | 7 | 0 | 7 |
| ChatbotPanel.xaml | 7 | 0 | 7 |
| TrafficViewer.xaml | 4 | 0 | 4 |
| ProtocolExplorer.xaml | 2 | 0 | 2 |
| PacketBuilder.xaml | 5 | 0 | 5 |
| StatusBarV4.xaml | 2 | 0 | 2 |
| StatusBar.xaml | 3 | 0 | 3 |

**Required Actions:**

```
[ ] Task UI-001: Wire all MainWindow buttons to handlers
    - BotOnlyButton → ShowLeftOnly_Click
    - ClientOnlyButton → ShowRightOnly_Click
    - BothPanelsButton → ShowBothPanels_Click
    - SwapPanelsButton → SwapPanels_Click
    - ReloadLeftButton → ReloadLeft_Click
    - ReloadRightButton → ReloadRight_Click
    - ClearCacheButton → ClearCache_Click
    - SolEditorButton → OpenSolEditor_Click
    - FiddlerButton → OpenFiddler_Click
    - SettingsButton → OpenSettings_Click
    - DecodeAmfButton → DecodeAmf_Click
    - CalculateTroopsButton → CalculateTroops_Click

[ ] Task UI-002: Wire all SettingsWindow buttons
    - SaveButton → Save_Click
    - CancelButton → Cancel_Click
    - BrowseAutoEvonyButton → BrowseAutoEvony_Click
    - BrowseEvonyClientButton → BrowseEvonyClient_Click

[ ] Task UI-003: Wire all SettingsControlCenter buttons
    - ResetDefaultsButton → ResetDefaults_Click
    - ExportSettingsButton → ExportSettings_Click
    - ImportSettingsButton → ImportSettings_Click
    - TestProxyButton → TestProxy_Click
    - TestMcpButton → TestMcp_Click
    - TestLlmButton → TestLlm_Click
    - TestWebhookButton → TestWebhook_Click

[ ] Task UI-004: Wire all ChatbotPanel buttons
    - SendButton → Send_Click
    - ClearHistoryButton → ClearHistory_Click
    - ExportChatButton → ExportChat_Click
    - ProtocolLookupButton → ProtocolLookup_Click
    - CalculatorButton → Calculator_Click
    - TrafficButton → Traffic_Click
    - KnowledgeButton → Knowledge_Click

[ ] Task UI-005: Wire all TrafficViewer buttons
    - StartCaptureButton → StartCapture_Click
    - StopCaptureButton → StopCapture_Click
    - ClearTrafficButton → ClearTraffic_Click
    - ExportTrafficButton → ExportTraffic_Click

[ ] Task UI-006: Wire all ProtocolExplorer buttons
    - CopyToClipboardButton → CopyToClipboard_Click
    - TestInChatButton → TestInChat_Click

[ ] Task UI-007: Wire all PacketBuilder buttons
    - BuildPacketButton → BuildPacket_Click
    - SendPacketButton → SendPacket_Click
    - ClearPacketButton → ClearPacket_Click
    - LoadTemplateButton → LoadTemplate_Click
    - SaveTemplateButton → SaveTemplate_Click
```

### 1.2 TextBoxes (3/26 wired = 12%)

| File | TextBoxes | With Handlers | Missing |
|------|-----------|---------------|---------|
| MainWindow.xaml | 3 | 0 | AmfInputTextBox, AmfOutputTextBox, TroopAmountTextBox |
| SettingsWindow.xaml | 7 | 0 | ProxyHostText, ProxyPortText, AutoEvonySwfPathText, EvonyClientSwfPathText, FlashPluginPathText, CachePathText, LogPathText |
| SettingsControlCenter.xaml | 10 | 3 | ProxyHostText, ProxyPortText, RagPortText, RtePortText, LmStudioUrlText, DiscordWebhookText, TelegramTokenText |
| ChatbotPanel.xaml | 2 | 0 | MessageInputTextBox, SearchTextBox |
| TrafficViewer.xaml | 4 | 0 | FilterTextBox, DecodedTextBox, RawHexTextBox, HeadersTextBox |

**Required Actions:**

```
[ ] Task UI-008: Add TextChanged handlers for validation
    - ProxyPortText → ValidatePort_TextChanged
    - RagPortText → ValidatePort_TextChanged
    - RtePortText → ValidatePort_TextChanged

[ ] Task UI-009: Add PropertyChanged bindings for settings TextBoxes
    - Bind all settings TextBoxes to AppSettings model
    - Implement two-way binding with UpdateSourceTrigger=PropertyChanged
```

### 1.3 ComboBoxes (4/13 wired = 31%)

| File | ComboBoxes | With Handlers | Missing |
|------|------------|---------------|---------|
| MainWindow.xaml | 2 | 1 | TroopTypeComboBox |
| SettingsWindow.xaml | 1 | 1 | DefaultServerCombo |
| SettingsControlCenter.xaml | 6 | 2 | ThemeCombo, LanguageCombo, UpdateChannelCombo, LogLevelCombo, ProxyTypeCombo, LlmBackendCombo |

**Required Actions:**

```
[ ] Task UI-010: Wire ComboBox SelectionChanged handlers
    - ThemeCombo → Theme_SelectionChanged → Apply theme immediately
    - LanguageCombo → Language_SelectionChanged → Apply language
    - LlmBackendCombo → LlmBackend_SelectionChanged → Update LLM settings
```

### 1.4 Toggle Switches (0/42 wired = 0%)

This is a **critical gap**. All 42 toggle switches in SettingsControlCenter.xaml have no event handlers.

| Category | Toggles | Status |
|----------|---------|--------|
| General | StartMinimizedToggle, StartWithWindowsToggle, CheckUpdatesToggle, ConfirmExitToggle, RememberPositionToggle | ❌ |
| Browser | EnableFlashToggle, EnableGpuToggle, EnableDevToolsToggle | ❌ |
| Proxy | ProxyEnabledToggle, TestProxyToggle | ❌ |
| MCP | McpEnabledToggle, McpAutoStartToggle, RagEnabledToggle, RteEnabledToggle | ❌ |
| LLM | LlmEnabledToggle, StreamResponseToggle | ❌ |
| Fiddler | FiddlerEnabledToggle, AutoStartFiddlerToggle, DecodeAmfToggle | ❌ |
| Automation | AutoPilotToggle, SafetyLimitsToggle, RandomizeDelayToggle | ❌ |
| Traffic | TrafficAnalysisToggle, PatternDetectionToggle, AutoScrollToggle | ❌ |
| Chatbot | ChatbotEnabledToggle, ShowTimestampsToggle, MarkdownToggle | ❌ |
| StatusBar | StatusBarEnabledToggle, ShowRagProgressToggle, ShowRteProgressToggle, ShowLlmStatsToggle, ShowGpuTempToggle, ShowPacketsToggle | ❌ |
| Webhooks | WebhooksEnabledToggle | ❌ |
| Advanced | DebugModeToggle, PerformanceModeToggle | ❌ |

**Required Actions:**

```
[ ] Task UI-011: Create Toggle_Checked/Unchecked handlers for all toggles
    - Pattern: {ToggleName}_Checked → Update setting → Save → Apply

[ ] Task UI-012: Wire toggles to SettingsManager
    - Each toggle change should:
      1. Update AppSettings model
      2. Call SettingsManager.SaveAsync()
      3. Apply change to relevant service

[ ] Task UI-013: Add toggle state persistence
    - Load toggle states from settings on window load
    - Save toggle states on change
```

### 1.5 Sliders (0/10 wired = 0%)

| Slider | Purpose | Status |
|--------|---------|--------|
| LogRetentionSlider | Days to keep logs | ❌ |
| CacheSizeSlider | Browser cache size MB | ❌ |
| ZoomSlider | Browser zoom level | ❌ |
| HealthCheckSlider | MCP health check interval | ❌ |
| TemperatureSlider | LLM temperature | ❌ |
| MaxTokensSlider | LLM max tokens | ❌ |
| MaxTrafficSlider | Max traffic entries | ❌ |
| MaxActionsSlider | AutoPilot max actions | ❌ |
| ActionDelaySlider | AutoPilot action delay | ❌ |
| MaxMemorySlider | Max memory usage | ❌ |

**Required Actions:**

```
[ ] Task UI-014: Wire Slider ValueChanged handlers
    - Pattern: {SliderName}_ValueChanged → Update setting → Save

[ ] Task UI-015: Add slider value display
    - Show current value next to each slider
    - Update display on value change
```

---

## 2. State Management Analysis

### 2.1 Singleton Services (29/34 thread-safe = 85%)

Five services are not using thread-safe lazy initialization:

| Service | Issue | Fix Required |
|---------|-------|--------------|
| ProxyMonitor | Not using Lazy<T> | Add LazyThreadSafetyMode.ExecutionAndPublication |
| AnalyticsDashboard | Not using Lazy<T> | Add LazyThreadSafetyMode.ExecutionAndPublication |
| MultiAccountOrchestrator | Not using Lazy<T> | Add LazyThreadSafetyMode.ExecutionAndPublication |
| PromptTemplateEngine | Not using Lazy<T> | Add LazyThreadSafetyMode.ExecutionAndPublication |
| MapScanner | Not using Lazy<T> | Add LazyThreadSafetyMode.ExecutionAndPublication |

**Required Actions:**

```
[ ] Task STATE-001: Fix singleton initialization pattern
    private static readonly Lazy<ServiceName> _lazyInstance =
        new Lazy<ServiceName>(() => new ServiceName(), 
            LazyThreadSafetyMode.ExecutionAndPublication);
    public static ServiceName Instance => _lazyInstance.Value;
```

### 2.2 Observable Properties

The audit found properties that should notify UI of changes but don't implement INotifyPropertyChanged:

```
[ ] Task STATE-002: Implement INotifyPropertyChanged in ViewModels
    - MainWindowViewModel
    - SettingsViewModel
    - ChatbotViewModel
    - TrafficViewModel

[ ] Task STATE-003: Add PropertyChanged notifications to all bindable properties
```

### 2.3 Caching ✅

Caching is implemented in:
- ConnectionPool.cs
- RealDataProvider.cs
- SettingsManager.cs

### 2.4 Settings Persistence ✅

SettingsManager has SaveAsync and LoadAsync methods.

---

## 3. Event Handlers Analysis

### 3.1 Click Handlers (52/52 implemented = 100%) ✅

All declared Click handlers have implementations in code-behind.

### 3.2 Change Handlers (10/43 implemented = 23%) 🔴

**33 change handlers are missing:**

| Handler Type | Declared | Implemented | Gap |
|--------------|----------|-------------|-----|
| SelectionChanged | 15 | 4 | 11 |
| TextChanged | 12 | 3 | 9 |
| ValueChanged | 10 | 0 | 10 |
| Checked/Unchecked | 6 | 3 | 3 |

**Required Actions:**

```
[ ] Task EVENT-001: Implement all SelectionChanged handlers
[ ] Task EVENT-002: Implement all TextChanged handlers  
[ ] Task EVENT-003: Implement all ValueChanged handlers
[ ] Task EVENT-004: Implement all Checked/Unchecked handlers
```

### 3.3 Keyboard Handlers (2/2 implemented = 100%) ✅

---

## 4. Validation Analysis

### 4.1 Client-Side Validation (26% coverage) 🔴

| Validation Type | Count | Status |
|-----------------|-------|--------|
| Form validation | 6 | ⚠️ Partial |
| Input sanitization | 0 | ❌ Missing |
| Required field checks | 8 | ⚠️ Partial |
| Format validation | 2 | ❌ Minimal |

**Required Actions:**

```
[ ] Task VAL-001: Add input validation for all TextBoxes
    - ProxyPort: 1-65535 range
    - URLs: Valid URL format
    - Paths: File/directory exists
    - Ports: Numeric only

[ ] Task VAL-002: Add form-level validation
    - Validate all required fields before save
    - Show validation summary
    - Highlight invalid fields

[ ] Task VAL-003: Add real-time validation feedback
    - Red border for invalid input
    - Tooltip with error message
    - Disable save until valid
```

### 4.2 Server-Side Validation (8 type checks)

```
[ ] Task VAL-004: Add comprehensive input validation in services
    - Validate all method parameters
    - Check for null/empty strings
    - Validate numeric ranges
    - Sanitize file paths
```

### 4.3 User-Friendly Messages (2/23 = 9%) 🔴

Most error messages are technical. Need to improve:

```
[ ] Task VAL-005: Rewrite error messages to be user-friendly
    - Before: "ArgumentNullException: value cannot be null"
    - After: "Please enter a value for this field"
```

---

## 5. Error Handling Analysis

### 5.1 Try-Catch Blocks (49 total)

| Metric | Count | Status |
|--------|-------|--------|
| Total try-catch | 49 | ✅ |
| With logging | 25 | ⚠️ 51% |
| Silent catches | 1 | ⚠️ |
| Specific exceptions | 7 | 🔴 14% |

**Required Actions:**

```
[ ] Task ERR-001: Add logging to all catch blocks
    catch (Exception ex)
    {
        App.Logger.Error(ex, "Operation failed: {Message}", ex.Message);
        // Handle error
    }

[ ] Task ERR-002: Use specific exception types
    - FileNotFoundException for file operations
    - HttpRequestException for network operations
    - JsonException for parsing
    - OperationCanceledException for cancellation

[ ] Task ERR-003: Remove silent catch blocks
    - Log all exceptions
    - Show user-friendly message when appropriate

[ ] Task ERR-004: Add global exception handler
    AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    Application.Current.DispatcherUnhandledException += OnDispatcherException;
```

### 5.2 Error Recovery

```
[ ] Task ERR-005: Implement retry logic for network operations
    - MCP connections: 3 retries with exponential backoff
    - HTTP requests: 2 retries
    - File operations: 1 retry

[ ] Task ERR-006: Implement fallback handlers
    - MCP offline → Use cached data
    - LLM offline → Show offline message
    - Settings corrupt → Reset to defaults
```

---

## 6. Backend Services Analysis

### 6.1 Services Overview (34 total)

| Metric | Count | Percentage |
|--------|-------|------------|
| With interfaces | 25 | 74% |
| With disposal | 27 | 79% |
| Async methods | 152 | - |
| Documented methods | 514 | - |

### 6.2 Missing Interfaces (9 services)

```
[ ] Task SVC-001: Add interfaces for services
    - IProxyMonitor
    - IAnalyticsDashboard
    - IMultiAccountOrchestrator
    - IPromptTemplateEngine
    - IMapScanner
    - IMemoryGuard
    - IDebugService
    - IFailsafeManager
    - IKeyboardShortcutManager
```

### 6.3 Missing Disposal (7 services)

```
[ ] Task SVC-002: Implement IDisposable
    - ProxyMonitor
    - AnalyticsDashboard
    - MultiAccountOrchestrator
    - PromptTemplateEngine
    - MapScanner
    - MemoryGuard
    - DebugService
```

### 6.4 Service Initialization in App.xaml.cs

**CRITICAL: 34 services are not initialized in App.xaml.cs**

```
[ ] Task SVC-003: Add service initialization in App.OnStartup
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Initialize core services
        _ = SettingsManager.Instance;
        _ = ErrorHandler.Instance;
        _ = ThemeManager.Instance;
        
        // Initialize infrastructure
        _ = ConnectionPool.Instance;
        _ = MemoryManager.Instance;
        _ = MemoryGuard.Instance;
        
        // Initialize MCP services
        await McpConnectionManager.Instance.InitializeAsync();
        _ = RealDataProvider.Instance;
        _ = LlmIntegrationService.Instance;
        
        // Initialize traffic services
        _ = ProxyMonitor.Instance;
        _ = FiddlerBridge.Instance;
        _ = TrafficPipeClient.Instance;
        
        // Initialize game services
        _ = SessionManager.Instance;
        _ = GameStateEngine.Instance;
        _ = ProtocolHandler.Instance;
        _ = PacketAnalysisEngine.Instance;
        
        // Initialize automation services
        _ = AutoPilotService.Instance;
        _ = VisualAutomationService.Instance;
        _ = CombatSimulator.Instance;
        _ = StrategicAdvisor.Instance;
        
        // Initialize utility services
        _ = ChatbotService.Instance;
        _ = SessionRecorder.Instance;
        _ = WebhookHub.Instance;
        _ = ExportImportManager.Instance;
        _ = StatusBarManager.Instance;
        _ = KeyboardShortcutManager.Instance;
        _ = FailsafeManager.Instance;
        _ = DebugService.Instance;
    }
```

---

## 7. Infrastructure Analysis

### 7.1 Configuration ✅

- Settings file: AppSettings.cs ✅
- Environment variables: Not implemented ❌
- Config validation: Partial ⚠️

```
[ ] Task INFRA-001: Add environment variable support
    - SVONY_LOG_LEVEL
    - SVONY_MCP_HOST
    - SVONY_DEBUG_MODE

[ ] Task INFRA-002: Add configuration validation on load
```

### 7.2 Logging ✅

- Serilog implemented ✅
- Log levels: Information, Warning, Error, Debug ✅
- Log rotation: Not verified ⚠️

```
[ ] Task INFRA-003: Verify log rotation configuration
[ ] Task INFRA-004: Add structured logging for all operations
```

### 7.3 Session Management ✅

SessionManager.cs handles session state.

### 7.4 Caching ✅

ConnectionPool and RealDataProvider implement caching.

---

## 8. Quality Assurance Analysis

### 8.1 Unit Tests (0 test files in project)

**CRITICAL: No test project exists**

```
[ ] Task QA-001: Create SvonyBrowser.Tests project
[ ] Task QA-002: Add unit tests for all services
[ ] Task QA-003: Add unit tests for all ViewModels
[ ] Task QA-004: Target 80% code coverage
```

### 8.2 Integration Tests

```
[ ] Task QA-005: Create integration test suite
[ ] Task QA-006: Test MCP connection workflow
[ ] Task QA-007: Test settings persistence
[ ] Task QA-008: Test error recovery scenarios
```

### 8.3 Documentation

| Type | Count | Status |
|------|-------|--------|
| XML comments | 737 | ✅ Good |
| README files | 54 | ✅ Good |
| API docs | 0 | ❌ Missing |

```
[ ] Task QA-009: Generate API documentation from XML comments
[ ] Task QA-010: Create developer guide
```

### 8.4 Error Recovery (3 retry implementations)

```
[ ] Task QA-011: Add retry logic to all network operations
[ ] Task QA-012: Add fallback handlers for offline scenarios
[ ] Task QA-013: Add graceful degradation for missing services
```

---

## 9. Complete Wiring Checklist

### 9.1 UI Wiring Tasks (Priority: CRITICAL)

| Task ID | Description | Status |
|---------|-------------|--------|
| UI-001 | Wire MainWindow buttons | ❌ |
| UI-002 | Wire SettingsWindow buttons | ❌ |
| UI-003 | Wire SettingsControlCenter buttons | ❌ |
| UI-004 | Wire ChatbotPanel buttons | ❌ |
| UI-005 | Wire TrafficViewer buttons | ❌ |
| UI-006 | Wire ProtocolExplorer buttons | ❌ |
| UI-007 | Wire PacketBuilder buttons | ❌ |
| UI-008 | Add TextChanged validation handlers | ❌ |
| UI-009 | Add PropertyChanged bindings | ❌ |
| UI-010 | Wire ComboBox handlers | ❌ |
| UI-011 | Create Toggle handlers | ❌ |
| UI-012 | Wire toggles to SettingsManager | ❌ |
| UI-013 | Add toggle persistence | ❌ |
| UI-014 | Wire Slider handlers | ❌ |
| UI-015 | Add slider value display | ❌ |

### 9.2 State Management Tasks (Priority: HIGH)

| Task ID | Description | Status |
|---------|-------------|--------|
| STATE-001 | Fix singleton initialization | ❌ |
| STATE-002 | Implement INotifyPropertyChanged | ❌ |
| STATE-003 | Add PropertyChanged notifications | ❌ |

### 9.3 Event Handler Tasks (Priority: HIGH)

| Task ID | Description | Status |
|---------|-------------|--------|
| EVENT-001 | Implement SelectionChanged handlers | ❌ |
| EVENT-002 | Implement TextChanged handlers | ❌ |
| EVENT-003 | Implement ValueChanged handlers | ❌ |
| EVENT-004 | Implement Checked/Unchecked handlers | ❌ |

### 9.4 Validation Tasks (Priority: HIGH)

| Task ID | Description | Status |
|---------|-------------|--------|
| VAL-001 | Add TextBox validation | ❌ |
| VAL-002 | Add form-level validation | ❌ |
| VAL-003 | Add real-time validation feedback | ❌ |
| VAL-004 | Add service input validation | ❌ |
| VAL-005 | Rewrite error messages | ❌ |

### 9.5 Error Handling Tasks (Priority: MEDIUM)

| Task ID | Description | Status |
|---------|-------------|--------|
| ERR-001 | Add logging to catch blocks | ❌ |
| ERR-002 | Use specific exception types | ❌ |
| ERR-003 | Remove silent catches | ❌ |
| ERR-004 | Add global exception handler | ❌ |
| ERR-005 | Implement retry logic | ❌ |
| ERR-006 | Implement fallback handlers | ❌ |

### 9.6 Service Tasks (Priority: HIGH)

| Task ID | Description | Status |
|---------|-------------|--------|
| SVC-001 | Add service interfaces | ❌ |
| SVC-002 | Implement IDisposable | ❌ |
| SVC-003 | Initialize services in App.xaml.cs | ❌ |

### 9.7 Infrastructure Tasks (Priority: MEDIUM)

| Task ID | Description | Status |
|---------|-------------|--------|
| INFRA-001 | Add environment variable support | ❌ |
| INFRA-002 | Add configuration validation | ❌ |
| INFRA-003 | Verify log rotation | ❌ |
| INFRA-004 | Add structured logging | ❌ |

### 9.8 Quality Assurance Tasks (Priority: HIGH)

| Task ID | Description | Status |
|---------|-------------|--------|
| QA-001 | Create test project | ❌ |
| QA-002 | Add service unit tests | ❌ |
| QA-003 | Add ViewModel unit tests | ❌ |
| QA-004 | Target 80% coverage | ❌ |
| QA-005 | Create integration tests | ❌ |
| QA-006 | Test MCP workflow | ❌ |
| QA-007 | Test settings persistence | ❌ |
| QA-008 | Test error recovery | ❌ |
| QA-009 | Generate API docs | ❌ |
| QA-010 | Create developer guide | ❌ |
| QA-011 | Add retry logic | ❌ |
| QA-012 | Add fallback handlers | ❌ |
| QA-013 | Add graceful degradation | ❌ |

---

## 10. Summary

### Total Tasks Required: 56

| Category | Tasks | Priority |
|----------|-------|----------|
| UI Wiring | 15 | CRITICAL |
| State Management | 3 | HIGH |
| Event Handlers | 4 | HIGH |
| Validation | 5 | HIGH |
| Error Handling | 6 | MEDIUM |
| Services | 3 | HIGH |
| Infrastructure | 4 | MEDIUM |
| Quality Assurance | 13 | HIGH |

### Estimated Effort

| Phase | Tasks | Estimated Time |
|-------|-------|----------------|
| Phase 1: Critical UI Wiring | 15 | 3-4 days |
| Phase 2: Event Handlers | 7 | 2 days |
| Phase 3: Validation | 5 | 1-2 days |
| Phase 4: Error Handling | 6 | 1 day |
| Phase 5: Services | 3 | 1 day |
| Phase 6: Infrastructure | 4 | 1 day |
| Phase 7: QA | 13 | 3-4 days |
| **Total** | **56** | **12-15 days** |

---

## Appendix A: Files Requiring Modification

| File | Changes Required |
|------|------------------|
| App.xaml.cs | Service initialization |
| MainWindow.xaml | Button Click attributes |
| MainWindow.xaml.cs | Handler implementations |
| SettingsWindow.xaml | Button Click attributes |
| SettingsWindow.xaml.cs | Handler implementations, SettingsManager integration |
| SettingsControlCenter.xaml | Toggle/Slider event attributes |
| SettingsControlCenter.xaml.cs | All toggle/slider handlers |
| ChatbotPanel.xaml | Button Click attributes |
| ChatbotPanel.xaml.cs | Handler implementations |
| TrafficViewer.xaml | Button Click attributes |
| TrafficViewer.xaml.cs | Handler implementations |
| ProtocolExplorer.xaml.cs | Handler implementations |
| PacketBuilder.xaml.cs | Handler implementations |
| StatusBar.xaml.cs | Service subscriptions |
| StatusBarV4.xaml.cs | Service subscriptions |
| All Services/*.cs | Interface implementation, disposal |

---

## Appendix B: Playwright Test Coverage Required

Based on this gap analysis, the Playwright test suite must cover:

| Component | Tests Required |
|-----------|----------------|
| 46 Buttons | 46 click tests |
| 26 TextBoxes | 26 input tests |
| 13 ComboBoxes | 13 selection tests |
| 42 Toggles | 84 tests (on/off each) |
| 10 Sliders | 30 tests (min/mid/max each) |
| 34 Services | 100+ method tests |
| Integration | 15 workflow tests |
| **Total** | **~315 tests** |
