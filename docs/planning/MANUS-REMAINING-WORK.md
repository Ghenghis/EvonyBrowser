# MANUS: Remaining Work for V7.0

## STATUS: CefSharp is FIXED - Do NOT touch it!
Claude Code fixed the CefSharp self-hosting in v7.0.2. The app now runs.
**DO NOT MODIFY**: Program.cs, CefSharp initialization, or build settings.

---

## REMAINING ISSUES TO FIX:

### Issue 1: Flash SWF Files Not Found
**Current behavior:** App shows "AutoEvony.swf Not Found"
**Expected:** App loads Flash content from SWF files

**Fix needed in**: `MainWindow.xaml.cs` or settings
```csharp
// The paths are hardcoded - make them configurable
// Current: C:\Users\Admin\Downloads\AutoEvony.swf
// Should: Read from settings or let user configure
```

**Simple fix:**
1. Add path configuration to Settings window
2. Store paths in AppSettings.json
3. Load from settings on startup

### Issue 2: Fiddler Proxy Not Detected
**Current behavior:** "Warning: Fiddler proxy not detected"
**Expected:** Connect to Fiddler for traffic capture

**Check these files:**
- `Services/ProxyMonitor.cs` - verify proxy detection logic
- Ensure Fiddler is running on expected port (8888)

### Issue 3: Complete Service Implementations
**Files to verify are complete:**
- [ ] CombatSimulator.cs
- [ ] StrategicAdvisor.cs
- [ ] McpConnectionManager.cs
- [ ] AutomationEngine.cs

### Issue 4: Test Coverage
**Goal:** 95% test coverage
**Current:** Tests exist but need to run against real project

**Run tests:**
```powershell
dotnet test SvonyBrowser.Tests --collect:"XPlat Code Coverage"
```

---

## RULES FOR MANUS:

1. **DO NOT** modify Program.cs - CefSharp is working
2. **DO NOT** change build settings in .csproj
3. **DO NOT** add or remove CefSharp packages
4. **ALWAYS** test builds locally before pushing
5. **FOCUS** on simple configuration and UI fixes
6. **ASK** if unsure about CefSharp-related code

---

## BUILD COMMAND (v7.0.2 works):
```powershell
dotnet publish SvonyBrowser/SvonyBrowser.csproj -c Release -r win-x64 --self-contained -o artifacts/release
```

## TEST COMMAND:
```powershell
dotnet test SvonyBrowser.Tests/SvonyBrowser.Tests.csproj
```

---

## WHAT'S ALREADY WORKING:
- CefSharp browser engine initializes
- UI renders correctly
- Co-Pilot chatbot panel works
- Dual browser panels display
- Logging system works
- Configuration directories created

## WHAT NEEDS FIXING:
1. Flash SWF path configuration (simple settings fix)
2. Fiddler proxy connection (check port settings)
3. Any UI buttons that don't respond
4. Test coverage verification

---

## SUCCESS CRITERIA:
- [ ] App loads Flash SWF files when paths are configured
- [ ] Fiddler proxy connects when Fiddler is running
- [ ] All UI elements respond to clicks
- [ ] Tests pass with 90%+ coverage
- [ ] No error dialogs on startup

This is straightforward work - no complex CefSharp issues remaining.
