import { test, expect } from '@playwright/test';
import { SettingsPage } from '../pages/SettingsPage';

/**
 * Settings Tests - Svony Browser v7.0
 * 
 * These tests verify the Settings Control Center functionality including:
 * - Category navigation
 * - Toggle switches
 * - Dropdown selections
 * - Settings persistence
 * - Reset functionality
 */

test.describe('Settings Control Center', () => {
  let settingsPage: SettingsPage;

  test.beforeEach(async ({ page }) => {
    settingsPage = new SettingsPage(page);
    await settingsPage.goto();
    await settingsPage.openSettings();
  });

  test.describe('Settings Panel', () => {
    test('should open settings panel', async ({ page }) => {
      await expect(settingsPage.settingsPanel).toBeVisible();
    });

    test('should close settings with X button', async ({ page }) => {
      await settingsPage.closeSettings();
      await expect(settingsPage.settingsPanel).not.toBeVisible();
    });

    test('should close settings with Escape key', async ({ page }) => {
      await page.keyboard.press('Escape');
      await expect(settingsPage.settingsPanel).not.toBeVisible();
    });

    test('should display all setting categories', async ({ page }) => {
      const categories = [
        'General', 'Browser', 'Proxy', 'MCP', 'LLM', 'Fiddler',
        'Automation', 'Traffic', 'Chatbot', 'Status Bar', 'Webhooks', 'Advanced'
      ];

      for (const category of categories) {
        await expect(settingsPage.getCategoryTab(category)).toBeVisible();
      }
    });
  });

  test.describe('Category Navigation', () => {
    test('should switch between categories', async ({ page }) => {
      await settingsPage.selectCategory('Browser');
      await expect(settingsPage.categoryContent).toContainText('Browser');

      await settingsPage.selectCategory('MCP');
      await expect(settingsPage.categoryContent).toContainText('MCP');
    });

    test('should highlight active category', async ({ page }) => {
      await settingsPage.selectCategory('LLM');
      await expect(settingsPage.getCategoryTab('LLM')).toHaveClass(/active/);
    });

    test('should show category-specific settings', async ({ page }) => {
      await settingsPage.selectCategory('Automation');
      await expect(settingsPage.getSetting('autopilotEnabled')).toBeVisible();
      await expect(settingsPage.getSetting('safetyLimits')).toBeVisible();
    });
  });

  test.describe('Toggle Switches', () => {
    test('should toggle boolean settings', async ({ page }) => {
      await settingsPage.selectCategory('General');
      
      const initialState = await settingsPage.getToggleState('darkMode');
      await settingsPage.toggleSetting('darkMode');
      const newState = await settingsPage.getToggleState('darkMode');
      
      expect(newState).not.toBe(initialState);
    });

    test('should persist toggle state', async ({ page }) => {
      await settingsPage.selectCategory('General');
      await settingsPage.toggleSetting('showTimestamps');
      const state = await settingsPage.getToggleState('showTimestamps');
      
      await settingsPage.closeSettings();
      await settingsPage.openSettings();
      await settingsPage.selectCategory('General');
      
      const persistedState = await settingsPage.getToggleState('showTimestamps');
      expect(persistedState).toBe(state);
    });

    test('should show toggle description', async ({ page }) => {
      await settingsPage.selectCategory('Automation');
      await expect(settingsPage.getSettingDescription('autopilotEnabled')).toBeVisible();
    });
  });

  test.describe('Dropdown Selections', () => {
    test('should select dropdown option', async ({ page }) => {
      await settingsPage.selectCategory('General');
      await settingsPage.selectDropdownOption('theme', 'Borg Dark');
      
      const selected = await settingsPage.getDropdownValue('theme');
      expect(selected).toBe('Borg Dark');
    });

    test('should show all dropdown options', async ({ page }) => {
      await settingsPage.selectCategory('General');
      const options = await settingsPage.getDropdownOptions('theme');
      
      expect(options).toContain('Borg Dark');
      expect(options).toContain('Light');
      expect(options).toContain('Evony Classic');
    });

    test('should persist dropdown selection', async ({ page }) => {
      await settingsPage.selectCategory('General');
      await settingsPage.selectDropdownOption('language', 'English');
      
      await settingsPage.closeSettings();
      await settingsPage.openSettings();
      await settingsPage.selectCategory('General');
      
      const selected = await settingsPage.getDropdownValue('language');
      expect(selected).toBe('English');
    });
  });

  test.describe('Text Inputs', () => {
    test('should accept text input', async ({ page }) => {
      await settingsPage.selectCategory('MCP');
      await settingsPage.setTextInput('ragServerPort', '3001');
      
      const value = await settingsPage.getTextInputValue('ragServerPort');
      expect(value).toBe('3001');
    });

    test('should validate numeric inputs', async ({ page }) => {
      await settingsPage.selectCategory('MCP');
      await settingsPage.setTextInput('ragServerPort', 'invalid');
      
      await expect(settingsPage.getInputError('ragServerPort')).toBeVisible();
    });

    test('should show input placeholder', async ({ page }) => {
      await settingsPage.selectCategory('Webhooks');
      const placeholder = await settingsPage.getInputPlaceholder('discordWebhookUrl');
      
      expect(placeholder).toContain('https://');
    });
  });

  test.describe('Slider Controls', () => {
    test('should adjust slider value', async ({ page }) => {
      await settingsPage.selectCategory('LLM');
      await settingsPage.setSliderValue('temperature', 0.7);
      
      const value = await settingsPage.getSliderValue('temperature');
      expect(value).toBeCloseTo(0.7, 1);
    });

    test('should show slider current value', async ({ page }) => {
      await settingsPage.selectCategory('LLM');
      await settingsPage.setSliderValue('maxTokens', 2048);
      
      await expect(settingsPage.getSliderLabel('maxTokens')).toContainText('2048');
    });

    test('should respect slider min/max bounds', async ({ page }) => {
      await settingsPage.selectCategory('LLM');
      
      // Try to set beyond max
      await settingsPage.setSliderValue('temperature', 2.0);
      const value = await settingsPage.getSliderValue('temperature');
      
      expect(value).toBeLessThanOrEqual(1.0);
    });
  });

  test.describe('Settings Persistence', () => {
    test('should save settings on change', async ({ page }) => {
      await settingsPage.selectCategory('General');
      await settingsPage.toggleSetting('autoSave');
      
      // Reload page
      await page.reload();
      await settingsPage.openSettings();
      await settingsPage.selectCategory('General');
      
      const state = await settingsPage.getToggleState('autoSave');
      expect(state).toBe(true);
    });

    test('should export settings to file', async ({ page }) => {
      const download = await settingsPage.exportSettings();
      expect(download.suggestedFilename()).toMatch(/settings.*\.json$/);
    });

    test('should import settings from file', async ({ page }) => {
      // Create a test settings file
      const testSettings = JSON.stringify({
        general: { theme: 'Light', language: 'English' },
      });
      
      await settingsPage.importSettings(testSettings);
      await settingsPage.selectCategory('General');
      
      const theme = await settingsPage.getDropdownValue('theme');
      expect(theme).toBe('Light');
    });
  });

  test.describe('Reset Functionality', () => {
    test('should reset category to defaults', async ({ page }) => {
      await settingsPage.selectCategory('General');
      await settingsPage.selectDropdownOption('theme', 'Cyberpunk');
      
      await settingsPage.resetCategory('General');
      
      const theme = await settingsPage.getDropdownValue('theme');
      expect(theme).toBe('Borg Dark'); // Default theme
    });

    test('should reset all settings to defaults', async ({ page }) => {
      await settingsPage.selectCategory('General');
      await settingsPage.selectDropdownOption('theme', 'Light');
      
      await settingsPage.resetAllSettings();
      
      await settingsPage.selectCategory('General');
      const theme = await settingsPage.getDropdownValue('theme');
      expect(theme).toBe('Borg Dark');
    });

    test('should confirm before reset all', async ({ page }) => {
      await settingsPage.clickResetAll();
      await expect(settingsPage.confirmDialog).toBeVisible();
      await expect(settingsPage.confirmDialog).toContainText('Are you sure');
    });
  });

  test.describe('Search Functionality', () => {
    test('should search settings', async ({ page }) => {
      await settingsPage.searchSettings('theme');
      await expect(settingsPage.searchResults).toContainText('Theme');
    });

    test('should highlight matching settings', async ({ page }) => {
      await settingsPage.searchSettings('port');
      const highlights = await settingsPage.getSearchHighlights();
      
      expect(highlights.length).toBeGreaterThan(0);
    });

    test('should navigate to setting from search', async ({ page }) => {
      await settingsPage.searchSettings('webhook');
      await settingsPage.clickSearchResult('discordWebhookUrl');
      
      await expect(settingsPage.getCategoryTab('Webhooks')).toHaveClass(/active/);
    });

    test('should clear search', async ({ page }) => {
      await settingsPage.searchSettings('test');
      await settingsPage.clearSearch();
      
      await expect(settingsPage.searchInput).toHaveValue('');
    });
  });
});

test.describe('Settings Accessibility', () => {
  test('should navigate with keyboard', async ({ page }) => {
    const settingsPage = new SettingsPage(page);
    await settingsPage.goto();
    await settingsPage.openSettings();
    
    // Tab through categories
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');
    await page.keyboard.press('Enter');
    
    // Should have navigated to a category
    await expect(settingsPage.categoryContent).toBeVisible();
  });

  test('should have proper ARIA labels', async ({ page }) => {
    const settingsPage = new SettingsPage(page);
    await settingsPage.goto();
    await settingsPage.openSettings();
    
    // Check for ARIA labels
    await expect(settingsPage.settingsPanel).toHaveAttribute('role', 'dialog');
    await expect(settingsPage.categoryTabs).toHaveAttribute('role', 'tablist');
  });

  test('should announce changes to screen readers', async ({ page }) => {
    const settingsPage = new SettingsPage(page);
    await settingsPage.goto();
    await settingsPage.openSettings();
    
    await settingsPage.selectCategory('General');
    await settingsPage.toggleSetting('darkMode');
    
    // Check for live region announcement
    await expect(settingsPage.liveRegion).toContainText(/saved|updated/i);
  });
});
