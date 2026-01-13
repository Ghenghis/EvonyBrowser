import { Page, Locator } from '@playwright/test';

/**
 * StatusBarPage Page Object - Svony Browser v7.0
 * 
 * This page object encapsulates all interactions with the status bar,
 * including widgets, progress indicators, and customization.
 */
export class StatusBarPage {
  readonly page: Page;
  
  // Main status bar
  readonly statusBar: Locator;
  readonly liveRegion: Locator;
  
  // Widget settings
  readonly widgetSettingsButton: Locator;
  readonly widgetSettingsPanel: Locator;
  readonly saveSettingsButton: Locator;
  readonly resetLayoutButton: Locator;
  
  // MCP widgets
  readonly mcpStatusWidget: Locator;
  readonly ragProgressWidget: Locator;
  readonly rteProgressWidget: Locator;
  
  // LLM widgets
  readonly llmStatusWidget: Locator;
  readonly tokensPerSecWidget: Locator;
  readonly vramWidget: Locator;
  readonly gpuTempWidget: Locator;
  readonly inferenceWidget: Locator;
  
  // Network widgets
  readonly packetRateWidget: Locator;
  readonly decodeRateWidget: Locator;
  readonly throughputWidget: Locator;
  readonly fiddlerStatusWidget: Locator;
  
  // Game state widgets
  readonly resourceWidget: Locator;
  readonly marchesWidget: Locator;
  readonly buildQueueWidget: Locator;
  readonly powerWidget: Locator;
  
  // Automation widgets
  readonly autopilotWidget: Locator;
  readonly queueDepthWidget: Locator;
  readonly apmWidget: Locator;
  
  // Tooltip
  readonly tooltip: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Main
    this.statusBar = page.locator('[data-testid="status-bar"]');
    this.liveRegion = page.locator('[aria-live="polite"]');
    
    // Settings
    this.widgetSettingsButton = page.locator('[data-testid="widget-settings"]');
    this.widgetSettingsPanel = page.locator('[data-testid="widget-settings-panel"]');
    this.saveSettingsButton = page.locator('[data-testid="save-widget-settings"]');
    this.resetLayoutButton = page.locator('[data-testid="reset-widget-layout"]');
    
    // MCP
    this.mcpStatusWidget = page.locator('[data-testid="widget-mcp-status"]');
    this.ragProgressWidget = page.locator('[data-testid="widget-rag-progress"]');
    this.rteProgressWidget = page.locator('[data-testid="widget-rte-progress"]');
    
    // LLM
    this.llmStatusWidget = page.locator('[data-testid="widget-llm-status"]');
    this.tokensPerSecWidget = page.locator('[data-testid="widget-tokens-per-sec"]');
    this.vramWidget = page.locator('[data-testid="widget-vram"]');
    this.gpuTempWidget = page.locator('[data-testid="widget-gpu-temp"]');
    this.inferenceWidget = page.locator('[data-testid="widget-inference"]');
    
    // Network
    this.packetRateWidget = page.locator('[data-testid="widget-packet-rate"]');
    this.decodeRateWidget = page.locator('[data-testid="widget-decode-rate"]');
    this.throughputWidget = page.locator('[data-testid="widget-throughput"]');
    this.fiddlerStatusWidget = page.locator('[data-testid="widget-fiddler-status"]');
    
    // Game state
    this.resourceWidget = page.locator('[data-testid="widget-resources"]');
    this.marchesWidget = page.locator('[data-testid="widget-marches"]');
    this.buildQueueWidget = page.locator('[data-testid="widget-build-queue"]');
    this.powerWidget = page.locator('[data-testid="widget-power"]');
    
    // Automation
    this.autopilotWidget = page.locator('[data-testid="widget-autopilot"]');
    this.queueDepthWidget = page.locator('[data-testid="widget-queue-depth"]');
    this.apmWidget = page.locator('[data-testid="widget-apm"]');
    
    // Tooltip
    this.tooltip = page.locator('[data-testid="widget-tooltip"]');
  }

  async goto() {
    await this.page.goto('/');
  }

  // Widget settings
  async openWidgetSettings() {
    await this.widgetSettingsButton.click();
    await this.widgetSettingsPanel.waitFor({ state: 'visible' });
  }

  async toggleWidget(widgetId: string) {
    await this.page.locator(`[data-testid="toggle-widget-${widgetId}"]`).click();
  }

  async moveWidgetLeft(widgetId: string) {
    await this.page.locator(`[data-testid="move-left-${widgetId}"]`).click();
  }

  async moveWidgetRight(widgetId: string) {
    await this.page.locator(`[data-testid="move-right-${widgetId}"]`).click();
  }

  async saveWidgetSettings() {
    await this.saveSettingsButton.click();
  }

  async resetWidgetLayout() {
    await this.resetLayoutButton.click();
  }

  async isWidgetVisible(widgetId: string): Promise<boolean> {
    return await this.page.locator(`[data-testid="widget-${widgetId}"]`).isVisible();
  }

  async getWidgetPosition(widgetId: string): Promise<number> {
    const widgets = await this.statusBar.locator('[data-testid^="widget-"]').all();
    for (let i = 0; i < widgets.length; i++) {
      const testId = await widgets[i].getAttribute('data-testid');
      if (testId === `widget-${widgetId}`) {
        return i;
      }
    }
    return -1;
  }

  // Progress getters
  async getRagProgress(): Promise<number> {
    const text = await this.ragProgressWidget.locator('.progress-value').textContent() || '0';
    return parseFloat(text.replace('%', ''));
  }

  async getRteProgress(): Promise<number> {
    const text = await this.rteProgressWidget.locator('.progress-value').textContent() || '0';
    return parseFloat(text.replace('%', ''));
  }

  async getInferenceProgress(): Promise<number> {
    const text = await this.inferenceWidget.locator('.progress-value').textContent() || '0';
    return parseFloat(text.replace('%', ''));
  }

  // LLM getters
  async getTokensPerSecond(): Promise<number> {
    const text = await this.tokensPerSecWidget.locator('.value').textContent() || '0';
    return parseFloat(text);
  }

  async getVramUsage(): Promise<number> {
    const text = await this.vramWidget.locator('.value').textContent() || '0';
    return parseFloat(text.replace(' GB', ''));
  }

  async getGpuTemperature(): Promise<number> {
    const text = await this.gpuTempWidget.locator('.value').textContent() || '0';
    return parseFloat(text.replace('°C', ''));
  }

  // Network getters
  async getPacketRate(): Promise<number> {
    const text = await this.packetRateWidget.locator('.value').textContent() || '0';
    return parseFloat(text.replace('/s', ''));
  }

  async getDecodeRate(): Promise<number> {
    const text = await this.decodeRateWidget.locator('.value').textContent() || '0';
    return parseFloat(text.replace('%', ''));
  }

  async getThroughput(): Promise<number> {
    const text = await this.throughputWidget.locator('.value').textContent() || '0';
    return parseFloat(text.replace(' KB/s', ''));
  }

  async getFiddlerStatus(): Promise<string> {
    return await this.fiddlerStatusWidget.locator('.status').textContent() || '';
  }

  // Game state getters
  async getActiveMarches(): Promise<number> {
    const text = await this.marchesWidget.locator('.value').textContent() || '0';
    return parseInt(text);
  }

  async getPowerLevel(): Promise<number> {
    const text = await this.powerWidget.locator('.value').textContent() || '0';
    return parseInt(text.replace(/,/g, ''));
  }

  // Automation getters
  async getAutopilotStatus(): Promise<string> {
    return await this.autopilotWidget.locator('.status').textContent() || '';
  }

  async getQueueDepth(): Promise<number> {
    const text = await this.queueDepthWidget.locator('.value').textContent() || '0';
    return parseInt(text);
  }

  async getActionsPerMinute(): Promise<number> {
    const text = await this.apmWidget.locator('.value').textContent() || '0';
    return parseFloat(text);
  }

  // Tooltip
  async hoverWidget(widgetId: string) {
    await this.page.locator(`[data-testid="widget-${widgetId}"]`).hover();
  }
}
