#Requires -Version 5.1
<#
.SYNOPSIS
    Evony RE Toolkit Launcher - Unified Fiddler + FlashBrowser Integration
    
.DESCRIPTION
    Launches Fiddler Classic and CefFlashBrowser with preconfigured settings
    for capturing and analyzing Evony game traffic from cc2.evony.com
    
.PARAMETER Server
    Target Evony server (default: cc2)
    
.PARAMETER NoFiddler
    Skip launching Fiddler (use existing instance)
    
.PARAMETER ExtractSWF
    Enable automatic SWF extraction mode
    
.PARAMETER Stealth
    Enable stealth mode (remove proxy headers)
    
.EXAMPLE
    .\Launch-EvonyRE.ps1
    .\Launch-EvonyRE.ps1 -Server cc3
    .\Launch-EvonyRE.ps1 -ExtractSWF -Stealth
#>

param(
    [string]$Server = "cc2",
    [switch]$NoFiddler,
    [switch]$ExtractSWF,
    [switch]$Stealth,
    [switch]$ClearCache,
    [switch]$InstallCert,
    [switch]$Help
)

# Script configuration
$ScriptRoot = Split-Path -Parent $PSScriptRoot
$ConfigPath = Join-Path $ScriptRoot "config\evony-re-config.json"
$FiddlerPath = Join-Path $ScriptRoot "Fiddler\Fiddler.exe"
$BrowserPath = Join-Path $ScriptRoot "FlashBrowser_x64\CefFlashBrowser.exe"
$LogPath = Join-Path $ScriptRoot "logs"
$CapturesPath = Join-Path $ScriptRoot "captures"
$SwfExtractPath = Join-Path $ScriptRoot "extracted-swf"

# Colors for output
$Colors = @{
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
    Info = "Cyan"
    Header = "Magenta"
}

function Write-Banner {
    $banner = @"

╔═══════════════════════════════════════════════════════════════════╗
║           EVONY RE TOOLKIT - Fiddler + FlashBrowser               ║
║                  Packet Capture & Analysis Suite                   ║
╠═══════════════════════════════════════════════════════════════════╣
║  Target: $($Server.ToUpper()).evony.com                                           ║
║  Mode:   $(if($Stealth){"STEALTH"}else{"STANDARD"})                                                ║
╚═══════════════════════════════════════════════════════════════════╝

"@
    Write-Host $banner -ForegroundColor $Colors.Header
}

function Write-Log {
    param([string]$Message, [string]$Level = "Info")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = $Colors[$Level]
    Write-Host "[$timestamp] " -NoNewline -ForegroundColor Gray
    Write-Host "[$Level] " -NoNewline -ForegroundColor $color
    Write-Host $Message -ForegroundColor White
    
    # Also log to file
    $logFile = Join-Path $LogPath "launcher-$(Get-Date -Format 'yyyy-MM-dd').log"
    "$timestamp [$Level] $Message" | Out-File -FilePath $logFile -Append -Encoding UTF8
}

function Test-FiddlerRunning {
    $fiddler = Get-Process -Name "Fiddler" -ErrorAction SilentlyContinue
    return $null -ne $fiddler
}

function Test-BrowserRunning {
    $browser = Get-Process -Name "CefFlashBrowser" -ErrorAction SilentlyContinue
    return $null -ne $browser
}

function Test-ProxyReady {
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient
        $tcp.Connect("127.0.0.1", 8888)
        $tcp.Close()
        return $true
    } catch {
        return $false
    }
}

function Install-FiddlerCertificate {
    Write-Log "Installing Fiddler root certificate for HTTPS decryption..." "Info"
    
    $certPath = Join-Path $ScriptRoot "Fiddler\TrustCert.exe"
    if (Test-Path $certPath) {
        Start-Process -FilePath $certPath -Wait -Verb RunAs
        Write-Log "Certificate installation initiated" "Success"
    } else {
        Write-Log "TrustCert.exe not found. Please install certificate manually via Fiddler." "Warning"
    }
}

function Set-BrowserProxy {
    param([string]$ProxyHost = "127.0.0.1", [int]$ProxyPort = 8888)
    
    # Set system proxy for the browser session
    $proxyServer = "${ProxyHost}:${ProxyPort}"
    
    # Create/update browser config for proxy
    $browserConfigPath = Join-Path $ScriptRoot "FlashBrowser_x64\proxy.config"
    @{
        ProxyEnabled = $true
        ProxyHost = $ProxyHost
        ProxyPort = $ProxyPort
    } | ConvertTo-Json | Set-Content -Path $browserConfigPath -Encoding UTF8
    
    Write-Log "Browser proxy configured: $proxyServer" "Success"
}

function Start-Fiddler {
    if (Test-FiddlerRunning) {
        Write-Log "Fiddler is already running" "Info"
        return $true
    }
    
    Write-Log "Starting Fiddler Classic..." "Info"
    
    # Start Fiddler
    Start-Process -FilePath $FiddlerPath
    
    # Wait for Fiddler to initialize
    $maxWait = 30
    $waited = 0
    while (-not (Test-ProxyReady) -and $waited -lt $maxWait) {
        Start-Sleep -Seconds 1
        $waited++
        Write-Host "." -NoNewline -ForegroundColor Gray
    }
    Write-Host ""
    
    if (Test-ProxyReady) {
        Write-Log "Fiddler proxy ready on 127.0.0.1:8888" "Success"
        return $true
    } else {
        Write-Log "Fiddler failed to start within $maxWait seconds" "Error"
        return $false
    }
}

function Start-FlashBrowser {
    param([string]$StartUrl)
    
    if (Test-BrowserRunning) {
        Write-Log "FlashBrowser is already running" "Warning"
        return
    }
    
    Write-Log "Starting CefFlashBrowser..." "Info"
    Write-Log "Target URL: $StartUrl" "Info"
    
    # Start browser with URL
    Start-Process -FilePath $BrowserPath -ArgumentList $StartUrl
    
    Write-Log "FlashBrowser launched" "Success"
}

function Clear-BrowserCache {
    $cachePath = Join-Path $env:LOCALAPPDATA "CefFlashBrowser\Cache"
    if (Test-Path $cachePath) {
        Write-Log "Clearing browser cache..." "Info"
        Remove-Item -Path $cachePath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Log "Cache cleared" "Success"
    }
}

function Initialize-SessionLogging {
    $sessionId = Get-Date -Format "yyyyMMdd-HHmmss"
    $sessionDir = Join-Path $CapturesPath $sessionId
    New-Item -ItemType Directory -Path $sessionDir -Force | Out-Null
    
    Write-Log "Session logging initialized: $sessionId" "Info"
    return $sessionDir
}

# Main execution
Write-Banner

# Ensure directories exist
@($LogPath, $CapturesPath, $SwfExtractPath) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force | Out-Null
    }
}

# Load configuration
if (Test-Path $ConfigPath) {
    $Config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
    Write-Log "Configuration loaded from $ConfigPath" "Success"
} else {
    Write-Log "Configuration file not found, using defaults" "Warning"
}

# Handle certificate installation
if ($InstallCert) {
    Install-FiddlerCertificate
    exit 0
}

# Clear cache if requested
if ($ClearCache) {
    Clear-BrowserCache
}

# Build target URL
$targetDomain = "$Server.evony.com"
$startUrl = "https://$targetDomain/"
Write-Log "Target server: $targetDomain" "Info"

# Initialize session
$sessionDir = Initialize-SessionLogging

# Start Fiddler (unless skipped)
if (-not $NoFiddler) {
    $fiddlerStarted = Start-Fiddler
    if (-not $fiddlerStarted) {
        Write-Log "Failed to start Fiddler. Exiting." "Error"
        exit 1
    }
    
    # Configure browser proxy
    Set-BrowserProxy -ProxyHost "127.0.0.1" -ProxyPort 8888
}

# Start the browser
Start-Sleep -Seconds 2
Start-FlashBrowser -StartUrl $startUrl

Write-Host ""
Write-Log "═══════════════════════════════════════════════════════════════════" "Header"
Write-Log "  Evony RE Toolkit is now running!" "Success"
Write-Log "  - Fiddler: Capturing traffic on 127.0.0.1:8888" "Info"
Write-Log "  - Browser: Connecting to $targetDomain" "Info"
Write-Log "  - Session: $sessionDir" "Info"
Write-Log "" "Info"
Write-Log "  HOTKEYS:" "Warning"
Write-Log "    Ctrl+Shift+C  - Toggle capture" "Info"
Write-Log "    Ctrl+Shift+E  - Export session" "Info"
Write-Log "    Ctrl+Shift+S  - Extract SWF files" "Info"
Write-Log "═══════════════════════════════════════════════════════════════════" "Header"

# Keep script running to show status
Write-Host ""
Write-Host "Press any key to exit launcher (Fiddler and Browser will continue running)..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
