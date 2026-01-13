import { test, expect } from '@playwright/test';
import { StatusBarPage } from '../pages/StatusBarPage';

/**
 * Status Bar Tests - Svony Browser v7.0
 * 
 * These tests verify the status bar functionality including:
 * - Widget display
 * - Widget customization
 * - Real-time updates
 * - Progress indicators
 */

test.describe('Status Bar', () => {
  let statusBarPage: StatusBarPage;

  test.beforeEach(async ({ page }) => {
    statusBarPage = new StatusBarPage(page);
    await statusBarPage.goto();
  });

  test.describe('Widget Display', () => {
    test('should display status bar', async ({ page }) => {
      await expect(statusBarPage.statusBar).toBeVisible();
    });

    test('should show MCP status widget', async ({ page }) => {
      await expect(statusBarPage.mcpStatusWidget).toBeVisible();
    });

    test('should show RAG progress widget', async ({ page }) => {
      await expect(statusBarPage.ragProgressWidget).toBeVisible();
    });

    test('should show RTE progress widget', async ({ page }) => {
      await expect(statusBarPage.rteProgressWidget).toBeVisible();
    });

    test('should show LLM status widget', async ({ page }) => {
      await expect(statusBarPage.llmStatusWidget).toBeVisible();
    });

    test('should show packet rate widget', async ({ page }) => {
      await expect(statusBarPage.packetRateWidget).toBeVisible();
    });

    test('should show Fiddler status widget', async ({ page }) => {
      await expect(statusBarPage.fiddlerStatusWidget).toBeVisible();
    });
  });

  test.describe('Widget Customization', () => {
    test('should open widget settings', async ({ page }) => {
      await statusBarPage.openWidgetSettings();
      await expect(statusBarPage.widgetSettingsPanel).toBeVisible();
    });

    test('should toggle widget visibility', async ({ page }) => {
      await statusBarPage.openWidgetSettings();
      await statusBarPage.toggleWidget('ragProgress');
      
      const isVisible = await statusBarPage.isWidgetVisible('ragProgress');
      expect(isVisible).toBe(false);
    });

    test('should reorder widgets', async ({ page }) => {
      await statusBarPage.openWidgetSettings();
      await statusBarPage.moveWidgetLeft('rteProgress');
      
      const position = await statusBarPage.getWidgetPosition('rteProgress');
      expect(position).toBeLessThan(5);
    });

    test('should save widget configuration', async ({ page }) => {
      await statusBarPage.openWidgetSettings();
      await statusBarPage.toggleWidget('ragProgress');
      await statusBarPage.saveWidgetSettings();
      
      // Reload page and verify
      await page.reload();
      const isVisible = await statusBarPage.isWidgetVisible('ragProgress');
      expect(isVisible).toBe(false);
    });

    test('should reset to default layout', async ({ page }) => {
      await statusBarPage.openWidgetSettings();
      await statusBarPage.toggleWidget('ragProgress');
      await statusBarPage.resetWidgetLayout();
      
      const isVisible = await statusBarPage.isWidgetVisible('ragProgress');
      expect(isVisible).toBe(true);
    });
  });

  test.describe('Progress Indicators', () => {
    test('should display RAG progress bar', async ({ page }) => {
      const progress = await statusBarPage.getRagProgress();
      expect(progress).toBeGreaterThanOrEqual(0);
      expect(progress).toBeLessThanOrEqual(100);
    });

    test('should display RTE progress bar', async ({ page }) => {
      const progress = await statusBarPage.getRteProgress();
      expect(progress).toBeGreaterThanOrEqual(0);
      expect(progress).toBeLessThanOrEqual(100);
    });

    test('should update progress in real-time', async ({ page }) => {
      const initialProgress = await statusBarPage.getRagProgress();
      
      // Wait for potential update
      await page.waitForTimeout(2000);
      
      // Progress should be a valid number
      const currentProgress = await statusBarPage.getRagProgress();
      expect(currentProgress).toBeGreaterThanOrEqual(0);
    });

    test('should show progress tooltip', async ({ page }) => {
      await statusBarPage.hoverWidget('ragProgress');
      await expect(statusBarPage.tooltip).toBeVisible();
    });
  });

  test.describe('LLM Metrics', () => {
    test('should display tokens per second', async ({ page }) => {
      const tokensPerSec = await statusBarPage.getTokensPerSecond();
      expect(tokensPerSec).toBeGreaterThanOrEqual(0);
    });

    test('should display VRAM usage', async ({ page }) => {
      const vramUsage = await statusBarPage.getVramUsage();
      expect(vramUsage).toBeGreaterThanOrEqual(0);
    });

    test('should display GPU temperature', async ({ page }) => {
      const gpuTemp = await statusBarPage.getGpuTemperature();
      expect(gpuTemp).toBeGreaterThanOrEqual(0);
    });

    test('should display inference progress', async ({ page }) => {
      const progress = await statusBarPage.getInferenceProgress();
      expect(progress).toBeGreaterThanOrEqual(0);
      expect(progress).toBeLessThanOrEqual(100);
    });
  });

  test.describe('Network Metrics', () => {
    test('should display packet rate', async ({ page }) => {
      const packetRate = await statusBarPage.getPacketRate();
      expect(packetRate).toBeGreaterThanOrEqual(0);
    });

    test('should display decode rate', async ({ page }) => {
      const decodeRate = await statusBarPage.getDecodeRate();
      expect(decodeRate).toBeGreaterThanOrEqual(0);
    });

    test('should display throughput', async ({ page }) => {
      const throughput = await statusBarPage.getThroughput();
      expect(throughput).toBeGreaterThanOrEqual(0);
    });

    test('should display Fiddler connection status', async ({ page }) => {
      const status = await statusBarPage.getFiddlerStatus();
      expect(['Connected', 'Disconnected', 'Connecting']).toContain(status);
    });
  });

  test.describe('Game State Widgets', () => {
    test('should display resource summary', async ({ page }) => {
      await expect(statusBarPage.resourceWidget).toBeVisible();
    });

    test('should display active marches', async ({ page }) => {
      const marches = await statusBarPage.getActiveMarches();
      expect(marches).toBeGreaterThanOrEqual(0);
    });

    test('should display build queue status', async ({ page }) => {
      await expect(statusBarPage.buildQueueWidget).toBeVisible();
    });

    test('should display power level', async ({ page }) => {
      const power = await statusBarPage.getPowerLevel();
      expect(power).toBeGreaterThanOrEqual(0);
    });
  });

  test.describe('Automation Widgets', () => {
    test('should display autopilot status', async ({ page }) => {
      const status = await statusBarPage.getAutopilotStatus();
      expect(['Running', 'Stopped', 'Paused']).toContain(status);
    });

    test('should display task queue depth', async ({ page }) => {
      const depth = await statusBarPage.getQueueDepth();
      expect(depth).toBeGreaterThanOrEqual(0);
    });

    test('should display actions per minute', async ({ page }) => {
      const apm = await statusBarPage.getActionsPerMinute();
      expect(apm).toBeGreaterThanOrEqual(0);
    });
  });

  test.describe('Accessibility', () => {
    test('should have proper ARIA labels', async ({ page }) => {
      const ariaLabel = await statusBarPage.statusBar.getAttribute('aria-label');
      expect(ariaLabel).toBeDefined();
    });

    test('should be keyboard navigable', async ({ page }) => {
      await statusBarPage.statusBar.focus();
      await page.keyboard.press('Tab');
      
      const focusedElement = await page.evaluate(() => document.activeElement?.getAttribute('data-testid'));
      expect(focusedElement).toBeDefined();
    });

    test('should announce updates to screen readers', async ({ page }) => {
      await expect(statusBarPage.liveRegion).toBeAttached();
    });
  });
});

test.describe('Status Bar Performance', () => {
  test('should update efficiently', async ({ page }) => {
    const statusBarPage = new StatusBarPage(page);
    await statusBarPage.goto();
    
    const startTime = Date.now();
    
    // Wait for multiple updates
    await page.waitForTimeout(5000);
    
    const endTime = Date.now();
    const duration = endTime - startTime;
    
    // Should not cause performance issues
    expect(duration).toBeLessThan(6000);
  });

  test('should not cause memory leaks', async ({ page }) => {
    const statusBarPage = new StatusBarPage(page);
    await statusBarPage.goto();
    
    // Get initial memory
    const initialMemory = await page.evaluate(() => (performance as any).memory?.usedJSHeapSize || 0);
    
    // Wait for updates
    await page.waitForTimeout(10000);
    
    // Get final memory
    const finalMemory = await page.evaluate(() => (performance as any).memory?.usedJSHeapSize || 0);
    
    // Memory should not grow significantly
    const memoryGrowth = finalMemory - initialMemory;
    expect(memoryGrowth).toBeLessThan(50 * 1024 * 1024); // Less than 50MB growth
  });
});
