# Svony Browser Assets

This folder contains all runtime assets required for Svony Browser to function properly.

## Folder Structure

```
Assets/
├── CefSharp/           # CefSharp 84.4.10 runtime files
│   ├── libcef.dll      # Main CEF library (126MB)
│   ├── CefSharp.dll    # CefSharp managed wrapper
│   ├── CefSharp.Core.dll
│   ├── CefSharp.BrowserSubprocess.exe
│   ├── icudtl.dat      # Unicode data
│   ├── locales/        # Language packs
│   ├── swiftshader/    # Software rendering fallback
│   └── *.pak           # Resource files
├── Plugins/
│   └── pepflashplayer.dll  # Flash Player plugin (v32.0.0.465)
├── SwfPlayer/
│   └── swfplayer.html  # HTML-based SWF player
├── Tools/
│   └── (optional tools like SolEditor.exe)
└── EmptyExe/
    └── (placeholder for subprocess)
```

## CefSharp Runtime

The CefSharp folder contains Chromium Embedded Framework (CEF) version 84.4.10, which is the last version to support Flash Player PPAPI plugins.

### Required Files

These files MUST be present for the browser to function:

| File | Description |
|------|-------------|
| libcef.dll | Main CEF library |
| CefSharp.dll | CefSharp managed wrapper |
| CefSharp.Core.dll | CefSharp core functionality |
| CefSharp.BrowserSubprocess.exe | Browser subprocess handler |
| icudtl.dat | Unicode support data |
| chrome_elf.dll | Crash reporting library |

### Optional Files

| File/Folder | Description |
|-------------|-------------|
| locales/ | Language packs (en-US.pak required) |
| swiftshader/ | Software rendering (GPU fallback) |
| devtools_resources.pak | Chrome DevTools support |
| cef_extensions.pak | Extension support |

## Flash Plugin

The `Plugins/pepflashplayer.dll` is Adobe Flash Player PPAPI plugin version 32.0.0.465. This is required for loading SWF files (like Evony game client).

### Flash Plugin Sources

If you need to obtain the Flash plugin:
- https://github.com/nicknisi/flash-player-npapi
- https://github.com/nicknisi/flash-player-ppapi
- https://github.com/nicknisi/flash-player-activex

## Build Integration

The project file (SvonyBrowser.Flash.csproj) includes a post-build target that automatically copies this Assets folder to the output directory:

```xml
<Target Name="CopyAssets" AfterTargets="Build">
  <Copy SourceFiles="@(AssetsFiles)" 
        DestinationFiles="@(AssetsFiles->'$(OutputPath)Assets\%(RecursiveDir)%(Filename)%(Extension)')" />
</Target>
```

## Runtime Path Resolution

The application looks for assets in the following order:

1. `[AppDirectory]/Assets/` - Primary location
2. `[AppDirectory]/` - Fallback for flat structure

## Troubleshooting

### "Assets folder not found"
Ensure the Assets folder is in the same directory as SvonyBrowser.exe.

### "Flash plugin not found"
Check that `Assets/Plugins/pepflashplayer.dll` exists and is the correct version.

### "libcef.dll not found"
The CefSharp runtime files are missing. Ensure all files in Assets/CefSharp/ are present.

### "CefSharp initialization failed"
- Verify all CefSharp DLLs are present
- Check that Visual C++ Redistributable 2015-2022 is installed
- Ensure running on Windows 7 SP1 or later

## Version Information

| Component | Version |
|-----------|---------|
| CefSharp | 84.4.10 |
| CEF | 84.4.1+gfdc7504+chromium-84.0.4147.105 |
| Chromium | 84.0.4147.105 |
| Flash Player | 32.0.0.465 |
