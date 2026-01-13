/**
 * Svony CLI WebSocket Server
 * Provides API access for IDE integrations (Claude Desktop, Windsurf, LM Studio)
 */

import { WebSocketServer, WebSocket } from 'ws';
import { spawn } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// MCP Server processes
const mcpServers = new Map();

/**
 * Start the WebSocket API server
 */
export async function startServer(port = 9876) {
  console.log(`Starting Svony WebSocket server on port ${port}...`);
  
  // Start MCP servers
  await startMcpServers();
  
  // Create WebSocket server
  const wss = new WebSocketServer({ port });
  
  wss.on('connection', (ws) => {
    console.log('Client connected');
    
    ws.on('message', async (data) => {
      try {
        const message = JSON.parse(data.toString());
        const { command, params } = message;
        
        console.log(`Received command: ${command}`);
        
        const result = await handleCommand(command, params);
        ws.send(JSON.stringify({ result }));
      } catch (error) {
        console.error('Error handling message:', error);
        ws.send(JSON.stringify({ error: error.message }));
      }
    });
    
    ws.on('close', () => {
      console.log('Client disconnected');
    });
    
    ws.on('error', (error) => {
      console.error('WebSocket error:', error);
    });
  });
  
  wss.on('listening', () => {
    console.log(`Svony WebSocket server running on ws://localhost:${port}`);
    console.log('Ready to accept connections from CLI, Claude Desktop, Windsurf, or LM Studio');
  });
  
  // Handle shutdown
  process.on('SIGINT', () => {
    console.log('\nShutting down...');
    stopMcpServers();
    wss.close();
    process.exit(0);
  });
  
  return wss;
}

/**
 * Start MCP server processes
 */
async function startMcpServers() {
  const mcpDir = path.join(__dirname, '..', '..', 'mcp-servers');
  
  const servers = [
    { name: 'evony-rag', path: path.join(mcpDir, 'evony-rag', 'index.js') },
    { name: 'evony-rte', path: path.join(mcpDir, 'evony-rte', 'index.js') },
    { name: 'evony-tools', path: path.join(mcpDir, 'evony-tools', 'index.js') }
  ];
  
  for (const server of servers) {
    try {
      console.log(`Starting MCP server: ${server.name}`);
      
      const proc = spawn('node', [server.path], {
        stdio: ['pipe', 'pipe', 'pipe'],
        cwd: path.dirname(server.path)
      });
      
      proc.stderr.on('data', (data) => {
        console.log(`[${server.name}] ${data.toString().trim()}`);
      });
      
      proc.on('error', (error) => {
        console.error(`[${server.name}] Error:`, error.message);
      });
      
      proc.on('exit', (code) => {
        console.log(`[${server.name}] Exited with code ${code}`);
        mcpServers.delete(server.name);
      });
      
      mcpServers.set(server.name, {
        process: proc,
        stdin: proc.stdin,
        stdout: proc.stdout,
        pendingRequests: new Map(),
        requestId: 0
      });
      
      // Initialize MCP connection
      await initializeMcpServer(server.name);
      
    } catch (error) {
      console.error(`Failed to start ${server.name}:`, error.message);
    }
  }
}

/**
 * Initialize MCP server connection
 */
async function initializeMcpServer(serverName) {
  const server = mcpServers.get(serverName);
  if (!server) return;
  
  // Set up response reader
  let buffer = '';
  server.stdout.on('data', (data) => {
    buffer += data.toString();
    
    // Process complete lines
    const lines = buffer.split('\n');
    buffer = lines.pop() || '';
    
    for (const line of lines) {
      if (!line.trim()) continue;
      
      try {
        const response = JSON.parse(line);
        const requestId = response.id;
        
        if (requestId && server.pendingRequests.has(requestId)) {
          const { resolve } = server.pendingRequests.get(requestId);
          server.pendingRequests.delete(requestId);
          resolve(response);
        }
      } catch (error) {
        // Not JSON, ignore
      }
    }
  });
  
  // Send initialize request
  await sendMcpRequest(serverName, 'initialize', {
    protocolVersion: '2024-11-05',
    capabilities: {},
    clientInfo: { name: 'svony-cli', version: '1.0.0' }
  });
  
  // Send initialized notification
  sendMcpNotification(serverName, 'notifications/initialized', {});
  
  console.log(`[${serverName}] Initialized`);
}

/**
 * Send MCP request
 */
async function sendMcpRequest(serverName, method, params) {
  const server = mcpServers.get(serverName);
  if (!server) throw new Error(`Server not found: ${serverName}`);
  
  const requestId = `${serverName}_${++server.requestId}`;
  
  const request = {
    jsonrpc: '2.0',
    id: requestId,
    method,
    params
  };
  
  return new Promise((resolve, reject) => {
    server.pendingRequests.set(requestId, { resolve, reject });
    
    server.stdin.write(JSON.stringify(request) + '\n');
    
    // Timeout
    setTimeout(() => {
      if (server.pendingRequests.has(requestId)) {
        server.pendingRequests.delete(requestId);
        reject(new Error('Request timeout'));
      }
    }, 30000);
  });
}

/**
 * Send MCP notification
 */
function sendMcpNotification(serverName, method, params) {
  const server = mcpServers.get(serverName);
  if (!server) return;
  
  const notification = {
    jsonrpc: '2.0',
    method,
    params
  };
  
  server.stdin.write(JSON.stringify(notification) + '\n');
}

/**
 * Call MCP tool
 */
async function callMcpTool(serverName, toolName, args) {
  const response = await sendMcpRequest(serverName, 'tools/call', {
    name: toolName,
    arguments: args
  });
  
  if (response.error) {
    throw new Error(response.error.message || 'Tool call failed');
  }
  
  // Parse result from content
  const content = response.result?.content?.[0];
  if (content?.type === 'text') {
    try {
      return JSON.parse(content.text);
    } catch {
      return { text: content.text };
    }
  }
  
  return response.result;
}

/**
 * Stop MCP servers
 */
function stopMcpServers() {
  for (const [name, server] of mcpServers) {
    console.log(`Stopping ${name}...`);
    server.process.kill();
  }
  mcpServers.clear();
}

/**
 * Handle incoming command
 */
async function handleCommand(command, params) {
  // Route commands to appropriate MCP server
  const rteCommands = ['amf_decode', 'amf_encode', 'protocol_lookup', 'protocol_list', 'protocol_search',
                       'traffic_capture', 'traffic_list', 'traffic_get', 'traffic_clear', 'traffic_export',
                       'packet_build', 'troop_info', 'building_info'];
  
  const ragCommands = ['evony_search', 'evony_query', 'evony_index', 'evony_list_sources'];
  
  const toolsCommands = ['calc_training', 'calc_march', 'calc_combat', 'calc_resources',
                         'generate_build_order', 'generate_attack_plan', 'coordinate_convert'];
  
  let serverName;
  if (rteCommands.includes(command)) {
    serverName = 'evony-rte';
  } else if (ragCommands.includes(command)) {
    serverName = 'evony-rag';
  } else if (toolsCommands.includes(command)) {
    serverName = 'evony-tools';
  } else {
    throw new Error(`Unknown command: ${command}`);
  }
  
  return await callMcpTool(serverName, command, params);
}

// Export for direct usage
export { handleCommand, callMcpTool };
