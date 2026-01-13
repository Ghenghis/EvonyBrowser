# Changelog

All notable changes to the Evony RE Toolkit.

## [1.0.0] - 2025-01-11

### Added
- Initial release of unified Fiddler + FlashBrowser integration
- Custom Fiddler rules (EvonyRE-CustomRules.cs) with:
  - Evony-only traffic filtering (cc2.evony.com default)
  - Packet type highlighting (login, march, battle, resource, chat, etc.)
  - AMF3 binary packet detection
  - Automatic SWF extraction
  - Stealth mode (proxy header removal)
  - AutoEvony/bot traffic detection
  - Traffic logging to timestamped files
  - JSON export for RTE integration
- Launch-EvonyRE.ps1 - Unified launcher script
- Setup-EvonyRE.ps1 - Automated setup and configuration
- Extract-TutorialSWF.ps1 - Tutorial SWF capture tool
- Server switching support (cc1-cc5 and custom)
- Comprehensive documentation (README.md, ARCHITECTURE.md)
- Configuration file (evony-re-config.json)

### Configuration Options
- Target server selection
- Proxy settings
- Capture and logging directories
- Packet type color customization
- SWF extraction patterns
- Stealth mode settings
- Hotkey bindings

### Fiddler Menu Items
- Evony RE submenu with all toggle options
- Tools menu actions:
  - Export Session for RTE
  - Change Target Server
  - Open SWF/Captures/Logs folders
- Context menu actions:
  - Copy as AMF Hex
  - Save SWF to Disk
- Quick links to Evony servers

### Directory Structure
- /config - Configuration files
- /scripts - PowerShell automation
- /captures - Session exports
- /extracted-swf - Captured SWF files
- /logs - Traffic and launcher logs

---

## Future Plans

### [1.1.0] - Planned
- [ ] Real-time AMF3 packet decoding display
- [ ] Packet replay functionality
- [ ] Integration with Evony RTE MCP server
- [ ] Auto-backup of SOL files before editing
- [ ] Network latency simulation for testing
- [ ] Packet injection/modification support

### [1.2.0] - Planned
- [ ] GUI control panel for settings
- [ ] Session comparison tool
- [ ] Traffic pattern analysis
- [ ] Automated packet classification using ML
- [ ] Export to Wireshark pcap format
