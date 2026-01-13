# 🔌 MCP Server Integration Guide

**Version:** 1.0  
**Last Updated:** 2025-01-12  
**Status:** Specification Document

---

## 📋 Overview

This document details the Model Context Protocol (MCP) server architecture for Svony Browser, enabling seamless integration with:

- Windsurf IDE
- Claude Desktop
- LM Studio
- Custom AI applications

---

## 🏗️ MCP Architecture

### Server Topology

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          MCP SERVER CLUSTER                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌───────────────────┐  ┌───────────────────┐  ┌───────────────────┐   │
│  │   EVONY-RAG MCP   │  │   EVONY-RTE MCP   │  │  EVONY-TOOLS MCP  │   │
│  │   Port: 3100      │  │   Port: 3101      │  │   Port: 3102      │   │
│  │                   │  │                   │  │                   │   │
│  │ ┌───────────────┐ │  │ ┌───────────────┐ │  │ ┌───────────────┐ │   │
│  │ │ Vector Store  │ │  │ │ AMF3 Decoder  │ │  │ │ SWF Decompile │ │   │
│  │ │ (ChromaDB)    │ │  │ │               │ │  │ │               │ │   │
│  │ └───────────────┘ │  │ └───────────────┘ │  │ └───────────────┘ │   │
│  │ ┌───────────────┐ │  │ ┌───────────────┐ │  │ ┌───────────────┐ │   │
│  │ │ Embeddings    │ │  │ │ Protocol DB   │ │  │ │ Script Gen    │ │   │
│  │ │ (Local/API)   │ │  │ │               │ │  │ │               │ │   │
│  │ └───────────────┘ │  │ └───────────────┘ │  │ └───────────────┘ │   │
│  │ ┌───────────────┐ │  │ ┌───────────────┐ │  │ ┌───────────────┐ │   │
│  │ │ Knowledge DB  │ │  │ │ Traffic Pipe  │ │  │ │ File Handlers │ │   │
│  │ │               │ │  │ │               │ │  │ │               │ │   │
│  │ └───────────────┘ │  │ └───────────────┘ │  │ └───────────────┘ │   │
│  └───────────────────┘  └───────────────────┘  └───────────────────┘   │
│            │                    │                      │                │
└────────────┼────────────────────┼──────────────────────┼────────────────┘
             │                    │                      │
             ▼                    ▼                      ▼
     ┌───────────────┐   ┌───────────────┐    ┌───────────────┐
     │  Windsurf IDE │   │ Claude Desktop│    │   LM Studio   │
     └───────────────┘   └───────────────┘    └───────────────┘
```

---

## 📦 Server Implementations

### 1. Evony RAG MCP Server

**Purpose:** Knowledge retrieval and semantic search

**Location:** `D:\Fiddler-FlashBrowser\mcp-servers\evony-rag\`

```javascript
// index.js - Main entry point
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { ChromaClient } from 'chromadb';

const server = new McpServer({
  name: 'evony-rag',
  version: '1.0.0',
});

// Initialize ChromaDB
const chroma = new ChromaClient();
const collection = await chroma.getOrCreateCollection({
  name: 'evony_knowledge',
  metadata: { description: 'Evony game knowledge base' }
});

// Tool: Search knowledge base
server.tool(
  'evony_search',
  'Search the Evony knowledge base with semantic search',
  {
    query: { type: 'string', description: 'Search query' },
    limit: { type: 'number', default: 10 },
    category: { type: 'string', enum: ['heroes', 'buildings', 'combat', 'all'] }
  },
  async ({ query, limit, category }) => {
    const results = await collection.query({
      queryTexts: [query],
      nResults: limit,
      where: category !== 'all' ? { category } : undefined
    });
    
    return {
      content: [{
        type: 'text',
        text: JSON.stringify(results, null, 2)
      }]
    };
  }
);

// Tool: Natural language Q&A
server.tool(
  'evony_query',
  'Ask a question about Evony',
  {
    question: { type: 'string', description: 'Your question' },
    include_sources: { type: 'boolean', default: true }
  },
  async ({ question, include_sources }) => {
    // Get relevant context
    const context = await collection.query({
      queryTexts: [question],
      nResults: 5
    });
    
    // Build response with context
    const response = {
      answer: buildAnswer(question, context),
      sources: include_sources ? context.documents : undefined
    };
    
    return {
      content: [{
        type: 'text',
        text: JSON.stringify(response, null, 2)
      }]
    };
  }
);

// Tool: Index new document
server.tool(
  'evony_index',
  'Add a new document to the knowledge base',
  {
    content: { type: 'string', description: 'Document content' },
    title: { type: 'string', description: 'Document title' },
    category: { type: 'string', description: 'Category tag' },
    source: { type: 'string', description: 'Source reference' }
  },
  async ({ content, title, category, source }) => {
    const id = `doc_${Date.now()}`;
    
    await collection.add({
      ids: [id],
      documents: [content],
      metadatas: [{ title, category, source, indexed_at: new Date().toISOString() }]
    });
    
    return {
      content: [{
        type: 'text',
        text: JSON.stringify({ success: true, id })
      }]
    };
  }
);

// Resources: Knowledge base stats
server.resource(
  'evony://knowledge/stats',
  'Knowledge base statistics',
  'application/json',
  async () => {
    const count = await collection.count();
    return JSON.stringify({
      total_documents: count,
      categories: await getCategories(),
      last_updated: await getLastUpdated()
    });
  }
);

// Start server
const transport = new StdioServerTransport();
await server.connect(transport);
```

### 2. Evony RTE MCP Server

**Purpose:** Real-time traffic analysis and packet decoding

**Location:** `D:\Fiddler-FlashBrowser\mcp-servers\evony-rte\`

```javascript
// index.js - Main entry point
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { AmfDecoder, AmfEncoder } from './amf-codec.js';
import { ProtocolDatabase } from './protocol-db.js';
import net from 'net';

const server = new McpServer({
  name: 'evony-rte',
  version: '1.0.0',
});

const amf = new AmfDecoder();
const protocolDb = new ProtocolDatabase('./protocol-definitions.json');
let trafficBuffer = [];
const MAX_BUFFER_SIZE = 1000;

// Connect to Fiddler traffic pipe
const pipeClient = net.createConnection('\\\\.\\pipe\\SvonyFiddlerTraffic');
pipeClient.on('data', (data) => {
  const packet = parseTrafficData(data);
  if (packet) {
    trafficBuffer.push(packet);
    if (trafficBuffer.length > MAX_BUFFER_SIZE) {
      trafficBuffer.shift();
    }
  }
});

// Tool: Decode AMF3 packet
server.tool(
  'evony_decode_amf',
  'Decode an AMF3 binary packet',
  {
    data: { type: 'string', description: 'Hex or base64 encoded data' },
    format: { type: 'string', enum: ['hex', 'base64'], default: 'hex' }
  },
  async ({ data, format }) => {
    const bytes = format === 'hex' 
      ? Buffer.from(data, 'hex')
      : Buffer.from(data, 'base64');
    
    const decoded = amf.decode(bytes);
    
    return {
      content: [{
        type: 'text',
        text: JSON.stringify(decoded, null, 2)
      }]
    };
  }
);

// Tool: Encode to AMF3
server.tool(
  'evony_encode_amf',
  'Encode data to AMF3 format',
  {
    data: { type: 'object', description: 'Data to encode' },
    output_format: { type: 'string', enum: ['hex', 'base64'], default: 'hex' }
  },
  async ({ data, output_format }) => {
    const encoder = new AmfEncoder();
    const encoded = encoder.encode(data);
    
    const result = output_format === 'hex'
      ? encoded.toString('hex')
      : encoded.toString('base64');
    
    return {
      content: [{
        type: 'text',
        text: result
      }]
    };
  }
);

// Tool: Get protocol definition
server.tool(
  'evony_get_protocol',
  'Get the protocol definition for an action',
  {
    action_name: { type: 'string', description: 'Action name like hero.getAllHeroLevel' }
  },
  async ({ action_name }) => {
    const protocol = protocolDb.getAction(action_name);
    
    if (!protocol) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: 'Action not found', action: action_name })
        }]
      };
    }
    
    return {
      content: [{
        type: 'text',
        text: JSON.stringify(protocol, null, 2)
      }]
    };
  }
);

// Tool: List protocol actions
server.tool(
  'evony_list_actions',
  'List all known protocol actions',
  {
    category: { type: 'string', enum: ['hero', 'building', 'army', 'all'], default: 'all' },
    search: { type: 'string', description: 'Filter by name' }
  },
  async ({ category, search }) => {
    let actions = protocolDb.getAllActions();
    
    if (category !== 'all') {
      actions = actions.filter(a => a.category === category);
    }
    
    if (search) {
      actions = actions.filter(a => a.name.toLowerCase().includes(search.toLowerCase()));
    }
    
    return {
      content: [{
        type: 'text',
        text: JSON.stringify(actions, null, 2)
      }]
    };
  }
);

// Tool: Get recent traffic
server.tool(
  'evony_get_traffic',
  'Get recently captured traffic',
  {
    limit: { type: 'number', default: 50 },
    action_filter: { type: 'string', description: 'Filter by action name pattern' }
  },
  async ({ limit, action_filter }) => {
    let traffic = trafficBuffer.slice(-limit);
    
    if (action_filter) {
      traffic = traffic.filter(t => 
        t.action?.toLowerCase().includes(action_filter.toLowerCase())
      );
    }
    
    return {
      content: [{
        type: 'text',
        text: JSON.stringify(traffic, null, 2)
      }]
    };
  }
);

// Tool: Capture control
server.tool(
  'evony_capture_control',
  'Control traffic capture',
  {
    action: { type: 'string', enum: ['status', 'clear', 'export'] }
  },
  async ({ action }) => {
    switch (action) {
      case 'status':
        return {
          content: [{
            type: 'text',
            text: JSON.stringify({
              connected: pipeClient.readyState === 'open',
              buffer_size: trafficBuffer.length,
              max_size: MAX_BUFFER_SIZE
            })
          }]
        };
      case 'clear':
        trafficBuffer = [];
        return { content: [{ type: 'text', text: '{"cleared": true}' }] };
      case 'export':
        return {
          content: [{
            type: 'text',
            text: JSON.stringify({
              exported_at: new Date().toISOString(),
              packet_count: trafficBuffer.length,
              packets: trafficBuffer
            })
          }]
        };
    }
  }
);

// Resource: Recent traffic
server.resource(
  'evony://traffic/recent',
  'Recent captured traffic',
  'application/json',
  async () => JSON.stringify(trafficBuffer.slice(-100))
);

// Resource: Protocol database
server.resource(
  'evony://protocol/all',
  'All protocol definitions',
  'application/json',
  async () => JSON.stringify(protocolDb.getAllActions())
);

// Start server
const transport = new StdioServerTransport();
await server.connect(transport);
```

### 3. Evony Tools MCP Server

**Purpose:** Analysis tools, script generation, SWF decompilation

**Location:** `D:\Fiddler-FlashBrowser\mcp-servers\evony-tools\`

```javascript
// index.js - Main entry point
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { execSync } from 'child_process';
import fs from 'fs/promises';
import path from 'path';

const server = new McpServer({
  name: 'evony-tools',
  version: '1.0.0',
});

const FFDEC_PATH = process.env.SWF_DECOMPILER || 'D:/Tools/ffdec/ffdec.jar';
const SCRIPTS_PATH = process.env.SCRIPTS_PATH || 'D:/Fiddler-FlashBrowser/scripts';

// Tool: Analyze SWF file
server.tool(
  'evony_analyze_swf',
  'Decompile and analyze a Flash SWF file',
  {
    file_path: { type: 'string', description: 'Path to SWF file' },
    extract: { type: 'array', items: { type: 'string' }, default: ['scripts'] }
  },
  async ({ file_path, extract }) => {
    const outputDir = path.join(path.dirname(file_path), 'decompiled');
    
    try {
      // Run FFDEC decompiler
      const extractArgs = extract.map(e => `-${e}`).join(' ');
      execSync(`java -jar "${FFDEC_PATH}" -export ${extractArgs} "${outputDir}" "${file_path}"`);
      
      // Read extracted content
      const results = {};
      for (const type of extract) {
        const typeDir = path.join(outputDir, type);
        if (await fs.access(typeDir).then(() => true).catch(() => false)) {
          results[type] = await listFilesRecursive(typeDir);
        }
      }
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            success: true,
            output_directory: outputDir,
            extracted: results
          }, null, 2)
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }]
      };
    }
  }
);

// Tool: Generate Fiddler script
server.tool(
  'evony_generate_fiddler_script',
  'Generate a FiddlerScript for Evony traffic',
  {
    purpose: { type: 'string', description: 'What the script should do' },
    filter_actions: { type: 'array', items: { type: 'string' }, default: [] },
    options: { type: 'object', default: {} }
  },
  async ({ purpose, filter_actions, options }) => {
    const template = await fs.readFile(
      path.join(SCRIPTS_PATH, 'fiddler', 'template.js'),
      'utf-8'
    );
    
    const script = generateFiddlerScript(template, {
      purpose,
      filter_actions,
      ...options
    });
    
    return {
      content: [{
        type: 'text',
        text: script
      }]
    };
  }
);

// Tool: Generate bot script
server.tool(
  'evony_generate_bot_script',
  'Generate an AutoEvony automation script',
  {
    task: { type: 'string', description: 'Automation task description' },
    parameters: { type: 'object', default: {} }
  },
  async ({ task, parameters }) => {
    const script = generateBotScript(task, parameters);
    
    return {
      content: [{
        type: 'text',
        text: script
      }]
    };
  }
);

// Tool: List available scripts
server.tool(
  'evony_list_scripts',
  'List available script templates',
  {
    type: { type: 'string', enum: ['fiddler', 'bot', 'analysis', 'all'], default: 'all' }
  },
  async ({ type }) => {
    const scripts = [];
    
    const scriptTypes = type === 'all' ? ['fiddler', 'bot', 'analysis'] : [type];
    
    for (const t of scriptTypes) {
      const dir = path.join(SCRIPTS_PATH, t);
      try {
        const files = await fs.readdir(dir);
        for (const file of files) {
          if (file.endsWith('.js') || file.endsWith('.py')) {
            scripts.push({
              type: t,
              name: file,
              path: path.join(dir, file)
            });
          }
        }
      } catch {}
    }
    
    return {
      content: [{
        type: 'text',
        text: JSON.stringify(scripts, null, 2)
      }]
    };
  }
);

// Tool: File upload handler
server.tool(
  'evony_process_file',
  'Process an uploaded file for analysis',
  {
    file_path: { type: 'string', description: 'Path to file' },
    file_type: { type: 'string', enum: ['txt', 'md', 'json', 'amf', 'swf', 'har'] }
  },
  async ({ file_path, file_type }) => {
    const content = await fs.readFile(file_path);
    
    switch (file_type) {
      case 'txt':
      case 'md':
        return { content: [{ type: 'text', text: content.toString() }] };
      
      case 'json':
        return { content: [{ type: 'text', text: JSON.stringify(JSON.parse(content), null, 2) }] };
      
      case 'amf':
        // Decode AMF and return
        return { content: [{ type: 'text', text: JSON.stringify(decodeAmf(content), null, 2) }] };
      
      case 'swf':
        // Return SWF metadata
        return { content: [{ type: 'text', text: JSON.stringify(getSwfMetadata(content), null, 2) }] };
      
      case 'har':
        // Parse and summarize HAR file
        return { content: [{ type: 'text', text: JSON.stringify(summarizeHar(content), null, 2) }] };
      
      default:
        return { content: [{ type: 'text', text: '{"error": "Unknown file type"}' }] };
    }
  }
);

// Resource: Script templates
server.resource(
  'evony://scripts/templates',
  'Available script templates',
  'application/json',
  async () => {
    const templates = [];
    const templatesDir = path.join(SCRIPTS_PATH, 'templates');
    const files = await fs.readdir(templatesDir);
    
    for (const file of files) {
      const content = await fs.readFile(path.join(templatesDir, file), 'utf-8');
      templates.push({ name: file, content });
    }
    
    return JSON.stringify(templates);
  }
);

// Start server
const transport = new StdioServerTransport();
await server.connect(transport);
```

---

## 🔧 Configuration Files

### Master Configuration

**Location:** `D:\Fiddler-FlashBrowser\config\mcp-config.json`

```json
{
  "servers": {
    "evony-rag": {
      "enabled": true,
      "port": 3100,
      "transport": "stdio",
      "command": "node",
      "args": ["./mcp-servers/evony-rag/index.js"],
      "env": {
        "CHROMA_DB_PATH": "./data/chroma-db",
        "KNOWLEDGE_PATH": "./knowledge-base",
        "EMBEDDING_MODEL": "all-MiniLM-L6-v2"
      },
      "autoStart": true,
      "restartOnFailure": true,
      "healthCheck": {
        "enabled": true,
        "interval": 30000,
        "timeout": 5000
      }
    },
    "evony-rte": {
      "enabled": true,
      "port": 3101,
      "transport": "stdio",
      "command": "node",
      "args": ["./mcp-servers/evony-rte/index.js"],
      "env": {
        "FIDDLER_PIPE": "\\\\.\\pipe\\SvonyFiddlerTraffic",
        "PROTOCOL_DB": "./data/protocol-db.json"
      },
      "autoStart": true,
      "restartOnFailure": true
    },
    "evony-tools": {
      "enabled": true,
      "port": 3102,
      "transport": "stdio",
      "command": "node",
      "args": ["./mcp-servers/evony-tools/index.js"],
      "env": {
        "SWF_DECOMPILER": "D:/Tools/ffdec/ffdec.jar",
        "SCRIPTS_PATH": "./scripts"
      },
      "autoStart": false
    }
  },
  "client": {
    "defaultServer": "evony-rag",
    "timeout": 30000,
    "retryAttempts": 3,
    "retryDelay": 1000
  }
}
```

### Claude Desktop Configuration

**Location:** `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "evony-rag": {
      "command": "node",
      "args": ["D:/Fiddler-FlashBrowser/mcp-servers/evony-rag/index.js"],
      "env": {
        "CHROMA_DB_PATH": "D:/Fiddler-FlashBrowser/data/chroma-db",
        "KNOWLEDGE_PATH": "D:/Fiddler-FlashBrowser/knowledge-base"
      }
    },
    "evony-rte": {
      "command": "node",
      "args": ["D:/Fiddler-FlashBrowser/mcp-servers/evony-rte/index.js"],
      "env": {
        "FIDDLER_PIPE": "\\\\.\\pipe\\SvonyFiddlerTraffic"
      }
    },
    "evony-tools": {
      "command": "node",
      "args": ["D:/Fiddler-FlashBrowser/mcp-servers/evony-tools/index.js"]
    }
  }
}
```

### Windsurf IDE Configuration

**Location:** Windsurf MCP settings

```json
{
  "mcp": {
    "servers": [
      {
        "name": "evony-rag",
        "command": "node D:/Fiddler-FlashBrowser/mcp-servers/evony-rag/index.js"
      },
      {
        "name": "evony-rte", 
        "command": "node D:/Fiddler-FlashBrowser/mcp-servers/evony-rte/index.js"
      },
      {
        "name": "evony-tools",
        "command": "node D:/Fiddler-FlashBrowser/mcp-servers/evony-tools/index.js"
      }
    ]
  }
}
```

---

## 📁 Directory Structure

```
D:\Fiddler-FlashBrowser\
├── mcp-servers/
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
│       ├── swf-analyzer.js
│       └── script-generator.js
├── data/
│   ├── chroma-db/
│   └── protocol-db.json
├── knowledge-base/
│   ├── heroes/
│   ├── buildings/
│   ├── combat/
│   └── strategies/
├── scripts/
│   ├── fiddler/
│   ├── bot/
│   ├── analysis/
│   └── templates/
└── config/
    ├── mcp-config.json
    └── claude-config.json
```

---

## 🚀 Setup Instructions

### 1. Install Dependencies

```bash
# Navigate to project root
cd D:\Fiddler-FlashBrowser

# Install RAG server dependencies
cd mcp-servers/evony-rag
npm install

# Install RTE server dependencies
cd ../evony-rte
npm install

# Install Tools server dependencies
cd ../evony-tools
npm install
```

### 2. Initialize Knowledge Base

```bash
# Run knowledge indexer
node scripts/index-knowledge.js

# Verify ChromaDB
node scripts/verify-chroma.js
```

### 3. Configure Claude Desktop

```bash
# Copy configuration
copy config\claude-config.json %APPDATA%\Claude\claude_desktop_config.json

# Restart Claude Desktop
```

### 4. Start MCP Servers

```bash
# Start all servers
node scripts/start-mcp-servers.js

# Or start individually
node mcp-servers/evony-rag/index.js
node mcp-servers/evony-rte/index.js
node mcp-servers/evony-tools/index.js
```

---

## 📚 Related Documentation

- [FEATURE-ROADMAP.md](./FEATURE-ROADMAP.md) - Overall feature roadmap
- [RAG-RTE-INTEGRATION.md](./RAG-RTE-INTEGRATION.md) - RAG/RTE backend details
- [CLI-TOOLS.md](./CLI-TOOLS.md) - CLI tools documentation
- [FIDDLER-SCRIPTS.md](./FIDDLER-SCRIPTS.md) - Fiddler script library
