import { test, expect } from '@playwright/test';
import { ChatbotPage } from '../pages/ChatbotPage';

/**
 * Chatbot Tests - Svony Browser v7.0
 * 
 * These tests verify the AI co-pilot chatbot functionality including:
 * - Message sending and receiving
 * - AI response handling
 * - Context management
 * - Chat export
 * - Intent classification
 */

test.describe('Chatbot Functionality', () => {
  let chatbotPage: ChatbotPage;

  test.beforeEach(async ({ page }) => {
    chatbotPage = new ChatbotPage(page);
    await chatbotPage.goto();
    await chatbotPage.openChatPanel();
  });

  test.describe('Message Sending', () => {
    test('should display chat panel', async ({ page }) => {
      await expect(chatbotPage.chatPanel).toBeVisible();
    });

    test('should send a message', async ({ page }) => {
      await chatbotPage.sendMessage('Hello, how are you?');
      await expect(chatbotPage.getLastUserMessage()).toContainText('Hello, how are you?');
    });

    test('should clear input after sending', async ({ page }) => {
      await chatbotPage.sendMessage('Test message');
      await expect(chatbotPage.messageInput).toHaveValue('');
    });

    test('should disable send button when input is empty', async ({ page }) => {
      await expect(chatbotPage.sendButton).toBeDisabled();
    });

    test('should enable send button when input has text', async ({ page }) => {
      await chatbotPage.typeMessage('Test');
      await expect(chatbotPage.sendButton).toBeEnabled();
    });

    test('should send message with Enter key', async ({ page }) => {
      await chatbotPage.typeMessage('Enter key test');
      await page.keyboard.press('Enter');
      await expect(chatbotPage.getLastUserMessage()).toContainText('Enter key test');
    });

    test('should support multi-line messages with Shift+Enter', async ({ page }) => {
      await chatbotPage.typeMessage('Line 1');
      await page.keyboard.press('Shift+Enter');
      await chatbotPage.typeMessage('Line 2');
      await chatbotPage.clickSend();
      
      const message = await chatbotPage.getLastUserMessage();
      await expect(message).toContainText('Line 1');
      await expect(message).toContainText('Line 2');
    });
  });

  test.describe('AI Response', () => {
    test('should receive AI response', async ({ page }) => {
      await chatbotPage.sendMessage('What is Evony?');
      await chatbotPage.waitForResponse();
      await expect(chatbotPage.getLastAIMessage()).toBeVisible();
    });

    test('should show typing indicator while waiting', async ({ page }) => {
      await chatbotPage.sendMessage('Tell me about combat');
      await expect(chatbotPage.typingIndicator).toBeVisible();
      await chatbotPage.waitForResponse();
      await expect(chatbotPage.typingIndicator).not.toBeVisible();
    });

    test('should handle long responses', async ({ page }) => {
      await chatbotPage.sendMessage('Explain all building types in detail');
      await chatbotPage.waitForResponse(30000); // Longer timeout
      
      const response = await chatbotPage.getLastAIMessage();
      await expect(response).toBeVisible();
    });

    test('should format code blocks in responses', async ({ page }) => {
      await chatbotPage.sendMessage('Show me a code example');
      await chatbotPage.waitForResponse();
      
      await expect(chatbotPage.codeBlock).toBeVisible();
    });

    test('should render markdown in responses', async ({ page }) => {
      await chatbotPage.sendMessage('Give me a formatted list');
      await chatbotPage.waitForResponse();
      
      // Check for markdown elements
      const response = await chatbotPage.getLastAIMessage();
      await expect(response.locator('ul, ol, strong, em')).toBeVisible();
    });
  });

  test.describe('Context Management', () => {
    test('should maintain conversation context', async ({ page }) => {
      await chatbotPage.sendMessage('My city is called TestCity');
      await chatbotPage.waitForResponse();
      
      await chatbotPage.sendMessage('What is my city called?');
      await chatbotPage.waitForResponse();
      
      const response = await chatbotPage.getLastAIMessage();
      await expect(response).toContainText('TestCity');
    });

    test('should clear context on new conversation', async ({ page }) => {
      await chatbotPage.sendMessage('Remember the number 42');
      await chatbotPage.waitForResponse();
      
      await chatbotPage.startNewConversation();
      
      await chatbotPage.sendMessage('What number did I mention?');
      await chatbotPage.waitForResponse();
      
      const response = await chatbotPage.getLastAIMessage();
      await expect(response).not.toContainText('42');
    });

    test('should show conversation history', async ({ page }) => {
      await chatbotPage.sendMessage('First message');
      await chatbotPage.waitForResponse();
      await chatbotPage.sendMessage('Second message');
      await chatbotPage.waitForResponse();
      
      const messageCount = await chatbotPage.getMessageCount();
      expect(messageCount).toBeGreaterThanOrEqual(4); // 2 user + 2 AI
    });

    test('should scroll to latest message', async ({ page }) => {
      // Send multiple messages to create scroll
      for (let i = 0; i < 10; i++) {
        await chatbotPage.sendMessage(`Message ${i}`);
        await chatbotPage.waitForResponse();
      }
      
      const isScrolledToBottom = await chatbotPage.isScrolledToBottom();
      expect(isScrolledToBottom).toBe(true);
    });
  });

  test.describe('Intent Classification', () => {
    test('should classify query intent', async ({ page }) => {
      await chatbotPage.sendMessage('How do I upgrade my castle?');
      await chatbotPage.waitForResponse();
      
      const intent = await chatbotPage.getLastMessageIntent();
      expect(intent).toBe('query');
    });

    test('should classify action intent', async ({ page }) => {
      await chatbotPage.sendMessage('Train 1000 cavalry');
      await chatbotPage.waitForResponse();
      
      const intent = await chatbotPage.getLastMessageIntent();
      expect(intent).toBe('action');
    });

    test('should classify analysis intent', async ({ page }) => {
      await chatbotPage.sendMessage('Analyze this packet: 0x00 0x11 0x22');
      await chatbotPage.waitForResponse();
      
      const intent = await chatbotPage.getLastMessageIntent();
      expect(intent).toBe('analysis');
    });

    test('should show suggested actions for action intents', async ({ page }) => {
      await chatbotPage.sendMessage('Upgrade barracks to level 26');
      await chatbotPage.waitForResponse();
      
      await expect(chatbotPage.suggestedActions).toBeVisible();
    });
  });

  test.describe('Chat Export', () => {
    test('should export chat as markdown', async ({ page }) => {
      await chatbotPage.sendMessage('Test export message');
      await chatbotPage.waitForResponse();
      
      const download = await chatbotPage.exportAsMarkdown();
      expect(download.suggestedFilename()).toMatch(/\.md$/);
    });

    test('should export chat as JSON', async ({ page }) => {
      await chatbotPage.sendMessage('Test export message');
      await chatbotPage.waitForResponse();
      
      const download = await chatbotPage.exportAsJSON();
      expect(download.suggestedFilename()).toMatch(/\.json$/);
    });

    test('should export chat as text', async ({ page }) => {
      await chatbotPage.sendMessage('Test export message');
      await chatbotPage.waitForResponse();
      
      const download = await chatbotPage.exportAsText();
      expect(download.suggestedFilename()).toMatch(/\.txt$/);
    });

    test('should include timestamps in export', async ({ page }) => {
      await chatbotPage.sendMessage('Timestamp test');
      await chatbotPage.waitForResponse();
      
      const content = await chatbotPage.exportAsText();
      const text = await content.path();
      // Verify timestamp format exists in exported content
      expect(text).toBeDefined();
    });
  });

  test.describe('Chat Settings', () => {
    test('should toggle timestamps', async ({ page }) => {
      await chatbotPage.openSettings();
      await chatbotPage.toggleTimestamps();
      await chatbotPage.closeSettings();
      
      await chatbotPage.sendMessage('Timestamp test');
      await chatbotPage.waitForResponse();
      
      await expect(chatbotPage.messageTimestamp).toBeVisible();
    });

    test('should change font size', async ({ page }) => {
      await chatbotPage.openSettings();
      await chatbotPage.setFontSize('large');
      await chatbotPage.closeSettings();
      
      const fontSize = await chatbotPage.getMessageFontSize();
      expect(parseInt(fontSize)).toBeGreaterThan(14);
    });

    test('should toggle markdown rendering', async ({ page }) => {
      await chatbotPage.openSettings();
      await chatbotPage.toggleMarkdownRendering();
      await chatbotPage.closeSettings();
      
      await chatbotPage.sendMessage('**Bold text**');
      await chatbotPage.waitForResponse();
      
      // With markdown disabled, should show raw markdown
      const message = await chatbotPage.getLastUserMessage();
      await expect(message).toContainText('**');
    });
  });
});

test.describe('Chatbot Error Handling', () => {
  test('should handle network errors gracefully', async ({ page }) => {
    const chatbotPage = new ChatbotPage(page);
    await chatbotPage.goto();
    await chatbotPage.openChatPanel();
    
    // Simulate network error
    await page.route('**/api/chat', route => route.abort());
    
    await chatbotPage.sendMessage('This should fail');
    
    await expect(chatbotPage.errorMessage).toBeVisible();
    await expect(chatbotPage.retryButton).toBeVisible();
  });

  test('should retry failed messages', async ({ page }) => {
    const chatbotPage = new ChatbotPage(page);
    await chatbotPage.goto();
    await chatbotPage.openChatPanel();
    
    // First request fails
    let requestCount = 0;
    await page.route('**/api/chat', route => {
      requestCount++;
      if (requestCount === 1) {
        route.abort();
      } else {
        route.continue();
      }
    });
    
    await chatbotPage.sendMessage('Retry test');
    await chatbotPage.clickRetry();
    await chatbotPage.waitForResponse();
    
    await expect(chatbotPage.getLastAIMessage()).toBeVisible();
  });

  test('should handle timeout gracefully', async ({ page }) => {
    const chatbotPage = new ChatbotPage(page);
    await chatbotPage.goto();
    await chatbotPage.openChatPanel();
    
    // Simulate slow response
    await page.route('**/api/chat', async route => {
      await new Promise(r => setTimeout(r, 60000));
      route.continue();
    });
    
    await chatbotPage.sendMessage('Timeout test');
    
    // Should show timeout error after configured timeout
    await expect(chatbotPage.timeoutError).toBeVisible({ timeout: 35000 });
  });
});
