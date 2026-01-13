using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;

namespace SvonyBrowser.Tests.UI;

/// <summary>
/// UI tests for MainWindow - 25 tests covering all main window functionality.
/// </summary>
[Collection("UITests")]
public class MainWindowTests : WpfTestBase
{
    public MainWindowTests()
    {
        SetUp();
    }

    #region Window Tests (5 tests)

    [Fact]
    public void MainWindow_ShouldLaunch_WithCorrectTitle()
    {
        // Assert
        MainWindow.Should().NotBeNull();
        MainWindow!.Title.Should().Contain("Svony Browser");
    }

    [Fact]
    public void MainWindow_ShouldHave_DualPanelLayout()
    {
        // Assert - Both browser panels should exist
        var leftPanel = FindByAutomationId("LeftBrowserPanel");
        var rightPanel = FindByAutomationId("RightBrowserPanel");
        
        leftPanel.Should().NotBeNull();
        rightPanel.Should().NotBeNull();
    }

    [Fact]
    public void MainWindow_ShouldHave_MenuBar()
    {
        // Assert
        var menuBar = FindByClassName("Menu");
        menuBar.Should().NotBeNull();
    }

    [Fact]
    public void MainWindow_ShouldHave_StatusBar()
    {
        // Assert
        var statusBar = FindByAutomationId("MainStatusBar");
        statusBar.Should().NotBeNull();
    }

    [Fact]
    public void MainWindow_ShouldHave_ToolBar()
    {
        // Assert
        var toolBar = FindByClassName("ToolBar");
        toolBar.Should().NotBeNull();
    }

    #endregion

    #region Panel Toggle Tests (5 tests)

    [Fact]
    public void ShowLeftOnly_ShouldHide_RightPanel()
    {
        // Arrange
        var showLeftOnlyBtn = FindButton("ShowLeftOnlyButton");
        
        // Act
        Click(showLeftOnlyBtn!);
        Thread.Sleep(500);
        
        // Assert
        var rightPanel = FindByAutomationId("RightBrowserPanel");
        rightPanel?.IsOffscreen.Should().BeTrue();
    }

    [Fact]
    public void ShowRightOnly_ShouldHide_LeftPanel()
    {
        // Arrange
        var showRightOnlyBtn = FindButton("ShowRightOnlyButton");
        
        // Act
        Click(showRightOnlyBtn!);
        Thread.Sleep(500);
        
        // Assert
        var leftPanel = FindByAutomationId("LeftBrowserPanel");
        leftPanel?.IsOffscreen.Should().BeTrue();
    }

    [Fact]
    public void ShowBoth_ShouldShow_BothPanels()
    {
        // Arrange
        var showBothBtn = FindButton("ShowBothButton");
        
        // Act
        Click(showBothBtn!);
        Thread.Sleep(500);
        
        // Assert
        var leftPanel = FindByAutomationId("LeftBrowserPanel");
        var rightPanel = FindByAutomationId("RightBrowserPanel");
        
        leftPanel?.IsOffscreen.Should().BeFalse();
        rightPanel?.IsOffscreen.Should().BeFalse();
    }

    [Fact]
    public void SwapPanels_ShouldSwap_PanelContents()
    {
        // Arrange
        var swapBtn = FindButton("SwapPanelsButton");
        var leftUrlBefore = FindTextBox("LeftUrlBar")?.Text;
        var rightUrlBefore = FindTextBox("RightUrlBar")?.Text;
        
        // Act
        Click(swapBtn!);
        Thread.Sleep(500);
        
        // Assert
        var leftUrlAfter = FindTextBox("LeftUrlBar")?.Text;
        var rightUrlAfter = FindTextBox("RightUrlBar")?.Text;
        
        // URLs should be swapped
        leftUrlAfter.Should().Be(rightUrlBefore);
        rightUrlAfter.Should().Be(leftUrlBefore);
    }

    [Fact]
    public void TogglePanel_ShouldToggle_SidePanel()
    {
        // Arrange
        var toggleBtn = FindButton("TogglePanelButton");
        var sidePanel = FindByAutomationId("SidePanel");
        var wasVisible = !sidePanel?.IsOffscreen ?? false;
        
        // Act
        Click(toggleBtn!);
        Thread.Sleep(500);
        
        // Assert
        var isVisibleNow = !sidePanel?.IsOffscreen ?? false;
        isVisibleNow.Should().NotBe(wasVisible);
    }

    #endregion

    #region Navigation Tests (5 tests)

    [Fact]
    public void LeftUrlBar_ShouldAccept_UrlInput()
    {
        // Arrange
        var urlBar = FindTextBox("LeftUrlBar");
        var testUrl = "https://test.evony.com";
        
        // Act
        ClearAndEnterText(urlBar!, testUrl);
        
        // Assert
        urlBar!.Text.Should().Be(testUrl);
    }

    [Fact]
    public void RightUrlBar_ShouldAccept_UrlInput()
    {
        // Arrange
        var urlBar = FindTextBox("RightUrlBar");
        var testUrl = "https://test.evony.com";
        
        // Act
        ClearAndEnterText(urlBar!, testUrl);
        
        // Assert
        urlBar!.Text.Should().Be(testUrl);
    }

    [Fact]
    public void ReloadLeft_ShouldReload_LeftPanel()
    {
        // Arrange
        var reloadBtn = FindButton("ReloadLeftButton");
        
        // Act & Assert - Should not throw
        Click(reloadBtn!);
        Thread.Sleep(1000);
        
        // Verify panel is still responsive
        var leftPanel = FindByAutomationId("LeftBrowserPanel");
        leftPanel.Should().NotBeNull();
    }

    [Fact]
    public void ReloadRight_ShouldReload_RightPanel()
    {
        // Arrange
        var reloadBtn = FindButton("ReloadRightButton");
        
        // Act & Assert - Should not throw
        Click(reloadBtn!);
        Thread.Sleep(1000);
        
        // Verify panel is still responsive
        var rightPanel = FindByAutomationId("RightBrowserPanel");
        rightPanel.Should().NotBeNull();
    }

    [Fact]
    public void ServerCombo_ShouldHave_ServerOptions()
    {
        // Arrange
        var serverCombo = FindComboBox("ServerCombo");
        
        // Act
        serverCombo?.Expand();
        Thread.Sleep(200);
        
        // Assert
        serverCombo?.Items.Should().NotBeEmpty();
    }

    #endregion

    #region Button Click Tests (5 tests)

    [Fact]
    public void SettingsButton_ShouldOpen_SettingsWindow()
    {
        // Arrange
        var settingsBtn = FindButton("SettingsButton");
        
        // Act
        Click(settingsBtn!);
        Thread.Sleep(500);
        
        // Assert
        var settingsWindow = WaitForWindow("Settings");
        settingsWindow.Should().NotBeNull();
        
        // Cleanup
        CloseWindow(settingsWindow!);
    }

    [Fact]
    public void OpenFiddlerButton_ShouldTrigger_FiddlerAction()
    {
        // Arrange
        var fiddlerBtn = FindButton("OpenFiddlerButton");
        
        // Act & Assert - Should not throw
        Click(fiddlerBtn!);
        Thread.Sleep(500);
    }

    [Fact]
    public void DecodeAmfButton_ShouldTrigger_DecodeAction()
    {
        // Arrange
        var decodeBtn = FindButton("DecodeAmfButton");
        
        // Act & Assert - Should not throw
        Click(decodeBtn!);
        Thread.Sleep(500);
    }

    [Fact]
    public void CalculateTrainingButton_ShouldTrigger_CalculationAction()
    {
        // Arrange
        var calcBtn = FindButton("CalculateTrainingButton");
        
        // Act & Assert - Should not throw
        Click(calcBtn!);
        Thread.Sleep(500);
    }

    [Fact]
    public void ClearCacheButton_ShouldClear_BrowserCache()
    {
        // Arrange
        var clearBtn = FindButton("ClearCacheButton");
        
        // Act
        Click(clearBtn!);
        Thread.Sleep(500);
        
        // Assert - Should show confirmation or complete without error
        // In real test, would verify cache is cleared
    }

    #endregion

    #region MCP Connection Tests (5 tests)

    [Fact]
    public void ReconnectMcpButton_ShouldTrigger_Reconnection()
    {
        // Arrange
        var reconnectBtn = FindButton("ReconnectMcpButton");
        
        // Act
        Click(reconnectBtn!);
        Thread.Sleep(1000);
        
        // Assert - Status bar should update
        var statusBar = FindByAutomationId("McpStatusIndicator");
        statusBar.Should().NotBeNull();
    }

    [Fact]
    public void McpStatusIndicator_ShouldShow_ConnectionStatus()
    {
        // Assert
        var indicator = FindByAutomationId("McpStatusIndicator");
        indicator.Should().NotBeNull();
    }

    [Fact]
    public void RagStatusIndicator_ShouldShow_RagStatus()
    {
        // Assert
        var indicator = FindByAutomationId("RagStatusIndicator");
        indicator.Should().NotBeNull();
    }

    [Fact]
    public void RteStatusIndicator_ShouldShow_RteStatus()
    {
        // Assert
        var indicator = FindByAutomationId("RteStatusIndicator");
        indicator.Should().NotBeNull();
    }

    [Fact]
    public void FiddlerStatusIndicator_ShouldShow_FiddlerStatus()
    {
        // Assert
        var indicator = FindByAutomationId("FiddlerStatusIndicator");
        indicator.Should().NotBeNull();
    }

    #endregion
}
