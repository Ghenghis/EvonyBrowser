import { Page, Locator, expect } from '@playwright/test';

/**
 * MainPage Page Object - Svony Browser v7.0
 * 
 * This page object encapsulates all interactions with the main browser window,
 * including navigation, dual panels, tabs, and address bar.
 */
export class MainPage {
  readonly page: Page;
  
  // Main window elements
  readonly mainWindow: Locator;
  readonly addressBar: Locator;
  readonly navButtons: Locator;
  readonly backButton: Locator;
  readonly forwardButton: Locator;
  readonly refreshButton: Locator;
  readonly settingsButton: Locator;
  
  // Browser panels
  readonly browserPanel1: Locator;
  readonly browserPanel2: Locator;
  readonly panel1AddressBar: Locator;
  readonly panel2AddressBar: Locator;
  readonly panelSplitter: Locator;
  readonly panel2ToggleButton: Locator;
  
  // Tab bar
  readonly tabBar: Locator;
  readonly newTabButton: Locator;
  readonly activeTab: Locator;
  
  // Side panel
  readonly sidePanel: Locator;
  readonly chatbotToggle: Locator;
  readonly trafficToggle: Locator;
  readonly protocolToggle: Locator;
  
  // Status bar
  readonly statusBar: Locator;
  
  // Dialogs and overlays
  readonly devTools: Locator;
  readonly errorMessage: Locator;
  readonly searchResults: Locator;
  readonly autocompleteSuggestions: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Main window
    this.mainWindow = page.locator('[data-testid="main-window"]');
    this.addressBar = page.locator('[data-testid="address-bar"]');
    this.navButtons = page.locator('[data-testid="nav-buttons"]');
    this.backButton = page.locator('[data-testid="back-button"]');
    this.forwardButton = page.locator('[data-testid="forward-button"]');
    this.refreshButton = page.locator('[data-testid="refresh-button"]');
    this.settingsButton = page.locator('[data-testid="settings-button"]');
    
    // Browser panels
    this.browserPanel1 = page.locator('[data-testid="browser-panel-1"]');
    this.browserPanel2 = page.locator('[data-testid="browser-panel-2"]');
    this.panel1AddressBar = page.locator('[data-testid="panel-1-address-bar"]');
    this.panel2AddressBar = page.locator('[data-testid="panel-2-address-bar"]');
    this.panelSplitter = page.locator('[data-testid="panel-splitter"]');
    this.panel2ToggleButton = page.locator('[data-testid="panel-2-toggle"]');
    
    // Tab bar
    this.tabBar = page.locator('[data-testid="tab-bar"]');
    this.newTabButton = page.locator('[data-testid="new-tab-button"]');
    this.activeTab = page.locator('[data-testid="tab"].active');
    
    // Side panel
    this.sidePanel = page.locator('[data-testid="side-panel"]');
    this.chatbotToggle = page.locator('[data-testid="chatbot-toggle"]');
    this.trafficToggle = page.locator('[data-testid="traffic-toggle"]');
    this.protocolToggle = page.locator('[data-testid="protocol-toggle"]');
    
    // Status bar
    this.statusBar = page.locator('[data-testid="status-bar"]');
    
    // Dialogs
    this.devTools = page.locator('[data-testid="dev-tools"]');
    this.errorMessage = page.locator('[data-testid="error-message"]');
    this.searchResults = page.locator('[data-testid="search-results"]');
    this.autocompleteSuggestions = page.locator('[data-testid="autocomplete-suggestions"]');
  }

  // Navigation methods
  async goto() {
    await this.page.goto('/');
    await this.mainWindow.waitFor({ state: 'visible' });
  }

  async navigateTo(url: string) {
    await this.addressBar.fill(url);
    await this.page.keyboard.press('Enter');
    await this.page.waitForLoadState('networkidle');
  }

  async navigatePanel1(url: string) {
    await this.panel1AddressBar.fill(url);
    await this.page.keyboard.press('Enter');
  }

  async navigatePanel2(url: string) {
    await this.panel2AddressBar.fill(url);
    await this.page.keyboard.press('Enter');
  }

  async goBack() {
    await this.backButton.click();
    await this.page.waitForLoadState('networkidle');
  }

  async goForward() {
    await this.forwardButton.click();
    await this.page.waitForLoadState('networkidle');
  }

  async refresh() {
    await this.refreshButton.click();
    await this.page.waitForLoadState('networkidle');
  }

  // Panel methods
  async togglePanel2() {
    await this.panel2ToggleButton.click();
  }

  async resizePanels(deltaX: number) {
    const splitterBox = await this.panelSplitter.boundingBox();
    if (splitterBox) {
      await this.page.mouse.move(
        splitterBox.x + splitterBox.width / 2,
        splitterBox.y + splitterBox.height / 2
      );
      await this.page.mouse.down();
      await this.page.mouse.move(
        splitterBox.x + splitterBox.width / 2 + deltaX,
        splitterBox.y + splitterBox.height / 2
      );
      await this.page.mouse.up();
    }
  }

  async getPanel1Width(): Promise<number> {
    const box = await this.browserPanel1.boundingBox();
    return box?.width || 0;
  }

  // Tab methods
  async createNewTab() {
    await this.newTabButton.click();
  }

  async closeCurrentTab() {
    await this.activeTab.locator('[data-testid="close-tab"]').click();
  }

  async switchToTab(index: number) {
    await this.tabBar.locator(`[data-testid="tab"]:nth-child(${index + 1})`).click();
  }

  async restoreClosedTab() {
    await this.page.keyboard.press('Control+Shift+t');
  }

  async getTabCount(): Promise<number> {
    return await this.tabBar.locator('[data-testid="tab"]').count();
  }

  // Address bar methods
  async typeInAddressBar(text: string) {
    await this.addressBar.click();
    await this.addressBar.fill(text);
  }

  async pressEnterInAddressBar() {
    await this.addressBar.press('Enter');
  }

  async focusAddressBar() {
    await this.addressBar.click();
  }

  // Cookie and storage methods
  async getPanel1Cookies(): Promise<any[]> {
    return await this.page.context().cookies();
  }

  async getPanel2Cookies(): Promise<any[]> {
    return await this.page.context().cookies();
  }

  async setPanel1LocalStorage(key: string, value: string) {
    await this.page.evaluate(([k, v]) => {
      localStorage.setItem(k, v);
    }, [key, value]);
  }

  async getPanel2LocalStorage(key: string): Promise<string | null> {
    return await this.page.evaluate((k) => {
      return localStorage.getItem(k);
    }, key);
  }

  // Settings methods
  async openSettings() {
    await this.settingsButton.click();
  }
}
