# Svony Browser v7.0.2 - Comprehensive Action Plan

## Executive Summary

The Svony Browser v7.0.0 and v7.0.1 releases fail at startup with the error "Could not load file or assembly 'CefSharp.Core.Runtime'". This document outlines the root cause analysis and the complete action plan to fix the issue.

## Root Cause Analysis

The fundamental issue is that **CefSharp does not support self-contained .NET 6 deployments** using the default `CefSharp.BrowserSubprocess.exe`. When publishing a self-contained application, the BrowserSubprocess executable cannot find the .NET runtime because it's compiled for .NET Core 3.1 and expects a system-installed runtime.

The official CefSharp solution is to implement **self-hosting**, where the main application executable serves as both the main process and the browser subprocess.

## Technical Details

### Why Self-Hosting is Required

When CefSharp initializes, it spawns a separate process (`CefSharp.BrowserSubprocess.exe`) to handle browser rendering. In a self-contained deployment, this subprocess cannot locate the .NET runtime because it's bundled within the main application, not installed system-wide.

The self-hosting pattern solves this by making the main application executable handle both roles. When launched with specific command-line arguments (by CEF), the application detects this and runs as a subprocess instead of showing the main window.

### Implementation Pattern

The self-hosting pattern requires a custom `Main` method that checks if the process is being launched as a subprocess:

```csharp
public static int Main(string[] args)
{
    // Check if running as browser subprocess
    var exitCode = CefSharp.BrowserSubprocess.SelfHost.Main(args);
    if (exitCode >= 0)
    {
        return exitCode; // Running as subprocess, exit when done
    }
    
    // Running as main application
    var settings = new CefSettings
    {
        BrowserSubprocessPath = Process.GetCurrentProcess().MainModule.FileName
    };
    
    Cef.Initialize(settings, performDependencyCheck: false);
    
    // Start WPF application
    var app = new App();
    app.InitializeComponent();
    return app.Run();
}
```

## Action Plan

### Phase 1: Create Program.cs Entry Point

Create a new `Program.cs` file that implements the self-hosting pattern. This file will be the new entry point for the application.

### Phase 2: Update App.xaml

Remove the `StartupUri` attribute from App.xaml. The application will now be started programmatically from Program.cs.

### Phase 3: Update App.xaml.cs

Modify App.xaml.cs to work with the new startup flow. The CefSharp initialization will move to Program.cs.

### Phase 4: Update Project File

Add the following properties to SvonyBrowser.csproj:

| Property | Value | Purpose |
|----------|-------|---------|
| RuntimeIdentifier | win-x64 | Target Windows x64 |
| SelfContained | true | Bundle .NET runtime |
| PublishSingleFile | false | Keep native DLLs separate |
| IncludeNativeLibrariesForSelfExtract | true | Include native libs |

### Phase 5: Update Release Workflow

Modify the GitHub Actions workflow to use the correct publish settings and ensure all native files are included.

### Phase 6: Test and Release

Create v7.0.2 tag and verify the release works correctly.

## Files to Modify

| File | Action | Description |
|------|--------|-------------|
| Program.cs | Create | New entry point with self-hosting |
| App.xaml | Modify | Remove StartupUri |
| App.xaml.cs | Modify | Adapt to new startup flow |
| SvonyBrowser.csproj | Modify | Add publish properties |
| CefSharpInitializer.cs | Modify | Update for self-hosting |
| release.yml | Modify | Fix publish commands |

## Expected Outcome

After implementing these changes, the Svony Browser v7.0.2 release will:

1. Start successfully on Windows 10/11 x64
2. Initialize CefSharp without errors
3. Load web pages correctly
4. Work as a self-contained application without requiring .NET runtime installation

## Risk Mitigation

The changes are based on the official CefSharp documentation and GitHub issues. The self-hosting pattern is the recommended approach by the CefSharp maintainers for self-contained deployments.

## Timeline

All changes will be implemented in a single commit to ensure atomicity. The release workflow will be triggered by creating the v7.0.2 tag.
