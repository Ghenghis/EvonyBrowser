#!/usr/bin/env node
/**
 * Evony RTE MCP Server
 * Real-Time Engine for traffic analysis, AMF3 decoding, and protocol inspection
 */

import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { z } from 'zod';
import { AmfDecoder, AmfEncoder } from './amf-codec.js';
import ProtocolDatabase from './protocol-db.js';
import fs from 'fs/promises';
import path from 'path';
import net from 'net';

// Configuration
const CONFIG = {
  capturesPath: process.env.CAPTURES_PATH || './data/captures',
  pipeName: process.env.PIPE_NAME || '\\\\.\\pipe\\EvonyTraffic',
  maxCaptureSize: 10 * 1024 * 1024 // 10MB
};

// Initialize components
const server = new McpServer({
  name: 'evony-rte',
  version: '1.0.0',
  description: 'Real-time traffic analysis and AMF3 decoding for Evony protocol'
});

const amfDecoder = new AmfDecoder();
const amfEncoder = new AmfEncoder();
const protocolDb = new ProtocolDatabase();

// Traffic capture storage
const capturedTraffic = [];
const MAX_CAPTURED_ITEMS = 1000;

// ============================================================================
// MCP Tools - AMF3 Decoding
// ============================================================================

/**
 * Tool: amf_decode - Decode AMF3 binary data
 */
server.tool(
  'amf_decode',
  'Decode AMF3 binary data from hex or base64 string',
  {
    data: z.string().describe('AMF3 data as hex or base64 string'),
    format: z.enum(['hex', 'base64']).default('hex').describe('Input data format')
  },
  async ({ data, format }) => {
    try {
      let buffer;
      if (format === 'hex') {
        buffer = Buffer.from(data.replace(/\s/g, ''), 'hex');
      } else {
        buffer = Buffer.from(data, 'base64');
      }
      
      const decoded = amfDecoder.decode(buffer);
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            success: true,
            input_bytes: buffer.length,
            decoded: decoded
          }, null, 2)
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

/**
 * Tool: amf_encode - Encode data to AMF3 binary
 */
server.tool(
  'amf_encode',
  'Encode JSON data to AMF3 binary format',
  {
    data: z.string().describe('JSON data to encode'),
    output_format: z.enum(['hex', 'base64']).default('hex').describe('Output format')
  },
  async ({ data, output_format }) => {
    try {
      const parsed = JSON.parse(data);
      const encoded = amfEncoder.encode(parsed);
      
      const output = output_format === 'hex' 
        ? encoded.toString('hex')
        : encoded.toString('base64');
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            success: true,
            output_bytes: encoded.length,
            encoded: output
          }, null, 2)
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

// ============================================================================
// MCP Tools - Protocol Analysis
// ============================================================================

/**
 * Tool: protocol_lookup - Look up protocol action definition
 */
server.tool(
  'protocol_lookup',
  'Look up Evony protocol action definition and parameters',
  {
    action: z.string().describe('Action name to look up (e.g., "hero.hireHero")')
  },
  async ({ action }) => {
    try {
      const actionDef = protocolDb.getAction(action);
      
      if (!actionDef) {
        // Try fuzzy search
        const matches = protocolDb.searchActions(action);
        return {
          content: [{
            type: 'text',
            text: JSON.stringify({
              found: false,
              message: `Action "${action}" not found`,
              similar_actions: matches.slice(0, 10).map(m => m.name)
            }, null, 2)
          }]
        };
      }
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            found: true,
            action,
            ...actionDef,
            documentation: protocolDb.generateActionDoc(action)
          }, null, 2)
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

/**
 * Tool: protocol_list - List all protocol actions
 */
server.tool(
  'protocol_list',
  'List all known Evony protocol actions by category',
  {
    category: z.string().optional().describe('Filter by category (castle, hero, troop, army, etc.)')
  },
  async ({ category }) => {
    try {
      let actions;
      if (category) {
        actions = protocolDb.getActionsByCategory(category);
      } else {
        actions = protocolDb.getAllActions();
      }
      
      const categories = protocolDb.getCategories();
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            total_actions: actions.length,
            categories,
            actions: actions.map(a => ({
              name: a.name,
              category: a.category,
              description: a.description
            }))
          }, null, 2)
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

/**
 * Tool: protocol_search - Search protocol actions
 */
server.tool(
  'protocol_search',
  'Search protocol actions by name or description',
  {
    query: z.string().describe('Search query'),
    limit: z.number().default(20).describe('Maximum results')
  },
  async ({ query, limit }) => {
    try {
      const results = protocolDb.searchActions(query).slice(0, limit);
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            query,
            results_count: results.length,
            results: results.map(r => ({
              name: r.name,
              category: r.category,
              description: r.description
            }))
          }, null, 2)
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

// ============================================================================
// MCP Tools - Traffic Capture
// ============================================================================

/**
 * Tool: traffic_capture - Capture and store traffic
 */
server.tool(
  'traffic_capture',
  'Capture traffic data for analysis',
  {
    direction: z.enum(['request', 'response']).describe('Traffic direction'),
    url: z.string().describe('Request URL'),
    data: z.string().describe('Traffic data (hex encoded)'),
    timestamp: z.number().optional().describe('Unix timestamp')
  },
  async ({ direction, url, data, timestamp }) => {
    try {
      const entry = {
        id: `cap_${Date.now()}_${Math.random().toString(36).substr(2, 6)}`,
        direction,
        url,
        data,
        timestamp: timestamp || Date.now(),
        decoded: null
      };
      
      // Try to decode if it looks like AMF3
      try {
        const buffer = Buffer.from(data.replace(/\s/g, ''), 'hex');
        entry.decoded = amfDecoder.decode(buffer);
      } catch {
        // Not AMF3 or decode failed
      }
      
      // Store capture
      capturedTraffic.unshift(entry);
      if (capturedTraffic.length > MAX_CAPTURED_ITEMS) {
        capturedTraffic.pop();
      }
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            success: true,
            capture_id: entry.id,
            decoded: entry.decoded !== null
          })
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

/**
 * Tool: traffic_list - List captured traffic
 */
server.tool(
  'traffic_list',
  'List captured traffic entries',
  {
    limit: z.number().default(50).describe('Maximum entries to return'),
    direction: z.enum(['request', 'response', 'all']).default('all').describe('Filter by direction'),
    decoded_only: z.boolean().default(false).describe('Only show decoded entries')
  },
  async ({ limit, direction, decoded_only }) => {
    try {
      let filtered = capturedTraffic;
      
      if (direction !== 'all') {
        filtered = filtered.filter(e => e.direction === direction);
      }
      
      if (decoded_only) {
        filtered = filtered.filter(e => e.decoded !== null);
      }
      
      const results = filtered.slice(0, limit).map(e => ({
        id: e.id,
        direction: e.direction,
        url: e.url,
        timestamp: new Date(e.timestamp).toISOString(),
        data_length: e.data.length / 2, // hex to bytes
        decoded: e.decoded ? 'yes' : 'no'
      }));
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            total_captured: capturedTraffic.length,
            filtered_count: filtered.length,
            returned: results.length,
            entries: results
          }, null, 2)
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

/**
 * Tool: traffic_get - Get specific traffic entry
 */
server.tool(
  'traffic_get',
  'Get detailed traffic entry by ID',
  {
    capture_id: z.string().describe('Capture ID')
  },
  async ({ capture_id }) => {
    try {
      const entry = capturedTraffic.find(e => e.id === capture_id);
      
      if (!entry) {
        return {
          content: [{
            type: 'text',
            text: JSON.stringify({ error: 'Capture not found' })
          }],
          isError: true
        };
      }
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            id: entry.id,
            direction: entry.direction,
            url: entry.url,
            timestamp: new Date(entry.timestamp).toISOString(),
            data_hex: entry.data.substring(0, 1000) + (entry.data.length > 1000 ? '...' : ''),
            data_length: entry.data.length / 2,
            decoded: entry.decoded
          }, null, 2)
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

/**
 * Tool: traffic_clear - Clear captured traffic
 */
server.tool(
  'traffic_clear',
  'Clear all captured traffic',
  {},
  async () => {
    const count = capturedTraffic.length;
    capturedTraffic.length = 0;
    
    return {
      content: [{
        type: 'text',
        text: JSON.stringify({
          success: true,
          cleared: count
        })
      }]
    };
  }
);

/**
 * Tool: traffic_export - Export captured traffic to file
 */
server.tool(
  'traffic_export',
  'Export captured traffic to JSON file',
  {
    filename: z.string().optional().describe('Output filename')
  },
  async ({ filename }) => {
    try {
      const outputFile = filename || `capture_${Date.now()}.json`;
      const outputPath = path.join(CONFIG.capturesPath, outputFile);
      
      // Ensure directory exists
      await fs.mkdir(CONFIG.capturesPath, { recursive: true });
      
      // Export data
      const exportData = {
        exported_at: new Date().toISOString(),
        total_entries: capturedTraffic.length,
        entries: capturedTraffic
      };
      
      await fs.writeFile(outputPath, JSON.stringify(exportData, null, 2));
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            success: true,
            file: outputPath,
            entries_exported: capturedTraffic.length
          })
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

// ============================================================================
// MCP Tools - Packet Building
// ============================================================================

/**
 * Tool: packet_build - Build Evony protocol packet
 */
server.tool(
  'packet_build',
  'Build an Evony protocol packet for a specific action',
  {
    action: z.string().describe('Action name (e.g., "hero.hireHero")'),
    params: z.string().describe('JSON parameters for the action')
  },
  async ({ action, params }) => {
    try {
      const actionDef = protocolDb.getAction(action);
      if (!actionDef) {
        return {
          content: [{
            type: 'text',
            text: JSON.stringify({ error: `Unknown action: ${action}` })
          }],
          isError: true
        };
      }
      
      const parsedParams = JSON.parse(params);
      
      // Build packet structure
      const packet = {
        action,
        params: parsedParams,
        timestamp: Date.now()
      };
      
      // Encode to AMF3
      const encoded = amfEncoder.encode(packet);
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            action,
            action_info: actionDef,
            params: parsedParams,
            packet_hex: encoded.toString('hex'),
            packet_base64: encoded.toString('base64'),
            packet_size: encoded.length
          }, null, 2)
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

// ============================================================================
// MCP Tools - Utility
// ============================================================================

/**
 * Tool: troop_info - Get troop information
 */
server.tool(
  'troop_info',
  'Get information about troop types',
  {
    code: z.string().optional().describe('Troop code (e.g., "a" for archer)'),
    parse_string: z.string().optional().describe('Parse troop string (e.g., "a:1000,c:500")')
  },
  async ({ code, parse_string }) => {
    try {
      if (parse_string) {
        const parsed = protocolDb.parseTroopString(parse_string);
        return {
          content: [{
            type: 'text',
            text: JSON.stringify({
              input: parse_string,
              parsed: parsed
            }, null, 2)
          }]
        };
      }
      
      if (code) {
        const info = protocolDb.getTroopInfo(code);
        return {
          content: [{
            type: 'text',
            text: JSON.stringify(info || { error: 'Unknown troop code' }, null, 2)
          }]
        };
      }
      
      // Return all troop codes
      return {
        content: [{
          type: 'text',
          text: JSON.stringify(protocolDb.troopCodes, null, 2)
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

/**
 * Tool: building_info - Get building information
 */
server.tool(
  'building_info',
  'Get information about building types',
  {
    type_id: z.number().optional().describe('Building type ID')
  },
  async ({ type_id }) => {
    try {
      if (type_id) {
        const name = protocolDb.getBuildingName(type_id);
        return {
          content: [{
            type: 'text',
            text: JSON.stringify({ type_id, name }, null, 2)
          }]
        };
      }
      
      // Return all building types
      return {
        content: [{
          type: 'text',
          text: JSON.stringify(protocolDb.buildingTypes, null, 2)
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

// ============================================================================
// MCP Resources
// ============================================================================

server.resource(
  'evony://rte/status',
  'RTE server status',
  async () => {
    return {
      contents: [{
        uri: 'evony://rte/status',
        mimeType: 'application/json',
        text: JSON.stringify({
          status: 'operational',
          captured_traffic: capturedTraffic.length,
          protocol_actions: protocolDb.getAllActions().length,
          categories: protocolDb.getCategories()
        })
      }]
    };
  }
);

server.resource(
  'evony://rte/protocol-summary',
  'Protocol action summary',
  async () => {
    const categories = protocolDb.getCategories();
    const summary = {};
    
    for (const cat of categories) {
      summary[cat] = protocolDb.getActionsByCategory(cat).length;
    }
    
    return {
      contents: [{
        uri: 'evony://rte/protocol-summary',
        mimeType: 'application/json',
        text: JSON.stringify({
          total_actions: protocolDb.getAllActions().length,
          by_category: summary
        })
      }]
    };
  }
);

// ============================================================================
// Server Startup
// ============================================================================

async function main() {
  console.error('[evony-rte] Starting Evony RTE MCP Server...');
  console.error(`[evony-rte] Protocol database loaded: ${protocolDb.getAllActions().length} actions`);
  
  // Ensure captures directory exists
  try {
    await fs.mkdir(CONFIG.capturesPath, { recursive: true });
  } catch (error) {
    console.error(`[evony-rte] Warning: Could not create captures directory: ${error.message}`);
  }
  
  // Start MCP server
  const transport = new StdioServerTransport();
  await server.connect(transport);
  
  console.error('[evony-rte] Server running on stdio transport');
}

main().catch(error => {
  console.error('[evony-rte] Fatal error:', error);
  process.exit(1);
});
