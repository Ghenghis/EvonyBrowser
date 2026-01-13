#Requires -Version 5.1
<#
.SYNOPSIS
    Evony RE Toolkit Setup - Installs custom Fiddler rules and configures the toolkit
    
.DESCRIPTION
    - Installs EvonyRE-CustomRules.cs to Fiddler Scripts folder
    - Configures FlashBrowser proxy settings
    - Sets up SSL certificate for HTTPS capture (optional)
    - Creates desktop shortcuts
    
.PARAMETER InstallRules
    Install custom Fiddler rules
    
.PARAMETER InstallCert
    Install Fiddler root certificate for HTTPS decryption
    
.PARAMETER CreateShortcuts
    Create desktop shortcuts
    
.PARAMETER All
    Run full setup (all options)
#>

param(
    [switch]$InstallRules,
    [switch]$InstallCert,
    [switch]$CreateShortcuts,
    [switch]$ConfigureBrowser,
    [switch]$All,
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $PSScriptRoot
$FiddlerScriptsPath = Join-Path $env:USERPROFILE "Documents\Fiddler2\Scripts"
$CustomRulesSource = Join-Path $ScriptRoot "config\EvonyRE-CustomRules.cs"

# Colors
$C = @{ S = "Green"; W = "Yellow"; E = "Red"; I = "Cyan"; H = "Magenta" }

function Write-Banner {
    Write-Host @"

╔═══════════════════════════════════════════════════════════════════╗
║              EVONY RE TOOLKIT - Setup Utility                      ║
╚═══════════════════════════════════════════════════════════════════╝

"@ -ForegroundColor $C.H
}

function Write-Step {
    param([string]$Message, [string]$Status = "Info")
    $icon = switch ($Status) {
        "Success" { "✓" }
        "Warning" { "⚠" }
        "Error" { "✗" }
        default { "→" }
    }
    Write-Host "  $icon " -NoNewline -ForegroundColor $C[$Status[0]]
    Write-Host $Message
}

function Install-CustomRules {
    Write-Host "`n[Installing Custom Fiddler Rules]" -ForegroundColor $C.I
    
    # Create Fiddler Scripts directory if it doesn't exist
    if (-not (Test-Path $FiddlerScriptsPath)) {
        New-Item -ItemType Directory -Path $FiddlerScriptsPath -Force | Out-Null
        Write-Step "Created Fiddler Scripts folder" "Success"
    }
    
    # Check if source exists
    if (-not (Test-Path $CustomRulesSource)) {
        Write-Step "Custom rules file not found: $CustomRulesSource" "Error"
        return $false
    }
    
    # Backup existing CustomRules.cs if it exists
    $targetPath = Join-Path $FiddlerScriptsPath "CustomRules.cs"
    if (Test-Path $targetPath) {
        $backupPath = Join-Path $FiddlerScriptsPath "CustomRules.cs.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        Copy-Item $targetPath $backupPath
        Write-Step "Backed up existing rules to: $backupPath" "Warning"
    }
    
    # Copy new rules
    Copy-Item $CustomRulesSource $targetPath -Force
    Write-Step "Installed EvonyRE-CustomRules.cs" "Success"
    
    # Verify installation
    if (Test-Path $targetPath) {
        Write-Step "Rules installed to: $targetPath" "Success"
        return $true
    } else {
        Write-Step "Installation verification failed" "Error"
        return $false
    }
}

function Install-FiddlerCertificate {
    Write-Host "`n[Installing Fiddler Root Certificate]" -ForegroundColor $C.I
    
    $trustCertPath = Join-Path $ScriptRoot "Fiddler\TrustCert.exe"
    
    if (-not (Test-Path $trustCertPath)) {
        Write-Step "TrustCert.exe not found at: $trustCertPath" "Error"
        Write-Step "Please install certificate manually via Fiddler > Tools > Options > HTTPS" "Warning"
        return $false
    }
    
    Write-Step "Launching certificate installer (requires admin approval)..." "Info"
    
    try {
        Start-Process -FilePath $trustCertPath -Verb RunAs -Wait
        Write-Step "Certificate installation completed" "Success"
        
        Write-Host @"

  NOTE: After installing the certificate:
    1. Open Fiddler
    2. Go to Tools > Options > HTTPS
    3. Check "Decrypt HTTPS traffic"
    4. Click OK and restart Fiddler

"@ -ForegroundColor $C.W
        return $true
    } catch {
        Write-Step "Certificate installation failed: $_" "Error"
        return $false
    }
}

function Configure-Browser {
    Write-Host "`n[Configuring FlashBrowser Proxy Settings]" -ForegroundColor $C.I
    
    $browserConfigDir = Join-Path $ScriptRoot "FlashBrowser_x64"
    
    # Create a settings file that the browser can read
    # Note: CefFlashBrowser stores settings in AppData, but we'll create a reference config
    
    $proxyConfig = @{
        ProxySettings = @{
            EnableProxy = $true
            IP = "127.0.0.1"
            Port = 8888
            UserName = ""
            Password = ""
        }
        Note = "To apply these settings, open FlashBrowser > Settings > Network and enter these proxy details"
    }
    
    $configPath = Join-Path $browserConfigDir "recommended-proxy-settings.json"
    $proxyConfig | ConvertTo-Json -Depth 3 | Set-Content $configPath -Encoding UTF8
    
    Write-Step "Created proxy configuration reference: $configPath" "Success"
    
    Write-Host @"

  IMPORTANT: Configure proxy in FlashBrowser manually:
    1. Open CefFlashBrowser.exe
    2. Click Settings (gear icon)
    3. Go to Network/Proxy settings
    4. Enter:
       - Enable Proxy: Yes
       - IP: 127.0.0.1
       - Port: 8888
    5. Save and restart the browser

"@ -ForegroundColor $C.W
    
    return $true
}

function Create-Shortcuts {
    Write-Host "`n[Creating Desktop Shortcuts]" -ForegroundColor $C.I
    
    $desktop = [Environment]::GetFolderPath("Desktop")
    $shell = New-Object -ComObject WScript.Shell
    
    # Main launcher shortcut
    $launcherPath = Join-Path $ScriptRoot "Launch-EvonyRE.bat"
    $shortcutPath = Join-Path $desktop "Evony RE Toolkit.lnk"
    
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $launcherPath
    $shortcut.WorkingDirectory = $ScriptRoot
    $shortcut.Description = "Launch Evony RE Toolkit (Fiddler + FlashBrowser)"
    $shortcut.IconLocation = Join-Path $ScriptRoot "Fiddler\App.ico"
    $shortcut.Save()
    
    Write-Step "Created: Evony RE Toolkit.lnk" "Success"
    
    # FlashBrowser only shortcut
    $browserPath = Join-Path $ScriptRoot "FlashBrowser_x64\CefFlashBrowser.exe"
    $shortcutPath2 = Join-Path $desktop "Evony FlashBrowser.lnk"
    
    $shortcut2 = $shell.CreateShortcut($shortcutPath2)
    $shortcut2.TargetPath = $browserPath
    $shortcut2.WorkingDirectory = Join-Path $ScriptRoot "FlashBrowser_x64"
    $shortcut2.Description = "CefFlashBrowser for Evony"
    $shortcut2.Arguments = "https://cc2.evony.com/"
    $shortcut2.Save()
    
    Write-Step "Created: Evony FlashBrowser.lnk" "Success"
    
    # Fiddler shortcut
    $fiddlerPath = Join-Path $ScriptRoot "Fiddler\Fiddler.exe"
    $shortcutPath3 = Join-Path $desktop "Evony Fiddler.lnk"
    
    $shortcut3 = $shell.CreateShortcut($shortcutPath3)
    $shortcut3.TargetPath = $fiddlerPath
    $shortcut3.WorkingDirectory = Join-Path $ScriptRoot "Fiddler"
    $shortcut3.Description = "Fiddler Classic for Evony RE"
    $shortcut3.Save()
    
    Write-Step "Created: Evony Fiddler.lnk" "Success"
    
    return $true
}

function Uninstall-Toolkit {
    Write-Host "`n[Uninstalling Evony RE Toolkit Components]" -ForegroundColor $C.I
    
    # Remove custom rules
    $rulesPath = Join-Path $FiddlerScriptsPath "CustomRules.cs"
    if (Test-Path $rulesPath) {
        # Check if it's our rules
        $content = Get-Content $rulesPath -Raw
        if ($content -match "EVONY RE TOOLKIT") {
            Remove-Item $rulesPath -Force
            Write-Step "Removed custom Fiddler rules" "Success"
            
            # Restore backup if exists
            $latestBackup = Get-ChildItem (Join-Path $FiddlerScriptsPath "CustomRules.cs.backup-*") | 
                            Sort-Object LastWriteTime -Descending | 
                            Select-Object -First 1
            
            if ($latestBackup) {
                Copy-Item $latestBackup.FullName $rulesPath
                Write-Step "Restored previous rules from backup" "Success"
            }
        } else {
            Write-Step "CustomRules.cs doesn't appear to be from Evony RE, skipping" "Warning"
        }
    }
    
    # Remove shortcuts
    $desktop = [Environment]::GetFolderPath("Desktop")
    @("Evony RE Toolkit.lnk", "Evony FlashBrowser.lnk", "Evony Fiddler.lnk") | ForEach-Object {
        $path = Join-Path $desktop $_
        if (Test-Path $path) {
            Remove-Item $path -Force
            Write-Step "Removed shortcut: $_" "Success"
        }
    }
    
    Write-Step "Uninstallation complete" "Success"
    Write-Host "`n  Note: Fiddler certificate and browser settings were not modified." -ForegroundColor $C.W
}

# Main execution
Write-Banner

if ($Uninstall) {
    Uninstall-Toolkit
    exit 0
}

# Default to All if no options specified
if (-not ($InstallRules -or $InstallCert -or $CreateShortcuts -or $ConfigureBrowser -or $All)) {
    $All = $true
}

$results = @{}

if ($All -or $InstallRules) {
    $results["Rules"] = Install-CustomRules
}

if ($All -or $ConfigureBrowser) {
    $results["Browser"] = Configure-Browser
}

if ($All -or $CreateShortcuts) {
    $results["Shortcuts"] = Create-Shortcuts
}

if ($InstallCert) {
    $results["Certificate"] = Install-FiddlerCertificate
}

# Summary
Write-Host "`n[Setup Summary]" -ForegroundColor $C.I
$results.GetEnumerator() | ForEach-Object {
    $status = if ($_.Value) { "Success" } else { "Error" }
    Write-Step "$($_.Key): $(if ($_.Value) { 'Completed' } else { 'Failed' })" $status
}

Write-Host @"

╔═══════════════════════════════════════════════════════════════════╗
║                       SETUP COMPLETE!                              ║
╠═══════════════════════════════════════════════════════════════════╣
║  Next Steps:                                                       ║
║    1. Configure browser proxy (see instructions above)             ║
║    2. Run Launch-EvonyRE.bat to start the toolkit                 ║
║    3. For HTTPS capture, run: .\Setup-EvonyRE.ps1 -InstallCert    ║
╚═══════════════════════════════════════════════════════════════════╝

"@ -ForegroundColor $C.H

Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
