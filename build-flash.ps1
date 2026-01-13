# Svony Browser Flash Build Script
# Builds the .NET Framework 4.6.2 version with CefSharp 84 (Flash support)

param(
    [string]$Configuration = "Release",
    [string]$CefSourcePath = "",
    [switch]$SkipCefCopy
)

$ErrorActionPreference = "Stop"

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Svony Browser Flash Build Script" -ForegroundColor Cyan
Write-Host "  .NET Framework 4.6.2 + CefSharp 84.4.10 (Flash Support)" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Paths
$ProjectRoot = $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "SvonyBrowser\SvonyBrowser.Flash.csproj"
$OutputPath = Join-Path $ProjectRoot "artifacts\flash-release"
$PackagesPath = Join-Path $ProjectRoot "packages"

# Check for MSBuild
$MSBuildPaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
)

$MSBuild = $null
foreach ($path in $MSBuildPaths) {
    if (Test-Path $path) {
        $MSBuild = $path
        break
    }
}

if (-not $MSBuild) {
    Write-Host "ERROR: MSBuild not found. Please install Visual Studio 2019 or 2022." -ForegroundColor Red
    exit 1
}

Write-Host "Using MSBuild: $MSBuild" -ForegroundColor Green

# Check for NuGet
$NuGet = Join-Path $ProjectRoot "nuget.exe"
if (-not (Test-Path $NuGet)) {
    Write-Host "Downloading NuGet..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $NuGet
}

# Restore NuGet packages
Write-Host ""
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
& $NuGet restore $ProjectFile -PackagesDirectory $PackagesPath
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: NuGet restore failed" -ForegroundColor Red
    exit 1
}

# Build the project
Write-Host ""
Write-Host "Building SvonyBrowser.Flash.csproj..." -ForegroundColor Yellow
& $MSBuild $ProjectFile /p:Configuration=$Configuration /p:Platform=x64 /t:Rebuild /v:minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Build successful!" -ForegroundColor Green

# Create output directory
$BuildOutput = Join-Path $ProjectRoot "SvonyBrowser\bin\x64\$Configuration"
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

# Copy build output
Write-Host ""
Write-Host "Copying build output to $OutputPath..." -ForegroundColor Yellow
Copy-Item -Path "$BuildOutput\*" -Destination $OutputPath -Recurse -Force

# Copy CefSharp 84 runtime files if source path provided
if (-not $SkipCefCopy -and $CefSourcePath) {
    Write-Host ""
    Write-Host "Copying CefSharp 84 runtime from $CefSourcePath..." -ForegroundColor Yellow
    
    $CefFiles = @(
        "libcef.dll",
        "CefSharp.dll",
        "CefSharp.Core.dll",
        "CefSharp.Wpf.dll",
        "CefSharp.BrowserSubprocess.exe",
        "CefSharp.BrowserSubprocess.Core.dll",
        "chrome_elf.dll",
        "d3dcompiler_47.dll",
        "icudtl.dat",
        "libEGL.dll",
        "libGLESv2.dll",
        "natives_blob.bin",
        "snapshot_blob.bin",
        "v8_context_snapshot.bin",
        "cef.pak",
        "cef_100_percent.pak",
        "cef_200_percent.pak",
        "cef_extensions.pak",
        "devtools_resources.pak"
    )
    
    foreach ($file in $CefFiles) {
        $sourcePath = Join-Path $CefSourcePath $file
        if (Test-Path $sourcePath) {
            Copy-Item -Path $sourcePath -Destination $OutputPath -Force
            Write-Host "  Copied: $file" -ForegroundColor Gray
        }
    }
    
    # Copy locales folder
    $localesSource = Join-Path $CefSourcePath "locales"
    if (Test-Path $localesSource) {
        $localesDest = Join-Path $OutputPath "locales"
        if (-not (Test-Path $localesDest)) {
            New-Item -ItemType Directory -Path $localesDest -Force | Out-Null
        }
        Copy-Item -Path "$localesSource\*" -Destination $localesDest -Force
        Write-Host "  Copied: locales folder" -ForegroundColor Gray
    }
    
    # Copy swiftshader folder
    $swiftshaderSource = Join-Path $CefSourcePath "swiftshader"
    if (Test-Path $swiftshaderSource) {
        $swiftshaderDest = Join-Path $OutputPath "swiftshader"
        if (-not (Test-Path $swiftshaderDest)) {
            New-Item -ItemType Directory -Path $swiftshaderDest -Force | Out-Null
        }
        Copy-Item -Path "$swiftshaderSource\*" -Destination $swiftshaderDest -Force
        Write-Host "  Copied: swiftshader folder" -ForegroundColor Gray
    }
}

# Copy Flash plugin
$FlashPluginPaths = @(
    "C:\Users\Admin\Downloads\SvonyBrowser-v7.0.5-source\Release\Assets\Plugins\pepflashplayer.dll",
    "$ProjectRoot\Assets\Plugins\pepflashplayer.dll",
    "$ProjectRoot\pepflashplayer.dll"
)

$PluginsDir = Join-Path $OutputPath "Plugins"
if (-not (Test-Path $PluginsDir)) {
    New-Item -ItemType Directory -Path $PluginsDir -Force | Out-Null
}

$FlashFound = $false
foreach ($flashPath in $FlashPluginPaths) {
    if (Test-Path $flashPath) {
        Copy-Item -Path $flashPath -Destination $PluginsDir -Force
        Write-Host ""
        Write-Host "Flash plugin copied from: $flashPath" -ForegroundColor Green
        $FlashFound = $true
        break
    }
}

if (-not $FlashFound) {
    Write-Host ""
    Write-Host "WARNING: pepflashplayer.dll not found!" -ForegroundColor Yellow
    Write-Host "Flash SWF files will NOT load without this file." -ForegroundColor Yellow
    Write-Host "Please copy pepflashplayer.dll to: $PluginsDir" -ForegroundColor Yellow
}

# Copy SWF files if available
$SwfPaths = @(
    "AutoEvony.swf",
    "EvonyClient1921.swf"
)

foreach ($swf in $SwfPaths) {
    $swfSource = Join-Path $ProjectRoot $swf
    if (Test-Path $swfSource) {
        Copy-Item -Path $swfSource -Destination $OutputPath -Force
        Write-Host "SWF file copied: $swf" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output: $OutputPath" -ForegroundColor White
Write-Host ""
Write-Host "To run:" -ForegroundColor Yellow
Write-Host "  1. Ensure pepflashplayer.dll is in the Plugins folder" -ForegroundColor White
Write-Host "  2. Copy CefSharp 84 runtime files if not already present" -ForegroundColor White
Write-Host "  3. Run SvonyBrowser.exe" -ForegroundColor White
Write-Host ""

# Usage example
Write-Host "Example with CefSharp 84 runtime copy:" -ForegroundColor Gray
Write-Host '  .\build-flash.ps1 -CefSourcePath "C:\path\to\cef84\runtime"' -ForegroundColor Gray
