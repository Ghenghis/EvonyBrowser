#!/usr/bin/env node

/**
 * Evony CDP MCP Server
 * 
 * Provides MCP tools for controlling CefFlashBrowser via Chrome DevTools Protocol
 * Enables Claude Desktop, Windsurf IDE, and LM Studio to automate Flash-based Evony
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
import { chromium } from 'playwright';
import { spawn } from 'child_process';
import fs from 'fs';
import path from 'path';

// Configuration
const CDP_HOST = process.env.CDP_HOST || 'localhost';
const CDP_PORT = parseInt(process.env.CDP_PORT || '9222');
const CEF_BROWSER_PATH = process.env.CEF_BROWSER_PATH || null;

// State
let browser = null;
let page = null;
let isConnected = false;
let viewport = { width: 1920, height: 1080 };
let screenshots = [];
let actionLog = [];

// Evony UI Element Map
const EvonyUIMap = {
  // Navigation
  cityView: { x: 100, y: 50, desc: 'City view button' },
  worldMap: { x: 200, y: 50, desc: 'World map button' },
  alliance: { x: 300, y: 50, desc: 'Alliance button' },
  mail: { x: 400, y: 50, desc: 'Mail button' },
  events: { x: 500, y: 50, desc: 'Events button' },
  
  // Bottom menu
  buildMenu: { x: 150, y: 600, desc: 'Build menu' },
  research: { x: 250, y: 600, desc: 'Research menu' },
  heroes: { x: 350, y: 600, desc: 'Heroes menu' },
  march: { x: 450, y: 600, desc: 'March panel' },
  items: { x: 550, y: 600, desc: 'Items menu' },
  
  // Buildings
  castle: { x: 960, y: 400, desc: 'Main castle' },
  barracks: { x: 800, y: 500, desc: 'Barracks' },
  stables: { x: 1100, y: 500, desc: 'Stables' },
  workshop: { x: 700, y: 400, desc: 'Workshop' },
  academy: { x: 1200, y: 400, desc: 'Academy' },
  
  // Dialog buttons
  confirmButton: { x: 960, y: 600, desc: 'Confirm button' },
  cancelButton: { x: 860, y: 600, desc: 'Cancel button' },
  closeButton: { x: 1100, y: 200, desc: 'Close button' },
  upgradeButton: { x: 960, y: 550, desc: 'Upgrade button' },
  trainButton: { x: 960, y: 550, desc: 'Train button' }
};

// Create MCP server
const server = new Server(
  { name: 'evony-cdp', version: '1.0.0' },
  { capabilities: { tools: {}, resources: {}, prompts: {} } }
);

// ============================================================================
// TOOLS
// ============================================================================

const tools = [
  // Connection Tools
  {
    name: 'cdp_launch',
    description: 'Launch CefFlashBrowser with CDP enabled',
    inputSchema: {
      type: 'object',
      properties: {
        url: { type: 'string', description: 'URL to open (default: https://cc2.evony.com)' },
        width: { type: 'number', description: 'Window width (default: 1920)' },
        height: { type: 'number', description: 'Window height (default: 1080)' }
      }
    }
  },
  {
    name: 'cdp_connect',
    description: 'Connect to running CefFlashBrowser via CDP',
    inputSchema: {
      type: 'object',
      properties: {
        host: { type: 'string', description: 'CDP host (default: localhost)' },
        port: { type: 'number', description: 'CDP port (default: 9222)' }
      }
    }
  },
  {
    name: 'cdp_disconnect',
    description: 'Disconnect from CefFlashBrowser',
    inputSchema: { type: 'object', properties: {} }
  },
  {
    name: 'cdp_status',
    description: 'Get CDP connection status',
    inputSchema: { type: 'object', properties: {} }
  },
  
  // Navigation Tools
  {
    name: 'cdp_navigate',
    description: 'Navigate to URL',
    inputSchema: {
      type: 'object',
      properties: {
        url: { type: 'string', description: 'URL to navigate to' }
      },
      required: ['url']
    }
  },
  {
    name: 'cdp_reload',
    description: 'Reload current page',
    inputSchema: {
      type: 'object',
      properties: {
        ignoreCache: { type: 'boolean', description: 'Ignore cache (default: false)' }
      }
    }
  },
  {
    name: 'cdp_back',
    description: 'Go back in history',
    inputSchema: { type: 'object', properties: {} }
  },
  {
    name: 'cdp_forward',
    description: 'Go forward in history',
    inputSchema: { type: 'object', properties: {} }
  },
  {
    name: 'cdp_get_url',
    description: 'Get current URL and title',
    inputSchema: { type: 'object', properties: {} }
  },
  
  // Click Tools
  {
    name: 'cdp_click',
    description: 'Click at coordinates',
    inputSchema: {
      type: 'object',
      properties: {
        x: { type: 'number', description: 'X coordinate' },
        y: { type: 'number', description: 'Y coordinate' },
        button: { type: 'string', enum: ['left', 'right', 'middle'], description: 'Mouse button' }
      },
      required: ['x', 'y']
    }
  },
  {
    name: 'cdp_double_click',
    description: 'Double-click at coordinates',
    inputSchema: {
      type: 'object',
      properties: {
        x: { type: 'number', description: 'X coordinate' },
        y: { type: 'number', description: 'Y coordinate' }
      },
      required: ['x', 'y']
    }
  },
  {
    name: 'cdp_click_element',
    description: 'Click a named Evony UI element',
    inputSchema: {
      type: 'object',
      properties: {
        element: { type: 'string', description: 'Element name (e.g., cityView, worldMap, castle)' }
      },
      required: ['element']
    }
  },
  {
    name: 'cdp_click_relative',
    description: 'Click at relative position (percentage)',
    inputSchema: {
      type: 'object',
      properties: {
        xPercent: { type: 'number', description: 'X position as percentage (0-1)' },
        yPercent: { type: 'number', description: 'Y position as percentage (0-1)' }
      },
      required: ['xPercent', 'yPercent']
    }
  },
  
  // Drag & Scroll Tools
  {
    name: 'cdp_drag',
    description: 'Drag from one point to another',
    inputSchema: {
      type: 'object',
      properties: {
        fromX: { type: 'number', description: 'Start X coordinate' },
        fromY: { type: 'number', description: 'Start Y coordinate' },
        toX: { type: 'number', description: 'End X coordinate' },
        toY: { type: 'number', description: 'End Y coordinate' },
        steps: { type: 'number', description: 'Number of steps (default: 10)' }
      },
      required: ['fromX', 'fromY', 'toX', 'toY']
    }
  },
  {
    name: 'cdp_scroll',
    description: 'Scroll at coordinates',
    inputSchema: {
      type: 'object',
      properties: {
        x: { type: 'number', description: 'X coordinate' },
        y: { type: 'number', description: 'Y coordinate' },
        deltaX: { type: 'number', description: 'Horizontal scroll amount' },
        deltaY: { type: 'number', description: 'Vertical scroll amount' }
      },
      required: ['x', 'y']
    }
  },
  {
    name: 'cdp_pan_map',
    description: 'Pan the world map in a direction',
    inputSchema: {
      type: 'object',
      properties: {
        direction: { type: 'string', enum: ['up', 'down', 'left', 'right'], description: 'Pan direction' },
        distance: { type: 'number', description: 'Pan distance in pixels (default: 200)' }
      },
      required: ['direction']
    }
  },
  
  // Keyboard Tools
  {
    name: 'cdp_type',
    description: 'Type text',
    inputSchema: {
      type: 'object',
      properties: {
        text: { type: 'string', description: 'Text to type' },
        delay: { type: 'number', description: 'Delay between keystrokes in ms (default: 50)' }
      },
      required: ['text']
    }
  },
  {
    name: 'cdp_press_key',
    description: 'Press a key',
    inputSchema: {
      type: 'object',
      properties: {
        key: { type: 'string', description: 'Key to press (e.g., Enter, Escape, Tab)' },
        ctrl: { type: 'boolean', description: 'Hold Ctrl' },
        alt: { type: 'boolean', description: 'Hold Alt' },
        shift: { type: 'boolean', description: 'Hold Shift' }
      },
      required: ['key']
    }
  },
  
  // Screenshot Tools
  {
    name: 'cdp_screenshot',
    description: 'Take a screenshot',
    inputSchema: {
      type: 'object',
      properties: {
        format: { type: 'string', enum: ['png', 'jpeg'], description: 'Image format' },
        quality: { type: 'number', description: 'JPEG quality (0-100)' },
        fullPage: { type: 'boolean', description: 'Capture full page' }
      }
    }
  },
  {
    name: 'cdp_screenshot_region',
    description: 'Take a screenshot of a region',
    inputSchema: {
      type: 'object',
      properties: {
        x: { type: 'number', description: 'X coordinate' },
        y: { type: 'number', description: 'Y coordinate' },
        width: { type: 'number', description: 'Width' },
        height: { type: 'number', description: 'Height' }
      },
      required: ['x', 'y', 'width', 'height']
    }
  },
  
  // Game Action Tools
  {
    name: 'evony_go_to_city',
    description: 'Navigate to city view',
    inputSchema: { type: 'object', properties: {} }
  },
  {
    name: 'evony_go_to_world',
    description: 'Navigate to world map',
    inputSchema: { type: 'object', properties: {} }
  },
  {
    name: 'evony_click_building',
    description: 'Click on a building',
    inputSchema: {
      type: 'object',
      properties: {
        building: { type: 'string', description: 'Building name (castle, barracks, stables, etc.)' }
      },
      required: ['building']
    }
  },
  {
    name: 'evony_upgrade_building',
    description: 'Upgrade a building',
    inputSchema: {
      type: 'object',
      properties: {
        building: { type: 'string', description: 'Building name' }
      },
      required: ['building']
    }
  },
  {
    name: 'evony_train_troops',
    description: 'Train troops',
    inputSchema: {
      type: 'object',
      properties: {
        troopType: { type: 'string', enum: ['infantry', 'cavalry', 'archer', 'siege'], description: 'Troop type' },
        quantity: { type: 'number', description: 'Number of troops to train' }
      },
      required: ['troopType', 'quantity']
    }
  },
  {
    name: 'evony_close_dialog',
    description: 'Close current dialog',
    inputSchema: { type: 'object', properties: {} }
  },
  {
    name: 'evony_confirm',
    description: 'Click confirm button',
    inputSchema: { type: 'object', properties: {} }
  },
  {
    name: 'evony_cancel',
    description: 'Click cancel button',
    inputSchema: { type: 'object', properties: {} }
  },
  
  // UI Map Tools
  {
    name: 'cdp_list_elements',
    description: 'List all known UI elements',
    inputSchema: { type: 'object', properties: {} }
  },
  {
    name: 'cdp_get_element',
    description: 'Get UI element coordinates',
    inputSchema: {
      type: 'object',
      properties: {
        element: { type: 'string', description: 'Element name' }
      },
      required: ['element']
    }
  },
  {
    name: 'cdp_add_element',
    description: 'Add custom UI element',
    inputSchema: {
      type: 'object',
      properties: {
        name: { type: 'string', description: 'Element name' },
        x: { type: 'number', description: 'X coordinate' },
        y: { type: 'number', description: 'Y coordinate' },
        description: { type: 'string', description: 'Element description' }
      },
      required: ['name', 'x', 'y']
    }
  },
  
  // Utility Tools
  {
    name: 'cdp_wait',
    description: 'Wait for specified duration',
    inputSchema: {
      type: 'object',
      properties: {
        ms: { type: 'number', description: 'Duration in milliseconds' }
      },
      required: ['ms']
    }
  },
  {
    name: 'cdp_execute_script',
    description: 'Execute JavaScript in the page',
    inputSchema: {
      type: 'object',
      properties: {
        script: { type: 'string', description: 'JavaScript code to execute' }
      },
      required: ['script']
    }
  },
  {
    name: 'cdp_get_viewport',
    description: 'Get viewport size',
    inputSchema: { type: 'object', properties: {} }
  },
  {
    name: 'cdp_get_action_log',
    description: 'Get recent action log',
    inputSchema: {
      type: 'object',
      properties: {
        limit: { type: 'number', description: 'Number of actions to return' }
      }
    }
  }
];

// Register tools handler
server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools
}));

// Tool execution handler
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;
  
  try {
    const result = await executeTool(name, args || {});
    logAction(name, args, result);
    
    return {
      content: [{ type: 'text', text: JSON.stringify(result, null, 2) }]
    };
  } catch (error) {
    logAction(name, args, { error: error.message });
    
    return {
      content: [{ type: 'text', text: `Error: ${error.message}` }],
      isError: true
    };
  }
});

// Tool execution logic
async function executeTool(name, args) {
  switch (name) {
    // Connection
    case 'cdp_launch':
      return await launchBrowser(args.url, args.width, args.height);
    case 'cdp_connect':
      return await connectBrowser(args.host, args.port);
    case 'cdp_disconnect':
      return await disconnectBrowser();
    case 'cdp_status':
      return getStatus();
    
    // Navigation
    case 'cdp_navigate':
      requireConnection();
      await page.goto(args.url);
      return { success: true, url: args.url };
    case 'cdp_reload':
      requireConnection();
      await page.reload({ waitUntil: args.ignoreCache ? 'networkidle' : 'load' });
      return { success: true };
    case 'cdp_back':
      requireConnection();
      await page.goBack();
      return { success: true };
    case 'cdp_forward':
      requireConnection();
      await page.goForward();
      return { success: true };
    case 'cdp_get_url':
      requireConnection();
      return { url: page.url(), title: await page.title() };
    
    // Click
    case 'cdp_click':
      requireConnection();
      await page.mouse.click(args.x, args.y, { button: args.button || 'left' });
      return { success: true, x: args.x, y: args.y };
    case 'cdp_double_click':
      requireConnection();
      await page.mouse.dblclick(args.x, args.y);
      return { success: true, x: args.x, y: args.y };
    case 'cdp_click_element':
      requireConnection();
      const element = EvonyUIMap[args.element];
      if (!element) throw new Error(`Unknown element: ${args.element}`);
      const scaledX = scaleX(element.x);
      const scaledY = scaleY(element.y);
      await page.mouse.click(scaledX, scaledY);
      return { success: true, element: args.element, x: scaledX, y: scaledY };
    case 'cdp_click_relative':
      requireConnection();
      const relX = viewport.width * args.xPercent;
      const relY = viewport.height * args.yPercent;
      await page.mouse.click(relX, relY);
      return { success: true, x: relX, y: relY };
    
    // Drag & Scroll
    case 'cdp_drag':
      requireConnection();
      await page.mouse.move(args.fromX, args.fromY);
      await page.mouse.down();
      const steps = args.steps || 10;
      for (let i = 1; i <= steps; i++) {
        const progress = i / steps;
        const x = args.fromX + (args.toX - args.fromX) * progress;
        const y = args.fromY + (args.toY - args.fromY) * progress;
        await page.mouse.move(x, y);
        await new Promise(r => setTimeout(r, 20));
      }
      await page.mouse.up();
      return { success: true };
    case 'cdp_scroll':
      requireConnection();
      await page.mouse.move(args.x, args.y);
      await page.mouse.wheel(args.deltaX || 0, args.deltaY || 0);
      return { success: true };
    case 'cdp_pan_map':
      requireConnection();
      const centerX = viewport.width / 2;
      const centerY = viewport.height / 2;
      const dist = args.distance || 200;
      let toX = centerX, toY = centerY;
      switch (args.direction) {
        case 'up': toY = centerY + dist; break;
        case 'down': toY = centerY - dist; break;
        case 'left': toX = centerX + dist; break;
        case 'right': toX = centerX - dist; break;
      }
      await executeTool('cdp_drag', { fromX: centerX, fromY: centerY, toX, toY });
      return { success: true, direction: args.direction };
    
    // Keyboard
    case 'cdp_type':
      requireConnection();
      await page.keyboard.type(args.text, { delay: args.delay || 50 });
      return { success: true, text: args.text };
    case 'cdp_press_key':
      requireConnection();
      const modifiers = [];
      if (args.ctrl) modifiers.push('Control');
      if (args.alt) modifiers.push('Alt');
      if (args.shift) modifiers.push('Shift');
      const keyCombo = [...modifiers, args.key].join('+');
      await page.keyboard.press(keyCombo);
      return { success: true, key: keyCombo };
    
    // Screenshot
    case 'cdp_screenshot':
      requireConnection();
      const screenshot = await page.screenshot({
        type: args.format || 'png',
        quality: args.format === 'jpeg' ? (args.quality || 80) : undefined,
        fullPage: args.fullPage || false
      });
      const screenshotId = Date.now().toString();
      screenshots.push({ id: screenshotId, data: screenshot.toString('base64'), timestamp: new Date() });
      if (screenshots.length > 10) screenshots.shift();
      return { success: true, screenshotId, size: screenshot.length };
    case 'cdp_screenshot_region':
      requireConnection();
      const regionShot = await page.screenshot({
        clip: { x: args.x, y: args.y, width: args.width, height: args.height }
      });
      const regionId = Date.now().toString();
      screenshots.push({ id: regionId, data: regionShot.toString('base64'), timestamp: new Date() });
      return { success: true, screenshotId: regionId, size: regionShot.length };
    
    // Game Actions
    case 'evony_go_to_city':
      return await executeTool('cdp_click_element', { element: 'cityView' });
    case 'evony_go_to_world':
      return await executeTool('cdp_click_element', { element: 'worldMap' });
    case 'evony_click_building':
      return await executeTool('cdp_click_element', { element: args.building });
    case 'evony_upgrade_building':
      await executeTool('evony_go_to_city', {});
      await new Promise(r => setTimeout(r, 1000));
      await executeTool('evony_click_building', { building: args.building });
      await new Promise(r => setTimeout(r, 500));
      await executeTool('cdp_click_element', { element: 'upgradeButton' });
      await new Promise(r => setTimeout(r, 500));
      await executeTool('cdp_click_element', { element: 'confirmButton' });
      return { success: true, building: args.building };
    case 'evony_train_troops':
      await executeTool('evony_go_to_city', {});
      await new Promise(r => setTimeout(r, 1000));
      const buildingMap = { infantry: 'barracks', cavalry: 'stables', siege: 'workshop' };
      await executeTool('evony_click_building', { building: buildingMap[args.troopType] || 'barracks' });
      await new Promise(r => setTimeout(r, 500));
      await executeTool('cdp_click_element', { element: 'trainButton' });
      await new Promise(r => setTimeout(r, 500));
      await executeTool('cdp_type', { text: args.quantity.toString() });
      await executeTool('cdp_click_element', { element: 'confirmButton' });
      return { success: true, troopType: args.troopType, quantity: args.quantity };
    case 'evony_close_dialog':
      return await executeTool('cdp_click_element', { element: 'closeButton' });
    case 'evony_confirm':
      return await executeTool('cdp_click_element', { element: 'confirmButton' });
    case 'evony_cancel':
      return await executeTool('cdp_click_element', { element: 'cancelButton' });
    
    // UI Map
    case 'cdp_list_elements':
      return { elements: Object.entries(EvonyUIMap).map(([name, data]) => ({ name, ...data })) };
    case 'cdp_get_element':
      const el = EvonyUIMap[args.element];
      if (!el) throw new Error(`Unknown element: ${args.element}`);
      return { name: args.element, ...el, scaledX: scaleX(el.x), scaledY: scaleY(el.y) };
    case 'cdp_add_element':
      EvonyUIMap[args.name] = { x: args.x, y: args.y, desc: args.description || '' };
      return { success: true, element: args.name };
    
    // Utility
    case 'cdp_wait':
      await new Promise(r => setTimeout(r, args.ms));
      return { success: true, waited: args.ms };
    case 'cdp_execute_script':
      requireConnection();
      const scriptResult = await page.evaluate(args.script);
      return { success: true, result: scriptResult };
    case 'cdp_get_viewport':
      return { viewport };
    case 'cdp_get_action_log':
      const limit = args.limit || 20;
      return { actions: actionLog.slice(-limit) };
    
    default:
      throw new Error(`Unknown tool: ${name}`);
  }
}

// Helper functions
async function launchBrowser(url, width, height) {
  if (!CEF_BROWSER_PATH || !fs.existsSync(CEF_BROWSER_PATH)) {
    throw new Error('CefFlashBrowser not found. Set CEF_BROWSER_PATH environment variable.');
  }
  
  const args = [
    `--remote-debugging-port=${CDP_PORT}`,
    '--remote-allow-origins=*',
    '--disable-gpu-sandbox',
    `--window-size=${width || 1920},${height || 1080}`
  ];
  
  if (url) args.push(url);
  
  spawn(CEF_BROWSER_PATH, args, { detached: true, stdio: 'ignore' }).unref();
  
  // Wait for CDP
  await waitForCdp();
  
  return await connectBrowser();
}

async function waitForCdp(maxAttempts = 30) {
  for (let i = 0; i < maxAttempts; i++) {
    try {
      const response = await fetch(`http://${CDP_HOST}:${CDP_PORT}/json/version`);
      if (response.ok) return true;
    } catch (e) {}
    await new Promise(r => setTimeout(r, 500));
  }
  throw new Error('CDP endpoint did not become ready');
}

async function connectBrowser(host, port) {
  const cdpHost = host || CDP_HOST;
  const cdpPort = port || CDP_PORT;
  
  browser = await chromium.connectOverCDP(`http://${cdpHost}:${cdpPort}`);
  const context = browser.contexts()[0];
  page = context.pages()[0] || await context.newPage();
  
  const vp = page.viewportSize();
  if (vp) {
    viewport = vp;
  }
  
  isConnected = true;
  
  return { success: true, endpoint: `http://${cdpHost}:${cdpPort}`, viewport };
}

async function disconnectBrowser() {
  if (browser) {
    await browser.close();
    browser = null;
    page = null;
  }
  isConnected = false;
  return { success: true };
}

function getStatus() {
  return {
    isConnected,
    viewport,
    endpoint: `http://${CDP_HOST}:${CDP_PORT}`,
    screenshotCount: screenshots.length,
    actionCount: actionLog.length
  };
}

function requireConnection() {
  if (!isConnected || !page) {
    throw new Error('Not connected to browser. Use cdp_connect first.');
  }
}

function scaleX(x) {
  return Math.round(x * (viewport.width / 1920));
}

function scaleY(y) {
  return Math.round(y * (viewport.height / 1080));
}

function logAction(tool, args, result) {
  actionLog.push({
    timestamp: new Date().toISOString(),
    tool,
    args,
    success: !result.error,
    error: result.error
  });
  
  if (actionLog.length > 100) {
    actionLog.shift();
  }
}

// ============================================================================
// RESOURCES
// ============================================================================

server.setRequestHandler(ListResourcesRequestSchema, async () => ({
  resources: [
    {
      uri: 'evony://cdp/status',
      name: 'CDP Connection Status',
      description: 'Current CDP connection status',
      mimeType: 'application/json'
    },
    {
      uri: 'evony://cdp/elements',
      name: 'UI Element Map',
      description: 'Known Evony UI elements and coordinates',
      mimeType: 'application/json'
    },
    {
      uri: 'evony://cdp/screenshots',
      name: 'Recent Screenshots',
      description: 'List of recent screenshots',
      mimeType: 'application/json'
    },
    {
      uri: 'evony://cdp/actions',
      name: 'Action Log',
      description: 'Recent automation actions',
      mimeType: 'application/json'
    }
  ]
}));

server.setRequestHandler(ReadResourceRequestSchema, async (request) => {
  const { uri } = request.params;
  
  let content;
  
  switch (uri) {
    case 'evony://cdp/status':
      content = JSON.stringify(getStatus(), null, 2);
      break;
    case 'evony://cdp/elements':
      content = JSON.stringify(EvonyUIMap, null, 2);
      break;
    case 'evony://cdp/screenshots':
      content = JSON.stringify(screenshots.map(s => ({ id: s.id, timestamp: s.timestamp, size: s.data.length })), null, 2);
      break;
    case 'evony://cdp/actions':
      content = JSON.stringify(actionLog.slice(-50), null, 2);
      break;
    default:
      throw new Error(`Unknown resource: ${uri}`);
  }
  
  return {
    contents: [{ uri, mimeType: 'application/json', text: content }]
  };
});

// ============================================================================
// PROMPTS
// ============================================================================

server.setRequestHandler(ListPromptsRequestSchema, async () => ({
  prompts: [
    {
      name: 'automate_building',
      description: 'Generate automation script for building upgrades',
      arguments: [
        { name: 'building', description: 'Building name', required: true },
        { name: 'targetLevel', description: 'Target level', required: false }
      ]
    },
    {
      name: 'automate_training',
      description: 'Generate automation script for troop training',
      arguments: [
        { name: 'troopType', description: 'Troop type', required: true },
        { name: 'quantity', description: 'Number of troops', required: true }
      ]
    },
    {
      name: 'find_coordinates',
      description: 'Help find coordinates for a UI element',
      arguments: [
        { name: 'elementDescription', description: 'Description of the element', required: true }
      ]
    }
  ]
}));

server.setRequestHandler(GetPromptRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;
  
  let messages;
  
  switch (name) {
    case 'automate_building':
      messages = [{
        role: 'user',
        content: {
          type: 'text',
          text: `Create an automation script to upgrade the ${args.building} building${args.targetLevel ? ` to level ${args.targetLevel}` : ''}.

Use these tools in sequence:
1. evony_go_to_city - Navigate to city view
2. evony_click_building - Click on the ${args.building}
3. cdp_click_element - Click upgrade button
4. evony_confirm - Confirm the upgrade

Remember that Flash content requires coordinate-based clicking. The UI element map has pre-defined coordinates for common elements.`
        }
      }];
      break;
    
    case 'automate_training':
      messages = [{
        role: 'user',
        content: {
          type: 'text',
          text: `Create an automation script to train ${args.quantity} ${args.troopType} troops.

Use the evony_train_troops tool with:
- troopType: "${args.troopType}"
- quantity: ${args.quantity}

This will automatically:
1. Navigate to city view
2. Click the appropriate building (barracks for infantry, stables for cavalry, workshop for siege)
3. Click train button
4. Enter quantity
5. Confirm training`
        }
      }];
      break;
    
    case 'find_coordinates':
      messages = [{
        role: 'user',
        content: {
          type: 'text',
          text: `Help me find the coordinates for: ${args.elementDescription}

Steps:
1. Use cdp_screenshot to take a screenshot
2. Analyze the screenshot to estimate coordinates
3. Use cdp_click_relative with percentage values to test
4. Once found, use cdp_add_element to save the coordinates

Current viewport: ${viewport.width}x${viewport.height}
Base resolution: 1920x1080

Coordinates are automatically scaled based on viewport size.`
        }
      }];
      break;
    
    default:
      throw new Error(`Unknown prompt: ${name}`);
  }
  
  return { messages };
});

// ============================================================================
// MAIN
// ============================================================================

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error('Evony CDP MCP Server running');
}

main().catch(console.error);
