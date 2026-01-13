#!/usr/bin/env node
/**
 * Evony Tools MCP Server
 * Provides automation tools, calculators, and utilities for Evony gameplay
 */

import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { z } from 'zod';

// Initialize MCP Server
const server = new McpServer({
  name: 'evony-tools',
  version: '1.0.0',
  description: 'Automation tools, calculators, and utilities for Evony'
});

// ============================================================================
// Game Data Constants
// ============================================================================

const TROOP_STATS = {
  wo: { name: 'Worker', attack: 1, defense: 1, speed: 50, load: 200, food: 1, time: 15 },
  w: { name: 'Warrior', attack: 10, defense: 10, speed: 150, load: 20, food: 2, time: 20 },
  s: { name: 'Scout', attack: 5, defense: 5, speed: 3000, load: 5, food: 3, time: 30 },
  p: { name: 'Pikeman', attack: 30, defense: 30, speed: 150, load: 25, food: 4, time: 60 },
  sw: { name: 'Swordsman', attack: 50, defense: 50, speed: 175, load: 30, food: 5, time: 90 },
  a: { name: 'Archer', attack: 100, defense: 25, speed: 200, load: 35, food: 6, time: 120 },
  c: { name: 'Cavalry', attack: 150, defense: 75, speed: 750, load: 50, food: 8, time: 180 },
  cata: { name: 'Cataphract', attack: 250, defense: 175, speed: 500, load: 75, food: 10, time: 300 },
  t: { name: 'Transporter', attack: 20, defense: 50, speed: 100, load: 5000, food: 5, time: 150 },
  b: { name: 'Ballista', attack: 400, defense: 25, speed: 50, load: 100, food: 10, time: 600 },
  r: { name: 'Ram', attack: 100, defense: 250, speed: 75, load: 150, food: 15, time: 900 },
  cp: { name: 'Catapult', attack: 600, defense: 25, speed: 25, load: 200, food: 20, time: 1200 }
};

const BUILDING_REQUIREMENTS = {
  1: { name: 'Town Hall', prereqs: {}, resources: { food: 100, lumber: 200, stone: 200, iron: 100 } },
  2: { name: 'Cottage', prereqs: {}, resources: { food: 50, lumber: 100, stone: 50, iron: 0 } },
  3: { name: 'Warehouse', prereqs: { 1: 1 }, resources: { food: 100, lumber: 300, stone: 200, iron: 50 } },
  4: { name: 'Barracks', prereqs: { 1: 1 }, resources: { food: 200, lumber: 200, stone: 200, iron: 100 } },
  5: { name: 'Academy', prereqs: { 1: 2 }, resources: { food: 300, lumber: 400, stone: 300, iron: 200 } },
  6: { name: 'Forge', prereqs: { 4: 1 }, resources: { food: 200, lumber: 300, stone: 200, iron: 300 } },
  7: { name: 'Workshop', prereqs: { 5: 1 }, resources: { food: 200, lumber: 400, stone: 300, iron: 200 } },
  8: { name: 'Stable', prereqs: { 4: 2 }, resources: { food: 300, lumber: 300, stone: 200, iron: 200 } },
  9: { name: 'Relief Station', prereqs: { 1: 2 }, resources: { food: 200, lumber: 300, stone: 200, iron: 100 } },
  10: { name: 'Embassy', prereqs: { 1: 1 }, resources: { food: 300, lumber: 400, stone: 400, iron: 200 } },
  11: { name: 'Marketplace', prereqs: { 1: 2 }, resources: { food: 200, lumber: 300, stone: 200, iron: 100 } },
  12: { name: 'Inn', prereqs: { 13: 1 }, resources: { food: 200, lumber: 300, stone: 200, iron: 100 } },
  13: { name: 'Feasting Hall', prereqs: { 1: 2 }, resources: { food: 400, lumber: 500, stone: 400, iron: 200 } },
  14: { name: 'Rally Spot', prereqs: { 1: 1 }, resources: { food: 200, lumber: 300, stone: 300, iron: 100 } },
  15: { name: 'Beacon Tower', prereqs: { 1: 2 }, resources: { food: 100, lumber: 200, stone: 300, iron: 100 } },
  16: { name: 'Walls', prereqs: {}, resources: { food: 0, lumber: 200, stone: 500, iron: 100 } },
  20: { name: 'Farm', prereqs: {}, resources: { food: 0, lumber: 100, stone: 50, iron: 0 } },
  21: { name: 'Sawmill', prereqs: {}, resources: { food: 50, lumber: 0, stone: 50, iron: 0 } },
  22: { name: 'Quarry', prereqs: {}, resources: { food: 50, lumber: 100, stone: 0, iron: 0 } },
  23: { name: 'Ironmine', prereqs: {}, resources: { food: 50, lumber: 100, stone: 50, iron: 0 } }
};

const TECH_DATA = {
  agriculture: { name: 'Agriculture', category: 'economy', effect: 'Food production +5%/level' },
  lumbering: { name: 'Lumbering', category: 'economy', effect: 'Lumber production +5%/level' },
  masonry: { name: 'Masonry', category: 'economy', effect: 'Stone production +5%/level' },
  mining: { name: 'Mining', category: 'economy', effect: 'Iron production +5%/level' },
  metalCasting: { name: 'Metal Casting', category: 'military', effect: 'Troop attack +5%/level' },
  informatics: { name: 'Informatics', category: 'military', effect: 'Scout speed +10%/level' },
  militaryScience: { name: 'Military Science', category: 'military', effect: 'Troop defense +5%/level' },
  militaryTradition: { name: 'Military Tradition', category: 'military', effect: 'Troop HP +5%/level' },
  ironWorking: { name: 'Iron Working', category: 'military', effect: 'Troop attack +2%/level' },
  logistics: { name: 'Logistics', category: 'military', effect: 'March speed +10%/level' },
  compass: { name: 'Compass', category: 'military', effect: 'March speed +5%/level' },
  horsebackRiding: { name: 'Horseback Riding', category: 'military', effect: 'Cavalry speed +5%/level' },
  archery: { name: 'Archery', category: 'military', effect: 'Archer attack +5%/level' },
  stockpile: { name: 'Stockpile', category: 'economy', effect: 'Warehouse capacity +10%/level' },
  medicine: { name: 'Medicine', category: 'economy', effect: 'Population growth +5%/level' },
  construction: { name: 'Construction', category: 'economy', effect: 'Building speed +10%/level' },
  engineering: { name: 'Engineering', category: 'military', effect: 'Fortification defense +5%/level' },
  machinery: { name: 'Machinery', category: 'military', effect: 'Trap attack +5%/level' },
  privateering: { name: 'Privateering', category: 'military', effect: 'Plunder capacity +5%/level' }
};

// ============================================================================
// MCP Tools - Calculators
// ============================================================================

/**
 * Tool: calc_training - Calculate troop training costs and time
 */
server.tool(
  'calc_training',
  'Calculate troop training costs, time, and requirements',
  {
    troop_type: z.string().describe('Troop code (wo, w, s, p, sw, a, c, cata, t, b, r, cp)'),
    amount: z.number().describe('Number of troops to train'),
    barracks_level: z.number().default(10).describe('Barracks level (affects time)'),
    tech_bonus: z.number().default(0).describe('Military science tech level')
  },
  async ({ troop_type, amount, barracks_level, tech_bonus }) => {
    try {
      const stats = TROOP_STATS[troop_type];
      if (!stats) {
        return {
          content: [{
            type: 'text',
            text: JSON.stringify({ error: `Unknown troop type: ${troop_type}` })
          }],
          isError: true
        };
      }
      
      // Base costs (simplified formula)
      const baseCosts = {
        food: stats.food * 10 * amount,
        lumber: stats.food * 5 * amount,
        stone: stats.food * 3 * amount,
        iron: stats.food * 2 * amount,
        gold: stats.food * 5 * amount
      };
      
      // Training time calculation
      const baseTime = stats.time * amount;
      const barracksBonus = 1 - (barracks_level * 0.05);
      const techBonus = 1 - (tech_bonus * 0.1);
      const totalTime = Math.ceil(baseTime * barracksBonus * techBonus);
      
      // Format time
      const hours = Math.floor(totalTime / 3600);
      const minutes = Math.floor((totalTime % 3600) / 60);
      const seconds = totalTime % 60;
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            troop: stats.name,
            amount,
            costs: baseCosts,
            training_time: {
              seconds: totalTime,
              formatted: `${hours}h ${minutes}m ${seconds}s`
            },
            bonuses_applied: {
              barracks_level: barracks_level,
              tech_bonus: tech_bonus
            },
            stats: {
              total_attack: stats.attack * amount,
              total_defense: stats.defense * amount,
              total_load: stats.load * amount,
              food_consumption: stats.food * amount + '/hr'
            }
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
 * Tool: calc_march - Calculate march time and capacity
 */
server.tool(
  'calc_march',
  'Calculate march time between coordinates',
  {
    from_x: z.number().describe('Starting X coordinate'),
    from_y: z.number().describe('Starting Y coordinate'),
    to_x: z.number().describe('Destination X coordinate'),
    to_y: z.number().describe('Destination Y coordinate'),
    slowest_troop: z.string().default('a').describe('Slowest troop type in army'),
    logistics_level: z.number().default(0).describe('Logistics tech level'),
    compass_level: z.number().default(0).describe('Compass tech level'),
    hero_attack: z.number().default(0).describe('Hero attack attribute')
  },
  async ({ from_x, from_y, to_x, to_y, slowest_troop, logistics_level, compass_level, hero_attack }) => {
    try {
      const stats = TROOP_STATS[slowest_troop];
      if (!stats) {
        return {
          content: [{
            type: 'text',
            text: JSON.stringify({ error: `Unknown troop type: ${slowest_troop}` })
          }],
          isError: true
        };
      }
      
      // Calculate distance
      const dx = to_x - from_x;
      const dy = to_y - from_y;
      const distance = Math.sqrt(dx * dx + dy * dy);
      
      // Base speed with bonuses
      const baseSpeed = stats.speed;
      const logisticsBonus = 1 + (logistics_level * 0.1);
      const compassBonus = 1 + (compass_level * 0.05);
      const heroBonus = 1 + (hero_attack * 0.005);
      const effectiveSpeed = baseSpeed * logisticsBonus * compassBonus * heroBonus;
      
      // Time calculation (distance / speed in game units)
      const marchTime = Math.ceil((distance * 60) / effectiveSpeed); // seconds
      
      const hours = Math.floor(marchTime / 3600);
      const minutes = Math.floor((marchTime % 3600) / 60);
      const seconds = marchTime % 60;
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            route: {
              from: { x: from_x, y: from_y },
              to: { x: to_x, y: to_y },
              distance: distance.toFixed(2)
            },
            slowest_troop: stats.name,
            speed: {
              base: baseSpeed,
              effective: effectiveSpeed.toFixed(2)
            },
            march_time: {
              seconds: marchTime,
              formatted: `${hours}h ${minutes}m ${seconds}s`
            },
            bonuses: {
              logistics: `+${logistics_level * 10}%`,
              compass: `+${compass_level * 5}%`,
              hero: `+${(hero_attack * 0.5).toFixed(1)}%`
            }
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
 * Tool: calc_combat - Estimate combat outcome
 */
server.tool(
  'calc_combat',
  'Estimate combat outcome between two armies',
  {
    attacker_troops: z.string().describe('Attacker troops (e.g., "a:10000,c:5000")'),
    defender_troops: z.string().describe('Defender troops (e.g., "sw:8000,p:5000")'),
    attacker_hero_attack: z.number().default(100).describe('Attacker hero attack stat'),
    defender_hero_attack: z.number().default(100).describe('Defender hero attack stat'),
    wall_level: z.number().default(0).describe('Defender wall level')
  },
  async ({ attacker_troops, defender_troops, attacker_hero_attack, defender_hero_attack, wall_level }) => {
    try {
      // Parse troop strings
      const parseTroops = (str) => {
        const result = {};
        str.split(',').forEach(part => {
          const [code, count] = part.split(':');
          if (code && count) {
            result[code.trim()] = parseInt(count, 10);
          }
        });
        return result;
      };
      
      const attackerArmy = parseTroops(attacker_troops);
      const defenderArmy = parseTroops(defender_troops);
      
      // Calculate total power
      const calcPower = (army, heroAttack, isDefender = false) => {
        let attack = 0;
        let defense = 0;
        let total = 0;
        
        for (const [code, count] of Object.entries(army)) {
          const stats = TROOP_STATS[code];
          if (stats) {
            attack += stats.attack * count;
            defense += stats.defense * count;
            total += count;
          }
        }
        
        const heroBonus = 1 + (heroAttack / 100);
        const wallBonus = isDefender ? (1 + wall_level * 0.05) : 1;
        
        return {
          attack: Math.floor(attack * heroBonus),
          defense: Math.floor(defense * heroBonus * wallBonus),
          total_troops: total
        };
      };
      
      const attackerPower = calcPower(attackerArmy, attacker_hero_attack);
      const defenderPower = calcPower(defenderArmy, defender_hero_attack, true);
      
      // Simple combat simulation
      const attackRatio = attackerPower.attack / (defenderPower.defense + 1);
      const defenseRatio = defenderPower.attack / (attackerPower.defense + 1);
      
      const attackerLossRate = Math.min(0.9, defenseRatio / (attackRatio + defenseRatio));
      const defenderLossRate = Math.min(0.9, attackRatio / (attackRatio + defenseRatio));
      
      const attackerLosses = Math.floor(attackerPower.total_troops * attackerLossRate);
      const defenderLosses = Math.floor(defenderPower.total_troops * defenderLossRate);
      
      const winner = attackRatio > defenseRatio ? 'Attacker' : 'Defender';
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            attacker: {
              troops: attackerArmy,
              power: attackerPower,
              estimated_losses: attackerLosses,
              remaining: attackerPower.total_troops - attackerLosses
            },
            defender: {
              troops: defenderArmy,
              power: defenderPower,
              wall_level,
              estimated_losses: defenderLosses,
              remaining: defenderPower.total_troops - defenderLosses
            },
            prediction: {
              winner,
              confidence: Math.abs(attackRatio - defenseRatio) > 0.5 ? 'High' : 'Medium',
              note: 'This is a simplified estimate. Actual results depend on many factors.'
            }
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
 * Tool: calc_resources - Calculate resource production
 */
server.tool(
  'calc_resources',
  'Calculate hourly resource production',
  {
    farms: z.number().default(0).describe('Number of farms'),
    farm_level: z.number().default(10).describe('Average farm level'),
    sawmills: z.number().default(0).describe('Number of sawmills'),
    sawmill_level: z.number().default(10).describe('Average sawmill level'),
    quarries: z.number().default(0).describe('Number of quarries'),
    quarry_level: z.number().default(10).describe('Average quarry level'),
    ironmines: z.number().default(0).describe('Number of iron mines'),
    ironmine_level: z.number().default(10).describe('Average iron mine level'),
    tech_bonus: z.number().default(0).describe('Production tech level'),
    mayor_politics: z.number().default(0).describe('Mayor politics attribute')
  },
  async ({ farms, farm_level, sawmills, sawmill_level, quarries, quarry_level, ironmines, ironmine_level, tech_bonus, mayor_politics }) => {
    try {
      // Base production per level
      const baseProduction = {
        food: 100,
        lumber: 100,
        stone: 100,
        iron: 100
      };
      
      // Calculate bonuses
      const techMultiplier = 1 + (tech_bonus * 0.05);
      const mayorMultiplier = 1 + (mayor_politics / 100);
      
      // Calculate production
      const production = {
        food: Math.floor(farms * farm_level * baseProduction.food * techMultiplier * mayorMultiplier),
        lumber: Math.floor(sawmills * sawmill_level * baseProduction.lumber * techMultiplier * mayorMultiplier),
        stone: Math.floor(quarries * quarry_level * baseProduction.stone * techMultiplier * mayorMultiplier),
        iron: Math.floor(ironmines * ironmine_level * baseProduction.iron * techMultiplier * mayorMultiplier)
      };
      
      const total = production.food + production.lumber + production.stone + production.iron;
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            hourly_production: production,
            daily_production: {
              food: production.food * 24,
              lumber: production.lumber * 24,
              stone: production.stone * 24,
              iron: production.iron * 24
            },
            total_hourly: total,
            bonuses: {
              tech: `+${tech_bonus * 5}%`,
              mayor: `+${mayor_politics}%`
            },
            buildings: {
              farms: { count: farms, level: farm_level },
              sawmills: { count: sawmills, level: sawmill_level },
              quarries: { count: quarries, level: quarry_level },
              ironmines: { count: ironmines, level: ironmine_level }
            }
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
// MCP Tools - Automation Helpers
// ============================================================================

/**
 * Tool: generate_build_order - Generate optimal build order
 */
server.tool(
  'generate_build_order',
  'Generate an optimal building order for a new city',
  {
    focus: z.enum(['balanced', 'military', 'economy', 'research']).default('balanced').describe('Build focus'),
    target_level: z.number().default(5).describe('Target town hall level')
  },
  async ({ focus, target_level }) => {
    try {
      const buildOrders = {
        balanced: [
          { building: 'Town Hall', level: 1 },
          { building: 'Cottage', level: 1 },
          { building: 'Farm', level: 1 },
          { building: 'Sawmill', level: 1 },
          { building: 'Quarry', level: 1 },
          { building: 'Ironmine', level: 1 },
          { building: 'Warehouse', level: 1 },
          { building: 'Barracks', level: 1 },
          { building: 'Town Hall', level: 2 },
          { building: 'Academy', level: 1 },
          { building: 'Rally Spot', level: 1 }
        ],
        military: [
          { building: 'Town Hall', level: 1 },
          { building: 'Barracks', level: 1 },
          { building: 'Cottage', level: 1 },
          { building: 'Farm', level: 2 },
          { building: 'Barracks', level: 2 },
          { building: 'Rally Spot', level: 1 },
          { building: 'Town Hall', level: 2 },
          { building: 'Stable', level: 1 },
          { building: 'Forge', level: 1 }
        ],
        economy: [
          { building: 'Town Hall', level: 1 },
          { building: 'Farm', level: 2 },
          { building: 'Sawmill', level: 2 },
          { building: 'Quarry', level: 2 },
          { building: 'Ironmine', level: 2 },
          { building: 'Cottage', level: 2 },
          { building: 'Warehouse', level: 2 },
          { building: 'Marketplace', level: 1 },
          { building: 'Town Hall', level: 2 }
        ],
        research: [
          { building: 'Town Hall', level: 1 },
          { building: 'Cottage', level: 1 },
          { building: 'Town Hall', level: 2 },
          { building: 'Academy', level: 1 },
          { building: 'Academy', level: 2 },
          { building: 'Warehouse', level: 1 },
          { building: 'Farm', level: 2 },
          { building: 'Academy', level: 3 }
        ]
      };
      
      const order = buildOrders[focus] || buildOrders.balanced;
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            focus,
            target_level,
            build_order: order.map((step, idx) => ({
              step: idx + 1,
              ...step
            })),
            tips: {
              balanced: 'Good for new players, provides steady growth',
              military: 'Focus on troop production early',
              economy: 'Maximize resource production first',
              research: 'Rush academy for tech advantages'
            }[focus]
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
 * Tool: generate_attack_plan - Generate attack plan
 */
server.tool(
  'generate_attack_plan',
  'Generate an attack plan for a target',
  {
    target_type: z.enum(['npc', 'valley', 'player', 'barbarian']).describe('Target type'),
    target_level: z.number().describe('Target level'),
    available_troops: z.string().describe('Available troops (e.g., "a:50000,c:10000")'),
    hero_attack: z.number().default(100).describe('Hero attack stat')
  },
  async ({ target_type, target_level, available_troops, hero_attack }) => {
    try {
      // Parse available troops
      const troops = {};
      available_troops.split(',').forEach(part => {
        const [code, count] = part.split(':');
        if (code && count) {
          troops[code.trim()] = parseInt(count, 10);
        }
      });
      
      // Recommended troops based on target
      const recommendations = {
        npc: {
          primary: 'a',
          ratio: { a: 0.8, c: 0.2 },
          waves: target_level <= 5 ? 1 : Math.ceil(target_level / 3)
        },
        valley: {
          primary: 'a',
          ratio: { a: 0.9, s: 0.1 },
          waves: 1
        },
        player: {
          primary: 'a',
          ratio: { a: 0.6, c: 0.3, cata: 0.1 },
          waves: 2
        },
        barbarian: {
          primary: 'a',
          ratio: { a: 0.7, c: 0.3 },
          waves: 1
        }
      };
      
      const rec = recommendations[target_type];
      
      // Calculate recommended army composition
      const totalTroops = Object.values(troops).reduce((a, b) => a + b, 0);
      const recommendedSize = Math.min(totalTroops, target_level * 5000);
      
      const composition = {};
      for (const [type, ratio] of Object.entries(rec.ratio)) {
        const needed = Math.floor(recommendedSize * ratio);
        composition[type] = Math.min(needed, troops[type] || 0);
      }
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            target: {
              type: target_type,
              level: target_level
            },
            available_troops: troops,
            recommended_composition: composition,
            waves: rec.waves,
            strategy: {
              primary_troop: rec.primary,
              hero_requirement: `Attack stat ${target_level * 10}+ recommended`,
              timing: target_type === 'player' ? 'Scout first, attack during off-hours' : 'Attack anytime'
            },
            warnings: target_type === 'player' ? [
              'Scout target first',
              'Check for reinforcements',
              'Consider alliance retaliation'
            ] : []
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
 * Tool: coordinate_convert - Convert between coordinate formats
 */
server.tool(
  'coordinate_convert',
  'Convert between coordinate formats and calculate field IDs',
  {
    x: z.number().optional().describe('X coordinate'),
    y: z.number().optional().describe('Y coordinate'),
    field_id: z.number().optional().describe('Field ID to convert')
  },
  async ({ x, y, field_id }) => {
    try {
      const MAP_WIDTH = 800;
      
      if (field_id !== undefined) {
        // Convert field ID to coordinates
        const calcX = field_id % MAP_WIDTH;
        const calcY = Math.floor(field_id / MAP_WIDTH);
        
        return {
          content: [{
            type: 'text',
            text: JSON.stringify({
              field_id,
              coordinates: { x: calcX, y: calcY },
              formatted: `(${calcX}, ${calcY})`
            }, null, 2)
          }]
        };
      } else if (x !== undefined && y !== undefined) {
        // Convert coordinates to field ID
        const calcFieldId = y * MAP_WIDTH + x;
        
        return {
          content: [{
            type: 'text',
            text: JSON.stringify({
              coordinates: { x, y },
              field_id: calcFieldId,
              formatted: `(${x}, ${y}) = Field ${calcFieldId}`
            }, null, 2)
          }]
        };
      } else {
        return {
          content: [{
            type: 'text',
            text: JSON.stringify({ error: 'Provide either x,y coordinates or field_id' })
          }],
          isError: true
        };
      }
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
  'evony://tools/troop-stats',
  'Complete troop statistics',
  async () => {
    return {
      contents: [{
        uri: 'evony://tools/troop-stats',
        mimeType: 'application/json',
        text: JSON.stringify(TROOP_STATS, null, 2)
      }]
    };
  }
);

server.resource(
  'evony://tools/building-data',
  'Building requirements and data',
  async () => {
    return {
      contents: [{
        uri: 'evony://tools/building-data',
        mimeType: 'application/json',
        text: JSON.stringify(BUILDING_REQUIREMENTS, null, 2)
      }]
    };
  }
);

server.resource(
  'evony://tools/tech-data',
  'Technology research data',
  async () => {
    return {
      contents: [{
        uri: 'evony://tools/tech-data',
        mimeType: 'application/json',
        text: JSON.stringify(TECH_DATA, null, 2)
      }]
    };
  }
);

// ============================================================================
// Server Startup
// ============================================================================

async function main() {
  console.error('[evony-tools] Starting Evony Tools MCP Server...');
  
  const transport = new StdioServerTransport();
  await server.connect(transport);
  
  console.error('[evony-tools] Server running on stdio transport');
}

main().catch(error => {
  console.error('[evony-tools] Fatal error:', error);
  process.exit(1);
});
