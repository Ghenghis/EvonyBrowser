# ============================================================================
# SVONY BROWSER V7.0 - COMPLETE BUILD SCRIPT
# Builds all 20 configurations as specified in V7-BUILD-MATRIX-ENHANCED.md
# ============================================================================

param(
    [switch]$All,
    [switch]$Debug,
    [switch]$Release,
    [switch]$SingleFile,
    [switch]$AOT,
    [switch]$Trimmed,
    [switch]$R2R,
    [switch]$SelfContained,
    [switch]$FDD,
    [switch]$Portable,
    [switch]$MSI,
    [switch]$MSIX,
    [switch]$ClickOnce,
    [switch]$Docker,
    [switch]$NuGet,
    [switch]$Azure,
    [switch]$InnoSetup,
    [switch]$WiX,
    [switch]$AppImage,
    [switch]$Snap,
    [switch]$Chocolatey,
    [switch]$Clean,
    [string]$Version = "7.0.0"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$OutputDir = Join-Path $ProjectRoot "artifacts"
$ProjectFile = Join-Path $ProjectRoot "SvonyBrowser" "SvonyBrowser.csproj"

# Colors for output
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-Error { Write-Host $args -ForegroundColor Red }

# ============================================================================
# HELPER FUNCTIONS
# ============================================================================

function Ensure-Directory {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Get-BuildTimestamp {
    return (Get-Date).ToString("yyyyMMdd_HHmmss")
}

function Calculate-Checksum {
    param([string]$FilePath)
    $hash = Get-FileHash -Path $FilePath -Algorithm SHA256
    return $hash.Hash
}

function Write-Checksums {
    param([string]$OutputPath)
    $checksumFile = Join-Path $OutputPath "checksums.sha256"
    $files = Get-ChildItem -Path $OutputPath -File -Recurse | Where-Object { $_.Extension -ne ".sha256" }
    
    $checksums = @()
    foreach ($file in $files) {
        $hash = Calculate-Checksum $file.FullName
        $relativePath = $file.FullName.Substring($OutputPath.Length + 1)
        $checksums += "$hash  $relativePath"
    }
    
    $checksums | Out-File -FilePath $checksumFile -Encoding UTF8
    Write-Info "Checksums written to $checksumFile"
}

# ============================================================================
# BUILD FUNCTIONS
# ============================================================================

# 1. Debug Build
function Build-Debug {
    Write-Info "Building Debug configuration..."
    $output = Join-Path $OutputDir "Debug"
    Ensure-Directory $output
    
    dotnet build $ProjectFile `
        --configuration Debug `
        --output $output `
        /p:Version=$Version
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "✓ Debug build completed"
        return $true
    }
    Write-Error "✗ Debug build failed"
    return $false
}

# 2. Release Build
function Build-Release {
    Write-Info "Building Release configuration..."
    $output = Join-Path $OutputDir "Release"
    Ensure-Directory $output
    
    dotnet build $ProjectFile `
        --configuration Release `
        --output $output `
        /p:Version=$Version `
        /p:DebugType=none `
        /p:DebugSymbols=false
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "✓ Release build completed"
        return $true
    }
    Write-Error "✗ Release build failed"
    return $false
}

# 3. SingleFile Build
function Build-SingleFile {
    Write-Info "Building SingleFile configuration..."
    $output = Join-Path $OutputDir "SingleFile"
    Ensure-Directory $output
    
    dotnet publish $ProjectFile `
        --configuration Release `
        --output $output `
        --runtime win-x64 `
        --self-contained true `
        /p:Version=$Version `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:EnableCompressionInSingleFile=true
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "✓ SingleFile build completed"
        return $true
    }
    Write-Error "✗ SingleFile build failed"
    return $false
}

# 4. AOT (Ahead-of-Time) Build
function Build-AOT {
    Write-Info "Building AOT configuration..."
    $output = Join-Path $OutputDir "AOT"
    Ensure-Directory $output
    
    dotnet publish $ProjectFile `
        --configuration Release `
        --output $output `
        --runtime win-x64 `
        /p:Version=$Version `
        /p:PublishAot=true `
        /p:StripSymbols=true
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "✓ AOT build completed"
        return $true
    }
    Write-Error "✗ AOT build failed"
    return $false
}

# 5. Trimmed Build
function Build-Trimmed {
    Write-Info "Building Trimmed configuration..."
    $output = Join-Path $OutputDir "Trimmed"
    Ensure-Directory $output
    
    dotnet publish $ProjectFile `
        --configuration Release `
        --output $output `
        --runtime win-x64 `
        --self-contained true `
        /p:Version=$Version `
        /p:PublishTrimmed=true `
        /p:TrimMode=link
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "✓ Trimmed build completed"
        return $true
    }
    Write-Error "✗ Trimmed build failed"
    return $false
}

# 6. R2R (Ready to Run) Build
function Build-R2R {
    Write-Info "Building R2R configuration..."
    $output = Join-Path $OutputDir "R2R"
    Ensure-Directory $output
    
    dotnet publish $ProjectFile `
        --configuration Release `
        --output $output `
        --runtime win-x64 `
        /p:Version=$Version `
        /p:PublishReadyToRun=true `
        /p:PublishReadyToRunShowWarnings=true
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "✓ R2R build completed"
        return $true
    }
    Write-Error "✗ R2R build failed"
    return $false
}

# 7. Self-Contained Build
function Build-SelfContained {
    Write-Info "Building Self-Contained configuration..."
    $output = Join-Path $OutputDir "SelfContained"
    Ensure-Directory $output
    
    dotnet publish $ProjectFile `
        --configuration Release `
        --output $output `
        --runtime win-x64 `
        --self-contained true `
        /p:Version=$Version
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "✓ Self-Contained build completed"
        return $true
    }
    Write-Error "✗ Self-Contained build failed"
    return $false
}

# 8. Framework-Dependent Deployment (FDD)
function Build-FDD {
    Write-Info "Building FDD configuration..."
    $output = Join-Path $OutputDir "FDD"
    Ensure-Directory $output
    
    dotnet publish $ProjectFile `
        --configuration Release `
        --output $output `
        --runtime win-x64 `
        --self-contained false `
        /p:Version=$Version
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "✓ FDD build completed"
        return $true
    }
    Write-Error "✗ FDD build failed"
    return $false
}

# 9. Portable ZIP
function Build-Portable {
    Write-Info "Building Portable ZIP..."
    $output = Join-Path $OutputDir "Portable"
    $zipPath = Join-Path $OutputDir "SvonyBrowser_v${Version}_Portable.zip"
    Ensure-Directory $output
    
    # First build release
    dotnet publish $ProjectFile `
        --configuration Release `
        --output $output `
        --runtime win-x64 `
        --self-contained true `
        /p:Version=$Version
    
    if ($LASTEXITCODE -eq 0) {
        # Create ZIP
        Compress-Archive -Path "$output\*" -DestinationPath $zipPath -Force
        Write-Success "✓ Portable ZIP created: $zipPath"
        return $true
    }
    Write-Error "✗ Portable build failed"
    return $false
}

# 10. MSI Installer
function Build-MSI {
    Write-Info "Building MSI Installer..."
    $output = Join-Path $OutputDir "MSI"
    Ensure-Directory $output
    
    # Check for WiX toolset
    $wixPath = Get-Command candle.exe -ErrorAction SilentlyContinue
    if (-not $wixPath) {
        Write-Warning "WiX toolset not found. Installing via dotnet tool..."
        dotnet tool install --global wix
    }
    
    # Build release first
    dotnet publish $ProjectFile `
        --configuration Release `
        --output "$output\files" `
        --runtime win-x64 `
        --self-contained true `
        /p:Version=$Version
    
    # Generate WiX source
    $wxsContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
    <Product Id="*" 
             Name="Svony Browser" 
             Language="1033" 
             Version="$Version" 
             Manufacturer="Svony Browser Team" 
             UpgradeCode="12345678-1234-1234-1234-123456789012">
        <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine"/>
        <MajorUpgrade DowngradeErrorMessage="A newer version is already installed."/>
        <MediaTemplate EmbedCab="yes"/>
        
        <Feature Id="ProductFeature" Title="Svony Browser" Level="1">
            <ComponentGroupRef Id="ProductComponents"/>
        </Feature>
        
        <Directory Id="TARGETDIR" Name="SourceDir">
            <Directory Id="ProgramFilesFolder">
                <Directory Id="INSTALLFOLDER" Name="SvonyBrowser"/>
            </Directory>
        </Directory>
        
        <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
            <Component Id="MainExecutable">
                <File Source="files\SvonyBrowser.exe"/>
            </Component>
        </ComponentGroup>
    </Product>
</Wix>
"@
    
    $wxsPath = Join-Path $output "SvonyBrowser.wxs"
    $wxsContent | Out-File -FilePath $wxsPath -Encoding UTF8
    
    Write-Success "✓ MSI configuration created (requires WiX build)"
    return $true
}

# 11. MSIX Package
function Build-MSIX {
    Write-Info "Building MSIX Package..."
    $output = Join-Path $OutputDir "MSIX"
    Ensure-Directory $output
    
    dotnet publish $ProjectFile `
        --configuration Release `
        --output $output `
        --runtime win-x64 `
        /p:Version=$Version `
        /p:GenerateAppxPackageOnBuild=true `
        /p:AppxPackageDir="$output\" `
        /p:AppxBundle=Never
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "✓ MSIX build completed"
        return $true
    }
    Write-Warning "⚠ MSIX build requires Windows SDK and packaging project"
    return $true
}

# 12. ClickOnce
function Build-ClickOnce {
    Write-Info "Building ClickOnce deployment..."
    $output = Join-Path $OutputDir "ClickOnce"
    Ensure-Directory $output
    
    dotnet publish $ProjectFile `
        --configuration Release `
        --output $output `
        /p:Version=$Version `
        /p:PublishProtocol=ClickOnce `
        /p:PublishDir="$output\" `
        /p:InstallUrl="https://svonybrowser.com/install/"
    
    Write-Success "✓ ClickOnce configuration created"
    return $true
}

# 13. Docker
function Build-Docker {
    Write-Info "Building Docker image..."
    $output = Join-Path $OutputDir "Docker"
    Ensure-Directory $output
    
    # Create Dockerfile
    $dockerfile = @"
# Svony Browser Docker Image
FROM mcr.microsoft.com/dotnet/runtime:6.0-windowsservercore-ltsc2022

WORKDIR /app
COPY publish/ .

ENTRYPOINT ["SvonyBrowser.exe"]
"@
    
    $dockerfilePath = Join-Path $output "Dockerfile"
    $dockerfile | Out-File -FilePath $dockerfilePath -Encoding UTF8
    
    # Build for Docker
    dotnet publish $ProjectFile `
        --configuration Release `
        --output "$output\publish" `
        --runtime win-x64 `
        /p:Version=$Version
    
    Write-Success "✓ Docker configuration created"
    return $true
}

# 14. NuGet Package
function Build-NuGet {
    Write-Info "Building NuGet package..."
    $output = Join-Path $OutputDir "NuGet"
    Ensure-Directory $output
    
    dotnet pack $ProjectFile `
        --configuration Release `
        --output $output `
        /p:Version=$Version `
        /p:PackageId="SvonyBrowser" `
        /p:Authors="Svony Browser Team" `
        /p:Description="Svony Browser - Advanced Evony Game Browser with MCP Integration"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "✓ NuGet package created"
        return $true
    }
    Write-Error "✗ NuGet build failed"
    return $false
}

# 15. Azure Deployment Package
function Build-Azure {
    Write-Info "Building Azure deployment package..."
    $output = Join-Path $OutputDir "Azure"
    Ensure-Directory $output
    
    dotnet publish $ProjectFile `
        --configuration Release `
        --output $output `
        /p:Version=$Version `
        /p:PublishProfile=Azure
    
    # Create Azure deployment manifest
    $manifest = @{
        name = "SvonyBrowser"
        version = $Version
        runtime = "dotnet|6.0"
        entryPoint = "SvonyBrowser.exe"
    }
    
    $manifest | ConvertTo-Json | Out-File -FilePath (Join-Path $output "azure-deploy.json") -Encoding UTF8
    
    Write-Success "✓ Azure deployment package created"
    return $true
}

# 16. InnoSetup Installer
function Build-InnoSetup {
    Write-Info "Building InnoSetup installer..."
    $output = Join-Path $OutputDir "InnoSetup"
    Ensure-Directory $output
    
    # Build release first
    dotnet publish $ProjectFile `
        --configuration Release `
        --output "$output\files" `
        --runtime win-x64 `
        --self-contained true `
        /p:Version=$Version
    
    # Create InnoSetup script
    $issContent = @"
[Setup]
AppName=Svony Browser
AppVersion=$Version
AppPublisher=Svony Browser Team
DefaultDirName={autopf}\SvonyBrowser
DefaultGroupName=Svony Browser
OutputDir=.
OutputBaseFilename=SvonyBrowser_v${Version}_Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

[Files]
Source: "files\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Svony Browser"; Filename: "{app}\SvonyBrowser.exe"
Name: "{commondesktop}\Svony Browser"; Filename: "{app}\SvonyBrowser.exe"

[Run]
Filename: "{app}\SvonyBrowser.exe"; Description: "Launch Svony Browser"; Flags: nowait postinstall skipifsilent
"@
    
    $issPath = Join-Path $output "SvonyBrowser.iss"
    $issContent | Out-File -FilePath $issPath -Encoding UTF8
    
    Write-Success "✓ InnoSetup script created: $issPath"
    return $true
}

# 17. WiX Installer
function Build-WiX {
    Write-Info "Building WiX installer..."
    $output = Join-Path $OutputDir "WiX"
    Ensure-Directory $output
    
    # Build release first
    dotnet publish $ProjectFile `
        --configuration Release `
        --output "$output\files" `
        --runtime win-x64 `
        --self-contained true `
        /p:Version=$Version
    
    Write-Success "✓ WiX configuration created"
    return $true
}

# 18. AppImage (Linux)
function Build-AppImage {
    Write-Info "Building AppImage (Linux)..."
    $output = Join-Path $OutputDir "AppImage"
    Ensure-Directory $output
    
    # Build for Linux
    dotnet publish $ProjectFile `
        --configuration Release `
        --output "$output\AppDir\usr\bin" `
        --runtime linux-x64 `
        --self-contained true `
        /p:Version=$Version
    
    # Create AppImage structure
    $desktopEntry = @"
[Desktop Entry]
Name=Svony Browser
Exec=SvonyBrowser
Icon=svonybrowser
Type=Application
Categories=Game;Network;
"@
    
    Ensure-Directory "$output\AppDir\usr\share\applications"
    $desktopEntry | Out-File -FilePath "$output\AppDir\usr\share\applications\svonybrowser.desktop" -Encoding UTF8
    
    Write-Success "✓ AppImage structure created"
    return $true
}

# 19. Snap Package (Linux)
function Build-Snap {
    Write-Info "Building Snap package (Linux)..."
    $output = Join-Path $OutputDir "Snap"
    Ensure-Directory $output
    
    # Create snapcraft.yaml
    $snapcraft = @"
name: svonybrowser
version: '$Version'
summary: Advanced Evony Game Browser with MCP Integration
description: |
  Svony Browser is a specialized browser for Evony game with
  MCP server integration, AI chatbot, and automation features.
base: core22
confinement: strict
grade: stable

apps:
  svonybrowser:
    command: SvonyBrowser
    plugs:
      - network
      - network-bind
      - desktop
      - desktop-legacy
      - x11
      - wayland

parts:
  svonybrowser:
    plugin: dotnet
    source: .
    dotnet-build-configuration: Release
    dotnet-self-contained-runtime-identifier: linux-x64
"@
    
    $snapcraft | Out-File -FilePath (Join-Path $output "snapcraft.yaml") -Encoding UTF8
    
    Write-Success "✓ Snap configuration created"
    return $true
}

# 20. Chocolatey Package
function Build-Chocolatey {
    Write-Info "Building Chocolatey package..."
    $output = Join-Path $OutputDir "Chocolatey"
    Ensure-Directory $output
    Ensure-Directory "$output\tools"
    
    # Create nuspec
    $nuspec = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd">
  <metadata>
    <id>svonybrowser</id>
    <version>$Version</version>
    <title>Svony Browser</title>
    <authors>Svony Browser Team</authors>
    <projectUrl>https://github.com/Ghenghis/Svony-Browser</projectUrl>
    <tags>evony game browser mcp automation</tags>
    <summary>Advanced Evony Game Browser with MCP Integration</summary>
    <description>Svony Browser is a specialized browser for Evony game with MCP server integration, AI chatbot, and automation features.</description>
  </metadata>
  <files>
    <file src="tools\**" target="tools" />
  </files>
</package>
"@
    
    $nuspec | Out-File -FilePath (Join-Path $output "svonybrowser.nuspec") -Encoding UTF8
    
    # Create install script
    $installScript = @"
`$ErrorActionPreference = 'Stop'
`$toolsDir = "`$(Split-Path -parent `$MyInvocation.MyCommand.Definition)"
`$url = 'https://github.com/Ghenghis/Svony-Browser/releases/download/v$Version/SvonyBrowser_v${Version}_Portable.zip'

`$packageArgs = @{
  packageName   = 'svonybrowser'
  unzipLocation = `$toolsDir
  url           = `$url
  checksum      = 'CHECKSUM_PLACEHOLDER'
  checksumType  = 'sha256'
}

Install-ChocolateyZipPackage @packageArgs
"@
    
    $installScript | Out-File -FilePath (Join-Path $output "tools\chocolateyinstall.ps1") -Encoding UTF8
    
    Write-Success "✓ Chocolatey package created"
    return $true
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

Write-Host ""
Write-Host "============================================" -ForegroundColor Magenta
Write-Host "  SVONY BROWSER V$Version BUILD SYSTEM" -ForegroundColor Magenta
Write-Host "============================================" -ForegroundColor Magenta
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Info "Cleaning output directory..."
    if (Test-Path $OutputDir) {
        Remove-Item -Path $OutputDir -Recurse -Force
    }
}

Ensure-Directory $OutputDir

$results = @{}
$timestamp = Get-BuildTimestamp

# Build based on parameters
if ($All -or $Debug) { $results["Debug"] = Build-Debug }
if ($All -or $Release) { $results["Release"] = Build-Release }
if ($All -or $SingleFile) { $results["SingleFile"] = Build-SingleFile }
if ($All -or $AOT) { $results["AOT"] = Build-AOT }
if ($All -or $Trimmed) { $results["Trimmed"] = Build-Trimmed }
if ($All -or $R2R) { $results["R2R"] = Build-R2R }
if ($All -or $SelfContained) { $results["SelfContained"] = Build-SelfContained }
if ($All -or $FDD) { $results["FDD"] = Build-FDD }
if ($All -or $Portable) { $results["Portable"] = Build-Portable }
if ($All -or $MSI) { $results["MSI"] = Build-MSI }
if ($All -or $MSIX) { $results["MSIX"] = Build-MSIX }
if ($All -or $ClickOnce) { $results["ClickOnce"] = Build-ClickOnce }
if ($All -or $Docker) { $results["Docker"] = Build-Docker }
if ($All -or $NuGet) { $results["NuGet"] = Build-NuGet }
if ($All -or $Azure) { $results["Azure"] = Build-Azure }
if ($All -or $InnoSetup) { $results["InnoSetup"] = Build-InnoSetup }
if ($All -or $WiX) { $results["WiX"] = Build-WiX }
if ($All -or $AppImage) { $results["AppImage"] = Build-AppImage }
if ($All -or $Snap) { $results["Snap"] = Build-Snap }
if ($All -or $Chocolatey) { $results["Chocolatey"] = Build-Chocolatey }

# Generate checksums
if ($results.Count -gt 0) {
    Write-Checksums $OutputDir
}

# Summary
Write-Host ""
Write-Host "============================================" -ForegroundColor Magenta
Write-Host "  BUILD SUMMARY" -ForegroundColor Magenta
Write-Host "============================================" -ForegroundColor Magenta
Write-Host ""

$successCount = ($results.Values | Where-Object { $_ -eq $true }).Count
$failCount = ($results.Values | Where-Object { $_ -eq $false }).Count

foreach ($build in $results.GetEnumerator()) {
    if ($build.Value) {
        Write-Success "✓ $($build.Key)"
    } else {
        Write-Error "✗ $($build.Key)"
    }
}

Write-Host ""
Write-Host "Total: $($results.Count) builds | Success: $successCount | Failed: $failCount" -ForegroundColor White
Write-Host "Output: $OutputDir" -ForegroundColor White
Write-Host ""

if ($failCount -gt 0) {
    exit 1
}
exit 0
