import { Page, Locator, Download } from '@playwright/test';

/**
 * SettingsPage Page Object - Svony Browser v7.0
 * 
 * This page object encapsulates all interactions with the Settings Control Center,
 * including category navigation, toggles, dropdowns, and persistence.
 */
export class SettingsPage {
  readonly page: Page;
  
  // Settings panel elements
  readonly settingsButton: Locator;
  readonly settingsPanel: Locator;
  readonly closeButton: Locator;
  
  // Category elements
  readonly categoryTabs: Locator;
  readonly categoryContent: Locator;
  
  // Search elements
  readonly searchInput: Locator;
  readonly searchResults: Locator;
  readonly clearSearchButton: Locator;
  
  // Reset elements
  readonly resetCategoryButton: Locator;
  readonly resetAllButton: Locator;
  readonly confirmDialog: Locator;
  readonly confirmButton: Locator;
  readonly cancelButton: Locator;
  
  // Export/Import elements
  readonly exportButton: Locator;
  readonly importButton: Locator;
  readonly fileInput: Locator;
  
  // Accessibility elements
  readonly liveRegion: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Settings panel
    this.settingsButton = page.locator('[data-testid="settings-button"]');
    this.settingsPanel = page.locator('[data-testid="settings-panel"]');
    this.closeButton = page.locator('[data-testid="settings-close"]');
    
    // Categories
    this.categoryTabs = page.locator('[data-testid="category-tabs"]');
    this.categoryContent = page.locator('[data-testid="category-content"]');
    
    // Search
    this.searchInput = page.locator('[data-testid="settings-search"]');
    this.searchResults = page.locator('[data-testid="search-results"]');
    this.clearSearchButton = page.locator('[data-testid="clear-search"]');
    
    // Reset
    this.resetCategoryButton = page.locator('[data-testid="reset-category"]');
    this.resetAllButton = page.locator('[data-testid="reset-all"]');
    this.confirmDialog = page.locator('[data-testid="confirm-dialog"]');
    this.confirmButton = page.locator('[data-testid="confirm-button"]');
    this.cancelButton = page.locator('[data-testid="cancel-button"]');
    
    // Export/Import
    this.exportButton = page.locator('[data-testid="export-settings"]');
    this.importButton = page.locator('[data-testid="import-settings"]');
    this.fileInput = page.locator('[data-testid="settings-file-input"]');
    
    // Accessibility
    this.liveRegion = page.locator('[aria-live="polite"]');
  }

  // Navigation
  async goto() {
    await this.page.goto('/');
  }

  async openSettings() {
    await this.settingsButton.click();
    await this.settingsPanel.waitFor({ state: 'visible' });
  }

  async closeSettings() {
    await this.closeButton.click();
    await this.settingsPanel.waitFor({ state: 'hidden' });
  }

  // Category methods
  getCategoryTab(name: string): Locator {
    return this.categoryTabs.locator(`[data-category="${name}"]`);
  }

  async selectCategory(name: string) {
    await this.getCategoryTab(name).click();
  }

  // Setting methods
  getSetting(name: string): Locator {
    return this.page.locator(`[data-setting="${name}"]`);
  }

  getSettingDescription(name: string): Locator {
    return this.page.locator(`[data-setting="${name}"] .description`);
  }

  // Toggle methods
  async toggleSetting(name: string) {
    await this.page.locator(`[data-setting="${name}"] [data-testid="toggle"]`).click();
  }

  async getToggleState(name: string): Promise<boolean> {
    const toggle = this.page.locator(`[data-setting="${name}"] [data-testid="toggle"]`);
    return await toggle.getAttribute('aria-checked') === 'true';
  }

  // Dropdown methods
  async selectDropdownOption(name: string, value: string) {
    await this.page.locator(`[data-setting="${name}"] select`).selectOption(value);
  }

  async getDropdownValue(name: string): Promise<string> {
    return await this.page.locator(`[data-setting="${name}"] select`).inputValue();
  }

  async getDropdownOptions(name: string): Promise<string[]> {
    const options = await this.page.locator(`[data-setting="${name}"] select option`).allTextContents();
    return options;
  }

  // Text input methods
  async setTextInput(name: string, value: string) {
    await this.page.locator(`[data-setting="${name}"] input[type="text"]`).fill(value);
  }

  async getTextInputValue(name: string): Promise<string> {
    return await this.page.locator(`[data-setting="${name}"] input[type="text"]`).inputValue();
  }

  getInputError(name: string): Locator {
    return this.page.locator(`[data-setting="${name}"] .error-message`);
  }

  async getInputPlaceholder(name: string): Promise<string | null> {
    return await this.page.locator(`[data-setting="${name}"] input`).getAttribute('placeholder');
  }

  // Slider methods
  async setSliderValue(name: string, value: number) {
    const slider = this.page.locator(`[data-setting="${name}"] input[type="range"]`);
    await slider.fill(value.toString());
  }

  async getSliderValue(name: string): Promise<number> {
    const value = await this.page.locator(`[data-setting="${name}"] input[type="range"]`).inputValue();
    return parseFloat(value);
  }

  getSliderLabel(name: string): Locator {
    return this.page.locator(`[data-setting="${name}"] .slider-value`);
  }

  // Search methods
  async searchSettings(query: string) {
    await this.searchInput.fill(query);
  }

  async clearSearch() {
    await this.clearSearchButton.click();
  }

  async getSearchHighlights(): Promise<Locator[]> {
    const highlights = await this.page.locator('.search-highlight').all();
    return highlights;
  }

  async clickSearchResult(settingName: string) {
    await this.searchResults.locator(`[data-setting="${settingName}"]`).click();
  }

  // Reset methods
  async resetCategory(name: string) {
    await this.selectCategory(name);
    await this.resetCategoryButton.click();
    await this.confirmButton.click();
  }

  async resetAllSettings() {
    await this.resetAllButton.click();
    await this.confirmButton.click();
  }

  async clickResetAll() {
    await this.resetAllButton.click();
  }

  // Export/Import methods
  async exportSettings(): Promise<Download> {
    const downloadPromise = this.page.waitForEvent('download');
    await this.exportButton.click();
    return await downloadPromise;
  }

  async importSettings(settingsJson: string) {
    // Create a temporary file with settings
    await this.page.evaluate((json) => {
      const blob = new Blob([json], { type: 'application/json' });
      const file = new File([blob], 'settings.json', { type: 'application/json' });
      const dataTransfer = new DataTransfer();
      dataTransfer.items.add(file);
      
      const input = document.querySelector('[data-testid="settings-file-input"]') as HTMLInputElement;
      if (input) {
        input.files = dataTransfer.files;
        input.dispatchEvent(new Event('change', { bubbles: true }));
      }
    }, settingsJson);
  }
}
