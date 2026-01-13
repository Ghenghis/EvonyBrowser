#Requires -Version 5.1
<#
.SYNOPSIS
    Evony Tutorial SWF Extractor
    
.DESCRIPTION
    Specialized tool to capture and extract the Evony tutorial/guide SWF files.
    These SWFs contain the advisor woman with speech guidance for new players.
    
    The tutorial SWFs are ONLY loaded when:
    - Starting a brand new account
    - Going through the initial game setup
    - First-time building/research prompts
    
.PARAMETER WatchMode
    Continuously monitor for new SWF downloads
    
.PARAMETER Server
    Target server (default: cc2)
    
.NOTES
    Tutorial SWF patterns to look for:
    - *tutorial*.swf
    - *guide*.swf  
    - *advisor*.swf
    - *intro*.swf
    - *newbie*.swf
    - *helper*.swf
    - *npc*.swf
    - *queen*.swf (the advisor character)
#>

param(
    [switch]$WatchMode,
    [string]$Server = "cc2",
    [string]$OutputDir = $null
)

$ScriptRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDir) {
    $OutputDir = Join-Path $ScriptRoot "extracted-swf\tutorial"
}

# Known tutorial/guide SWF patterns
$TutorialPatterns = @(
    "*tutorial*",
    "*guide*",
    "*advisor*",
    "*intro*",
    "*newbie*",
    "*helper*",
    "*npc*",
    "*queen*",
    "*assistant*",
    "*tips*",
    "*hint*",
    "*novice*",
    "*beginner*"
)

# Colors
$C = @{ S = "Green"; W = "Yellow"; E = "Red"; I = "Cyan"; H = "Magenta" }

function Write-Banner {
    Write-Host @"

╔═══════════════════════════════════════════════════════════════════╗
║         EVONY TUTORIAL SWF EXTRACTOR                               ║
║                                                                    ║
║  Captures the advisor/guide SWF files from new account setup       ║
╚═══════════════════════════════════════════════════════════════════╝

"@ -ForegroundColor $C.H
}

function Write-Log {
    param([string]$Message, [string]$Level = "Info")
    $timestamp = Get-Date -Format "HH:mm:ss"
    $color = $C[$Level[0]]
    Write-Host "[$timestamp] " -NoNewline -ForegroundColor Gray
    Write-Host $Message -ForegroundColor $color
}

function Test-TutorialSWF {
    param([string]$FileName)
    
    $name = $FileName.ToLower()
    
    foreach ($pattern in $TutorialPatterns) {
        if ($name -like $pattern) {
            return $true
        }
    }
    
    return $false
}

function Get-SWFFromCaptures {
    Write-Log "Scanning captures folder for SWF files..." "Info"
    
    $capturesDir = Join-Path $ScriptRoot "captures"
    $swfExtractDir = Join-Path $ScriptRoot "extracted-swf"
    
    # Look for any SAZ files (Fiddler session archives)
    $sazFiles = Get-ChildItem -Path $capturesDir -Filter "*.saz" -Recurse -ErrorAction SilentlyContinue
    
    Write-Log "Found $($sazFiles.Count) SAZ capture files" "Info"
    
    # Also check the daily extraction folders
    $swfFiles = Get-ChildItem -Path $swfExtractDir -Filter "*.swf" -Recurse -ErrorAction SilentlyContinue
    
    $tutorialSWFs = @()
    
    foreach ($swf in $swfFiles) {
        if (Test-TutorialSWF $swf.Name) {
            $tutorialSWFs += $swf
            Write-Log "Found tutorial SWF: $($swf.Name)" "Success"
        }
    }
    
    if ($tutorialSWFs.Count -eq 0) {
        Write-Log "No tutorial SWFs found yet" "Warning"
    } else {
        Write-Log "Found $($tutorialSWFs.Count) potential tutorial SWF(s)" "Success"
        
        # Copy to tutorial folder
        if (-not (Test-Path $OutputDir)) {
            New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
        }
        
        foreach ($swf in $tutorialSWFs) {
            $destPath = Join-Path $OutputDir $swf.Name
            if (-not (Test-Path $destPath)) {
                Copy-Item $swf.FullName $destPath
                Write-Log "Copied: $($swf.Name)" "Success"
            }
        }
    }
    
    return $tutorialSWFs
}

function Watch-ForSWF {
    Write-Log "Starting SWF watch mode..." "Info"
    Write-Log "Monitoring: $(Join-Path $ScriptRoot 'extracted-swf')" "Info"
    Write-Host ""
    Write-Host "  Waiting for tutorial SWF files..." -ForegroundColor $C.W
    Write-Host "  Start a NEW Evony account to trigger the tutorial." -ForegroundColor $C.W
    Write-Host ""
    Write-Host "  Press Ctrl+C to stop watching." -ForegroundColor Gray
    Write-Host ""
    
    $watchPath = Join-Path $ScriptRoot "extracted-swf"
    
    # Create a file system watcher
    $watcher = New-Object System.IO.FileSystemWatcher
    $watcher.Path = $watchPath
    $watcher.Filter = "*.swf"
    $watcher.IncludeSubdirectories = $true
    $watcher.EnableRaisingEvents = $true
    
    # Event handler for new files
    $action = {
        $path = $Event.SourceEventArgs.FullPath
        $name = $Event.SourceEventArgs.Name
        $changeType = $Event.SourceEventArgs.ChangeType
        
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] " -NoNewline -ForegroundColor Gray
        
        if (Test-TutorialSWF $name) {
            Write-Host "★ TUTORIAL SWF DETECTED: $name" -ForegroundColor Green
            
            # Copy to tutorial folder
            $tutorialDir = Join-Path $using:ScriptRoot "extracted-swf\tutorial"
            if (-not (Test-Path $tutorialDir)) {
                New-Item -ItemType Directory -Path $tutorialDir -Force | Out-Null
            }
            
            $destPath = Join-Path $tutorialDir (Split-Path $name -Leaf)
            Copy-Item $path $destPath -Force
            Write-Host "         Copied to: $destPath" -ForegroundColor Cyan
        } else {
            Write-Host "SWF captured: $name" -ForegroundColor Yellow
        }
    }
    
    Register-ObjectEvent $watcher "Created" -Action $action | Out-Null
    
    try {
        while ($true) {
            Start-Sleep -Seconds 1
        }
    } finally {
        $watcher.EnableRaisingEvents = $false
        $watcher.Dispose()
        Get-EventSubscriber | Unregister-Event
    }
}

function Show-Instructions {
    Write-Host @"

═══════════════════════════════════════════════════════════════════════
  HOW TO CAPTURE THE TUTORIAL/GUIDE SWF
═══════════════════════════════════════════════════════════════════════

  The tutorial SWF with the advisor woman only loads for NEW accounts.
  
  STEPS:
  
  1. Run Setup-EvonyRE.bat (if not already done)
  
  2. Run Launch-EvonyRE.bat to start Fiddler + Browser
  
  3. Make sure Fiddler shows "Capturing" in the status bar
  
  4. In the browser, go to: https://$Server.evony.com/
  
  5. Create a NEW account (or use a fresh account that hasn't
     completed the tutorial)
  
  6. Play through the initial tutorial steps
     - The advisor woman will appear with speech bubbles
     - This is when the tutorial SWFs are loaded
  
  7. Check the extracted-swf folder for captured SWFs
  
  8. Run this script to identify and collect tutorial SWFs:
     .\Extract-TutorialSWF.ps1
  
  9. Or use watch mode to monitor in real-time:
     .\Extract-TutorialSWF.ps1 -WatchMode

═══════════════════════════════════════════════════════════════════════

  EXPECTED SWF FILES:
  
  Look for files containing these keywords:
    - tutorial, guide, advisor, intro, newbie
    - helper, npc, queen, assistant, tips

═══════════════════════════════════════════════════════════════════════

"@ -ForegroundColor $C.I
}

# Main
Write-Banner

# Ensure output directory exists
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

Show-Instructions

if ($WatchMode) {
    Watch-ForSWF
} else {
    # Scan existing files
    $found = Get-SWFFromCaptures
    
    Write-Host ""
    Write-Host "  Tutorial SWFs collected in: $OutputDir" -ForegroundColor $C.S
    Write-Host ""
    
    if ($found.Count -eq 0) {
        Write-Host "  No tutorial SWFs found yet. Follow the instructions above." -ForegroundColor $C.W
        Write-Host ""
        Write-Host "  TIP: Run with -WatchMode to monitor in real-time:" -ForegroundColor Gray
        Write-Host "       .\Extract-TutorialSWF.ps1 -WatchMode" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
