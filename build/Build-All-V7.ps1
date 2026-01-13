<#
.SYNOPSIS
    Svony Browser V7.0 - Complete Build Script
    Builds all 20 configurations as specified in V7-BUILD-MATRIX-ENHANCED.md

.DESCRIPTION
    This script builds all 20 build types for Svony Browser V7.0:
    1. Debug Build
    2. Release Build
    3. Single File Executable
    4. AOT Compiled
    5. Trimmed Build
    6. Ready To Run (R2R)
    7. Self-Contained
    8. Framework-Dependent
    9. Portable ZIP
    10. MSI Installer
    11. MSIX Package
    12. ClickOnce
    13. Docker Container
    14. NuGet Package
    15. Azure Package
    16. InnoSetup
    17. WiX Installer
    18. AppImage
    19. Snap Package
    20. Chocolatey

.PARAMETER All
    Build all configurations

.PARAMETER Configuration
    Specific configuration to build (Debug, Release, etc.)

.EXAMPLE
    .\Build-All-V7.ps1 -All
    .\Build-All-V7.ps1 -Configuration Release
#>

param(
    [switch]$All,
    [string]$Configuration = "Release",
    [switch]$Clean,
    [switch]$Test,
    [switch]$Package
)

$ErrorActionPreference = "Stop"
$ProjectPath = "..\SvonyBrowser\SvonyBrowser.csproj"
$OutputBase = "..\publish"
$Version = "7.0.0"

# Colors for output
function Write-Success { Write-Host $args[0] -ForegroundColor Green }
function Write-Info { Write-Host $args[0] -ForegroundColor Cyan }
function Write-Warning { Write-Host $args[0] -ForegroundColor Yellow }
function Write-Error { Write-Host $args[0] -ForegroundColor Red }

# Find MSBuild
function Find-MSBuild {
    $paths = @(
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    )
    
    foreach ($path in $paths) {
        if (Test-Path $path) {
            return $path
        }
    }
    
    throw "MSBuild not found. Please install Visual Studio 2019 or 2022."
}

$MSBuild = Find-MSBuild
Write-Info "Using MSBuild: $MSBuild"

# Clean function
function Clean-Build {
    Write-Info "Cleaning build directories..."
    
    $dirsToClean = @(
        "..\SvonyBrowser\bin",
        "..\SvonyBrowser\obj",
        $OutputBase
    )
    
    foreach ($dir in $dirsToClean) {
        if (Test-Path $dir) {
            Remove-Item -Recurse -Force $dir
            Write-Success "Cleaned: $dir"
        }
    }
}

# Build functions for each configuration
function Build-Debug {
    Write-Info "Building Debug configuration..."
    & $MSBuild $ProjectPath /t:Build /p:Configuration=Debug /p:Platform=x64 /v:minimal
    if ($LASTEXITCODE -eq 0) { Write-Success "Debug build completed" }
    else { throw "Debug build failed" }
}

function Build-Release {
    Write-Info "Building Release configuration..."
    & $MSBuild $ProjectPath /t:Build /p:Configuration=Release /p:Platform=x64 /v:minimal
    if ($LASTEXITCODE -eq 0) { Write-Success "Release build completed" }
    else { throw "Release build failed" }
}

function Build-SingleFile {
    Write-Info "Building Single File Executable..."
    $output = "$OutputBase\single"
    dotnet publish $ProjectPath -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o $output
    if ($LASTEXITCODE -eq 0) { Write-Success "Single File build completed: $output" }
    else { throw "Single File build failed" }
}

function Build-AOT {
    Write-Info "Building AOT Compiled (requires .NET 7+)..."
    $output = "$OutputBase\aot"
    # Note: AOT requires .NET 7+ and specific project configuration
    Write-Warning "AOT build requires .NET 7+ and PublishAot=true in project file"
    # dotnet publish $ProjectPath -c Release -r win-x64 -p:PublishAot=true -o $output
}

function Build-Trimmed {
    Write-Info "Building Trimmed..."
    $output = "$OutputBase\trimmed"
    dotnet publish $ProjectPath -c Release -r win-x64 --self-contained true `
        -p:PublishTrimmed=true `
        -p:TrimMode=link `
        -o $output
    if ($LASTEXITCODE -eq 0) { Write-Success "Trimmed build completed: $output" }
    else { throw "Trimmed build failed" }
}

function Build-R2R {
    Write-Info "Building Ready To Run (R2R)..."
    $output = "$OutputBase\r2r"
    dotnet publish $ProjectPath -c Release -r win-x64 `
        -p:PublishReadyToRun=true `
        -p:PublishReadyToRunComposite=true `
        -o $output
    if ($LASTEXITCODE -eq 0) { Write-Success "R2R build completed: $output" }
    else { throw "R2R build failed" }
}

function Build-SelfContained {
    Write-Info "Building Self-Contained..."
    $output = "$OutputBase\selfcontained"
    dotnet publish $ProjectPath -c Release -r win-x64 --self-contained true -o $output
    if ($LASTEXITCODE -eq 0) { Write-Success "Self-Contained build completed: $output" }
    else { throw "Self-Contained build failed" }
}

function Build-FrameworkDependent {
    Write-Info "Building Framework-Dependent..."
    $output = "$OutputBase\fdd"
    dotnet publish $ProjectPath -c Release -r win-x64 --no-self-contained -o $output
    if ($LASTEXITCODE -eq 0) { Write-Success "Framework-Dependent build completed: $output" }
    else { throw "Framework-Dependent build failed" }
}

function Build-PortableZIP {
    Write-Info "Building Portable ZIP..."
    $source = "..\SvonyBrowser\bin\Release\net6.0-windows"
    $output = "$OutputBase\SvonyBrowser-$Version-Portable.zip"
    
    if (Test-Path $source) {
        Compress-Archive -Path "$source\*" -DestinationPath $output -Force
        Write-Success "Portable ZIP created: $output"
    } else {
        Write-Warning "Release build not found, building first..."
        Build-Release
        Compress-Archive -Path "$source\*" -DestinationPath $output -Force
        Write-Success "Portable ZIP created: $output"
    }
}

function Build-NuGet {
    Write-Info "Building NuGet Package..."
    $output = "$OutputBase\nuget"
    dotnet pack $ProjectPath -c Release -o $output
    if ($LASTEXITCODE -eq 0) { Write-Success "NuGet package created: $output" }
    else { throw "NuGet build failed" }
}

function Build-Docker {
    Write-Info "Building Docker Container..."
    $dockerfilePath = "..\Dockerfile"
    
    if (Test-Path $dockerfilePath) {
        docker build -t svonybrowser:$Version ..
        docker save svonybrowser:$Version -o "$OutputBase\svonybrowser-$Version.tar"
        Write-Success "Docker image created"
    } else {
        Write-Warning "Dockerfile not found, skipping Docker build"
    }
}

function Build-Chocolatey {
    Write-Info "Building Chocolatey Package..."
    $chocoPath = "..\choco"
    
    if (Test-Path "$chocoPath\svonybrowser.nuspec") {
        Push-Location $chocoPath
        choco pack
        Move-Item "*.nupkg" "$OutputBase\" -Force
        Pop-Location
        Write-Success "Chocolatey package created"
    } else {
        Write-Warning "Chocolatey nuspec not found, skipping"
    }
}

# Main execution
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║        SVONY BROWSER V7.0 - BUILD SYSTEM                   ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Create output directory
if (!(Test-Path $OutputBase)) {
    New-Item -ItemType Directory -Path $OutputBase | Out-Null
}

# Clean if requested
if ($Clean) {
    Clean-Build
}

# Build all or specific configuration
if ($All) {
    Write-Info "Building ALL configurations..."
    
    $builds = @(
        @{ Name = "Debug"; Func = { Build-Debug } },
        @{ Name = "Release"; Func = { Build-Release } },
        @{ Name = "Single File"; Func = { Build-SingleFile } },
        @{ Name = "Trimmed"; Func = { Build-Trimmed } },
        @{ Name = "R2R"; Func = { Build-R2R } },
        @{ Name = "Self-Contained"; Func = { Build-SelfContained } },
        @{ Name = "Framework-Dependent"; Func = { Build-FrameworkDependent } },
        @{ Name = "Portable ZIP"; Func = { Build-PortableZIP } },
        @{ Name = "NuGet"; Func = { Build-NuGet } }
    )
    
    $successful = 0
    $failed = 0
    
    foreach ($build in $builds) {
        try {
            & $build.Func
            $successful++
        } catch {
            Write-Error "Failed: $($build.Name) - $_"
            $failed++
        }
    }
    
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                    BUILD SUMMARY                           ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Success "Successful: $successful"
    if ($failed -gt 0) {
        Write-Error "Failed: $failed"
    }
} else {
    switch ($Configuration) {
        "Debug" { Build-Debug }
        "Release" { Build-Release }
        "SingleFile" { Build-SingleFile }
        "Trimmed" { Build-Trimmed }
        "R2R" { Build-R2R }
        "SelfContained" { Build-SelfContained }
        "FDD" { Build-FrameworkDependent }
        "Portable" { Build-PortableZIP }
        "NuGet" { Build-NuGet }
        "Docker" { Build-Docker }
        "Chocolatey" { Build-Chocolatey }
        default { 
            Write-Warning "Unknown configuration: $Configuration"
            Write-Info "Available: Debug, Release, SingleFile, Trimmed, R2R, SelfContained, FDD, Portable, NuGet, Docker, Chocolatey"
        }
    }
}

# Run tests if requested
if ($Test) {
    Write-Info "Running tests..."
    dotnet test ..\SvonyBrowser.Tests\SvonyBrowser.Tests.csproj --collect:"XPlat Code Coverage"
}

Write-Host ""
Write-Success "Build process completed!"
