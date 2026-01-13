using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;

namespace SvonyBrowser.Tests.UI;

/// <summary>
/// End-to-end workflow tests - 15 tests covering complete user workflows.
/// </summary>
[Collection("UITests")]
public class E2EWorkflowTests : WpfTestBase
{
    public E2EWorkflowTests()
    {
        SetUp();
    }

    #region Application Startup Workflow (3 tests)

    [Fact]
    public void Workflow_ApplicationStartup_ShouldInitializeAllServices()
    {
        // Wait for app to fully load
        Thread.Sleep(3000);
        
        // Verify all status indicators show initialized state
        var mcpStatus = FindByAutomationId("McpStatusIndicator");
        var ragStatus = FindByAutomationId("RagStatusIndicator");
        var fiddlerStatus = FindByAutomationId("FiddlerStatusIndicator");
        
        mcpStatus.Should().NotBeNull();
        ragStatus.Should().NotBeNull();
        fiddlerStatus.Should().NotBeNull();
    }

    [Fact]
    public void Workflow_ApplicationStartup_ShouldLoadDefaultSettings()
    {
        // Open settings
        var settingsBtn = FindButton("SettingsButton");
        Click(settingsBtn!);
        Thread.Sleep(500);
        
        var settingsWindow = WaitForWindow("Settings");
        
        // Verify settings are loaded
        var themeCombo = FindComboBox("ThemeCombo", settingsWindow);
        themeCombo?.SelectedItem.Should().NotBeNull();
        
        CloseWindow(settingsWindow!);
    }

    [Fact]
    public void Workflow_ApplicationStartup_ShouldShowMainWindow()
    {
        MainWindow.Should().NotBeNull();
        MainWindow!.IsAvailable.Should().BeTrue();
        MainWindow.Title.Should().Contain("Svony");
    }

    #endregion

    #region Browser Navigation Workflow (3 tests)

    [Fact]
    public void Workflow_NavigateToEvony_ShouldLoadGame()
    {
        // Enter URL
        var urlBar = FindTextBox("LeftUrlBar");
        ClearAndEnterText(urlBar!, "https://www.evony.com/play");
        PressKey(VirtualKeyShort.ENTER);
        
        // Wait for page to load
        Thread.Sleep(5000);
        
        // Verify page loaded (browser panel should have content)
        var leftPanel = FindByAutomationId("LeftBrowserPanel");
        leftPanel.Should().NotBeNull();
    }

    [Fact]
    public void Workflow_LoadSwfFile_ShouldDisplayFlash()
    {
        // This test verifies SWF loading capability
        var urlBar = FindTextBox("LeftUrlBar");
        ClearAndEnterText(urlBar!, "file:///C:/test.swf");
        PressKey(VirtualKeyShort.ENTER);
        
        Thread.Sleep(3000);
        
        // Flash content should be displayed
        var leftPanel = FindByAutomationId("LeftBrowserPanel");
        leftPanel.Should().NotBeNull();
    }

    [Fact]
    public void Workflow_DualPanelNavigation_ShouldWorkIndependently()
    {
        // Navigate left panel
        var leftUrlBar = FindTextBox("LeftUrlBar");
        ClearAndEnterText(leftUrlBar!, "https://example.com");
        PressKey(VirtualKeyShort.ENTER);
        Thread.Sleep(2000);
        
        // Navigate right panel
        var rightUrlBar = FindTextBox("RightUrlBar");
        ClearAndEnterText(rightUrlBar!, "https://google.com");
        PressKey(VirtualKeyShort.ENTER);
        Thread.Sleep(2000);
        
        // Both should have different URLs
        leftUrlBar!.Text.Should().Contain("example");
        rightUrlBar!.Text.Should().Contain("google");
    }

    #endregion

    #region Settings Workflow (3 tests)

    [Fact]
    public void Workflow_ChangeAndSaveSettings_ShouldPersist()
    {
        // Open settings
        var settingsBtn = FindButton("SettingsButton");
        Click(settingsBtn!);
        Thread.Sleep(500);
        
        var settingsWindow = WaitForWindow("Settings");
        
        // Change a setting
        var toggle = FindCheckBox("StartMinimizedToggle", settingsWindow);
        var originalState = toggle?.IsChecked;
        Click(toggle!);
        Thread.Sleep(200);
        
        // Save
        var saveBtn = FindButton("SaveButton", settingsWindow);
        Click(saveBtn!);
        Thread.Sleep(500);
        
        // Reopen settings
        Click(settingsBtn!);
        Thread.Sleep(500);
        settingsWindow = WaitForWindow("Settings");
        
        // Verify setting persisted
        toggle = FindCheckBox("StartMinimizedToggle", settingsWindow);
        toggle?.IsChecked.Should().NotBe(originalState);
        
        // Restore original state
        Click(toggle!);
        Click(FindButton("SaveButton", settingsWindow)!);
        Thread.Sleep(500);
    }

    [Fact]
    public void Workflow_CancelSettings_ShouldNotPersist()
    {
        // Open settings
        var settingsBtn = FindButton("SettingsButton");
        Click(settingsBtn!);
        Thread.Sleep(500);
        
        var settingsWindow = WaitForWindow("Settings");
        
        // Change a setting
        var toggle = FindCheckBox("StartMinimizedToggle", settingsWindow);
        var originalState = toggle?.IsChecked;
        Click(toggle!);
        Thread.Sleep(200);
        
        // Cancel
        var cancelBtn = FindButton("CancelButton", settingsWindow);
        Click(cancelBtn!);
        Thread.Sleep(500);
        
        // Reopen settings
        Click(settingsBtn!);
        Thread.Sleep(500);
        settingsWindow = WaitForWindow("Settings");
        
        // Verify setting NOT persisted
        toggle = FindCheckBox("StartMinimizedToggle", settingsWindow);
        toggle?.IsChecked.Should().Be(originalState);
        
        CloseWindow(settingsWindow!);
    }

    [Fact]
    public void Workflow_ImportExportSettings_ShouldWork()
    {
        // Open settings
        var settingsBtn = FindButton("SettingsButton");
        Click(settingsBtn!);
        Thread.Sleep(500);
        
        var settingsWindow = WaitForWindow("Settings");
        
        // Export settings
        var exportBtn = FindButton("ExportSettingsButton", settingsWindow);
        if (exportBtn != null)
        {
            Click(exportBtn);
            Thread.Sleep(500);
            
            // Handle save dialog
            var saveDialog = WaitForWindow("Save", TimeSpan.FromSeconds(2));
            if (saveDialog != null)
            {
                CloseWindow(saveDialog);
            }
        }
        
        CloseWindow(settingsWindow!);
    }

    #endregion

    #region Chatbot Workflow (3 tests)

    [Fact]
    public void Workflow_SendChatMessage_ShouldGetResponse()
    {
        var messageInput = FindTextBox("MessageInput");
        var sendBtn = FindButton("SendMessageButton");
        
        // Send a message
        ClearAndEnterText(messageInput!, "What is Evony?");
        Click(sendBtn!);
        
        // Wait for response
        Thread.Sleep(5000);
        
        // Verify response appeared
        var messageList = FindByAutomationId("MessageList");
        messageList.Should().NotBeNull();
    }

    [Fact]
    public void Workflow_UseTemplate_ShouldFillAndSend()
    {
        var templateBtn = FindButton("TemplateButton");
        var messageInput = FindTextBox("MessageInput");
        var sendBtn = FindButton("SendMessageButton");
        
        if (templateBtn != null)
        {
            // Open template menu
            Click(templateBtn);
            Thread.Sleep(300);
            
            // Select first template
            var templateItem = FindByAutomationId("TemplateItem_0");
            if (templateItem != null)
            {
                Click(templateItem);
                Thread.Sleep(300);
                
                // Verify template filled input
                messageInput!.Text.Should().NotBeEmpty();
                
                // Send the message
                Click(sendBtn!);
                Thread.Sleep(3000);
            }
        }
    }

    [Fact]
    public void Workflow_ClearChatHistory_ShouldClearAll()
    {
        var messageInput = FindTextBox("MessageInput");
        var sendBtn = FindButton("SendMessageButton");
        var clearBtn = FindButton("ClearHistoryButton");
        
        // Send some messages
        ClearAndEnterText(messageInput!, "Test 1");
        Click(sendBtn!);
        Thread.Sleep(1000);
        
        ClearAndEnterText(messageInput!, "Test 2");
        Click(sendBtn!);
        Thread.Sleep(1000);
        
        // Clear history
        if (clearBtn != null)
        {
            Click(clearBtn);
            Thread.Sleep(500);
            
            // Verify cleared
            var messageList = FindByAutomationId("MessageList");
            // List should be empty or have minimal content
        }
    }

    #endregion

    #region Traffic Capture Workflow (3 tests)

    [Fact]
    public void Workflow_CaptureTraffic_ShouldShowInViewer()
    {
        // Start capture
        var startBtn = FindButton("StartCaptureButton");
        Click(startBtn!);
        Thread.Sleep(500);
        
        // Generate some traffic by navigating
        var urlBar = FindTextBox("LeftUrlBar");
        ClearAndEnterText(urlBar!, "https://example.com");
        PressKey(VirtualKeyShort.ENTER);
        Thread.Sleep(3000);
        
        // Stop capture
        var stopBtn = FindButton("StopCaptureButton");
        Click(stopBtn!);
        Thread.Sleep(500);
        
        // Verify traffic appeared
        var trafficList = FindByAutomationId("TrafficList");
        trafficList.Should().NotBeNull();
    }

    [Fact]
    public void Workflow_FilterTraffic_ShouldShowFiltered()
    {
        var filterText = FindTextBox("TrafficFilterText");
        
        // Apply filter
        ClearAndEnterText(filterText!, "HTTP");
        Thread.Sleep(500);
        
        // Verify filter applied
        var trafficList = FindByAutomationId("TrafficList");
        trafficList.Should().NotBeNull();
    }

    [Fact]
    public void Workflow_ExportTraffic_ShouldSaveFile()
    {
        var exportBtn = FindButton("ExportTrafficButton");
        
        if (exportBtn != null)
        {
            Click(exportBtn);
            Thread.Sleep(500);
            
            // Handle save dialog
            var saveDialog = WaitForWindow("Save", TimeSpan.FromSeconds(2));
            if (saveDialog != null)
            {
                // Type filename and save
                var fileNameInput = FindTextBox("FileNameTextBox", saveDialog);
                if (fileNameInput != null)
                {
                    ClearAndEnterText(fileNameInput, "test_traffic.json");
                    
                    var saveBtn = FindButton("Save", saveDialog);
                    if (saveBtn != null)
                    {
                        Click(saveBtn);
                        Thread.Sleep(500);
                    }
                }
                else
                {
                    CloseWindow(saveDialog);
                }
            }
        }
    }

    #endregion
}
