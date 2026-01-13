# Windows Build Fix - CefSharp XAML Compilation Issue

## Current Status
- **Validation Build**: ✅ PASSES (0 errors, 26 warnings)
- **Windows Build**: ❌ 1 error - CefSharp assembly not found during XAML compilation

## The Problem
```
MainWindow.xaml(278,56): error MC1000: Unknown build error, 'Could not find assembly 
'CefSharp, Version=119.4.30.0, Culture=neutral, PublicKeyToken=40c4b6fc221f4138'.
```

This is a **known issue** with SDK-style WPF projects and CefSharp. The XAML compiler runs in a separate process and cannot find the CefSharp assemblies.

## Solution Options

### Option 1: Build with Visual Studio (Recommended)
1. Open `SvonyBrowser.sln` in Visual Studio 2022
2. Right-click solution → Restore NuGet Packages
3. Build → Build Solution (F6)

Visual Studio's MSBuild handles CefSharp assembly resolution correctly.

### Option 2: Use Developer Command Prompt
```cmd
"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"
cd D:\Fiddler-FlashBrowser
msbuild SvonyBrowser\SvonyBrowser.csproj /t:Restore,Build /p:Configuration=Release
```

### Option 3: Add CefSharp Assembly Hint Path
Add to `SvonyBrowser.csproj` before `</Project>`:
```xml
<Target Name="CefSharpAssemblyHint" BeforeTargets="MarkupCompilePass1">
  <ItemGroup>
    <ReferencePath Include="$(NuGetPackageRoot)cefsharp.common\119.4.30\lib\net462\CefSharp.dll" />
  </ItemGroup>
</Target>
```

### Option 4: Use CefSharp.Wpf.HwndHost (For .NET 6+)
Replace in `.csproj`:
```xml
<PackageReference Include="CefSharp.Wpf.HwndHost" Version="119.4.30" />
```
Then update XAML to use `cef:ChromiumWebBrowser` from the HwndHost namespace.

## Files Modified for v7.0

| File                  | Change                                                 |
| --------------------- | ------------------------------------------------------ |
| `App.xaml.cs`         | Added `DataPath` property                              |
| `SvonyBrowser.csproj` | Reverted to net6.0-windows, added GeneratePathProperty |

## Verification Steps

1. Build with Visual Studio
2. Verify 0 errors
3. Run application
4. Commit and push
5. Create GitHub release v7.0.0

## GitHub Release

After successful build:
```bash
git add -A
git commit -m "v7.0.0 - Windows build fix and production ready"
git tag v7.0.0
git push origin main --tags
```

The release workflow will automatically create a GitHub release.
