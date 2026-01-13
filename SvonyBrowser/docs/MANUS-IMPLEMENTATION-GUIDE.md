# 🚀 Manus Implementation Guide

**Version:** 1.0  
**Last Updated:** 2025-01-12  
**Status:** Complete Implementation Checklist for Manus

---

## 📋 Executive Summary

This guide provides Manus with everything needed to implement the **Svony Browser 2.0** enhancement project. All documentation, protocols, keys, and code templates are ready.

---

## 📁 Documentation Index

### Core Documentation (Already Created)

| File                          | Size    | Purpose                       |
| ----------------------------- | ------- | ----------------------------- |
| `FEATURE-ROADMAP.md`          | 19.5 KB | 10-week milestone plan        |
| `RAG-RTE-INTEGRATION.md`      | 22.8 KB | MCP server architecture       |
| `CHATBOT-DESIGN.md`           | 26.0 KB | Co-pilot UI specs + XAML      |
| `CLI-TOOLS.md`                | 18.7 KB | Windsurf/Claude/LM Studio CLI |
| `FIDDLER-SCRIPTS.md`          | 23.8 KB | 11 FiddlerScript templates    |
| `MCP-INTEGRATION.md`          | 24.8 KB | MCP setup & config            |
| `EVONY-PROTOCOLS-EXTENDED.md` | **NEW** | Complete API reference        |
| `EVONY-KEYS-DATA.md`          | **NEW** | All keys, IDs, constants      |
| `EXPLOITS-WORKAROUNDS.md`     | **NEW** | RE techniques & failsafes     |

### Existing Reference Docs

| File                          | Location                          |
| ----------------------------- | --------------------------------- |
| `EVONY_PROTOCOL_REFERENCE.md` | `D:\Fiddler-FlashBrowser\docs\`   |
| `PROJECT_HANDOFF.md`          | `D:\Fiddler-FlashBrowser\docs\`   |
| `IMPLEMENTATION_GUIDE.md`     | `D:\Fiddler-FlashBrowser\docs\`   |
| `EvonyRE-CustomRules.cs`      | `D:\Fiddler-FlashBrowser\config\` |

---

## 🎯 Implementation Priority Order

### Phase 1: MCP Infrastructure (Week 1-2)
```
Priority: CRITICAL

Tasks:
1. Create mcp-servers/ directory structure
2. Implement evony-rag MCP server (Port 3100)
   - ChromaDB integration
   - Embedding model setup
   - Knowledge indexing
3. Implement evony-rte MCP server (Port 3101)
   - AMF3 codec
   - Named pipe connection to Fiddler
   - Protocol database
4. Configure auto-connect in Svony Browser
```

### Phase 2: Chatbot Interface (Week 3-4)
```
Priority: HIGH

Tasks:
1. Add right-side panel to MainWindow.xaml
2. Implement ChatbotPanel.xaml from CHATBOT-DESIGN.md
3. Create ChatbotService.cs for LLM streaming
4. Add file upload (txt, md, json, amf, swf)
5. Integrate quick tools (decode, query, generate)
6. Connect to RAG/RTE MCPs
```

### Phase 3: CLI Tools (Week 5-6)
```
Priority: HIGH

Tasks:
1. Create cli/ directory structure
2. Build svony-cli.exe commands
3. Configure Windsurf MCP connection
4. Configure Claude Desktop MCP
5. Create LM Studio API bridge
6. Add WebSocket API for real-time updates
```

### Phase 4: Fiddler Deep Integration (Week 7-8)
```
Priority: MEDIUM

Tasks:
1. Install EvonyRE-CustomRules.cs to Fiddler
2. Implement named pipe server in Svony Browser
3. Create real-time traffic feed
4. Add packet modification UI
5. Implement session recording/replay
```

### Phase 5: Advanced Features (Week 9-10)
```
Priority: MEDIUM

Tasks:
1. AMF Inspector panel
2. SWF Decompiler integration
3. Protocol documentation generator
4. Automation script editor
5. Performance optimization
```

---

## 🔑 Critical Keys & Constants

### Server Configuration
```json
{
  "target_server": "cc2.evony.com",
  "proxy": "127.0.0.1:8888",
  "mcp_ports": {
    "rag": 3100,
    "rte": 3101,
    "tools": 3102
  }
}
```

### API Command Pattern
```actionscript
c.af.get[CATEGORY]Commands().[ACTION](params)

Categories:
- getCastleCommands()
- getHeroCommands()
- getTroopCommands()
- getArmyCommands()
- getColonyCommands()
- getCapitalCommands()
- getShopCommands()
- getAllianceCommands()
- getTechCommands()
- getQuestCommands()
- getMailCommands()
```

### Building Type IDs
```
1=Town Hall, 2=Cottage, 3=Warehouse, 4=Barracks,
5=Academy, 6=Forge, 7=Workshop, 8=Stable,
9=Relief Station, 10=Embassy, 11=Marketplace,
12=Inn, 13=Feasting Hall, 14=Rally Spot,
15=Beacon Tower, 16=Walls, 20=Farm, 21=Sawmill,
22=Quarry, 23=Ironmine
```

### Troop Codes
```
wo=Worker, w=Warrior, s=Scout, p=Pikeman,
sw=Swordsman, a=Archer, c=Cavalry, cata=Cataphract,
t=Transporter, b=Ballista, r=Ram, cp=Catapult
```

### Security Salts
```python
LOGIN_SALT = "evony"
API_SALT = "gameapi2010"
SESSION_SALT = "flash_session_2010"
```

---

## 📂 Directory Structure to Create

```
D:\Fiddler-FlashBrowser\
├── mcp-servers/              # NEW - MCP servers
│   ├── evony-rag/
│   │   ├── index.js
│   │   ├── package.json
│   │   ├── embeddings.js
│   │   └── knowledge-loader.js
│   ├── evony-rte/
│   │   ├── index.js
│   │   ├── package.json
│   │   ├── amf-codec.js
│   │   └── protocol-db.js
│   └── evony-tools/
│       ├── index.js
│       ├── package.json
│       └── script-generator.js
├── cli/                      # NEW - CLI tools
│   ├── svony-cli/
│   │   ├── Program.cs
│   │   └── Commands/
│   └── api/
│       └── websocket-server.js
├── data/                     # NEW - Data storage
│   ├── chroma-db/
│   ├── protocol-db.json
│   └── captures/
├── knowledge-base/           # NEW - RAG knowledge
│   ├── heroes/
│   ├── buildings/
│   ├── combat/
│   └── strategies/
└── SvonyBrowser/             # EXISTING - Update
    ├── Panels/               # NEW
    │   └── ChatbotPanel.xaml
    ├── Services/             # UPDATE
    │   ├── McpConnectionManager.cs
    │   ├── ChatbotService.cs
    │   └── TrafficPipeClient.cs
    └── docs/                 # COMPLETE
```

---

## 🛠️ Code Templates Ready

### MCP Server Template
See: `MCP-INTEGRATION.md` - Complete Node.js MCP servers

### Chatbot UI Template
See: `CHATBOT-DESIGN.md` - Full XAML + C# backend

### Fiddler Scripts
See: `FIDDLER-SCRIPTS.md` - 11 ready-to-use scripts

### CLI Commands
See: `CLI-TOOLS.md` - All command specifications

### Protocol Reference
See: `EVONY-PROTOCOLS-EXTENDED.md` - Complete API docs

---

## ✅ Pre-Implementation Checklist

```
[ ] Node.js 18+ installed
[ ] .NET 8 SDK installed
[ ] Fiddler Classic installed
[ ] CefSharp NuGet packages
[ ] ChromaDB Python/Node package
[ ] Git repository configured
```

---

## 🚀 Quick Start Commands

### 1. Clone/Update Repository
```bash
cd D:\Fiddler-FlashBrowser
git pull origin main
```

### 2. Install MCP Dependencies
```bash
cd mcp-servers/evony-rag
npm install

cd ../evony-rte
npm install

cd ../evony-tools
npm install
```

### 3. Build Svony Browser
```powershell
.\Build-SvonyBrowser.bat
```

### 4. Start MCP Servers
```bash
node mcp-servers/evony-rag/index.js &
node mcp-servers/evony-rte/index.js &
```

### 5. Launch Application
```powershell
.\Launch-SvonyBrowser.bat
```

---

## 📊 RAG Knowledge Base

**Available Data:**
- 339,160 chunks indexed
- 55,871 symbols
- 3 query modes: research, forensics, full_access

**Query via MCP:**
```javascript
// From CLI or chatbot
evony_search({ query: "hero attributes", k: 20 })
evony_query({ question: "How do cavalry attacks work?" })
```

---

## ⚠️ Known Issues & Workarounds

1. **Flash Deprecation** - Use bundled pepflashplayer.dll
2. **CefSharp Memory** - Implement proper IDisposable
3. **Proxy Detection** - Use stealth mode in Fiddler rules
4. **Rate Limiting** - Implement human-like delays
5. **Session Timeout** - Add keepalive heartbeat

---

## 📚 All Documentation Files

```
SvonyBrowser/docs/
├── FEATURE-ROADMAP.md          ✅ Ready
├── RAG-RTE-INTEGRATION.md      ✅ Ready
├── CHATBOT-DESIGN.md           ✅ Ready
├── CLI-TOOLS.md                ✅ Ready
├── FIDDLER-SCRIPTS.md          ✅ Ready
├── MCP-INTEGRATION.md          ✅ Ready
├── EVONY-PROTOCOLS-EXTENDED.md ✅ Ready
├── EVONY-KEYS-DATA.md          ✅ Ready
├── EXPLOITS-WORKAROUNDS.md     ✅ Ready
├── MANUS-IMPLEMENTATION-GUIDE.md ✅ Ready (this file)
└── diagrams/
    ├── chatbot-interface.svg   ✅ Ready
    ├── mcp-architecture.svg    ✅ Ready
    └── full-system-architecture.svg ✅ Ready
```

---

## 🎯 Success Criteria

The project is complete when:

1. ✅ MCP servers (RAG, RTE, Tools) running
2. ✅ Chatbot panel integrated with streaming
3. ✅ CLI tools working with Windsurf/Claude
4. ✅ Fiddler capturing only cc2.evony.com
5. ✅ Named pipe forwarding traffic
6. ✅ Real-time packet decoding
7. ✅ File upload/analysis working
8. ✅ All documentation complete

---

**Ready for Manus to implement! 🚀**
