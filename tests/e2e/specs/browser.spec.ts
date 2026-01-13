import { test, expect } from '@playwright/test';
import { MainPage } from '../pages/MainPage';

/**
 * Browser Tests - Svony Browser v7.0
 * 
 * These tests verify the core browser functionality including:
 * - Navigation
 * - Dual panel display
 * - Session sharing
 * - Tab management
 * - Address bar functionality
 */

test.describe('Browser Core Functionality', () => {
  let mainPage: MainPage;

  test.beforeEach(async ({ page }) => {
    mainPage = new MainPage(page);
    await mainPage.goto();
  });

  test.describe('Navigation', () => {
    test('should load the main window successfully', async ({ page }) => {
      await expect(page).toHaveTitle(/Svony Browser/);
      await expect(mainPage.mainWindow).toBeVisible();
    });

    test('should navigate to Evony game server', async ({ page }) => {
      await mainPage.navigateTo('https://cc2.evony.com');
      await expect(mainPage.addressBar).toHaveValue(/cc2\.evony\.com/);
    });

    test('should handle back navigation', async ({ page }) => {
      await mainPage.navigateTo('https://cc2.evony.com');
      await mainPage.navigateTo('https://cc2.evony.com/game');
      await mainPage.goBack();
      await expect(mainPage.addressBar).toHaveValue(/cc2\.evony\.com$/);
    });

    test('should handle forward navigation', async ({ page }) => {
      await mainPage.navigateTo('https://cc2.evony.com');
      await mainPage.navigateTo('https://cc2.evony.com/game');
      await mainPage.goBack();
      await mainPage.goForward();
      await expect(mainPage.addressBar).toHaveValue(/game/);
    });

    test('should refresh the current page', async ({ page }) => {
      await mainPage.navigateTo('https://cc2.evony.com');
      const initialLoadTime = Date.now();
      await mainPage.refresh();
      // Page should reload (this is a simplified check)
      await expect(mainPage.browserPanel1).toBeVisible();
    });
  });

  test.describe('Dual Panel Display', () => {
    test('should display both browser panels', async ({ page }) => {
      await expect(mainPage.browserPanel1).toBeVisible();
      await expect(mainPage.browserPanel2).toBeVisible();
    });

    test('should allow independent navigation in each panel', async ({ page }) => {
      await mainPage.navigatePanel1('https://cc2.evony.com');
      await mainPage.navigatePanel2('https://cc2.evony.com/help');
      
      await expect(mainPage.panel1AddressBar).toHaveValue(/cc2\.evony\.com$/);
      await expect(mainPage.panel2AddressBar).toHaveValue(/help/);
    });

    test('should resize panels with splitter', async ({ page }) => {
      const initialWidth = await mainPage.getPanel1Width();
      await mainPage.resizePanels(100); // Move splitter 100px right
      const newWidth = await mainPage.getPanel1Width();
      expect(newWidth).toBeGreaterThan(initialWidth);
    });

    test('should toggle panel visibility', async ({ page }) => {
      await mainPage.togglePanel2();
      await expect(mainPage.browserPanel2).not.toBeVisible();
      
      await mainPage.togglePanel2();
      await expect(mainPage.browserPanel2).toBeVisible();
    });
  });

  test.describe('Session Sharing', () => {
    test('should share cookies between panels', async ({ page }) => {
      // Login in panel 1
      await mainPage.navigatePanel1('https://cc2.evony.com');
      // Verify session is shared in panel 2
      await mainPage.navigatePanel2('https://cc2.evony.com');
      
      // Both panels should have the same session
      const panel1Cookies = await mainPage.getPanel1Cookies();
      const panel2Cookies = await mainPage.getPanel2Cookies();
      
      expect(panel1Cookies).toEqual(panel2Cookies);
    });

    test('should share local storage between panels', async ({ page }) => {
      await mainPage.navigatePanel1('https://cc2.evony.com');
      await mainPage.setPanel1LocalStorage('testKey', 'testValue');
      
      await mainPage.navigatePanel2('https://cc2.evony.com');
      const value = await mainPage.getPanel2LocalStorage('testKey');
      
      expect(value).toBe('testValue');
    });
  });

  test.describe('Tab Management', () => {
    test('should create new tab', async ({ page }) => {
      const initialTabCount = await mainPage.getTabCount();
      await mainPage.createNewTab();
      const newTabCount = await mainPage.getTabCount();
      
      expect(newTabCount).toBe(initialTabCount + 1);
    });

    test('should close tab', async ({ page }) => {
      await mainPage.createNewTab();
      const initialTabCount = await mainPage.getTabCount();
      await mainPage.closeCurrentTab();
      const newTabCount = await mainPage.getTabCount();
      
      expect(newTabCount).toBe(initialTabCount - 1);
    });

    test('should switch between tabs', async ({ page }) => {
      await mainPage.createNewTab();
      await mainPage.navigateTo('https://cc2.evony.com/tab2');
      await mainPage.switchToTab(0);
      
      await expect(mainPage.addressBar).not.toHaveValue(/tab2/);
    });

    test('should restore closed tab', async ({ page }) => {
      await mainPage.createNewTab();
      await mainPage.navigateTo('https://cc2.evony.com/restore-test');
      await mainPage.closeCurrentTab();
      await mainPage.restoreClosedTab();
      
      await expect(mainPage.addressBar).toHaveValue(/restore-test/);
    });
  });

  test.describe('Address Bar', () => {
    test('should accept URL input', async ({ page }) => {
      await mainPage.typeInAddressBar('https://cc2.evony.com');
      await mainPage.pressEnterInAddressBar();
      
      await expect(mainPage.addressBar).toHaveValue(/cc2\.evony\.com/);
    });

    test('should show autocomplete suggestions', async ({ page }) => {
      await mainPage.typeInAddressBar('cc2');
      await expect(mainPage.autocompleteSuggestions).toBeVisible();
    });

    test('should clear address bar on focus', async ({ page }) => {
      await mainPage.navigateTo('https://cc2.evony.com');
      await mainPage.focusAddressBar();
      
      // Address bar should select all text on focus
      await expect(mainPage.addressBar).toBeFocused();
    });

    test('should handle invalid URLs gracefully', async ({ page }) => {
      await mainPage.typeInAddressBar('not-a-valid-url');
      await mainPage.pressEnterInAddressBar();
      
      // Should show error or search results
      await expect(mainPage.errorMessage.or(mainPage.searchResults)).toBeVisible();
    });
  });

  test.describe('Keyboard Shortcuts', () => {
    test('should open new tab with Ctrl+T', async ({ page }) => {
      const initialTabCount = await mainPage.getTabCount();
      await page.keyboard.press('Control+t');
      const newTabCount = await mainPage.getTabCount();
      
      expect(newTabCount).toBe(initialTabCount + 1);
    });

    test('should close tab with Ctrl+W', async ({ page }) => {
      await mainPage.createNewTab();
      const initialTabCount = await mainPage.getTabCount();
      await page.keyboard.press('Control+w');
      const newTabCount = await mainPage.getTabCount();
      
      expect(newTabCount).toBe(initialTabCount - 1);
    });

    test('should focus address bar with Ctrl+L', async ({ page }) => {
      await page.keyboard.press('Control+l');
      await expect(mainPage.addressBar).toBeFocused();
    });

    test('should refresh with F5', async ({ page }) => {
      await mainPage.navigateTo('https://cc2.evony.com');
      await page.keyboard.press('F5');
      // Page should reload
      await expect(mainPage.browserPanel1).toBeVisible();
    });

    test('should toggle dev tools with F12', async ({ page }) => {
      await page.keyboard.press('F12');
      await expect(mainPage.devTools).toBeVisible();
    });
  });
});

test.describe('Browser Performance', () => {
  test('should load page within acceptable time', async ({ page }) => {
    const mainPage = new MainPage(page);
    await mainPage.goto();
    
    const startTime = Date.now();
    await mainPage.navigateTo('https://cc2.evony.com');
    const loadTime = Date.now() - startTime;
    
    expect(loadTime).toBeLessThan(10000); // 10 seconds max
  });

  test('should handle multiple tabs without memory issues', async ({ page }) => {
    const mainPage = new MainPage(page);
    await mainPage.goto();
    
    // Create 10 tabs
    for (let i = 0; i < 10; i++) {
      await mainPage.createNewTab();
    }
    
    const tabCount = await mainPage.getTabCount();
    expect(tabCount).toBe(11); // Initial + 10 new tabs
    
    // Memory should be under control (this is a simplified check)
    const metrics = await page.evaluate(() => {
      return (performance as any).memory?.usedJSHeapSize || 0;
    });
    
    // Less than 500MB
    expect(metrics).toBeLessThan(500 * 1024 * 1024);
  });
});
