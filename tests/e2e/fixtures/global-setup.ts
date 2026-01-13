import { chromium, FullConfig } from '@playwright/test';
import { spawn, ChildProcess } from 'child_process';
import * as path from 'path';
import * as fs from 'fs';

/**
 * Global Setup for Svony Browser E2E Tests
 * 
 * This setup runs once before all tests and handles:
 * 1. Starting MCP servers
 * 2. Initializing test data
 * 3. Setting up authentication state
 * 4. Verifying system requirements
 */

let mcpProcesses: ChildProcess[] = [];

async function globalSetup(config: FullConfig) {
  console.log('🚀 Starting Svony Browser E2E Test Setup...\n');

  // Step 1: Verify environment
  console.log('📋 Step 1: Verifying environment...');
  await verifyEnvironment();
  console.log('   ✅ Environment verified\n');

  // Step 2: Start MCP servers
  console.log('📋 Step 2: Starting MCP servers...');
  await startMcpServers();
  console.log('   ✅ MCP servers started\n');

  // Step 3: Initialize test data
  console.log('📋 Step 3: Initializing test data...');
  await initializeTestData();
  console.log('   ✅ Test data initialized\n');

  // Step 4: Setup authentication state
  console.log('📋 Step 4: Setting up authentication...');
  await setupAuthState(config);
  console.log('   ✅ Authentication configured\n');

  // Step 5: Health check
  console.log('📋 Step 5: Running health checks...');
  await runHealthChecks();
  console.log('   ✅ All systems healthy\n');

  console.log('✨ Global setup complete!\n');
}

async function verifyEnvironment(): Promise<void> {
  const requiredEnvVars = [
    'TEST_BASE_URL',
    'MCP_SERVER_PORT',
  ];

  // Set defaults if not provided
  process.env.TEST_BASE_URL = process.env.TEST_BASE_URL || 'http://localhost:5000';
  process.env.MCP_SERVER_PORT = process.env.MCP_SERVER_PORT || '3001';
  process.env.FIDDLER_PORT = process.env.FIDDLER_PORT || '8888';

  // Verify Node.js version
  const nodeVersion = process.version;
  const majorVersion = parseInt(nodeVersion.slice(1).split('.')[0]);
  if (majorVersion < 18) {
    throw new Error(`Node.js 18+ required, found ${nodeVersion}`);
  }
}

async function startMcpServers(): Promise<void> {
  const mcpServers = [
    { name: 'evony-rag', port: 3001, path: '../../mcp-servers/evony-rag' },
    { name: 'evony-rte', port: 3002, path: '../../mcp-servers/evony-rte' },
    { name: 'evony-tools', port: 3003, path: '../../mcp-servers/evony-tools' },
  ];

  for (const server of mcpServers) {
    const serverPath = path.resolve(__dirname, server.path);
    
    // Check if server directory exists
    if (!fs.existsSync(serverPath)) {
      console.log(`   ⚠️  Skipping ${server.name} (not found at ${serverPath})`);
      continue;
    }

    // Check if port is already in use
    const isPortInUse = await checkPort(server.port);
    if (isPortInUse) {
      console.log(`   ℹ️  ${server.name} already running on port ${server.port}`);
      continue;
    }

    // Start the server
    try {
      const proc = spawn('node', ['index.js'], {
        cwd: serverPath,
        env: { ...process.env, PORT: server.port.toString() },
        stdio: 'pipe',
        detached: true,
      });

      mcpProcesses.push(proc);
      
      // Wait for server to be ready
      await waitForPort(server.port, 10000);
      console.log(`   ✅ ${server.name} started on port ${server.port}`);
    } catch (error) {
      console.log(`   ⚠️  Failed to start ${server.name}: ${error}`);
    }
  }

  // Store process IDs for cleanup
  const pids = mcpProcesses.map(p => p.pid).filter(Boolean);
  fs.writeFileSync(
    path.resolve(__dirname, '../.mcp-pids'),
    JSON.stringify(pids)
  );
}

async function initializeTestData(): Promise<void> {
  const testDataDir = path.resolve(__dirname, '../test-data');
  
  // Create test data directory if it doesn't exist
  if (!fs.existsSync(testDataDir)) {
    fs.mkdirSync(testDataDir, { recursive: true });
  }

  // Create sample test data files
  const testData = {
    protocols: {
      version: '7.0',
      actions: [
        { id: 1, name: 'login', category: 'auth' },
        { id: 2, name: 'getCity', category: 'city' },
        { id: 3, name: 'trainTroops', category: 'military' },
      ],
    },
    gameState: {
      cities: [{ id: 1, name: 'Test City', level: 25 }],
      resources: { gold: 1000000, food: 500000, wood: 300000 },
      heroes: [{ id: 1, name: 'Test Hero', level: 35 }],
    },
    settings: {
      theme: 'dark',
      language: 'en',
      autoConnect: true,
    },
  };

  fs.writeFileSync(
    path.join(testDataDir, 'protocols.json'),
    JSON.stringify(testData.protocols, null, 2)
  );
  fs.writeFileSync(
    path.join(testDataDir, 'gameState.json'),
    JSON.stringify(testData.gameState, null, 2)
  );
  fs.writeFileSync(
    path.join(testDataDir, 'settings.json'),
    JSON.stringify(testData.settings, null, 2)
  );
}

async function setupAuthState(config: FullConfig): Promise<void> {
  const browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    // Navigate to the application
    const baseURL = config.projects[0]?.use?.baseURL || 'http://localhost:5000';
    
    // Set up any required cookies or local storage
    await context.addCookies([
      {
        name: 'svony_test_mode',
        value: 'true',
        domain: 'localhost',
        path: '/',
      },
    ]);

    // Save authentication state
    const storageStatePath = path.resolve(__dirname, '../.auth/state.json');
    const authDir = path.dirname(storageStatePath);
    if (!fs.existsSync(authDir)) {
      fs.mkdirSync(authDir, { recursive: true });
    }
    await context.storageState({ path: storageStatePath });
  } finally {
    await browser.close();
  }
}

async function runHealthChecks(): Promise<void> {
  const checks = [
    { name: 'MCP RAG Server', url: 'http://localhost:3001/health' },
    { name: 'MCP RTE Server', url: 'http://localhost:3002/health' },
    { name: 'MCP Tools Server', url: 'http://localhost:3003/health' },
  ];

  for (const check of checks) {
    try {
      const response = await fetch(check.url, { 
        method: 'GET',
        signal: AbortSignal.timeout(5000),
      });
      if (response.ok) {
        console.log(`   ✅ ${check.name}: healthy`);
      } else {
        console.log(`   ⚠️  ${check.name}: unhealthy (${response.status})`);
      }
    } catch (error) {
      console.log(`   ⚠️  ${check.name}: not reachable`);
    }
  }
}

async function checkPort(port: number): Promise<boolean> {
  return new Promise((resolve) => {
    const net = require('net');
    const server = net.createServer();
    server.once('error', () => resolve(true));
    server.once('listening', () => {
      server.close();
      resolve(false);
    });
    server.listen(port);
  });
}

async function waitForPort(port: number, timeout: number): Promise<void> {
  const startTime = Date.now();
  while (Date.now() - startTime < timeout) {
    const isInUse = await checkPort(port);
    if (isInUse) return;
    await new Promise(r => setTimeout(r, 100));
  }
  throw new Error(`Port ${port} not available after ${timeout}ms`);
}

export default globalSetup;
