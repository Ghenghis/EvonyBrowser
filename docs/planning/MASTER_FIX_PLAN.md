# Master Fix Plan for Svony Browser V7.0

## Error Categories Identified

### Category 1: CefSharp Assembly Reference Issues (CRITICAL)
The WPF temporary project doesn't properly reference CefSharp assemblies.

**Errors:**
- CS0012: Type 'CefSettingsBase' is defined in assembly not referenced (CefSharp.Core)
- CS0012: Type 'FrameLoadEndEventArgs' is defined in assembly not referenced (CefSharp)
- CS0012: Type 'LoadErrorEventArgs' is defined in assembly not referenced (CefSharp)
- CS0117: 'CefSettings' does not contain definition for various properties
- CS0103: 'Cef', 'LogSeverity' do not exist in current context

**Root Cause:** CefSharp.Core package not being properly restored/referenced

**Fix:** Add explicit CefSharp.Core package reference to project

### Category 2: Missing Settings Properties (HIGH)
Settings classes missing expected properties.

**Errors:**
- CS1061: 'McpSettings' missing: AutoStart, RetryDelaySeconds, TimeoutSeconds
- CS1061: 'GeneralSettings' missing: CheckForUpdates
- CS1061: 'FiddlerSettings' missing: AutoStart, AutoDecodeAmf
- CS1061: 'BrowserSettings' missing: EnableGpu, CacheSizeMb
- CS1061: 'AutomationSettings' missing: ActionDelayMs
- CS1061: 'AdvancedSettings' missing: MaxMemoryMb

**Root Cause:** Settings classes don't have all properties defined

**Fix:** Add missing properties to Settings classes

### Category 3: Type Conflicts (MEDIUM)
Duplicate type definitions causing conflicts.

**Errors:**
- CS0019: Operator '==' cannot be applied to McpConnectionStatus
- CS0123: No overload for 'OnMcpStatusChanged' matches delegate
- CS1503: Cannot convert from 'SvonyBrowser.Services.McpConnectionStatus' to 'SvonyBrowser.McpConnectionStatus'

**Root Cause:** McpConnectionStatus enum defined in multiple namespaces

**Fix:** Remove duplicate enum, use single definition

### Category 4: Missing Methods (MEDIUM)
Methods referenced but not defined.

**Errors:**
- CS1061: 'ProtocolHandler' missing: SendPacket
- CS1061: 'ChromiumWebBrowser' missing: LoadHtml

**Root Cause:** Methods not implemented or wrong method names

**Fix:** Add missing methods or fix method calls

### Category 5: ChatMessageViewModel Missing Properties (LOW)
**Errors:**
- CS1061: 'ChatMessageViewModel' missing: Timestamp, Role

**Fix:** Add missing properties to ChatMessageViewModel

## Implementation Order

1. Fix CefSharp package references (add CefSharp.Core explicitly)
2. Fix Settings classes (add all missing properties)
3. Fix McpConnectionStatus type conflict
4. Fix missing methods
5. Fix ChatMessageViewModel
6. Test build locally
7. Push and verify

## Files to Modify

1. SvonyBrowser.csproj - Add CefSharp.Core reference
2. Services/SettingsManager.cs - Add missing settings properties
3. Services/McpConnectionManager.cs - Fix McpConnectionStatus
4. MainWindow.xaml.cs - Fix event handlers and LoadHtml
5. Controls/ChatbotPanel.xaml.cs - Fix ChatMessageViewModel
