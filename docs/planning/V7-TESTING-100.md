# 🧪 V7.0 TESTING 100% - COMPLETE TEST COVERAGE

**EXACT TEST CODE FOR EVERY SERVICE - 100% COVERAGE REQUIRED**

---

## 🔴 TEST PROJECT SETUP

```powershell
# Create test project with ALL packages
cd D:\Fiddler-FlashBrowser
dotnet new xunit -n SvonyBrowser.Tests

cd SvonyBrowser.Tests
dotnet add package Microsoft.NET.Test.Sdk --version 17.8.0
dotnet add package xunit --version 2.6.1
dotnet add package xunit.runner.visualstudio --version 2.5.3
dotnet add package FluentAssertions --version 6.12.0
dotnet add package Moq --version 4.20.70
dotnet add package coverlet.msbuild --version 6.0.0
dotnet add reference ..\SvonyBrowser\SvonyBrowser.csproj
```

---

## 📋 COMPLETE TEST TEMPLATE FOR EACH SERVICE

### Universal Test Template (USE FOR ALL 28 SERVICES)
```csharp
using FluentAssertions;
using Moq;
using SvonyBrowser.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SvonyBrowser.Tests.Unit.Services;

public class [SERVICE_NAME]Tests : IDisposable
{
    private readonly [SERVICE_NAME] _sut;
    
    public [SERVICE_NAME]Tests()
    {
        _sut = [SERVICE_NAME].Instance;
    }
    
    [Fact]
    public void Instance_ShouldReturnSingleton()
    {
        var instance1 = [SERVICE_NAME].Instance;
        var instance2 = [SERVICE_NAME].Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        _sut.Should().NotBeNull();
        // Add specific property checks
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("test")]
    public void Method_ShouldHandleVariousInputs(string input)
    {
        // Test each public method
        // Act & Assert for each
    }
    
    [Fact]
    public async Task AsyncMethod_ShouldCompleteSuccessfully()
    {
        // Test async methods
        var result = await _sut.SomeAsyncMethod();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public void ErrorCondition_ShouldThrowExpectedException()
    {
        // Test error handling
        Action act = () => _sut.MethodThatThrows();
        act.Should().Throw<ExpectedException>();
    }
    
    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        _sut.Dispose();
        // Verify cleanup
        _sut.IsDisposed.Should().BeTrue();
    }
    
    public void Dispose() => _sut?.Dispose();
}
```

---

## 🎯 SERVICES THAT NEED TESTS (ALL 28)

| Service                  | Min Tests | Priority |
| ------------------------ | --------- | -------- |
| GameStateEngine          | 30        | CRITICAL |
| ProtocolHandler          | 25        | CRITICAL |
| StatusBarManager         | 20        | CRITICAL |
| McpConnectionManager     | 25        | CRITICAL |
| AutoPilotService         | 20        | HIGH     |
| CombatSimulator          | 25        | HIGH     |
| SessionRecorder          | 15        | HIGH     |
| TrafficPipeClient        | 15        | HIGH     |
| AnalyticsDashboard       | 15        | MEDIUM   |
| ChatbotService           | 20        | MEDIUM   |
| ExportImportManager      | 15        | MEDIUM   |
| MapScanner               | 15        | MEDIUM   |
| MultiAccountOrchestrator | 20        | MEDIUM   |
| PromptTemplateEngine     | 10        | MEDIUM   |
| ProxyMonitor             | 10        | MEDIUM   |
| SessionManager           | 15        | MEDIUM   |
| StrategicAdvisor         | 20        | MEDIUM   |
| WebhookHub               | 10        | LOW      |
| FiddlerBridge            | 15        | HIGH     |
| CdpConnectionService     | 20        | HIGH     |
| LlmIntegrationService    | 15        | MEDIUM   |
| RealDataProvider         | 10        | MEDIUM   |
| VisualAutomationService  | 15        | MEDIUM   |
| PacketAnalysisEngine     | 20        | HIGH     |
| DebugService             | 10        | LOW      |
| FailsafeManager          | 15        | HIGH     |
| MemoryGuard              | 10        | HIGH     |
| CircuitBreaker           | 10        | HIGH     |

---

## 📊 COVERAGE CONFIGURATION

### coverlet.runsettings
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>opencover,cobertura</Format>
          <Exclude>[*]*.Program,[*]*.Startup</Exclude>
          <Include>[SvonyBrowser*]*</Include>
          <ExcludeByAttribute>Obsolete,GeneratedCode</ExcludeByAttribute>
          <SingleHit>false</SingleHit>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

---

## 🚀 RUN ALL TESTS WITH COVERAGE

### Complete Test Script
```powershell
# run-tests.ps1
Write-Host "SVONY BROWSER V7.0 - COMPLETE TEST RUN" -ForegroundColor Cyan

# Clean
Remove-Item -Recurse -Force TestResults -ErrorAction SilentlyContinue

# Build
dotnet build SvonyBrowser.Tests.csproj

# Run ALL tests with coverage
dotnet test `
  --collect:"XPlat Code Coverage" `
  --settings coverlet.runsettings `
  --results-directory TestResults `
  --logger "trx;LogFileName=results.trx" `
  --logger "console;verbosity=normal"

# Generate HTML report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator `
  -reports:"TestResults\**\coverage.cobertura.xml" `
  -targetdir:"TestResults\Report" `
  -reporttypes:"Html;Badges"

# Check coverage
$xml = [xml](Get-Content TestResults\*\coverage.cobertura.xml)
$rate = [double]$xml.coverage.'line-rate'
$percentage = [math]::Round($rate * 100, 2)

if ($percentage -lt 95) {
    Write-Error "COVERAGE FAILED: $percentage% (Required: 95%)"
    exit 1
} else {
    Write-Host "COVERAGE PASSED: $percentage%" -ForegroundColor Green
}

# Open report
Start-Process TestResults\Report\index.html
```

---

## ✅ SPECIFIC TEST EXAMPLES

### GameStateEngineTests.cs (COMPLETE)
```csharp
public class GameStateEngineTests
{
    private readonly GameStateEngine _sut = GameStateEngine.Instance;
    
    [Theory]
    [InlineData("food", 1000)]
    [InlineData("wood", 2000)]
    [InlineData("stone", 3000)]
    [InlineData("iron", 4000)]
    public void UpdateResource_ShouldWork(string type, long amount)
    {
        _sut.UpdateResource(type, amount);
        _sut.CurrentState.Resources.Values[type].Should().Be(amount);
    }
    
    [Fact]
    public void GetResourceRates_ShouldCalculate()
    {
        var rates = _sut.GetResourceRates();
        rates.Should().NotBeNull();
        rates.Should().HaveCountGreaterThan(0);
    }
    
    [Fact]
    public void GetActiveMarches_ShouldFilter()
    {
        _sut.CurrentState.Marches.Add(new MarchState { Status = "active" });
        _sut.CurrentState.Marches.Add(new MarchState { Status = "completed" });
        
        var active = _sut.GetActiveMarches();
        active.Should().HaveCount(1);
    }
    
    // Add 27 more tests...
}
```

### ProtocolHandlerTests.cs
```csharp
public class ProtocolHandlerTests
{
    private readonly ProtocolHandler _sut = ProtocolHandler.Instance;
    
    [Fact]
    public async Task SendPacket_ShouldSucceed()
    {
        var result = await _sut.SendPacket("test", new JObject());
        result.Should().BeTrue();
    }
    
    [Fact]
    public void GetPacketRate_ShouldCalculate()
    {
        var rate = _sut.GetPacketRate();
        rate.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void GetDecodeSuccessRate_ShouldBePercentage()
    {
        var rate = _sut.GetDecodeSuccessRate();
        rate.Should().BeInRange(0, 100);
    }
    
    // Add 22 more tests...
}
```

---

## 🎭 E2E TEST WITH PLAYWRIGHT

### PlaywrightE2ETests.cs
```csharp
using Microsoft.Playwright;

public class PlaywrightE2ETests : IAsyncLifetime
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private IPage _page;
    
    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _page = await _browser.NewPageAsync();
    }
    
    [Fact]
    public async Task FullUserFlow_ShouldWork()
    {
        // Launch app
        await _page.GotoAsync("http://localhost:5000");
        
        // Login
        await _page.FillAsync("#username", "test");
        await _page.FillAsync("#password", "test");
        await _page.ClickAsync("#login");
        
        // Verify dual panels
        var left = await _page.QuerySelectorAsync("#leftBrowser");
        var right = await _page.QuerySelectorAsync("#rightBrowser");
        
        left.Should().NotBeNull();
        right.Should().NotBeNull();
        
        // Connect MCP
        await _page.ClickAsync("#connectMcp");
        await _page.WaitForSelectorAsync(".connected");
        
        // Load Evony
        await _page.FillAsync("#leftUrl", "https://evony.com");
        await _page.PressAsync("#leftUrl", "Enter");
        
        // Verify loaded
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
    
    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }
}
```

---

## 📈 GITHUB ACTIONS TEST WORKFLOW

### .github/workflows/tests.yml
```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 6.0.x
    
    - name: Restore
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test with Coverage
      run: |
        dotnet test `
          --no-build `
          --collect:"XPlat Code Coverage" `
          --results-directory TestResults
    
    - name: Generate Report
      run: |
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator `
          -reports:TestResults\**\coverage.cobertura.xml `
          -targetdir:coverage-report `
          -reporttypes:MarkdownSummary
    
    - name: Upload Coverage
      uses: codecov/codecov-action@v3
      with:
        files: TestResults/**/coverage.cobertura.xml
        fail_ci_if_error: true
    
    - name: Check Coverage Threshold
      run: |
        $xml = [xml](Get-Content TestResults\*\coverage.cobertura.xml)
        $rate = [double]$xml.coverage.'line-rate'
        if ($rate -lt 0.95) {
          throw "Coverage below 95%: $($rate * 100)%"
        }
```

---

## ✅ FINAL TEST CHECKLIST

```
TEST PROJECT
[ ] Created SvonyBrowser.Tests project
[ ] All packages installed
[ ] Reference to main project added

UNIT TESTS (400+ TOTAL)
[ ] All 28 services have tests
[ ] Each service has 10+ tests minimum
[ ] Singleton pattern tested
[ ] All public methods tested
[ ] Error conditions tested
[ ] Dispose tested

INTEGRATION TESTS
[ ] Database integration tested
[ ] MCP connection tested
[ ] File I/O tested

E2E TESTS
[ ] Full user flow tested
[ ] Dual panel tested
[ ] MCP connection tested
[ ] Evony loading tested

COVERAGE
[ ] Line coverage >= 95%
[ ] Branch coverage >= 90%
[ ] All critical paths covered
[ ] Report generated

CI/CD
[ ] GitHub Actions workflow created
[ ] Tests run on every push
[ ] Coverage uploaded to Codecov
[ ] Build fails if coverage < 95%
```

---

**NO EXCUSES - 100% TEST COVERAGE OR NO RELEASE**
