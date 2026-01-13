# 🔴 V7.0 START HERE - MANUS MUST CODE EVERYTHING

**CRITICAL: THE CODE IS NOT COMPLETE - ONLY DOCUMENTATION EXISTS**

---

## ⚠️ CURRENT PROJECT STATE

### What EXISTS (✅):
- Basic project structure
- MainWindow.xaml (with CefSharp error)
- App.xaml.cs (basic)
- SvonyBrowser.csproj file
- V7 Documentation (10+ docs)

### What DOES NOT EXIST (❌ MANUS MUST CODE):
- ❌ All 28 service classes
- ❌ Test project
- ❌ Working CefSharp integration
- ❌ All features described in docs
- ❌ Build configurations
- ❌ Playwright tests
- ❌ 200+ failsafes
- ❌ Zero errors/warnings build

---

## 🎯 MANUS IMPLEMENTATION CHECKLIST

### STEP 1: Fix Build Errors
```powershell
# Current error count: 50+ errors
# Required: 0 errors, 0 warnings

[ ] Fix CefSharp XAML compilation (MainWindow.xaml line 278)
[ ] Fix all CS0117 errors (missing properties/methods)
[ ] Fix all CS0103 errors (names not found)
[ ] Restore NuGet packages correctly
[ ] Build with Visual Studio 2022 (NOT dotnet CLI)
```

### STEP 2: Implement 28 Missing Services
Each service needs full implementation as described in V7-FAILSAFES.md:

```csharp
// CRITICAL SERVICES (implement first)
[ ] Services/GameStateEngine.cs - Singleton pattern, resource tracking
[ ] Services/ProtocolHandler.cs - Packet processing, protocol management
[ ] Services/StatusBarManager.cs - Status updates, UI coordination
[ ] Services/McpConnectionManager.cs - MCP server connections
[ ] Services/AutoPilotService.cs - Automation logic
[ ] Services/CombatSimulator.cs - Battle calculations

// HIGH PRIORITY SERVICES
[ ] Services/SessionRecorder.cs - Record/replay functionality
[ ] Services/TrafficPipeClient.cs - Fiddler integration
[ ] Services/FiddlerBridge.cs - Traffic interception
[ ] Services/CdpConnectionService.cs - Chrome DevTools Protocol
[ ] Services/PacketAnalysisEngine.cs - Packet analysis
[ ] Services/FailsafeManager.cs - Error recovery
[ ] Services/MemoryGuard.cs - Memory management
[ ] Services/CircuitBreaker.cs - Service protection

// MEDIUM PRIORITY SERVICES  
[ ] Services/AnalyticsDashboard.cs - Metrics and analytics
[ ] Services/ChatbotService.cs - AI chat integration
[ ] Services/ExportImportManager.cs - Data export/import
[ ] Services/MapScanner.cs - Map analysis
[ ] Services/MultiAccountOrchestrator.cs - Multi-account support
[ ] Services/PromptTemplateEngine.cs - Template management
[ ] Services/ProxyMonitor.cs - Proxy management
[ ] Services/SessionManager.cs - Session handling
[ ] Services/StrategicAdvisor.cs - Strategy recommendations
[ ] Services/LlmIntegrationService.cs - LLM integration
[ ] Services/RealDataProvider.cs - Real-time data
[ ] Services/VisualAutomationService.cs - Visual automation

// LOW PRIORITY SERVICES
[ ] Services/WebhookHub.cs - Webhook management
[ ] Services/DebugService.cs - Debug utilities
```

### STEP 3: Create Test Project
Follow V7-TESTING-100.md exactly:

```powershell
[ ] Create SvonyBrowser.Tests project
[ ] Install all test packages (xunit, FluentAssertions, Moq, etc.)
[ ] Implement tests for ALL 28 services
[ ] Achieve 95%+ code coverage
[ ] Setup coverage reporting
```

### STEP 4: Implement Playwright E2E Tests
Follow V7-PLAYWRIGHT-COMPLETE.md:

```powershell
[ ] npm init playwright@latest
[ ] Create playwright.config.ts
[ ] Implement all test suites
[ ] Setup CI/CD integration
```

### STEP 5: Implement 200+ Failsafes
Follow V7-FAILSAFES-ENHANCED.md:

```csharp
[ ] Windows Build Failsafes (1-100)
[ ] UI Automation Failsafes (101-120)  
[ ] Database Failsafes (121-140)
[ ] Security Failsafes (141-160)
[ ] Performance Failsafes (161-180)
[ ] Integration Failsafes (181-200)
```

### STEP 6: Build All 20 Configurations
Follow V7-BUILD-MATRIX-ENHANCED.md:

```powershell
[ ] Debug Build
[ ] Release Build
[ ] Single File Executable
[ ] AOT Compiled
[ ] Trimmed Build
[ ] Ready To Run
[ ] Self-Contained
[ ] Framework-Dependent
[ ] Portable ZIP
[ ] MSI Installer
[ ] MSIX Package
[ ] ClickOnce
[ ] Docker Container
[ ] NuGet Package
[ ] Azure Package
[ ] InnoSetup
[ ] WiX Installer
[ ] AppImage (Linux)
[ ] Snap Package
[ ] Chocolatey Package
```

---

## 📚 V7 DOCUMENTATION REFERENCES

### Build & Configuration
- **V7-WINDOWS-BUILDS-ENHANCED.md** - 15 VS2022 versions, MSBuild paths
- **V7-BUILD-MATRIX-ENHANCED.md** - 20 build types with exact commands
- **V7-ERROR-FIXES.md** - Every error and solution

### Implementation Guides
- **V7-FAILSAFES-ENHANCED.md** - 200+ failsafe implementations with code
- **V7-TESTING-100.md** - Complete test templates for 100% coverage
- **V7-PLAYWRIGHT-COMPLETE.md** - Full Playwright automation

### Master Guides
- **V7-COMPLETE-MANUS.md** - Original master guide
- **WINDOWS-BUILD-FIX.md** - CefSharp XAML fixes

---

## 🔥 EXACT IMPLEMENTATION ORDER

### Phase 1: Make It Build (Day 1)
```powershell
1. Fix CefSharp XAML error
2. Create stub implementations for all 28 services
3. Fix all compile errors
4. Achieve first successful build
```

### Phase 2: Core Services (Day 2)
```powershell
5. Implement GameStateEngine fully
6. Implement ProtocolHandler fully
7. Implement McpConnectionManager fully
8. Implement StatusBarManager fully
9. Test core functionality
```

### Phase 3: Testing (Day 3)
```powershell
10. Create test project
11. Write unit tests for core services
12. Achieve 95% coverage on core
13. Setup Playwright
14. Create E2E tests
```

### Phase 4: All Services (Day 4)
```powershell
15. Implement remaining 24 services
16. Write tests for all services
17. Achieve 95% total coverage
```

### Phase 5: Failsafes & Polish (Day 5)
```powershell
18. Implement 200+ failsafes
19. Build all 20 configurations
20. Final testing and verification
```

---

## ✅ VERIFICATION CHECKLIST

```powershell
# Build Verification
[ ] dotnet build: 0 errors, 0 warnings
[ ] msbuild: 0 errors, 0 warnings
[ ] All 20 build configurations work

# Test Verification
[ ] 400+ unit tests passing
[ ] 95%+ code coverage
[ ] All Playwright tests passing
[ ] Performance benchmarks met

# Service Verification
[ ] All 28 services implemented
[ ] All services have IDisposable
[ ] All services have error handling
[ ] All services have logging

# Documentation Verification
[ ] Code matches V7 documentation
[ ] All features documented work
[ ] README.md updated with V7.0
```

---

## 🚀 FINAL STEPS

```bash
# After ALL code is complete and tested:

1. Final build and test
   dotnet build --configuration Release
   dotnet test --collect:"XPlat Code Coverage"

2. Verify zero issues
   - 0 errors
   - 0 warnings  
   - 95%+ coverage

3. Commit everything
   git add .
   git commit -m "Implement V7.0 - all 28 services, 200+ failsafes, 95% test coverage"

4. Create release tag
   git tag v7.0.0 -m "Version 7.0.0 - Complete implementation"

5. Push to GitHub
   git push origin main --tags

6. Create GitHub Release
   - Go to GitHub releases
   - Create release from v7.0.0 tag
   - Upload all 20 build artifacts
   - Publish release
```

---

## ⚠️ CRITICAL NOTES FOR MANUS

1. **DO NOT SKIP STEPS** - Each step builds on the previous
2. **USE VISUAL STUDIO 2022** - NOT dotnet CLI for builds
3. **TEST EVERYTHING** - 95% coverage is REQUIRED
4. **FOLLOW DOCS EXACTLY** - All V7 docs have exact code
5. **VERIFY 5 TIMES** - As requested, audit everything 5 times

---

## 🔴 CURRENT ERRORS TO FIX FIRST

```
1. CefSharp XAML compilation error (line 278)
2. CS0117: 'App' does not contain 'DataPath'
3. CS0103: 'GameStateEngine' does not exist
4. CS0103: 'ProtocolHandler' does not exist
5. CS0103: 'StatusBarManager' does not exist
[... 45+ more errors]
```

---

**MANUS: START WITH STEP 1 - FIX BUILD ERRORS**

**ALL CODE MUST BE IMPLEMENTED FROM THE V7 DOCUMENTATION**

**THIS IS NOT OPTIONAL - V7.0 REQUIRES FULL IMPLEMENTATION**
