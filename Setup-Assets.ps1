# Setup-Assets.ps1
# Copies CefSharp DLLs from NuGet packages to Assets folder for proper SWF/Flash support
# This script follows the CefFlashBrowser asset structure pattern

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

# Paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $ScriptDir "SvonyBrowser"
$PackagesDir = Join-Path $ScriptDir "packages"
$AssetsDir = Join-Path $ScriptDir "release\Assets"
$CefSharpAssetsDir = Join-Path $AssetsDir "CefSharp"
$PluginsDir = Join-Path $AssetsDir "Plugins"
$SwfPlayerDir = Join-Path $AssetsDir "SwfPlayer"
$EmptyExeDir = Join-Path $AssetsDir "EmptyExe"

# NuGet package paths
$CefSharpCommonDir = Join-Path $PackagesDir "CefSharp.Common.84.4.10"
$CefSharpWpfDir = Join-Path $PackagesDir "CefSharp.Wpf.84.4.10"
$CefRedistX64Dir = Join-Path $PackagesDir "cef.redist.x64.84.4.1"
$CefRedistX86Dir = Join-Path $PackagesDir "cef.redist.x86.84.4.1"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SvonyBrowser Assets Setup Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration: $Configuration"
Write-Host "Platform: $Platform"
Write-Host "Assets Directory: $AssetsDir"
Write-Host ""

# Check if NuGet packages exist
if (-not (Test-Path $CefSharpCommonDir)) {
    Write-Host "ERROR: CefSharp.Common package not found at: $CefSharpCommonDir" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please run 'nuget restore' first to download the required packages." -ForegroundColor Yellow
    Write-Host "Or download CefSharp 84.4.10 manually from:"
    Write-Host "  https://www.nuget.org/packages/CefSharp.Common/84.4.10"
    exit 1
}

# Create directories if they don't exist
Write-Host "Creating asset directories..." -ForegroundColor Green
New-Item -ItemType Directory -Force -Path $CefSharpAssetsDir | Out-Null
New-Item -ItemType Directory -Force -Path $PluginsDir | Out-Null
New-Item -ItemType Directory -Force -Path $SwfPlayerDir | Out-Null
New-Item -ItemType Directory -Force -Path $EmptyExeDir | Out-Null

# Determine CEF resources path based on platform
# Native CEF files are in cef.redist package, not CefSharp.Common
if ($Platform -eq "x64") {
    $CefNativePath = Join-Path $CefRedistX64Dir "CEF"
} else {
    $CefNativePath = Join-Path $CefRedistX86Dir "CEF"
}

# Managed DLLs and subprocess are in CefSharp.Common
$CefSharpManagedPath = Join-Path $CefSharpCommonDir "CefSharp\$Platform"
if (-not (Test-Path $CefSharpManagedPath)) {
    $CefSharpManagedPath = Join-Path $CefSharpCommonDir "CefSharp\x64"
}

Write-Host "CEF Native Path: $CefNativePath"
Write-Host "CefSharp Managed Path: $CefSharpManagedPath"

# Copy CEF native files (libcef.dll, etc.)
Write-Host ""
Write-Host "Copying CefSharp native files..." -ForegroundColor Green

$NativeFiles = @(
    "libcef.dll",
    "chrome_elf.dll",
    "d3dcompiler_47.dll",
    "libEGL.dll",
    "libGLESv2.dll",
    "snapshot_blob.bin",
    "v8_context_snapshot.bin",
    "icudtl.dat",
    "cef.pak",
    "cef_100_percent.pak",
    "cef_200_percent.pak",
    "cef_extensions.pak",
    "devtools_resources.pak"
)

foreach ($file in $NativeFiles) {
    $sourcePath = Join-Path $CefNativePath $file
    if (Test-Path $sourcePath) {
        Write-Host "  Copying $file..."
        Copy-Item $sourcePath -Destination $CefSharpAssetsDir -Force
    } else {
        Write-Host "  WARNING: $file not found at $sourcePath" -ForegroundColor Yellow
    }
}

# Copy locales folder
$LocalesSource = Join-Path $CefNativePath "locales"
$LocalesDest = Join-Path $CefSharpAssetsDir "locales"
if (Test-Path $LocalesSource) {
    Write-Host "  Copying locales folder..."
    if (Test-Path $LocalesDest) {
        Remove-Item $LocalesDest -Recurse -Force
    }
    Copy-Item $LocalesSource -Destination $LocalesDest -Recurse -Force
}

# Copy swiftshader folder (software rendering fallback)
$SwiftshaderSource = Join-Path $CefNativePath "swiftshader"
$SwiftshaderDest = Join-Path $CefSharpAssetsDir "swiftshader"
if (Test-Path $SwiftshaderSource) {
    Write-Host "  Copying swiftshader folder..."
    if (Test-Path $SwiftshaderDest) {
        Remove-Item $SwiftshaderDest -Recurse -Force
    }
    Copy-Item $SwiftshaderSource -Destination $SwiftshaderDest -Recurse -Force
}

# Copy CefSharp managed DLLs (they're in CefSharp\x64, not lib\net462)
Write-Host ""
Write-Host "Copying CefSharp managed assemblies..." -ForegroundColor Green

$ManagedFiles = @(
    "CefSharp.dll",
    "CefSharp.Core.dll",
    "CefSharp.BrowserSubprocess.Core.dll"
)

foreach ($file in $ManagedFiles) {
    $sourcePath = Join-Path $CefSharpManagedPath $file
    if (Test-Path $sourcePath) {
        Write-Host "  Copying $file..."
        Copy-Item $sourcePath -Destination $CefSharpAssetsDir -Force
    } else {
        Write-Host "  WARNING: $file not found at $sourcePath" -ForegroundColor Yellow
    }
}

# Copy CefSharp.Wpf.dll
$WpfDllPath = Join-Path $CefSharpWpfDir "lib\net462\CefSharp.Wpf.dll"
if (Test-Path $WpfDllPath) {
    Write-Host "  Copying CefSharp.Wpf.dll..."
    Copy-Item $WpfDllPath -Destination $CefSharpAssetsDir -Force
}

# Copy BrowserSubprocess
$SubprocessPath = Join-Path $CefSharpManagedPath "CefSharp.BrowserSubprocess.exe"
if (Test-Path $SubprocessPath) {
    Write-Host "  Copying CefSharp.BrowserSubprocess.exe..."
    Copy-Item $SubprocessPath -Destination $CefSharpAssetsDir -Force
}

# Check for Flash plugin
Write-Host ""
Write-Host "Checking Flash plugin..." -ForegroundColor Green
$FlashPath = Join-Path $PluginsDir "pepflashplayer.dll"
if (Test-Path $FlashPath) {
    Write-Host "  Flash plugin found: $FlashPath" -ForegroundColor Green
} else {
    Write-Host "  WARNING: Flash plugin not found!" -ForegroundColor Yellow
    Write-Host "  Please manually copy pepflashplayer.dll to: $PluginsDir"
    Write-Host "  You can obtain it from:"
    Write-Host "    - https://github.com/nicknisi/flash-player-ppapi"
    Write-Host "    - Or extract from an old Chrome/Flash installation"
}

# Check for SwfPlayer HTML
Write-Host ""
Write-Host "Checking SwfPlayer..." -ForegroundColor Green
$SwfPlayerPath = Join-Path $SwfPlayerDir "swfplayer.html"
if (Test-Path $SwfPlayerPath) {
    Write-Host "  SwfPlayer found: $SwfPlayerPath" -ForegroundColor Green
} else {
    Write-Host "  WARNING: swfplayer.html not found!" -ForegroundColor Yellow
    Write-Host "  Creating default swfplayer.html..."
    
    $SwfPlayerHtml = @'
<!DOCTYPE html>
<html>

<head>
    <meta charset="UTF-8">
    <title>FlashPlayer</title>
    <style>
        #flash-player {
            position: absolute;
            left: 0px;
            top: 0px;
            width: 100%;
            height: 100vh;
        }
    </style>
    <script>
        function getFileName(src) {
            src = src.replace(/\\/g, '/');
            return src.split('/').pop().split('?')[0];
        }
        function loadSwf(src) { // called by CefSharp
            let flash = document.createElement("embed");
            flash.id = "flash-player";
            flash.setAttribute("src", src);
            document.body.replaceChild(flash, document.getElementById(flash.id));
            document.title = getFileName(src);
        }
    </script>
</head>

<body>
    <embed id="flash-player" src="" />
</body>

</html>
'@
    
    Set-Content -Path $SwfPlayerPath -Value $SwfPlayerHtml -Encoding UTF8
    Write-Host "  Created: $SwfPlayerPath" -ForegroundColor Green
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Assets Setup Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Assets directory: $AssetsDir"
Write-Host ""
Write-Host "Structure:"
Write-Host "  Assets/"
Write-Host "    CefSharp/         - CefSharp runtime files"
Write-Host "    Plugins/          - Flash plugin (pepflashplayer.dll)"
Write-Host "    SwfPlayer/        - SWF player HTML"
Write-Host "    EmptyExe/         - Empty executable (optional)"
Write-Host ""

# Verify critical files
Write-Host "Verifying critical files..." -ForegroundColor Green
$CriticalFiles = @(
    (Join-Path $CefSharpAssetsDir "libcef.dll"),
    (Join-Path $CefSharpAssetsDir "CefSharp.dll"),
    (Join-Path $CefSharpAssetsDir "CefSharp.Core.dll"),
    (Join-Path $CefSharpAssetsDir "CefSharp.BrowserSubprocess.exe"),
    (Join-Path $CefSharpAssetsDir "icudtl.dat")
)

$AllPresent = $true
foreach ($file in $CriticalFiles) {
    if (Test-Path $file) {
        Write-Host "  [OK] $(Split-Path -Leaf $file)" -ForegroundColor Green
    } else {
        Write-Host "  [MISSING] $(Split-Path -Leaf $file)" -ForegroundColor Red
        $AllPresent = $false
    }
}

if ($AllPresent) {
    Write-Host ""
    Write-Host "All critical files present!" -ForegroundColor Green
    Write-Host "You can now build and run SvonyBrowser." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Some critical files are missing!" -ForegroundColor Red
    Write-Host "Please run 'nuget restore' and try again." -ForegroundColor Yellow
}
