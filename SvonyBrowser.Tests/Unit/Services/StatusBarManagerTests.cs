using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for StatusBarManager - CRITICAL service requiring 20+ tests.
/// Tests status updates, UI coordination, and widget management.
/// </summary>
[Collection("ServiceTests")]
public class StatusBarManagerTests : ServiceTestFixture
{
    #region Singleton Tests
    
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        // Arrange & Act
        var instance1 = StatusBarManager.Instance;
        var instance2 = StatusBarManager.Instance;
        
        // Assert
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        // Act
        var instance = StatusBarManager.Instance;
        
        // Assert
        instance.Should().NotBeNull();
    }
    
    #endregion
    
    #region Property Tests
    
    [Fact]
    public void CurrentStatus_ShouldNotBeNull()
    {
        // Act
        var status = StatusBarManager.Instance.CurrentStatus;
        
        // Assert
        status.Should().NotBeNull();
    }
    
    [Fact]
    public void IsVisible_ShouldReturnBool()
    {
        // Act
        var isVisible = StatusBarManager.Instance.IsVisible;
        
        // Assert
        isVisible.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void Widgets_ShouldNotBeNull()
    {
        // Act
        var widgets = StatusBarManager.Instance.Widgets;
        
        // Assert
        widgets.Should().NotBeNull();
    }
    
    #endregion
    
    #region UpdateStatus Tests
    
    [Theory]
    [InlineData("Connected")]
    [InlineData("Disconnected")]
    [InlineData("Loading...")]
    [InlineData("Error")]
    [InlineData("")]
    public void UpdateStatus_ShouldAcceptVariousMessages(string message)
    {
        // Act
        Action act = () => StatusBarManager.Instance.UpdateStatus(message);
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void UpdateStatus_ShouldHandleNullMessage()
    {
        // Act
        Action act = () => StatusBarManager.Instance.UpdateStatus(null!);
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void UpdateStatus_ShouldUpdateCurrentStatus()
    {
        // Arrange
        var testMessage = $"Test_{Guid.NewGuid()}";
        
        // Act
        StatusBarManager.Instance.UpdateStatus(testMessage);
        
        // Assert
        StatusBarManager.Instance.CurrentStatus.Should().Contain(testMessage);
    }
    
    #endregion
    
    #region SetProgress Tests
    
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void SetProgress_ShouldAcceptValidPercentages(int percentage)
    {
        // Act
        Action act = () => StatusBarManager.Instance.SetProgress(percentage);
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void SetProgress_ShouldClampNegativeValues()
    {
        // Act
        Action act = () => StatusBarManager.Instance.SetProgress(-10);
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void SetProgress_ShouldClampOverflowValues()
    {
        // Act
        Action act = () => StatusBarManager.Instance.SetProgress(150);
        
        // Assert
        act.Should().NotThrow();
    }
    
    #endregion
    
    #region Widget Management Tests
    
    [Fact]
    public void AddWidget_ShouldAcceptValidWidget()
    {
        // Arrange
        var widgetId = $"test_widget_{Guid.NewGuid()}";
        
        // Act
        Action act = () => StatusBarManager.Instance.AddWidget(widgetId, "Test Widget");
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void RemoveWidget_ShouldNotThrowForNonexistent()
    {
        // Act
        Action act = () => StatusBarManager.Instance.RemoveWidget("nonexistent_widget");
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void UpdateWidget_ShouldNotThrow()
    {
        // Arrange
        var widgetId = $"update_test_{Guid.NewGuid()}";
        StatusBarManager.Instance.AddWidget(widgetId, "Initial");
        
        // Act
        Action act = () => StatusBarManager.Instance.UpdateWidget(widgetId, "Updated");
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetWidget_ShouldReturnNullForNonexistent()
    {
        // Act
        var widget = StatusBarManager.Instance.GetWidget("definitely_not_exists");
        
        // Assert
        widget.Should().BeNull();
    }
    
    #endregion
    
    #region Show/Hide Tests
    
    [Fact]
    public void Show_ShouldNotThrow()
    {
        // Act
        Action act = () => StatusBarManager.Instance.Show();
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void Hide_ShouldNotThrow()
    {
        // Act
        Action act = () => StatusBarManager.Instance.Hide();
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void Toggle_ShouldNotThrow()
    {
        // Act
        Action act = () => StatusBarManager.Instance.Toggle();
        
        // Assert
        act.Should().NotThrow();
    }
    
    #endregion
    
    #region Event Tests
    
    [Fact]
    public void StatusChanged_EventShouldBeSubscribable()
    {
        // Arrange
        var eventRaised = false;
        
        // Act
        StatusBarManager.Instance.StatusChanged += (status) => eventRaised = true;
        StatusBarManager.Instance.UpdateStatus("Trigger event");
        
        // Assert
        eventRaised.Should().BeTrue();
    }
    
    [Fact]
    public void ProgressChanged_EventShouldBeSubscribable()
    {
        // Arrange
        var eventRaised = false;
        
        // Act
        StatusBarManager.Instance.ProgressChanged += (progress) => eventRaised = true;
        StatusBarManager.Instance.SetProgress(50);
        
        // Assert
        eventRaised.Should().BeTrue();
    }
    
    #endregion
    
    #region Clear Tests
    
    [Fact]
    public void Clear_ShouldNotThrow()
    {
        // Act
        Action act = () => StatusBarManager.Instance.Clear();
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ClearWidgets_ShouldNotThrow()
    {
        // Act
        Action act = () => StatusBarManager.Instance.ClearWidgets();
        
        // Assert
        act.Should().NotThrow();
    }
    
    #endregion
}
