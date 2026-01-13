#!/usr/bin/env node
/**
 * Svony CLI v5.0 - Complete Command-Line Interface
 * 
 * Full access to all Svony Browser features for:
 * - Claude Desktop
 * - Windsurf IDE
 * - LM Studio
 * 
 * 168 commands across 20 categories
 */

import { Command } from 'commander';
import chalk from 'chalk';
import ora from 'ora';
import { WebSocket } from 'ws';
import fs from 'fs/promises';
import path from 'path';
import { fileURLToPath } from 'url';
import { createInterface } from 'readline';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Configuration
const CONFIG = {
  wsHost: process.env.SVONY_WS_HOST || 'localhost',
  wsPort: parseInt(process.env.SVONY_WS_PORT || '9876'),
  configDir: process.env.SVONY_CONFIG_DIR || path.join(process.env.HOME || '', '.svony'),
  version: '5.0.0'
};

// Initialize CLI
const program = new Command();

program
  .name('svony')
  .description('Svony CLI v5.0 - Complete browser control for Evony protocol analysis')
  .version(CONFIG.version);

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

async function sendCommand(command, params = {}) {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(`ws://${CONFIG.wsHost}:${CONFIG.wsPort}`);
    
    ws.on('open', () => {
      ws.send(JSON.stringify({ command, params }));
    });
    
    ws.on('message', (data) => {
      try {
        const response = JSON.parse(data.toString());
        ws.close();
        
        if (response.error) {
          reject(new Error(response.error));
        } else {
          resolve(response.result || response);
        }
      } catch (error) {
        ws.close();
        reject(error);
      }
    });
    
    ws.on('error', (error) => {
      reject(new Error(`Connection failed: ${error.message}\nStart the browser or server first.`));
    });
    
    setTimeout(() => {
      ws.close();
      reject(new Error('Request timeout'));
    }, 30000);
  });
}

function formatJson(obj) {
  return JSON.stringify(obj, null, 2);
}

function printSuccess(message) {
  console.log(chalk.green.bold(`\n✓ ${message}\n`));
}

function printError(message) {
  console.log(chalk.red.bold(`\n✗ ${message}\n`));
}

function printTable(data, columns) {
  if (!data || data.length === 0) {
    console.log(chalk.gray('No data'));
    return;
  }
  
  // Simple table output
  console.log(chalk.cyan(columns.join('\t')));
  console.log(chalk.gray('-'.repeat(60)));
  data.forEach(row => {
    const values = columns.map(col => row[col] || '-');
    console.log(values.join('\t'));
  });
}

// ============================================================================
// 1. BROWSER COMMANDS
// ============================================================================

const browserCmd = program
  .command('browser')
  .description('Browser control commands');

browserCmd
  .command('navigate <url>')
  .description('Navigate to URL')
  .option('-p, --panel <panel>', 'Panel (left, right, both)', 'left')
  .action(async (url, options) => {
    const spinner = ora('Navigating...').start();
    try {
      const result = await sendCommand('browser_navigate', { url, panel: options.panel });
      spinner.succeed(`Navigated to ${url}`);
    } catch (err) {
      spinner.fail(err.message);
    }
  });

browserCmd
  .command('back')
  .description('Go back')
  .option('-p, --panel <panel>', 'Panel', 'left')
  .action(async (options) => {
    try {
      await sendCommand('browser_back', { panel: options.panel });
      printSuccess('Navigated back');
    } catch (err) {
      printError(err.message);
    }
  });

browserCmd
  .command('forward')
  .description('Go forward')
  .option('-p, --panel <panel>', 'Panel', 'left')
  .action(async (options) => {
    try {
      await sendCommand('browser_forward', { panel: options.panel });
      printSuccess('Navigated forward');
    } catch (err) {
      printError(err.message);
    }
  });

browserCmd
  .command('refresh')
  .description('Refresh page')
  .option('-p, --panel <panel>', 'Panel', 'left')
  .option('--hard', 'Hard refresh')
  .action(async (options) => {
    try {
      await sendCommand('browser_refresh', { panel: options.panel, hard: options.hard });
      printSuccess('Page refreshed');
    } catch (err) {
      printError(err.message);
    }
  });

browserCmd
  .command('panel <action>')
  .description('Control panels (show_left, show_right, show_both, hide_left, hide_right, swap)')
  .action(async (action) => {
    try {
      await sendCommand('browser_panel_control', { action });
      printSuccess(`Panel action: ${action}`);
    } catch (err) {
      printError(err.message);
    }
  });

browserCmd
  .command('screenshot [filename]')
  .description('Take screenshot')
  .option('-p, --panel <panel>', 'Panel (left, right, both, full)', 'full')
  .option('-f, --format <format>', 'Format (png, jpg)', 'png')
  .action(async (filename, options) => {
    const spinner = ora('Taking screenshot...').start();
    try {
      const result = await sendCommand('browser_screenshot', {
        panel: options.panel,
        format: options.format,
        filename
      });
      spinner.succeed(`Screenshot saved: ${result.filename}`);
    } catch (err) {
      spinner.fail(err.message);
    }
  });

browserCmd
  .command('url')
  .description('Get current URL')
  .option('-p, --panel <panel>', 'Panel', 'left')
  .action(async (options) => {
    try {
      const result = await sendCommand('browser_get_url', { panel: options.panel });
      console.log(chalk.cyan('URL:'), result.url);
    } catch (err) {
      printError(err.message);
    }
  });

browserCmd
  .command('title')
  .description('Get page title')
  .option('-p, --panel <panel>', 'Panel', 'left')
  .action(async (options) => {
    try {
      const result = await sendCommand('browser_get_title', { panel: options.panel });
      console.log(chalk.cyan('Title:'), result.title);
    } catch (err) {
      printError(err.message);
    }
  });

browserCmd
  .command('exec <script>')
  .description('Execute JavaScript')
  .option('-p, --panel <panel>', 'Panel', 'left')
  .action(async (script, options) => {
    try {
      const result = await sendCommand('browser_execute_js', { script, panel: options.panel });
      console.log(chalk.cyan('Result:'), formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

browserCmd
  .command('status')
  .description('Get browser status')
  .action(async () => {
    const spinner = ora('Getting status...').start();
    try {
      const result = await sendCommand('browser_get_status', {});
      spinner.stop();
      console.log(chalk.green.bold('\nBrowser Status\n'));
      console.log(formatJson(result));
    } catch (err) {
      spinner.fail(err.message);
    }
  });

// ============================================================================
// 2. SESSION COMMANDS
// ============================================================================

const sessionCmd = program
  .command('session')
  .description('Session management commands');

sessionCmd
  .command('list')
  .description('List all sessions')
  .action(async () => {
    try {
      const result = await sendCommand('session_list', {});
      printSuccess(`Found ${result.sessions?.length || 0} sessions`);
      result.sessions?.forEach(s => {
        console.log(`  ${chalk.cyan(s.id)} - ${s.name} (${s.server})`);
      });
    } catch (err) {
      printError(err.message);
    }
  });

sessionCmd
  .command('create <name>')
  .description('Create new session')
  .option('-s, --server <server>', 'Game server')
  .action(async (name, options) => {
    try {
      const result = await sendCommand('session_create', { name, server: options.server });
      printSuccess(`Created session: ${result.sessionId}`);
    } catch (err) {
      printError(err.message);
    }
  });

sessionCmd
  .command('switch <sessionId>')
  .description('Switch to session')
  .action(async (sessionId) => {
    try {
      await sendCommand('session_switch', { sessionId });
      printSuccess(`Switched to session: ${sessionId}`);
    } catch (err) {
      printError(err.message);
    }
  });

sessionCmd
  .command('delete <sessionId>')
  .description('Delete session')
  .action(async (sessionId) => {
    try {
      await sendCommand('session_delete', { sessionId });
      printSuccess(`Deleted session: ${sessionId}`);
    } catch (err) {
      printError(err.message);
    }
  });

sessionCmd
  .command('info [sessionId]')
  .description('Get session info')
  .action(async (sessionId) => {
    try {
      const result = await sendCommand('session_get_info', { sessionId });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

sessionCmd
  .command('export [sessionId]')
  .description('Export session')
  .option('-f, --format <format>', 'Format (json, encrypted)', 'json')
  .action(async (sessionId, options) => {
    try {
      const result = await sendCommand('session_export', { sessionId, format: options.format });
      console.log(result.data);
    } catch (err) {
      printError(err.message);
    }
  });

sessionCmd
  .command('import <data>')
  .description('Import session')
  .option('-f, --format <format>', 'Format (json, encrypted)', 'json')
  .action(async (data, options) => {
    try {
      const result = await sendCommand('session_import', { data, format: options.format });
      printSuccess(`Imported session: ${result.sessionId}`);
    } catch (err) {
      printError(err.message);
    }
  });

sessionCmd
  .command('sync [sessionId]')
  .description('Sync session with game')
  .action(async (sessionId) => {
    const spinner = ora('Syncing...').start();
    try {
      await sendCommand('session_sync', { sessionId });
      spinner.succeed('Session synced');
    } catch (err) {
      spinner.fail(err.message);
    }
  });

// ============================================================================
// 3. ACCOUNT COMMANDS
// ============================================================================

const accountCmd = program
  .command('account')
  .description('Multi-account management');

accountCmd
  .command('list')
  .description('List all accounts')
  .action(async () => {
    try {
      const result = await sendCommand('account_list', {});
      printSuccess(`Found ${result.accounts?.length || 0} accounts`);
      result.accounts?.forEach(a => {
        const status = a.active ? chalk.green('●') : chalk.gray('○');
        console.log(`  ${status} ${chalk.cyan(a.id)} - ${a.name} (${a.server})`);
      });
    } catch (err) {
      printError(err.message);
    }
  });

accountCmd
  .command('add <name>')
  .description('Add account')
  .option('-s, --server <server>', 'Game server')
  .action(async (name, options) => {
    try {
      const result = await sendCommand('account_add', { name, server: options.server });
      printSuccess(`Added account: ${result.accountId}`);
    } catch (err) {
      printError(err.message);
    }
  });

accountCmd
  .command('remove <accountId>')
  .description('Remove account')
  .action(async (accountId) => {
    try {
      await sendCommand('account_remove', { accountId });
      printSuccess(`Removed account: ${accountId}`);
    } catch (err) {
      printError(err.message);
    }
  });

accountCmd
  .command('switch <accountId>')
  .description('Switch active account')
  .action(async (accountId) => {
    try {
      await sendCommand('account_switch', { accountId });
      printSuccess(`Switched to account: ${accountId}`);
    } catch (err) {
      printError(err.message);
    }
  });

accountCmd
  .command('sync [accountId]')
  .description('Sync account data')
  .action(async (accountId) => {
    const spinner = ora('Syncing...').start();
    try {
      await sendCommand('account_sync', { accountId });
      spinner.succeed('Account synced');
    } catch (err) {
      spinner.fail(err.message);
    }
  });

accountCmd
  .command('coordinate <action>')
  .description('Coordinate action across accounts')
  .option('-a, --accounts <ids>', 'Account IDs (comma-separated)')
  .option('-t, --timing <timing>', 'Timing (simultaneous, staggered, sequential)', 'staggered')
  .action(async (action, options) => {
    try {
      const accountIds = options.accounts?.split(',') || [];
      const result = await sendCommand('account_coordinate', {
        accountIds,
        action,
        timing: options.timing
      });
      printSuccess(`Coordinated action: ${action}`);
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

accountCmd
  .command('status')
  .description('Get all account statuses')
  .action(async () => {
    try {
      const result = await sendCommand('account_get_status', {});
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 4. GAME STATE COMMANDS
// ============================================================================

const gameCmd = program
  .command('game')
  .description('Game state commands');

gameCmd
  .command('status')
  .description('Get full game status')
  .action(async () => {
    const spinner = ora('Getting game status...').start();
    try {
      const result = await sendCommand('game_get_status', {});
      spinner.stop();
      console.log(formatJson(result));
    } catch (err) {
      spinner.fail(err.message);
    }
  });

gameCmd
  .command('resources')
  .description('Get resource levels')
  .option('-c, --city <cityId>', 'City ID')
  .action(async (options) => {
    try {
      const result = await sendCommand('game_get_resources', { cityId: options.city });
      console.log(chalk.green.bold('\nResources\n'));
      console.log(chalk.yellow('Food:'), result.food?.toLocaleString(), chalk.gray(`(${result.foodRate > 0 ? '+' : ''}${result.foodRate}/hr)`));
      console.log(chalk.yellow('Wood:'), result.wood?.toLocaleString(), chalk.gray(`(${result.woodRate > 0 ? '+' : ''}${result.woodRate}/hr)`));
      console.log(chalk.yellow('Stone:'), result.stone?.toLocaleString(), chalk.gray(`(${result.stoneRate > 0 ? '+' : ''}${result.stoneRate}/hr)`));
      console.log(chalk.yellow('Iron:'), result.iron?.toLocaleString(), chalk.gray(`(${result.ironRate > 0 ? '+' : ''}${result.ironRate}/hr)`));
      console.log(chalk.yellow('Gold:'), result.gold?.toLocaleString());
    } catch (err) {
      printError(err.message);
    }
  });

gameCmd
  .command('troops')
  .description('Get troop counts')
  .option('-c, --city <cityId>', 'City ID')
  .option('--include-marching', 'Include marching troops')
  .action(async (options) => {
    try {
      const result = await sendCommand('game_get_troops', {
        cityId: options.city,
        includeMarching: options.includeMarching
      });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

gameCmd
  .command('heroes')
  .description('Get hero information')
  .option('-i, --id <heroId>', 'Specific hero ID')
  .action(async (options) => {
    try {
      const result = await sendCommand('game_get_heroes', { heroId: options.id });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

gameCmd
  .command('buildings')
  .description('Get building status')
  .option('-c, --city <cityId>', 'City ID')
  .option('-t, --type <type>', 'Building type filter')
  .action(async (options) => {
    try {
      const result = await sendCommand('game_get_buildings', {
        cityId: options.city,
        buildingType: options.type
      });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

gameCmd
  .command('research')
  .description('Get research status')
  .option('-c, --category <category>', 'Category filter')
  .action(async (options) => {
    try {
      const result = await sendCommand('game_get_research', { category: options.category });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

gameCmd
  .command('marches')
  .description('Get active marches')
  .option('-t, --type <type>', 'Type (all, attack, reinforce, gather, scout)', 'all')
  .action(async (options) => {
    try {
      const result = await sendCommand('game_get_marches', { type: options.type });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

gameCmd
  .command('alliance')
  .description('Get alliance info')
  .action(async () => {
    try {
      const result = await sendCommand('game_get_alliance', {});
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

gameCmd
  .command('events')
  .description('Get active events')
  .option('-t, --type <type>', 'Event type filter')
  .action(async (options) => {
    try {
      const result = await sendCommand('game_get_events', { type: options.type });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

gameCmd
  .command('mail')
  .description('Get mail/messages')
  .option('-f, --folder <folder>', 'Folder (inbox, sent, system, reports)', 'inbox')
  .option('-l, --limit <n>', 'Limit', '50')
  .action(async (options) => {
    try {
      const result = await sendCommand('game_get_mail', {
        folder: options.folder,
        limit: parseInt(options.limit)
      });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

gameCmd
  .command('reports')
  .description('Get battle reports')
  .option('-t, --type <type>', 'Type (all, attack, defense, scout)', 'all')
  .option('-l, --limit <n>', 'Limit', '50')
  .action(async (options) => {
    try {
      const result = await sendCommand('game_get_reports', {
        type: options.type,
        limit: parseInt(options.limit)
      });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 5. AUTOPILOT COMMANDS
// ============================================================================

const autopilotCmd = program
  .command('autopilot')
  .alias('auto')
  .description('Automation commands');

autopilotCmd
  .command('start')
  .description('Start autopilot')
  .option('-p, --profile <profile>', 'Automation profile')
  .action(async (options) => {
    try {
      await sendCommand('autopilot_start', { profile: options.profile });
      printSuccess('Autopilot started');
    } catch (err) {
      printError(err.message);
    }
  });

autopilotCmd
  .command('stop')
  .description('Stop autopilot')
  .action(async () => {
    try {
      await sendCommand('autopilot_stop', {});
      printSuccess('Autopilot stopped');
    } catch (err) {
      printError(err.message);
    }
  });

autopilotCmd
  .command('pause')
  .description('Pause autopilot')
  .action(async () => {
    try {
      await sendCommand('autopilot_pause', {});
      printSuccess('Autopilot paused');
    } catch (err) {
      printError(err.message);
    }
  });

autopilotCmd
  .command('resume')
  .description('Resume autopilot')
  .action(async () => {
    try {
      await sendCommand('autopilot_resume', {});
      printSuccess('Autopilot resumed');
    } catch (err) {
      printError(err.message);
    }
  });

autopilotCmd
  .command('status')
  .description('Get autopilot status')
  .action(async () => {
    try {
      const result = await sendCommand('autopilot_get_status', {});
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

autopilotCmd
  .command('config')
  .description('Get/set autopilot config')
  .option('-s, --set <json>', 'Set configuration (JSON)')
  .action(async (options) => {
    try {
      if (options.set) {
        const config = JSON.parse(options.set);
        await sendCommand('autopilot_set_config', { config });
        printSuccess('Configuration updated');
      } else {
        const result = await sendCommand('autopilot_get_config', {});
        console.log(formatJson(result));
      }
    } catch (err) {
      printError(err.message);
    }
  });

autopilotCmd
  .command('add-task <task>')
  .description('Add task to queue')
  .option('-p, --priority <n>', 'Priority (1-10)', '5')
  .option('--params <json>', 'Task parameters (JSON)')
  .option('--schedule <cron>', 'Cron schedule')
  .action(async (task, options) => {
    try {
      const result = await sendCommand('autopilot_add_task', {
        task,
        priority: parseInt(options.priority),
        params: options.params ? JSON.parse(options.params) : {},
        schedule: options.schedule
      });
      printSuccess(`Added task: ${result.taskId}`);
    } catch (err) {
      printError(err.message);
    }
  });

autopilotCmd
  .command('remove-task <taskId>')
  .description('Remove task from queue')
  .action(async (taskId) => {
    try {
      await sendCommand('autopilot_remove_task', { taskId });
      printSuccess(`Removed task: ${taskId}`);
    } catch (err) {
      printError(err.message);
    }
  });

autopilotCmd
  .command('logs')
  .description('Get automation logs')
  .option('-l, --limit <n>', 'Limit', '100')
  .option('--level <level>', 'Level (all, info, warning, error)', 'all')
  .action(async (options) => {
    try {
      const result = await sendCommand('autopilot_get_logs', {
        limit: parseInt(options.limit),
        level: options.level
      });
      result.logs?.forEach(log => {
        const color = log.level === 'error' ? chalk.red : log.level === 'warning' ? chalk.yellow : chalk.white;
        console.log(color(`[${log.timestamp}] ${log.level}: ${log.message}`));
      });
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 6. COMBAT COMMANDS
// ============================================================================

const combatCmd = program
  .command('combat')
  .description('Combat simulation commands');

combatCmd
  .command('simulate')
  .description('Simulate battle')
  .option('-a, --attacker <json>', 'Attacker composition (JSON)')
  .option('-d, --defender <json>', 'Defender composition (JSON)')
  .option('-i, --iterations <n>', 'Monte Carlo iterations', '1000')
  .action(async (options) => {
    const spinner = ora('Simulating battle...').start();
    try {
      const result = await sendCommand('combat_simulate', {
        attacker: JSON.parse(options.attacker || '{}'),
        defender: JSON.parse(options.defender || '{}'),
        iterations: parseInt(options.iterations)
      });
      spinner.stop();
      console.log(chalk.green.bold('\nBattle Simulation Results\n'));
      console.log(chalk.cyan('Win Rate:'), `${result.winRate}%`);
      console.log(chalk.cyan('Expected Losses:'), result.expectedLosses);
      console.log(chalk.cyan('Confidence:'), result.confidence);
    } catch (err) {
      spinner.fail(err.message);
    }
  });

combatCmd
  .command('optimize')
  .description('Optimize troop composition')
  .option('-t, --target <json>', 'Target info (JSON)')
  .option('-a, --available <json>', 'Available troops (JSON)')
  .option('-o, --objective <obj>', 'Objective (minimize_losses, maximize_damage, balanced)', 'balanced')
  .action(async (options) => {
    const spinner = ora('Optimizing...').start();
    try {
      const result = await sendCommand('combat_optimize', {
        target: JSON.parse(options.target || '{}'),
        availableTroops: JSON.parse(options.available || '{}'),
        objective: options.objective
      });
      spinner.stop();
      console.log(formatJson(result));
    } catch (err) {
      spinner.fail(err.message);
    }
  });

combatCmd
  .command('rally <x> <y>')
  .description('Plan rally attack')
  .option('-s, --size <n>', 'Rally size', '5')
  .option('-t, --timing <time>', 'Rally timing')
  .action(async (x, y, options) => {
    try {
      const result = await sendCommand('combat_plan_rally', {
        targetX: parseInt(x),
        targetY: parseInt(y),
        rallySize: parseInt(options.size),
        timing: options.timing
      });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

combatCmd
  .command('defense')
  .description('Analyze defense')
  .option('-c, --city <cityId>', 'City ID')
  .action(async (options) => {
    try {
      const result = await sendCommand('combat_analyze_defense', { cityId: options.city });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

combatCmd
  .command('history')
  .description('Get combat history')
  .option('-l, --limit <n>', 'Limit', '50')
  .option('-t, --type <type>', 'Type (all, wins, losses)', 'all')
  .action(async (options) => {
    try {
      const result = await sendCommand('combat_get_history', {
        limit: parseInt(options.limit),
        type: options.type
      });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

combatCmd
  .command('power')
  .description('Calculate army power')
  .option('-t, --troops <json>', 'Troop composition (JSON)')
  .option('-h, --heroes <ids>', 'Hero IDs (comma-separated)')
  .action(async (options) => {
    try {
      const result = await sendCommand('combat_calculate_power', {
        troops: JSON.parse(options.troops || '{}'),
        heroes: options.heroes?.split(',').map(Number) || []
      });
      console.log(chalk.cyan('Total Power:'), result.totalPower?.toLocaleString());
    } catch (err) {
      printError(err.message);
    }
  });

combatCmd
  .command('buffs')
  .description('Get active combat buffs')
  .action(async () => {
    try {
      const result = await sendCommand('combat_get_buffs', {});
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 7. MAP COMMANDS
// ============================================================================

const mapCmd = program
  .command('map')
  .description('Map intelligence commands');

mapCmd
  .command('scan <x> <y>')
  .description('Scan map area')
  .option('-r, --radius <n>', 'Radius', '50')
  .action(async (x, y, options) => {
    const spinner = ora('Scanning...').start();
    try {
      const result = await sendCommand('map_scan_area', {
        centerX: parseInt(x),
        centerY: parseInt(y),
        radius: parseInt(options.radius)
      });
      spinner.stop();
      console.log(formatJson(result));
    } catch (err) {
      spinner.fail(err.message);
    }
  });

mapCmd
  .command('targets <x> <y>')
  .description('Find farm targets')
  .option('-r, --radius <n>', 'Radius', '100')
  .option('--min-power <n>', 'Min power', '0')
  .option('--max-power <n>', 'Max power', '1000000')
  .option('-l, --limit <n>', 'Limit', '50')
  .action(async (x, y, options) => {
    const spinner = ora('Finding targets...').start();
    try {
      const result = await sendCommand('map_find_targets', {
        centerX: parseInt(x),
        centerY: parseInt(y),
        radius: parseInt(options.radius),
        minPower: parseInt(options.minPower),
        maxPower: parseInt(options.maxPower),
        limit: parseInt(options.limit)
      });
      spinner.stop();
      console.log(formatJson(result));
    } catch (err) {
      spinner.fail(err.message);
    }
  });

mapCmd
  .command('monsters <x> <y>')
  .description('Find monsters')
  .option('-r, --radius <n>', 'Radius', '100')
  .option('--min-level <n>', 'Min level', '1')
  .option('--max-level <n>', 'Max level', '50')
  .option('-t, --type <type>', 'Monster type')
  .action(async (x, y, options) => {
    const spinner = ora('Finding monsters...').start();
    try {
      const result = await sendCommand('map_find_monsters', {
        centerX: parseInt(x),
        centerY: parseInt(y),
        radius: parseInt(options.radius),
        minLevel: parseInt(options.minLevel),
        maxLevel: parseInt(options.maxLevel),
        type: options.type
      });
      spinner.stop();
      console.log(formatJson(result));
    } catch (err) {
      spinner.fail(err.message);
    }
  });

mapCmd
  .command('resources <x> <y>')
  .description('Find resource tiles')
  .option('-r, --radius <n>', 'Radius', '100')
  .option('-t, --type <type>', 'Resource type (food, wood, stone, iron, gold, all)', 'all')
  .option('--min-level <n>', 'Min level', '1')
  .action(async (x, y, options) => {
    const spinner = ora('Finding resources...').start();
    try {
      const result = await sendCommand('map_find_resources', {
        centerX: parseInt(x),
        centerY: parseInt(y),
        radius: parseInt(options.radius),
        resourceType: options.type,
        minLevel: parseInt(options.minLevel)
      });
      spinner.stop();
      console.log(formatJson(result));
    } catch (err) {
      spinner.fail(err.message);
    }
  });

mapCmd
  .command('tile <x> <y>')
  .description('Get tile info')
  .action(async (x, y) => {
    try {
      const result = await sendCommand('map_get_tile_info', {
        x: parseInt(x),
        y: parseInt(y)
      });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

mapCmd
  .command('distance <fromX> <fromY> <toX> <toY>')
  .description('Calculate distance')
  .action(async (fromX, fromY, toX, toY) => {
    try {
      const result = await sendCommand('map_calculate_distance', {
        fromX: parseInt(fromX),
        fromY: parseInt(fromY),
        toX: parseInt(toX),
        toY: parseInt(toY)
      });
      console.log(chalk.cyan('Distance:'), result.distance, 'tiles');
    } catch (err) {
      printError(err.message);
    }
  });

mapCmd
  .command('export <x> <y>')
  .description('Export map data')
  .option('-r, --radius <n>', 'Radius', '100')
  .option('-f, --format <format>', 'Format (json, csv)', 'json')
  .action(async (x, y, options) => {
    const spinner = ora('Exporting...').start();
    try {
      const result = await sendCommand('map_export_data', {
        centerX: parseInt(x),
        centerY: parseInt(y),
        radius: parseInt(options.radius),
        format: options.format
      });
      spinner.succeed(`Exported to ${result.filename}`);
    } catch (err) {
      spinner.fail(err.message);
    }
  });

// ============================================================================
// 8. PACKET COMMANDS
// ============================================================================

const packetCmd = program
  .command('packet')
  .description('Packet operations');

packetCmd
  .command('capture')
  .description('Start/stop packet capture')
  .option('--start', 'Start capture')
  .option('--stop', 'Stop capture')
  .option('-f, --filter <pattern>', 'Action filter pattern')
  .option('-m, --max <n>', 'Max packets', '10000')
  .action(async (options) => {
    try {
      if (options.stop) {
        await sendCommand('packet_capture_stop', {});
        printSuccess('Capture stopped');
      } else {
        await sendCommand('packet_capture_start', {
          filter: options.filter,
          maxPackets: parseInt(options.max)
        });
        printSuccess('Capture started');
      }
    } catch (err) {
      printError(err.message);
    }
  });

packetCmd
  .command('list')
  .description('List captured packets')
  .option('-l, --limit <n>', 'Limit', '100')
  .option('-d, --direction <dir>', 'Direction (all, request, response)', 'all')
  .option('-f, --filter <pattern>', 'Action filter')
  .action(async (options) => {
    try {
      const result = await sendCommand('packet_list', {
        limit: parseInt(options.limit),
        direction: options.direction,
        filter: options.filter
      });
      printSuccess(`Found ${result.total} packets`);
      result.packets?.forEach(p => {
        const dir = p.direction === 'request' ? chalk.blue('→') : chalk.green('←');
        console.log(`  ${dir} ${p.id} ${chalk.cyan(p.action)} (${p.size} bytes)`);
      });
    } catch (err) {
      printError(err.message);
    }
  });

packetCmd
  .command('get <packetId>')
  .description('Get packet details')
  .action(async (packetId) => {
    try {
      const result = await sendCommand('packet_get', { packetId });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

packetCmd
  .command('decode <data>')
  .description('Decode packet data')
  .option('-f, --format <format>', 'Input format (hex, base64)', 'hex')
  .action(async (data, options) => {
    try {
      const result = await sendCommand('packet_decode', { data, format: options.format });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

packetCmd
  .command('encode <action>')
  .description('Encode packet')
  .option('-p, --params <json>', 'Parameters (JSON)')
  .option('-f, --format <format>', 'Output format (hex, base64)', 'hex')
  .action(async (action, options) => {
    try {
      const result = await sendCommand('packet_encode', {
        action,
        params: JSON.parse(options.params || '{}'),
        outputFormat: options.format
      });
      console.log(result.encoded);
    } catch (err) {
      printError(err.message);
    }
  });

packetCmd
  .command('inject <action>')
  .description('Inject packet')
  .option('-p, --params <json>', 'Parameters (JSON)')
  .option('-r, --raw <data>', 'Raw packet data')
  .action(async (action, options) => {
    try {
      await sendCommand('packet_inject', {
        action,
        params: options.params ? JSON.parse(options.params) : undefined,
        raw: options.raw
      });
      printSuccess('Packet injected');
    } catch (err) {
      printError(err.message);
    }
  });

packetCmd
  .command('replay <packetIds>')
  .description('Replay packets')
  .option('-s, --speed <n>', 'Speed multiplier', '1.0')
  .action(async (packetIds, options) => {
    const spinner = ora('Replaying...').start();
    try {
      await sendCommand('packet_replay', {
        packetIds: packetIds.split(','),
        speed: parseFloat(options.speed)
      });
      spinner.succeed('Replay complete');
    } catch (err) {
      spinner.fail(err.message);
    }
  });

packetCmd
  .command('compare <id1> <id2>')
  .description('Compare two packets')
  .action(async (id1, id2) => {
    try {
      const result = await sendCommand('packet_compare', { packetId1: id1, packetId2: id2 });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

packetCmd
  .command('search <query>')
  .description('Search packets')
  .option('-i, --in <scope>', 'Search in (action, params, raw, all)', 'all')
  .option('-l, --limit <n>', 'Limit', '50')
  .action(async (query, options) => {
    try {
      const result = await sendCommand('packet_search', {
        query,
        searchIn: options.in,
        limit: parseInt(options.limit)
      });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

packetCmd
  .command('export [filename]')
  .description('Export packets')
  .option('-f, --format <format>', 'Format (json, pcap, har)', 'json')
  .action(async (filename, options) => {
    const spinner = ora('Exporting...').start();
    try {
      const result = await sendCommand('packet_export', { format: options.format, filename });
      spinner.succeed(`Exported to ${result.filename}`);
    } catch (err) {
      spinner.fail(err.message);
    }
  });

packetCmd
  .command('clear')
  .description('Clear captured packets')
  .action(async () => {
    try {
      await sendCommand('packet_clear', {});
      printSuccess('Packets cleared');
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 9. PROTOCOL COMMANDS
// ============================================================================

const protocolCmd = program
  .command('protocol')
  .description('Protocol analysis commands');

protocolCmd
  .command('lookup <action>')
  .description('Look up protocol action')
  .action(async (action) => {
    try {
      const result = await sendCommand('protocol_lookup', { action });
      if (result.found) {
        console.log(chalk.green.bold(`\n✓ ${action}\n`));
        console.log(chalk.cyan('Category:'), result.category);
        console.log(chalk.cyan('Description:'), result.description);
        console.log(chalk.yellow('\nParameters:'));
        console.log(formatJson(result.parameters));
      } else {
        printError(`Action not found: ${action}`);
      }
    } catch (err) {
      printError(err.message);
    }
  });

protocolCmd
  .command('list')
  .description('List protocol actions')
  .option('-c, --category <category>', 'Category filter')
  .option('-l, --limit <n>', 'Limit', '100')
  .action(async (options) => {
    try {
      const result = await sendCommand('protocol_list', {
        category: options.category,
        limit: parseInt(options.limit)
      });
      printSuccess(`Found ${result.total} actions`);
      result.actions?.forEach(a => {
        console.log(`  ${chalk.cyan(a.name)} ${chalk.gray(`(${a.category})`)}`);
      });
    } catch (err) {
      printError(err.message);
    }
  });

protocolCmd
  .command('search <query>')
  .description('Search protocol actions')
  .option('-l, --limit <n>', 'Limit', '50')
  .action(async (query, options) => {
    try {
      const result = await sendCommand('protocol_search', {
        query,
        limit: parseInt(options.limit)
      });
      printSuccess(`Found ${result.total} matches`);
      result.results?.forEach(r => {
        console.log(`  ${chalk.cyan(r.name)} - ${r.description}`);
      });
    } catch (err) {
      printError(err.message);
    }
  });

protocolCmd
  .command('learn <packetId>')
  .description('Learn action from packet')
  .option('-n, --name <name>', 'Action name')
  .option('-d, --description <desc>', 'Description')
  .action(async (packetId, options) => {
    try {
      const result = await sendCommand('protocol_learn', {
        packetId,
        name: options.name,
        description: options.description
      });
      printSuccess(`Learned action: ${result.action}`);
    } catch (err) {
      printError(err.message);
    }
  });

protocolCmd
  .command('export')
  .description('Export protocol database')
  .option('-f, --format <format>', 'Format (json, markdown, typescript)', 'json')
  .option('-c, --category <category>', 'Category filter')
  .action(async (options) => {
    try {
      const result = await sendCommand('protocol_export', {
        format: options.format,
        category: options.category
      });
      console.log(result.data);
    } catch (err) {
      printError(err.message);
    }
  });

protocolCmd
  .command('import <data>')
  .description('Import protocol database')
  .option('--merge', 'Merge with existing')
  .action(async (data, options) => {
    try {
      await sendCommand('protocol_import', { data, merge: options.merge });
      printSuccess('Protocol database imported');
    } catch (err) {
      printError(err.message);
    }
  });

protocolCmd
  .command('docs')
  .description('Generate documentation')
  .option('-c, --category <category>', 'Category filter')
  .option('-f, --format <format>', 'Format (markdown, html)', 'markdown')
  .action(async (options) => {
    try {
      const result = await sendCommand('protocol_generate_docs', {
        category: options.category,
        format: options.format
      });
      console.log(result.docs);
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 10. FUZZ COMMANDS
// ============================================================================

const fuzzCmd = program
  .command('fuzz')
  .description('Protocol fuzzing commands');

fuzzCmd
  .command('start')
  .description('Start fuzzing')
  .option('-m, --mode <mode>', 'Mode (action_discovery, parameter_boundary, type_confusion, sequence_breaking)', 'action_discovery')
  .option('-t, --target <target>', 'Target action/category')
  .option('-p, --parallelism <n>', 'Parallelism', '3')
  .option('-d, --delay <ms>', 'Delay between requests', '100')
  .action(async (options) => {
    try {
      await sendCommand('fuzz_start', {
        mode: options.mode,
        target: options.target,
        parallelism: parseInt(options.parallelism),
        delayMs: parseInt(options.delay)
      });
      printSuccess('Fuzzing started');
    } catch (err) {
      printError(err.message);
    }
  });

fuzzCmd
  .command('stop')
  .description('Stop fuzzing')
  .action(async () => {
    try {
      await sendCommand('fuzz_stop', {});
      printSuccess('Fuzzing stopped');
    } catch (err) {
      printError(err.message);
    }
  });

fuzzCmd
  .command('status')
  .description('Get fuzzing status')
  .action(async () => {
    try {
      const result = await sendCommand('fuzz_get_status', {});
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

fuzzCmd
  .command('results')
  .description('Get fuzzing results')
  .option('-t, --type <type>', 'Type (all, discoveries, errors)', 'all')
  .action(async (options) => {
    try {
      const result = await sendCommand('fuzz_get_results', { type: options.type });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

fuzzCmd
  .command('export')
  .description('Export discoveries')
  .option('-f, --format <format>', 'Format (json, markdown)', 'json')
  .action(async (options) => {
    try {
      const result = await sendCommand('fuzz_export_discoveries', { format: options.format });
      console.log(result.data);
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 11. FIDDLER COMMANDS
// ============================================================================

const fiddlerCmd = program
  .command('fiddler')
  .description('Fiddler integration commands');

fiddlerCmd
  .command('connect')
  .description('Connect to Fiddler')
  .option('-p, --pipe <name>', 'Pipe name', 'SvonyFiddlerPipe')
  .action(async (options) => {
    try {
      await sendCommand('fiddler_connect', { pipeName: options.pipe });
      printSuccess('Connected to Fiddler');
    } catch (err) {
      printError(err.message);
    }
  });

fiddlerCmd
  .command('disconnect')
  .description('Disconnect from Fiddler')
  .action(async () => {
    try {
      await sendCommand('fiddler_disconnect', {});
      printSuccess('Disconnected from Fiddler');
    } catch (err) {
      printError(err.message);
    }
  });

fiddlerCmd
  .command('status')
  .description('Get Fiddler status')
  .action(async () => {
    try {
      const result = await sendCommand('fiddler_get_status', {});
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

fiddlerCmd
  .command('capture')
  .description('Start/stop Fiddler capture')
  .option('--start', 'Start capture')
  .option('--stop', 'Stop capture')
  .option('-f, --filter <pattern>', 'URL filter', '*evony*')
  .action(async (options) => {
    try {
      if (options.stop) {
        await sendCommand('fiddler_capture_stop', {});
        printSuccess('Fiddler capture stopped');
      } else {
        await sendCommand('fiddler_capture_start', { filter: options.filter });
        printSuccess('Fiddler capture started');
      }
    } catch (err) {
      printError(err.message);
    }
  });

fiddlerCmd
  .command('breakpoint <pattern>')
  .description('Set breakpoint')
  .option('-t, --type <type>', 'Type (request, response)', 'request')
  .option('-a, --action <action>', 'Action (break, modify, drop)', 'break')
  .action(async (pattern, options) => {
    try {
      await sendCommand('fiddler_set_breakpoint', {
        pattern,
        type: options.type,
        action: options.action
      });
      printSuccess(`Breakpoint set: ${pattern}`);
    } catch (err) {
      printError(err.message);
    }
  });

fiddlerCmd
  .command('inject <action>')
  .description('Inject via Fiddler')
  .option('-p, --params <json>', 'Parameters (JSON)')
  .action(async (action, options) => {
    try {
      await sendCommand('fiddler_inject', {
        action,
        params: JSON.parse(options.params || '{}')
      });
      printSuccess('Packet injected via Fiddler');
    } catch (err) {
      printError(err.message);
    }
  });

fiddlerCmd
  .command('export')
  .description('Export Fiddler session')
  .option('-f, --format <format>', 'Format (saz, har, json)', 'saz')
  .option('-o, --output <filename>', 'Output filename')
  .action(async (options) => {
    try {
      const result = await sendCommand('fiddler_export_session', {
        format: options.format,
        filename: options.output
      });
      printSuccess(`Exported to ${result.filename}`);
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 12. LLM COMMANDS
// ============================================================================

const llmCmd = program
  .command('llm')
  .description('LLM integration commands');

llmCmd
  .command('connect')
  .description('Connect to LM Studio')
  .option('-h, --host <host>', 'Host', 'localhost')
  .option('-p, --port <port>', 'Port', '1234')
  .action(async (options) => {
    try {
      await sendCommand('llm_connect', {
        host: options.host,
        port: parseInt(options.port)
      });
      printSuccess('Connected to LM Studio');
    } catch (err) {
      printError(err.message);
    }
  });

llmCmd
  .command('disconnect')
  .description('Disconnect from LM Studio')
  .action(async () => {
    try {
      await sendCommand('llm_disconnect', {});
      printSuccess('Disconnected from LM Studio');
    } catch (err) {
      printError(err.message);
    }
  });

llmCmd
  .command('status')
  .description('Get LLM status')
  .action(async () => {
    try {
      const result = await sendCommand('llm_get_status', {});
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

llmCmd
  .command('explain <packetId>')
  .description('Explain packet using LLM')
  .option('-d, --detail <level>', 'Detail level (brief, detailed, technical)', 'detailed')
  .action(async (packetId, options) => {
    const spinner = ora('Analyzing with LLM...').start();
    try {
      const result = await sendCommand('llm_explain_packet', {
        packetId,
        detail: options.detail
      });
      spinner.stop();
      console.log(chalk.green.bold('\nLLM Analysis\n'));
      console.log(result.explanation);
    } catch (err) {
      spinner.fail(err.message);
    }
  });

llmCmd
  .command('generate <task>')
  .description('Generate automation script')
  .option('-l, --language <lang>', 'Language (python, javascript, csharp)', 'python')
  .action(async (task, options) => {
    const spinner = ora('Generating script...').start();
    try {
      const result = await sendCommand('llm_generate_script', {
        task,
        language: options.language
      });
      spinner.stop();
      console.log(chalk.green.bold('\nGenerated Script\n'));
      console.log(result.script);
    } catch (err) {
      spinner.fail(err.message);
    }
  });

llmCmd
  .command('decode <data>')
  .description('Decode unknown structure using LLM')
  .option('-c, --context <context>', 'Context about the data')
  .action(async (data, options) => {
    const spinner = ora('Decoding with LLM...').start();
    try {
      const result = await sendCommand('llm_decode_unknown', {
        data,
        context: options.context
      });
      spinner.stop();
      console.log(formatJson(result));
    } catch (err) {
      spinner.fail(err.message);
    }
  });

llmCmd
  .command('ask <question>')
  .description('Ask LLM a question')
  .option('--no-context', 'Exclude game context')
  .action(async (question, options) => {
    const spinner = ora('Thinking...').start();
    try {
      const result = await sendCommand('llm_ask', {
        question,
        includeContext: options.context !== false
      });
      spinner.stop();
      console.log(chalk.green.bold('\nAnswer\n'));
      console.log(result.answer);
    } catch (err) {
      spinner.fail(err.message);
    }
  });

llmCmd
  .command('stats')
  .description('Get inference statistics')
  .action(async () => {
    try {
      const result = await sendCommand('llm_get_stats', {});
      console.log(chalk.green.bold('\nLLM Statistics\n'));
      console.log(chalk.cyan('Tokens/sec:'), result.tokensPerSec);
      console.log(chalk.cyan('VRAM Usage:'), `${result.vramUsed}GB / ${result.vramTotal}GB`);
      console.log(chalk.cyan('GPU Temp:'), `${result.gpuTemp}°C`);
      console.log(chalk.cyan('Model:'), result.model);
    } catch (err) {
      printError(err.message);
    }
  });

llmCmd
  .command('models')
  .description('List available models')
  .action(async () => {
    try {
      const result = await sendCommand('llm_list_models', {});
      printSuccess(`Found ${result.models?.length || 0} models`);
      result.models?.forEach(m => {
        console.log(`  ${chalk.cyan(m.name)} (${m.size})`);
      });
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 13. CHAT COMMANDS
// ============================================================================

const chatCmd = program
  .command('chat')
  .description('Chatbot interface commands');

chatCmd
  .command('send <message>')
  .description('Send message to chatbot')
  .action(async (message) => {
    const spinner = ora('Sending...').start();
    try {
      const result = await sendCommand('chat_send', { message });
      spinner.stop();
      console.log(chalk.green.bold('\nResponse\n'));
      console.log(result.response);
    } catch (err) {
      spinner.fail(err.message);
    }
  });

chatCmd
  .command('history')
  .description('Get chat history')
  .option('-l, --limit <n>', 'Limit', '50')
  .action(async (options) => {
    try {
      const result = await sendCommand('chat_get_history', { limit: parseInt(options.limit) });
      result.messages?.forEach(m => {
        const prefix = m.role === 'user' ? chalk.blue('You:') : chalk.green('AI:');
        console.log(`${prefix} ${m.content}`);
        console.log();
      });
    } catch (err) {
      printError(err.message);
    }
  });

chatCmd
  .command('clear')
  .description('Clear chat history')
  .action(async () => {
    try {
      await sendCommand('chat_clear', {});
      printSuccess('Chat history cleared');
    } catch (err) {
      printError(err.message);
    }
  });

chatCmd
  .command('templates')
  .description('List prompt templates')
  .option('-c, --category <category>', 'Category filter')
  .action(async (options) => {
    try {
      const result = await sendCommand('chat_list_templates', { category: options.category });
      result.templates?.forEach(t => {
        console.log(`  ${chalk.cyan(t.id)} - ${t.name}`);
        console.log(`    ${chalk.gray(t.description)}`);
      });
    } catch (err) {
      printError(err.message);
    }
  });

chatCmd
  .command('use-template <templateId>')
  .description('Use prompt template')
  .option('-v, --vars <json>', 'Template variables (JSON)')
  .action(async (templateId, options) => {
    const spinner = ora('Using template...').start();
    try {
      const result = await sendCommand('chat_use_template', {
        templateId,
        variables: options.vars ? JSON.parse(options.vars) : {}
      });
      spinner.stop();
      console.log(chalk.green.bold('\nResponse\n'));
      console.log(result.response);
    } catch (err) {
      spinner.fail(err.message);
    }
  });

chatCmd
  .command('export')
  .description('Export chat history')
  .option('-f, --format <format>', 'Format (json, markdown, txt)', 'json')
  .action(async (options) => {
    try {
      const result = await sendCommand('chat_export', { format: options.format });
      console.log(result.data);
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 14. ANALYTICS COMMANDS
// ============================================================================

const analyticsCmd = program
  .command('analytics')
  .description('Analytics commands');

analyticsCmd
  .command('summary')
  .description('Get analytics summary')
  .option('-p, --period <period>', 'Period (day, week, month, all)', 'week')
  .action(async (options) => {
    try {
      const result = await sendCommand('analytics_get_summary', { period: options.period });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

analyticsCmd
  .command('power')
  .description('Get power history')
  .option('-p, --period <period>', 'Period (day, week, month)', 'week')
  .action(async (options) => {
    try {
      const result = await sendCommand('analytics_get_power', { period: options.period });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

analyticsCmd
  .command('resources')
  .description('Get resource efficiency')
  .option('-p, --period <period>', 'Period (day, week, month)', 'week')
  .action(async (options) => {
    try {
      const result = await sendCommand('analytics_get_resources', { period: options.period });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

analyticsCmd
  .command('combat')
  .description('Get combat statistics')
  .option('-p, --period <period>', 'Period (day, week, month)', 'week')
  .action(async (options) => {
    try {
      const result = await sendCommand('analytics_get_combat', { period: options.period });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

analyticsCmd
  .command('activity')
  .description('Get activity patterns')
  .option('-p, --period <period>', 'Period (day, week, month)', 'week')
  .action(async (options) => {
    try {
      const result = await sendCommand('analytics_get_activity', { period: options.period });
      console.log(formatJson(result));
    } catch (err) {
      printError(err.message);
    }
  });

analyticsCmd
  .command('export')
  .description('Export analytics')
  .option('-p, --period <period>', 'Period (day, week, month, all)', 'all')
  .option('-f, --format <format>', 'Format (json, csv, xlsx)', 'json')
  .action(async (options) => {
    try {
      const result = await sendCommand('analytics_export', {
        period: options.period,
        format: options.format
      });
      printSuccess(`Exported to ${result.filename}`);
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 15. WEBHOOK COMMANDS
// ============================================================================

const webhookCmd = program
  .command('webhook')
  .description('Webhook commands');

webhookCmd
  .command('list')
  .description('List webhooks')
  .action(async () => {
    try {
      const result = await sendCommand('webhook_list', {});
      printSuccess(`Found ${result.webhooks?.length || 0} webhooks`);
      result.webhooks?.forEach(w => {
        const status = w.enabled ? chalk.green('●') : chalk.gray('○');
        console.log(`  ${status} ${chalk.cyan(w.id)} - ${w.name} (${w.platform})`);
      });
    } catch (err) {
      printError(err.message);
    }
  });

webhookCmd
  .command('add <name> <url>')
  .description('Add webhook')
  .option('-p, --platform <platform>', 'Platform (discord, telegram, slack, teams, custom)', 'custom')
  .option('-e, --events <events>', 'Events (comma-separated)')
  .action(async (name, url, options) => {
    try {
      const result = await sendCommand('webhook_add', {
        name,
        url,
        platform: options.platform,
        events: options.events?.split(',') || []
      });
      printSuccess(`Added webhook: ${result.webhookId}`);
    } catch (err) {
      printError(err.message);
    }
  });

webhookCmd
  .command('remove <webhookId>')
  .description('Remove webhook')
  .action(async (webhookId) => {
    try {
      await sendCommand('webhook_remove', { webhookId });
      printSuccess(`Removed webhook: ${webhookId}`);
    } catch (err) {
      printError(err.message);
    }
  });

webhookCmd
  .command('test <webhookId>')
  .description('Test webhook')
  .action(async (webhookId) => {
    const spinner = ora('Testing webhook...').start();
    try {
      const result = await sendCommand('webhook_test', { webhookId });
      spinner.succeed(`Webhook test: ${result.success ? 'Success' : 'Failed'}`);
    } catch (err) {
      spinner.fail(err.message);
    }
  });

webhookCmd
  .command('events <webhookId>')
  .description('Configure webhook events')
  .option('-e, --events <events>', 'Events (comma-separated)')
  .action(async (webhookId, options) => {
    try {
      await sendCommand('webhook_set_events', {
        webhookId,
        events: options.events?.split(',') || []
      });
      printSuccess('Webhook events updated');
    } catch (err) {
      printError(err.message);
    }
  });

webhookCmd
  .command('enable <webhookId>')
  .description('Enable webhook')
  .action(async (webhookId) => {
    try {
      await sendCommand('webhook_enable', { webhookId });
      printSuccess(`Enabled webhook: ${webhookId}`);
    } catch (err) {
      printError(err.message);
    }
  });

webhookCmd
  .command('disable <webhookId>')
  .description('Disable webhook')
  .action(async (webhookId) => {
    try {
      await sendCommand('webhook_disable', { webhookId });
      printSuccess(`Disabled webhook: ${webhookId}`);
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 16. RECORDING COMMANDS
// ============================================================================

const recordingCmd = program
  .command('recording')
  .alias('rec')
  .description('Session recording commands');

recordingCmd
  .command('start')
  .description('Start recording')
  .option('-n, --name <name>', 'Recording name')
  .option('-f, --filter <pattern>', 'Action filter')
  .action(async (options) => {
    try {
      const result = await sendCommand('recording_start', {
        name: options.name,
        filter: options.filter
      });
      printSuccess(`Recording started: ${result.recordingId}`);
    } catch (err) {
      printError(err.message);
    }
  });

recordingCmd
  .command('stop')
  .description('Stop recording')
  .action(async () => {
    try {
      const result = await sendCommand('recording_stop', {});
      printSuccess(`Recording stopped: ${result.recordingId}`);
    } catch (err) {
      printError(err.message);
    }
  });

recordingCmd
  .command('list')
  .description('List recordings')
  .action(async () => {
    try {
      const result = await sendCommand('recording_list', {});
      printSuccess(`Found ${result.recordings?.length || 0} recordings`);
      result.recordings?.forEach(r => {
        console.log(`  ${chalk.cyan(r.id)} - ${r.name} (${r.packetCount} packets)`);
      });
    } catch (err) {
      printError(err.message);
    }
  });

recordingCmd
  .command('play <recordingId>')
  .description('Play recording')
  .option('-s, --speed <n>', 'Speed multiplier', '1.0')
  .option('--start-from <n>', 'Start from packet index', '0')
  .action(async (recordingId, options) => {
    const spinner = ora('Playing recording...').start();
    try {
      await sendCommand('recording_play', {
        recordingId,
        speed: parseFloat(options.speed),
        startFrom: parseInt(options.startFrom)
      });
      spinner.succeed('Playback complete');
    } catch (err) {
      spinner.fail(err.message);
    }
  });

recordingCmd
  .command('export <recordingId>')
  .description('Export recording')
  .option('-f, --format <format>', 'Format (json, pcap)', 'json')
  .action(async (recordingId, options) => {
    try {
      const result = await sendCommand('recording_export', {
        recordingId,
        format: options.format
      });
      printSuccess(`Exported to ${result.filename}`);
    } catch (err) {
      printError(err.message);
    }
  });

recordingCmd
  .command('delete <recordingId>')
  .description('Delete recording')
  .action(async (recordingId) => {
    try {
      await sendCommand('recording_delete', { recordingId });
      printSuccess(`Deleted recording: ${recordingId}`);
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 17. STATUSBAR COMMANDS
// ============================================================================

const statusbarCmd = program
  .command('statusbar')
  .alias('sb')
  .description('Status bar commands');

statusbarCmd
  .command('widgets')
  .description('List status bar widgets')
  .action(async () => {
    try {
      const result = await sendCommand('statusbar_get_widgets', {});
      result.widgets?.forEach(w => {
        const status = w.visible ? chalk.green('●') : chalk.gray('○');
        console.log(`  ${status} ${chalk.cyan(w.id)} - ${w.name} (${w.type})`);
      });
    } catch (err) {
      printError(err.message);
    }
  });

statusbarCmd
  .command('update <widgetId>')
  .description('Update widget value')
  .option('-v, --value <n>', 'Value')
  .option('-t, --text <text>', 'Display text')
  .action(async (widgetId, options) => {
    try {
      await sendCommand('statusbar_update', {
        widgetId,
        value: options.value ? parseFloat(options.value) : undefined,
        text: options.text
      });
      printSuccess(`Updated widget: ${widgetId}`);
    } catch (err) {
      printError(err.message);
    }
  });

statusbarCmd
  .command('show <widgetId>')
  .description('Show widget')
  .action(async (widgetId) => {
    try {
      await sendCommand('statusbar_show', { widgetId });
      printSuccess(`Showing widget: ${widgetId}`);
    } catch (err) {
      printError(err.message);
    }
  });

statusbarCmd
  .command('hide <widgetId>')
  .description('Hide widget')
  .action(async (widgetId) => {
    try {
      await sendCommand('statusbar_hide', { widgetId });
      printSuccess(`Hidden widget: ${widgetId}`);
    } catch (err) {
      printError(err.message);
    }
  });

statusbarCmd
  .command('preset')
  .description('Save/load preset')
  .option('-s, --save <name>', 'Save preset')
  .option('-l, --load <name>', 'Load preset')
  .action(async (options) => {
    try {
      if (options.save) {
        await sendCommand('statusbar_save_preset', { name: options.save });
        printSuccess(`Saved preset: ${options.save}`);
      } else if (options.load) {
        await sendCommand('statusbar_load_preset', { name: options.load });
        printSuccess(`Loaded preset: ${options.load}`);
      }
    } catch (err) {
      printError(err.message);
    }
  });

statusbarCmd
  .command('reset')
  .description('Reset to defaults')
  .action(async () => {
    try {
      await sendCommand('statusbar_reset', {});
      printSuccess('Status bar reset to defaults');
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 18. SETTINGS COMMANDS
// ============================================================================

const settingsCmd = program
  .command('settings')
  .description('Settings commands');

settingsCmd
  .command('get <key>')
  .description('Get setting value')
  .action(async (key) => {
    try {
      const result = await sendCommand('settings_get', { key });
      console.log(chalk.cyan(`${key}:`), result.value);
    } catch (err) {
      printError(err.message);
    }
  });

settingsCmd
  .command('set <key> <value>')
  .description('Set setting value')
  .action(async (key, value) => {
    try {
      // Try to parse as JSON, otherwise use as string
      let parsedValue;
      try {
        parsedValue = JSON.parse(value);
      } catch {
        parsedValue = value;
      }
      await sendCommand('settings_set', { key, value: parsedValue });
      printSuccess(`Set ${key} = ${value}`);
    } catch (err) {
      printError(err.message);
    }
  });

settingsCmd
  .command('list')
  .description('List all settings')
  .option('-c, --category <category>', 'Category filter')
  .action(async (options) => {
    try {
      const result = await sendCommand('settings_list', { category: options.category });
      Object.entries(result.settings || {}).forEach(([key, value]) => {
        console.log(`  ${chalk.cyan(key)}: ${JSON.stringify(value)}`);
      });
    } catch (err) {
      printError(err.message);
    }
  });

settingsCmd
  .command('reset')
  .description('Reset settings')
  .option('-c, --category <category>', 'Category to reset')
  .option('-y, --yes', 'Confirm reset')
  .action(async (options) => {
    if (!options.yes) {
      console.log(chalk.yellow('Use --yes to confirm reset'));
      return;
    }
    try {
      await sendCommand('settings_reset', { category: options.category, confirm: true });
      printSuccess('Settings reset');
    } catch (err) {
      printError(err.message);
    }
  });

settingsCmd
  .command('export')
  .description('Export settings')
  .option('-f, --format <format>', 'Format (json, yaml)', 'json')
  .action(async (options) => {
    try {
      const result = await sendCommand('settings_export', { format: options.format });
      console.log(result.data);
    } catch (err) {
      printError(err.message);
    }
  });

settingsCmd
  .command('import <data>')
  .description('Import settings')
  .option('-f, --format <format>', 'Format (json, yaml)', 'json')
  .option('--merge', 'Merge with existing')
  .action(async (data, options) => {
    try {
      await sendCommand('settings_import', {
        data,
        format: options.format,
        merge: options.merge
      });
      printSuccess('Settings imported');
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 19. MCP COMMANDS
// ============================================================================

const mcpCmd = program
  .command('mcp')
  .description('MCP server management');

mcpCmd
  .command('status')
  .description('Get all MCP server status')
  .action(async () => {
    try {
      const result = await sendCommand('mcp_get_status', {});
      console.log(chalk.green.bold('\nMCP Server Status\n'));
      Object.entries(result.servers || {}).forEach(([name, status]) => {
        const indicator = status.connected ? chalk.green('●') : chalk.red('○');
        console.log(`  ${indicator} ${chalk.cyan(name)} - ${status.status}`);
      });
    } catch (err) {
      printError(err.message);
    }
  });

mcpCmd
  .command('connect <server>')
  .description('Connect to MCP server')
  .action(async (server) => {
    try {
      await sendCommand('mcp_connect', { server });
      printSuccess(`Connected to ${server}`);
    } catch (err) {
      printError(err.message);
    }
  });

mcpCmd
  .command('disconnect <server>')
  .description('Disconnect from MCP server')
  .action(async (server) => {
    try {
      await sendCommand('mcp_disconnect', { server });
      printSuccess(`Disconnected from ${server}`);
    } catch (err) {
      printError(err.message);
    }
  });

mcpCmd
  .command('tools')
  .description('List available MCP tools')
  .option('-s, --server <server>', 'Server filter')
  .option('-c, --category <category>', 'Category filter')
  .action(async (options) => {
    try {
      const result = await sendCommand('mcp_list_tools', {
        server: options.server,
        category: options.category
      });
      printSuccess(`Found ${result.tools?.length || 0} tools`);
      result.tools?.forEach(t => {
        console.log(`  ${chalk.cyan(t.name)} - ${t.description}`);
      });
    } catch (err) {
      printError(err.message);
    }
  });

mcpCmd
  .command('call <tool>')
  .description('Call MCP tool directly')
  .option('-p, --params <json>', 'Tool parameters (JSON)')
  .action(async (tool, options) => {
    const spinner = ora(`Calling ${tool}...`).start();
    try {
      const result = await sendCommand('mcp_call_tool', {
        tool,
        params: options.params ? JSON.parse(options.params) : {}
      });
      spinner.stop();
      console.log(formatJson(result));
    } catch (err) {
      spinner.fail(err.message);
    }
  });

mcpCmd
  .command('logs')
  .description('Get MCP logs')
  .option('-s, --server <server>', 'Server filter')
  .option('-l, --limit <n>', 'Limit', '100')
  .option('--level <level>', 'Level (all, info, warning, error)', 'all')
  .action(async (options) => {
    try {
      const result = await sendCommand('mcp_get_logs', {
        server: options.server,
        limit: parseInt(options.limit),
        level: options.level
      });
      result.logs?.forEach(log => {
        const color = log.level === 'error' ? chalk.red : log.level === 'warning' ? chalk.yellow : chalk.white;
        console.log(color(`[${log.timestamp}] ${log.server}: ${log.message}`));
      });
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// 20. EXPORT/IMPORT COMMANDS
// ============================================================================

const exportCmd = program
  .command('export')
  .description('Export data');

exportCmd
  .command('all [filename]')
  .description('Export all data')
  .option('--no-settings', 'Exclude settings')
  .option('--no-protocols', 'Exclude protocols')
  .option('--no-packets', 'Exclude packets')
  .option('--no-sessions', 'Exclude sessions')
  .action(async (filename, options) => {
    const spinner = ora('Exporting all data...').start();
    try {
      const result = await sendCommand('export_all', {
        filename,
        includeSettings: options.settings !== false,
        includeProtocols: options.protocols !== false,
        includePackets: options.packets !== false,
        includeSessions: options.sessions !== false
      });
      spinner.succeed(`Exported to ${result.filename}`);
    } catch (err) {
      spinner.fail(err.message);
    }
  });

const importCmd = program
  .command('import')
  .description('Import data');

importCmd
  .command('validate <filename>')
  .description('Validate import file')
  .option('-t, --type <type>', 'Type (protocols, packets, sessions, settings, auto)', 'auto')
  .action(async (filename, options) => {
    try {
      const result = await sendCommand('import_validate', { filename, type: options.type });
      if (result.valid) {
        printSuccess(`Valid ${result.type} file`);
        console.log(chalk.cyan('Records:'), result.recordCount);
      } else {
        printError(`Invalid file: ${result.error}`);
      }
    } catch (err) {
      printError(err.message);
    }
  });

// ============================================================================
// SERVER COMMAND
// ============================================================================

program
  .command('server')
  .description('Start WebSocket API server')
  .option('-p, --port <port>', 'Port number', '9876')
  .action(async (options) => {
    const { startServer } = await import('./server.js');
    await startServer(parseInt(options.port));
  });

// ============================================================================
// STATUS COMMAND
// ============================================================================

program
  .command('status')
  .description('Check connection status')
  .action(async () => {
    const spinner = ora('Checking status...').start();
    
    try {
      const ws = new WebSocket(`ws://${CONFIG.wsHost}:${CONFIG.wsPort}`);
      
      await new Promise((resolve, reject) => {
        ws.on('open', () => {
          spinner.succeed('Connected to Svony Browser');
          ws.close();
          resolve();
        });
        ws.on('error', (err) => {
          spinner.fail('Not connected');
          console.log(chalk.yellow('\nStart the browser or run: svony server'));
          reject(err);
        });
        
        setTimeout(() => {
          ws.close();
          reject(new Error('Connection timeout'));
        }, 5000);
      });
    } catch (error) {
      // Error already handled
    }
  });

// ============================================================================
// CONFIG COMMAND
// ============================================================================

program
  .command('config')
  .description('Show CLI configuration')
  .action(async () => {
    console.log(chalk.green.bold('\nSvony CLI v5.0 Configuration\n'));
    console.log(chalk.cyan('WebSocket Host:'), CONFIG.wsHost);
    console.log(chalk.cyan('WebSocket Port:'), CONFIG.wsPort);
    console.log(chalk.cyan('Config Directory:'), CONFIG.configDir);
    console.log(chalk.cyan('Version:'), CONFIG.version);
    console.log(chalk.cyan('Commands:'), '168 across 20 categories');
  });

// ============================================================================
// INTERACTIVE MODE
// ============================================================================

program
  .command('interactive')
  .alias('i')
  .description('Start interactive mode')
  .action(async () => {
    console.log(chalk.green.bold('\nSvony CLI v5.0 - Interactive Mode\n'));
    console.log(chalk.gray('Type "help" for commands, "exit" to quit\n'));
    
    const rl = createInterface({
      input: process.stdin,
      output: process.stdout,
      prompt: chalk.cyan('svony> ')
    });
    
    rl.prompt();
    
    rl.on('line', async (line) => {
      const input = line.trim();
      
      if (input === 'exit' || input === 'quit') {
        console.log(chalk.gray('Goodbye!'));
        rl.close();
        process.exit(0);
      }
      
      if (input === 'help') {
        console.log(chalk.yellow('\nAvailable command categories:'));
        console.log('  browser, session, account, game, autopilot, combat, map');
        console.log('  packet, protocol, fuzz, fiddler, llm, chat, analytics');
        console.log('  webhook, recording, statusbar, settings, mcp, export, import');
        console.log('\nType "<category> --help" for category commands');
        console.log();
      } else if (input) {
        try {
          await program.parseAsync(['node', 'svony', ...input.split(' ')]);
        } catch (err) {
          // Commander handles errors
        }
      }
      
      rl.prompt();
    });
  });

// ============================================================================
// MAIN
// ============================================================================

program.parse();
