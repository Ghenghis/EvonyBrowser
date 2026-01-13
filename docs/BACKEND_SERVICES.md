# Backend Services Implementation Guide

**Current Compliance: 79%**  
**Target: 100%**  
**Gap: 21% (17 tasks)**

---

## Overview

The Backend Services layer contains 34 singleton services that handle business logic, data access, and external integrations. The current implementation is 79% complete with gaps in interfaces, disposal, and initialization.

## Gap Analysis

| Component | Current | Target | Gap |
|-----------|---------|--------|-----|
| Services with Interfaces | 25/34 | 34/34 | 9 |
| Services with IDisposable | 27/34 | 34/34 | 7 |
| Services Initialized | 0/34 | 34/34 | 34 |

---

## Task 1: Add Missing Interfaces

Nine services are missing interface definitions. Interfaces enable dependency injection, testing, and loose coupling.

### 1.1 Interface Template

```csharp
namespace SvonyBrowser.Services.Interfaces
{
    public interface IServiceName
    {
        // Core methods
        Task InitializeAsync();
        
        // Service-specific methods
        // ...
    }
}
```

### 1.2 Services Requiring Interfaces

| Service | Interface | Key Methods |
|---------|-----------|-------------|
| ProxyMonitor | IProxyMonitor | StartMonitoring, StopMonitoring, GetStatus |
| AnalyticsDashboard | IAnalyticsDashboard | GetMetrics, RefreshData, ExportReport |
| MultiAccountOrchestrator | IMultiAccountOrchestrator | AddAccount, RemoveAccount, SwitchAccount |
| PromptTemplateEngine | IPromptTemplateEngine | LoadTemplate, RenderPrompt, SaveTemplate |
| MapScanner | IMapScanner | ScanMap, GetTileInfo, ExportMapData |
| MemoryGuard | IMemoryGuard | CheckMemory, ForceGC, GetMemoryStats |
| DebugService | IDebugService | Log, GetLogs, ClearLogs, SetLogLevel |
| FailsafeManager | IFailsafeManager | Enable, Disable, CheckSafety, EmergencyStop |
| KeyboardShortcutManager | IKeyboardShortcutManager | RegisterShortcut, UnregisterShortcut, HandleKey |

---

## Task 2: Implement IDisposable

Seven services manage resources that need proper cleanup.

### 2.1 Disposal Pattern

```csharp
public class ServiceName : IServiceName, IDisposable
{
    private bool _disposed;
    private readonly HttpClient _httpClient;
    private readonly CancellationTokenSource _cts;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            _httpClient?.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
        }
        
        _disposed = true;
    }
}
```

### 2.2 Services Requiring IDisposable

| Service | Resources to Dispose |
|---------|---------------------|
| ProxyMonitor | HttpClient, Timer |
| AnalyticsDashboard | Timer, DataConnections |
| MultiAccountOrchestrator | SessionHandles |
| PromptTemplateEngine | FileStreams |
| MapScanner | ImageResources |
| MemoryGuard | Timer |
| DebugService | FileStreams, LogWriters |

---

## Task 3: Initialize Services in App.xaml.cs

**CRITICAL**: All 34 services must be initialized in the correct order during application startup.

### 3.1 Initialization Order

Services must be initialized in dependency order:

**Layer 1: Core Infrastructure (No Dependencies)**
```csharp
_ = ErrorHandler.Instance;
_ = SettingsManager.Instance;
_ = ThemeManager.Instance;
```

**Layer 2: Memory & Logging**
```csharp
_ = MemoryManager.Instance;
_ = MemoryGuard.Instance;
_ = DebugService.Instance;
```

**Layer 3: Network & Connections**
```csharp
_ = ConnectionPool.Instance;
await McpConnectionManager.Instance.InitializeAsync();
_ = FiddlerBridge.Instance;
_ = TrafficPipeClient.Instance;
```

**Layer 4: Data Providers**
```csharp
_ = RealDataProvider.Instance;
_ = LlmIntegrationService.Instance;
```

**Layer 5: Game Services**
```csharp
_ = SessionManager.Instance;
_ = GameStateEngine.Instance;
_ = ProtocolHandler.Instance;
_ = PacketAnalysisEngine.Instance;
```

**Layer 6: Automation**
```csharp
_ = AutoPilotService.Instance;
_ = VisualAutomationService.Instance;
_ = CombatSimulator.Instance;
_ = StrategicAdvisor.Instance;
```

**Layer 7: UI Services**
```csharp
_ = ChatbotService.Instance;
_ = StatusBarManager.Instance;
_ = KeyboardShortcutManager.Instance;
```

**Layer 8: Utilities**
```csharp
_ = SessionRecorder.Instance;
_ = WebhookHub.Instance;
_ = ExportImportManager.Instance;
_ = ProxyMonitor.Instance;
_ = AnalyticsDashboard.Instance;
_ = MultiAccountOrchestrator.Instance;
_ = PromptTemplateEngine.Instance;
_ = MapScanner.Instance;
_ = ProtocolFuzzer.Instance;
_ = CdpConnectionService.Instance;
_ = FailsafeManager.Instance;
```

### 3.2 App.xaml.cs Implementation

```csharp
protected override async void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    
    try
    {
        // Layer 1: Core Infrastructure
        Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("Logs/svony-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        
        _ = ErrorHandler.Instance;
        await SettingsManager.Instance.LoadAsync();
        _ = ThemeManager.Instance;
        
        // Layer 2: Memory & Logging
        _ = MemoryManager.Instance;
        _ = MemoryGuard.Instance;
        _ = DebugService.Instance;
        
        // Layer 3: Network & Connections
        _ = ConnectionPool.Instance;
        await McpConnectionManager.Instance.InitializeAsync();
        _ = FiddlerBridge.Instance;
        _ = TrafficPipeClient.Instance;
        
        // Continue with remaining layers...
        
        Logger.Information("All services initialized successfully");
    }
    catch (Exception ex)
    {
        Logger?.Error(ex, "Failed to initialize services");
        MessageBox.Show($"Startup failed: {ex.Message}", "Error", 
            MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown(1);
    }
}

protected override void OnExit(ExitEventArgs e)
{
    // Dispose services in reverse order
    try
    {
        DebugService.Instance?.Dispose();
        MemoryGuard.Instance?.Dispose();
        // ... dispose all disposable services
    }
    catch (Exception ex)
    {
        Logger?.Error(ex, "Error during shutdown");
    }
    
    base.OnExit(e);
}
```

---

## Implementation Checklist

### Interfaces
- [ ] SVC-001: Create IProxyMonitor
- [ ] SVC-002: Create IAnalyticsDashboard
- [ ] SVC-003: Create IMultiAccountOrchestrator
- [ ] SVC-004: Create IPromptTemplateEngine
- [ ] SVC-005: Create IMapScanner
- [ ] SVC-006: Create IMemoryGuard
- [ ] SVC-007: Create IDebugService
- [ ] SVC-008: Create IFailsafeManager
- [ ] SVC-009: Create IKeyboardShortcutManager

### IDisposable
- [ ] SVC-010: Add IDisposable to ProxyMonitor
- [ ] SVC-011: Add IDisposable to AnalyticsDashboard
- [ ] SVC-012: Add IDisposable to MultiAccountOrchestrator
- [ ] SVC-013: Add IDisposable to PromptTemplateEngine
- [ ] SVC-014: Add IDisposable to MapScanner
- [ ] SVC-015: Add IDisposable to MemoryGuard
- [ ] SVC-016: Add IDisposable to DebugService

### Initialization
- [ ] SVC-017: Implement App.OnStartup with all 34 services

---

## Verification

After implementation:

1. All services initialize without errors
2. Application starts within 5 seconds
3. Services dispose cleanly on exit
4. No memory leaks after extended use
5. Services can be mocked for testing
