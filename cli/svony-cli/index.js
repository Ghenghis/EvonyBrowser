#!/usr/bin/env node
/**
 * Svony CLI - Command-line interface for Evony protocol analysis and automation
 * Supports integration with Claude Desktop, Windsurf IDE, and LM Studio
 */

import { Command } from 'commander';
import chalk from 'chalk';
import ora from 'ora';
import { WebSocket } from 'ws';
import fs from 'fs/promises';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Configuration
const CONFIG = {
  wsHost: process.env.SVONY_WS_HOST || 'localhost',
  wsPort: parseInt(process.env.SVONY_WS_PORT || '9876'),
  configDir: process.env.SVONY_CONFIG_DIR || path.join(process.env.HOME || '', '.svony'),
  version: '1.0.0'
};

// Initialize CLI
const program = new Command();

program
  .name('svony')
  .description('Svony CLI - Evony protocol analysis and automation')
  .version(CONFIG.version);

// ============================================================================
// Protocol Commands
// ============================================================================

const protocolCmd = program
  .command('protocol')
  .description('Protocol analysis commands');

protocolCmd
  .command('lookup <action>')
  .description('Look up protocol action definition')
  .action(async (action) => {
    const spinner = ora('Looking up protocol action...').start();
    
    try {
      const result = await sendCommand('protocol_lookup', { action });
      spinner.stop();
      
      if (result.found) {
        console.log(chalk.green.bold(`\n✓ Action: ${action}\n`));
        console.log(chalk.cyan('Category:'), result.category);
        console.log(chalk.cyan('Description:'), result.description);
        
        console.log(chalk.yellow('\nRequest Parameters:'));
        for (const [key, type] of Object.entries(result.request || {})) {
          console.log(`  ${chalk.white(key)}: ${chalk.gray(type)}`);
        }
        
        console.log(chalk.yellow('\nResponse:'));
        for (const [key, type] of Object.entries(result.response || {})) {
          console.log(`  ${chalk.white(key)}: ${chalk.gray(type)}`);
        }
      } else {
        console.log(chalk.red(`\n✗ Action "${action}" not found`));
        if (result.similar_actions?.length > 0) {
          console.log(chalk.yellow('\nSimilar actions:'));
          result.similar_actions.forEach(a => console.log(`  - ${a}`));
        }
      }
    } catch (error) {
      spinner.fail('Failed to lookup action');
      console.error(chalk.red(error.message));
    }
  });

protocolCmd
  .command('list [category]')
  .description('List protocol actions')
  .option('-l, --limit <n>', 'Limit results', '50')
  .action(async (category, options) => {
    const spinner = ora('Fetching protocol actions...').start();
    
    try {
      const result = await sendCommand('protocol_list', { category });
      spinner.stop();
      
      console.log(chalk.green.bold(`\n✓ Found ${result.total_actions} actions\n`));
      
      if (result.categories) {
        console.log(chalk.yellow('Categories:'), result.categories.join(', '));
        console.log();
      }
      
      const actions = result.actions?.slice(0, parseInt(options.limit)) || [];
      actions.forEach(a => {
        console.log(`${chalk.cyan(a.name)} ${chalk.gray(`(${a.category})`)}`);
        console.log(`  ${a.description}`);
      });
    } catch (error) {
      spinner.fail('Failed to list actions');
      console.error(chalk.red(error.message));
    }
  });

protocolCmd
  .command('search <query>')
  .description('Search protocol actions')
  .option('-l, --limit <n>', 'Limit results', '20')
  .action(async (query, options) => {
    const spinner = ora('Searching...').start();
    
    try {
      const result = await sendCommand('protocol_search', { query, limit: parseInt(options.limit) });
      spinner.stop();
      
      console.log(chalk.green.bold(`\n✓ Found ${result.results_count} matches\n`));
      
      result.results?.forEach(r => {
        console.log(`${chalk.cyan(r.name)} ${chalk.gray(`(${r.category})`)}`);
        console.log(`  ${r.description}`);
      });
    } catch (error) {
      spinner.fail('Search failed');
      console.error(chalk.red(error.message));
    }
  });

// ============================================================================
// AMF Commands
// ============================================================================

const amfCmd = program
  .command('amf')
  .description('AMF3 encoding/decoding commands');

amfCmd
  .command('decode <data>')
  .description('Decode AMF3 hex data')
  .option('-f, --format <format>', 'Input format (hex, base64)', 'hex')
  .action(async (data, options) => {
    const spinner = ora('Decoding AMF3 data...').start();
    
    try {
      const result = await sendCommand('amf_decode', { data, format: options.format });
      spinner.stop();
      
      if (result.success) {
        console.log(chalk.green.bold('\n✓ Decoded successfully\n'));
        console.log(chalk.cyan('Input bytes:'), result.input_bytes);
        console.log(chalk.yellow('\nDecoded data:'));
        console.log(JSON.stringify(result.decoded, null, 2));
      } else {
        console.log(chalk.red('\n✗ Decode failed'));
        console.log(result.error);
      }
    } catch (error) {
      spinner.fail('Decode failed');
      console.error(chalk.red(error.message));
    }
  });

amfCmd
  .command('encode <json>')
  .description('Encode JSON to AMF3')
  .option('-f, --format <format>', 'Output format (hex, base64)', 'hex')
  .action(async (json, options) => {
    const spinner = ora('Encoding to AMF3...').start();
    
    try {
      const result = await sendCommand('amf_encode', { data: json, output_format: options.format });
      spinner.stop();
      
      if (result.success) {
        console.log(chalk.green.bold('\n✓ Encoded successfully\n'));
        console.log(chalk.cyan('Output bytes:'), result.output_bytes);
        console.log(chalk.yellow('\nEncoded data:'));
        console.log(result.encoded);
      } else {
        console.log(chalk.red('\n✗ Encode failed'));
        console.log(result.error);
      }
    } catch (error) {
      spinner.fail('Encode failed');
      console.error(chalk.red(error.message));
    }
  });

// ============================================================================
// Traffic Commands
// ============================================================================

const trafficCmd = program
  .command('traffic')
  .description('Traffic capture and analysis commands');

trafficCmd
  .command('list')
  .description('List captured traffic')
  .option('-l, --limit <n>', 'Limit results', '50')
  .option('-d, --direction <dir>', 'Filter by direction (request, response, all)', 'all')
  .option('--decoded', 'Only show decoded entries')
  .action(async (options) => {
    const spinner = ora('Fetching traffic...').start();
    
    try {
      const result = await sendCommand('traffic_list', {
        limit: parseInt(options.limit),
        direction: options.direction,
        decoded_only: options.decoded || false
      });
      spinner.stop();
      
      console.log(chalk.green.bold(`\n✓ ${result.total_captured} total entries\n`));
      
      result.entries?.forEach(e => {
        const dirColor = e.direction === 'request' ? chalk.blue : chalk.green;
        console.log(`${dirColor(`[${e.direction}]`)} ${e.timestamp} - ${e.data_length} bytes (${e.decoded})`);
      });
    } catch (error) {
      spinner.fail('Failed to list traffic');
      console.error(chalk.red(error.message));
    }
  });

trafficCmd
  .command('get <id>')
  .description('Get traffic entry details')
  .action(async (id) => {
    const spinner = ora('Fetching entry...').start();
    
    try {
      const result = await sendCommand('traffic_get', { capture_id: id });
      spinner.stop();
      
      console.log(chalk.green.bold('\n✓ Traffic Entry\n'));
      console.log(chalk.cyan('ID:'), result.id);
      console.log(chalk.cyan('Direction:'), result.direction);
      console.log(chalk.cyan('URL:'), result.url);
      console.log(chalk.cyan('Timestamp:'), result.timestamp);
      console.log(chalk.cyan('Size:'), result.data_length, 'bytes');
      
      if (result.decoded) {
        console.log(chalk.yellow('\nDecoded:'));
        console.log(JSON.stringify(result.decoded, null, 2));
      }
    } catch (error) {
      spinner.fail('Failed to get entry');
      console.error(chalk.red(error.message));
    }
  });

trafficCmd
  .command('clear')
  .description('Clear captured traffic')
  .action(async () => {
    const spinner = ora('Clearing traffic...').start();
    
    try {
      const result = await sendCommand('traffic_clear', {});
      spinner.stop();
      console.log(chalk.green.bold(`\n✓ Cleared ${result.cleared} entries`));
    } catch (error) {
      spinner.fail('Failed to clear traffic');
      console.error(chalk.red(error.message));
    }
  });

trafficCmd
  .command('export [filename]')
  .description('Export traffic to file')
  .action(async (filename) => {
    const spinner = ora('Exporting traffic...').start();
    
    try {
      const result = await sendCommand('traffic_export', { filename });
      spinner.stop();
      console.log(chalk.green.bold(`\n✓ Exported ${result.entries_exported} entries to ${result.file}`));
    } catch (error) {
      spinner.fail('Export failed');
      console.error(chalk.red(error.message));
    }
  });

// ============================================================================
// Calculator Commands
// ============================================================================

const calcCmd = program
  .command('calc')
  .description('Game calculation commands');

calcCmd
  .command('training <troop> <amount>')
  .description('Calculate troop training costs')
  .option('-b, --barracks <level>', 'Barracks level', '10')
  .option('-t, --tech <level>', 'Military science tech level', '0')
  .action(async (troop, amount, options) => {
    const spinner = ora('Calculating...').start();
    
    try {
      const result = await sendCommand('calc_training', {
        troop_type: troop,
        amount: parseInt(amount),
        barracks_level: parseInt(options.barracks),
        tech_bonus: parseInt(options.tech)
      });
      spinner.stop();
      
      console.log(chalk.green.bold(`\n✓ Training ${amount} ${result.troop}\n`));
      
      console.log(chalk.yellow('Costs:'));
      console.log(`  Food:   ${result.costs.food}`);
      console.log(`  Lumber: ${result.costs.lumber}`);
      console.log(`  Stone:  ${result.costs.stone}`);
      console.log(`  Iron:   ${result.costs.iron}`);
      console.log(`  Gold:   ${result.costs.gold}`);
      
      console.log(chalk.yellow('\nTime:'), result.training_time.formatted);
      
      console.log(chalk.yellow('\nStats:'));
      console.log(`  Total Attack:  ${result.stats.total_attack}`);
      console.log(`  Total Defense: ${result.stats.total_defense}`);
      console.log(`  Total Load:    ${result.stats.total_load}`);
      console.log(`  Food/hr:       ${result.stats.food_consumption}`);
    } catch (error) {
      spinner.fail('Calculation failed');
      console.error(chalk.red(error.message));
    }
  });

calcCmd
  .command('march <fromX> <fromY> <toX> <toY>')
  .description('Calculate march time')
  .option('-s, --slowest <troop>', 'Slowest troop type', 'a')
  .option('-l, --logistics <level>', 'Logistics tech level', '0')
  .action(async (fromX, fromY, toX, toY, options) => {
    const spinner = ora('Calculating...').start();
    
    try {
      const result = await sendCommand('calc_march', {
        from_x: parseInt(fromX),
        from_y: parseInt(fromY),
        to_x: parseInt(toX),
        to_y: parseInt(toY),
        slowest_troop: options.slowest,
        logistics_level: parseInt(options.logistics)
      });
      spinner.stop();
      
      console.log(chalk.green.bold('\n✓ March Calculation\n'));
      console.log(chalk.cyan('From:'), `(${result.route.from.x}, ${result.route.from.y})`);
      console.log(chalk.cyan('To:'), `(${result.route.to.x}, ${result.route.to.y})`);
      console.log(chalk.cyan('Distance:'), result.route.distance, 'tiles');
      console.log(chalk.cyan('Slowest:'), result.slowest_troop);
      console.log(chalk.yellow('\nMarch Time:'), result.march_time.formatted);
    } catch (error) {
      spinner.fail('Calculation failed');
      console.error(chalk.red(error.message));
    }
  });

// ============================================================================
// Knowledge Commands
// ============================================================================

const kbCmd = program
  .command('kb')
  .description('Knowledge base commands');

kbCmd
  .command('search <query>')
  .description('Search knowledge base')
  .option('-l, --limit <n>', 'Limit results', '10')
  .option('-c, --category <cat>', 'Category filter', 'all')
  .action(async (query, options) => {
    const spinner = ora('Searching knowledge base...').start();
    
    try {
      const result = await sendCommand('evony_search', {
        query,
        limit: parseInt(options.limit),
        category: options.category
      });
      spinner.stop();
      
      console.log(chalk.green.bold(`\n✓ Found ${result.total_results} results\n`));
      
      result.results?.forEach(r => {
        console.log(chalk.cyan(`[${r.metadata?.category || 'unknown'}]`), r.id);
        console.log(`  ${r.content?.substring(0, 200)}...`);
        console.log(`  Relevance: ${r.relevance}`);
        console.log();
      });
    } catch (error) {
      spinner.fail('Search failed');
      console.error(chalk.red(error.message));
    }
  });

kbCmd
  .command('ask <question>')
  .description('Ask a question')
  .action(async (question) => {
    const spinner = ora('Thinking...').start();
    
    try {
      const result = await sendCommand('evony_query', {
        question,
        include_sources: true
      });
      spinner.stop();
      
      console.log(chalk.green.bold('\n✓ Answer\n'));
      console.log(result.answer);
      
      if (result.sources?.length > 0) {
        console.log(chalk.yellow('\nSources:'));
        result.sources.forEach(s => {
          console.log(`  - ${s.category}: ${s.filename}`);
        });
      }
    } catch (error) {
      spinner.fail('Query failed');
      console.error(chalk.red(error.message));
    }
  });

// ============================================================================
// Server Commands
// ============================================================================

program
  .command('server')
  .description('Start WebSocket API server')
  .option('-p, --port <port>', 'Port number', '9876')
  .action(async (options) => {
    const { startServer } = await import('./server.js');
    await startServer(parseInt(options.port));
  });

program
  .command('status')
  .description('Check connection status')
  .action(async () => {
    const spinner = ora('Checking status...').start();
    
    try {
      const ws = new WebSocket(`ws://${CONFIG.wsHost}:${CONFIG.wsPort}`);
      
      await new Promise((resolve, reject) => {
        ws.on('open', () => {
          spinner.succeed('Connected to Svony server');
          ws.close();
          resolve();
        });
        ws.on('error', (err) => {
          spinner.fail('Not connected');
          console.log(chalk.yellow('\nStart the server with: svony server'));
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
// Configuration Commands
// ============================================================================

program
  .command('config')
  .description('Show configuration')
  .action(async () => {
    console.log(chalk.green.bold('\nSvony CLI Configuration\n'));
    console.log(chalk.cyan('WebSocket Host:'), CONFIG.wsHost);
    console.log(chalk.cyan('WebSocket Port:'), CONFIG.wsPort);
    console.log(chalk.cyan('Config Directory:'), CONFIG.configDir);
    console.log(chalk.cyan('Version:'), CONFIG.version);
  });

// ============================================================================
// Helper Functions
// ============================================================================

async function sendCommand(command, params) {
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
      reject(new Error(`Connection failed: ${error.message}\nStart the server with: svony server`));
    });
    
    setTimeout(() => {
      ws.close();
      reject(new Error('Request timeout'));
    }, 30000);
  });
}

// ============================================================================
// Main
// ============================================================================

program.parse();
