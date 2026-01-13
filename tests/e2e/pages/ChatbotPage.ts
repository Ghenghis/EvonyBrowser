import { Page, Locator, Download } from '@playwright/test';

/**
 * ChatbotPage Page Object - Svony Browser v7.0
 * 
 * This page object encapsulates all interactions with the AI chatbot panel,
 * including message sending, response handling, and chat management.
 */
export class ChatbotPage {
  readonly page: Page;
  
  // Chat panel elements
  readonly chatPanel: Locator;
  readonly chatToggleButton: Locator;
  readonly messageList: Locator;
  readonly messageInput: Locator;
  readonly sendButton: Locator;
  readonly typingIndicator: Locator;
  
  // Message elements
  readonly userMessages: Locator;
  readonly aiMessages: Locator;
  readonly messageTimestamp: Locator;
  readonly codeBlock: Locator;
  
  // Action elements
  readonly suggestedActions: Locator;
  readonly retryButton: Locator;
  readonly newConversationButton: Locator;
  
  // Settings elements
  readonly settingsButton: Locator;
  readonly settingsPanel: Locator;
  readonly timestampToggle: Locator;
  readonly fontSizeSelect: Locator;
  readonly markdownToggle: Locator;
  
  // Export elements
  readonly exportButton: Locator;
  readonly exportMarkdownOption: Locator;
  readonly exportJsonOption: Locator;
  readonly exportTextOption: Locator;
  
  // Error elements
  readonly errorMessage: Locator;
  readonly timeoutError: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Chat panel
    this.chatPanel = page.locator('[data-testid="chatbot-panel"]');
    this.chatToggleButton = page.locator('[data-testid="chatbot-toggle"]');
    this.messageList = page.locator('[data-testid="message-list"]');
    this.messageInput = page.locator('[data-testid="message-input"]');
    this.sendButton = page.locator('[data-testid="send-button"]');
    this.typingIndicator = page.locator('[data-testid="typing-indicator"]');
    
    // Messages
    this.userMessages = page.locator('[data-testid="user-message"]');
    this.aiMessages = page.locator('[data-testid="ai-message"]');
    this.messageTimestamp = page.locator('[data-testid="message-timestamp"]');
    this.codeBlock = page.locator('[data-testid="code-block"]');
    
    // Actions
    this.suggestedActions = page.locator('[data-testid="suggested-actions"]');
    this.retryButton = page.locator('[data-testid="retry-button"]');
    this.newConversationButton = page.locator('[data-testid="new-conversation"]');
    
    // Settings
    this.settingsButton = page.locator('[data-testid="chat-settings-button"]');
    this.settingsPanel = page.locator('[data-testid="chat-settings-panel"]');
    this.timestampToggle = page.locator('[data-testid="timestamp-toggle"]');
    this.fontSizeSelect = page.locator('[data-testid="font-size-select"]');
    this.markdownToggle = page.locator('[data-testid="markdown-toggle"]');
    
    // Export
    this.exportButton = page.locator('[data-testid="export-button"]');
    this.exportMarkdownOption = page.locator('[data-testid="export-markdown"]');
    this.exportJsonOption = page.locator('[data-testid="export-json"]');
    this.exportTextOption = page.locator('[data-testid="export-text"]');
    
    // Errors
    this.errorMessage = page.locator('[data-testid="error-message"]');
    this.timeoutError = page.locator('[data-testid="timeout-error"]');
  }

  // Navigation
  async goto() {
    await this.page.goto('/');
  }

  async openChatPanel() {
    const isVisible = await this.chatPanel.isVisible();
    if (!isVisible) {
      await this.chatToggleButton.click();
      await this.chatPanel.waitFor({ state: 'visible' });
    }
  }

  async closeChatPanel() {
    const isVisible = await this.chatPanel.isVisible();
    if (isVisible) {
      await this.chatToggleButton.click();
      await this.chatPanel.waitFor({ state: 'hidden' });
    }
  }

  // Message methods
  async sendMessage(message: string) {
    await this.messageInput.fill(message);
    await this.sendButton.click();
  }

  async typeMessage(message: string) {
    await this.messageInput.fill(message);
  }

  async clickSend() {
    await this.sendButton.click();
  }

  async waitForResponse(timeout: number = 15000) {
    await this.typingIndicator.waitFor({ state: 'visible', timeout: 5000 }).catch(() => {});
    await this.typingIndicator.waitFor({ state: 'hidden', timeout });
  }

  getLastUserMessage(): Locator {
    return this.userMessages.last();
  }

  getLastAIMessage(): Locator {
    return this.aiMessages.last();
  }

  async getMessageCount(): Promise<number> {
    const userCount = await this.userMessages.count();
    const aiCount = await this.aiMessages.count();
    return userCount + aiCount;
  }

  async getLastMessageIntent(): Promise<string> {
    const lastMessage = this.aiMessages.last();
    return await lastMessage.getAttribute('data-intent') || 'unknown';
  }

  // Scroll methods
  async isScrolledToBottom(): Promise<boolean> {
    return await this.messageList.evaluate((el) => {
      return Math.abs(el.scrollHeight - el.clientHeight - el.scrollTop) < 5;
    });
  }

  // Conversation methods
  async startNewConversation() {
    await this.newConversationButton.click();
  }

  async clickRetry() {
    await this.retryButton.click();
  }

  // Settings methods
  async openSettings() {
    await this.settingsButton.click();
    await this.settingsPanel.waitFor({ state: 'visible' });
  }

  async closeSettings() {
    await this.page.keyboard.press('Escape');
    await this.settingsPanel.waitFor({ state: 'hidden' });
  }

  async toggleTimestamps() {
    await this.timestampToggle.click();
  }

  async setFontSize(size: 'small' | 'medium' | 'large') {
    await this.fontSizeSelect.selectOption(size);
  }

  async toggleMarkdownRendering() {
    await this.markdownToggle.click();
  }

  async getMessageFontSize(): Promise<string> {
    return await this.aiMessages.first().evaluate((el) => {
      return window.getComputedStyle(el).fontSize;
    });
  }

  // Export methods
  async exportAsMarkdown(): Promise<Download> {
    await this.exportButton.click();
    const downloadPromise = this.page.waitForEvent('download');
    await this.exportMarkdownOption.click();
    return await downloadPromise;
  }

  async exportAsJSON(): Promise<Download> {
    await this.exportButton.click();
    const downloadPromise = this.page.waitForEvent('download');
    await this.exportJsonOption.click();
    return await downloadPromise;
  }

  async exportAsText(): Promise<Download> {
    await this.exportButton.click();
    const downloadPromise = this.page.waitForEvent('download');
    await this.exportTextOption.click();
    return await downloadPromise;
  }
}
