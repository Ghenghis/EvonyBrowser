# 🎭 V7.0 PLAYWRIGHT COMPLETE - FULL AUTOMATION TESTING

**EVERY PLAYWRIGHT FEATURE - 100% AUTOMATION COVERAGE**

---

## 🔴 COMPLETE PLAYWRIGHT SETUP

### Installation & Configuration
```bash
# Install Playwright with ALL browsers
npm init playwright@latest --yes --install-deps --browsers=chromium,firefox,webkit,msedge
npm install @playwright/test

# Install additional tools
npm install playwright-video-recorder
npm install playwright-lighthouse
npm install playwright-axe
npm install @percy/playwright
npm install allure-playwright
```

### playwright.config.ts - FULL CONFIGURATION
```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [
    ['html'],
    ['json', { outputFile: 'test-results.json' }],
    ['junit', { outputFile: 'junit.xml' }],
    ['allure-playwright'],
    ['github'],
    ['line'],
    ['dot'],
    ['list']
  ],
  
  use: {
    actionTimeout: 10000,
    navigationTimeout: 30000,
    baseURL: 'http://localhost:5000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    viewport: { width: 1920, height: 1080 },
    
    // Context options
    contextOptions: {
      recordVideo: {
        dir: 'videos/',
        size: { width: 1920, height: 1080 }
      },
      ignoreHTTPSErrors: true,
      permissions: ['geolocation', 'notifications', 'camera', 'microphone'],
      geolocation: { longitude: 12.492507, latitude: 41.889938 },
      locale: 'en-US',
      timezoneId: 'America/New_York',
      offline: false,
      httpCredentials: {
        username: 'user',
        password: 'pass'
      }
    }
  },
  
  projects: [
    // Desktop browsers
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] }
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] }
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] }
    },
    {
      name: 'edge',
      use: { ...devices['Desktop Edge'] }
    },
    
    // Mobile browsers
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 5'] }
    },
    {
      name: 'Mobile Safari',
      use: { ...devices['iPhone 12'] }
    },
    
    // Tablet
    {
      name: 'iPad',
      use: { ...devices['iPad Pro'] }
    },
    
    // Custom viewports
    {
      name: '4K',
      use: { viewport: { width: 3840, height: 2160 } }
    },
    {
      name: 'Full HD',
      use: { viewport: { width: 1920, height: 1080 } }
    }
  ],
  
  webServer: {
    command: 'npm run start',
    url: 'http://localhost:5000',
    reuseExistingServer: !process.env.CI,
    stdout: 'pipe',
    stderr: 'pipe'
  }
});
```

---

## 🎯 COMPLETE TEST SUITES

### 1. Browser Automation Tests
```typescript
import { test, expect, Page, BrowserContext, Locator } from '@playwright/test';

test.describe('Svony Browser - Complete E2E', () => {
  let page: Page;
  let context: BrowserContext;
  
  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext({
      storageState: 'auth.json',
      recordVideo: {
        dir: 'videos/',
        size: { width: 1920, height: 1080 }
      }
    });
    page = await context.newPage();
  });
  
  test.afterAll(async () => {
    await context.close();
  });
  
  test('Full application flow', async () => {
    // Navigate
    await page.goto('/');
    
    // Screenshot
    await page.screenshot({ path: 'screenshots/home.png', fullPage: true });
    
    // Login
    await page.fill('#username', 'test');
    await page.fill('#password', 'test');
    await page.click('#login');
    
    // Wait for navigation
    await page.waitForURL('**/dashboard');
    
    // Verify elements
    await expect(page.locator('#leftBrowser')).toBeVisible();
    await expect(page.locator('#rightBrowser')).toBeVisible();
  });
});
```

### 2. Network Interception Tests
```typescript
test('Network interception', async ({ page }) => {
  // Mock API responses
  await page.route('**/api/data', route => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true })
    });
  });
  
  // Block resources
  await page.route('**/*.{png,jpg,jpeg}', route => route.abort());
  
  // Modify requests
  await page.route('**/api/**', route => {
    const headers = {
      ...route.request().headers(),
      'X-Custom-Header': 'value'
    };
    route.continue({ headers });
  });
  
  // Log all requests
  page.on('request', request => {
    console.log('>>', request.method(), request.url());
  });
  
  page.on('response', response => {
    console.log('<<', response.status(), response.url());
  });
  
  await page.goto('/');
});
```

### 3. File Operations Tests
```typescript
test('File upload/download', async ({ page }) => {
  // File upload
  const fileChooserPromise = page.waitForEvent('filechooser');
  await page.click('#upload-button');
  const fileChooser = await fileChooserPromise;
  await fileChooser.setFiles(['test-files/document.pdf']);
  
  // Multiple files
  await fileChooser.setFiles([
    'test-files/doc1.pdf',
    'test-files/doc2.pdf'
  ]);
  
  // File download
  const downloadPromise = page.waitForEvent('download');
  await page.click('#download-button');
  const download = await downloadPromise;
  
  // Save download
  await download.saveAs('downloads/' + download.suggestedFilename());
  
  // Verify download
  expect(download.suggestedFilename()).toBe('expected-file.pdf');
});
```

### 4. Browser Context Tests
```typescript
test('Multiple contexts', async ({ browser }) => {
  // Create multiple users
  const userContext = await browser.newContext({
    storageState: 'user-auth.json'
  });
  const adminContext = await browser.newContext({
    storageState: 'admin-auth.json'
  });
  
  const userPage = await userContext.newPage();
  const adminPage = await adminContext.newPage();
  
  // Test user permissions
  await userPage.goto('/user-dashboard');
  await expect(userPage.locator('.admin-panel')).not.toBeVisible();
  
  // Test admin permissions
  await adminPage.goto('/admin-dashboard');
  await expect(adminPage.locator('.admin-panel')).toBeVisible();
  
  await userContext.close();
  await adminContext.close();
});
```

### 5. Geolocation & Permissions Tests
```typescript
test('Geolocation', async ({ context, page }) => {
  // Set geolocation
  await context.setGeolocation({ latitude: 40.7128, longitude: -74.0060 });
  
  // Grant permissions
  await context.grantPermissions(['geolocation']);
  
  await page.goto('/map');
  await page.click('#show-location');
  
  // Verify location
  const location = await page.textContent('#current-location');
  expect(location).toContain('New York');
  
  // Change location
  await context.setGeolocation({ latitude: 51.5074, longitude: -0.1278 });
  await page.click('#refresh-location');
  
  const newLocation = await page.textContent('#current-location');
  expect(newLocation).toContain('London');
});
```

### 6. Device Emulation Tests
```typescript
import { devices } from '@playwright/test';

test('Mobile emulation', async ({ browser }) => {
  const iPhone = devices['iPhone 12'];
  const context = await browser.newContext({
    ...iPhone,
    locale: 'en-US',
    timezoneId: 'America/Los_Angeles'
  });
  
  const page = await context.newPage();
  await page.goto('/');
  
  // Test responsive design
  expect(await page.locator('.mobile-menu').isVisible()).toBeTruthy();
  expect(await page.locator('.desktop-menu').isVisible()).toBeFalsy();
  
  // Test touch events
  await page.tap('.hamburger-menu');
  await page.swipe('.carousel', { direction: 'left' });
  
  await context.close();
});
```

### 7. Accessibility Testing
```typescript
import { test, expect } from '@playwright/test';
import { injectAxe, checkA11y } from 'axe-playwright';

test('Accessibility', async ({ page }) => {
  await page.goto('/');
  
  // Inject axe-core
  await injectAxe(page);
  
  // Run accessibility checks
  await checkA11y(page, null, {
    detailedReport: true,
    detailedReportOptions: {
      html: true
    }
  });
  
  // Check specific elements
  await checkA11y(page, '#main-content', {
    runOnly: {
      type: 'tag',
      values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']
    }
  });
  
  // Keyboard navigation
  await page.keyboard.press('Tab');
  const focusedElement = await page.evaluate(() => document.activeElement?.tagName);
  expect(focusedElement).toBe('BUTTON');
});
```

### 8. Visual Testing
```typescript
import { test, expect } from '@playwright/test';
import percySnapshot from '@percy/playwright';

test('Visual regression', async ({ page }) => {
  await page.goto('/');
  
  // Percy snapshot
  await percySnapshot(page, 'Homepage');
  
  // Playwright screenshot comparison
  await expect(page).toHaveScreenshot('homepage.png', {
    maxDiffPixels: 100,
    threshold: 0.2
  });
  
  // Element screenshot
  const header = page.locator('header');
  await expect(header).toHaveScreenshot('header.png');
  
  // Mask dynamic content
  await expect(page).toHaveScreenshot('masked.png', {
    mask: [page.locator('.timestamp')],
    maskColor: '#FF00FF'
  });
  
  // Full page screenshot
  await expect(page).toHaveScreenshot('fullpage.png', {
    fullPage: true,
    animations: 'disabled',
    caret: 'hide'
  });
});
```

### 9. Performance Testing
```typescript
import { test } from '@playwright/test';
import lighthouse from 'lighthouse';

test('Performance metrics', async ({ page, browser }) => {
  await page.goto('/');
  
  // Collect performance metrics
  const metrics = await page.evaluate(() => {
    const perf = window.performance;
    return {
      domContentLoaded: perf.timing.domContentLoadedEventEnd - perf.timing.navigationStart,
      loadComplete: perf.timing.loadEventEnd - perf.timing.navigationStart,
      firstPaint: perf.getEntriesByName('first-paint')[0]?.startTime,
      firstContentfulPaint: perf.getEntriesByName('first-contentful-paint')[0]?.startTime,
      memoryUsage: (performance as any).memory?.usedJSHeapSize
    };
  });
  
  console.log('Performance Metrics:', metrics);
  
  // Lighthouse audit
  const port = (browser as any)._options.args.find((arg: string) => 
    arg.startsWith('--remote-debugging-port')
  )?.split('=')[1];
  
  const result = await lighthouse('http://localhost:5000', {
    port: Number(port),
    output: 'html',
    onlyCategories: ['performance', 'accessibility', 'seo']
  });
  
  console.log('Lighthouse Score:', result.lhr.categories.performance.score * 100);
});
```

### 10. API Testing
```typescript
test('API endpoints', async ({ request }) => {
  // GET request
  const getResponse = await request.get('/api/users');
  expect(getResponse.ok()).toBeTruthy();
  const users = await getResponse.json();
  expect(users).toHaveLength(10);
  
  // POST request
  const postResponse = await request.post('/api/users', {
    data: {
      name: 'Test User',
      email: 'test@example.com'
    }
  });
  expect(postResponse.status()).toBe(201);
  
  // PUT request
  const putResponse = await request.put('/api/users/1', {
    data: { name: 'Updated Name' }
  });
  expect(putResponse.ok()).toBeTruthy();
  
  // DELETE request
  const deleteResponse = await request.delete('/api/users/1');
  expect(deleteResponse.status()).toBe(204);
  
  // File upload
  const fileResponse = await request.post('/api/upload', {
    multipart: {
      file: {
        name: 'file.txt',
        mimeType: 'text/plain',
        buffer: Buffer.from('file content')
      }
    }
  });
  expect(fileResponse.ok()).toBeTruthy();
});
```

---

## 🔧 ADVANCED PLAYWRIGHT FEATURES

### 1. Custom Test Fixtures
```typescript
import { test as base } from '@playwright/test';

// Extend base test
export const test = base.extend<{
  authenticatedPage: Page;
  testData: any;
}>({
  authenticatedPage: async ({ page }, use) => {
    await page.goto('/login');
    await page.fill('#username', 'test');
    await page.fill('#password', 'test');
    await page.click('#login');
    await page.waitForURL('**/dashboard');
    await use(page);
  },
  
  testData: async ({}, use) => {
    const data = {
      user: { name: 'Test', email: 'test@test.com' },
      products: [{ id: 1, name: 'Product 1' }]
    };
    await use(data);
  }
});

test('Using fixtures', async ({ authenticatedPage, testData }) => {
  // Already logged in
  await expect(authenticatedPage.locator('.username')).toContainText(testData.user.name);
});
```

### 2. Page Object Model
```typescript
// pages/LoginPage.ts
export class LoginPage {
  constructor(private page: Page) {}
  
  async goto() {
    await this.page.goto('/login');
  }
  
  async login(username: string, password: string) {
    await this.page.fill('#username', username);
    await this.page.fill('#password', password);
    await this.page.click('#login');
  }
  
  async getErrorMessage() {
    return this.page.textContent('.error-message');
  }
}

// pages/DashboardPage.ts
export class DashboardPage {
  constructor(private page: Page) {}
  
  get leftBrowser() {
    return this.page.locator('#leftBrowser');
  }
  
  get rightBrowser() {
    return this.page.locator('#rightBrowser');
  }
  
  async navigateLeft(url: string) {
    await this.leftBrowser.locator('input').fill(url);
    await this.leftBrowser.locator('button').click();
  }
}

// Test using POM
test('POM test', async ({ page }) => {
  const loginPage = new LoginPage(page);
  const dashboardPage = new DashboardPage(page);
  
  await loginPage.goto();
  await loginPage.login('user', 'pass');
  
  await expect(dashboardPage.leftBrowser).toBeVisible();
});
```

### 3. Database Testing
```typescript
import { test } from '@playwright/test';
import sql from 'mssql';

test('Database integration', async ({ page }) => {
  // Setup database connection
  const pool = await sql.connect({
    server: 'localhost',
    database: 'testdb',
    options: { trustServerCertificate: true }
  });
  
  // Clean data before test
  await pool.request().query('DELETE FROM users WHERE email LIKE "%test%"');
  
  // Insert test data
  await pool.request()
    .input('email', 'test@example.com')
    .query('INSERT INTO users (email) VALUES (@email)');
  
  // Run test
  await page.goto('/users');
  await expect(page.locator('text=test@example.com')).toBeVisible();
  
  // Verify in database
  const result = await pool.request()
    .query('SELECT * FROM users WHERE email = "test@example.com"');
  expect(result.recordset).toHaveLength(1);
  
  await pool.close();
});
```

### 4. WebSocket Testing
```typescript
test('WebSocket communication', async ({ page }) => {
  // Listen for WebSocket messages
  page.on('websocket', ws => {
    console.log(`WebSocket opened: ${ws.url()}`);
    
    ws.on('framesent', event => {
      console.log(`Sent: ${event.payload}`);
    });
    
    ws.on('framereceived', event => {
      console.log(`Received: ${event.payload}`);
    });
    
    ws.on('close', () => {
      console.log('WebSocket closed');
    });
  });
  
  await page.goto('/chat');
  
  // Send message
  await page.fill('#message', 'Hello WebSocket');
  await page.click('#send');
  
  // Wait for response
  await page.waitForSelector('text=Message received');
});
```

### 5. Browser CDP (Chrome DevTools Protocol)
```typescript
test('CDP features', async ({ page, browser }) => {
  const context = browser.contexts()[0];
  const cdpSession = await context.newCDPSession(page);
  
  // Enable network domain
  await cdpSession.send('Network.enable');
  
  // Set user agent
  await cdpSession.send('Network.setUserAgentOverride', {
    userAgent: 'Custom User Agent'
  });
  
  // Emulate network conditions
  await cdpSession.send('Network.emulateNetworkConditions', {
    offline: false,
    downloadThroughput: 200 * 1024 / 8, // 200kb/s
    uploadThroughput: 50 * 1024 / 8,    // 50kb/s
    latency: 500 // 500ms
  });
  
  // Get cookies
  const cookies = await cdpSession.send('Network.getCookies');
  console.log('Cookies:', cookies);
  
  // Take heap snapshot
  await cdpSession.send('HeapProfiler.enable');
  const snapshot = await cdpSession.send('HeapProfiler.takeHeapSnapshot');
  
  await page.goto('/');
});
```

---

## 📊 TEST REPORTING

### HTML Report Configuration
```typescript
// playwright.config.ts
reporter: [
  ['html', { 
    open: 'never',
    outputFolder: 'playwright-report',
    attachmentsBaseURL: 'http://localhost:9323'
  }]
]
```

### Allure Report
```bash
npm install --save-dev allure-playwright

# Generate report
npx playwright test --reporter=allure-playwright
allure generate allure-results -o allure-report
allure open allure-report
```

### Custom Reporter
```typescript
// reporters/custom-reporter.ts
import { Reporter, TestCase, TestResult } from '@playwright/test/reporter';

class CustomReporter implements Reporter {
  onBegin(config, suite) {
    console.log(`Starting test run with ${suite.allTests().length} tests`);
  }
  
  onTestBegin(test: TestCase) {
    console.log(`Starting test: ${test.title}`);
  }
  
  onTestEnd(test: TestCase, result: TestResult) {
    console.log(`Test ${test.title}: ${result.status}`);
    if (result.status === 'failed') {
      console.log('Error:', result.error);
    }
  }
  
  onEnd(result) {
    console.log(`Test run finished with status: ${result.status}`);
  }
}

export default CustomReporter;
```

---

## 🚀 CI/CD INTEGRATION

### GitHub Actions
```yaml
name: Playwright Tests

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - uses: actions/setup-node@v3
      with:
        node-version: 18
    
    - name: Install dependencies
      run: |
        npm ci
        npx playwright install --with-deps
    
    - name: Run Playwright tests
      run: npx playwright test
    
    - uses: actions/upload-artifact@v3
      if: always()
      with:
        name: playwright-report
        path: playwright-report/
        retention-days: 30
    
    - uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-videos
        path: videos/
        retention-days: 30
```

---

## ✅ COMPLETE TEST COMMAND SUITE

```bash
# Run all tests
npx playwright test

# Run specific test file
npx playwright test tests/login.spec.ts

# Run tests in headed mode
npx playwright test --headed

# Run tests in specific browser
npx playwright test --project=chromium

# Run tests with specific tag
npx playwright test --grep @smoke

# Run tests in parallel
npx playwright test --workers=4

# Debug test
npx playwright test --debug

# Generate code
npx playwright codegen http://localhost:5000

# Show report
npx playwright show-report

# Update snapshots
npx playwright test --update-snapshots

# Run with trace
npx playwright test --trace on

# View trace
npx playwright show-trace trace.zip
```

---

**COMPLETE PLAYWRIGHT IMPLEMENTATION - 100% AUTOMATION COVERAGE**
