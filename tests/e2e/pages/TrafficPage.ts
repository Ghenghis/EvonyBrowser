import { Page, Locator, Download } from '@playwright/test';

/**
 * TrafficPage Page Object - Svony Browser v7.0
 * 
 * This page object encapsulates all interactions with the traffic viewer panel,
 * including packet capture, filtering, and export.
 */
export class TrafficPage {
  readonly page: Page;
  
  // Panel elements
  readonly trafficPanel: Locator;
  readonly trafficToggle: Locator;
  
  // Capture controls
  readonly startCaptureButton: Locator;
  readonly stopCaptureButton: Locator;
  readonly clearButton: Locator;
  readonly captureStatus: Locator;
  
  // Packet list
  readonly packetList: Locator;
  readonly packetRows: Locator;
  
  // Packet details
  readonly packetDetails: Locator;
  readonly rawHexView: Locator;
  readonly decodedView: Locator;
  readonly parameterList: Locator;
  
  // Filter controls
  readonly directionFilter: Locator;
  readonly actionFilter: Locator;
  readonly searchInput: Locator;
  readonly clearFiltersButton: Locator;
  
  // View controls
  readonly rawViewButton: Locator;
  readonly decodedViewButton: Locator;
  readonly copyButton: Locator;
  
  // Export controls
  readonly exportButton: Locator;
  readonly exportJsonOption: Locator;
  readonly exportPcapOption: Locator;
  
  // Auto-scroll
  readonly autoScrollToggle: Locator;
  
  // Stats
  readonly packetRate: Locator;
  readonly packetCount: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Panel
    this.trafficPanel = page.locator('[data-testid="traffic-panel"]');
    this.trafficToggle = page.locator('[data-testid="traffic-toggle"]');
    
    // Capture controls
    this.startCaptureButton = page.locator('[data-testid="start-capture"]');
    this.stopCaptureButton = page.locator('[data-testid="stop-capture"]');
    this.clearButton = page.locator('[data-testid="clear-packets"]');
    this.captureStatus = page.locator('[data-testid="capture-status"]');
    
    // Packet list
    this.packetList = page.locator('[data-testid="packet-list"]');
    this.packetRows = page.locator('[data-testid="packet-row"]');
    
    // Packet details
    this.packetDetails = page.locator('[data-testid="packet-details"]');
    this.rawHexView = page.locator('[data-testid="raw-hex-view"]');
    this.decodedView = page.locator('[data-testid="decoded-view"]');
    this.parameterList = page.locator('[data-testid="parameter-list"]');
    
    // Filters
    this.directionFilter = page.locator('[data-testid="direction-filter"]');
    this.actionFilter = page.locator('[data-testid="action-filter"]');
    this.searchInput = page.locator('[data-testid="packet-search"]');
    this.clearFiltersButton = page.locator('[data-testid="clear-filters"]');
    
    // View controls
    this.rawViewButton = page.locator('[data-testid="raw-view-button"]');
    this.decodedViewButton = page.locator('[data-testid="decoded-view-button"]');
    this.copyButton = page.locator('[data-testid="copy-packet"]');
    
    // Export
    this.exportButton = page.locator('[data-testid="export-packets"]');
    this.exportJsonOption = page.locator('[data-testid="export-json"]');
    this.exportPcapOption = page.locator('[data-testid="export-pcap"]');
    
    // Auto-scroll
    this.autoScrollToggle = page.locator('[data-testid="auto-scroll"]');
    
    // Stats
    this.packetRate = page.locator('[data-testid="packet-rate"]');
    this.packetCount = page.locator('[data-testid="packet-count"]');
  }

  // Navigation
  async goto() {
    await this.page.goto('/');
  }

  async openTrafficPanel() {
    const isVisible = await this.trafficPanel.isVisible();
    if (!isVisible) {
      await this.trafficToggle.click();
      await this.trafficPanel.waitFor({ state: 'visible' });
    }
  }

  // Capture methods
  async startCapture() {
    await this.startCaptureButton.click();
  }

  async stopCapture() {
    await this.stopCaptureButton.click();
  }

  async clearPackets() {
    await this.clearButton.click();
  }

  // Packet methods
  async getPacketCount(): Promise<number> {
    return await this.packetRows.count();
  }

  getPacketRow(index: number): Locator {
    return this.packetRows.nth(index);
  }

  async selectPacket(index: number) {
    await this.getPacketRow(index).click();
  }

  async getAllPacketDirections(): Promise<string[]> {
    const directions: string[] = [];
    const count = await this.getPacketCount();
    for (let i = 0; i < count; i++) {
      const direction = await this.getPacketRow(i).locator('.direction').textContent();
      if (direction) directions.push(direction);
    }
    return directions;
  }

  async getAllPacketActions(): Promise<string[]> {
    const actions: string[] = [];
    const count = await this.getPacketCount();
    for (let i = 0; i < count; i++) {
      const action = await this.getPacketRow(i).locator('.action').textContent();
      if (action) actions.push(action);
    }
    return actions;
  }

  // Filter methods
  async setDirectionFilter(direction: 'all' | 'incoming' | 'outgoing') {
    await this.directionFilter.selectOption(direction);
  }

  async getDirectionFilter(): Promise<string> {
    return await this.directionFilter.inputValue();
  }

  async setActionFilter(action: string) {
    await this.actionFilter.selectOption(action);
  }

  async searchPackets(query: string) {
    await this.searchInput.fill(query);
  }

  async clearFilters() {
    await this.clearFiltersButton.click();
  }

  // View methods
  async showRawView() {
    await this.rawViewButton.click();
  }

  async showDecodedView() {
    await this.decodedViewButton.click();
  }

  async copyPacket() {
    await this.copyButton.click();
  }

  async getDecodedContent(): Promise<string | null> {
    return await this.decodedView.textContent();
  }

  async getPacketActionType(): Promise<string | null> {
    return await this.packetDetails.locator('.action-type').textContent();
  }

  // Export methods
  async exportAsJSON(): Promise<Download> {
    await this.exportButton.click();
    const downloadPromise = this.page.waitForEvent('download');
    await this.exportJsonOption.click();
    return await downloadPromise;
  }

  async exportAsPCAP(): Promise<Download> {
    await this.exportButton.click();
    const downloadPromise = this.page.waitForEvent('download');
    await this.exportPcapOption.click();
    return await downloadPromise;
  }

  // Auto-scroll methods
  async enableAutoScroll() {
    const isEnabled = await this.isAutoScrollEnabled();
    if (!isEnabled) {
      await this.autoScrollToggle.click();
    }
  }

  async disableAutoScroll() {
    const isEnabled = await this.isAutoScrollEnabled();
    if (isEnabled) {
      await this.autoScrollToggle.click();
    }
  }

  async isAutoScrollEnabled(): Promise<boolean> {
    return await this.autoScrollToggle.getAttribute('aria-checked') === 'true';
  }

  async isScrolledToBottom(): Promise<boolean> {
    return await this.packetList.evaluate((el) => {
      return Math.abs(el.scrollHeight - el.clientHeight - el.scrollTop) < 5;
    });
  }

  async scrollToTop() {
    await this.packetList.evaluate((el) => {
      el.scrollTop = 0;
    });
  }
}
