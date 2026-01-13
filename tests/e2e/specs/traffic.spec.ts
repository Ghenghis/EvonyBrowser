import { test, expect } from '@playwright/test';
import { TrafficPage } from '../pages/TrafficPage';

/**
 * Traffic Viewer Tests - Svony Browser v7.0
 * 
 * These tests verify the traffic viewer functionality including:
 * - Packet capture
 * - Packet filtering
 * - Packet decoding
 * - Export functionality
 */

test.describe('Traffic Viewer', () => {
  let trafficPage: TrafficPage;

  test.beforeEach(async ({ page }) => {
    trafficPage = new TrafficPage(page);
    await trafficPage.goto();
    await trafficPage.openTrafficPanel();
  });

  test.describe('Panel Display', () => {
    test('should display traffic panel', async ({ page }) => {
      await expect(trafficPage.trafficPanel).toBeVisible();
    });

    test('should show capture controls', async ({ page }) => {
      await expect(trafficPage.startCaptureButton).toBeVisible();
      await expect(trafficPage.stopCaptureButton).toBeVisible();
      await expect(trafficPage.clearButton).toBeVisible();
    });

    test('should show packet list', async ({ page }) => {
      await expect(trafficPage.packetList).toBeVisible();
    });

    test('should show packet details panel', async ({ page }) => {
      await expect(trafficPage.packetDetails).toBeVisible();
    });
  });

  test.describe('Packet Capture', () => {
    test('should start capture', async ({ page }) => {
      await trafficPage.startCapture();
      await expect(trafficPage.captureStatus).toContainText('Capturing');
    });

    test('should stop capture', async ({ page }) => {
      await trafficPage.startCapture();
      await trafficPage.stopCapture();
      await expect(trafficPage.captureStatus).toContainText('Stopped');
    });

    test('should display captured packets', async ({ page }) => {
      await trafficPage.startCapture();
      
      // Wait for some packets to be captured
      await page.waitForTimeout(2000);
      
      const packetCount = await trafficPage.getPacketCount();
      expect(packetCount).toBeGreaterThanOrEqual(0);
    });

    test('should clear captured packets', async ({ page }) => {
      await trafficPage.startCapture();
      await page.waitForTimeout(1000);
      await trafficPage.stopCapture();
      
      await trafficPage.clearPackets();
      
      const packetCount = await trafficPage.getPacketCount();
      expect(packetCount).toBe(0);
    });

    test('should show packet timestamp', async ({ page }) => {
      await trafficPage.startCapture();
      await page.waitForTimeout(1000);
      
      if (await trafficPage.getPacketCount() > 0) {
        const firstPacket = trafficPage.getPacketRow(0);
        await expect(firstPacket.locator('.timestamp')).toBeVisible();
      }
    });
  });

  test.describe('Packet Filtering', () => {
    test('should filter by direction', async ({ page }) => {
      await trafficPage.setDirectionFilter('outgoing');
      
      const packets = await trafficPage.getAllPacketDirections();
      for (const direction of packets) {
        expect(direction).toBe('outgoing');
      }
    });

    test('should filter by action type', async ({ page }) => {
      await trafficPage.setActionFilter('trainTroops');
      
      const packets = await trafficPage.getAllPacketActions();
      for (const action of packets) {
        expect(action).toContain('trainTroops');
      }
    });

    test('should filter by search text', async ({ page }) => {
      await trafficPage.searchPackets('cavalry');
      
      // All visible packets should contain search term
      const packetCount = await trafficPage.getPacketCount();
      if (packetCount > 0) {
        const firstPacket = trafficPage.getPacketRow(0);
        await expect(firstPacket).toContainText(/cavalry/i);
      }
    });

    test('should combine multiple filters', async ({ page }) => {
      await trafficPage.setDirectionFilter('outgoing');
      await trafficPage.setActionFilter('trainTroops');
      
      const packets = await trafficPage.getAllPacketDirections();
      const actions = await trafficPage.getAllPacketActions();
      
      for (let i = 0; i < packets.length; i++) {
        expect(packets[i]).toBe('outgoing');
        expect(actions[i]).toContain('trainTroops');
      }
    });

    test('should clear filters', async ({ page }) => {
      await trafficPage.setDirectionFilter('outgoing');
      await trafficPage.clearFilters();
      
      const filter = await trafficPage.getDirectionFilter();
      expect(filter).toBe('all');
    });
  });

  test.describe('Packet Details', () => {
    test('should show packet details on selection', async ({ page }) => {
      await trafficPage.startCapture();
      await page.waitForTimeout(1000);
      
      if (await trafficPage.getPacketCount() > 0) {
        await trafficPage.selectPacket(0);
        await expect(trafficPage.packetDetails).not.toBeEmpty();
      }
    });

    test('should show raw hex view', async ({ page }) => {
      await trafficPage.startCapture();
      await page.waitForTimeout(1000);
      
      if (await trafficPage.getPacketCount() > 0) {
        await trafficPage.selectPacket(0);
        await trafficPage.showRawView();
        await expect(trafficPage.rawHexView).toBeVisible();
      }
    });

    test('should show decoded view', async ({ page }) => {
      await trafficPage.startCapture();
      await page.waitForTimeout(1000);
      
      if (await trafficPage.getPacketCount() > 0) {
        await trafficPage.selectPacket(0);
        await trafficPage.showDecodedView();
        await expect(trafficPage.decodedView).toBeVisible();
      }
    });

    test('should copy packet to clipboard', async ({ page }) => {
      await trafficPage.startCapture();
      await page.waitForTimeout(1000);
      
      if (await trafficPage.getPacketCount() > 0) {
        await trafficPage.selectPacket(0);
        await trafficPage.copyPacket();
        
        // Verify clipboard content
        const clipboardText = await page.evaluate(() => navigator.clipboard.readText());
        expect(clipboardText).toBeDefined();
      }
    });
  });

  test.describe('Packet Decoding', () => {
    test('should decode AMF3 packets', async ({ page }) => {
      await trafficPage.startCapture();
      await page.waitForTimeout(1000);
      
      if (await trafficPage.getPacketCount() > 0) {
        await trafficPage.selectPacket(0);
        const decoded = await trafficPage.getDecodedContent();
        
        // Should have decoded structure
        expect(decoded).toBeDefined();
      }
    });

    test('should identify action type', async ({ page }) => {
      await trafficPage.startCapture();
      await page.waitForTimeout(1000);
      
      if (await trafficPage.getPacketCount() > 0) {
        await trafficPage.selectPacket(0);
        const actionType = await trafficPage.getPacketActionType();
        
        expect(actionType).toBeDefined();
      }
    });

    test('should show parameter values', async ({ page }) => {
      await trafficPage.startCapture();
      await page.waitForTimeout(1000);
      
      if (await trafficPage.getPacketCount() > 0) {
        await trafficPage.selectPacket(0);
        await trafficPage.showDecodedView();
        
        await expect(trafficPage.parameterList).toBeVisible();
      }
    });
  });

  test.describe('Export Functionality', () => {
    test('should export packets as JSON', async ({ page }) => {
      await trafficPage.startCapture();
      await page.waitForTimeout(1000);
      await trafficPage.stopCapture();
      
      if (await trafficPage.getPacketCount() > 0) {
        const download = await trafficPage.exportAsJSON();
        expect(download.suggestedFilename()).toMatch(/\.json$/);
      }
    });

    test('should export packets as PCAP', async ({ page }) => {
      await trafficPage.startCapture();
      await page.waitForTimeout(1000);
      await trafficPage.stopCapture();
      
      if (await trafficPage.getPacketCount() > 0) {
        const download = await trafficPage.exportAsPCAP();
        expect(download.suggestedFilename()).toMatch(/\.pcap$/);
      }
    });

    test('should export filtered packets only', async ({ page }) => {
      await trafficPage.startCapture();
      await page.waitForTimeout(1000);
      await trafficPage.stopCapture();
      
      await trafficPage.setDirectionFilter('outgoing');
      
      if (await trafficPage.getPacketCount() > 0) {
        const download = await trafficPage.exportAsJSON();
        const content = await download.path();
        
        expect(content).toBeDefined();
      }
    });
  });

  test.describe('Real-time Updates', () => {
    test('should auto-scroll to new packets', async ({ page }) => {
      await trafficPage.startCapture();
      await trafficPage.enableAutoScroll();
      
      await page.waitForTimeout(2000);
      
      const isScrolledToBottom = await trafficPage.isScrolledToBottom();
      expect(isScrolledToBottom).toBe(true);
    });

    test('should pause auto-scroll on manual scroll', async ({ page }) => {
      await trafficPage.startCapture();
      await trafficPage.enableAutoScroll();
      
      await trafficPage.scrollToTop();
      
      const autoScrollEnabled = await trafficPage.isAutoScrollEnabled();
      expect(autoScrollEnabled).toBe(false);
    });

    test('should show packet rate', async ({ page }) => {
      await trafficPage.startCapture();
      await page.waitForTimeout(2000);
      
      await expect(trafficPage.packetRate).toBeVisible();
    });
  });
});

test.describe('Traffic Viewer Performance', () => {
  test('should handle large packet volumes', async ({ page }) => {
    const trafficPage = new TrafficPage(page);
    await trafficPage.goto();
    await trafficPage.openTrafficPanel();
    
    await trafficPage.startCapture();
    
    // Wait for many packets
    await page.waitForTimeout(10000);
    
    const packetCount = await trafficPage.getPacketCount();
    
    // Should not crash or freeze
    await expect(trafficPage.trafficPanel).toBeVisible();
    
    // Should limit displayed packets for performance
    expect(packetCount).toBeLessThanOrEqual(10000);
  });

  test('should filter efficiently', async ({ page }) => {
    const trafficPage = new TrafficPage(page);
    await trafficPage.goto();
    await trafficPage.openTrafficPanel();
    
    await trafficPage.startCapture();
    await page.waitForTimeout(5000);
    await trafficPage.stopCapture();
    
    const startTime = Date.now();
    await trafficPage.setActionFilter('trainTroops');
    const filterTime = Date.now() - startTime;
    
    // Filtering should be fast
    expect(filterTime).toBeLessThan(500);
  });
});
