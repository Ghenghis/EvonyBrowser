# State Management Implementation Guide

**Current Compliance: 85%**  
**Target: 100%**  
**Gap: 15% (8 tasks)**

---

## Overview

The State Management layer handles application state, singleton services, observable properties, and settings persistence. The current implementation is 85% complete with the following gaps identified.

## Gap Analysis

| Component | Status | Issue |
|-----------|--------|-------|
| Singleton Services | 85% | 5 services not thread-safe |
| Observable Properties | 0% | No INotifyPropertyChanged |
| Caching | 100% | ✅ Complete |
| Settings Persistence | 100% | ✅ Complete |

---

## Task 1: Fix Singleton Thread Safety

Five services are using non-thread-safe singleton patterns. Each must be updated to use `Lazy<T>` with `LazyThreadSafetyMode.ExecutionAndPublication`.

### 1.1 ProxyMonitor.cs

**Current Pattern (Unsafe):**
```csharp
private static ProxyMonitor _instance;
public static ProxyMonitor Instance => _instance ?? (_instance = new ProxyMonitor());
```

**Required Pattern (Thread-Safe):**
```csharp
private static readonly Lazy<ProxyMonitor> _lazyInstance = 
    new Lazy<ProxyMonitor>(() => new ProxyMonitor(), 
        LazyThreadSafetyMode.ExecutionAndPublication);

public static ProxyMonitor Instance => _lazyInstance.Value;
```

### 1.2 Services Requiring Fix

| Service | File | Priority |
|---------|------|----------|
| ProxyMonitor | Services/ProxyMonitor.cs | HIGH |
| AnalyticsDashboard | Services/AnalyticsDashboard.cs | MEDIUM |
| MultiAccountOrchestrator | Services/MultiAccountOrchestrator.cs | MEDIUM |
| PromptTemplateEngine | Services/PromptTemplateEngine.cs | LOW |
| MapScanner | Services/MapScanner.cs | LOW |

---

## Task 2: Implement ViewModelBase

Create a base class for all ViewModels that implements `INotifyPropertyChanged` with a helper method for property changes.

### 2.1 ViewModelBase.cs

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SvonyBrowser.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
```

---

## Task 3: Create MainWindowViewModel

The MainWindow needs a ViewModel to handle state for panel visibility, browser state, and tool outputs.

### 3.1 Properties Required

| Property | Type | Purpose |
|----------|------|---------|
| IsLeftPanelVisible | bool | Bot panel visibility |
| IsRightPanelVisible | bool | Client panel visibility |
| LeftPanelUrl | string | Current URL in left browser |
| RightPanelUrl | string | Current URL in right browser |
| AmfInput | string | AMF decoder input |
| AmfOutput | string | AMF decoder output |
| TroopAmount | int | Troop calculator input |
| TroopType | string | Selected troop type |
| StatusMessage | string | Status bar message |
| IsLoading | bool | Loading indicator |

---

## Task 4: Create SettingsViewModel

The SettingsWindow and SettingsControlCenter need a shared ViewModel bound to AppSettings.

### 4.1 Properties Required

All properties from AppSettings.cs should be exposed with PropertyChanged notifications.

---

## Implementation Checklist

- [ ] STATE-001: Fix ProxyMonitor singleton
- [ ] STATE-002: Fix AnalyticsDashboard singleton
- [ ] STATE-003: Fix MultiAccountOrchestrator singleton
- [ ] STATE-004: Fix PromptTemplateEngine singleton
- [ ] STATE-005: Fix MapScanner singleton
- [ ] STATE-006: Create ViewModelBase class
- [ ] STATE-007: Create MainWindowViewModel
- [ ] STATE-008: Create SettingsViewModel

---

## Verification

After implementation, verify:

1. All singletons are thread-safe (run concurrent access test)
2. ViewModels properly notify UI of changes
3. Settings changes persist correctly
4. No memory leaks from event handlers
