#!/usr/bin/env node

/**
 * Svony Playwright CDP Bridge
 * 
 * Connects Playwright to CefFlashBrowser via Chrome DevTools Protocol
 * for Flash-based Evony game automation
 */

import { chromium } from 'playwright';
import { spawn } from 'child_process';
import fs from 'fs';
import path from 'path';

// Configuration
const CDP_HOST = process.env.CDP_HOST || 'localhost';
const CDP_PORT = parseInt(process.env.CDP_PORT || '9222');
const CEF_BROWSER_PATH = process.env.CEF_BROWSER_PATH || findCefBrowser();
const DEFAULT_URL = 'https://cc2.evony.com';

/**
 * Find CefFlashBrowser executable
 */
function findCefBrowser() {
  const possiblePaths = [
    'C:\\Program Files\\CefFlashBrowser\\CefFlashBrowser.exe',
    'C:\\Program Files (x86)\\CefFlashBrowser\\CefFlashBrowser.exe',
    path.join(process.env.LOCALAPPDATA || '', 'CefFlashBrowser', 'CefFlashBrowser.exe'),
    path.join(process.cwd(), 'CefFlashBrowser', 'CefFlashBrowser.exe')
  ];
  
  for (const p of possiblePaths) {
    if (fs.existsSync(p)) {
      return p;
    }
  }
  
  return null;
}

/**
 * Launch CefFlashBrowser with CDP enabled
 */
async function launchBrowser(startUrl = DEFAULT_URL, options = {}) {
  const {
    width = 1920,
    height = 1080,
    headless = false
  } = options;
  
  if (!CEF_BROWSER_PATH || !fs.existsSync(CEF_BROWSER_PATH)) {
    throw new Error(`CefFlashBrowser not found at: ${CEF_BROWSER_PATH}`);
  }
  
  const args = [
    `--remote-debugging-port=${CDP_PORT}`,
    '--remote-allow-origins=*',
    '--disable-gpu-sandbox',
    '--enable-logging',
    `--window-size=${width},${height}`
  ];
  
  if (startUrl) {
    args.push(startUrl);
  }
  
  console.log(`Launching CefFlashBrowser: ${CEF_BROWSER_PATH}`);
  console.log(`Arguments: ${args.join(' ')}`);
  
  const browserProcess = spawn(CEF_BROWSER_PATH, args, {
    detached: true,
    stdio: 'ignore'
  });
  
  browserProcess.unref();
  
  // Wait for CDP to be ready
  await waitForCdp();
  
  return browserProcess;
}

/**
 * Wait for CDP endpoint to be ready
 */
async function waitForCdp(maxAttempts = 30) {
  const endpoint = `http://${CDP_HOST}:${CDP_PORT}/json/version`;
  
  for (let i = 0; i < maxAttempts; i++) {
    try {
      const response = await fetch(endpoint);
      if (response.ok) {
        console.log('CDP endpoint ready');
        return true;
      }
    } catch (e) {
      // Not ready yet
    }
    
    await new Promise(resolve => setTimeout(resolve, 500));
  }
  
  throw new Error('CDP endpoint did not become ready');
}

/**
 * Connect Playwright to running CefFlashBrowser
 */
async function connectPlaywright() {
  const cdpEndpoint = `http://${CDP_HOST}:${CDP_PORT}`;
  
  console.log(`Connecting Playwright to CDP: ${cdpEndpoint}`);
  
  const browser = await chromium.connectOverCDP(cdpEndpoint);
  const context = browser.contexts()[0];
  const page = context.pages()[0] || await context.newPage();
  
  return { browser, context, page };
}

/**
 * Evony UI Element Map
 * Pre-defined coordinates for common UI elements
 */
const EvonyUIMap = {
  // Top navigation
  cityView: { x: 100, y: 50 },
  worldMap: { x: 200, y: 50 },
  alliance: { x: 300, y: 50 },
  mail: { x: 400, y: 50 },
  events: { x: 500, y: 50 },
  
  // Bottom menu
  buildMenu: { x: 150, y: 600 },
  research: { x: 250, y: 600 },
  heroes: { x: 350, y: 600 },
  march: { x: 450, y: 600 },
  items: { x: 550, y: 600 },
  
  // Resource bar
  food: { x: 200, y: 20 },
  wood: { x: 300, y: 20 },
  stone: { x: 400, y: 20 },
  iron: { x: 500, y: 20 },
  gold: { x: 600, y: 20 },
  
  // City buildings (approximate)
  castle: { x: 960, y: 400 },
  barracks: { x: 800, y: 500 },
  stables: { x: 1100, y: 500 },
  workshop: { x: 700, y: 400 },
  academy: { x: 1200, y: 400 },
  
  // Dialog buttons
  confirmButton: { x: 960, y: 600 },
  cancelButton: { x: 860, y: 600 },
  closeButton: { x: 1100, y: 200 },
  
  // Upgrade/Train buttons
  upgradeButton: { x: 960, y: 550 },
  trainButton: { x: 960, y: 550 },
  instantButton: { x: 1060, y: 550 }
};

/**
 * Visual Automation Helper
 */
class VisualAutomation {
  constructor(page) {
    this.page = page;
    this.viewport = null;
  }
  
  async initialize() {
    this.viewport = await this.page.viewportSize();
    console.log(`Viewport: ${this.viewport.width}x${this.viewport.height}`);
  }
  
  /**
   * Click at absolute coordinates
   */
  async clickAt(x, y, options = {}) {
    const { delay = 100, button = 'left' } = options;
    
    console.log(`Clicking at (${x}, ${y})`);
    await this.page.mouse.click(x, y, { button, delay });
  }
  
  /**
   * Click at relative coordinates (percentage)
   */
  async clickRelative(xPercent, yPercent, options = {}) {
    const x = this.viewport.width * xPercent;
    const y = this.viewport.height * yPercent;
    
    await this.clickAt(x, y, options);
  }
  
  /**
   * Click a named UI element
   */
  async clickElement(elementName, options = {}) {
    const element = EvonyUIMap[elementName];
    
    if (!element) {
      throw new Error(`Unknown UI element: ${elementName}`);
    }
    
    await this.clickAt(element.x, element.y, options);
  }
  
  /**
   * Double click at coordinates
   */
  async doubleClickAt(x, y) {
    console.log(`Double-clicking at (${x}, ${y})`);
    await this.page.mouse.dblclick(x, y);
  }
  
  /**
   * Right click at coordinates
   */
  async rightClickAt(x, y) {
    console.log(`Right-clicking at (${x}, ${y})`);
    await this.page.mouse.click(x, y, { button: 'right' });
  }
  
  /**
   * Drag from one point to another
   */
  async drag(fromX, fromY, toX, toY, options = {}) {
    const { steps = 10 } = options;
    
    console.log(`Dragging from (${fromX}, ${fromY}) to (${toX}, ${toY})`);
    
    await this.page.mouse.move(fromX, fromY);
    await this.page.mouse.down();
    
    for (let i = 1; i <= steps; i++) {
      const progress = i / steps;
      const x = fromX + (toX - fromX) * progress;
      const y = fromY + (toY - fromY) * progress;
      
      await this.page.mouse.move(x, y);
      await new Promise(resolve => setTimeout(resolve, 20));
    }
    
    await this.page.mouse.up();
  }
  
  /**
   * Scroll at coordinates
   */
  async scroll(x, y, deltaX = 0, deltaY = 0) {
    console.log(`Scrolling at (${x}, ${y}) by (${deltaX}, ${deltaY})`);
    await this.page.mouse.wheel(deltaX, deltaY);
  }
  
  /**
   * Type text
   */
  async type(text, options = {}) {
    const { delay = 50 } = options;
    
    console.log(`Typing: ${text}`);
    await this.page.keyboard.type(text, { delay });
  }
  
  /**
   * Press key
   */
  async pressKey(key) {
    console.log(`Pressing key: ${key}`);
    await this.page.keyboard.press(key);
  }
  
  /**
   * Take screenshot
   */
  async screenshot(options = {}) {
    const { path: filePath, fullPage = true } = options;
    
    console.log(`Taking screenshot${filePath ? ` to ${filePath}` : ''}`);
    return await this.page.screenshot({ path: filePath, fullPage });
  }
  
  /**
   * Wait for a duration
   */
  async wait(ms) {
    console.log(`Waiting ${ms}ms`);
    await new Promise(resolve => setTimeout(resolve, ms));
  }
}

/**
 * Evony Game Automation
 */
class EvonyAutomation extends VisualAutomation {
  constructor(page) {
    super(page);
  }
  
  /**
   * Navigate to city view
   */
  async goToCity() {
    await this.clickElement('cityView');
    await this.wait(1000);
  }
  
  /**
   * Navigate to world map
   */
  async goToWorldMap() {
    await this.clickElement('worldMap');
    await this.wait(1000);
  }
  
  /**
   * Open build menu
   */
  async openBuildMenu() {
    await this.clickElement('buildMenu');
    await this.wait(500);
  }
  
  /**
   * Open research menu
   */
  async openResearch() {
    await this.clickElement('research');
    await this.wait(500);
  }
  
  /**
   * Open heroes menu
   */
  async openHeroes() {
    await this.clickElement('heroes');
    await this.wait(500);
  }
  
  /**
   * Click on a building
   */
  async clickBuilding(buildingName) {
    const building = EvonyUIMap[buildingName];
    
    if (building) {
      await this.clickAt(building.x, building.y);
      await this.wait(500);
    } else {
      console.warn(`Building not in UI map: ${buildingName}`);
    }
  }
  
  /**
   * Click upgrade button
   */
  async clickUpgrade() {
    await this.clickElement('upgradeButton');
    await this.wait(500);
  }
  
  /**
   * Click train button
   */
  async clickTrain() {
    await this.clickElement('trainButton');
    await this.wait(500);
  }
  
  /**
   * Click confirm button
   */
  async clickConfirm() {
    await this.clickElement('confirmButton');
    await this.wait(500);
  }
  
  /**
   * Click cancel button
   */
  async clickCancel() {
    await this.clickElement('cancelButton');
    await this.wait(500);
  }
  
  /**
   * Close current dialog
   */
  async closeDialog() {
    await this.clickElement('closeButton');
    await this.wait(300);
  }
  
  /**
   * Upgrade a building
   */
  async upgradeBuilding(buildingName) {
    console.log(`Upgrading building: ${buildingName}`);
    
    await this.goToCity();
    await this.clickBuilding(buildingName);
    await this.clickUpgrade();
    await this.clickConfirm();
    
    console.log('Upgrade initiated');
  }
  
  /**
   * Train troops
   */
  async trainTroops(buildingName, quantity = 1) {
    console.log(`Training troops at: ${buildingName}`);
    
    await this.goToCity();
    await this.clickBuilding(buildingName);
    await this.clickTrain();
    
    // Type quantity
    await this.type(quantity.toString());
    await this.clickConfirm();
    
    console.log('Training initiated');
  }
  
  /**
   * Pan world map
   */
  async panMap(direction, distance = 200) {
    const center = {
      x: this.viewport.width / 2,
      y: this.viewport.height / 2
    };
    
    let toX = center.x;
    let toY = center.y;
    
    switch (direction) {
      case 'up':
        toY = center.y + distance;
        break;
      case 'down':
        toY = center.y - distance;
        break;
      case 'left':
        toX = center.x + distance;
        break;
      case 'right':
        toX = center.x - distance;
        break;
    }
    
    await this.drag(center.x, center.y, toX, toY);
    await this.wait(500);
  }
  
  /**
   * Click on map coordinates
   */
  async clickMapTile(mapX, mapY) {
    // This requires knowing the map-to-screen coordinate conversion
    // For now, use relative positioning
    console.log(`Clicking map tile: (${mapX}, ${mapY})`);
    
    // Placeholder - actual implementation needs map coordinate system
    const screenX = this.viewport.width / 2;
    const screenY = this.viewport.height / 2;
    
    await this.clickAt(screenX, screenY);
  }
}

/**
 * Main entry point
 */
async function main() {
  const args = process.argv.slice(2);
  const command = args[0] || 'connect';
  
  switch (command) {
    case 'launch':
      await launchBrowser(args[1] || DEFAULT_URL);
      console.log('Browser launched. Use "connect" to attach Playwright.');
      break;
      
    case 'connect':
      const { browser, page } = await connectPlaywright();
      const automation = new EvonyAutomation(page);
      await automation.initialize();
      
      console.log('Connected to CefFlashBrowser');
      console.log('Automation ready. Use the exported classes for scripting.');
      
      // Keep process running
      process.on('SIGINT', async () => {
        console.log('Disconnecting...');
        await browser.close();
        process.exit(0);
      });
      break;
      
    case 'test':
      await testAutomation();
      break;
      
    default:
      console.log(`
Svony Playwright CDP Bridge

Usage:
  node index.js launch [url]    - Launch CefFlashBrowser with CDP
  node index.js connect         - Connect Playwright to running browser
  node index.js test            - Run test automation

Environment Variables:
  CDP_HOST          - CDP host (default: localhost)
  CDP_PORT          - CDP port (default: 9222)
  CEF_BROWSER_PATH  - Path to CefFlashBrowser.exe
      `);
  }
}

/**
 * Test automation
 */
async function testAutomation() {
  console.log('Running test automation...');
  
  const { browser, page } = await connectPlaywright();
  const automation = new EvonyAutomation(page);
  await automation.initialize();
  
  // Take screenshot
  await automation.screenshot({ path: 'test-screenshot.png' });
  
  // Test clicking
  await automation.goToCity();
  await automation.wait(2000);
  
  await automation.goToWorldMap();
  await automation.wait(2000);
  
  console.log('Test complete');
  await browser.close();
}

// Export for use as module
export {
  launchBrowser,
  waitForCdp,
  connectPlaywright,
  VisualAutomation,
  EvonyAutomation,
  EvonyUIMap
};

// Run if called directly
main().catch(console.error);
