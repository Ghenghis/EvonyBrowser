# Svony CLI Tools

Command-line interface tools for integrating Svony Browser with AI assistants and IDEs.

## Overview

The CLI provides:
- MCP server management
- WebSocket server for real-time communication
- Integration with Claude Desktop, Windsurf IDE, and LM Studio

## Installation

```bash
cd svony-cli
npm install
```

## Usage

### Start CLI

```bash
npm start
```

Or with options:

```bash
node index.js --port 3100 --verbose
```

### Commands

| Command | Description |
|---------|-------------|
| `start` | Start all MCP servers |
| `stop` | Stop all MCP servers |
| `status` | Show server status |
| `logs` | View server logs |
| `config` | Show/edit configuration |

### Interactive Mode

```bash
$ svony-cli
Svony CLI v1.0.0

> status
evony-rag: Connected (port 3001)
evony-rte: Connected (port 3002)
evony-tools: Connected (port 3003)

> help
Available commands:
  start [server]  - Start MCP server(s)
  stop [server]   - Stop MCP server(s)
  status          - Show server status
  logs [server]   - View server logs
  config          - Show configuration
  exit            - Exit CLI
```

## Configuration Files

### Claude Desktop

Copy `claude-desktop-config.json` to:
- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "evony-rag": {
      "command": "node",
      "args": ["/path/to/mcp-servers/evony-rag/index.js"]
    },
    "evony-rte": {
      "command": "node",
      "args": ["/path/to/mcp-servers/evony-rte/index.js"]
    },
    "evony-tools": {
      "command": "node",
      "args": ["/path/to/mcp-servers/evony-tools/index.js"]
    }
  }
}
```

### Windsurf IDE

Copy `windsurf-config.json` to your Windsurf settings:

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

Import `lm-studio-config.json` via LM Studio's MCP settings panel.

## WebSocket Server

The CLI includes a WebSocket server for real-time communication:

```javascript
// Connect to WebSocket server
const ws = new WebSocket('ws://localhost:3100');

ws.onopen = () => {
  // Subscribe to traffic events
  ws.send(JSON.stringify({
    type: 'subscribe',
    channel: 'traffic'
  }));
};

ws.onmessage = (event) => {
  const data = JSON.parse(event.data);
  console.log('Received:', data);
};
```

### WebSocket Events

| Event | Description |
|-------|-------------|
| `traffic` | New traffic captured |
| `protocol` | Protocol action detected |
| `status` | Server status change |
| `error` | Error occurred |

## API

### REST Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/status` | GET | Get server status |
| `/api/servers` | GET | List MCP servers |
| `/api/servers/:name/start` | POST | Start a server |
| `/api/servers/:name/stop` | POST | Stop a server |
| `/api/traffic` | GET | Get recent traffic |
| `/api/protocol/:action` | GET | Look up protocol action |

### Example API Usage

```bash
# Get server status
curl http://localhost:3100/api/status

# Start a server
curl -X POST http://localhost:3100/api/servers/evony-rag/start

# Look up protocol action
curl http://localhost:3100/api/protocol/hero.hireHero
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `SVONY_CLI_PORT` | 3100 | CLI server port |
| `SVONY_MCP_RAG_PORT` | 3001 | RAG server port |
| `SVONY_MCP_RTE_PORT` | 3002 | RTE server port |
| `SVONY_MCP_TOOLS_PORT` | 3003 | Tools server port |
| `SVONY_LOG_LEVEL` | info | Log level (debug/info/warn/error) |

## Development

### Building

```bash
npm run build
```

### Testing

```bash
npm test
```

### Linting

```bash
npm run lint
```

## Troubleshooting

### Server won't start
- Check if ports are available
- Verify Node.js version (18+)
- Check for missing dependencies

### Connection refused
- Ensure server is running
- Check firewall settings
- Verify port configuration

### MCP errors
- Check server logs
- Verify JSON-RPC format
- Ensure tool parameters are correct

## License

MIT License - See LICENSE file for details.
