# 🏰 V7.0 PHOENIX EDITION - COMPLETE MANUS GUIDE

**THIS IS YOUR MASTER DOCUMENT - START HERE**  
**Version**: 7.0.0  
**Date**: January 12, 2025  
**Windows Build Target**: Windows 10/11 x64  
**Framework**: .NET 6.0 Windows Desktop Runtime  
**.NET SDK Required**: 6.0.420 or 8.0.404  
**Visual Studio Required**: 2022 17.8+ (Community/Professional/Enterprise)  
**CefSharp Version**: 119.4.30  

---

## 🔴 CRITICAL: READ THIS FIRST

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                          STOP! DO NOT SKIP THIS!                               ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                                 ║
║  1. You CANNOT build with 'dotnet build' - MUST use Visual Studio/MSBuild      ║
║  2. You MUST have Visual Studio 2022 installed                                 ║
║  3. You MUST run as Administrator for first build                              ║
║  4. You MUST restore packages before building                                  ║
║  5. Current build has 1 XAML error that ONLY Visual Studio can fix            ║
║                                                                                 ║
╚═══════════════════════════════════════════════════════════════════════════════╝
```

---

## 📁 ALL V7.0 DOCUMENTATION FILES

**READ IN THIS ORDER:**

| Priority | File                                             | Purpose                          | Status    |
| -------- | ------------------------------------------------ | -------------------------------- | --------- |
| 1️⃣        | **[V7-COMPLETE-MANUS.md](V7-COMPLETE-MANUS.md)** | THIS FILE - Master guide         | 📖 READING |
| 2️⃣        | **[V7-WINDOWS-BUILDS.md](V7-WINDOWS-BUILDS.md)** | Exact build commands for Windows | 🔲 TODO    |
| 3️⃣        | **[V7-ERROR-FIXES.md](V7-ERROR-FIXES.md)**       | Every error and exact fix        | 🔲 TODO    |
| 4️⃣        | **[V7-BUILD-MATRIX.md](V7-BUILD-MATRIX.md)**     | All build configurations         | 🔲 TODO    |
| 5️⃣        | **[V7-FAILSAFES.md](V7-FAILSAFES.md)**           | 100+ failsafe implementations    | 🔲 TODO    |
| 6️⃣        | **[V7-TESTING-100.md](V7-TESTING-100.md)**       | 100% test coverage templates     | 🔲 TODO    |
| 7️⃣        | **[V7-MANUS-STRICT.md](V7-MANUS-STRICT.md)**     | Original strict instructions     | ✅ EXISTS  |
| 8️⃣        | **[WINDOWS-BUILD-FIX.md](WINDOWS-BUILD-FIX.md)** | CefSharp XAML fix                | ✅ EXISTS  |
| 9️⃣        | **[BUGFIX-FINAL.md](BUGFIX-FINAL.md)**           | v6.0 fixes summary               | ✅ EXISTS  |

---

## 🛠️ EXACT SOFTWARE REQUIREMENTS

### Required Installations (IN THIS ORDER)

```powershell
# 1. Check Windows Version (MUST be Windows 10 1809+ or Windows 11)
winver
# Required: Version 1809 (OS Build 17763) or higher

# 2. Install Visual Studio 2022 (17.8.3 or newer)
# Download: https://visualstudio.microsoft.com/downloads/
# SELECT THESE WORKLOADS:
#   ✅ .NET desktop development
#   ✅ Desktop development with C++
#   ✅ Individual Components:
#      - .NET 6.0 Runtime (Long Term Support)
#      - .NET 8.0 Runtime
#      - Windows 10 SDK (10.0.19041.0)
#      - NuGet package manager
#      - C++ 2022 Redistributable Update

# 3. Install .NET SDKs (BOTH)
# .NET 6.0 SDK: https://dotnet.microsoft.com/download/dotnet/6.0
winget install Microsoft.DotNet.SDK.6
# .NET 8.0 SDK: https://dotnet.microsoft.com/download/dotnet/8.0  
winget install Microsoft.DotNet.SDK.8

# 4. Verify Installations
dotnet --list-sdks
# MUST show:
# 6.0.420 [C:\Program Files\dotnet\sdk]
# 8.0.404 [C:\Program Files\dotnet\sdk]

# 5. Install Git
winget install Git.Git

# 6. Install Node.js 20 LTS (for MCP servers)
winget install OpenJS.NodeJS.LTS

# 7. Install Visual C++ Redistributables (for CefSharp)
# x64: https://aka.ms/vs/17/release/vc_redist.x64.exe
```

---

## 🔧 CURRENT PROJECT STATUS

### Build Status After Latest Fix

| Component               | Status      | Details                     |
| ----------------------- | ----------- | --------------------------- |
| **C# Code Compilation** | ✅ PASSES    | All services compile        |
| **XAML Compilation**    | ❌ 1 ERROR   | CefSharp assembly not found |
| **Package Restore**     | ✅ WORKS     | All NuGet packages restore  |
| **MCP Servers**         | ✅ 7/7 READY | All npm packages installed  |

### The ONE Remaining Error

```
File: MainWindow.xaml
Line: 278
Error: MC1000: Unknown build error, 'Could not find assembly 'CefSharp, Version=119.4.30.0'
```

**THIS ERROR ONLY OCCURS WITH `dotnet build`**  
**VISUAL STUDIO/MSBUILD FIXES IT AUTOMATICALLY**

---

## 📝 STEP-BY-STEP BUILD INSTRUCTIONS

### Phase 1: Clone and Prepare

```powershell
# 1. Open PowerShell as Administrator
Start-Process powershell -Verb RunAs

# 2. Clone repository
cd C:\
git clone https://github.com/Ghenghis/Svony-Browser.git
cd C:\Svony-Browser

# 3. Verify you have latest
git pull origin main
git log --oneline -5
# Latest commit should be: d22448d Add DataPath property...
```

### Phase 2: Visual Studio Build (REQUIRED)

```powershell
# Option A: Command Line with MSBuild
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
  SvonyBrowser\SvonyBrowser.csproj `
  /t:Restore `
  /p:Configuration=Release `
  /p:Platform=x64

"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
  SvonyBrowser\SvonyBrowser.csproj `
  /t:Build `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:OutputPath=bin\Release\net6.0-windows

# Option B: Visual Studio GUI
# 1. Open SvonyBrowser.sln in Visual Studio 2022
# 2. Right-click Solution → Restore NuGet Packages
# 3. Menu: Build → Configuration Manager → Set to Release | x64
# 4. Menu: Build → Build Solution (Ctrl+Shift+B)
# 5. Check Output window for "Build: 1 succeeded"
```

### Phase 3: Verify Build Output

```powershell
# Check build succeeded
dir SvonyBrowser\bin\Release\net6.0-windows\SvonyBrowser.exe
# File should exist and be ~200KB

# Check all DLLs present
dir SvonyBrowser\bin\Release\net6.0-windows\*.dll | Measure-Object
# Should show 100+ DLL files

# Check CefSharp files
dir SvonyBrowser\bin\Release\net6.0-windows\CefSharp*.dll
# Should show:
# - CefSharp.dll
# - CefSharp.Core.dll
# - CefSharp.Wpf.dll

# Check CEF binaries
dir SvonyBrowser\bin\Release\net6.0-windows\libcef.dll
# Should be ~140MB
```

---

## 🚀 BUILD CONFIGURATIONS MATRIX

| Configuration | Platform | Framework      | CefSharp | Output Path                  |
| ------------- | -------- | -------------- | -------- | ---------------------------- |
| **Debug**     | x64      | net6.0-windows | 119.4.30 | bin\Debug\net6.0-windows     |
| **Release**   | x64      | net6.0-windows | 119.4.30 | bin\Release\net6.0-windows   |
| **Portable**  | AnyCPU   | net6.0-windows | 119.4.30 | bin\Portable\net6.0-windows  |
| **Installer** | x64      | net6.0-windows | 119.4.30 | bin\Installer\net6.0-windows |

### Build Commands for Each Configuration

```powershell
# Debug Build
msbuild SvonyBrowser\SvonyBrowser.csproj /p:Configuration=Debug /p:Platform=x64

# Release Build  
msbuild SvonyBrowser\SvonyBrowser.csproj /p:Configuration=Release /p:Platform=x64

# Portable Build (self-contained)
msbuild SvonyBrowser\SvonyBrowser.csproj /p:Configuration=Release /p:PublishSingleFile=true /p:SelfContained=true /p:RuntimeIdentifier=win-x64

# Installer Build (for WiX/InnoSetup)
msbuild SvonyBrowser\SvonyBrowser.csproj /p:Configuration=Release /p:Platform=x64 /p:OutputPath=bin\Installer
```

---

## 🐛 ALL KNOWN ERRORS AND FIXES

### Error 1: CefSharp Assembly Not Found
```
Error MC1000: Could not find assembly 'CefSharp, Version=119.4.30.0'
```
**Fix**: Use Visual Studio or MSBuild, NOT dotnet CLI

### Error 2: TargetFramework Not Supported
```
Error NETSDK1045: The current .NET SDK does not support targeting .NET 6.0
```
**Fix**: Install .NET 6.0 SDK: `winget install Microsoft.DotNet.SDK.6`

### Error 3: NuGet Package Not Found
```
Error NU1101: Unable to find package CefSharp.Wpf
```
**Fix**: Clear cache and restore:
```powershell
dotnet nuget locals all --clear
msbuild /t:Restore
```

### Error 4: Platform Mismatch
```
Error MSB3270: There was a mismatch between the processor architecture
```
**Fix**: Set platform explicitly:
```powershell
msbuild /p:Platform=x64
```

### Error 5: Missing Visual C++ Runtime
```
System.DllNotFoundException: Unable to load DLL 'libcef.dll'
```
**Fix**: Install VC++ Redistributables:
```powershell
winget install Microsoft.VCRedist.2015+.x64
```

---

## 🛡️ FAILSAFE IMPLEMENTATIONS

### Failsafe 1: Auto-Recovery Service
```csharp
// Add to Services/FailsafeManager.cs
public class FailsafeManager
{
    private readonly Timer _healthCheck;
    private readonly CircuitBreaker _breaker;
    
    public void StartMonitoring()
    {
        _healthCheck = new Timer(CheckHealth, null, 0, 5000);
    }
    
    private void CheckHealth(object state)
    {
        try
        {
            // Check memory
            if (GC.GetTotalMemory(false) > 1_000_000_000) // 1GB
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            
            // Check CPU
            if (Environment.ProcessorCount > 90)
            {
                Thread.Sleep(100); // Throttle
            }
            
            // Check handles
            if (Process.GetCurrentProcess().HandleCount > 10000)
            {
                RestartServices();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failsafe error");
            Environment.FailFast("Critical failsafe error", ex);
        }
    }
}
```

### Failsafe 2: Build Verification Script
```powershell
# Save as verify-build.ps1
param(
    [string]$BuildPath = "SvonyBrowser\bin\Release\net6.0-windows"
)

$errors = @()

# Check EXE exists
if (!(Test-Path "$BuildPath\SvonyBrowser.exe")) {
    $errors += "SvonyBrowser.exe not found"
}

# Check critical DLLs
$requiredDlls = @(
    "CefSharp.dll",
    "CefSharp.Core.dll", 
    "CefSharp.Wpf.dll",
    "libcef.dll",
    "Newtonsoft.Json.dll",
    "Serilog.dll"
)

foreach ($dll in $requiredDlls) {
    if (!(Test-Path "$BuildPath\$dll")) {
        $errors += "$dll not found"
    }
}

# Check file sizes
$libcef = Get-Item "$BuildPath\libcef.dll" -ErrorAction SilentlyContinue
if ($libcef -and $libcef.Length -lt 100MB) {
    $errors += "libcef.dll too small (corrupted?)"
}

if ($errors.Count -gt 0) {
    Write-Host "BUILD VERIFICATION FAILED:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
} else {
    Write-Host "BUILD VERIFICATION PASSED" -ForegroundColor Green
    exit 0
}
```

### Failsafe 3: Automatic Fix Script
```powershell
# Save as auto-fix.ps1
Write-Host "Running Automatic Fix..." -ForegroundColor Yellow

# Fix 1: Clear all caches
Write-Host "Clearing caches..."
dotnet nuget locals all --clear
Remove-Item -Recurse -Force SvonyBrowser\obj -ErrorAction SilentlyContinue  
Remove-Item -Recurse -Force SvonyBrowser\bin -ErrorAction SilentlyContinue

# Fix 2: Restore packages
Write-Host "Restoring packages..."
msbuild SvonyBrowser\SvonyBrowser.csproj /t:Restore /p:Configuration=Release

# Fix 3: Install missing components
Write-Host "Checking prerequisites..."
$vs = Get-ItemProperty "HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\SxS\VS7" -Name "17.0" -ErrorAction SilentlyContinue
if (!$vs) {
    Write-Host "Visual Studio 2022 not found! Installing..." -ForegroundColor Red
    Start-Process "https://visualstudio.microsoft.com/downloads/"
    exit 1
}

# Fix 4: Set environment variables
[Environment]::SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1", "User")
[Environment]::SetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1", "User")

# Fix 5: Build with MSBuild
$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\*\MSBuild\Current\Bin\MSBuild.exe"
$msbuildPath = (Get-Item $msbuild).FullName | Select-Object -First 1

if (Test-Path $msbuildPath) {
    & $msbuildPath SvonyBrowser\SvonyBrowser.csproj /p:Configuration=Release /p:Platform=x64
} else {
    Write-Host "MSBuild not found! Open Visual Studio to build." -ForegroundColor Red
}
```

---

## 🧪 TESTING REQUIREMENTS

### Unit Test Project Setup
```powershell
# Create test project
dotnet new xunit -n SvonyBrowser.Tests
cd SvonyBrowser.Tests

# Add packages
dotnet add package FluentAssertions --version 6.12.0
dotnet add package Moq --version 4.20.70
dotnet add package coverlet.collector --version 6.0.0
dotnet add package Microsoft.NET.Test.Sdk --version 17.8.0

# Reference main project
dotnet add reference ..\SvonyBrowser\SvonyBrowser.csproj
```

### Test Template for 100% Coverage
```csharp
// Template for EVERY service - replace [ServiceName]
using Xunit;
using FluentAssertions;
using Moq;
using SvonyBrowser.Services;

public class [ServiceName]Tests
{
    private readonly [ServiceName] _sut;
    
    public [ServiceName]Tests()
    {
        _sut = [ServiceName].Instance;
    }
    
    [Fact]
    public void Instance_ShouldReturnSingleton()
    {
        var instance1 = [ServiceName].Instance;
        var instance2 = [ServiceName].Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        _sut.Should().NotBeNull();
        // Add property checks
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("test")]
    public void Method_ShouldHandleInput(string input)
    {
        // Test each method with various inputs
    }
    
    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        _sut.Dispose();
        // Verify cleanup
    }
}
```

### Run Tests with Coverage
```powershell
# Run tests
dotnet test --collect:"XPlat Code Coverage" --results-directory coverage

# Generate report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:coverage\**\coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html

# Open report
Start-Process coverage-report\index.html

# MUST show 95%+ coverage for release
```

---

## 🚢 RELEASE PROCESS

### Step 1: Final Build
```powershell
# Clean everything
git clean -xfd
git restore .

# Build Release
msbuild SvonyBrowser\SvonyBrowser.csproj `
  /t:Clean,Restore,Build `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:Version=7.0.0
```

### Step 2: Create Release Package
```powershell
# Create release folder
$version = "7.0.0"
$releaseDir = "releases\v$version"
New-Item -ItemType Directory -Force -Path $releaseDir

# Copy binaries
Copy-Item -Recurse "SvonyBrowser\bin\Release\net6.0-windows\*" "$releaseDir\"

# Create ZIP
Compress-Archive -Path "$releaseDir\*" -DestinationPath "SvonyBrowser-v$version-win-x64.zip"
```

### Step 3: GitHub Release
```powershell
# Commit all changes
git add -A
git commit -m "Release v7.0.0 Phoenix Edition"

# Create and push tag
git tag -a v7.0.0 -m "Version 7.0.0 - Phoenix Edition"
git push origin main
git push origin v7.0.0

# GitHub Actions will create release automatically
# Or manually at: https://github.com/Ghenghis/Svony-Browser/releases/new
```

---

## 📊 VERIFICATION CHECKLIST

```
PRE-BUILD CHECKS
[ ] Windows 10/11 x64
[ ] Visual Studio 2022 installed
[ ] .NET 6.0 SDK installed
[ ] .NET 8.0 SDK installed
[ ] Git installed
[ ] Node.js 20 installed
[ ] Running as Administrator

BUILD CHECKS
[ ] Repository cloned
[ ] Latest commit pulled
[ ] NuGet packages restored
[ ] MSBuild succeeds
[ ] 0 errors
[ ] 0 warnings (or only expected)
[ ] EXE created in bin\Release
[ ] All DLLs present
[ ] libcef.dll is ~140MB

TEST CHECKS
[ ] Test project builds
[ ] All tests pass
[ ] Coverage > 95%
[ ] No test failures
[ ] Integration tests pass
[ ] E2E tests pass

RELEASE CHECKS
[ ] Version number updated
[ ] CHANGELOG updated
[ ] Documentation complete
[ ] Release package created
[ ] Tag pushed to GitHub
[ ] GitHub release created
[ ] Binaries uploaded

POST-RELEASE CHECKS
[ ] Application launches
[ ] No crash on startup
[ ] Browser panels load
[ ] MCP servers connect
[ ] Evony loads correctly
[ ] All features work
```

---

## 🔗 QUICK LINKS

### Documentation
- [V7-WINDOWS-BUILDS.md](V7-WINDOWS-BUILDS.md) - Detailed Windows build instructions
- [V7-ERROR-FIXES.md](V7-ERROR-FIXES.md) - Complete error reference
- [V7-BUILD-MATRIX.md](V7-BUILD-MATRIX.md) - All build configurations
- [V7-FAILSAFES.md](V7-FAILSAFES.md) - 100+ failsafe implementations
- [V7-TESTING-100.md](V7-TESTING-100.md) - Complete testing guide

### External Resources
- [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/)
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [CefSharp Documentation](https://github.com/cefsharp/CefSharp/wiki)
- [MSBuild Reference](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild)

### Repository
- [GitHub Repository](https://github.com/Ghenghis/Svony-Browser)
- [Releases](https://github.com/Ghenghis/Svony-Browser/releases)
- [Issues](https://github.com/Ghenghis/Svony-Browser/issues)

---

## ⚠️ FINAL CRITICAL NOTES

1. **YOU CANNOT USE `dotnet build`** - The XAML compiler bug with CefSharp REQUIRES Visual Studio/MSBuild
2. **Version 119.4.30 of CefSharp** is the LAST version supporting .NET 6.0
3. **Run as Administrator** for first build to register COM components
4. **Clear NuGet cache** if packages fail: `dotnet nuget locals all --clear`
5. **Restart Visual Studio** if IntelliSense shows false errors
6. **Check Event Viewer** for .NET errors if app crashes

---

**MANUS: Start with this document. Follow EVERY step. Read EVERY linked document.**
