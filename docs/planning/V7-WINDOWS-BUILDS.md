# 🪟 V7.0 WINDOWS BUILD GUIDE - EXACT COMMANDS

**NO AMBIGUITY - EXACT COMMANDS FOR EVERY SCENARIO**

---

## 🔴 THE ONE BUILD COMMAND THAT WORKS

```powershell
# THIS IS THE ONLY COMMAND THAT WILL BUILD SUCCESSFULLY:
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    "D:\Fiddler-FlashBrowser\SvonyBrowser\SvonyBrowser.csproj" `
    /t:Restore,Build `
    /p:Configuration=Release `
    /p:Platform=x64 `
    /p:RestorePackagesConfig=true `
    /p:SolutionDir="D:\Fiddler-FlashBrowser\" `
    /v:detailed
```

---

## 🛠️ VISUAL STUDIO 2022 VERSIONS

### Community Edition (FREE)
```
Download: https://c2rsetup.officeapps.live.com/c2r/downloadVS.aspx?sku=community&channel=Release&version=VS2022
Version Required: 17.8.3 or newer
Size: ~3GB download, ~20GB installed
```

### Professional Edition
```
Download: https://c2rsetup.officeapps.live.com/c2r/downloadVS.aspx?sku=professional&channel=Release&version=VS2022
License Required: Yes
```

### Enterprise Edition  
```
Download: https://c2rsetup.officeapps.live.com/c2r/downloadVS.aspx?sku=enterprise&channel=Release&version=VS2022
License Required: Yes
```

---

## 📦 REQUIRED VISUAL STUDIO WORKLOADS

```xml
<!-- .vsconfig file - save and import during VS install -->
{
  "version": "1.0",
  "components": [
    "Microsoft.VisualStudio.Component.CoreEditor",
    "Microsoft.VisualStudio.Workload.CoreEditor",
    "Microsoft.NetCore.Component.SDK",
    "Microsoft.NetCore.Component.DevelopmentTools",
    "Microsoft.Net.Component.4.8.SDK",
    "Microsoft.Net.Component.4.7.2.TargetingPack",
    "Microsoft.VisualStudio.Component.NuGet",
    "Microsoft.VisualStudio.Component.Roslyn.Compiler",
    "Microsoft.VisualStudio.Component.Roslyn.LanguageServices",
    "Microsoft.VisualStudio.Component.FSharp",
    "Microsoft.VisualStudio.Component.DiagnosticTools",
    "Microsoft.VisualStudio.Component.EntityFramework",
    "Microsoft.VisualStudio.Component.Debugger.JustInTime",
    "Microsoft.VisualStudio.Component.IntelliCode",
    "Microsoft.VisualStudio.Workload.NetCrossPlat",
    "Microsoft.VisualStudio.Workload.ManagedDesktop",
    "Microsoft.VisualStudio.Component.ManagedDesktop.Prerequisites",
    "Microsoft.ComponentGroup.Blend",
    "Microsoft.VisualStudio.ComponentGroup.MSIX.Packaging",
    "Microsoft.VisualStudio.Component.DotNetModelBuilder",
    "Microsoft.VisualStudio.Component.MSXML",
    "Microsoft.Component.NetFX.Native",
    "Microsoft.VisualStudio.Component.Graphics.Tools",
    "Microsoft.VisualStudio.Component.VC.CoreIde",
    "Microsoft.VisualStudio.Component.VC.Redist.14.Latest"
  ]
}
```

### Manual Selection During Install
```
✅ .NET desktop development
   ✅ .NET 8.0 Runtime
   ✅ .NET 6.0 Runtime (Long Term Support)
   ✅ .NET Framework 4.8 development tools
   ✅ C# and Visual Basic Roslyn compilers
   ✅ MSBuild
   ✅ .NET profiling tools
   ✅ Just-In-Time debugger
   ✅ C# and Visual Basic
   ✅ IntelliCode
   ✅ Live Share

✅ Desktop development with C++
   ✅ MSVC v143 - VS 2022 C++ x64/x86 build tools
   ✅ Windows 11 SDK (10.0.22621.0)
   ✅ Windows 10 SDK (10.0.19041.0)  
   ✅ C++ core features
   ✅ C++ Redistributable Update

✅ Individual components
   ✅ .NET 6.0 Runtime (Long Term Support)
   ✅ .NET 8.0 Runtime
   ✅ NuGet package manager
   ✅ NuGet targets and build tasks
   ✅ Text Template Transformation
```

---

## 🔨 MSBUILD PATHS BY EDITION

### Find Your MSBuild.exe
```powershell
# Community Edition
$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

# Professional Edition  
$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"

# Enterprise Edition
$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"

# Build Tools Only
$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"

# Find MSBuild automatically
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -products * -requires Microsoft.Component.MSBuild `
    -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
```

---

## 🎯 BUILD COMMAND FOR EACH SCENARIO

### 1. DEBUG BUILD (Development)
```powershell
# Full debug build with symbols
& $msbuild "D:\Fiddler-FlashBrowser\SvonyBrowser\SvonyBrowser.csproj" `
    /t:Clean,Restore,Build `
    /p:Configuration=Debug `
    /p:Platform=x64 `
    /p:DebugType=full `
    /p:DebugSymbols=true `
    /p:Optimize=false `
    /v:normal
```

### 2. RELEASE BUILD (Production)
```powershell
# Optimized release build
& $msbuild "D:\Fiddler-FlashBrowser\SvonyBrowser\SvonyBrowser.csproj" `
    /t:Clean,Restore,Build `
    /p:Configuration=Release `
    /p:Platform=x64 `
    /p:DebugType=none `
    /p:DebugSymbols=false `
    /p:Optimize=true `
    /v:minimal
```

### 3. PORTABLE BUILD (Single EXE)
```powershell
# Self-contained single file
& $msbuild "D:\Fiddler-FlashBrowser\SvonyBrowser\SvonyBrowser.csproj" `
    /t:Publish `
    /p:Configuration=Release `
    /p:Platform=x64 `
    /p:PublishSingleFile=true `
    /p:SelfContained=true `
    /p:RuntimeIdentifier=win-x64 `
    /p:PublishTrimmed=false `
    /p:PublishReadyToRun=true `
    /v:normal
```

### 4. INSTALLER BUILD (For Setup.exe)
```powershell
# Build for installer packaging
& $msbuild "D:\Fiddler-FlashBrowser\SvonyBrowser\SvonyBrowser.csproj" `
    /t:Clean,Restore,Build,Publish `
    /p:Configuration=Release `
    /p:Platform=x64 `
    /p:PublishProfile=FolderProfile `
    /p:PublishDir="D:\Fiddler-FlashBrowser\Installer\Files\" `
    /p:UseAppHost=true `
    /v:normal
```

### 5. CI/CD BUILD (GitHub Actions)
```powershell
# Automated build for CI/CD
& $msbuild "D:\Fiddler-FlashBrowser\SvonyBrowser\SvonyBrowser.csproj" `
    /t:Restore,Build,Pack `
    /p:Configuration=Release `
    /p:Platform=x64 `
    /p:ContinuousIntegrationBuild=true `
    /p:Deterministic=true `
    /p:Version=7.0.0 `
    /p:FileVersion=7.0.0.0 `
    /p:AssemblyVersion=7.0.0.0 `
    /v:normal `
    /bl:build.binlog
```

### 6. XAML VALIDATION BUILD
```powershell
# Validate XAML without full build
& $msbuild "D:\Fiddler-FlashBrowser\SvonyBrowser\SvonyBrowser.csproj" `
    /t:ValidateXaml,MarkupCompilePass1,MarkupCompilePass2 `
    /p:Configuration=Debug `
    /p:Platform=x64 `
    /v:detailed
```

---

## 🔧 FIX CEFSHARP XAML ERROR

### The Error
```
MainWindow.xaml(278,56): error MC1000: Unknown build error, 
'Could not find assembly 'CefSharp, Version=119.4.30.0'
```

### Solution 1: Add Assembly Reference Paths
```xml
<!-- Add to SvonyBrowser.csproj -->
<PropertyGroup>
  <ReferencePath>$(NuGetPackageRoot)cefsharp.common\119.4.30\lib\net462\;$(NuGetPackageRoot)cefsharp.wpf\119.4.30\lib\net462\</ReferencePath>
  <AssemblySearchPaths>$(AssemblySearchPaths);$(ReferencePath)</AssemblySearchPaths>
</PropertyGroup>
```

### Solution 2: Force Assembly Resolution
```powershell
# Build with explicit assembly paths
& $msbuild "D:\Fiddler-FlashBrowser\SvonyBrowser\SvonyBrowser.csproj" `
    /t:Restore,Build `
    /p:Configuration=Release `
    /p:Platform=x64 `
    /p:ReferencePath="C:\Users\%USERNAME%\.nuget\packages\cefsharp.common\119.4.30\lib\net462\;C:\Users\%USERNAME%\.nuget\packages\cefsharp.wpf\119.4.30\lib\net462\" `
    /v:detailed
```

### Solution 3: Pre-copy CefSharp DLLs
```powershell
# Copy CefSharp to output before build
$packages = "$env:USERPROFILE\.nuget\packages"
$output = "D:\Fiddler-FlashBrowser\SvonyBrowser\bin\Debug\net6.0-windows"

New-Item -ItemType Directory -Force -Path $output
Copy-Item "$packages\cefsharp.common\119.4.30\lib\net462\*.dll" -Destination $output -Force
Copy-Item "$packages\cefsharp.wpf\119.4.30\lib\net462\*.dll" -Destination $output -Force

# Then build
& $msbuild "D:\Fiddler-FlashBrowser\SvonyBrowser\SvonyBrowser.csproj" /t:Build
```

---

## 🖥️ DEVELOPER COMMAND PROMPT BUILDS

### VS2022 Developer Command Prompt
```batch
:: Open Developer Command Prompt
"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"

:: Navigate to project
cd /d D:\Fiddler-FlashBrowser

:: Build with msbuild
msbuild SvonyBrowser\SvonyBrowser.csproj /t:Restore,Build /p:Configuration=Release /p:Platform=x64

:: Or use dotnet with MSBuild
dotnet msbuild SvonyBrowser\SvonyBrowser.csproj /t:Restore,Build /p:Configuration=Release
```

### VS2022 Developer PowerShell
```powershell
# Open Developer PowerShell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\Launch-VsDevShell.ps1"

# Build
msbuild SvonyBrowser\SvonyBrowser.csproj `
    /t:Restore,Build `
    /p:Configuration=Release `
    /p:Platform=x64
```

---

## 📦 OUTPUT LOCATIONS

### Debug Build Output
```
D:\Fiddler-FlashBrowser\SvonyBrowser\bin\Debug\net6.0-windows\
├── SvonyBrowser.exe (200KB)
├── SvonyBrowser.dll (500KB)
├── SvonyBrowser.pdb (200KB)
├── CefSharp.dll (1MB)
├── CefSharp.Core.dll (2MB)
├── CefSharp.Wpf.dll (200KB)
├── libcef.dll (140MB)
├── chrome_elf.dll (1MB)
├── d3dcompiler_47.dll (4MB)
├── libEGL.dll (100KB)
├── libGLESv2.dll (7MB)
├── locales\ (folder with 50+ .pak files)
├── swiftshader\ (folder)
└── ... (100+ other files)
```

### Release Build Output
```
D:\Fiddler-FlashBrowser\SvonyBrowser\bin\Release\net6.0-windows\
├── SvonyBrowser.exe (200KB - optimized)
├── SvonyBrowser.dll (400KB - optimized)
├── [NO .pdb files in release]
└── ... (same DLLs as debug)
```

### Publish Output (Single File)
```
D:\Fiddler-FlashBrowser\SvonyBrowser\bin\Release\net6.0-windows\publish\
├── SvonyBrowser.exe (450MB - everything embedded)
├── libcef.dll (140MB - cannot be embedded)
├── chrome_elf.dll (1MB - cannot be embedded)
└── locales\ (required external)
```

---

## ✅ BUILD VERIFICATION

### Verify Successful Build
```powershell
# Check exit code
if ($LASTEXITCODE -eq 0) {
    Write-Host "BUILD SUCCEEDED" -ForegroundColor Green
} else {
    Write-Host "BUILD FAILED with code $LASTEXITCODE" -ForegroundColor Red
}

# Check output files exist
$outputDir = "D:\Fiddler-FlashBrowser\SvonyBrowser\bin\Release\net6.0-windows"
$required = @(
    "$outputDir\SvonyBrowser.exe",
    "$outputDir\CefSharp.dll",
    "$outputDir\libcef.dll"
)

foreach ($file in $required) {
    if (Test-Path $file) {
        Write-Host "✓ $(Split-Path $file -Leaf) found" -ForegroundColor Green
    } else {
        Write-Host "✗ $(Split-Path $file -Leaf) MISSING" -ForegroundColor Red
    }
}
```

---

## 🚀 COMPLETE BUILD SCRIPT

```powershell
# Save as: build-v7.ps1
param(
    [ValidateSet("Debug", "Release", "Publish")]
    [string]$Configuration = "Release",
    
    [ValidateSet("x64", "x86", "AnyCPU")]
    [string]$Platform = "x64",
    
    [switch]$Clean,
    [switch]$Test,
    [switch]$Package
)

# Find MSBuild
$vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vsWhere -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1

if (!$msbuild -or !(Test-Path $msbuild)) {
    Write-Error "MSBuild not found. Install Visual Studio 2022."
    exit 1
}

$projectPath = "D:\Fiddler-FlashBrowser\SvonyBrowser\SvonyBrowser.csproj"

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning..." -ForegroundColor Yellow
    & $msbuild $projectPath /t:Clean /v:minimal
}

# Restore packages
Write-Host "Restoring packages..." -ForegroundColor Yellow
& $msbuild $projectPath /t:Restore /v:minimal

# Build
Write-Host "Building $Configuration|$Platform..." -ForegroundColor Yellow
& $msbuild $projectPath `
    /t:Build `
    /p:Configuration=$Configuration `
    /p:Platform=$Platform `
    /v:normal

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit $LASTEXITCODE
}

# Test if requested
if ($Test) {
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test ..\SvonyBrowser.Tests\SvonyBrowser.Tests.csproj
}

# Package if requested
if ($Package) {
    Write-Host "Creating package..." -ForegroundColor Yellow
    & $msbuild $projectPath /t:Publish /p:Configuration=$Configuration
}

Write-Host "BUILD COMPLETE!" -ForegroundColor Green
```

---

## 🔴 TROUBLESHOOTING

### Error: "The imported project was not found"
```powershell
# Fix: Install .NET desktop workload
dotnet workload install microsoft-net-sdk-desktop
```

### Error: "Could not load file or assembly"
```powershell
# Fix: Clear and restore
Remove-Item -Recurse -Force "$env:USERPROFILE\.nuget\packages\cefsharp*"
dotnet restore --force
```

### Error: "Access denied"
```powershell
# Fix: Run as Administrator
Start-Process powershell -Verb RunAs
```

### Error: "The target process exited without raising CoreCLR started event"
```powershell
# Fix: Install Visual C++ Redistributables
winget install Microsoft.VCRedist.2015+.x64
```

---

**USE THIS DOCUMENT FOR EXACT WINDOWS BUILD COMMANDS**
