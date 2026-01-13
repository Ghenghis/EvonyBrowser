# SvonyBrowser Asset Setup Guide

This document explains how to properly set up the assets required for SvonyBrowser to load SWF/Flash content correctly.

## Overview

SvonyBrowser uses the **CefFlashBrowser asset loading pattern**, which loads CefSharp DLLs from a dedicated `Assets` folder rather than from the application directory. This approach:

1. Keeps the application directory clean
2. Allows for easier asset management and updates
3. Enables proper Flash plugin loading for SWF content

## Required Asset Structure

```
[OutputDirectory]/
├── SvonyBrowser.exe
├── Assets/
│   ├── CefSharp/                    # CefSharp runtime files
│   │   ├── libcef.dll               # Main CEF library (~126MB)
│   │   ├── CefSharp.dll             # CefSharp managed wrapper
│   │   ├── CefSharp.Core.dll        # CefSharp core functionality
│   │   ├── CefSharp.Wpf.dll         # CefSharp WPF integration
│   │   ├── CefSharp.BrowserSubprocess.exe
│   │   ├── icudtl.dat               # Unicode support
│   │   ├── chrome_elf.dll
│   │   ├── d3dcompiler_47.dll
│   │   ├── libEGL.dll
│   │   ├── libGLESv2.dll
│   │   ├── cef.pak
│   │   ├── cef_100_percent.pak
│   │   ├── cef_200_percent.pak
│   │   ├── cef_extensions.pak
│   │   ├── devtools_resources.pak
│   │   ├── snapshot_blob.bin
│   │   ├── v8_context_snapshot.bin
│   │   ├── locales/                 # Language packs
│   │   │   ├── en-US.pak            # Required
│   │   │   └── ...
│   │   └── swiftshader/             # Software rendering fallback
│   ├── Plugins/
│   │   └── pepflashplayer.dll       # Flash Player PPAPI plugin (v32.0.0.465)
│   ├── SwfPlayer/
│   │   └── swfplayer.html           # HTML-based SWF player
│   └── EmptyExe/
│       └── SvonyBrowser.EmptyExe.exe  # Optional: prevents black popup window
├── Cache/
├── Logs/
├── config/
└── data/
```

## Setup Methods

### Method 1: Automated Setup Script (Recommended)

Run the PowerShell setup script after restoring NuGet packages:

```powershell
# 1. Restore NuGet packages
nuget restore SvonyBrowser.sln

# 2. Run the setup script
.\Setup-Assets.ps1 -Configuration Release -Platform x64
```

### Method 2: Manual Setup

1. **Restore NuGet packages** to get CefSharp 84.4.10:
   ```
   nuget restore SvonyBrowser.sln
   ```

2. **Copy CefSharp files** from `packages\CefSharp.Common.84.4.10\CefSharp\x64\` to `Assets\CefSharp\`

3. **Copy managed DLLs** from `packages\CefSharp.Common.84.4.10\lib\net462\` to `Assets\CefSharp\`

4. **Obtain Flash plugin** (`pepflashplayer.dll` v32.0.0.465) and place in `Assets\Plugins\`

5. **Create swfplayer.html** in `Assets\SwfPlayer\` (see template below)

## Flash Plugin Sources

The Flash PPAPI plugin (`pepflashplayer.dll`) can be obtained from:

- **GitHub**: https://github.com/nicknisi/flash-player-ppapi
- **Archive.org**: Search for "pepflashplayer.dll 32.0.0.465"
- **Old Chrome installations**: Extract from Chrome browser data (version 84 or earlier)

**Note**: Adobe officially discontinued Flash Player on December 31, 2020. Use archived versions for legacy content.

## SwfPlayer HTML Template

The `swfplayer.html` file enables loading SWF files via JavaScript:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>FlashPlayer</title>
    <style>
        #flash-player {
            position: absolute;
            left: 0px;
            top: 0px;
            width: 100%;
            height: 100vh;
        }
    </style>
    <script>
        function getFileName(src) {
            src = src.replace(/\\/g, '/');
            return src.split('/').pop().split('?')[0];
        }
        function loadSwf(src) { // called by CefSharp
            let flash = document.createElement("embed");
            flash.id = "flash-player";
            flash.setAttribute("src", src);
            document.body.replaceChild(flash, document.getElementById(flash.id));
            document.title = getFileName(src);
        }
    </script>
</head>
<body>
    <embed id="flash-player" src="" />
</body>
</html>
```

## How It Works

### DLL Loading Mechanism

1. **SetDllDirectory**: Before any CefSharp code runs, `Program.cs` calls `Win32.SetDllDirectory()` to tell Windows to look for DLLs in `Assets\CefSharp\`

2. **AssemblyResolve**: A resolver is registered to load .NET assemblies from `Assets\CefSharp\` when the runtime can't find them in the default locations

3. **CefSettings**: The `BrowserSubprocessPath`, `ResourcesDirPath`, and `LocalesDirPath` are all configured to point to the Assets folder

### Flash Plugin Configuration

The Flash plugin is loaded via CefSharp command-line arguments:

```csharp
settings.CefCommandLineArgs.Add("ppapi-flash-path", flashPath);
settings.CefCommandLineArgs.Add("ppapi-flash-version", "32.0.0.465");
settings.CefCommandLineArgs.Add("enable-system-flash", "1");
```

### Loading SWF Files

To load an SWF file:

1. Navigate the browser to `swfplayer.html`
2. Execute JavaScript: `browser.ExecuteScriptAsync("loadSwf", swfFilePath)`

## Troubleshooting

### "Assets folder not found"
Ensure the Assets folder is in the same directory as `SvonyBrowser.exe`.

### "libcef.dll not found"
The CefSharp runtime files are missing. Run the setup script or manually copy files from NuGet packages.

### "Flash plugin not found"
Place `pepflashplayer.dll` in `Assets\Plugins\`. Flash content won't work without it.

### "CefSharp initialization failed"
- Verify all CefSharp DLLs are present in `Assets\CefSharp\`
- Install Visual C++ Redistributable 2015-2022 (x64)
- Ensure running on Windows 7 SP1 or later

### "Black popup window appears"
Create an empty executable at `Assets\EmptyExe\SvonyBrowser.EmptyExe.exe` to suppress this.

## Version Information

| Component      | Version                                |
| -------------- | -------------------------------------- |
| CefSharp       | 84.4.10                                |
| CEF            | 84.4.1+gfdc7504+chromium-84.0.4147.105 |
| Chromium       | 84.0.4147.105                          |
| Flash Player   | 32.0.0.465                             |
| .NET Framework | 4.6.2                                  |

## References

- [CefFlashBrowser](https://github.com/Mzying2001/CefFlashBrowser) - Reference implementation
- [CefSharp Documentation](https://github.com/cefsharp/CefSharp/wiki)
- [Flash Player Archive](https://github.com/nicknisi/flash-player-ppapi)
