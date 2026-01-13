import { defineConfig, devices } from '@playwright/test';

/**
 * Svony Browser v7.0 - Playwright Configuration
 * 
 * This configuration provides comprehensive E2E testing for the Svony Browser
 * application, including browser automation, MCP server testing, and UI validation.
 */

export default defineConfig({
  // Test directory
  testDir: './specs',
  
  // Test file patterns
  testMatch: '**/*.spec.ts',
  
  // Parallel execution
  fullyParallel: true,
  
  // Fail the build on CI if you accidentally left test.only in the source code
  forbidOnly: !!process.env.CI,
  
  // Retry on CI only
  retries: process.env.CI ? 2 : 0,
  
  // Opt out of parallel tests on CI
  workers: process.env.CI ? 1 : undefined,
  
  // Reporter configuration
  reporter: [
    ['html', { outputFolder: 'reports/html', open: 'never' }],
    ['junit', { outputFile: 'reports/junit/results.xml' }],
    ['json', { outputFile: 'reports/json/results.json' }],
    ['list']
  ],
  
  // Shared settings for all projects
  use: {
    // Base URL for the application
    baseURL: 'http://localhost:5000',
    
    // Collect trace when retrying the failed test
    trace: 'on-first-retry',
    
    // Capture screenshot on failure
    screenshot: 'only-on-failure',
    
    // Video recording
    video: 'retain-on-failure',
    
    // Viewport size
    viewport: { width: 1920, height: 1080 },
    
    // Action timeout
    actionTimeout: 10000,
    
    // Navigation timeout
    navigationTimeout: 30000,
  },

  // Configure projects for major browsers
  projects: [
    // Setup project - runs before all tests
    {
      name: 'setup',
      testMatch: /global-setup\.ts/,
    },
    
    // Desktop Chrome
    {
      name: 'chromium',
      use: { 
        ...devices['Desktop Chrome'],
        channel: 'chrome',
      },
      dependencies: ['setup'],
    },
    
    // Desktop Edge
    {
      name: 'edge',
      use: { 
        ...devices['Desktop Edge'],
        channel: 'msedge',
      },
      dependencies: ['setup'],
    },
    
    // MCP Server Tests (Node.js based)
    {
      name: 'mcp-tests',
      testMatch: '**/mcp.spec.ts',
      use: {
        browserName: 'chromium',
      },
      dependencies: ['setup'],
    },
    
    // Performance Tests
    {
      name: 'performance',
      testMatch: '**/performance.spec.ts',
      use: {
        browserName: 'chromium',
        launchOptions: {
          args: ['--enable-precise-memory-info'],
        },
      },
      dependencies: ['setup'],
    },
  ],

  // Global setup and teardown
  globalSetup: require.resolve('./fixtures/global-setup.ts'),
  globalTeardown: require.resolve('./fixtures/global-teardown.ts'),

  // Output folder for test artifacts
  outputDir: 'test-results/',

  // Timeout for each test
  timeout: 60000,

  // Expect timeout
  expect: {
    timeout: 10000,
  },

  // Web server configuration (if needed)
  webServer: {
    command: 'npm run start:test',
    url: 'http://localhost:5000',
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
  },
});
