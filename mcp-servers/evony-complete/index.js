#!/usr/bin/env node
/**
 * Evony Complete MCP Server
 * 
 * Provides FULL access to all Svony Browser features for:
 * - Claude Desktop
 * - Windsurf IDE
 * - LM Studio
 * 
 * 168 tools across 20 categories for complete browser control
 */

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  ListResourcesRequestSchema,
  ReadResourceRequestSchema,
  ListPromptsRequestSchema,
  GetPromptRequestSchema
} from '@modelcontextprotocol/sdk/types.js';
import WebSocket from 'ws';

// Configuration
const CONFIG = {
  browserHost: process.env.SVONY_BROWSER_HOST || 'localhost',
  browserPort: parseInt(process.env.SVONY_BROWSER_PORT || '9876'),
  lmStudioHost: process.env.LM_STUDIO_HOST || 'localhost',
  lmStudioPort: parseInt(process.env.LM_STUDIO_PORT || '1234'),
  version: '5.0.0'
};

// WebSocket connection to Svony Browser
let browserWs = null;
let browserConnected = false;

// ============================================================================
// TOOL DEFINITIONS - 168 Tools across 20 Categories
// ============================================================================

const TOOLS = {
  // =========================================================================
  // 1. BROWSER CONTROL (10 tools)
  // =========================================================================
  browser_navigate: {
    description: 'Navigate browser to a URL',
    inputSchema: {
      type: 'object',
      properties: {
        url: { type: 'string', description: 'URL to navigate to' },
        panel: { type: 'string', enum: ['left', 'right', 'both'], default: 'left', description: 'Which panel to navigate' }
      },
      required: ['url']
    }
  },
  browser_back: {
    description: 'Navigate browser back',
    inputSchema: {
      type: 'object',
      properties: {
        panel: { type: 'string', enum: ['left', 'right', 'both'], default: 'left' }
      }
    }
  },
  browser_forward: {
    description: 'Navigate browser forward',
    inputSchema: {
      type: 'object',
      properties: {
        panel: { type: 'string', enum: ['left', 'right', 'both'], default: 'left' }
      }
    }
  },
  browser_refresh: {
    description: 'Refresh browser page',
    inputSchema: {
      type: 'object',
      properties: {
        panel: { type: 'string', enum: ['left', 'right', 'both'], default: 'left' },
        hard: { type: 'boolean', default: false, description: 'Hard refresh (clear cache)' }
      }
    }
  },
  browser_panel_control: {
    description: 'Control browser panels (show/hide/swap)',
    inputSchema: {
      type: 'object',
      properties: {
        action: { type: 'string', enum: ['show_left', 'show_right', 'show_both', 'hide_left', 'hide_right', 'swap', 'maximize_left', 'maximize_right'] }
      },
      required: ['action']
    }
  },
  browser_screenshot: {
    description: 'Take screenshot of browser',
    inputSchema: {
      type: 'object',
      properties: {
        panel: { type: 'string', enum: ['left', 'right', 'both', 'full'], default: 'full' },
        format: { type: 'string', enum: ['png', 'jpg'], default: 'png' },
        filename: { type: 'string', description: 'Output filename' }
      }
    }
  },
  browser_get_url: {
    description: 'Get current URL of browser panel',
    inputSchema: {
      type: 'object',
      properties: {
        panel: { type: 'string', enum: ['left', 'right'], default: 'left' }
      }
    }
  },
  browser_get_title: {
    description: 'Get page title of browser panel',
    inputSchema: {
      type: 'object',
      properties: {
        panel: { type: 'string', enum: ['left', 'right'], default: 'left' }
      }
    }
  },
  browser_execute_js: {
    description: 'Execute JavaScript in browser panel',
    inputSchema: {
      type: 'object',
      properties: {
        script: { type: 'string', description: 'JavaScript code to execute' },
        panel: { type: 'string', enum: ['left', 'right'], default: 'left' }
      },
      required: ['script']
    }
  },
  browser_get_status: {
    description: 'Get full browser status',
    inputSchema: { type: 'object', properties: {} }
  },

  // =========================================================================
  // 2. SESSION MANAGEMENT (8 tools)
  // =========================================================================
  session_list: {
    description: 'List all sessions',
    inputSchema: { type: 'object', properties: {} }
  },
  session_create: {
    description: 'Create new session',
    inputSchema: {
      type: 'object',
      properties: {
        name: { type: 'string', description: 'Session name' },
        server: { type: 'string', description: 'Game server' }
      },
      required: ['name']
    }
  },
  session_switch: {
    description: 'Switch to a session',
    inputSchema: {
      type: 'object',
      properties: {
        sessionId: { type: 'string', description: 'Session ID to switch to' }
      },
      required: ['sessionId']
    }
  },
  session_delete: {
    description: 'Delete a session',
    inputSchema: {
      type: 'object',
      properties: {
        sessionId: { type: 'string', description: 'Session ID to delete' }
      },
      required: ['sessionId']
    }
  },
  session_get_info: {
    description: 'Get session information',
    inputSchema: {
      type: 'object',
      properties: {
        sessionId: { type: 'string', description: 'Session ID (current if not specified)' }
      }
    }
  },
  session_export: {
    description: 'Export session data',
    inputSchema: {
      type: 'object',
      properties: {
        sessionId: { type: 'string' },
        format: { type: 'string', enum: ['json', 'encrypted'], default: 'json' }
      }
    }
  },
  session_import: {
    description: 'Import session data',
    inputSchema: {
      type: 'object',
      properties: {
        data: { type: 'string', description: 'Session data to import' },
        format: { type: 'string', enum: ['json', 'encrypted'], default: 'json' }
      },
      required: ['data']
    }
  },
  session_sync: {
    description: 'Sync session with game server',
    inputSchema: {
      type: 'object',
      properties: {
        sessionId: { type: 'string' }
      }
    }
  },

  // =========================================================================
  // 3. ACCOUNT MANAGEMENT (8 tools)
  // =========================================================================
  account_list: {
    description: 'List all managed accounts',
    inputSchema: { type: 'object', properties: {} }
  },
  account_add: {
    description: 'Add account to manager',
    inputSchema: {
      type: 'object',
      properties: {
        name: { type: 'string', description: 'Account name/alias' },
        server: { type: 'string', description: 'Game server' },
        credentials: { type: 'object', description: 'Account credentials' }
      },
      required: ['name', 'server']
    }
  },
  account_remove: {
    description: 'Remove account from manager',
    inputSchema: {
      type: 'object',
      properties: {
        accountId: { type: 'string', description: 'Account ID to remove' }
      },
      required: ['accountId']
    }
  },
  account_switch: {
    description: 'Switch active account',
    inputSchema: {
      type: 'object',
      properties: {
        accountId: { type: 'string', description: 'Account ID to switch to' }
      },
      required: ['accountId']
    }
  },
  account_get_info: {
    description: 'Get account information',
    inputSchema: {
      type: 'object',
      properties: {
        accountId: { type: 'string' }
      }
    }
  },
  account_sync: {
    description: 'Sync account data from game',
    inputSchema: {
      type: 'object',
      properties: {
        accountId: { type: 'string' }
      }
    }
  },
  account_coordinate: {
    description: 'Coordinate action across multiple accounts',
    inputSchema: {
      type: 'object',
      properties: {
        accountIds: { type: 'array', items: { type: 'string' }, description: 'Account IDs to coordinate' },
        action: { type: 'string', description: 'Action to coordinate' },
        params: { type: 'object', description: 'Action parameters' },
        timing: { type: 'string', enum: ['simultaneous', 'staggered', 'sequential'], default: 'staggered' }
      },
      required: ['accountIds', 'action']
    }
  },
  account_get_status: {
    description: 'Get status of all accounts',
    inputSchema: { type: 'object', properties: {} }
  },

  // =========================================================================
  // 4. GAME STATE (12 tools)
  // =========================================================================
  game_get_status: {
    description: 'Get full game status',
    inputSchema: { type: 'object', properties: {} }
  },
  game_get_resources: {
    description: 'Get resource levels and production rates',
    inputSchema: {
      type: 'object',
      properties: {
        cityId: { type: 'integer', description: 'City ID (main city if not specified)' }
      }
    }
  },
  game_get_troops: {
    description: 'Get troop counts by type',
    inputSchema: {
      type: 'object',
      properties: {
        cityId: { type: 'integer' },
        includeMarching: { type: 'boolean', default: true }
      }
    }
  },
  game_get_heroes: {
    description: 'Get hero information',
    inputSchema: {
      type: 'object',
      properties: {
        heroId: { type: 'integer', description: 'Specific hero ID (all if not specified)' }
      }
    }
  },
  game_get_buildings: {
    description: 'Get building status',
    inputSchema: {
      type: 'object',
      properties: {
        cityId: { type: 'integer' },
        buildingType: { type: 'string', description: 'Filter by building type' }
      }
    }
  },
  game_get_research: {
    description: 'Get research status',
    inputSchema: {
      type: 'object',
      properties: {
        category: { type: 'string', description: 'Research category filter' }
      }
    }
  },
  game_get_marches: {
    description: 'Get active marches',
    inputSchema: {
      type: 'object',
      properties: {
        type: { type: 'string', enum: ['all', 'attack', 'reinforce', 'gather', 'scout'], default: 'all' }
      }
    }
  },
  game_get_alliance: {
    description: 'Get alliance information',
    inputSchema: { type: 'object', properties: {} }
  },
  game_get_events: {
    description: 'Get active events',
    inputSchema: {
      type: 'object',
      properties: {
        type: { type: 'string', description: 'Event type filter' }
      }
    }
  },
  game_get_mail: {
    description: 'Get mail/messages',
    inputSchema: {
      type: 'object',
      properties: {
        folder: { type: 'string', enum: ['inbox', 'sent', 'system', 'reports'], default: 'inbox' },
        limit: { type: 'integer', default: 50 }
      }
    }
  },
  game_get_reports: {
    description: 'Get battle reports',
    inputSchema: {
      type: 'object',
      properties: {
        type: { type: 'string', enum: ['all', 'attack', 'defense', 'scout'], default: 'all' },
        limit: { type: 'integer', default: 50 }
      }
    }
  },
  game_track_changes: {
    description: 'Track game state changes',
    inputSchema: {
      type: 'object',
      properties: {
        categories: { type: 'array', items: { type: 'string' }, description: 'Categories to track' },
        interval: { type: 'integer', default: 5000, description: 'Update interval in ms' }
      }
    }
  },

  // =========================================================================
  // 5. AUTOPILOT (10 tools)
  // =========================================================================
  autopilot_start: {
    description: 'Start autopilot automation',
    inputSchema: {
      type: 'object',
      properties: {
        profile: { type: 'string', description: 'Automation profile to use' }
      }
    }
  },
  autopilot_stop: {
    description: 'Stop autopilot',
    inputSchema: { type: 'object', properties: {} }
  },
  autopilot_pause: {
    description: 'Pause autopilot',
    inputSchema: { type: 'object', properties: {} }
  },
  autopilot_resume: {
    description: 'Resume paused autopilot',
    inputSchema: { type: 'object', properties: {} }
  },
  autopilot_get_status: {
    description: 'Get autopilot status',
    inputSchema: { type: 'object', properties: {} }
  },
  autopilot_get_config: {
    description: 'Get autopilot configuration',
    inputSchema: { type: 'object', properties: {} }
  },
  autopilot_set_config: {
    description: 'Set autopilot configuration',
    inputSchema: {
      type: 'object',
      properties: {
        config: { type: 'object', description: 'Configuration object' }
      },
      required: ['config']
    }
  },
  autopilot_add_task: {
    description: 'Add task to autopilot queue',
    inputSchema: {
      type: 'object',
      properties: {
        task: { type: 'string', description: 'Task type' },
        params: { type: 'object', description: 'Task parameters' },
        priority: { type: 'integer', default: 5, description: 'Priority (1-10)' },
        schedule: { type: 'string', description: 'Cron schedule (optional)' }
      },
      required: ['task']
    }
  },
  autopilot_remove_task: {
    description: 'Remove task from autopilot',
    inputSchema: {
      type: 'object',
      properties: {
        taskId: { type: 'string', description: 'Task ID to remove' }
      },
      required: ['taskId']
    }
  },
  autopilot_get_logs: {
    description: 'Get autopilot logs',
    inputSchema: {
      type: 'object',
      properties: {
        limit: { type: 'integer', default: 100 },
        level: { type: 'string', enum: ['all', 'info', 'warning', 'error'], default: 'all' }
      }
    }
  },

  // =========================================================================
  // 6. COMBAT (8 tools)
  // =========================================================================
  combat_simulate: {
    description: 'Simulate battle outcome',
    inputSchema: {
      type: 'object',
      properties: {
        attacker: { type: 'object', description: 'Attacker composition' },
        defender: { type: 'object', description: 'Defender composition' },
        iterations: { type: 'integer', default: 1000, description: 'Monte Carlo iterations' }
      },
      required: ['attacker', 'defender']
    }
  },
  combat_optimize: {
    description: 'Optimize troop composition',
    inputSchema: {
      type: 'object',
      properties: {
        target: { type: 'object', description: 'Target to attack' },
        availableTroops: { type: 'object', description: 'Available troops' },
        objective: { type: 'string', enum: ['minimize_losses', 'maximize_damage', 'balanced'], default: 'balanced' }
      },
      required: ['target', 'availableTroops']
    }
  },
  combat_plan_rally: {
    description: 'Plan rally attack',
    inputSchema: {
      type: 'object',
      properties: {
        targetX: { type: 'integer' },
        targetY: { type: 'integer' },
        rallySize: { type: 'integer', description: 'Number of participants' },
        timing: { type: 'string', description: 'Rally timing' }
      },
      required: ['targetX', 'targetY']
    }
  },
  combat_analyze_defense: {
    description: 'Analyze defense setup',
    inputSchema: {
      type: 'object',
      properties: {
        cityId: { type: 'integer', description: 'City to analyze (own city if not specified)' }
      }
    }
  },
  combat_get_history: {
    description: 'Get combat history',
    inputSchema: {
      type: 'object',
      properties: {
        limit: { type: 'integer', default: 50 },
        type: { type: 'string', enum: ['all', 'wins', 'losses'], default: 'all' }
      }
    }
  },
  combat_calculate_power: {
    description: 'Calculate army power',
    inputSchema: {
      type: 'object',
      properties: {
        troops: { type: 'object', description: 'Troop composition' },
        heroes: { type: 'array', items: { type: 'integer' }, description: 'Hero IDs' },
        buffs: { type: 'array', items: { type: 'string' }, description: 'Active buffs' }
      },
      required: ['troops']
    }
  },
  combat_compare_armies: {
    description: 'Compare two armies',
    inputSchema: {
      type: 'object',
      properties: {
        army1: { type: 'object', description: 'First army' },
        army2: { type: 'object', description: 'Second army' }
      },
      required: ['army1', 'army2']
    }
  },
  combat_get_buffs: {
    description: 'Get active combat buffs',
    inputSchema: { type: 'object', properties: {} }
  },

  // =========================================================================
  // 7. MAP INTELLIGENCE (8 tools)
  // =========================================================================
  map_scan_area: {
    description: 'Scan map area',
    inputSchema: {
      type: 'object',
      properties: {
        centerX: { type: 'integer' },
        centerY: { type: 'integer' },
        radius: { type: 'integer', default: 50 }
      },
      required: ['centerX', 'centerY']
    }
  },
  map_find_targets: {
    description: 'Find farm targets',
    inputSchema: {
      type: 'object',
      properties: {
        centerX: { type: 'integer' },
        centerY: { type: 'integer' },
        radius: { type: 'integer', default: 100 },
        minPower: { type: 'integer', default: 0 },
        maxPower: { type: 'integer', default: 1000000 },
        limit: { type: 'integer', default: 50 }
      },
      required: ['centerX', 'centerY']
    }
  },
  map_find_monsters: {
    description: 'Find monsters on map',
    inputSchema: {
      type: 'object',
      properties: {
        centerX: { type: 'integer' },
        centerY: { type: 'integer' },
        radius: { type: 'integer', default: 100 },
        minLevel: { type: 'integer', default: 1 },
        maxLevel: { type: 'integer', default: 50 },
        type: { type: 'string', description: 'Monster type filter' }
      },
      required: ['centerX', 'centerY']
    }
  },
  map_find_resources: {
    description: 'Find resource tiles',
    inputSchema: {
      type: 'object',
      properties: {
        centerX: { type: 'integer' },
        centerY: { type: 'integer' },
        radius: { type: 'integer', default: 100 },
        resourceType: { type: 'string', enum: ['food', 'wood', 'stone', 'iron', 'gold', 'all'], default: 'all' },
        minLevel: { type: 'integer', default: 1 }
      },
      required: ['centerX', 'centerY']
    }
  },
  map_track_players: {
    description: 'Track player movements',
    inputSchema: {
      type: 'object',
      properties: {
        playerIds: { type: 'array', items: { type: 'string' }, description: 'Player IDs to track' },
        interval: { type: 'integer', default: 60000, description: 'Update interval in ms' }
      },
      required: ['playerIds']
    }
  },
  map_get_tile_info: {
    description: 'Get tile information',
    inputSchema: {
      type: 'object',
      properties: {
        x: { type: 'integer' },
        y: { type: 'integer' }
      },
      required: ['x', 'y']
    }
  },
  map_calculate_distance: {
    description: 'Calculate distance between points',
    inputSchema: {
      type: 'object',
      properties: {
        fromX: { type: 'integer' },
        fromY: { type: 'integer' },
        toX: { type: 'integer' },
        toY: { type: 'integer' }
      },
      required: ['fromX', 'fromY', 'toX', 'toY']
    }
  },
  map_export_data: {
    description: 'Export map data',
    inputSchema: {
      type: 'object',
      properties: {
        centerX: { type: 'integer' },
        centerY: { type: 'integer' },
        radius: { type: 'integer', default: 100 },
        format: { type: 'string', enum: ['json', 'csv'], default: 'json' }
      },
      required: ['centerX', 'centerY']
    }
  },

  // =========================================================================
  // 8. PACKET OPERATIONS (12 tools)
  // =========================================================================
  packet_capture_start: {
    description: 'Start packet capture',
    inputSchema: {
      type: 'object',
      properties: {
        filter: { type: 'string', description: 'Action filter pattern' },
        maxPackets: { type: 'integer', default: 10000 }
      }
    }
  },
  packet_capture_stop: {
    description: 'Stop packet capture',
    inputSchema: { type: 'object', properties: {} }
  },
  packet_list: {
    description: 'List captured packets',
    inputSchema: {
      type: 'object',
      properties: {
        limit: { type: 'integer', default: 100 },
        direction: { type: 'string', enum: ['all', 'request', 'response'], default: 'all' },
        filter: { type: 'string', description: 'Action filter' }
      }
    }
  },
  packet_get: {
    description: 'Get packet details',
    inputSchema: {
      type: 'object',
      properties: {
        packetId: { type: 'string', description: 'Packet ID' }
      },
      required: ['packetId']
    }
  },
  packet_decode: {
    description: 'Decode packet data',
    inputSchema: {
      type: 'object',
      properties: {
        data: { type: 'string', description: 'Raw packet data (hex or base64)' },
        format: { type: 'string', enum: ['hex', 'base64'], default: 'hex' }
      },
      required: ['data']
    }
  },
  packet_encode: {
    description: 'Encode data to packet format',
    inputSchema: {
      type: 'object',
      properties: {
        action: { type: 'string', description: 'Action name' },
        params: { type: 'object', description: 'Action parameters' },
        outputFormat: { type: 'string', enum: ['hex', 'base64'], default: 'hex' }
      },
      required: ['action', 'params']
    }
  },
  packet_inject: {
    description: 'Inject packet into game',
    inputSchema: {
      type: 'object',
      properties: {
        action: { type: 'string', description: 'Action name' },
        params: { type: 'object', description: 'Action parameters' },
        raw: { type: 'string', description: 'Raw packet data (alternative to action/params)' }
      }
    }
  },
  packet_replay: {
    description: 'Replay captured packets',
    inputSchema: {
      type: 'object',
      properties: {
        packetIds: { type: 'array', items: { type: 'string' }, description: 'Packet IDs to replay' },
        speed: { type: 'number', default: 1.0, description: 'Replay speed multiplier' },
        modifyParams: { type: 'object', description: 'Parameters to modify during replay' }
      },
      required: ['packetIds']
    }
  },
  packet_compare: {
    description: 'Compare two packets',
    inputSchema: {
      type: 'object',
      properties: {
        packetId1: { type: 'string' },
        packetId2: { type: 'string' }
      },
      required: ['packetId1', 'packetId2']
    }
  },
  packet_search: {
    description: 'Search packets',
    inputSchema: {
      type: 'object',
      properties: {
        query: { type: 'string', description: 'Search query' },
        searchIn: { type: 'string', enum: ['action', 'params', 'raw', 'all'], default: 'all' },
        limit: { type: 'integer', default: 50 }
      },
      required: ['query']
    }
  },
  packet_export: {
    description: 'Export packets',
    inputSchema: {
      type: 'object',
      properties: {
        packetIds: { type: 'array', items: { type: 'string' }, description: 'Specific packets (all if not specified)' },
        format: { type: 'string', enum: ['json', 'pcap', 'har'], default: 'json' },
        filename: { type: 'string' }
      }
    }
  },
  packet_clear: {
    description: 'Clear captured packets',
    inputSchema: { type: 'object', properties: {} }
  },

  // =========================================================================
  // 9. PROTOCOL ANALYSIS (8 tools)
  // =========================================================================
  protocol_lookup: {
    description: 'Look up protocol action',
    inputSchema: {
      type: 'object',
      properties: {
        action: { type: 'string', description: 'Action name to look up' }
      },
      required: ['action']
    }
  },
  protocol_list: {
    description: 'List protocol actions',
    inputSchema: {
      type: 'object',
      properties: {
        category: { type: 'string', description: 'Category filter' },
        limit: { type: 'integer', default: 100 }
      }
    }
  },
  protocol_search: {
    description: 'Search protocol actions',
    inputSchema: {
      type: 'object',
      properties: {
        query: { type: 'string', description: 'Search query' },
        limit: { type: 'integer', default: 50 }
      },
      required: ['query']
    }
  },
  protocol_learn: {
    description: 'Learn new protocol action from traffic',
    inputSchema: {
      type: 'object',
      properties: {
        packetId: { type: 'string', description: 'Packet ID to learn from' },
        name: { type: 'string', description: 'Action name (auto-detect if not specified)' },
        description: { type: 'string', description: 'Action description' }
      },
      required: ['packetId']
    }
  },
  protocol_export: {
    description: 'Export protocol database',
    inputSchema: {
      type: 'object',
      properties: {
        format: { type: 'string', enum: ['json', 'markdown', 'typescript'], default: 'json' },
        category: { type: 'string', description: 'Category filter' }
      }
    }
  },
  protocol_import: {
    description: 'Import protocol database',
    inputSchema: {
      type: 'object',
      properties: {
        data: { type: 'string', description: 'Protocol data to import' },
        format: { type: 'string', enum: ['json'], default: 'json' },
        merge: { type: 'boolean', default: true, description: 'Merge with existing' }
      },
      required: ['data']
    }
  },
  protocol_validate: {
    description: 'Validate action parameters',
    inputSchema: {
      type: 'object',
      properties: {
        action: { type: 'string', description: 'Action name' },
        params: { type: 'object', description: 'Parameters to validate' }
      },
      required: ['action', 'params']
    }
  },
  protocol_generate_docs: {
    description: 'Generate protocol documentation',
    inputSchema: {
      type: 'object',
      properties: {
        category: { type: 'string', description: 'Category filter' },
        format: { type: 'string', enum: ['markdown', 'html'], default: 'markdown' }
      }
    }
  },

  // =========================================================================
  // 10. FUZZING (6 tools)
  // =========================================================================
  fuzz_start: {
    description: 'Start protocol fuzzing',
    inputSchema: {
      type: 'object',
      properties: {
        mode: { type: 'string', enum: ['action_discovery', 'parameter_boundary', 'type_confusion', 'sequence_breaking'], default: 'action_discovery' },
        target: { type: 'string', description: 'Target action or category' },
        parallelism: { type: 'integer', default: 3 },
        delayMs: { type: 'integer', default: 100 }
      }
    }
  },
  fuzz_stop: {
    description: 'Stop fuzzing',
    inputSchema: { type: 'object', properties: {} }
  },
  fuzz_get_status: {
    description: 'Get fuzzing status',
    inputSchema: { type: 'object', properties: {} }
  },
  fuzz_get_results: {
    description: 'Get fuzzing results',
    inputSchema: {
      type: 'object',
      properties: {
        type: { type: 'string', enum: ['all', 'discoveries', 'errors'], default: 'all' }
      }
    }
  },
  fuzz_set_config: {
    description: 'Set fuzzing configuration',
    inputSchema: {
      type: 'object',
      properties: {
        config: { type: 'object', description: 'Fuzzing configuration' }
      },
      required: ['config']
    }
  },
  fuzz_export_discoveries: {
    description: 'Export fuzzing discoveries',
    inputSchema: {
      type: 'object',
      properties: {
        format: { type: 'string', enum: ['json', 'markdown'], default: 'json' }
      }
    }
  },

  // =========================================================================
  // 11. FIDDLER INTEGRATION (8 tools)
  // =========================================================================
  fiddler_connect: {
    description: 'Connect to Fiddler',
    inputSchema: {
      type: 'object',
      properties: {
        pipeName: { type: 'string', default: 'SvonyFiddlerPipe' }
      }
    }
  },
  fiddler_disconnect: {
    description: 'Disconnect from Fiddler',
    inputSchema: { type: 'object', properties: {} }
  },
  fiddler_get_status: {
    description: 'Get Fiddler connection status',
    inputSchema: { type: 'object', properties: {} }
  },
  fiddler_capture_start: {
    description: 'Start Fiddler capture',
    inputSchema: {
      type: 'object',
      properties: {
        filter: { type: 'string', default: '*evony*', description: 'URL filter' }
      }
    }
  },
  fiddler_capture_stop: {
    description: 'Stop Fiddler capture',
    inputSchema: { type: 'object', properties: {} }
  },
  fiddler_set_breakpoint: {
    description: 'Set Fiddler breakpoint',
    inputSchema: {
      type: 'object',
      properties: {
        type: { type: 'string', enum: ['request', 'response'], default: 'request' },
        pattern: { type: 'string', description: 'URL or action pattern' },
        action: { type: 'string', enum: ['break', 'modify', 'drop'], default: 'break' }
      },
      required: ['pattern']
    }
  },
  fiddler_inject: {
    description: 'Inject packet via Fiddler',
    inputSchema: {
      type: 'object',
      properties: {
        action: { type: 'string', description: 'Action name' },
        params: { type: 'object', description: 'Action parameters' }
      },
      required: ['action', 'params']
    }
  },
  fiddler_export_session: {
    description: 'Export Fiddler session',
    inputSchema: {
      type: 'object',
      properties: {
        format: { type: 'string', enum: ['saz', 'har', 'json'], default: 'saz' },
        filename: { type: 'string' }
      }
    }
  },

  // =========================================================================
  // 12. LLM INTEGRATION (10 tools)
  // =========================================================================
  llm_connect: {
    description: 'Connect to LM Studio',
    inputSchema: {
      type: 'object',
      properties: {
        host: { type: 'string', default: 'localhost' },
        port: { type: 'integer', default: 1234 }
      }
    }
  },
  llm_disconnect: {
    description: 'Disconnect from LM Studio',
    inputSchema: { type: 'object', properties: {} }
  },
  llm_get_status: {
    description: 'Get LLM connection status',
    inputSchema: { type: 'object', properties: {} }
  },
  llm_explain_packet: {
    description: 'Use LLM to explain packet',
    inputSchema: {
      type: 'object',
      properties: {
        packetId: { type: 'string', description: 'Packet ID to explain' },
        detail: { type: 'string', enum: ['brief', 'detailed', 'technical'], default: 'detailed' }
      },
      required: ['packetId']
    }
  },
  llm_generate_script: {
    description: 'Generate automation script using LLM',
    inputSchema: {
      type: 'object',
      properties: {
        task: { type: 'string', description: 'Task description' },
        language: { type: 'string', enum: ['python', 'javascript', 'csharp'], default: 'python' }
      },
      required: ['task']
    }
  },
  llm_decode_unknown: {
    description: 'Use LLM to decode unknown structure',
    inputSchema: {
      type: 'object',
      properties: {
        data: { type: 'string', description: 'Unknown data (hex or base64)' },
        context: { type: 'string', description: 'Context about the data' }
      },
      required: ['data']
    }
  },
  llm_ask: {
    description: 'Ask LLM a question about Evony',
    inputSchema: {
      type: 'object',
      properties: {
        question: { type: 'string', description: 'Question to ask' },
        includeContext: { type: 'boolean', default: true, description: 'Include game context' }
      },
      required: ['question']
    }
  },
  llm_get_stats: {
    description: 'Get LLM inference statistics',
    inputSchema: { type: 'object', properties: {} }
  },
  llm_set_config: {
    description: 'Set LLM configuration',
    inputSchema: {
      type: 'object',
      properties: {
        model: { type: 'string', description: 'Model name' },
        temperature: { type: 'number', default: 0.7 },
        maxTokens: { type: 'integer', default: 2048 },
        systemPrompt: { type: 'string', description: 'System prompt override' }
      }
    }
  },
  llm_list_models: {
    description: 'List available LLM models',
    inputSchema: { type: 'object', properties: {} }
  },

  // =========================================================================
  // 13. CHAT INTERFACE (8 tools)
  // =========================================================================
  chat_send: {
    description: 'Send message to chatbot',
    inputSchema: {
      type: 'object',
      properties: {
        message: { type: 'string', description: 'Message to send' },
        context: { type: 'object', description: 'Additional context' }
      },
      required: ['message']
    }
  },
  chat_get_history: {
    description: 'Get chat history',
    inputSchema: {
      type: 'object',
      properties: {
        limit: { type: 'integer', default: 50 }
      }
    }
  },
  chat_clear: {
    description: 'Clear chat history',
    inputSchema: { type: 'object', properties: {} }
  },
  chat_set_context: {
    description: 'Set chat context',
    inputSchema: {
      type: 'object',
      properties: {
        context: { type: 'object', description: 'Context object' }
      },
      required: ['context']
    }
  },
  chat_get_context: {
    description: 'Get current chat context',
    inputSchema: { type: 'object', properties: {} }
  },
  chat_list_templates: {
    description: 'List prompt templates',
    inputSchema: {
      type: 'object',
      properties: {
        category: { type: 'string', description: 'Category filter' }
      }
    }
  },
  chat_use_template: {
    description: 'Use prompt template',
    inputSchema: {
      type: 'object',
      properties: {
        templateId: { type: 'string', description: 'Template ID' },
        variables: { type: 'object', description: 'Template variables' }
      },
      required: ['templateId']
    }
  },
  chat_export: {
    description: 'Export chat history',
    inputSchema: {
      type: 'object',
      properties: {
        format: { type: 'string', enum: ['json', 'markdown', 'txt'], default: 'json' }
      }
    }
  },

  // =========================================================================
  // 14. ANALYTICS (8 tools)
  // =========================================================================
  analytics_get_summary: {
    description: 'Get analytics summary',
    inputSchema: {
      type: 'object',
      properties: {
        period: { type: 'string', enum: ['day', 'week', 'month', 'all'], default: 'week' }
      }
    }
  },
  analytics_get_power: {
    description: 'Get power history',
    inputSchema: {
      type: 'object',
      properties: {
        period: { type: 'string', enum: ['day', 'week', 'month'], default: 'week' }
      }
    }
  },
  analytics_get_resources: {
    description: 'Get resource efficiency analytics',
    inputSchema: {
      type: 'object',
      properties: {
        period: { type: 'string', enum: ['day', 'week', 'month'], default: 'week' }
      }
    }
  },
  analytics_get_combat: {
    description: 'Get combat statistics',
    inputSchema: {
      type: 'object',
      properties: {
        period: { type: 'string', enum: ['day', 'week', 'month'], default: 'week' }
      }
    }
  },
  analytics_get_activity: {
    description: 'Get activity patterns',
    inputSchema: {
      type: 'object',
      properties: {
        period: { type: 'string', enum: ['day', 'week', 'month'], default: 'week' }
      }
    }
  },
  analytics_set_period: {
    description: 'Set default analysis period',
    inputSchema: {
      type: 'object',
      properties: {
        period: { type: 'string', enum: ['day', 'week', 'month'] }
      },
      required: ['period']
    }
  },
  analytics_export: {
    description: 'Export analytics data',
    inputSchema: {
      type: 'object',
      properties: {
        period: { type: 'string', enum: ['day', 'week', 'month', 'all'], default: 'all' },
        format: { type: 'string', enum: ['json', 'csv', 'xlsx'], default: 'json' }
      }
    }
  },
  analytics_reset: {
    description: 'Reset analytics data',
    inputSchema: {
      type: 'object',
      properties: {
        confirm: { type: 'boolean', description: 'Confirm reset' }
      },
      required: ['confirm']
    }
  },

  // =========================================================================
  // 15. WEBHOOKS (8 tools)
  // =========================================================================
  webhook_list: {
    description: 'List configured webhooks',
    inputSchema: { type: 'object', properties: {} }
  },
  webhook_add: {
    description: 'Add webhook',
    inputSchema: {
      type: 'object',
      properties: {
        name: { type: 'string', description: 'Webhook name' },
        url: { type: 'string', description: 'Webhook URL' },
        platform: { type: 'string', enum: ['discord', 'telegram', 'slack', 'teams', 'custom'], default: 'custom' },
        events: { type: 'array', items: { type: 'string' }, description: 'Events to trigger on' }
      },
      required: ['name', 'url']
    }
  },
  webhook_remove: {
    description: 'Remove webhook',
    inputSchema: {
      type: 'object',
      properties: {
        webhookId: { type: 'string', description: 'Webhook ID' }
      },
      required: ['webhookId']
    }
  },
  webhook_test: {
    description: 'Test webhook',
    inputSchema: {
      type: 'object',
      properties: {
        webhookId: { type: 'string', description: 'Webhook ID' }
      },
      required: ['webhookId']
    }
  },
  webhook_set_events: {
    description: 'Configure webhook events',
    inputSchema: {
      type: 'object',
      properties: {
        webhookId: { type: 'string' },
        events: { type: 'array', items: { type: 'string' } }
      },
      required: ['webhookId', 'events']
    }
  },
  webhook_get_logs: {
    description: 'Get webhook logs',
    inputSchema: {
      type: 'object',
      properties: {
        webhookId: { type: 'string' },
        limit: { type: 'integer', default: 50 }
      }
    }
  },
  webhook_enable: {
    description: 'Enable webhook',
    inputSchema: {
      type: 'object',
      properties: {
        webhookId: { type: 'string' }
      },
      required: ['webhookId']
    }
  },
  webhook_disable: {
    description: 'Disable webhook',
    inputSchema: {
      type: 'object',
      properties: {
        webhookId: { type: 'string' }
      },
      required: ['webhookId']
    }
  },

  // =========================================================================
  // 16. RECORDING (8 tools)
  // =========================================================================
  recording_start: {
    description: 'Start session recording',
    inputSchema: {
      type: 'object',
      properties: {
        name: { type: 'string', description: 'Recording name' },
        filter: { type: 'string', description: 'Action filter' }
      }
    }
  },
  recording_stop: {
    description: 'Stop session recording',
    inputSchema: { type: 'object', properties: {} }
  },
  recording_list: {
    description: 'List recordings',
    inputSchema: { type: 'object', properties: {} }
  },
  recording_get: {
    description: 'Get recording details',
    inputSchema: {
      type: 'object',
      properties: {
        recordingId: { type: 'string' }
      },
      required: ['recordingId']
    }
  },
  recording_play: {
    description: 'Play recording',
    inputSchema: {
      type: 'object',
      properties: {
        recordingId: { type: 'string' },
        speed: { type: 'number', default: 1.0 },
        startFrom: { type: 'integer', default: 0, description: 'Start from packet index' }
      },
      required: ['recordingId']
    }
  },
  recording_export: {
    description: 'Export recording',
    inputSchema: {
      type: 'object',
      properties: {
        recordingId: { type: 'string' },
        format: { type: 'string', enum: ['json', 'pcap'], default: 'json' }
      },
      required: ['recordingId']
    }
  },
  recording_import: {
    description: 'Import recording',
    inputSchema: {
      type: 'object',
      properties: {
        data: { type: 'string', description: 'Recording data' },
        format: { type: 'string', enum: ['json', 'pcap'], default: 'json' }
      },
      required: ['data']
    }
  },
  recording_delete: {
    description: 'Delete recording',
    inputSchema: {
      type: 'object',
      properties: {
        recordingId: { type: 'string' }
      },
      required: ['recordingId']
    }
  },

  // =========================================================================
  // 17. STATUS BAR (8 tools)
  // =========================================================================
  statusbar_get_widgets: {
    description: 'Get all status bar widgets',
    inputSchema: { type: 'object', properties: {} }
  },
  statusbar_update: {
    description: 'Update widget value',
    inputSchema: {
      type: 'object',
      properties: {
        widgetId: { type: 'string', description: 'Widget ID' },
        value: { type: 'number', description: 'New value' },
        text: { type: 'string', description: 'Display text' }
      },
      required: ['widgetId']
    }
  },
  statusbar_show: {
    description: 'Show widget',
    inputSchema: {
      type: 'object',
      properties: {
        widgetId: { type: 'string' }
      },
      required: ['widgetId']
    }
  },
  statusbar_hide: {
    description: 'Hide widget',
    inputSchema: {
      type: 'object',
      properties: {
        widgetId: { type: 'string' }
      },
      required: ['widgetId']
    }
  },
  statusbar_set_layout: {
    description: 'Set status bar layout',
    inputSchema: {
      type: 'object',
      properties: {
        layout: { type: 'object', description: 'Layout configuration' }
      },
      required: ['layout']
    }
  },
  statusbar_save_preset: {
    description: 'Save status bar preset',
    inputSchema: {
      type: 'object',
      properties: {
        name: { type: 'string', description: 'Preset name' }
      },
      required: ['name']
    }
  },
  statusbar_load_preset: {
    description: 'Load status bar preset',
    inputSchema: {
      type: 'object',
      properties: {
        name: { type: 'string', description: 'Preset name' }
      },
      required: ['name']
    }
  },
  statusbar_reset: {
    description: 'Reset status bar to defaults',
    inputSchema: { type: 'object', properties: {} }
  },

  // =========================================================================
  // 18. SETTINGS (8 tools)
  // =========================================================================
  settings_get: {
    description: 'Get setting value',
    inputSchema: {
      type: 'object',
      properties: {
        key: { type: 'string', description: 'Setting key' }
      },
      required: ['key']
    }
  },
  settings_set: {
    description: 'Set setting value',
    inputSchema: {
      type: 'object',
      properties: {
        key: { type: 'string', description: 'Setting key' },
        value: { description: 'Setting value' }
      },
      required: ['key', 'value']
    }
  },
  settings_list: {
    description: 'List all settings',
    inputSchema: {
      type: 'object',
      properties: {
        category: { type: 'string', description: 'Category filter' }
      }
    }
  },
  settings_reset: {
    description: 'Reset settings to defaults',
    inputSchema: {
      type: 'object',
      properties: {
        category: { type: 'string', description: 'Category to reset (all if not specified)' },
        confirm: { type: 'boolean' }
      },
      required: ['confirm']
    }
  },
  settings_export: {
    description: 'Export settings',
    inputSchema: {
      type: 'object',
      properties: {
        format: { type: 'string', enum: ['json', 'yaml'], default: 'json' }
      }
    }
  },
  settings_import: {
    description: 'Import settings',
    inputSchema: {
      type: 'object',
      properties: {
        data: { type: 'string', description: 'Settings data' },
        format: { type: 'string', enum: ['json', 'yaml'], default: 'json' },
        merge: { type: 'boolean', default: true }
      },
      required: ['data']
    }
  },
  settings_validate: {
    description: 'Validate settings',
    inputSchema: { type: 'object', properties: {} }
  },
  settings_get_schema: {
    description: 'Get settings schema',
    inputSchema: {
      type: 'object',
      properties: {
        category: { type: 'string', description: 'Category filter' }
      }
    }
  },

  // =========================================================================
  // 19. MCP MANAGEMENT (6 tools)
  // =========================================================================
  mcp_get_status: {
    description: 'Get all MCP server status',
    inputSchema: { type: 'object', properties: {} }
  },
  mcp_connect: {
    description: 'Connect to MCP server',
    inputSchema: {
      type: 'object',
      properties: {
        server: { type: 'string', description: 'Server name (rag, rte, tools, advanced, v4)' }
      },
      required: ['server']
    }
  },
  mcp_disconnect: {
    description: 'Disconnect from MCP server',
    inputSchema: {
      type: 'object',
      properties: {
        server: { type: 'string', description: 'Server name' }
      },
      required: ['server']
    }
  },
  mcp_list_tools: {
    description: 'List available MCP tools',
    inputSchema: {
      type: 'object',
      properties: {
        server: { type: 'string', description: 'Server name (all if not specified)' },
        category: { type: 'string', description: 'Category filter' }
      }
    }
  },
  mcp_call_tool: {
    description: 'Call MCP tool directly',
    inputSchema: {
      type: 'object',
      properties: {
        tool: { type: 'string', description: 'Tool name' },
        params: { type: 'object', description: 'Tool parameters' }
      },
      required: ['tool']
    }
  },
  mcp_get_logs: {
    description: 'Get MCP logs',
    inputSchema: {
      type: 'object',
      properties: {
        server: { type: 'string' },
        limit: { type: 'integer', default: 100 },
        level: { type: 'string', enum: ['all', 'info', 'warning', 'error'], default: 'all' }
      }
    }
  },

  // =========================================================================
  // 20. EXPORT/IMPORT (8 tools)
  // =========================================================================
  export_all: {
    description: 'Export all data',
    inputSchema: {
      type: 'object',
      properties: {
        filename: { type: 'string', description: 'Output filename' },
        includeSettings: { type: 'boolean', default: true },
        includeProtocols: { type: 'boolean', default: true },
        includePackets: { type: 'boolean', default: true },
        includeSessions: { type: 'boolean', default: true },
        includeAnalytics: { type: 'boolean', default: true }
      }
    }
  },
  export_protocols: {
    description: 'Export protocol database',
    inputSchema: {
      type: 'object',
      properties: {
        format: { type: 'string', enum: ['json', 'markdown', 'typescript'], default: 'json' },
        filename: { type: 'string' }
      }
    }
  },
  export_packets: {
    description: 'Export captured packets',
    inputSchema: {
      type: 'object',
      properties: {
        format: { type: 'string', enum: ['json', 'pcap', 'har'], default: 'json' },
        filename: { type: 'string' }
      }
    }
  },
  export_sessions: {
    description: 'Export sessions',
    inputSchema: {
      type: 'object',
      properties: {
        format: { type: 'string', enum: ['json', 'encrypted'], default: 'json' },
        filename: { type: 'string' }
      }
    }
  },
  import_protocols: {
    description: 'Import protocol database',
    inputSchema: {
      type: 'object',
      properties: {
        filename: { type: 'string', description: 'File to import' },
        merge: { type: 'boolean', default: true }
      },
      required: ['filename']
    }
  },
  import_packets: {
    description: 'Import packets',
    inputSchema: {
      type: 'object',
      properties: {
        filename: { type: 'string' },
        format: { type: 'string', enum: ['json', 'pcap', 'har'], default: 'json' }
      },
      required: ['filename']
    }
  },
  import_sessions: {
    description: 'Import sessions',
    inputSchema: {
      type: 'object',
      properties: {
        filename: { type: 'string' },
        format: { type: 'string', enum: ['json', 'encrypted'], default: 'json' }
      },
      required: ['filename']
    }
  },
  import_validate: {
    description: 'Validate import file',
    inputSchema: {
      type: 'object',
      properties: {
        filename: { type: 'string' },
        type: { type: 'string', enum: ['protocols', 'packets', 'sessions', 'settings', 'auto'], default: 'auto' }
      },
      required: ['filename']
    }
  }
};

// ============================================================================
// RESOURCES
// ============================================================================

const RESOURCES = [
  {
    uri: 'evony://browser/status',
    name: 'Browser Status',
    description: 'Current browser status including URLs, panels, and connection state',
    mimeType: 'application/json'
  },
  {
    uri: 'evony://game/state',
    name: 'Game State',
    description: 'Current game state including resources, troops, heroes, and buildings',
    mimeType: 'application/json'
  },
  {
    uri: 'evony://packets/recent',
    name: 'Recent Packets',
    description: 'Recently captured packets',
    mimeType: 'application/json'
  },
  {
    uri: 'evony://protocols/database',
    name: 'Protocol Database',
    description: 'Known protocol actions and their definitions',
    mimeType: 'application/json'
  },
  {
    uri: 'evony://analytics/summary',
    name: 'Analytics Summary',
    description: 'Analytics summary data',
    mimeType: 'application/json'
  },
  {
    uri: 'evony://settings/all',
    name: 'All Settings',
    description: 'All application settings',
    mimeType: 'application/json'
  },
  {
    uri: 'evony://mcp/status',
    name: 'MCP Status',
    description: 'Status of all MCP servers',
    mimeType: 'application/json'
  },
  {
    uri: 'evony://llm/status',
    name: 'LLM Status',
    description: 'LLM connection and inference status',
    mimeType: 'application/json'
  }
];

// ============================================================================
// PROMPTS
// ============================================================================

const PROMPTS = [
  {
    name: 'analyze_packet',
    description: 'Analyze a captured packet',
    arguments: [
      { name: 'packetId', description: 'Packet ID to analyze', required: true }
    ]
  },
  {
    name: 'optimize_army',
    description: 'Optimize army composition for a target',
    arguments: [
      { name: 'targetType', description: 'Type of target (player, monster, tile)', required: true },
      { name: 'targetPower', description: 'Target power level', required: false }
    ]
  },
  {
    name: 'generate_automation',
    description: 'Generate automation script',
    arguments: [
      { name: 'task', description: 'Task to automate', required: true },
      { name: 'language', description: 'Programming language', required: false }
    ]
  },
  {
    name: 'explain_protocol',
    description: 'Explain a protocol action',
    arguments: [
      { name: 'action', description: 'Action name to explain', required: true }
    ]
  },
  {
    name: 'daily_strategy',
    description: 'Generate daily strategy recommendations',
    arguments: [
      { name: 'focus', description: 'Focus area (growth, combat, resources)', required: false }
    ]
  }
];

// ============================================================================
// SERVER IMPLEMENTATION
// ============================================================================

const server = new Server(
  {
    name: 'evony-complete',
    version: CONFIG.version
  },
  {
    capabilities: {
      tools: {},
      resources: {},
      prompts: {}
    }
  }
);

// Connect to Svony Browser
async function connectToBrowser() {
  return new Promise((resolve, reject) => {
    browserWs = new WebSocket(`ws://${CONFIG.browserHost}:${CONFIG.browserPort}`);
    
    browserWs.on('open', () => {
      browserConnected = true;
      console.error('[evony-complete] Connected to Svony Browser');
      resolve();
    });
    
    browserWs.on('close', () => {
      browserConnected = false;
      console.error('[evony-complete] Disconnected from Svony Browser');
    });
    
    browserWs.on('error', (err) => {
      console.error('[evony-complete] Browser connection error:', err.message);
      reject(err);
    });
  });
}

// Send command to browser
async function sendToBrowser(command, params) {
  if (!browserConnected) {
    try {
      await connectToBrowser();
    } catch (err) {
      return { error: 'Not connected to Svony Browser. Please start the browser first.' };
    }
  }
  
  return new Promise((resolve, reject) => {
    const timeout = setTimeout(() => {
      reject(new Error('Request timeout'));
    }, 30000);
    
    const messageHandler = (data) => {
      clearTimeout(timeout);
      browserWs.off('message', messageHandler);
      try {
        const response = JSON.parse(data.toString());
        resolve(response.result || response);
      } catch (err) {
        reject(err);
      }
    };
    
    browserWs.on('message', messageHandler);
    browserWs.send(JSON.stringify({ command, params }));
  });
}

// List tools handler
server.setRequestHandler(ListToolsRequestSchema, async () => {
  const tools = Object.entries(TOOLS).map(([name, def]) => ({
    name,
    description: def.description,
    inputSchema: def.inputSchema
  }));
  
  return { tools };
});

// Call tool handler
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;
  
  if (!TOOLS[name]) {
    return {
      content: [{ type: 'text', text: `Unknown tool: ${name}` }],
      isError: true
    };
  }
  
  try {
    const result = await sendToBrowser(name, args || {});
    
    return {
      content: [{
        type: 'text',
        text: JSON.stringify(result, null, 2)
      }]
    };
  } catch (err) {
    return {
      content: [{ type: 'text', text: `Error: ${err.message}` }],
      isError: true
    };
  }
});

// List resources handler
server.setRequestHandler(ListResourcesRequestSchema, async () => {
  return { resources: RESOURCES };
});

// Read resource handler
server.setRequestHandler(ReadResourceRequestSchema, async (request) => {
  const { uri } = request.params;
  
  try {
    // Map URI to command
    const commandMap = {
      'evony://browser/status': 'browser_get_status',
      'evony://game/state': 'game_get_status',
      'evony://packets/recent': 'packet_list',
      'evony://protocols/database': 'protocol_list',
      'evony://analytics/summary': 'analytics_get_summary',
      'evony://settings/all': 'settings_list',
      'evony://mcp/status': 'mcp_get_status',
      'evony://llm/status': 'llm_get_status'
    };
    
    const command = commandMap[uri];
    if (!command) {
      throw new Error(`Unknown resource: ${uri}`);
    }
    
    const result = await sendToBrowser(command, {});
    
    return {
      contents: [{
        uri,
        mimeType: 'application/json',
        text: JSON.stringify(result, null, 2)
      }]
    };
  } catch (err) {
    return {
      contents: [{
        uri,
        mimeType: 'text/plain',
        text: `Error: ${err.message}`
      }]
    };
  }
});

// List prompts handler
server.setRequestHandler(ListPromptsRequestSchema, async () => {
  return { prompts: PROMPTS };
});

// Get prompt handler
server.setRequestHandler(GetPromptRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;
  
  const prompt = PROMPTS.find(p => p.name === name);
  if (!prompt) {
    throw new Error(`Unknown prompt: ${name}`);
  }
  
  // Generate prompt messages based on template
  const messages = [];
  
  switch (name) {
    case 'analyze_packet':
      messages.push({
        role: 'user',
        content: {
          type: 'text',
          text: `Analyze packet ${args.packetId}. Decode the AMF3 data, explain the action, parameters, and expected response. Identify any security implications.`
        }
      });
      break;
      
    case 'optimize_army':
      messages.push({
        role: 'user',
        content: {
          type: 'text',
          text: `Optimize army composition for attacking a ${args.targetType}${args.targetPower ? ` with power ${args.targetPower}` : ''}. Consider available troops, heroes, and buffs.`
        }
      });
      break;
      
    case 'generate_automation':
      messages.push({
        role: 'user',
        content: {
          type: 'text',
          text: `Generate a ${args.language || 'Python'} automation script for: ${args.task}. Include error handling and safety checks.`
        }
      });
      break;
      
    case 'explain_protocol':
      messages.push({
        role: 'user',
        content: {
          type: 'text',
          text: `Explain the protocol action "${args.action}". Include parameters, expected response, use cases, and example code.`
        }
      });
      break;
      
    case 'daily_strategy':
      messages.push({
        role: 'user',
        content: {
          type: 'text',
          text: `Generate daily strategy recommendations${args.focus ? ` focusing on ${args.focus}` : ''}. Consider current game state, events, and optimal resource allocation.`
        }
      });
      break;
  }
  
  return {
    description: prompt.description,
    messages
  };
});

// Start server
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  
  console.error(`[evony-complete] MCP Server v${CONFIG.version} started`);
  console.error(`[evony-complete] ${Object.keys(TOOLS).length} tools available`);
  console.error(`[evony-complete] ${RESOURCES.length} resources available`);
  console.error(`[evony-complete] ${PROMPTS.length} prompts available`);
  
  // Try to connect to browser
  try {
    await connectToBrowser();
  } catch (err) {
    console.error('[evony-complete] Browser not available, will connect on first request');
  }
}

main().catch(console.error);
