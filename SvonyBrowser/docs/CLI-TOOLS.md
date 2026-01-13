# 🖥️ CLI Tools Suite Documentation

**Version:** 1.0  
**Last Updated:** 2025-01-12  
**Status:** Specification Document

---

## 📋 Overview

The Svony Browser CLI Tools Suite provides first-class integration with:

- **Windsurf IDE** - Full MCP server with all tools
- **Claude Desktop** - MCP configuration for AI assistance
- **LM Studio** - Local LLM API bridge for offline analysis

---

## 🌊 Windsurf IDE Integration

### MCP Server Configuration

**Location:** `D:\Fiddler-FlashBrowser\mcp-servers\svony-mcp\`

```json
{
  "name": "svony-browser",
  "version": "2.0.0",
  "description": "Evony reverse engineering and traffic analysis MCP server",
  "protocol": "mcp",
  "transport": "stdio",
  "capabilities": {
    "tools": true,
    "resources": true,
    "prompts": true
  }
}
```

### Available Tools

#### 1. RAG Tools

```typescript
// evony_rag_search - Semantic search across knowledge base
{
  "name": "evony_rag_search",
  "description": "Search the Evony knowledge base with semantic search",
  "inputSchema": {
    "type": "object",
    "properties": {
      "query": {
        "type": "string",
        "description": "Search query for Evony knowledge"
      },
      "limit": {
        "type": "integer",
        "default": 10,
        "description": "Maximum results to return"
      },
      "category": {
        "type": "string",
        "enum": ["heroes", "buildings", "research", "combat", "events", "all"],
        "default": "all"
      }
    },
    "required": ["query"]
  }
}

// evony_rag_query - Natural language Q&A
{
  "name": "evony_rag_query",
  "description": "Ask a question about Evony and get an AI-powered answer",
  "inputSchema": {
    "type": "object",
    "properties": {
      "question": {
        "type": "string",
        "description": "Question about Evony gameplay, mechanics, or strategy"
      },
      "include_sources": {
        "type": "boolean",
        "default": true
      }
    },
    "required": ["question"]
  }
}
```

#### 2. RTE Tools

```typescript
// evony_decode_packet - Decode AMF3 binary data
{
  "name": "evony_decode_packet",
  "description": "Decode an AMF3 packet from hex or base64",
  "inputSchema": {
    "type": "object",
    "properties": {
      "data": {
        "type": "string",
        "description": "Hex or base64 encoded AMF3 data"
      },
      "format": {
        "type": "string",
        "enum": ["hex", "base64"],
        "default": "hex"
      },
      "output_format": {
        "type": "string",
        "enum": ["json", "tree", "raw"],
        "default": "json"
      }
    },
    "required": ["data"]
  }
}

// evony_encode_packet - Encode data to AMF3
{
  "name": "evony_encode_packet",
  "description": "Encode JSON data to AMF3 binary format",
  "inputSchema": {
    "type": "object",
    "properties": {
      "data": {
        "type": "object",
        "description": "JSON data to encode"
      },
      "output_format": {
        "type": "string",
        "enum": ["hex", "base64"],
        "default": "hex"
      }
    },
    "required": ["data"]
  }
}

// evony_get_protocol - Get action protocol definition
{
  "name": "evony_get_protocol",
  "description": "Get the protocol definition for an Evony action",
  "inputSchema": {
    "type": "object",
    "properties": {
      "action_name": {
        "type": "string",
        "description": "Action name like 'hero.getAllHeroLevel'"
      }
    },
    "required": ["action_name"]
  }
}

// evony_list_actions - List all known protocol actions
{
  "name": "evony_list_actions",
  "description": "List all documented Evony protocol actions",
  "inputSchema": {
    "type": "object",
    "properties": {
      "category": {
        "type": "string",
        "enum": ["hero", "building", "army", "alliance", "event", "shop", "all"],
        "default": "all"
      },
      "search": {
        "type": "string",
        "description": "Search filter for action names"
      }
    }
  }
}
```

#### 3. Traffic Tools

```typescript
// evony_capture_traffic - Control traffic capture
{
  "name": "evony_capture_traffic",
  "description": "Start, stop, or check status of Evony traffic capture",
  "inputSchema": {
    "type": "object",
    "properties": {
      "action": {
        "type": "string",
        "enum": ["start", "stop", "status", "clear"],
        "description": "Traffic capture action"
      },
      "filter": {
        "type": "string",
        "default": "cc2.evony.com",
        "description": "Host filter for traffic"
      }
    },
    "required": ["action"]
  }
}

// evony_get_traffic - Get captured traffic
{
  "name": "evony_get_traffic",
  "description": "Get recently captured Evony traffic",
  "inputSchema": {
    "type": "object",
    "properties": {
      "limit": {
        "type": "integer",
        "default": 50
      },
      "action_filter": {
        "type": "string",
        "description": "Filter by action name pattern"
      },
      "direction": {
        "type": "string",
        "enum": ["request", "response", "both"],
        "default": "both"
      },
      "time_range_minutes": {
        "type": "integer",
        "default": 30
      }
    }
  }
}

// evony_inject_packet - Inject a packet
{
  "name": "evony_inject_packet",
  "description": "Inject a modified packet into the traffic stream",
  "inputSchema": {
    "type": "object",
    "properties": {
      "packet_data": {
        "type": "object",
        "description": "Packet data to inject"
      },
      "direction": {
        "type": "string",
        "enum": ["request", "response"]
      },
      "session_id": {
        "type": "string",
        "description": "Target session ID"
      }
    },
    "required": ["packet_data", "direction"]
  }
}
```

#### 4. Analysis Tools

```typescript
// evony_analyze_swf - Analyze SWF file
{
  "name": "evony_analyze_swf",
  "description": "Decompile and analyze a Flash SWF file",
  "inputSchema": {
    "type": "object",
    "properties": {
      "file_path": {
        "type": "string",
        "description": "Path to SWF file"
      },
      "extract": {
        "type": "array",
        "items": {
          "type": "string",
          "enum": ["scripts", "assets", "classes", "strings"]
        },
        "default": ["scripts"]
      }
    },
    "required": ["file_path"]
  }
}

// evony_generate_script - Generate automation script
{
  "name": "evony_generate_script",
  "description": "Generate a Fiddler or bot automation script",
  "inputSchema": {
    "type": "object",
    "properties": {
      "script_type": {
        "type": "string",
        "enum": ["fiddler", "autoevony", "packet_replay"]
      },
      "action": {
        "type": "string",
        "description": "What the script should do"
      },
      "template": {
        "type": "string",
        "description": "Base template to use"
      }
    },
    "required": ["script_type", "action"]
  }
}
```

### Resources

```typescript
// Available resources for Windsurf
{
  "resources": [
    {
      "uri": "evony://knowledge/heroes",
      "name": "Hero Database",
      "description": "Complete hero stats and abilities",
      "mimeType": "application/json"
    },
    {
      "uri": "evony://knowledge/buildings",
      "name": "Building Database",
      "description": "Building stats and requirements",
      "mimeType": "application/json"
    },
    {
      "uri": "evony://protocol/actions",
      "name": "Protocol Actions",
      "description": "All documented protocol actions",
      "mimeType": "application/json"
    },
    {
      "uri": "evony://traffic/recent",
      "name": "Recent Traffic",
      "description": "Last 100 captured packets",
      "mimeType": "application/json"
    },
    {
      "uri": "evony://scripts/fiddler",
      "name": "Fiddler Scripts",
      "description": "Available Fiddler script templates",
      "mimeType": "text/javascript"
    }
  ]
}
```

### Prompts

```typescript
// Pre-built prompts for common tasks
{
  "prompts": [
    {
      "name": "analyze_march",
      "description": "Analyze a march formation and suggest improvements",
      "arguments": [
        { "name": "hero_lineup", "required": true },
        { "name": "target_type", "required": true }
      ]
    },
    {
      "name": "decode_and_explain",
      "description": "Decode a packet and explain its purpose",
      "arguments": [
        { "name": "packet_data", "required": true }
      ]
    },
    {
      "name": "find_protocol",
      "description": "Find the protocol action for a game feature",
      "arguments": [
        { "name": "feature_description", "required": true }
      ]
    },
    {
      "name": "generate_farm_script",
      "description": "Generate a farming automation script",
      "arguments": [
        { "name": "farm_type", "required": true },
        { "name": "conditions", "required": false }
      ]
    }
  ]
}
```

---

## 🤖 Claude Desktop Integration

### Configuration File

**Location:** `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "evony-rag": {
      "command": "node",
      "args": ["D:/Fiddler-FlashBrowser/mcp-servers/evony-rag/index.js"],
      "env": {
        "EVONY_KNOWLEDGE_PATH": "D:/Fiddler-FlashBrowser/knowledge-base",
        "CHROMA_DB_PATH": "D:/Fiddler-FlashBrowser/chroma-db",
        "EMBEDDING_MODEL": "all-MiniLM-L6-v2"
      }
    },
    "evony-rte": {
      "command": "node",
      "args": ["D:/Fiddler-FlashBrowser/mcp-servers/evony-rte/index.js"],
      "env": {
        "FIDDLER_PIPE": "\\\\.\\pipe\\SvonyFiddlerTraffic",
        "PROTOCOL_DB": "D:/Fiddler-FlashBrowser/protocol-db",
        "AMF_DECODER": "native"
      }
    },
    "evony-tools": {
      "command": "node",
      "args": ["D:/Fiddler-FlashBrowser/mcp-servers/evony-tools/index.js"],
      "env": {
        "SWF_DECOMPILER": "D:/Tools/ffdec/ffdec.jar",
        "SCRIPTS_PATH": "D:/Fiddler-FlashBrowser/scripts"
      }
    }
  }
}
```

### Usage Examples in Claude Desktop

```markdown
## Query Evony Knowledge

User: @evony-rag What's the best hero combination for SvS rally?

Claude: Based on the Evony knowledge base, the optimal SvS rally lineup is...
[Sources: hero_guide.md, svs_strategy.md]


## Decode Traffic

User: @evony-rte Decode this packet: 0A0B01...

Claude: This is a `hero.getAllHeroLevel` request:
```json
{
  "heroId": 12345,
  "includeItems": true
}
```


## Generate Script

User: @evony-tools Generate a Fiddler script to log all hero actions

Claude: Here's a FiddlerScript that logs hero-related traffic:
```javascript
// Auto-generated by Evony Tools
static function OnBeforeRequest(oSession: Session) {
    if (oSession.HostnameIs("cc2.evony.com")) {
        // Filter hero actions
        ...
    }
}
```
```

---

## 🧠 LM Studio Integration

### API Bridge Configuration

**Location:** `D:\Fiddler-FlashBrowser\config\lmstudio-config.json`

```json
{
  "lmStudio": {
    "apiUrl": "http://localhost:1234/v1",
    "defaultModel": "mistral-7b-instruct",
    "contextLength": 8192,
    "ragAugmentation": true,
    "systemPrompt": "You are an Evony expert assistant with access to game knowledge and traffic analysis capabilities."
  },
  "ragIntegration": {
    "enabled": true,
    "topK": 5,
    "minSimilarity": 0.7,
    "includeInPrompt": true
  },
  "rteIntegration": {
    "enabled": true,
    "autoDecodeTraffic": true,
    "includeRecentTraffic": true,
    "trafficWindowMinutes": 10
  }
}
```

### LM Studio API Wrapper

```csharp
public class LmStudioClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly LmStudioConfig _config;
    private readonly IEvonyRagClient _ragClient;

    public async IAsyncEnumerable<string> StreamAsync(string prompt, ChatContext context = null)
    {
        // Augment prompt with RAG context
        var augmentedPrompt = await AugmentWithRagAsync(prompt, context);
        
        var request = new
        {
            model = _config.DefaultModel,
            messages = new[]
            {
                new { role = "system", content = _config.SystemPrompt },
                new { role = "user", content = augmentedPrompt }
            },
            stream = true,
            max_tokens = 2000,
            temperature = 0.7
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"{_config.ApiUrl}/chat/completions", 
            request,
            new HttpCompletionOption.ResponseHeadersRead);

        await foreach (var chunk in ReadSseStreamAsync(response))
        {
            yield return chunk;
        }
    }

    private async Task<string> AugmentWithRagAsync(string prompt, ChatContext context)
    {
        if (!_config.RagIntegration.Enabled)
            return prompt;

        // Get relevant documents from RAG
        var docs = await _ragClient.SearchAsync(prompt, _config.RagIntegration.TopK);
        var relevantDocs = docs.Where(d => d.Similarity >= _config.RagIntegration.MinSimilarity);

        if (!relevantDocs.Any())
            return prompt;

        // Build augmented prompt
        var contextSection = string.Join("\n\n", relevantDocs.Select(d => 
            $"[{d.Source}]\n{d.Content}"));

        return $"""
            Context from Evony Knowledge Base:
            {contextSection}

            User Question: {prompt}

            Please answer based on the provided context. If the context doesn't contain relevant information, say so.
            """;
    }
}
```

---

## 📟 Command Line Interface

### svony-cli Tool

**Installation:**
```powershell
# Add to PATH
$env:PATH += ";D:\Fiddler-FlashBrowser\cli"

# Or use directly
D:\Fiddler-FlashBrowser\cli\svony-cli.exe
```

### Commands

```bash
# RAG Commands
svony-cli rag search "hero combinations"
svony-cli rag query "What's the best way to farm resources?"
svony-cli rag index ./my-notes.md --category strategies

# RTE Commands
svony-cli rte decode --hex "0A0B01..."
svony-cli rte decode --file packet.bin
svony-cli rte protocol hero.getAllHeroLevel
svony-cli rte actions --category hero --search "level"

# Traffic Commands
svony-cli traffic start --filter cc2.evony.com
svony-cli traffic status
svony-cli traffic export --format har --output session.har
svony-cli traffic replay session.har

# Analysis Commands
svony-cli analyze swf AutoEvony.swf --extract scripts
svony-cli analyze traffic --last 100 --group-by action

# Script Commands
svony-cli script generate fiddler --action "log hero requests"
svony-cli script run ./my-script.js
svony-cli script list --type fiddler

# Server Commands
svony-cli server start --port 3100
svony-cli server status
svony-cli server logs --tail 100
```

### CLI Output Formats

```bash
# JSON output (default)
svony-cli rag search "rally" --format json

# Table output
svony-cli rte actions --format table

# YAML output
svony-cli traffic export --format yaml

# Markdown output
svony-cli rag query "hero guide" --format markdown
```

### Integration with Shell Scripts

```bash
#!/bin/bash
# Example: Auto-analyze captured traffic

# Start capture
svony-cli traffic start

# Wait for game activity
sleep 300

# Export and analyze
svony-cli traffic export --format json | \
  svony-cli analyze traffic --stdin | \
  svony-cli rag index --category traffic-analysis

# Stop capture
svony-cli traffic stop
```

---

## 🔌 API Endpoints

### REST API (Optional HTTP Server)

```yaml
# Start HTTP server
svony-cli server start --http --port 8080

# Available endpoints:
POST /api/rag/search
  Body: { "query": "string", "limit": 10 }

POST /api/rag/query
  Body: { "question": "string" }

POST /api/rte/decode
  Body: { "data": "hex string", "format": "hex" }

GET /api/rte/protocol/:actionName

GET /api/traffic/recent
  Query: limit, direction, action_filter

POST /api/traffic/inject
  Body: { "packet_data": {}, "direction": "request" }

GET /api/status
  Returns: { "rag": "connected", "rte": "connected", "fiddler": "connected" }
```

### WebSocket API (Real-time)

```javascript
// Connect to WebSocket
const ws = new WebSocket('ws://localhost:8080/ws');

// Subscribe to traffic
ws.send(JSON.stringify({
  type: 'subscribe',
  channel: 'traffic',
  filter: { host: 'cc2.evony.com' }
}));

// Receive traffic events
ws.onmessage = (event) => {
  const data = JSON.parse(event.data);
  if (data.type === 'traffic') {
    console.log('Captured:', data.packet);
  }
};

// Send commands
ws.send(JSON.stringify({
  type: 'command',
  action: 'decode',
  data: '0A0B01...'
}));
```

---

## 📁 Directory Structure

```
D:\Fiddler-FlashBrowser\
├── mcp-servers/
│   ├── svony-mcp/           # Combined MCP server
│   │   ├── index.js
│   │   ├── package.json
│   │   └── tools/
│   │       ├── rag-tools.js
│   │       ├── rte-tools.js
│   │       └── analysis-tools.js
│   ├── evony-rag/           # RAG-specific server
│   │   ├── index.js
│   │   └── embeddings.js
│   └── evony-rte/           # RTE-specific server
│       ├── index.js
│       └── amf-decoder.js
├── cli/
│   ├── svony-cli.exe        # Windows executable
│   ├── svony-cli            # Linux/Mac binary
│   └── completions/         # Shell completions
│       ├── svony-cli.bash
│       ├── svony-cli.zsh
│       └── svony-cli.ps1
├── config/
│   ├── mcp-config.json
│   ├── lmstudio-config.json
│   └── claude-config.json
└── scripts/
    ├── fiddler/
    ├── automation/
    └── analysis/
```

---

## 📚 Related Documentation

- [FEATURE-ROADMAP.md](./FEATURE-ROADMAP.md) - Overall feature roadmap
- [RAG-RTE-INTEGRATION.md](./RAG-RTE-INTEGRATION.md) - RAG/RTE backend details
- [CHATBOT-DESIGN.md](./CHATBOT-DESIGN.md) - Chatbot interface design
- [FIDDLER-SCRIPTS.md](./FIDDLER-SCRIPTS.md) - Fiddler script library
