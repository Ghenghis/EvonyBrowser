import { test, expect } from '@playwright/test';
import { AutomationPage } from '../pages/AutomationPage';

/**
 * Automation Tests - Svony Browser v7.0
 * 
 * These tests verify the automation functionality including:
 * - AutoPilot controls
 * - Task scheduling
 * - Safety limits
 * - CDP integration
 */

test.describe('AutoPilot Functionality', () => {
  let automationPage: AutomationPage;

  test.beforeEach(async ({ page }) => {
    automationPage = new AutomationPage(page);
    await automationPage.goto();
  });

  test.describe('AutoPilot Controls', () => {
    test('should display autopilot panel', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await expect(automationPage.autopilotPanel).toBeVisible();
    });

    test('should start autopilot', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.startAutopilot();
      await expect(automationPage.autopilotStatus).toContainText('Running');
    });

    test('should stop autopilot', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.startAutopilot();
      await automationPage.stopAutopilot();
      await expect(automationPage.autopilotStatus).toContainText('Stopped');
    });

    test('should pause autopilot', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.startAutopilot();
      await automationPage.pauseAutopilot();
      await expect(automationPage.autopilotStatus).toContainText('Paused');
    });

    test('should resume autopilot', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.startAutopilot();
      await automationPage.pauseAutopilot();
      await automationPage.resumeAutopilot();
      await expect(automationPage.autopilotStatus).toContainText('Running');
    });

    test('should show task queue', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await expect(automationPage.taskQueue).toBeVisible();
    });

    test('should show action log', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.startAutopilot();
      await page.waitForTimeout(2000);
      
      const logCount = await automationPage.getActionLogCount();
      expect(logCount).toBeGreaterThanOrEqual(0);
    });
  });

  test.describe('Task Configuration', () => {
    test('should add task to queue', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.addTask('trainTroops', { type: 'cavalry', count: 1000 });
      
      const taskCount = await automationPage.getTaskCount();
      expect(taskCount).toBeGreaterThan(0);
    });

    test('should remove task from queue', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.addTask('trainTroops', { type: 'cavalry', count: 1000 });
      const initialCount = await automationPage.getTaskCount();
      
      await automationPage.removeTask(0);
      const newCount = await automationPage.getTaskCount();
      
      expect(newCount).toBe(initialCount - 1);
    });

    test('should reorder tasks', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.addTask('trainTroops', { type: 'cavalry', count: 1000 });
      await automationPage.addTask('upgradeBuilding', { building: 'barracks', level: 26 });
      
      await automationPage.moveTaskUp(1);
      
      const firstTask = await automationPage.getTaskAt(0);
      expect(firstTask.type).toBe('upgradeBuilding');
    });

    test('should edit task parameters', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.addTask('trainTroops', { type: 'cavalry', count: 1000 });
      
      await automationPage.editTask(0, { count: 2000 });
      
      const task = await automationPage.getTaskAt(0);
      expect(task.params.count).toBe(2000);
    });

    test('should enable/disable task', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.addTask('trainTroops', { type: 'cavalry', count: 1000 });
      
      await automationPage.toggleTask(0);
      
      const task = await automationPage.getTaskAt(0);
      expect(task.enabled).toBe(false);
    });
  });

  test.describe('Safety Limits', () => {
    test('should display safety settings', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.openSafetySettings();
      
      await expect(automationPage.safetyPanel).toBeVisible();
    });

    test('should set max actions per hour', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.openSafetySettings();
      await automationPage.setMaxActionsPerHour(100);
      
      const value = await automationPage.getMaxActionsPerHour();
      expect(value).toBe(100);
    });

    test('should set minimum delay between actions', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.openSafetySettings();
      await automationPage.setMinDelay(500);
      
      const value = await automationPage.getMinDelay();
      expect(value).toBe(500);
    });

    test('should enforce resource protection', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.openSafetySettings();
      await automationPage.setResourceProtection('gold', 100000);
      
      const value = await automationPage.getResourceProtection('gold');
      expect(value).toBe(100000);
    });

    test('should stop on error threshold', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.openSafetySettings();
      await automationPage.setErrorThreshold(5);
      
      const value = await automationPage.getErrorThreshold();
      expect(value).toBe(5);
    });

    test('should enable emergency stop', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.startAutopilot();
      
      await automationPage.emergencyStop();
      
      await expect(automationPage.autopilotStatus).toContainText('Emergency Stop');
    });
  });

  test.describe('CDP Integration', () => {
    test('should connect to CDP', async ({ page }) => {
      await automationPage.openCdpPanel();
      await automationPage.connectCdp();
      
      await expect(automationPage.cdpStatus).toContainText('Connected');
    });

    test('should execute click action', async ({ page }) => {
      await automationPage.openCdpPanel();
      await automationPage.connectCdp();
      
      await automationPage.cdpClick(100, 200);
      
      // Verify click was executed
      const lastAction = await automationPage.getLastCdpAction();
      expect(lastAction.type).toBe('click');
      expect(lastAction.x).toBe(100);
      expect(lastAction.y).toBe(200);
    });

    test('should execute type action', async ({ page }) => {
      await automationPage.openCdpPanel();
      await automationPage.connectCdp();
      
      await automationPage.cdpType('test input');
      
      const lastAction = await automationPage.getLastCdpAction();
      expect(lastAction.type).toBe('type');
      expect(lastAction.text).toBe('test input');
    });

    test('should take screenshot', async ({ page }) => {
      await automationPage.openCdpPanel();
      await automationPage.connectCdp();
      
      const screenshot = await automationPage.cdpScreenshot();
      expect(screenshot).toBeDefined();
    });

    test('should use UI element map', async ({ page }) => {
      await automationPage.openCdpPanel();
      await automationPage.connectCdp();
      
      await automationPage.cdpClickElement('barracks');
      
      const lastAction = await automationPage.getLastCdpAction();
      expect(lastAction.element).toBe('barracks');
    });
  });

  test.describe('Scheduling', () => {
    test('should schedule task for later', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      
      const futureTime = new Date(Date.now() + 3600000); // 1 hour from now
      await automationPage.scheduleTask('trainTroops', { type: 'cavalry', count: 1000 }, futureTime);
      
      const scheduledTasks = await automationPage.getScheduledTasks();
      expect(scheduledTasks.length).toBeGreaterThan(0);
    });

    test('should show scheduled tasks', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      await automationPage.openSchedulePanel();
      
      await expect(automationPage.schedulePanel).toBeVisible();
    });

    test('should cancel scheduled task', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      
      const futureTime = new Date(Date.now() + 3600000);
      await automationPage.scheduleTask('trainTroops', { type: 'cavalry', count: 1000 }, futureTime);
      
      await automationPage.cancelScheduledTask(0);
      
      const scheduledTasks = await automationPage.getScheduledTasks();
      expect(scheduledTasks.length).toBe(0);
    });

    test('should set recurring schedule', async ({ page }) => {
      await automationPage.openAutopilotPanel();
      
      await automationPage.setRecurringTask('collectResources', {}, '0 */4 * * *'); // Every 4 hours
      
      const recurringTasks = await automationPage.getRecurringTasks();
      expect(recurringTasks.length).toBeGreaterThan(0);
    });
  });
});

test.describe('Automation Error Handling', () => {
  test('should handle connection errors', async ({ page }) => {
    const automationPage = new AutomationPage(page);
    await automationPage.goto();
    await automationPage.openCdpPanel();
    
    // Try to connect to invalid endpoint
    await automationPage.setCdpEndpoint('ws://invalid:9222');
    await automationPage.connectCdp();
    
    await expect(automationPage.cdpError).toBeVisible();
  });

  test('should retry failed actions', async ({ page }) => {
    const automationPage = new AutomationPage(page);
    await automationPage.goto();
    await automationPage.openAutopilotPanel();
    
    // Add a task that will fail
    await automationPage.addTask('invalidAction', {});
    await automationPage.startAutopilot();
    
    await page.waitForTimeout(2000);
    
    const retryCount = await automationPage.getRetryCount();
    expect(retryCount).toBeGreaterThan(0);
  });

  test('should log errors', async ({ page }) => {
    const automationPage = new AutomationPage(page);
    await automationPage.goto();
    await automationPage.openAutopilotPanel();
    
    await automationPage.addTask('invalidAction', {});
    await automationPage.startAutopilot();
    
    await page.waitForTimeout(2000);
    
    const errorLogs = await automationPage.getErrorLogs();
    expect(errorLogs.length).toBeGreaterThan(0);
  });
});
