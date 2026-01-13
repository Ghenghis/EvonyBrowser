#!/usr/bin/env node

/**
 * Evony Advanced MCP Server
 * 
 * Game-changing MCP server with advanced AI tools for:
 * - Combat simulation and optimization
 * - Strategic planning and recommendations
 * - Map scanning and target discovery
 * - Multi-account coordination
 * - Analytics and insights
 */

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  ListResourcesRequestSchema,
  ReadResourceRequestSchema,
} from '@modelcontextprotocol/sdk/types.js';
import WebSocket from 'ws';
import fs from 'fs';
import path from 'path';

// Configuration
const CONFIG = {
  name: 'evony-advanced',
  version: '2.0.0',
  wsPort: 3004,
  dataPath: process.env.EVONY_DATA_PATH || './data'
};

// WebSocket connection to Svony Browser
let browserWs = null;
let pendingRequests = new Map();

// Initialize MCP Server
const server = new Server(
  { name: CONFIG.name, version: CONFIG.version },
  { capabilities: { tools: {}, resources: {} } }
);

// ============================================================================
// TOOL DEFINITIONS
// ============================================================================

const TOOLS = [
  // Combat Tools
  {
    name: 'simulate_battle',
    description: 'Simulate a battle between attacker and defender to predict outcome, losses, and optimal strategy',
    inputSchema: {
      type: 'object',
      properties: {
        attacker_troops: {
          type: 'object',
          description: 'Attacker troop composition {troop_type: count}',
          additionalProperties: { type: 'number' }
        },
        defender_troops: {
          type: 'object',
          description: 'Defender troop composition {troop_type: count}',
          additionalProperties: { type: 'number' }
        },
        attacker_hero: {
          type: 'object',
          description: 'Attacker hero stats',
          properties: {
            name: { type: 'string' },
            level: { type: 'number' },
            attack_bonus: { type: 'number' },
            defense_bonus: { type: 'number' }
          }
        },
        defender_hero: {
          type: 'object',
          description: 'Defender hero stats'
        },
        wall_level: { type: 'number', description: 'Defender wall level' },
        terrain: { type: 'string', enum: ['plains', 'forest', 'mountain', 'swamp'] }
      },
      required: ['attacker_troops', 'defender_troops']
    }
  },
  {
    name: 'find_optimal_composition',
    description: 'Find the optimal troop composition to defeat a target with minimal losses',
    inputSchema: {
      type: 'object',
      properties: {
        available_troops: {
          type: 'object',
          description: 'Available troops {troop_type: count}'
        },
        target_troops: {
          type: 'object',
          description: 'Target defender troops'
        },
        target_win_rate: {
          type: 'number',
          description: 'Desired win probability (0-1)',
          default: 0.9
        }
      },
      required: ['available_troops', 'target_troops']
    }
  },
  {
    name: 'analyze_rally',
    description: 'Analyze and optimize a rally attack with multiple participants',
    inputSchema: {
      type: 'object',
      properties: {
        target_x: { type: 'number' },
        target_y: { type: 'number' },
        target_power: { type: 'number' },
        participants: {
          type: 'array',
          items: {
            type: 'object',
            properties: {
              name: { type: 'string' },
              troops: { type: 'object' },
              hero: { type: 'object' }
            }
          }
        },
        rally_time_minutes: { type: 'number' }
      },
      required: ['target_x', 'target_y', 'participants']
    }
  },

  // Map & Target Tools
  {
    name: 'scan_area',
    description: 'Scan a map area to discover players, resources, and monsters',
    inputSchema: {
      type: 'object',
      properties: {
        center_x: { type: 'number' },
        center_y: { type: 'number' },
        radius: { type: 'number', default: 50 }
      },
      required: ['center_x', 'center_y']
    }
  },
  {
    name: 'find_farm_targets',
    description: 'Find optimal farm targets based on power ratio, distance, and activity',
    inputSchema: {
      type: 'object',
      properties: {
        from_x: { type: 'number' },
        from_y: { type: 'number' },
        max_power_ratio: { type: 'number', default: 0.5 },
        max_distance: { type: 'number', default: 100 },
        skip_bubbled: { type: 'boolean', default: true },
        max_results: { type: 'number', default: 20 }
      },
      required: ['from_x', 'from_y']
    }
  },
  {
    name: 'find_gathering_spots',
    description: 'Find optimal resource gathering locations',
    inputSchema: {
      type: 'object',
      properties: {
        from_x: { type: 'number' },
        from_y: { type: 'number' },
        resource_type: { type: 'string', enum: ['gold', 'food', 'lumber', 'stone', 'iron'] },
        min_level: { type: 'number', default: 5 },
        max_distance: { type: 'number', default: 50 }
      },
      required: ['from_x', 'from_y']
    }
  },
  {
    name: 'find_monsters',
    description: 'Find monsters in an area for hunting',
    inputSchema: {
      type: 'object',
      properties: {
        from_x: { type: 'number' },
        from_y: { type: 'number' },
        min_level: { type: 'number' },
        max_level: { type: 'number' },
        radius: { type: 'number', default: 100 }
      },
      required: ['from_x', 'from_y']
    }
  },

  // Strategic Planning Tools
  {
    name: 'get_strategic_advice',
    description: 'Get AI-powered strategic advice based on current game state',
    inputSchema: {
      type: 'object',
      properties: {
        focus_area: {
          type: 'string',
          enum: ['growth', 'combat', 'economy', 'heroes', 'alliance', 'events'],
          description: 'Area to focus advice on'
        },
        time_horizon: {
          type: 'string',
          enum: ['immediate', 'daily', 'weekly', 'monthly'],
          default: 'daily'
        }
      }
    }
  },
  {
    name: 'generate_daily_plan',
    description: 'Generate an optimized daily activity plan',
    inputSchema: {
      type: 'object',
      properties: {
        play_time_hours: { type: 'number', description: 'Available play time' },
        priorities: {
          type: 'array',
          items: { type: 'string' },
          description: 'Priority areas (e.g., ["growth", "pvp", "events"])'
        }
      }
    }
  },
  {
    name: 'analyze_alliance_war',
    description: 'Analyze alliance war situation and provide coordination strategy',
    inputSchema: {
      type: 'object',
      properties: {
        enemy_alliance: { type: 'string' },
        our_members: { type: 'number' },
        enemy_members: { type: 'number' },
        war_type: { type: 'string', enum: ['svs', 'kvk', 'alliance_war'] }
      }
    }
  },

  // Analytics Tools
  {
    name: 'get_analytics_summary',
    description: 'Get comprehensive analytics summary with insights',
    inputSchema: {
      type: 'object',
      properties: {
        days: { type: 'number', default: 7, description: 'Days of history to analyze' }
      }
    }
  },
  {
    name: 'get_growth_projection',
    description: 'Project future power growth based on current trends',
    inputSchema: {
      type: 'object',
      properties: {
        days_ahead: { type: 'number', default: 30 }
      }
    }
  },
  {
    name: 'compare_with_alliance',
    description: 'Compare your stats with alliance average',
    inputSchema: { type: 'object', properties: {} }
  },

  // Multi-Account Tools
  {
    name: 'list_accounts',
    description: 'List all managed accounts and their status',
    inputSchema: { type: 'object', properties: {} }
  },
  {
    name: 'switch_account',
    description: 'Switch to a different account',
    inputSchema: {
      type: 'object',
      properties: {
        account_id: { type: 'string' }
      },
      required: ['account_id']
    }
  },
  {
    name: 'coordinate_rally',
    description: 'Coordinate a rally attack across multiple accounts',
    inputSchema: {
      type: 'object',
      properties: {
        target_x: { type: 'number' },
        target_y: { type: 'number' },
        participating_accounts: { type: 'array', items: { type: 'string' } },
        rally_time: { type: 'string', description: 'ISO timestamp for rally' }
      },
      required: ['target_x', 'target_y', 'participating_accounts']
    }
  },
  {
    name: 'mass_resource_transfer',
    description: 'Transfer resources from farm accounts to main account',
    inputSchema: {
      type: 'object',
      properties: {
        target_account: { type: 'string' },
        source_accounts: { type: 'array', items: { type: 'string' } }
      },
      required: ['target_account']
    }
  },

  // Automation Tools
  {
    name: 'start_autopilot',
    description: 'Start intelligent auto-pilot on current or specified accounts',
    inputSchema: {
      type: 'object',
      properties: {
        accounts: { type: 'array', items: { type: 'string' } },
        tasks: {
          type: 'array',
          items: { type: 'string' },
          description: 'Tasks to automate (e.g., ["gather", "train", "build"])'
        }
      }
    }
  },
  {
    name: 'stop_autopilot',
    description: 'Stop auto-pilot on all accounts',
    inputSchema: { type: 'object', properties: {} }
  },
  {
    name: 'schedule_action',
    description: 'Schedule an action to run at a specific time',
    inputSchema: {
      type: 'object',
      properties: {
        action: { type: 'string' },
        parameters: { type: 'object' },
        scheduled_time: { type: 'string', description: 'ISO timestamp' },
        repeat: { type: 'boolean', default: false }
      },
      required: ['action', 'scheduled_time']
    }
  },

  // Recording & Replay Tools
  {
    name: 'start_recording',
    description: 'Start recording game session for replay or analysis',
    inputSchema: {
      type: 'object',
      properties: {
        name: { type: 'string' },
        capture_responses: { type: 'boolean', default: true }
      }
    }
  },
  {
    name: 'stop_recording',
    description: 'Stop current recording session',
    inputSchema: { type: 'object', properties: {} }
  },
  {
    name: 'replay_recording',
    description: 'Replay a recorded session',
    inputSchema: {
      type: 'object',
      properties: {
        recording_id: { type: 'string' },
        speed: { type: 'number', default: 1.0 }
      },
      required: ['recording_id']
    }
  },

  // Export/Import Tools
  {
    name: 'export_data',
    description: 'Export game data, configurations, or analytics',
    inputSchema: {
      type: 'object',
      properties: {
        type: {
          type: 'string',
          enum: ['all', 'gamestate', 'analytics', 'config', 'recordings']
        },
        format: { type: 'string', enum: ['json', 'csv', 'archive'], default: 'json' }
      },
      required: ['type']
    }
  },
  {
    name: 'import_data',
    description: 'Import data from exported file',
    inputSchema: {
      type: 'object',
      properties: {
        file_path: { type: 'string' }
      },
      required: ['file_path']
    }
  },

  // Webhook Tools
  {
    name: 'configure_webhook',
    description: 'Configure a webhook for notifications',
    inputSchema: {
      type: 'object',
      properties: {
        name: { type: 'string' },
        url: { type: 'string' },
        format: { type: 'string', enum: ['discord', 'telegram', 'slack', 'teams', 'generic'] },
        events: {
          type: 'array',
          items: { type: 'string' },
          description: 'Events to notify (e.g., ["under_attack", "march_complete"])'
        }
      },
      required: ['name', 'url', 'events']
    }
  },
  {
    name: 'test_webhook',
    description: 'Send a test notification to a webhook',
    inputSchema: {
      type: 'object',
      properties: {
        webhook_name: { type: 'string' }
      },
      required: ['webhook_name']
    }
  }
];

// ============================================================================
// TOOL HANDLERS
// ============================================================================

async function handleTool(name, args) {
  console.error(`[MCP] Handling tool: ${name}`);
  
  // Forward to Svony Browser via WebSocket
  if (browserWs && browserWs.readyState === WebSocket.OPEN) {
    return await sendToBrowser(name, args);
  }
  
  // Fallback to local handling
  return await handleToolLocally(name, args);
}

async function sendToBrowser(tool, args) {
  return new Promise((resolve, reject) => {
    const requestId = Date.now().toString();
    
    pendingRequests.set(requestId, { resolve, reject });
    
    browserWs.send(JSON.stringify({
      type: 'tool_call',
      requestId,
      tool,
      args
    }));
    
    // Timeout after 30 seconds
    setTimeout(() => {
      if (pendingRequests.has(requestId)) {
        pendingRequests.delete(requestId);
        reject(new Error('Request timeout'));
      }
    }, 30000);
  });
}

async function handleToolLocally(name, args) {
  switch (name) {
    case 'simulate_battle':
      return simulateBattle(args);
    case 'find_optimal_composition':
      return findOptimalComposition(args);
    case 'get_strategic_advice':
      return getStrategicAdvice(args);
    case 'generate_daily_plan':
      return generateDailyPlan(args);
    case 'get_analytics_summary':
      return getAnalyticsSummary(args);
    default:
      return { error: `Tool ${name} requires connection to Svony Browser` };
  }
}

// ============================================================================
// LOCAL TOOL IMPLEMENTATIONS
// ============================================================================

function simulateBattle(args) {
  const { attacker_troops, defender_troops, wall_level = 0, terrain } = args;
  
  // Calculate total power
  const attackerPower = calculatePower(attacker_troops);
  const defenderPower = calculatePower(defender_troops) * (1 + wall_level * 0.05);
  
  // Apply terrain modifiers
  let attackMod = 1.0, defenseMod = 1.0;
  if (terrain === 'mountain') defenseMod = 1.2;
  if (terrain === 'plains') attackMod = 1.1;
  
  const effectiveAttack = attackerPower * attackMod;
  const effectiveDefense = defenderPower * defenseMod;
  
  // Calculate outcome
  const powerRatio = effectiveAttack / (effectiveDefense + 1);
  const winProbability = Math.min(0.99, powerRatio / (powerRatio + 1));
  
  // Estimate losses
  const attackerLossRate = Math.min(0.9, 1 / (powerRatio + 0.5));
  const defenderLossRate = Math.min(0.95, powerRatio / (powerRatio + 0.5));
  
  return {
    winner: winProbability > 0.5 ? 'attacker' : 'defender',
    win_probability: winProbability,
    attacker_power: attackerPower,
    defender_power: defenderPower,
    estimated_attacker_losses: Object.fromEntries(
      Object.entries(attacker_troops).map(([k, v]) => [k, Math.floor(v * attackerLossRate)])
    ),
    estimated_defender_losses: Object.fromEntries(
      Object.entries(defender_troops).map(([k, v]) => [k, Math.floor(v * defenderLossRate)])
    ),
    recommendations: generateBattleRecommendations(powerRatio, attacker_troops, defender_troops)
  };
}

function calculatePower(troops) {
  const powerValues = {
    worker: 1, warrior: 10, scout: 5, pikeman: 20, swordsman: 25,
    archer: 22, cavalry: 40, cataphract: 60, ballista: 50, ram: 45, catapult: 70
  };
  
  return Object.entries(troops).reduce((total, [type, count]) => {
    return total + (powerValues[type.toLowerCase()] || 10) * count;
  }, 0);
}

function generateBattleRecommendations(powerRatio, attackerTroops, defenderTroops) {
  const recommendations = [];
  
  if (powerRatio < 1.5) {
    recommendations.push('Consider adding more troops for a safer victory');
  }
  
  if (defenderTroops.cavalry > 0 && !attackerTroops.pikeman) {
    recommendations.push('Add pikemen to counter enemy cavalry');
  }
  
  if (defenderTroops.archer > 0 && !attackerTroops.cavalry) {
    recommendations.push('Add cavalry to quickly eliminate enemy archers');
  }
  
  if (powerRatio > 3) {
    recommendations.push('Overwhelming force - you could use fewer troops');
  }
  
  return recommendations;
}

function findOptimalComposition(args) {
  const { available_troops, target_troops, target_win_rate = 0.9 } = args;
  
  const targetPower = calculatePower(target_troops);
  const requiredPower = targetPower * (1 / (1 - target_win_rate));
  
  // Prioritize troops by efficiency
  const priorities = ['cavalry', 'cataphract', 'archer', 'pikeman', 'swordsman', 'ballista'];
  const composition = {};
  let currentPower = 0;
  
  for (const troopType of priorities) {
    if (!available_troops[troopType]) continue;
    
    const powerPerTroop = calculatePower({ [troopType]: 1 });
    const needed = Math.ceil((requiredPower - currentPower) / powerPerTroop);
    const toUse = Math.min(needed, available_troops[troopType]);
    
    if (toUse > 0) {
      composition[troopType] = toUse;
      currentPower += toUse * powerPerTroop;
    }
    
    if (currentPower >= requiredPower) break;
  }
  
  return {
    recommended_composition: composition,
    total_power: currentPower,
    target_power: targetPower,
    estimated_win_rate: currentPower / (currentPower + targetPower),
    notes: currentPower < requiredPower 
      ? 'Insufficient troops for target win rate' 
      : 'Optimal composition found'
  };
}

function getStrategicAdvice(args) {
  const { focus_area = 'growth', time_horizon = 'daily' } = args;
  
  const advice = {
    growth: {
      immediate: [
        'Complete all available quests',
        'Collect resources from production buildings',
        'Start longest upgrade before going offline'
      ],
      daily: [
        'Focus on keep upgrades as primary goal',
        'Train troops continuously',
        'Participate in daily events for rewards'
      ],
      weekly: [
        'Plan building upgrades around events',
        'Save speedups for critical moments',
        'Coordinate with alliance for help'
      ]
    },
    combat: {
      immediate: [
        'Scout targets before attacking',
        'Check for bubble status',
        'Verify hero assignments'
      ],
      daily: [
        'Farm inactive players for resources',
        'Hunt monsters for rewards',
        'Participate in alliance rallies'
      ],
      weekly: [
        'Build up troop reserves',
        'Upgrade military technology',
        'Plan for server events'
      ]
    },
    economy: {
      immediate: [
        'Collect all resource tiles',
        'Check market for good deals',
        'Use resource items if needed'
      ],
      daily: [
        'Send gathering marches continuously',
        'Upgrade resource buildings',
        'Trade excess resources'
      ],
      weekly: [
        'Balance resource production',
        'Plan for major upgrades',
        'Stock up before events'
      ]
    }
  };
  
  return {
    focus_area,
    time_horizon,
    recommendations: advice[focus_area]?.[time_horizon] || advice.growth.daily,
    priority_actions: advice[focus_area]?.immediate || advice.growth.immediate
  };
}

function generateDailyPlan(args) {
  const { play_time_hours = 2, priorities = ['growth', 'events'] } = args;
  
  const plan = {
    total_time: play_time_hours,
    schedule: []
  };
  
  // Morning routine (30% of time)
  plan.schedule.push({
    phase: 'Morning Routine',
    duration_minutes: Math.floor(play_time_hours * 60 * 0.3),
    tasks: [
      'Collect overnight production',
      'Start new building upgrades',
      'Queue troop training',
      'Send gathering marches'
    ]
  });
  
  // Main activities (50% of time)
  plan.schedule.push({
    phase: 'Main Activities',
    duration_minutes: Math.floor(play_time_hours * 60 * 0.5),
    tasks: priorities.includes('pvp') 
      ? ['Scout and attack farm targets', 'Participate in rallies', 'Hunt monsters']
      : ['Complete daily quests', 'Participate in events', 'Help alliance members']
  });
  
  // Evening wrap-up (20% of time)
  plan.schedule.push({
    phase: 'Evening Wrap-up',
    duration_minutes: Math.floor(play_time_hours * 60 * 0.2),
    tasks: [
      'Start longest upgrades',
      'Shield if needed',
      'Set up overnight gathering'
    ]
  });
  
  return plan;
}

function getAnalyticsSummary(args) {
  const { days = 7 } = args;
  
  return {
    period_days: days,
    summary: {
      power_growth: 'Data requires connection to Svony Browser',
      resource_efficiency: 'Data requires connection to Svony Browser',
      combat_statistics: 'Data requires connection to Svony Browser'
    },
    insights: [
      'Connect to Svony Browser for detailed analytics',
      'Historical data tracking requires active session'
    ]
  };
}

// ============================================================================
// MCP HANDLERS
// ============================================================================

server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: TOOLS
}));

server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;
  
  try {
    const result = await handleTool(name, args || {});
    return {
      content: [{ type: 'text', text: JSON.stringify(result, null, 2) }]
    };
  } catch (error) {
    return {
      content: [{ type: 'text', text: `Error: ${error.message}` }],
      isError: true
    };
  }
});

server.setRequestHandler(ListResourcesRequestSchema, async () => ({
  resources: [
    {
      uri: 'evony://gamestate',
      name: 'Current Game State',
      description: 'Real-time game state including resources, troops, and buildings',
      mimeType: 'application/json'
    },
    {
      uri: 'evony://analytics',
      name: 'Analytics Dashboard',
      description: 'Historical analytics and performance metrics',
      mimeType: 'application/json'
    },
    {
      uri: 'evony://map',
      name: 'Map Data',
      description: 'Scanned map data including players, resources, and monsters',
      mimeType: 'application/json'
    }
  ]
}));

server.setRequestHandler(ReadResourceRequestSchema, async (request) => {
  const { uri } = request.params;
  
  // Forward to browser or return cached data
  const data = await getResourceData(uri);
  
  return {
    contents: [{
      uri,
      mimeType: 'application/json',
      text: JSON.stringify(data, null, 2)
    }]
  };
});

async function getResourceData(uri) {
  if (browserWs && browserWs.readyState === WebSocket.OPEN) {
    return await sendToBrowser('get_resource', { uri });
  }
  
  return { error: 'Not connected to Svony Browser' };
}

// ============================================================================
// WEBSOCKET CONNECTION
// ============================================================================

function connectToBrowser() {
  try {
    browserWs = new WebSocket(`ws://localhost:${CONFIG.wsPort}`);
    
    browserWs.on('open', () => {
      console.error('[MCP] Connected to Svony Browser');
    });
    
    browserWs.on('message', (data) => {
      try {
        const message = JSON.parse(data);
        
        if (message.requestId && pendingRequests.has(message.requestId)) {
          const { resolve, reject } = pendingRequests.get(message.requestId);
          pendingRequests.delete(message.requestId);
          
          if (message.error) {
            reject(new Error(message.error));
          } else {
            resolve(message.result);
          }
        }
      } catch (error) {
        console.error('[MCP] Error processing message:', error);
      }
    });
    
    browserWs.on('close', () => {
      console.error('[MCP] Disconnected from Svony Browser');
      browserWs = null;
      
      // Reconnect after delay
      setTimeout(connectToBrowser, 5000);
    });
    
    browserWs.on('error', (error) => {
      console.error('[MCP] WebSocket error:', error.message);
    });
  } catch (error) {
    console.error('[MCP] Connection error:', error.message);
    setTimeout(connectToBrowser, 5000);
  }
}

// ============================================================================
// MAIN
// ============================================================================

async function main() {
  console.error('[MCP] Starting Evony Advanced MCP Server...');
  
  // Connect to Svony Browser
  connectToBrowser();
  
  // Start MCP server
  const transport = new StdioServerTransport();
  await server.connect(transport);
  
  console.error('[MCP] Server running');
}

main().catch(console.error);
