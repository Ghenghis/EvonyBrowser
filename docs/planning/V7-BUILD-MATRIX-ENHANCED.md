# 🎯 V7.0 BUILD MATRIX ENHANCED - 20+ BUILD CONFIGURATIONS

**EVERY POSSIBLE BUILD TYPE - NO GAPS - COMPLETE COVERAGE**

---

## 📊 COMPLETE BUILD MATRIX - 20 CONFIGURATIONS

| #   | Build Type                 | Configuration | Platform  | Framework        | Output                 | Use Case             |
| --- | -------------------------- | ------------- | --------- | ---------------- | ---------------------- | -------------------- |
| 1   | **Development Debug**      | Debug         | x64       | net6.0-windows   | /bin/Debug             | Local development    |
| 2   | **Production Release**     | Release       | x64       | net6.0-windows   | /bin/Release           | Distribution         |
| 3   | **Single File Executable** | Release       | x64       | net6.0-windows   | /publish/single        | No .NET required     |
| 4   | **AOT Compiled**           | Release       | x64       | net7.0-windows   | /publish/aot           | Fast startup         |
| 5   | **Trimmed Build**          | Release       | x64       | net6.0-windows   | /publish/trimmed       | Smaller size         |
| 6   | **Ready To Run**           | Release       | x64       | net6.0-windows   | /publish/r2r           | Optimized JIT        |
| 7   | **Self-Contained**         | Release       | x64       | net6.0-windows   | /publish/selfcontained | Includes runtime     |
| 8   | **Framework-Dependent**    | Release       | x64       | net6.0-windows   | /publish/fdd           | Requires .NET        |
| 9   | **Portable ZIP**           | Release       | AnyCPU    | net6.0-windows   | /portable              | Extract & run        |
| 10  | **MSI Installer**          | Release       | x64       | net6.0-windows   | /installer/msi         | Windows Installer    |
| 11  | **MSIX Package**           | Release       | x64       | net6.0-windows10 | /package/msix          | Store ready          |
| 12  | **ClickOnce**              | Release       | x64       | net6.0-windows   | /clickonce             | Auto-update          |
| 13  | **Docker Container**       | Release       | linux-x64 | net6.0           | /docker                | Containerized        |
| 14  | **NuGet Package**          | Release       | AnyCPU    | netstandard2.0   | /nuget                 | Library distribution |
| 15  | **Azure Package**          | Release       | x64       | net6.0           | /azure                 | Cloud deployment     |
| 16  | **InnoSetup**              | Release       | x64       | net6.0-windows   | /installer/inno        | Custom installer     |
| 17  | **WiX Installer**          | Release       | x64       | net6.0-windows   | /installer/wix         | Enterprise MSI       |
| 18  | **Portable AppImage**      | Release       | linux-x64 | net6.0           | /appimage              | Linux portable       |
| 19  | **Snap Package**           | Release       | linux-x64 | net6.0           | /snap                  | Ubuntu Store         |
| 20  | **Chocolatey Package**     | Release       | x64       | net6.0-windows   | /choco                 | Package manager      |

---

## 🔨 BUILD COMMANDS FOR ALL 20 TYPES

### 1. Development Debug Build
```powershell
msbuild SvonyBrowser.csproj `
  /t:Build `
  /p:Configuration=Debug `
  /p:Platform=x64 `
  /p:DebugType=full `
  /p:DebugSymbols=true `
  /p:DefineConstants="DEBUG;TRACE;WINDOWS"
```

### 2. Production Release Build
```powershell
msbuild SvonyBrowser.csproj `
  /t:Build `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:Optimize=true `
  /p:DebugType=none
```

### 3. Single File Executable
```powershell
dotnet publish SvonyBrowser.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -o publish/single
```

### 4. AOT Compiled (NET7+)
```powershell
dotnet publish SvonyBrowser.csproj `
  -c Release `
  -r win-x64 `
  -p:PublishAot=true `
  -p:StripSymbols=true `
  -p:IlcOptimizationPreference=Speed `
  -o publish/aot
```

### 5. Trimmed Build
```powershell
dotnet publish SvonyBrowser.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishTrimmed=true `
  -p:TrimMode=link `
  -p:TrimmerRemoveSymbols=true `
  -o publish/trimmed
```

### 6. Ready To Run (R2R)
```powershell
dotnet publish SvonyBrowser.csproj `
  -c Release `
  -r win-x64 `
  -p:PublishReadyToRun=true `
  -p:PublishReadyToRunComposite=true `
  -p:PublishReadyToRunShowWarnings=true `
  -o publish/r2r
```

### 7. Self-Contained Deployment
```powershell
dotnet publish SvonyBrowser.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishDir=publish/selfcontained `
  -p:UseAppHost=true
```

### 8. Framework-Dependent Deployment
```powershell
dotnet publish SvonyBrowser.csproj `
  -c Release `
  --no-self-contained `
  -p:PublishDir=publish/fdd `
  -p:UseAppHost=true
```

### 9. Portable ZIP Archive
```powershell
# Build portable version
msbuild SvonyBrowser.csproj /t:Publish /p:Configuration=Release /p:Portable=true

# Create ZIP
Compress-Archive -Path bin\Release\net6.0-windows\* `
  -DestinationPath portable\SvonyBrowser-Portable.zip `
  -CompressionLevel Optimal
```

### 10. MSI Installer
```xml
<!-- Setup.wixproj -->
<Project Sdk="WixToolset.Sdk/4.0.0">
  <PropertyGroup>
    <OutputType>Package</OutputType>
    <DefineSolutionProperties>false</DefineSolutionProperties>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="SvonyBrowser.csproj" />
  </ItemGroup>
</Project>
```
```powershell
msbuild Setup.wixproj /t:Build /p:Configuration=Release
```

### 11. MSIX Package (Windows Store)
```xml
<!-- Package.appxmanifest -->
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10">
  <Identity Name="SvonyBrowser" Publisher="CN=ShadowByte" Version="7.0.0.0" />
  <Properties>
    <DisplayName>Svony Browser</DisplayName>
    <PublisherDisplayName>ShadowByte Labs</PublisherDisplayName>
  </Properties>
</Package>
```
```powershell
msbuild SvonyBrowser.csproj `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:AppxPackage=true `
  /p:AppxPackageDir=package\msix
```

### 12. ClickOnce Deployment
```powershell
msbuild SvonyBrowser.csproj `
  /t:Publish `
  /p:PublishUrl="http://myserver/svonybrowser/" `
  /p:InstallUrl="http://myserver/svonybrowser/" `
  /p:PublishProtocol=ClickOnce `
  /p:UpdateEnabled=true `
  /p:UpdateMode=Foreground
```

### 13. Docker Container
```dockerfile
# Dockerfile.production
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY ["SvonyBrowser.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SvonyBrowser.dll"]
```
```bash
docker build -t svony:7.0.0 -f Dockerfile.production .
docker save svony:7.0.0 -o docker/svony-7.0.0.tar
```

### 14. NuGet Package
```xml
<!-- SvonyBrowser.csproj additions -->
<PropertyGroup>
  <PackageId>SvonyBrowser</PackageId>
  <Version>7.0.0</Version>
  <Authors>ShadowByte</Authors>
  <Company>ShadowByte Labs</Company>
  <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  <PackageOutputPath>./nuget</PackageOutputPath>
</PropertyGroup>
```
```powershell
dotnet pack SvonyBrowser.csproj `
  -c Release `
  -p:PackageVersion=7.0.0 `
  -o nuget
```

### 15. Azure Web App Package
```powershell
msbuild SvonyBrowser.csproj `
  /p:DeployOnBuild=true `
  /p:PublishProfile=Azure `
  /p:WebPublishMethod=Package `
  /p:PackageAsSingleFile=true `
  /p:SkipInvalidConfigurations=true `
  /p:PackageLocation=azure\SvonyBrowser.zip
```

### 16. InnoSetup Installer
```iss
; SvonyBrowser.iss
[Setup]
AppName=Svony Browser
AppVersion=7.0.0
DefaultDirName={pf}\SvonyBrowser
DefaultGroupName=Svony Browser
UninstallDisplayIcon={app}\SvonyBrowser.exe
Compression=lzma2
SolidCompression=yes
OutputDir=installer\inno
OutputBaseFilename=SvonyBrowser-Setup

[Files]
Source: "bin\Release\net6.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Svony Browser"; Filename: "{app}\SvonyBrowser.exe"
Name: "{commondesktop}\Svony Browser"; Filename: "{app}\SvonyBrowser.exe"

[Run]
Filename: "{app}\SvonyBrowser.exe"; Description: "Launch Svony Browser"; Flags: nowait postinstall skipifsilent
```
```powershell
iscc SvonyBrowser.iss
```

### 17. WiX Installer (Enterprise)
```xml
<!-- Product.wxs -->
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" Name="Svony Browser" Language="1033" Version="7.0.0.0" 
           Manufacturer="ShadowByte Labs" UpgradeCode="PUT-GUID-HERE">
    <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />
    <MajorUpgrade DowngradeErrorMessage="Newer version installed." />
    <MediaTemplate EmbedCab="yes" />
    
    <Feature Id="ProductFeature" Title="Svony Browser" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
    </Feature>
  </Product>
</Wix>
```
```powershell
candle Product.wxs
light Product.wixobj -o installer\wix\SvonyBrowser.msi
```

### 18. Linux AppImage
```bash
# Create AppDir structure
mkdir -p SvonyBrowser.AppDir/usr/bin
cp -r bin/Release/net6.0/linux-x64/publish/* SvonyBrowser.AppDir/usr/bin/

# Create desktop entry
cat > SvonyBrowser.AppDir/SvonyBrowser.desktop <<EOF
[Desktop Entry]
Type=Application
Name=Svony Browser
Exec=SvonyBrowser
Icon=svony
Categories=Network;
EOF

# Create AppImage
appimagetool SvonyBrowser.AppDir appimage/SvonyBrowser-7.0.0-x86_64.AppImage
```

### 19. Snap Package (Ubuntu)
```yaml
# snapcraft.yaml
name: svony-browser
version: '7.0.0'
summary: AI-Powered Evony Browser
description: |
  Dual-panel browser with AI integration for Evony

base: core20
confinement: strict

apps:
  svony-browser:
    command: bin/SvonyBrowser
    plugs: [network, browser-support, x11, opengl]

parts:
  svony:
    plugin: dotnet
    source: .
    dotnet-version: '6.0'
```
```bash
snapcraft
mv svony-browser_7.0.0_amd64.snap snap/
```

### 20. Chocolatey Package
```powershell
# svonybrowser.nuspec
<?xml version="1.0" encoding="utf-8"?>
<package>
  <metadata>
    <id>svonybrowser</id>
    <version>7.0.0</version>
    <title>Svony Browser</title>
    <authors>ShadowByte</authors>
    <projectUrl>https://github.com/Ghenghis/Svony-Browser</projectUrl>
    <description>AI-Powered Dual-Panel Evony Browser</description>
  </metadata>
  <files>
    <file src="bin\Release\net6.0-windows\**" target="tools" />
  </files>
</package>
```
```powershell
choco pack svonybrowser.nuspec
mv svonybrowser.7.0.0.nupkg choco/
```

---

## 🎮 PLATFORM-SPECIFIC BUILDS

### Windows ARM64
```powershell
msbuild /p:Configuration=Release /p:Platform=ARM64 /p:RuntimeIdentifier=win-arm64
```

### macOS Universal Binary
```bash
dotnet publish -c Release -r osx-x64 -o publish/osx-x64
dotnet publish -c Release -r osx-arm64 -o publish/osx-arm64
lipo -create publish/osx-x64/SvonyBrowser publish/osx-arm64/SvonyBrowser -output publish/SvonyBrowser-universal
```

### Linux Multi-Architecture
```bash
# x64
dotnet publish -c Release -r linux-x64 -o publish/linux-x64

# ARM64
dotnet publish -c Release -r linux-arm64 -o publish/linux-arm64

# ARM32
dotnet publish -c Release -r linux-arm -o publish/linux-arm

# MUSL (Alpine)
dotnet publish -c Release -r linux-musl-x64 -o publish/linux-musl-x64
```

---

## 📦 BUILD PROFILES

### Minimum Size Profile
```xml
<PropertyGroup Condition="'$(Configuration)'=='MinSize'">
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>link</TrimMode>
  <PublishSingleFile>true</PublishSingleFile>
  <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
  <DebugType>none</DebugType>
  <DebugSymbols>false</DebugSymbols>
  <StripSymbols>true</StripSymbols>
</PropertyGroup>
```

### Maximum Performance Profile
```xml
<PropertyGroup Condition="'$(Configuration)'=='MaxPerf'">
  <PublishReadyToRun>true</PublishReadyToRun>
  <PublishReadyToRunComposite>true</PublishReadyToRunComposite>
  <TieredCompilation>false</TieredCompilation>
  <TieredCompilationQuickJit>false</TieredCompilationQuickJit>
  <Optimize>true</Optimize>
  <IlcOptimizationPreference>Speed</IlcOptimizationPreference>
</PropertyGroup>
```

### Maximum Compatibility Profile
```xml
<PropertyGroup Condition="'$(Configuration)'=='MaxCompat'">
  <TargetFramework>net462</TargetFramework>
  <PlatformTarget>AnyCPU</PlatformTarget>
  <Prefer32Bit>false</Prefer32Bit>
  <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  <Deterministic>false</Deterministic>
</PropertyGroup>
```

---

## 🚀 AUTOMATED BUILD SCRIPT FOR ALL 20 TYPES

```powershell
# build-all-types.ps1
param(
    [switch]$All,
    [int[]]$BuildNumbers = @()
)

$buildConfigs = @(
    @{Num=1; Name="Debug"; Cmd="msbuild /t:Build /p:Configuration=Debug"},
    @{Num=2; Name="Release"; Cmd="msbuild /t:Build /p:Configuration=Release"},
    @{Num=3; Name="SingleFile"; Cmd="dotnet publish -p:PublishSingleFile=true"},
    @{Num=4; Name="AOT"; Cmd="dotnet publish -p:PublishAot=true"},
    @{Num=5; Name="Trimmed"; Cmd="dotnet publish -p:PublishTrimmed=true"},
    @{Num=6; Name="R2R"; Cmd="dotnet publish -p:PublishReadyToRun=true"},
    @{Num=7; Name="SelfContained"; Cmd="dotnet publish --self-contained"},
    @{Num=8; Name="FrameworkDep"; Cmd="dotnet publish --no-self-contained"},
    @{Num=9; Name="PortableZIP"; Cmd={./scripts/build-portable.ps1}},
    @{Num=10; Name="MSI"; Cmd="msbuild Setup.wixproj"},
    @{Num=11; Name="MSIX"; Cmd="msbuild /p:AppxPackage=true"},
    @{Num=12; Name="ClickOnce"; Cmd="msbuild /t:Publish /p:PublishProtocol=ClickOnce"},
    @{Num=13; Name="Docker"; Cmd="docker build -t svony:7.0.0 ."},
    @{Num=14; Name="NuGet"; Cmd="dotnet pack"},
    @{Num=15; Name="Azure"; Cmd="msbuild /p:DeployOnBuild=true"},
    @{Num=16; Name="InnoSetup"; Cmd="iscc SvonyBrowser.iss"},
    @{Num=17; Name="WiX"; Cmd="candle Product.wxs && light Product.wixobj"},
    @{Num=18; Name="AppImage"; Cmd="./scripts/build-appimage.sh"},
    @{Num=19; Name="Snap"; Cmd="snapcraft"},
    @{Num=20; Name="Chocolatey"; Cmd="choco pack"}
)

# Determine which builds to run
$toBuild = if ($All) { $buildConfigs } 
           elseif ($BuildNumbers) { $buildConfigs | Where-Object { $_.Num -in $BuildNumbers } }
           else { $buildConfigs[0..2] } # Default to first 3

# Execute builds
$results = @()
foreach ($build in $toBuild) {
    Write-Host "Building #$($build.Num): $($build.Name)" -ForegroundColor Cyan
    
    try {
        if ($build.Cmd -is [ScriptBlock]) {
            & $build.Cmd
        } else {
            Invoke-Expression $build.Cmd
        }
        
        $results += [PSCustomObject]@{
            Number = $build.Num
            Name = $build.Name
            Status = "SUCCESS"
            Time = Get-Date
        }
        Write-Host "✓ $($build.Name) completed" -ForegroundColor Green
    }
    catch {
        $results += [PSCustomObject]@{
            Number = $build.Num
            Name = $build.Name
            Status = "FAILED"
            Error = $_.Exception.Message
        }
        Write-Host "✗ $($build.Name) failed: $_" -ForegroundColor Red
    }
}

# Generate report
$results | Format-Table -AutoSize
$results | Export-Csv "build-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').csv"

Write-Host "`nBuild Summary:" -ForegroundColor Yellow
Write-Host "Success: $(($results | Where-Object Status -eq 'SUCCESS').Count)" -ForegroundColor Green
Write-Host "Failed: $(($results | Where-Object Status -eq 'FAILED').Count)" -ForegroundColor Red
```

---

## ✅ BUILD VERIFICATION MATRIX

| Build # | Verification Command                                     | Expected Result       |
| ------- | -------------------------------------------------------- | --------------------- |
| 1       | `Test-Path bin\Debug\*.pdb`                              | Debug symbols present |
| 2       | `(Get-Item bin\Release\SvonyBrowser.exe).Length -lt 1MB` | Optimized size        |
| 3       | `Test-Path publish\single\SvonyBrowser.exe`              | Single file exists    |
| 4       | `.\publish\aot\SvonyBrowser.exe --version`               | Runs without .NET     |
| 5       | `(Get-ChildItem publish\trimmed).Count -lt 50`           | Fewer files           |
| 6       | `Measure-Command { .\publish\r2r\SvonyBrowser.exe }`     | Fast startup          |
| 7       | `Test-Path publish\selfcontained\dotnet.dll`             | Runtime included      |
| 8       | `Test-Path publish\fdd\dotnet.dll`                       | Runtime NOT included  |
| 9       | `Expand-Archive portable\*.zip -Force`                   | Extracts successfully |
| 10      | `msiexec /i installer\*.msi /quiet`                      | Installs silently     |
| 11      | `Get-AppxPackage SvonyBrowser`                           | Package registered    |
| 12      | `Test-Path clickonce\*.application`                      | Manifest created      |
| 13      | `docker images svony:7.0.0`                              | Image exists          |
| 14      | `nuget list source local`                                | Package listed        |
| 15      | `Test-Path azure\*.zip`                                  | Azure package created |
| 16      | `Test-Path installer\inno\*.exe`                         | Setup.exe created     |
| 17      | `Test-Path installer\wix\*.msi`                          | MSI created           |
| 18      | `file appimage/*.AppImage`                               | Linux executable      |
| 19      | `snap list svony-browser`                                | Snap installed        |
| 20      | `choco list svonybrowser`                                | Package available     |

---

**20+ BUILD TYPES - COMPLETE MATRIX - ZERO GAPS**
