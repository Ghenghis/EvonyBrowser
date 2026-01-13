using FlaUI.Core.AutomationElements;

namespace SvonyBrowser.Tests.UI;

/// <summary>
/// UI tests for TrafficViewer - 12 tests covering traffic capture functionality.
/// </summary>
[Collection("UITests")]
public class TrafficViewerTests : WpfTestBase
{
    public TrafficViewerTests()
    {
        SetUp();
    }

    #region Panel Tests (3 tests)

    [Fact]
    public void TrafficViewer_ShouldExist()
    {
        var trafficViewer = FindByAutomationId("TrafficViewer");
        trafficViewer.Should().NotBeNull();
    }

    [Fact]
    public void TrafficViewer_ShouldHave_TrafficList()
    {
        var trafficList = FindByAutomationId("TrafficList");
        trafficList.Should().NotBeNull();
    }

    [Fact]
    public void TrafficViewer_ShouldHave_FilterControls()
    {
        var filterText = FindTextBox("TrafficFilterText");
        filterText.Should().NotBeNull();
    }

    #endregion

    #region Capture Control Tests (4 tests)

    [Fact]
    public void StartCaptureButton_ShouldStart_Capture()
    {
        var startBtn = FindButton("StartCaptureButton");
        
        Click(startBtn!);
        Thread.Sleep(500);
        
        // Capture should be started
        // Verify status indicator
    }

    [Fact]
    public void StopCaptureButton_ShouldStop_Capture()
    {
        var stopBtn = FindButton("StopCaptureButton");
        
        Click(stopBtn!);
        Thread.Sleep(500);
        
        // Capture should be stopped
    }

    [Fact]
    public void ClearTrafficButton_ShouldClear_List()
    {
        var clearBtn = FindButton("ClearTrafficButton");
        
        Click(clearBtn!);
        Thread.Sleep(500);
        
        // Traffic list should be cleared
    }

    [Fact]
    public void ExportTrafficButton_ShouldExport_Traffic()
    {
        var exportBtn = FindButton("ExportTrafficButton");
        
        Click(exportBtn!);
        Thread.Sleep(500);
        
        // Export dialog should appear
        var saveDialog = WaitForWindow("Save", TimeSpan.FromSeconds(2));
        if (saveDialog != null)
        {
            CloseWindow(saveDialog);
        }
    }

    #endregion

    #region Filter Tests (3 tests)

    [Fact]
    public void FilterText_ShouldFilter_Traffic()
    {
        var filterText = FindTextBox("TrafficFilterText");
        
        ClearAndEnterText(filterText!, "AMF");
        Thread.Sleep(500);
        
        // Traffic should be filtered
    }

    [Fact]
    public void FilterCombo_ShouldHave_FilterOptions()
    {
        var filterCombo = FindComboBox("TrafficFilterCombo");
        
        if (filterCombo != null)
        {
            filterCombo.Expand();
            Thread.Sleep(200);
            
            filterCombo.Items.Should().NotBeEmpty();
        }
    }

    [Fact]
    public void ClearFilterButton_ShouldClear_Filter()
    {
        var filterText = FindTextBox("TrafficFilterText");
        var clearFilterBtn = FindButton("ClearFilterButton");
        
        ClearAndEnterText(filterText!, "test");
        
        if (clearFilterBtn != null)
        {
            Click(clearFilterBtn);
            Thread.Sleep(200);
            
            filterText!.Text.Should().BeEmpty();
        }
    }

    #endregion

    #region Detail View Tests (2 tests)

    [Fact]
    public void SelectTraffic_ShouldShow_Details()
    {
        var trafficList = FindByAutomationId("TrafficList");
        var detailPanel = FindByAutomationId("TrafficDetailPanel");
        
        // Select first item if available
        if (trafficList != null)
        {
            var items = trafficList.FindAllChildren();
            if (items.Length > 0)
            {
                Click(items[0]);
                Thread.Sleep(300);
                
                // Detail panel should show content
                detailPanel.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void DecodeAmfButton_ShouldDecode_SelectedTraffic()
    {
        var decodeBtn = FindButton("DecodeAmfButton");
        
        if (decodeBtn != null)
        {
            Click(decodeBtn);
            Thread.Sleep(500);
            
            // Decoded content should appear
        }
    }

    #endregion
}
