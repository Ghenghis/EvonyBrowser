#Requires -Version 5.1
<#
.SYNOPSIS
    Svony Browser Launcher - Advanced PowerShell launcher with options

.DESCRIPTION
    Launches Svony Browser with optional Fiddler auto-start, server selection,
    and proxy verification.

.PARAMETER Server
    Target server (cc1-cc5). Default: cc2

.PARAMETER NoFiddler
    Skip starting Fiddler

.PARAMETER Build
    Build the project before launching

.PARAMETER Debug
    Use Debug build instead of Release

.EXAMPLE
    .\Launch-SvonyBrowser.ps1
    .\Launch-SvonyBrowser.ps1 -Server cc3
    .\Launch-SvonyBrowser.ps1 -Build -Debug
#>
param(
    [ValidateSet("cc1", "cc2", "cc3", "cc4", "cc5")]
    [string]$Server = "cc2",
    
    [switch]$NoFiddler,
    [switch]$Build,
    [switch]$Debug
)

$ErrorActionPreference = "Stop"
$BasePath = Split-Path -Parent $MyInvocation.MyCommand.Path

# Banner
Write-Host ""
Write-Host "  ╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "  ║              SVONY BROWSER - Dual Panel Evony             ║" -ForegroundColor Cyan
Write-Host "  ║           AutoEvony + EvonyClient Side-by-Side            ║" -ForegroundColor Cyan
Write-Host "  ╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Build if requested
if ($Build) {
    Write-Host "[BUILD] Building Svony Browser..." -ForegroundColor Yellow
    $config = if ($Debug) { "Debug" } else { "Release" }
    Push-Location "$BasePath\SvonyBrowser"
    try {
        dotnet build -c $config
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed"
        }
        Write-Host "       Build successful ✓" -ForegroundColor Green
    } finally {
        Pop-Location
    }
    Write-Host ""
}

# Start Fiddler
if (-not $NoFiddler) {
    Write-Host "[1/3] Checking Fiddler proxy..." -ForegroundColor White
    $fiddlerRunning = Get-Process -Name "Fiddler" -ErrorAction SilentlyContinue
    
    if ($fiddlerRunning) {
        Write-Host "      Fiddler is already running ✓" -ForegroundColor Green
    } else {
        $fiddlerPath = Join-Path $BasePath "Fiddler\Fiddler.exe"
        if (Test-Path $fiddlerPath) {
            Write-Host "      Starting Fiddler..." -ForegroundColor Yellow
            Start-Process $fiddlerPath
            Start-Sleep -Seconds 3
            Write-Host "      Fiddler started ✓" -ForegroundColor Green
        } else {
            Write-Host "      [WARNING] Fiddler not found at $fiddlerPath" -ForegroundColor Red
        }
    }
    Write-Host ""
}

# Wait for proxy
Write-Host "[2/3] Waiting for proxy (127.0.0.1:8888)..." -ForegroundColor White
$maxWait = 15
$waited = 0

while ($waited -lt $maxWait) {
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $tcpClient.Connect("127.0.0.1", 8888)
        $tcpClient.Close()
        Write-Host "      Proxy ready ✓" -ForegroundColor Green
        break
    } catch {
        $waited++
        if ($waited -ge $maxWait) {
            Write-Host "      [WARNING] Proxy not responding" -ForegroundColor Yellow
        } else {
            Write-Host "      Waiting... ($waited/$maxWait)" -ForegroundColor Gray
            Start-Sleep -Seconds 1
        }
    }
}
Write-Host ""

# Launch browser
Write-Host "[3/3] Starting Svony Browser..." -ForegroundColor White

$config = if ($Debug) { "Debug" } else { "Release" }
$exePath = Join-Path $BasePath "SvonyBrowser\bin\$config\net6.0-windows\SvonyBrowser.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "      [ERROR] SvonyBrowser.exe not found!" -ForegroundColor Red
    Write-Host "      Path: $exePath" -ForegroundColor Gray
    Write-Host ""
    Write-Host "      Build the project first:" -ForegroundColor Yellow
    Write-Host "        cd SvonyBrowser" -ForegroundColor White
    Write-Host "        dotnet build -c Release" -ForegroundColor White
    Write-Host ""
    exit 1
}

Start-Process $exePath -ArgumentList "--server=$Server"
Write-Host "      Svony Browser launched ✓" -ForegroundColor Green
Write-Host "      Server: $Server.evony.com" -ForegroundColor Cyan

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor DarkGray
Write-Host "  Keyboard Shortcuts:" -ForegroundColor White
Write-Host "    Ctrl+1  = Show Bot Only" -ForegroundColor Gray
Write-Host "    Ctrl+2  = Show Both Panels" -ForegroundColor Gray
Write-Host "    Ctrl+3  = Show Client Only" -ForegroundColor Gray
Write-Host "    Ctrl+S  = Swap Panels" -ForegroundColor Gray
Write-Host "    F5      = Reload Left Panel" -ForegroundColor Gray
Write-Host "    F6      = Reload Right Panel" -ForegroundColor Gray
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor DarkGray
Write-Host ""
