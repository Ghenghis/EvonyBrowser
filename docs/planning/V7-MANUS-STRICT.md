# 🏰 SVONY BROWSER V7.0 - MANUS STRICT INSTRUCTIONS

**VERSION**: 7.0.0 Phoenix Edition  
**TARGET**: Production-Ready, Zero Errors, 100% Test Coverage  
**PRIORITY**: BUILD MUST PASS FIRST

---

## ⚠️ CRITICAL RULES - READ BEFORE ANYTHING

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                        ⛔ MANDATORY REQUIREMENTS ⛔                            ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                                ║
║  1. BUILD MUST PASS - Run `dotnet build` after EVERY change                   ║
║  2. ZERO ERRORS - No CS#### errors allowed                                    ║
║  3. ZERO WARNINGS - Use /warnaserror flag                                     ║
║  4. 100% TEST COVERAGE - Test REAL codebase files only                        ║
║  5. NO FAKE TESTS - Never create mock/stub test files                         ║
║  6. NO PLACEHOLDERS - Full implementations only                               ║
║  7. NO TODO COMMENTS - Implement or don't touch                               ║
║  8. CHECK GITHUB RELEASES - Verify releases are enabled                       ║
║  9. CHECK GITHUB ISSUES - Fix all reported bugs                               ║
║  10. COMMIT FREQUENTLY - Small, verified commits                              ║
║                                                                                ║
╚═══════════════════════════════════════════════════════════════════════════════╝
```

---

## 📋 PHASE 1: FIX BUILD ERRORS (DO THIS FIRST)

### Step 1.1: Clean and Restore
```powershell
cd D:\Fiddler-FlashBrowser
dotnet clean SvonyBrowser/SvonyBrowser.csproj
dotnet restore SvonyBrowser/SvonyBrowser.csproj
```

### Step 1.2: Fix .csproj Duplicate Items
The SDK-style project auto-includes files. Remove these sections from `SvonyBrowser.csproj`:
- Remove all `<Compile Include="...">` items (SDK includes them automatically)
- Remove all `<Page Include="...">` items (SDK includes them automatically)
- Keep only `<PackageReference>`, `<Content>`, and `<None>` items

### Step 1.3: Current Build Errors to Fix

| File                         | Line | Error                       | Fix                                                      |
| ---------------------------- | ---- | --------------------------- | -------------------------------------------------------- |
| `MainWindow.xaml`            | 278  | CefSharp assembly not found | Run `dotnet restore` first, ensure packages restored     |
| `SettingsControlCenter.xaml` | 204  | 'local' undeclared prefix   | Add `xmlns:local="clr-namespace:SvonyBrowser"` to Window |
| `SettingsControlCenter.xaml` | 45   | thumbTransform not found    | Change to use Margin instead of Transform                |

### Step 1.4: Verify Build
```powershell
dotnet build SvonyBrowser/SvonyBrowser.csproj
# Must show: Build succeeded
# Must show: 0 Error(s)
```

---

## 📋 PHASE 2: FIX ALL WARNINGS

### Step 2.1: Build with Warnings as Errors
```powershell
dotnet build SvonyBrowser/SvonyBrowser.csproj /warnaserror
```

### Step 2.2: Common Warning Fixes

| Warning                  | Fix                                |
| ------------------------ | ---------------------------------- |
| CS8600 Null assignment   | Add `?` to type or null check      |
| CS8601 Null reference    | Use `?.` or `!` operator           |
| CS8602 Dereference null  | Add null check before access       |
| CS8604 Null argument     | Validate parameter before passing  |
| CS0168 Unused variable   | Remove or use `_` discard          |
| CS0219 Unused assignment | Remove assignment                  |
| NETSDK1138 EOL framework | Expected - suppress in .csproj     |
| NU1903 Vulnerability     | Expected - CefSharp issue          |
| NU1701 Package restored  | Expected - CefSharp .NET Framework |

### Step 2.3: Suppress Expected Warnings
Add to `SvonyBrowser.csproj` PropertyGroup:
```xml
<NoWarn>$(NoWarn);NETSDK1138;NU1903;NU1701</NoWarn>
```

---

## 📋 PHASE 3: IMPLEMENT 100% TESTING

### Step 3.1: Create Test Project
```powershell
cd D:\Fiddler-FlashBrowser
dotnet new xunit -n SvonyBrowser.Tests -o SvonyBrowser.Tests
cd SvonyBrowser.Tests
dotnet add reference ../SvonyBrowser/SvonyBrowser.csproj
dotnet add package FluentAssertions
dotnet add package Moq
dotnet add package Microsoft.Playwright
dotnet add package coverlet.collector
```

### Step 3.2: Test Project Structure
```
SvonyBrowser.Tests/
├── Unit/
│   └── Services/
│       ├── GameStateEngineTests.cs      # Test GameStateEngine.cs
│       ├── ProtocolHandlerTests.cs      # Test ProtocolHandler.cs
│       ├── CombatSimulatorTests.cs      # Test CombatSimulator.cs
│       ├── AutoPilotServiceTests.cs     # Test AutoPilotService.cs
│       └── ... (ALL 28 services)
├── Integration/
│   └── McpConnectionTests.cs
├── E2E/
│   └── PlaywrightTests.cs
└── SvonyBrowser.Tests.csproj
```

### Step 3.3: Test Template (USE THIS EXACT PATTERN)
```csharp
using FluentAssertions;
using SvonyBrowser.Services;
using Xunit;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Tests for [ServiceName] - Testing REAL file: SvonyBrowser/Services/[ServiceName].cs
/// </summary>
public class [ServiceName]Tests
{
    [Fact]
    public void Instance_ShouldReturnSingleton()
    {
        var instance1 = [ServiceName].Instance;
        var instance2 = [ServiceName].Instance;
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void [MethodName]_WhenCalled_ShouldReturnExpectedResult()
    {
        // Arrange
        var sut = [ServiceName].Instance;
        
        // Act
        var result = sut.[MethodName]();
        
        // Assert
        result.Should().NotBeNull();
    }
}
```

### Step 3.4: Run Tests with Coverage
```powershell
dotnet test SvonyBrowser.Tests/SvonyBrowser.Tests.csproj `
  --collect:"XPlat Code Coverage" `
  --results-directory ./coverage

# View coverage report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:coverage/**/coverage.cobertura.xml -targetdir:coverage/report
```

### Step 3.5: Coverage Requirements
- **Unit Tests**: 100% of all public methods in Services/
- **Integration Tests**: 100% of MCP server connections
- **E2E Tests**: 100% of UI critical paths
- **Performance Tests**: Memory leak detection, response times

---

## 📋 PHASE 4: GITHUB CHECKS

### Step 4.1: Check Releases
1. Go to: https://github.com/Ghenghis/Svony-Browser/releases
2. Verify releases are ENABLED (not disabled)
3. Create release v7.0.0 if not exists:
   ```bash
   git tag v7.0.0
   git push origin v7.0.0
   ```

### Step 4.2: Check Issues
1. Go to: https://github.com/Ghenghis/Svony-Browser/issues
2. Review ALL open issues
3. Fix bugs reported by users
4. Close fixed issues with commit reference

### Step 4.3: Verify CI/CD
1. Check: https://github.com/Ghenghis/Svony-Browser/actions
2. Ensure workflows exist:
   - `.github/workflows/ci.yml` - Build/Test on push
   - `.github/workflows/release.yml` - Create release on tag

---

## 📋 PHASE 5: FAILSAFE IMPLEMENTATION

### Step 5.1: Create DebugService.cs
```csharp
// File: SvonyBrowser/Services/DebugService.cs
namespace SvonyBrowser.Services;

public sealed class DebugService : IDisposable
{
    private static readonly Lazy<DebugService> _instance = new(() => new());
    public static DebugService Instance => _instance.Value;
    
    public ConcurrentQueue<DebugEntry> Entries { get; } = new();
    public event Action<DebugEntry>? EntryAdded;
    
    public void Log(string message, LogLevel level = LogLevel.Information,
        Exception? ex = null,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        var entry = new DebugEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message,
            Exception = ex,
            CallerMethod = caller,
            CallerFile = Path.GetFileName(file),
            CallerLine = line
        };
        
        Entries.Enqueue(entry);
        while (Entries.Count > 10000 && Entries.TryDequeue(out _)) { }
        
        EntryAdded?.Invoke(entry);
        
        // Auto-correction for known errors
        if (ex != null) TryAutoCorrect(ex);
    }
    
    private void TryAutoCorrect(Exception ex)
    {
        switch (ex)
        {
            case OutOfMemoryException:
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Log("Auto-corrected: Memory cleared", LogLevel.Warning);
                break;
            case SocketException:
                Task.Run(() => McpConnectionManager.Instance.ReconnectAllAsync());
                Log("Auto-corrected: Reconnecting MCP servers", LogLevel.Warning);
                break;
        }
    }
    
    public void Dispose() { while (Entries.TryDequeue(out _)) { } }
}

public class DebugEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = "";
    public Exception? Exception { get; set; }
    public string CallerMethod { get; set; } = "";
    public string CallerFile { get; set; } = "";
    public int CallerLine { get; set; }
}

public enum LogLevel { Debug, Information, Warning, Error, Critical }
```

### Step 5.2: Create CircuitBreaker
```csharp
// Add to Services/CircuitBreaker.cs
public class CircuitBreaker
{
    private int _failureCount;
    private DateTime _lastFailure;
    private readonly int _threshold = 5;
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(1);
    
    public bool IsOpen => _failureCount >= _threshold && 
                          DateTime.UtcNow - _lastFailure < _timeout;
    
    public void RecordSuccess() => _failureCount = 0;
    
    public void RecordFailure()
    {
        _failureCount++;
        _lastFailure = DateTime.UtcNow;
    }
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, T fallback)
    {
        if (IsOpen) return fallback;
        try
        {
            var result = await action();
            RecordSuccess();
            return result;
        }
        catch
        {
            RecordFailure();
            return fallback;
        }
    }
}
```

---

## 📋 PHASE 6: PLAYWRIGHT STUDIO CLI

### Step 6.1: Create CLI Package
```powershell
mkdir -p CLI/playwright-studio
cd CLI/playwright-studio
npm init -y
npm install commander playwright chalk ora
```

### Step 6.2: CLI Commands to Implement

| Command                     | Description                       |
| --------------------------- | --------------------------------- |
| `pws launch`                | Launch browser (chromium/firefox) |
| `pws record`                | Record actions, generate code     |
| `pws task <name>`           | Run saved automation task         |
| `pws task-list`             | List all tasks                    |
| `pws task-create`           | Create new task                   |
| `pws screenshot`            | Take screenshot                   |
| `pws pdf`                   | Generate PDF                      |
| `pws trace start/stop/view` | Tracing commands                  |
| `pws ai "<prompt>"`         | AI-powered automation             |
| `pws evony login`           | Login to Evony                    |
| `pws evony collect`         | Collect resources                 |
| `pws evony build`           | Auto build                        |
| `pws evony train`           | Auto train                        |

### Step 6.3: CLI Implementation (index.js)
```javascript
#!/usr/bin/env node
import { program } from 'commander';
import { chromium } from 'playwright';
import chalk from 'chalk';
import ora from 'ora';

program.name('pws').version('7.0.0');

program.command('launch')
  .option('-b, --browser <type>', 'Browser', 'chromium')
  .option('-h, --headless', 'Headless mode')
  .action(async (opts) => {
    const browser = await chromium.launch({ headless: opts.headless });
    const page = await browser.newPage();
    console.log(chalk.green('Browser launched'));
  });

program.command('screenshot')
  .option('-u, --url <url>', 'URL')
  .option('-o, --output <file>', 'Output file', 'screenshot.png')
  .action(async (opts) => {
    const browser = await chromium.launch();
    const page = await browser.newPage();
    await page.goto(opts.url);
    await page.screenshot({ path: opts.output });
    await browser.close();
    console.log(chalk.green(`Screenshot saved: ${opts.output}`));
  });

// Add all other commands...

program.parse();
```

---

## 📋 PHASE 7: DOCUMENTATION CHECKLIST

### Required Documentation Files

| File                    | Status    | Description                        |
| ----------------------- | --------- | ---------------------------------- |
| `README.md`             | ✅ Updated | Main project readme with ASCII art |
| `CHANGELOG.md`          | 🔲 Create  | Full version history               |
| `ARCHITECTURE.md`       | ✅ Exists  | System architecture                |
| `API-REFERENCE.md`      | ✅ Exists  | API documentation                  |
| `SERVICES-REFERENCE.md` | ✅ Exists  | Service documentation              |
| `TESTING-GUIDE.md`      | 🔲 Create  | Testing instructions               |
| `DEPLOYMENT-GUIDE.md`   | 🔲 Create  | Deployment instructions            |
| `TROUBLESHOOTING.md`    | 🔲 Create  | Common issues/fixes                |

### Required Diagrams

| Diagram              | Status | Location                               |
| -------------------- | ------ | -------------------------------------- |
| Service Architecture | ✅      | docs/diagrams/service-architecture.png |
| Data Flow            | ✅      | docs/diagrams/data-flow-v6.png         |
| MCP Architecture     | ✅      | docs/diagrams/mcp-architecture.png     |
| Testing Flow         | 🔲      | docs/diagrams/testing-flow.png         |
| CI/CD Pipeline       | 🔲      | docs/diagrams/cicd-pipeline.png        |

---

## 📋 FINAL VERIFICATION CHECKLIST

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                      ✅ FINAL VERIFICATION CHECKLIST                          ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                                ║
║  BUILD                                                                         ║
║  [ ] dotnet build succeeds with 0 errors                                      ║
║  [ ] dotnet build /warnaserror succeeds with 0 warnings                       ║
║  [ ] dotnet publish creates working executable                                ║
║                                                                                ║
║  TESTS                                                                         ║
║  [ ] dotnet test passes 100%                                                  ║
║  [ ] Coverage report shows 95%+ coverage                                      ║
║  [ ] All E2E Playwright tests pass                                            ║
║  [ ] No fake/mock test files exist                                            ║
║                                                                                ║
║  GITHUB                                                                        ║
║  [ ] Releases enabled and v7.0.0 exists                                       ║
║  [ ] All issues addressed                                                     ║
║  [ ] CI/CD workflows passing                                                  ║
║  [ ] All changes committed and pushed                                         ║
║                                                                                ║
║  FEATURES                                                                      ║
║  [ ] DebugService with auto-correction                                        ║
║  [ ] CircuitBreaker failsafe                                                  ║
║  [ ] EventViewerPanel UI                                                      ║
║  [ ] Playwright Studio CLI                                                    ║
║  [ ] All MCP servers working                                                  ║
║                                                                                ║
║  DOCUMENTATION                                                                 ║
║  [ ] README.md complete with v7.0 details                                     ║
║  [ ] All markdown files updated                                               ║
║  [ ] All diagrams created                                                     ║
║  [ ] API reference complete                                                   ║
║                                                                                ║
╚═══════════════════════════════════════════════════════════════════════════════╝
```

---

## 🚀 EXECUTION COMMANDS

```powershell
# Full verification script
cd D:\Fiddler-FlashBrowser

# 1. Build
dotnet build SvonyBrowser/SvonyBrowser.csproj
if ($LASTEXITCODE -ne 0) { Write-Error "BUILD FAILED"; exit 1 }

# 2. Build with warnings
dotnet build SvonyBrowser/SvonyBrowser.csproj /warnaserror
if ($LASTEXITCODE -ne 0) { Write-Warning "WARNINGS PRESENT" }

# 3. Test
dotnet test SvonyBrowser.Tests/SvonyBrowser.Tests.csproj --collect:"XPlat Code Coverage"

# 4. Commit
git add -A
git commit -m "v7.0.0 Phoenix Edition - Production Ready"
git tag v7.0.0
git push origin main --tags
```

---

**MANUS: Follow this document step-by-step. BUILD MUST PASS FIRST before any other work.**
