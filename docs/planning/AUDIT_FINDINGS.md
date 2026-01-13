# Svony Browser CefSharp Audit Findings

## Issue Summary
**Error**: "Could not load file or assembly 'CefSharp.Core.Runtime, Version=119.4.30.0'"

## Key Findings from CefSharp GitHub Issues

### Finding 1: SelfContained builds require special handling
From GitHub Issue #4295:
- The main CefSharp libs are compiled as `.NET Core 3.1`
- `CefSharp.BrowserSubprocess.exe` is shipped as `.NET Core 3.1` for x64/x86
- For SelfContained builds, the `CefSharp.BrowserSubprocess.runtimeconfig.json` file must be DELETED
- The RollForward mechanism doesn't work properly with SelfContained builds

### Finding 2: CefSharp.Core.Runtime is a native DLL
- `CefSharp.Core.Runtime.dll` is a native C++/CLI assembly
- It's located in `runtimes/win-x64/native/` in the NuGet package
- It must be in the same directory as the executable at runtime

### Finding 3: Required files for CefSharp deployment
The following files MUST be present in the output directory:
1. `CefSharp.Core.Runtime.dll` (native)
2. `CefSharp.Core.dll` (managed)
3. `CefSharp.dll` (managed)
4. `CefSharp.Wpf.dll` (managed)
5. `CefSharp.BrowserSubprocess.exe`
6. `libcef.dll` (~200MB native CEF library)
7. All other CEF native files (icudtl.dat, resources, locales, etc.)

### Finding 4: PublishSingleFile is NOT supported
- CefSharp cannot be published as a single file
- Native DLLs must remain as separate files
- Use `PublishSingleFile=false`

## Root Cause Analysis

The current release workflow uses:
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
```

The problem is that the native files from `runtimes/win-x64/native/` are not being copied to the output directory.

## Solution Options

### Option A: Use dotnet publish with proper settings
```xml
<PropertyGroup>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <SelfContained>true</SelfContained>
  <PublishSingleFile>false</PublishSingleFile>
</PropertyGroup>
```

### Option B: Manually copy native files in workflow
Add a step to copy files from `runtimes/win-x64/native/` to the publish output.

### Option C: Use CefSharp.Common.NETCore targets
The NuGet package includes MSBuild targets that should copy files automatically.
Check if `CefSharpTargetDir` is set correctly.

## Next Steps
1. Check the project file for CefSharp-related MSBuild properties
2. Verify the NuGet package restore includes all native files
3. Add explicit file copy steps to the workflow
4. Test locally before pushing to GitHub


## Critical Finding: Self-Hosting BrowserSubprocess is REQUIRED

From GitHub Issue #3407 (CefSharp maintainer amaitland):

> **For publishing self contained and single file applications self hosting the BrowserSubprocess is required.**

### The Solution for WPF Self-Contained Apps

The main application must self-host the BrowserSubprocess. Here's the pattern:

```csharp
[STAThread]
public static int Main(string[] args)
{
    // To support High DPI this must be before CefSharp.BrowserSubprocess.SelfHost.Main
    Cef.EnableHighDPISupport();

    // Self-host the browser subprocess
    var exitCode = CefSharp.BrowserSubprocess.SelfHost.Main(args);

    if (exitCode >= 0)
    {
        return exitCode;
    }

    var settings = new CefSettings()
    {
        CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache"),
        // Point to the main executable itself
        BrowserSubprocessPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
    };

    // Perform dependency check to make sure all relevant resources are in our output directory.
    Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);

    // ... rest of app startup
}
```

### Key Points:
1. `CefSharp.BrowserSubprocess.SelfHost.Main(args)` must be called FIRST
2. If it returns >= 0, the process is running as a subprocess and should exit
3. `BrowserSubprocessPath` must point to the main executable itself
4. `performDependencyCheck` should be `false` for self-contained apps

### For .NET 5/6 Single File Apps:
Must set these properties:
- `IncludeNativeLibrariesForSelfExtract=true`
- `IncludeAllContentForSelfExtract=true`

### For Self-Contained Apps:
- Delete `CefSharp.BrowserSubprocess.runtimeconfig.json` from output
- This is handled automatically by CefSharp targets when `SelfContained=true`

## Action Items

1. **Modify App.xaml.cs** to implement self-hosting pattern
2. **Update project file** with correct publish settings
3. **Update release workflow** to ensure native files are copied
4. **Test locally** before pushing


## Codebase Audit Results

### Current Architecture Issues

1. **No Program.cs with Main entry point**
   - WPF app uses App.xaml with `StartupUri="MainWindow.xaml"`
   - No way to implement self-hosting pattern without a Main method
   - This is the ROOT CAUSE of the CefSharp.Core.Runtime issue

2. **CefSharpInitializer.cs Issues**
   - Uses `CefSharp.BrowserSubprocess.exe` which doesn't work with self-contained apps
   - Should use self-hosting pattern instead
   - `performDependencyCheck: true` should be `false` for self-contained

3. **Project File Issues**
   - Missing `RuntimeIdentifier` property
   - Missing `SelfContained` property
   - Missing `PublishSingleFile=false` property
   - Missing `IncludeNativeLibrariesForSelfExtract` property

4. **Release Workflow Issues**
   - Uses `dotnet publish` without proper properties
   - Doesn't handle CefSharp native files correctly
   - `continue-on-error: true` masks failures

### Files That Need Changes

| File | Changes Required |
|------|------------------|
| `SvonyBrowser.csproj` | Add RuntimeIdentifier, SelfContained, publish properties |
| `App.xaml` | Remove StartupUri, add custom Main |
| `App.xaml.cs` | Implement self-hosting pattern |
| `CefSharpInitializer.cs` | Update for self-hosting |
| `.github/workflows/release.yml` | Fix publish commands |

### Required Changes Summary

1. **Create Program.cs** with proper Main entry point that:
   - Calls `CefSharp.BrowserSubprocess.SelfHost.Main(args)` first
   - Sets `BrowserSubprocessPath` to the main executable
   - Initializes CefSharp with `performDependencyCheck: false`

2. **Update App.xaml**:
   - Remove `StartupUri="MainWindow.xaml"`
   - App will be started from Program.cs

3. **Update SvonyBrowser.csproj**:
   - Add `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`
   - Add `<SelfContained>true</SelfContained>`
   - Add `<PublishSingleFile>false</PublishSingleFile>`
   - Add `<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>`

4. **Update release.yml**:
   - Use `msbuild` instead of `dotnet publish` for better control
   - Ensure all native files are included in the output
