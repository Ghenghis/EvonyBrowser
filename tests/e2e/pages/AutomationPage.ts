import { Page, Locator } from '@playwright/test';

/**
 * AutomationPage Page Object - Svony Browser v7.0
 * 
 * This page object encapsulates all interactions with the automation panel,
 * including AutoPilot, CDP integration, and task scheduling.
 */
export class AutomationPage {
  readonly page: Page;
  
  // AutoPilot panel
  readonly autopilotPanel: Locator;
  readonly autopilotToggle: Locator;
  readonly autopilotStatus: Locator;
  readonly startButton: Locator;
  readonly stopButton: Locator;
  readonly pauseButton: Locator;
  readonly resumeButton: Locator;
  readonly emergencyStopButton: Locator;
  
  // Task queue
  readonly taskQueue: Locator;
  readonly taskRows: Locator;
  readonly addTaskButton: Locator;
  
  // Action log
  readonly actionLog: Locator;
  readonly actionLogEntries: Locator;
  
  // Safety settings
  readonly safetyButton: Locator;
  readonly safetyPanel: Locator;
  readonly maxActionsInput: Locator;
  readonly minDelayInput: Locator;
  readonly errorThresholdInput: Locator;
  
  // CDP panel
  readonly cdpPanel: Locator;
  readonly cdpToggle: Locator;
  readonly cdpStatus: Locator;
  readonly cdpConnectButton: Locator;
  readonly cdpEndpointInput: Locator;
  readonly cdpError: Locator;
  
  // Schedule panel
  readonly schedulePanel: Locator;
  readonly scheduleToggle: Locator;
  readonly scheduledTasks: Locator;
  readonly recurringTasks: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // AutoPilot
    this.autopilotPanel = page.locator('[data-testid="autopilot-panel"]');
    this.autopilotToggle = page.locator('[data-testid="autopilot-toggle"]');
    this.autopilotStatus = page.locator('[data-testid="autopilot-status"]');
    this.startButton = page.locator('[data-testid="autopilot-start"]');
    this.stopButton = page.locator('[data-testid="autopilot-stop"]');
    this.pauseButton = page.locator('[data-testid="autopilot-pause"]');
    this.resumeButton = page.locator('[data-testid="autopilot-resume"]');
    this.emergencyStopButton = page.locator('[data-testid="emergency-stop"]');
    
    // Task queue
    this.taskQueue = page.locator('[data-testid="task-queue"]');
    this.taskRows = page.locator('[data-testid="task-row"]');
    this.addTaskButton = page.locator('[data-testid="add-task"]');
    
    // Action log
    this.actionLog = page.locator('[data-testid="action-log"]');
    this.actionLogEntries = page.locator('[data-testid="action-log-entry"]');
    
    // Safety
    this.safetyButton = page.locator('[data-testid="safety-settings"]');
    this.safetyPanel = page.locator('[data-testid="safety-panel"]');
    this.maxActionsInput = page.locator('[data-testid="max-actions"]');
    this.minDelayInput = page.locator('[data-testid="min-delay"]');
    this.errorThresholdInput = page.locator('[data-testid="error-threshold"]');
    
    // CDP
    this.cdpPanel = page.locator('[data-testid="cdp-panel"]');
    this.cdpToggle = page.locator('[data-testid="cdp-toggle"]');
    this.cdpStatus = page.locator('[data-testid="cdp-status"]');
    this.cdpConnectButton = page.locator('[data-testid="cdp-connect"]');
    this.cdpEndpointInput = page.locator('[data-testid="cdp-endpoint"]');
    this.cdpError = page.locator('[data-testid="cdp-error"]');
    
    // Schedule
    this.schedulePanel = page.locator('[data-testid="schedule-panel"]');
    this.scheduleToggle = page.locator('[data-testid="schedule-toggle"]');
    this.scheduledTasks = page.locator('[data-testid="scheduled-task"]');
    this.recurringTasks = page.locator('[data-testid="recurring-task"]');
  }

  async goto() {
    await this.page.goto('/');
  }

  // AutoPilot methods
  async openAutopilotPanel() {
    const isVisible = await this.autopilotPanel.isVisible();
    if (!isVisible) {
      await this.autopilotToggle.click();
      await this.autopilotPanel.waitFor({ state: 'visible' });
    }
  }

  async startAutopilot() {
    await this.startButton.click();
  }

  async stopAutopilot() {
    await this.stopButton.click();
  }

  async pauseAutopilot() {
    await this.pauseButton.click();
  }

  async resumeAutopilot() {
    await this.resumeButton.click();
  }

  async emergencyStop() {
    await this.emergencyStopButton.click();
  }

  // Task methods
  async addTask(type: string, params: Record<string, any>) {
    await this.addTaskButton.click();
    await this.page.locator('[data-testid="task-type-select"]').selectOption(type);
    
    for (const [key, value] of Object.entries(params)) {
      await this.page.locator(`[data-testid="task-param-${key}"]`).fill(String(value));
    }
    
    await this.page.locator('[data-testid="save-task"]').click();
  }

  async removeTask(index: number) {
    await this.taskRows.nth(index).locator('[data-testid="remove-task"]').click();
  }

  async moveTaskUp(index: number) {
    await this.taskRows.nth(index).locator('[data-testid="move-up"]').click();
  }

  async editTask(index: number, params: Record<string, any>) {
    await this.taskRows.nth(index).locator('[data-testid="edit-task"]').click();
    
    for (const [key, value] of Object.entries(params)) {
      await this.page.locator(`[data-testid="task-param-${key}"]`).fill(String(value));
    }
    
    await this.page.locator('[data-testid="save-task"]').click();
  }

  async toggleTask(index: number) {
    await this.taskRows.nth(index).locator('[data-testid="toggle-task"]').click();
  }

  async getTaskCount(): Promise<number> {
    return await this.taskRows.count();
  }

  async getTaskAt(index: number): Promise<{ type: string; params: Record<string, any>; enabled: boolean }> {
    const row = this.taskRows.nth(index);
    const type = await row.locator('.task-type').textContent() || '';
    const enabled = await row.locator('[data-testid="toggle-task"]').getAttribute('aria-checked') === 'true';
    const paramsText = await row.locator('.task-params').textContent() || '{}';
    
    return { type, params: JSON.parse(paramsText), enabled };
  }

  async getActionLogCount(): Promise<number> {
    return await this.actionLogEntries.count();
  }

  // Safety methods
  async openSafetySettings() {
    await this.safetyButton.click();
    await this.safetyPanel.waitFor({ state: 'visible' });
  }

  async setMaxActionsPerHour(value: number) {
    await this.maxActionsInput.fill(String(value));
  }

  async getMaxActionsPerHour(): Promise<number> {
    return parseInt(await this.maxActionsInput.inputValue());
  }

  async setMinDelay(value: number) {
    await this.minDelayInput.fill(String(value));
  }

  async getMinDelay(): Promise<number> {
    return parseInt(await this.minDelayInput.inputValue());
  }

  async setResourceProtection(resource: string, value: number) {
    await this.page.locator(`[data-testid="protect-${resource}"]`).fill(String(value));
  }

  async getResourceProtection(resource: string): Promise<number> {
    return parseInt(await this.page.locator(`[data-testid="protect-${resource}"]`).inputValue());
  }

  async setErrorThreshold(value: number) {
    await this.errorThresholdInput.fill(String(value));
  }

  async getErrorThreshold(): Promise<number> {
    return parseInt(await this.errorThresholdInput.inputValue());
  }

  // CDP methods
  async openCdpPanel() {
    const isVisible = await this.cdpPanel.isVisible();
    if (!isVisible) {
      await this.cdpToggle.click();
      await this.cdpPanel.waitFor({ state: 'visible' });
    }
  }

  async setCdpEndpoint(endpoint: string) {
    await this.cdpEndpointInput.fill(endpoint);
  }

  async connectCdp() {
    await this.cdpConnectButton.click();
  }

  async cdpClick(x: number, y: number) {
    await this.page.locator('[data-testid="cdp-click-x"]').fill(String(x));
    await this.page.locator('[data-testid="cdp-click-y"]').fill(String(y));
    await this.page.locator('[data-testid="cdp-execute-click"]').click();
  }

  async cdpType(text: string) {
    await this.page.locator('[data-testid="cdp-type-input"]').fill(text);
    await this.page.locator('[data-testid="cdp-execute-type"]').click();
  }

  async cdpScreenshot(): Promise<string> {
    await this.page.locator('[data-testid="cdp-screenshot"]').click();
    return await this.page.locator('[data-testid="cdp-screenshot-result"]').getAttribute('src') || '';
  }

  async cdpClickElement(elementName: string) {
    await this.page.locator('[data-testid="cdp-element-select"]').selectOption(elementName);
    await this.page.locator('[data-testid="cdp-click-element"]').click();
  }

  async getLastCdpAction(): Promise<{ type: string; x?: number; y?: number; text?: string; element?: string }> {
    const actionText = await this.page.locator('[data-testid="cdp-last-action"]').textContent() || '{}';
    return JSON.parse(actionText);
  }

  // Schedule methods
  async openSchedulePanel() {
    await this.scheduleToggle.click();
    await this.schedulePanel.waitFor({ state: 'visible' });
  }

  async scheduleTask(type: string, params: Record<string, any>, time: Date) {
    await this.page.locator('[data-testid="schedule-task"]').click();
    await this.page.locator('[data-testid="schedule-type"]').selectOption(type);
    await this.page.locator('[data-testid="schedule-time"]').fill(time.toISOString().slice(0, 16));
    await this.page.locator('[data-testid="save-schedule"]').click();
  }

  async cancelScheduledTask(index: number) {
    await this.scheduledTasks.nth(index).locator('[data-testid="cancel-schedule"]').click();
  }

  async getScheduledTasks(): Promise<any[]> {
    const count = await this.scheduledTasks.count();
    const tasks = [];
    for (let i = 0; i < count; i++) {
      const text = await this.scheduledTasks.nth(i).textContent();
      tasks.push(text);
    }
    return tasks;
  }

  async setRecurringTask(type: string, params: Record<string, any>, cron: string) {
    await this.page.locator('[data-testid="add-recurring"]').click();
    await this.page.locator('[data-testid="recurring-type"]').selectOption(type);
    await this.page.locator('[data-testid="recurring-cron"]').fill(cron);
    await this.page.locator('[data-testid="save-recurring"]').click();
  }

  async getRecurringTasks(): Promise<any[]> {
    const count = await this.recurringTasks.count();
    const tasks = [];
    for (let i = 0; i < count; i++) {
      const text = await this.recurringTasks.nth(i).textContent();
      tasks.push(text);
    }
    return tasks;
  }

  async getRetryCount(): Promise<number> {
    const text = await this.page.locator('[data-testid="retry-count"]').textContent() || '0';
    return parseInt(text);
  }

  async getErrorLogs(): Promise<string[]> {
    const count = await this.page.locator('[data-testid="error-log-entry"]').count();
    const logs = [];
    for (let i = 0; i < count; i++) {
      const text = await this.page.locator('[data-testid="error-log-entry"]').nth(i).textContent();
      if (text) logs.push(text);
    }
    return logs;
  }
}
