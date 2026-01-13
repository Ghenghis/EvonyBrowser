# 🎯 V7.0 BUILD MATRIX - EVERY CONFIGURATION

**ALL POSSIBLE BUILD TYPES, PLATFORMS, FRAMEWORKS - NO AMBIGUITY**

---

## 📊 COMPLETE BUILD MATRIX

| Build Type      | Configuration | Platform  | Framework      | Runtime        | Output       | Size   | Use Case       |
| --------------- | ------------- | --------- | -------------- | -------------- | ------------ | ------ | -------------- |
| **Development** | Debug         | x64       | net6.0-windows | Dependent      | /bin/Debug   | ~300MB | Local dev      |
| **Production**  | Release       | x64       | net6.0-windows | Dependent      | /bin/Release | ~250MB | Distribution   |
| **Portable**    | Release       | x64       | net6.0-windows | Self-contained | /publish     | ~450MB | No .NET needed |
| **Installer**   | Release       | x64       | net6.0-windows | Dependent      | /installer   | ~250MB | MSI/Setup.exe  |
| **Docker**      | Release       | linux-x64 | net6.0         | Self-contained | /docker      | ~400MB | Container      |
| **CI/CD**       | Release       | AnyCPU    | net6.0-windows | Dependent      | /artifacts   | ~250MB | Automation     |
| **Test**        | Debug         | x64       | net6.0-windows | Dependent      | /test-output | ~350MB | Testing        |
| **Benchmark**   | Release       | x64       | net6.0-windows | Dependent      | /benchmark   | ~250MB | Performance    |

---

## 🔨 BUILD COMMANDS FOR EACH TYPE

### 1. DEVELOPMENT BUILD
```xml
<!-- Configuration: Debug | Platform: x64 -->
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">
  <DefineConstants>DEBUG;TRACE;WINDOWS</DefineConstants>
  <DebugType>full</DebugType>
  <DebugSymbols>true</DebugSymbols>
  <Optimize>false</Optimize>
  <OutputPath>bin\Debug\net6.0-windows\</OutputPath>
  <PlatformTarget>x64</PlatformTarget>
  <WarningLevel>4</WarningLevel>
  <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
</PropertyGroup>
```

**Build Command:**
```powershell
msbuild SvonyBrowser.csproj `
  /p:Configuration=Debug `
  /p:Platform=x64 `
  /p:DefineConstants="DEBUG;TRACE;WINDOWS" `
  /p:DebugType=full `
  /p:DebugSymbols=true `
  /p:Optimize=false
```

---

### 2. PRODUCTION BUILD
```xml
<!-- Configuration: Release | Platform: x64 -->
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'">
  <DefineConstants>RELEASE;WINDOWS</DefineConstants>
  <DebugType>none</DebugType>
  <DebugSymbols>false</DebugSymbols>
  <Optimize>true</Optimize>
  <OutputPath>bin\Release\net6.0-windows\</OutputPath>
  <PlatformTarget>x64</PlatformTarget>
  <WarningLevel>4</WarningLevel>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

**Build Command:**
```powershell
msbuild SvonyBrowser.csproj `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:DefineConstants="RELEASE;WINDOWS" `
  /p:DebugType=none `
  /p:DebugSymbols=false `
  /p:Optimize=true `
  /p:TreatWarningsAsErrors=true
```

---

### 3. PORTABLE BUILD (Self-Contained)
```xml
<!-- Publish Profile: Portable.pubxml -->
<Project>
  <PropertyGroup>
    <Configuration>Release</Configuration>
    <Platform>x64</Platform>
    <PublishDir>bin\Portable\</PublishDir>
    <PublishProtocol>FileSystem</PublishProtocol>
    <TargetFramework>net6.0-windows</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <PublishReadyToRun>true</PublishReadyToRun>
    <PublishTrimmed>false</PublishTrimmed>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  </PropertyGroup>
</Project>
```

**Build Command:**
```powershell
dotnet publish SvonyBrowser.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishReadyToRun=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o bin\Portable
```

---

### 4. INSTALLER BUILD (For WiX/InnoSetup)
```xml
<!-- Configuration: Installer | Platform: x64 -->
<PropertyGroup Condition="'$(Configuration)'=='Installer'">
  <OutputPath>bin\Installer\</OutputPath>
  <IntermediateOutputPath>obj\Installer\</IntermediateOutputPath>
  <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  <PackageId>SvonyBrowser</PackageId>
  <PackageVersion>7.0.0</PackageVersion>
  <Authors>ShadowByte</Authors>
  <Product>Svony Browser</Product>
  <Description>AI-Powered Evony Browser</Description>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageProjectUrl>https://github.com/Ghenghis/Svony-Browser</PackageProjectUrl>
</PropertyGroup>
```

**Build Command:**
```powershell
msbuild SvonyBrowser.csproj `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:OutputPath=bin\Installer\ `
  /p:GeneratePackageOnBuild=true `
  /p:PackageVersion=7.0.0
```

**InnoSetup Script:**
```iss
[Setup]
AppName=Svony Browser
AppVersion=7.0.0
DefaultDirName={pf}\SvonyBrowser
DefaultGroupName=Svony Browser
OutputDir=installer-output
OutputBaseFilename=SvonyBrowser-Setup-7.0.0
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "bin\Release\net6.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Svony Browser"; Filename: "{app}\SvonyBrowser.exe"
Name: "{commondesktop}\Svony Browser"; Filename: "{app}\SvonyBrowser.exe"

[Run]
Filename: "{app}\SvonyBrowser.exe"; Description: "Launch Svony Browser"; Flags: nowait postinstall skipifsilent
```

---

### 5. DOCKER BUILD (Linux Container)
```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SvonyBrowser/SvonyBrowser.csproj", "SvonyBrowser/"]
RUN dotnet restore "SvonyBrowser/SvonyBrowser.csproj"
COPY . .
WORKDIR "/src/SvonyBrowser"
RUN dotnet build "SvonyBrowser.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SvonyBrowser.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SvonyBrowser.dll"]
```

**Build Command:**
```bash
docker build -t svony-browser:7.0.0 .
docker run -d -p 8080:80 svony-browser:7.0.0
```

---

### 6. CI/CD BUILD (GitHub Actions)
```yaml
# .github/workflows/build-matrix.yml
name: Build Matrix

on: [push, pull_request]

strategy:
  matrix:
    configuration: [Debug, Release]
    platform: [x64, x86]
    os: [windows-2019, windows-2022]

jobs:
  build:
    runs-on: ${{ matrix.os }}
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: |
          6.0.x
          8.0.x
    
    - name: Setup MSBuild
      uses: microsoft/setup-msbuild@v1.3
    
    - name: Restore
      run: msbuild /t:Restore /p:Configuration=${{ matrix.configuration }}
    
    - name: Build
      run: |
        msbuild SvonyBrowser.csproj `
          /p:Configuration=${{ matrix.configuration }} `
          /p:Platform=${{ matrix.platform }} `
          /p:Version=7.0.${{ github.run_number }}
    
    - name: Upload Artifacts
      uses: actions/upload-artifact@v3
      with:
        name: build-${{ matrix.os }}-${{ matrix.configuration }}-${{ matrix.platform }}
        path: |
          bin/${{ matrix.configuration }}/net6.0-windows/
```

---

### 7. TEST BUILD
```xml
<!-- Test Configuration -->
<PropertyGroup Condition="'$(Configuration)'=='Test'">
  <DefineConstants>DEBUG;TEST;WINDOWS</DefineConstants>
  <DebugType>full</DebugType>
  <DebugSymbols>true</DebugSymbols>
  <Optimize>false</Optimize>
  <OutputPath>bin\Test\</OutputPath>
  <EnableCoverage>true</EnableCoverage>
  <CollectCoverage>true</CollectCoverage>
  <CoverletOutputFormat>opencover</CoverletOutputFormat>
</PropertyGroup>
```

**Build & Test Command:**
```powershell
# Build test version
msbuild SvonyBrowser.csproj /p:Configuration=Test

# Run tests with coverage
dotnet test SvonyBrowser.Tests.csproj `
  --configuration Test `
  --collect:"XPlat Code Coverage" `
  --results-directory TestResults `
  --logger "trx;LogFileName=test-results.trx" `
  --logger "html;LogFileName=test-results.html" `
  /p:CollectCoverage=true `
  /p:CoverletOutputFormat=opencover `
  /p:CoverletOutput=./TestResults/coverage.xml
```

---

### 8. BENCHMARK BUILD
```xml
<!-- Benchmark Configuration -->
<PropertyGroup Condition="'$(Configuration)'=='Benchmark'">
  <DefineConstants>RELEASE;BENCHMARK;WINDOWS</DefineConstants>
  <DebugType>pdbonly</DebugType>
  <DebugSymbols>true</DebugSymbols>
  <Optimize>true</Optimize>
  <OutputPath>bin\Benchmark\</OutputPath>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  <TieredCompilation>false</TieredCompilation>
</PropertyGroup>
```

**Benchmark Command:**
```powershell
# Build for benchmarking
msbuild SvonyBrowser.csproj `
  /p:Configuration=Benchmark `
  /p:TieredCompilation=false `
  /p:Optimize=true

# Run benchmarks
dotnet run -c Benchmark --project SvonyBrowser.Benchmarks
```

---

## 🎮 PLATFORM CONFIGURATIONS

### Windows x64 (Primary)
```xml
<PropertyGroup Condition="'$(Platform)'=='x64'">
  <PlatformTarget>x64</PlatformTarget>
  <Prefer32Bit>false</Prefer32Bit>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
</PropertyGroup>
```

### Windows x86 (Legacy)
```xml
<PropertyGroup Condition="'$(Platform)'=='x86'">
  <PlatformTarget>x86</PlatformTarget>
  <Prefer32Bit>true</Prefer32Bit>
  <RuntimeIdentifier>win-x86</RuntimeIdentifier>
</PropertyGroup>
```

### Windows ARM64
```xml
<PropertyGroup Condition="'$(Platform)'=='ARM64'">
  <PlatformTarget>ARM64</PlatformTarget>
  <RuntimeIdentifier>win-arm64</RuntimeIdentifier>
</PropertyGroup>
```

### AnyCPU (CI/CD)
```xml
<PropertyGroup Condition="'$(Platform)'=='AnyCPU'">
  <PlatformTarget>AnyCPU</PlatformTarget>
  <Prefer32Bit>false</Prefer32Bit>
</PropertyGroup>
```

---

## 📁 OUTPUT STRUCTURE

```
D:\Fiddler-FlashBrowser\
├── bin\
│   ├── Debug\
│   │   └── net6.0-windows\
│   │       ├── SvonyBrowser.exe
│   │       ├── SvonyBrowser.dll
│   │       ├── SvonyBrowser.pdb
│   │       └── [200+ DLL files]
│   ├── Release\
│   │   └── net6.0-windows\
│   │       ├── SvonyBrowser.exe
│   │       ├── SvonyBrowser.dll
│   │       └── [200+ DLL files]
│   ├── Portable\
│   │   ├── SvonyBrowser.exe (450MB single file)
│   │   └── [Required external files]
│   ├── Installer\
│   │   └── [Files for installer]
│   ├── Test\
│   │   └── [Test output with coverage]
│   └── Benchmark\
│       └── [Benchmark results]
├── obj\
│   └── [Intermediate build files]
├── TestResults\
│   ├── coverage.xml
│   └── test-results.trx
└── packages\
    └── [NuGet packages]
```

---

## 🚀 MULTI-TARGET BUILD

```xml
<!-- Build for multiple frameworks -->
<PropertyGroup>
  <TargetFrameworks>net6.0-windows;net7.0-windows;net8.0-windows</TargetFrameworks>
</PropertyGroup>

<!-- Conditional compilation -->
<ItemGroup Condition="'$(TargetFramework)'=='net6.0-windows'">
  <PackageReference Include="CefSharp.Wpf" Version="119.4.30" />
</ItemGroup>

<ItemGroup Condition="'$(TargetFramework)'=='net8.0-windows'">
  <PackageReference Include="CefSharp.Wpf.NETCore" Version="119.4.30" />
</ItemGroup>
```

**Build All Targets:**
```powershell
# Build all framework targets
msbuild SvonyBrowser.csproj /p:TargetFrameworks="net6.0-windows;net7.0-windows;net8.0-windows"

# Publish each target
foreach ($framework in @("net6.0-windows", "net7.0-windows", "net8.0-windows")) {
    dotnet publish -f $framework -c Release -o "bin\$framework"
}
```

---

## ⚡ PERFORMANCE OPTIMIZATIONS

### AOT Compilation (net7.0+)
```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <InvariantGlobalization>false</InvariantGlobalization>
  <TrimMode>partial</TrimMode>
</PropertyGroup>
```

### ReadyToRun
```xml
<PropertyGroup>
  <PublishReadyToRun>true</PublishReadyToRun>
  <PublishReadyToRunShowWarnings>true</PublishReadyToRunShowWarnings>
</PropertyGroup>
```

### Trimming
```xml
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>link</TrimMode>
  <SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>
</PropertyGroup>
```

---

## 🎯 COMPLETE BUILD SCRIPT

```powershell
# build-all-configs.ps1
param(
    [switch]$All,
    [switch]$Clean
)

$configs = @(
    @{Name="Debug-x64"; Config="Debug"; Platform="x64"},
    @{Name="Release-x64"; Config="Release"; Platform="x64"},
    @{Name="Release-x86"; Config="Release"; Platform="x86"},
    @{Name="Portable"; Config="Release"; Platform="x64"; Publish=$true},
    @{Name="Test"; Config="Debug"; Platform="x64"; Test=$true}
)

$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

foreach ($build in $configs) {
    Write-Host "Building $($build.Name)..." -ForegroundColor Cyan
    
    if ($Clean) {
        & $msbuild SvonyBrowser.csproj /t:Clean /p:Configuration=$($build.Config) /p:Platform=$($build.Platform)
    }
    
    & $msbuild SvonyBrowser.csproj `
        /t:Restore,Build `
        /p:Configuration=$($build.Config) `
        /p:Platform=$($build.Platform) `
        /v:minimal
    
    if ($build.Publish) {
        dotnet publish -c $($build.Config) -r win-x64 --self-contained
    }
    
    if ($build.Test) {
        dotnet test --configuration $($build.Config)
    }
}

Write-Host "ALL BUILDS COMPLETE!" -ForegroundColor Green
```

---

**USE THIS MATRIX TO BUILD ANY CONFIGURATION**
