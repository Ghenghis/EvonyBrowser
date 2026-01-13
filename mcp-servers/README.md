# Evony MCP Servers

This directory contains Model Context Protocol (MCP) servers for the Svony Browser project. These servers provide AI-powered analysis and automation capabilities.

## Servers

### evony-rag (Port 3001)
Retrieval-Augmented Generation server for knowledge base queries.

**Features:**
- Protocol documentation lookup
- Game mechanics knowledge base
- Strategy guides and tips
- Semantic search across documentation

**Tools:**
- `search_knowledge` - Search the knowledge base
- `get_protocol_docs` - Get protocol documentation
- `get_strategy_guide` - Get strategy guides

### evony-rte (Port 3002)
Real-Time Engine server for traffic analysis and protocol handling.

**Features:**
- AMF3 encoding/decoding
- Protocol action lookup
- Traffic analysis
- Packet building

**Tools:**
- `decode_amf3` - Decode AMF3 binary data
- `encode_amf3` - Encode data to AMF3
- `lookup_action` - Look up protocol action
- `analyze_traffic` - Analyze captured traffic
- `build_packet` - Build a protocol packet

### evony-tools (Port 3003)
Utility tools server for calculations and automation.

**Features:**
- Training cost calculations
- March time calculations
- Combat simulations
- Build order generation

**Tools:**
- `calculate_training` - Calculate training costs
- `calculate_march` - Calculate march times
- `simulate_combat` - Simulate combat outcomes
- `generate_build_order` - Generate build orders

## Installation

```bash
# Install dependencies for all servers
cd evony-rag && npm install
cd ../evony-rte && npm install
cd ../evony-tools && npm install
```

## Running

Each server can be started individually:

```bash
# Start RAG server
cd evony-rag && npm start

# Start RTE server
cd evony-rte && npm start

# Start Tools server
cd evony-tools && npm start
```

Or use the CLI to start all servers:

```bash
cd ../cli/svony-cli && npm start
```

## Configuration

Server configuration is stored in `SvonyBrowser/config/mcp-config.json`:

```json
{
  "mcpServers": {
    "evony-rag": {
      "command": "node",
      "args": ["index.js"],
      "cwd": "./mcp-servers/evony-rag",
      "port": 3001,
      "autoConnect": true
    },
    "evony-rte": {
      "command": "node",
      "args": ["index.js"],
      "cwd": "./mcp-servers/evony-rte",
      "port": 3002,
      "autoConnect": true
    },
    "evony-tools": {
      "command": "node",
      "args": ["index.js"],
      "cwd": "./mcp-servers/evony-tools",
      "port": 3003,
      "autoConnect": true
    }
  }
}
```

## Protocol

All servers implement the Model Context Protocol (MCP) specification:

- **Transport**: stdio (standard input/output)
- **Format**: JSON-RPC 2.0
- **Methods**:
  - `initialize` - Initialize the server
  - `tools/list` - List available tools
  - `tools/call` - Call a tool
  - `resources/list` - List available resources
  - `resources/read` - Read a resource

## Integration

### Claude Desktop

Add to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "evony-rag": {
      "command": "node",
      "args": ["/path/to/mcp-servers/evony-rag/index.js"]
    }
  }
}
```

### Windsurf IDE

Add to Windsurf settings:

```json
{
  "mcp.servers": {
    "evony-rag": {
      "command": "node",
      "args": ["/path/to/mcp-servers/evony-rag/index.js"]
    }
  }
}
```

### LM Studio

Configure in LM Studio's MCP settings panel.

## Development

### Adding New Tools

1. Define the tool in the server's tool list
2. Implement the handler function
3. Register the handler in the tool call dispatcher

Example:

```javascript
// Add to tools list
{
  name: 'my_new_tool',
  description: 'Description of the tool',
  inputSchema: {
    type: 'object',
    properties: {
      param1: { type: 'string', description: 'First parameter' }
    },
    required: ['param1']
  }
}

// Implement handler
async function handleMyNewTool(args) {
  const { param1 } = args;
  // Implementation
  return { result: 'success' };
}

// Register in dispatcher
case 'my_new_tool':
  return handleMyNewTool(params.arguments);
```

### Testing

Use the MCP CLI to test tools:

```bash
manus-mcp-cli call evony-rag search_knowledge '{"query": "hero training"}'
```

## Troubleshooting

### Server won't start
- Check Node.js version (requires 18+)
- Verify all dependencies are installed
- Check port availability

### Connection issues
- Verify server is running
- Check firewall settings
- Review logs for errors

### Tool errors
- Validate input parameters
- Check server logs
- Verify data files exist

## License

MIT License - See LICENSE file for details.
