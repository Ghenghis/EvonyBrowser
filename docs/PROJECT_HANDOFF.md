# Svony Browser - Project Handoff for Claude Desktop

## Executive Summary

**Project**: Svony Browser - Dual-panel Flash browser for Evony
**Goal**: Run AutoEvony.swf and EvonyClient1921.swf side-by-side, sharing a single authenticated connection
**Status**: Architecture complete, ready for implementation

---

## What You Have (Existing Assets)

```
D:\Fiddler-FlashBrowser\
├── AutoEvony.swf              ✅ Bot interface (4.7MB)
├── EvonyClient1921.swf        ✅ Game client (2.4MB)
├── FlashBrowser_x64/          ✅ CefFlashBrowser with Flash support
├── Fiddler/                   ✅ Proxy with custom Evony rules
├── config/                    ✅ Configuration files
│   └── EvonyRE-CustomRules.cs ✅ Packet classification
├── ARCHITECTURE.md            ✅ Existing system docs
├── README.md                  ✅ Usage guide
└── docs/                      ✅ NEW documentation
    ├── SVONY_BROWSER_ARCHITECTURE.md
    ├── EVONY_PROTOCOL_REFERENCE.md
    ├── IMPLEMENTATION_GUIDE.md
    └── PROJECT_HANDOFF.md (this file)
```

---

## The Key Insight

**Session Sharing via Shared Cache**:
- Both CefSharp browser instances use the **same CachePath**
- Flash SOL files (Local Shared Objects) are stored in the cache
- Cookies and session tokens are shared via RequestContext
- **Login once → Both panels authenticated!**

```
┌─────────────────┐     ┌─────────────────┐
│   AutoEvony     │     │  Evony Client   │
│    (Left)       │     │    (Right)      │
└────────┬────────┘     └────────┬────────┘
         │                       │
         └───────────┬───────────┘
                     │
         ┌───────────▼───────────┐
         │    SHARED CACHE       │
         │  • SOL files          │
         │  • Cookies            │
         │  • Session tokens     │
         └───────────┬───────────┘
                     │
         ┌───────────▼───────────┐
         │   Fiddler Proxy       │
         │   127.0.0.1:8888      │
         └───────────┬───────────┘
                     │
                     ▼
            cc2.evony.com
```

---

## Implementation Checklist

### Phase 1: Project Setup
- [ ] Create Visual Studio WPF project (`SvonyBrowser`)
- [ ] Add NuGet packages: CefSharp.Wpf, Newtonsoft.Json, Serilog
- [ ] Configure CefSharp with shared cache path
- [ ] Set proxy to 127.0.0.1:8888 (Fiddler)
- [ ] Configure Flash plugin path

### Phase 2: UI Layout
- [ ] Create MainWindow with Grid split layout
- [ ] Add GridSplitter for resize
- [ ] Add toolbar with panel toggle buttons
- [ ] Add server selector dropdown
- [ ] Add status bar

### Phase 3: Browser Panels
- [ ] Create left panel for AutoEvony.swf
- [ ] Create right panel for EvonyClient1921.swf
- [ ] Load SWF files on startup
- [ ] Implement show/hide/swap functionality

### Phase 4: Session Management
- [ ] Create SessionManager service
- [ ] Create MessageRouter service
- [ ] Detect login via Fiddler packet capture
- [ ] Broadcast state updates to both panels

### Phase 5: Testing & Polish
- [ ] Test session sharing between panels
- [ ] Verify Fiddler captures traffic correctly
- [ ] Add error handling and logging
- [ ] Create launcher scripts

---

## Critical Code Snippets

### CefSharp Initialization (App.xaml.cs)
```csharp
var settings = new CefSettings
{
    CachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache"),
    PersistSessionCookies = true,
    PersistUserPreferences = true
};

// Proxy through Fiddler
settings.CefCommandLineArgs.Add("proxy-server", "127.0.0.1:8888");

// Flash plugin
settings.CefCommandLineArgs.Add("ppapi-flash-path", flashPluginPath);
settings.CefCommandLineArgs.Add("ppapi-flash-version", "32.0.0.465");

Cef.Initialize(settings);
```

### Split Panel Layout (MainWindow.xaml)
```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="5"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    
    <!-- Left: AutoEvony -->
    <cef:ChromiumWebBrowser Grid.Column="0" x:Name="LeftBrowser"/>
    
    <!-- Splitter -->
    <GridSplitter Grid.Column="1" Width="5" HorizontalAlignment="Stretch"/>
    
    <!-- Right: Evony Client -->
    <cef:ChromiumWebBrowser Grid.Column="2" x:Name="RightBrowser"/>
</Grid>
```

### Load SWF Files
```csharp
LeftBrowser.Address = $"file:///{autoEvonyPath.Replace('\\', '/')}";
RightBrowser.Address = $"file:///{evonyClientPath.Replace('\\', '/')}";
```

---

## Evony Protocol Quick Reference

### API Command Pattern
```actionscript
// General pattern
c.af.get[Category]Commands().[action](params)

// Examples
c.af.getHeroCommands().getHerosListFromTavern(castleId)
c.af.getCastleCommands().upgradeBuilding(castleId, positionId)
c.af.getArmyCommands().sendArmy(armyId, targetX, targetY, missionType)
c.af.getCapitalCommands().levyArmy(castleId, fieldId)
```

### Packet Types (Fiddler colors)
| Type   | Color  | Keywords        |
| ------ | ------ | --------------- |
| LOGIN  | Green  | login, auth     |
| MARCH  | Orange | march, troop    |
| BATTLE | Red    | battle, attack  |
| BUILD  | Purple | build, upgrade  |
| CHAT   | Yellow | chat, message   |
| AMF    | Amber  | 0x00 0x03 magic |

### Building Type IDs
```
1=Town Hall, 2=Cottage, 3=Warehouse, 4=Barracks, 5=Academy,
6=Forge, 7=Workshop, 8=Stable, 9=Relief Station, 10=Embassy,
11=Marketplace, 12=Inn, 13=Feasting Hall, 14=Rally Spot
```

### Troop Codes
```
wo=Worker, w=Warrior, s=Scout, p=Pikeman, sw=Swordsman,
a=Archer, c=Cavalry, cata=Cataphract, t=Transporter,
b=Ballista, r=Ram, cp=Catapult
```

---

## Questions Answered

### Q: How does AutoEvony work?
**A**: AutoEvony is a Flash SWF that implements the Evony protocol directly. It connects via the same socket/HTTP mechanisms as the official client but provides a bot-oriented UI.

### Q: What's the "same connection" requirement?
**A**: Evony allows one connection per account. By sharing the cache (SOL files, cookies), both SWFs share the same authenticated session. Login in one = logged in both.

### Q: Source vs Wrapper approach?
**A**: We're building a **WPF wrapper** using CefSharp that hosts two browser instances with a shared cache. This leverages the existing CefFlashBrowser infrastructure.

---

## File Locations Reference

| Resource         | Path                                                                         |
| ---------------- | ---------------------------------------------------------------------------- |
| AutoEvony SWF    | `D:\Fiddler-FlashBrowser\AutoEvony.swf`                                      |
| Evony Client SWF | `D:\Fiddler-FlashBrowser\EvonyClient1921.swf`                                |
| Flash Plugin     | `D:\Fiddler-FlashBrowser\FlashBrowser_x64\Assets\Plugins\pepflashplayer.dll` |
| Fiddler Rules    | `D:\Fiddler-FlashBrowser\config\EvonyRE-CustomRules.cs`                      |
| Architecture Doc | `D:\Fiddler-FlashBrowser\docs\SVONY_BROWSER_ARCHITECTURE.md`                 |
| Protocol Ref     | `D:\Fiddler-FlashBrowser\docs\EVONY_PROTOCOL_REFERENCE.md`                   |
| Implementation   | `D:\Fiddler-FlashBrowser\docs\IMPLEMENTATION_GUIDE.md`                       |

---

## RAG Knowledge Base

Cascade has access to an **Evony knowledge base MCP server** with:
- **339,160 chunks** of Evony data
- **55,871 symbols** indexed
- **3 query modes**: research, forensics, full_access

Use this command pattern to query:
```
mcp0_evony_search(query="your search terms", k=20)
```

Useful searches:
- `hero castle troop building` - Game mechanics
- `AMF protocol socket connection` - Network protocol
- `login authentication session` - Auth flow
- `API command endpoint` - Available commands

---

## Success Criteria

The project is complete when:
1. ✅ Both SWFs load in split panels
2. ✅ Panels can be resized, hidden, swapped
3. ✅ Single login authenticates both panels
4. ✅ Actions in either panel reflect in both
5. ✅ Fiddler captures and classifies all traffic
6. ✅ Session persists across panel operations

---

## Get Started

1. **Read**: `IMPLEMENTATION_GUIDE.md` for step-by-step instructions
2. **Reference**: `EVONY_PROTOCOL_REFERENCE.md` for protocol details
3. **Understand**: `SVONY_BROWSER_ARCHITECTURE.md` for system design
4. **Build**: Create the WPF project and implement phases 1-5
5. **Test**: Verify session sharing works between panels

**Good luck, Claude Desktop! 🚀**
