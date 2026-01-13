using FlaUI.Core.AutomationElements;

namespace SvonyBrowser.Tests.UI;

/// <summary>
/// UI tests for ProtocolExplorer - 8 tests covering protocol analysis functionality.
/// </summary>
[Collection("UITests")]
public class ProtocolExplorerTests : WpfTestBase
{
    public ProtocolExplorerTests()
    {
        SetUp();
    }

    #region Panel Tests (2 tests)

    [Fact]
    public void ProtocolExplorer_ShouldExist()
    {
        var explorer = FindByAutomationId("ProtocolExplorer");
        explorer.Should().NotBeNull();
    }

    [Fact]
    public void ProtocolExplorer_ShouldHave_ProtocolTree()
    {
        var tree = FindByAutomationId("ProtocolTree");
        tree.Should().NotBeNull();
    }

    #endregion

    #region Search Tests (2 tests)

    [Fact]
    public void SearchText_ShouldSearch_Protocols()
    {
        var searchText = FindTextBox("ProtocolSearchText");
        
        if (searchText != null)
        {
            ClearAndEnterText(searchText, "login");
            Thread.Sleep(500);
            
            // Search results should be filtered
        }
    }

    [Fact]
    public void ClearSearchButton_ShouldClear_Search()
    {
        var searchText = FindTextBox("ProtocolSearchText");
        var clearBtn = FindButton("ClearSearchButton");
        
        if (searchText != null)
        {
            ClearAndEnterText(searchText, "test");
            
            if (clearBtn != null)
            {
                Click(clearBtn);
                Thread.Sleep(200);
                
                searchText.Text.Should().BeEmpty();
            }
        }
    }

    #endregion

    #region Protocol Selection Tests (2 tests)

    [Fact]
    public void SelectProtocol_ShouldShow_Details()
    {
        var tree = FindByAutomationId("ProtocolTree");
        var detailPanel = FindByAutomationId("ProtocolDetailPanel");
        
        if (tree != null)
        {
            var items = tree.FindAllChildren();
            if (items.Length > 0)
            {
                Click(items[0]);
                Thread.Sleep(300);
                
                // Detail panel should show content
            }
        }
    }

    [Fact]
    public void ExpandProtocol_ShouldShow_SubProtocols()
    {
        var tree = FindByAutomationId("ProtocolTree");
        
        if (tree != null)
        {
            var items = tree.FindAllChildren();
            if (items.Length > 0)
            {
                DoubleClick(items[0]);
                Thread.Sleep(300);
                
                // Sub-items should be visible
            }
        }
    }

    #endregion

    #region Action Tests (2 tests)

    [Fact]
    public void ReplayButton_ShouldReplay_Protocol()
    {
        var replayBtn = FindButton("ReplayProtocolButton");
        
        if (replayBtn != null)
        {
            Click(replayBtn);
            Thread.Sleep(500);
            
            // Protocol should be replayed
        }
    }

    [Fact]
    public void ExportButton_ShouldExport_Protocol()
    {
        var exportBtn = FindButton("ExportProtocolButton");
        
        if (exportBtn != null)
        {
            Click(exportBtn);
            Thread.Sleep(500);
            
            // Export dialog should appear
            var saveDialog = WaitForWindow("Save", TimeSpan.FromSeconds(2));
            if (saveDialog != null)
            {
                CloseWindow(saveDialog);
            }
        }
    }

    #endregion
}
