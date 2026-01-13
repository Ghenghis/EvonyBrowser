# Changelog

All notable changes to Svony Browser are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-01-12

### Added
- **Dual-panel browser interface** with AutoEvony (left) and EvonyClient (right)
- **Session sharing** via shared CefSharp CachePath - login once, both panels authenticate
- **Fiddler proxy integration** for traffic capture and inspection
- **Panel view controls**: Show left only, right only, or both panels
- **Panel swapping**: Exchange left and right panel contents
- **Resizable panels**: GridSplitter for adjustable panel widths
- **Server selection**: Quick switch between cc1-cc5 servers
- **Settings dialog**: Configure proxy, server, and UI preferences
- **Keyboard shortcuts**: 
  - `Ctrl+1/2/3` for panel modes
  - `Ctrl+S` for swap
  - `F5/F6` for reload
  - `Ctrl+R` for focused panel reload
- **SOL Editor launcher**: Quick access to Flash shared object files
- **Fiddler launcher**: Start/focus Fiddler from toolbar
- **Dark theme UI** with consistent styling
- **Logging system** using Serilog with daily rotation
- **Settings persistence** to JSON file
- **Panel layout memory**: Remember last panel configuration

### Technical Details
- .NET 6.0 WPF application targeting x64
- CefSharp 119.4.30 for Chromium browser controls
- Flash PPAPI plugin support (pepflashplayer.dll 32.0.0.465)
- Thread-safe SessionManager singleton
- ProxyMonitor with async TCP connectivity checks
- Proper IDisposable implementation throughout
- Global exception handling with graceful recovery
- DPI-aware manifest for high-resolution displays

### Bug Fixes & Improvements (v1.0.0 - Quality Audit)

#### Memory Leak Prevention
- Implemented `IDisposable` pattern in `MainWindow` with proper cleanup
- Added event handler unsubscription in `Dispose()` methods
- Fixed `CancellationTokenSource` disposal in window closing
- Added `_disposed` flags to prevent operations on disposed objects
- Implemented thread-safe singleton pattern in `SessionManager` using `Lazy<T>`

#### Thread Safety
- Added `SynchronizationContext` capture for cross-thread UI updates
- Used `lock` objects for thread-safe property access in `SessionManager`
- Implemented `Interlocked` operations in `ProxyMonitor` to prevent overlapping checks
- Used `Dispatcher.BeginInvoke` for UI updates from background threads

#### Exception Handling
- Added global `DispatcherUnhandledException` handler for UI thread errors
- Added `AppDomain.UnhandledException` handler for background thread errors
- Added `TaskScheduler.UnobservedTaskException` handler for async errors
- Wrapped all external operations in try-catch with logging
- Added graceful recovery for non-critical errors

#### Resource Management
- Added proper `Cef.Shutdown()` in `OnExit` with error handling
- Implemented `Logger.Dispose()` at application exit
- Added `Timer.Dispose()` in `ProxyMonitor.Dispose()`
- Cleared event handlers in disposal to prevent memory leaks

#### Validation & Error Handling
- Added input validation in `SettingsWindow` (port range, required fields)
- Added unsaved changes confirmation in settings dialog
- Added null checks and argument validation throughout
- Added proper error messages with user-friendly explanations

#### Code Quality
- Added comprehensive XML documentation comments
- Organized code into logical regions
- Used consistent naming conventions
- Added proper logging at appropriate levels (Debug, Info, Warning, Error, Fatal)
- Removed unused code and imports
- Added `sealed` modifier where appropriate
- Used `readonly` for immutable fields

#### Build & Configuration
- Updated `.csproj` with proper metadata (Version, Authors, Description)
- Added `app.manifest` for DPI awareness and UAC settings
- Added `GlobalUsings.cs` for common imports
- Added `System.Web.HttpUtility` package for HTML encoding
- Added build targets for directory creation and plugin copying
- Added warning for missing Flash plugin during build

### Documentation
- Comprehensive `README.md` with:
  - Installation instructions
  - Usage guide with keyboard shortcuts
  - Architecture diagram (ASCII art)
  - Troubleshooting section
  - Technology stack table
- `ARCHITECTURE.md` with technical details
- SVG diagrams:
  - `architecture-overview.svg`
  - `session-sharing-flow.svg`
  - `component-diagram.svg`
  - `data-flow.svg`
  - `build-deployment.svg`
  - `protocol-blueprint.svg`
  - `svony-logo.svg`

---

## [Unreleased]

### Planned Features
- [ ] Auto-login support with saved credentials (encrypted)
- [ ] Multiple account profiles
- [ ] Session export/import
- [ ] Traffic logging to file
- [ ] Custom FiddlerScript injection
- [ ] Resource monitor panel
- [ ] Light theme option
- [ ] Localization support
- [ ] Auto-update mechanism
- [ ] Plugin system for extensions

---

## Migration Notes

### From Fiddler-FlashBrowser

The original `Fiddler-FlashBrowser` toolkit has been enhanced with:

1. **New dual-panel architecture** - Both bot and game client in one window
2. **Session synchronization** - No need to login separately
3. **WPF-based UI** - Modern, theme-able interface
4. **Integrated tools** - SOL editor and Fiddler access from toolbar
5. **Logging** - Comprehensive application logging

To migrate:
1. Keep existing `Fiddler/` directory with custom rules
2. Keep existing `FlashBrowser_x64/` for Flash plugin
3. Place SWF files in root directory
4. Build and run `SvonyBrowser`

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0 | 2025-01-12 | Initial release with quality audit |
| 0.9.0 | 2025-01-12 | Beta with core functionality |
| 0.1.0 | 2025-01-12 | Initial prototype |
