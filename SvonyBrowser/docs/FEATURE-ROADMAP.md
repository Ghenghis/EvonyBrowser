# 🚀 Svony Browser - Feature Roadmap & Enhancement Plan

**Version:** 2.0 Milestone  
**Created:** 2025-01-12  
**Status:** Planning Phase  
**Target:** Full Evony Reverse Engineering Suite

---

## 📋 Executive Summary

This document outlines the comprehensive feature roadmap to transform Svony Browser from a dual-panel Flash browser into a **full-featured Evony reverse engineering and automation platform** with:

- **Real-time RAG/RTE MCP Integration** - Live AI-powered knowledge base and traffic analysis
- **Advanced Chatbot Interface** - Co-pilot style assistant with tools and file support
- **Fiddler Deep Integration** - Automated packet capture, filtering, and script injection
- **CLI Tools Suite** - First-class support for Windsurf IDE, Claude Desktop, LM Studio
- **Packet Editing & Injection** - Real-time AMF3 modification capabilities

---

## 🎯 Feature Categories

### Category 1: RAG & RTE Real-Time Integration
| Priority   | Feature                          | Impact | Effort |
| ---------- | -------------------------------- | ------ | ------ |
| 🔴 Critical | Evony RAG MCP Live Connection    | High   | Medium |
| 🔴 Critical | Evony RTE MCP Traffic Analysis   | High   | High   |
| 🔴 Critical | Real-time Knowledge Base Queries | High   | Medium |
| 🟠 High     | Auto-sync Traffic to RAG         | Medium | Medium |
| 🟠 High     | RTE Protocol Decoder Integration | High   | High   |

### Category 2: Advanced Chatbot Interface
| Priority   | Feature                        | Impact | Effort |
| ---------- | ------------------------------ | ------ | ------ |
| 🔴 Critical | Right-panel Co-pilot Chatbot   | High   | High   |
| 🔴 Critical | File Upload (txt, md, scripts) | High   | Medium |
| 🟠 High     | Tool Execution Panel           | High   | High   |
| 🟠 High     | Context-aware Evony Assistance | High   | Medium |
| 🟡 Medium   | Voice Input/Output             | Medium | Low    |

### Category 3: Fiddler Deep Integration  
| Priority   | Feature                      | Impact | Effort |
| ---------- | ---------------------------- | ------ | ------ |
| 🔴 Critical | cc2.evony.com Traffic Filter | High   | Low    |
| 🔴 Critical | AMF3 Packet Decoder          | High   | High   |
| 🟠 High     | Custom FiddlerScript Library | High   | Medium |
| 🟠 High     | Real-time Packet Editing     | High   | High   |
| 🟡 Medium   | Traffic Recording & Replay   | Medium | Medium |

### Category 4: CLI Tools Suite
| Priority   | Feature                    | Impact | Effort |
| ---------- | -------------------------- | ------ | ------ |
| 🔴 Critical | Windsurf IDE MCP Server    | High   | Medium |
| 🔴 Critical | Claude Desktop Integration | High   | Medium |
| 🟠 High     | LM Studio API Bridge       | High   | Medium |
| 🟠 High     | Command-line RAG Queries   | Medium | Low    |
| 🟡 Medium   | Batch Script Execution     | Medium | Low    |

### Category 5: Reverse Engineering Tools
| Priority   | Feature                          | Impact | Effort |
| ---------- | -------------------------------- | ------ | ------ |
| 🔴 Critical | SWF Decompiler Integration       | High   | Medium |
| 🔴 Critical | AMF3 Inspector Panel             | High   | High   |
| 🟠 High     | Protocol Documentation Generator | High   | Medium |
| 🟠 High     | Action Script Analyzer           | Medium | High   |
| 🟡 Medium   | Memory Pattern Scanner           | Medium | High   |

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        SVONY BROWSER 2.0                                │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────────────┐  │
│  │  AutoEvony  │  │EvonyClient  │  │      AI CO-PILOT CHATBOT        │  │
│  │   Panel     │  │   Panel     │  │  ┌─────────────────────────┐    │  │
│  │             │  │             │  │  │  Chat Messages          │    │  │
│  │  [SWF]      │  │  [SWF]      │  │  │  - RAG Queries          │    │  │
│  │             │  │             │  │  │  - RTE Analysis         │    │  │
│  │             │  │             │  │  │  - Packet Decode        │    │  │
│  └─────────────┘  └─────────────┘  │  └─────────────────────────┘    │  │
│                                     │  ┌─────────────────────────┐    │  │
│                                     │  │  Tools Panel            │    │  │
│                                     │  │  [📁][📤][🔧][⚙️]      │    │  │
│                                     │  └─────────────────────────┘    │  │
│                                     │  ┌─────────────────────────┐    │  │
│                                     │  │  Input Area             │    │  │
│                                     │  │  [________________][➤]  │    │  │
│                                     │  └─────────────────────────┘    │  │
│                                     └─────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────────────┤
│                           STATUS BAR                                     │
│  [RAG: ●] [RTE: ●] [Fiddler: ●] [cc2.evony.com: 🔴 LIVE]               │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
        ┌───────────────────────────┼───────────────────────────┐
        │                           │                           │
        ▼                           ▼                           ▼
┌───────────────┐         ┌─────────────────┐         ┌─────────────────┐
│  EVONY RAG    │         │   EVONY RTE     │         │    FIDDLER      │
│  MCP SERVER   │         │   MCP SERVER    │         │    PROXY        │
│               │         │                 │         │                 │
│ - Knowledge   │         │ - Traffic Parse │         │ - cc2.evony.com │
│ - Game Data   │         │ - AMF3 Decode   │         │ - Packet Filter │
│ - Strategies  │         │ - Protocol Doc  │         │ - Script Inject │
└───────────────┘         └─────────────────┘         └─────────────────┘
```

---

## 📦 Milestone 1: Core Infrastructure (Week 1-2)

### 1.1 MCP Server Connections

**Files to Create:**
- `Services/McpConnectionManager.cs` - Manage RAG/RTE MCP connections
- `Services/EvonyRagClient.cs` - RAG query interface
- `Services/EvonyRteClient.cs` - RTE traffic analysis interface
- `Config/mcp-config.json` - MCP server configuration

**Key Features:**
```csharp
public interface IEvonyRagClient
{
    Task<RagResponse> QueryAsync(string question, CancellationToken ct);
    Task<List<Document>> SearchAsync(string query, int limit = 10);
    Task IndexTrafficAsync(AmfPacket packet);
    bool IsConnected { get; }
}

public interface IEvonyRteClient  
{
    Task<DecodedPacket> DecodeAmfAsync(byte[] rawData);
    Task<ProtocolInfo> GetProtocolInfoAsync(string actionName);
    Task<List<ActionDefinition>> ListActionsAsync();
    event EventHandler<TrafficEventArgs> OnTrafficCaptured;
    bool IsConnected { get; }
}
```

### 1.2 Fiddler Traffic Filter

**FiddlerScript for cc2.evony.com:**
```javascript
// CustomRules.js - Evony Traffic Filter
static function OnBeforeRequest(oSession: Session) {
    // Only capture Evony traffic
    if (!oSession.HostnameIs("cc2.evony.com")) {
        oSession["ui-hide"] = "true";
        return;
    }
    
    // Tag Evony sessions
    oSession["ui-color"] = "gold";
    oSession["ui-bold"] = "true";
    
    // Send to Svony Browser via named pipe
    SendToSvonyBrowser(oSession);
}

static function OnBeforeResponse(oSession: Session) {
    if (oSession.HostnameIs("cc2.evony.com")) {
        // Decode AMF3 response
        if (oSession.oResponse.headers.ExistsAndContains("Content-Type", "application/x-amf")) {
            var amfData = DecodeAMF3(oSession.ResponseBody);
            SendToSvonyAnalyzer(amfData);
        }
    }
}
```

---

## 📦 Milestone 2: Chatbot Interface (Week 3-4)

### 2.1 Co-Pilot Panel Design

**UI Components:**
- `Controls/ChatbotPanel.xaml` - Main chatbot container
- `Controls/ChatMessage.xaml` - Individual message display
- `Controls/ToolsToolbar.xaml` - File upload, tools, settings
- `Controls/CodeBlock.xaml` - Syntax-highlighted code display

**Features:**
1. **Message Types:**
   - User messages (right-aligned, blue)
   - AI responses (left-aligned, gold/Evony theme)
   - System messages (centered, gray)
   - Code blocks (syntax highlighted)
   - Traffic analysis results (collapsible panels)

2. **Tool Integration:**
   - 📁 File browser (select local files)
   - 📤 Upload zone (drag & drop)
   - 🔧 Quick tools (decode packet, query RAG, analyze SWF)
   - ⚙️ Settings (model selection, temperature, context)

3. **File Support:**
   - `.txt` - Plain text files
   - `.md` - Markdown with preview
   - `.json` - JSON with syntax highlighting
   - `.amf` - AMF3 binary files
   - `.swf` - Flash files (decompile option)
   - `.fiddler` - Fiddler session archives

### 2.2 Chatbot Backend

**Architecture:**
```
┌─────────────────────────────────────────────────────────┐
│                   CHATBOT SERVICE                        │
├─────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │
│  │  RAG Mode   │  │  RTE Mode   │  │ Hybrid Mode │      │
│  │             │  │             │  │             │      │
│  │ Query KB    │  │ Analyze     │  │ Both + LLM  │      │
│  │ Search Docs │  │ Traffic     │  │ Context     │      │
│  └─────────────┘  └─────────────┘  └─────────────┘      │
├─────────────────────────────────────────────────────────┤
│  LLM Backends: Claude | GPT-4 | LM Studio Local        │
└─────────────────────────────────────────────────────────┘
```

---

## 📦 Milestone 3: CLI Tools Suite (Week 5-6)

### 3.1 Windsurf IDE Integration

**MCP Server Definition:**
```json
{
  "name": "svony-browser",
  "description": "Evony reverse engineering and traffic analysis",
  "tools": [
    {
      "name": "evony_rag_query",
      "description": "Query the Evony knowledge base",
      "parameters": {
        "query": "string",
        "limit": "integer"
      }
    },
    {
      "name": "evony_decode_packet",
      "description": "Decode an AMF3 packet",
      "parameters": {
        "hex_data": "string"
      }
    },
    {
      "name": "evony_capture_traffic",
      "description": "Start/stop traffic capture",
      "parameters": {
        "action": "start|stop|status"
      }
    },
    {
      "name": "evony_inject_packet",
      "description": "Inject a modified packet",
      "parameters": {
        "packet_data": "object"
      }
    }
  ]
}
```

### 3.2 Claude Desktop Integration

**Configuration:**
```json
{
  "mcpServers": {
    "evony-rag": {
      "command": "node",
      "args": ["D:/Fiddler-FlashBrowser/mcp-servers/evony-rag/index.js"],
      "env": {
        "EVONY_RAG_PATH": "D:/Fiddler-FlashBrowser/knowledge-base"
      }
    },
    "evony-rte": {
      "command": "node", 
      "args": ["D:/Fiddler-FlashBrowser/mcp-servers/evony-rte/index.js"],
      "env": {
        "FIDDLER_PIPE": "\\\\.\\pipe\\SvonyFiddler"
      }
    }
  }
}
```

### 3.3 LM Studio API Bridge

**Features:**
- Local LLM inference for offline analysis
- Custom Evony fine-tuned models
- RAG augmentation pipeline
- Streaming responses

---

## 📦 Milestone 4: Reverse Engineering Tools (Week 7-8)

### 4.1 AMF3 Inspector Panel

**Capabilities:**
1. **Packet Tree View** - Hierarchical AMF3 structure display
2. **Hex Editor** - Raw byte editing with AMF3 awareness
3. **Value Editor** - Edit decoded values, re-encode automatically
4. **Diff View** - Compare original vs modified packets
5. **History** - Track all packet modifications

### 4.2 Protocol Documentation Generator

**Auto-generated Docs:**
```markdown
## Action: hero.getAllHeroLevel

**Request:**
| Field        | Type | Description            |
| ------------ | ---- | ---------------------- |
| heroId       | int  | Hero unique identifier |
| includeItems | bool | Include equipped items |

**Response:**
| Field  | Type         | Description        |
| ------ | ------------ | ------------------ |
| level  | int          | Current hero level |
| exp    | long         | Experience points  |
| skills | Array<Skill> | Unlocked skills    |
```

### 4.3 SWF Decompiler Integration

**Features:**
- Integrated FFDec/JPEXS support
- ActionScript 3 extraction
- Asset extraction (images, sounds)
- Class hierarchy viewer
- String search across SWF

---

## 📦 Milestone 5: Advanced Features (Week 9-10)

### 5.1 Traffic Recording & Replay

**Features:**
1. **Session Recording** - Capture full play sessions
2. **Replay Mode** - Replay traffic for testing
3. **Diff Analysis** - Compare session differences
4. **Export Formats** - HAR, PCAP, Custom JSON

### 5.2 Packet Injection Framework

**Capabilities:**
```csharp
public class PacketInjector
{
    // Inject packet into active session
    Task InjectAsync(AmfPacket packet);
    
    // Modify packet in-flight
    Task<AmfPacket> ModifyAsync(AmfPacket original, Func<AmfPacket, AmfPacket> modifier);
    
    // Block specific actions
    void BlockAction(string actionName);
    
    // Replace response
    void MockResponse(string actionName, object response);
}
```

### 5.3 Automation Scripts

**Script Types:**
1. **Fiddler Scripts** - Traffic manipulation
2. **Bot Scripts** - AutoEvony automation
3. **Analysis Scripts** - Batch packet analysis
4. **Export Scripts** - Data extraction

---

## 🔧 Technical Requirements

### Dependencies to Add

```xml
<!-- SvonyBrowser.csproj additions -->
<PackageReference Include="MCP.Client" Version="1.0.0" />
<PackageReference Include="AMF.NET" Version="3.0.0" />
<PackageReference Include="FlaUI.Core" Version="4.0.0" />
<PackageReference Include="ICSharpCode.Decompiler" Version="8.0.0" />
<PackageReference Include="LiteDB" Version="5.0.0" />
<PackageReference Include="Microsoft.SemanticKernel" Version="1.0.0" />
<PackageReference Include="OpenAI" Version="1.0.0" />
```

### New Project Structure

```
SvonyBrowser/
├── Controls/
│   ├── ChatbotPanel.xaml
│   ├── ChatMessage.xaml
│   ├── AmfInspector.xaml
│   ├── ToolsToolbar.xaml
│   └── TrafficViewer.xaml
├── Services/
│   ├── McpConnectionManager.cs
│   ├── EvonyRagClient.cs
│   ├── EvonyRteClient.cs
│   ├── AmfDecoder.cs
│   ├── PacketInjector.cs
│   └── ChatbotService.cs
├── Models/
│   ├── AmfPacket.cs
│   ├── ChatMessage.cs
│   ├── RagDocument.cs
│   └── ProtocolAction.cs
├── Scripts/
│   ├── fiddler/
│   │   ├── evony-filter.js
│   │   └── amf-decoder.js
│   └── automation/
│       └── sample-bot.js
└── Config/
    ├── mcp-config.json
    └── chatbot-config.json
```

---

## 📊 Success Metrics

| Metric                  | Target  | Measurement           |
| ----------------------- | ------- | --------------------- |
| RAG Query Latency       | < 500ms | P95 response time     |
| Packet Decode Time      | < 100ms | Per packet average    |
| Chatbot Response        | < 2s    | First token time      |
| Traffic Filter Accuracy | 100%    | Only cc2.evony.com    |
| CLI Tool Coverage       | 100%    | All tools in all IDEs |

---

## 📅 Timeline Summary

| Week | Milestone           | Deliverables                     |
| ---- | ------------------- | -------------------------------- |
| 1-2  | Core Infrastructure | MCP connections, Fiddler filter  |
| 3-4  | Chatbot Interface   | UI panel, file upload, tools     |
| 5-6  | CLI Tools Suite     | Windsurf, Claude, LM Studio      |
| 7-8  | RE Tools            | AMF inspector, SWF decompiler    |
| 9-10 | Advanced Features   | Recording, injection, automation |

---

## 📚 Related Documentation

- [RAG-RTE-INTEGRATION.md](./RAG-RTE-INTEGRATION.md) - Detailed RAG/RTE architecture
- [CHATBOT-DESIGN.md](./CHATBOT-DESIGN.md) - Chatbot specifications
- [CLI-TOOLS.md](./CLI-TOOLS.md) - CLI tools documentation
- [FIDDLER-SCRIPTS.md](./FIDDLER-SCRIPTS.md) - Fiddler script library
- [MCP-INTEGRATION.md](./MCP-INTEGRATION.md) - MCP server setup

---

*This roadmap is a living document and will be updated as development progresses.*
