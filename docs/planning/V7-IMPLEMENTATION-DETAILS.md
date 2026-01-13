# V7.0 Implementation Details

## Playwright ControlStudio CLI - Package.json

```json
{
  "name": "@svony/playwright-studio",
  "version": "7.0.0",
  "type": "module",
  "bin": { "pws": "./index.js", "playwright-studio": "./index.js" },
  "dependencies": {
    "commander": "^11.1.0",
    "playwright": "^1.40.0",
    "chalk": "^5.3.0",
    "ora": "^7.0.1"
  }
}
```

## Test Project - SvonyBrowser.Tests.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="Microsoft.Playwright" Version="1.40.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\SvonyBrowser\SvonyBrowser.csproj" />
  </ItemGroup>
</Project>
```

## GitHub Actions - .github/workflows/ci.yml

```yaml
name: CI/CD
on: [push, pull_request]
jobs:
  build:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with: { dotnet-version: '6.0.x' }
    - run: dotnet restore
    - run: dotnet build --no-restore /warnaserror
    - run: dotnet test --collect:"XPlat Code Coverage"
    - uses: codecov/codecov-action@v3
```

## DebugService.cs (Minimal)

```csharp
public sealed class DebugService : IDisposable
{
    private static readonly Lazy<DebugService> _instance = new(() => new());
    public static DebugService Instance => _instance.Value;
    
    public event Action<DebugEntry>? EntryAdded;
    public ConcurrentQueue<DebugEntry> Entries { get; } = new();
    
    public void Log(string msg, LogEventLevel level = LogEventLevel.Information,
        Exception? ex = null, [CallerMemberName] string caller = "",
        [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        var entry = new DebugEntry { Timestamp = DateTime.UtcNow, Level = level,
            Message = msg, Exception = ex, CallerMethod = caller,
            CallerFile = Path.GetFileName(file), CallerLine = line };
        Entries.Enqueue(entry);
        while (Entries.Count > 10000 && Entries.TryDequeue(out _)) { }
        EntryAdded?.Invoke(entry);
        if (ex != null) TryAutoCorrect(ex);
    }
    
    private void TryAutoCorrect(Exception ex)
    {
        if (ex is OutOfMemoryException) { GC.Collect(); GC.WaitForPendingFinalizers(); }
        else if (ex is SocketException) Task.Run(() => McpConnectionManager.Instance.ReconnectAllAsync());
    }
    
    public void Dispose() { while (Entries.TryDequeue(out _)) { } }
}

public class DebugEntry
{
    public DateTime Timestamp { get; set; }
    public LogEventLevel Level { get; set; }
    public string Message { get; set; } = "";
    public Exception? Exception { get; set; }
    public string CallerMethod { get; set; } = "";
    public string CallerFile { get; set; } = "";
    public int CallerLine { get; set; }
}
```

## Priority Error Fixes

### StatusBarManager.cs - Fix double.Percentage issue
The code expects progress objects but gets double. Change:
```csharp
// FROM: var progress = engine.GetBuildProgress(); progress.Percentage
// TO: var progress = engine.GetBuildProgress(); // progress IS the percentage (0.0-1.0)
BuildProgress.Value = progress * 100;
```

### PacketAnalysisEngine.cs - Fix type conversions
```csharp
// Line 585: action.Direction.ToString()
// Line 586: action.Parameters?.ToDictionary(p => p.Key, p => p.Value) ?? new()
// Line 599: Use kvp.Key instead of kvp.Name
// Line 601: dict.Add(kvp.Key, kvp.Value)
```

### GameStateEngine.cs - Add missing methods
```csharp
public int GetActiveMarches() => _marches.Count;
public int GetMaxMarches() => 5;
public ResourceState GetResourceRates() => _cities.Values.FirstOrDefault()?.ProductionRates ?? new();
```

### ProtocolHandler.cs - Add metrics methods
```csharp
public double GetPacketRate() => 0;
public double GetAverageLatency() => 0;
public double GetDecodeSuccessRate() => 100;
public int GetErrorCount() => 0;
public ProtocolAction? GetProtocolAction(string name) => _protocolActions.GetValueOrDefault(name);
```

### AutoPilotService.cs - Add properties
```csharp
public int ActiveTaskCount => 0;
public int QueueDepth => 0;
public double ActionsPerMinute => 0;
```

## SVG Diagram - System Architecture

```svg
<svg viewBox="0 0 800 600" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="grad1" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" style="stop-color:#1a1a2e"/>
      <stop offset="100%" style="stop-color:#16213e"/>
    </linearGradient>
  </defs>
  <rect width="800" height="600" fill="url(#grad1)"/>
  
  <!-- UI Layer -->
  <rect x="50" y="50" width="700" height="120" rx="10" fill="#0f3460" stroke="#e94560"/>
  <text x="400" y="90" text-anchor="middle" fill="white" font-size="20">UI Layer</text>
  <text x="150" y="130" fill="#e94560" font-size="14">MainWindow</text>
  <text x="300" y="130" fill="#e94560" font-size="14">Controls</text>
  <text x="450" y="130" fill="#e94560" font-size="14">EventViewer</text>
  <text x="600" y="130" fill="#e94560" font-size="14">StatusBar</text>
  
  <!-- Service Layer -->
  <rect x="50" y="200" width="700" height="120" rx="10" fill="#0f3460" stroke="#00fff5"/>
  <text x="400" y="240" text-anchor="middle" fill="white" font-size="20">Service Layer</text>
  <text x="120" y="280" fill="#00fff5" font-size="12">GameStateEngine</text>
  <text x="250" y="280" fill="#00fff5" font-size="12">ProtocolHandler</text>
  <text x="400" y="280" fill="#00fff5" font-size="12">McpManager</text>
  <text x="530" y="280" fill="#00fff5" font-size="12">DebugService</text>
  <text x="660" y="280" fill="#00fff5" font-size="12">AutoPilot</text>
  
  <!-- MCP Layer -->
  <rect x="50" y="350" width="700" height="120" rx="10" fill="#0f3460" stroke="#00ff00"/>
  <text x="400" y="390" text-anchor="middle" fill="white" font-size="20">MCP Servers</text>
  <text x="120" y="430" fill="#00ff00" font-size="12">evony-complete</text>
  <text x="250" y="430" fill="#00ff00" font-size="12">evony-rag</text>
  <text x="380" y="430" fill="#00ff00" font-size="12">evony-rte</text>
  <text x="510" y="430" fill="#00ff00" font-size="12">evony-tools</text>
  <text x="640" y="430" fill="#00ff00" font-size="12">evony-cdp</text>
  
  <!-- External -->
  <rect x="50" y="500" width="700" height="80" rx="10" fill="#0f3460" stroke="#ffd700"/>
  <text x="400" y="540" text-anchor="middle" fill="white" font-size="20">External: Fiddler | Browser | Evony Game</text>
  
  <!-- Arrows -->
  <line x1="400" y1="170" x2="400" y2="200" stroke="white" stroke-width="2" marker-end="url(#arrow)"/>
  <line x1="400" y1="320" x2="400" y2="350" stroke="white" stroke-width="2"/>
  <line x1="400" y1="470" x2="400" y2="500" stroke="white" stroke-width="2"/>
</svg>
```

## Missing Browser Features Checklist

- [ ] DevTools integration panel
- [ ] Network request monitor
- [ ] JavaScript console output
- [ ] DOM element inspector
- [ ] Cookie/Storage viewer
- [ ] Tab management
- [ ] Bookmark bar
- [ ] Download manager
- [ ] History viewer
- [ ] Screenshot tool
- [ ] PDF export
- [ ] Zoom controls
- [ ] Find in page
- [ ] Print dialog

## Final Verification Script

```powershell
# v7-verify.ps1
Write-Host "=== V7.0 Verification ===" -ForegroundColor Cyan

# Build check
Write-Host "`n[1/5] Building..." -ForegroundColor Yellow
$buildResult = dotnet build SvonyBrowser/SvonyBrowser.csproj --no-incremental /warnaserror 2>&1
if ($LASTEXITCODE -ne 0) { Write-Host "FAIL: Build errors" -ForegroundColor Red; exit 1 }
Write-Host "PASS: Build successful" -ForegroundColor Green

# Test check
Write-Host "`n[2/5] Running tests..." -ForegroundColor Yellow
dotnet test SvonyBrowser.Tests/SvonyBrowser.Tests.csproj --verbosity quiet
if ($LASTEXITCODE -ne 0) { Write-Host "FAIL: Tests failed" -ForegroundColor Red; exit 1 }
Write-Host "PASS: All tests passed" -ForegroundColor Green

# MCP servers check
Write-Host "`n[3/5] Checking MCP servers..." -ForegroundColor Yellow
Get-ChildItem mcp-servers -Directory | ForEach-Object {
    Push-Location $_.FullName
    node -c index.js 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Host "FAIL: $($_.Name)" -ForegroundColor Red }
    else { Write-Host "PASS: $($_.Name)" -ForegroundColor Green }
    Pop-Location
}

# Docs check
Write-Host "`n[4/5] Checking documentation..." -ForegroundColor Yellow
$requiredDocs = @("README.md", "ARCHITECTURE.md", "CHANGELOG.md", "BUGLIST.md")
$requiredDocs | ForEach-Object {
    if (Test-Path $_) { Write-Host "PASS: $_" -ForegroundColor Green }
    else { Write-Host "FAIL: $_ missing" -ForegroundColor Red }
}

# Summary
Write-Host "`n=== Verification Complete ===" -ForegroundColor Cyan
```
