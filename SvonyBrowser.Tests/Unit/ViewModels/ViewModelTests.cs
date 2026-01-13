using SvonyBrowser.ViewModels;
using SvonyBrowser.Tests.Fixtures;
using System.ComponentModel;

namespace SvonyBrowser.Tests.Unit.ViewModels;

/// <summary>
/// Unit tests for ViewModels - MVVM data binding.
/// </summary>
[Collection("ServiceTests")]
public class ViewModelTests : ServiceTestFixture
{
    #region ViewModelBase Tests
    
    [Fact]
    public void ViewModelBase_ShouldImplementINotifyPropertyChanged()
    {
        var vm = new TestViewModel();
        vm.Should().BeAssignableTo<INotifyPropertyChanged>();
    }
    
    [Fact]
    public void ViewModelBase_SetProperty_ShouldRaisePropertyChanged()
    {
        var vm = new TestViewModel();
        var eventRaised = false;
        string? changedProperty = null;
        
        vm.PropertyChanged += (s, e) =>
        {
            eventRaised = true;
            changedProperty = e.PropertyName;
        };
        
        vm.TestProperty = "New Value";
        
        eventRaised.Should().BeTrue();
        changedProperty.Should().Be(nameof(TestViewModel.TestProperty));
    }
    
    [Fact]
    public void ViewModelBase_SetProperty_SameValue_ShouldNotRaiseEvent()
    {
        var vm = new TestViewModel { TestProperty = "Initial" };
        var eventCount = 0;
        
        vm.PropertyChanged += (s, e) => eventCount++;
        
        vm.TestProperty = "Initial"; // Same value
        
        eventCount.Should().Be(0);
    }
    
    #endregion
    
    #region MainWindowViewModel Tests
    
    [Fact]
    public void MainWindowViewModel_ShouldNotBeNull()
    {
        var vm = new MainWindowViewModel();
        vm.Should().NotBeNull();
    }
    
    [Fact]
    public void MainWindowViewModel_Title_ShouldHaveDefaultValue()
    {
        var vm = new MainWindowViewModel();
        vm.Title.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public void MainWindowViewModel_IsLeftPanelVisible_ShouldBeTrue()
    {
        var vm = new MainWindowViewModel();
        vm.IsLeftPanelVisible.Should().BeTrue();
    }
    
    [Fact]
    public void MainWindowViewModel_IsRightPanelVisible_ShouldBeTrue()
    {
        var vm = new MainWindowViewModel();
        vm.IsRightPanelVisible.Should().BeTrue();
    }
    
    [Fact]
    public void MainWindowViewModel_SetLeftPanelVisible_ShouldRaiseEvent()
    {
        var vm = new MainWindowViewModel();
        var eventRaised = false;
        
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsLeftPanelVisible))
                eventRaised = true;
        };
        
        vm.IsLeftPanelVisible = false;
        
        eventRaised.Should().BeTrue();
    }
    
    [Fact]
    public void MainWindowViewModel_McpStatus_ShouldHaveDefaultValue()
    {
        var vm = new MainWindowViewModel();
        vm.McpStatus.Should().NotBeNull();
    }
    
    [Fact]
    public void MainWindowViewModel_RagStatus_ShouldHaveDefaultValue()
    {
        var vm = new MainWindowViewModel();
        vm.RagStatus.Should().NotBeNull();
    }
    
    [Fact]
    public void MainWindowViewModel_RteStatus_ShouldHaveDefaultValue()
    {
        var vm = new MainWindowViewModel();
        vm.RteStatus.Should().NotBeNull();
    }
    
    [Fact]
    public void MainWindowViewModel_IsBusy_ShouldBeFalse()
    {
        var vm = new MainWindowViewModel();
        vm.IsBusy.Should().BeFalse();
    }
    
    [Fact]
    public void MainWindowViewModel_StatusMessage_ShouldNotBeNull()
    {
        var vm = new MainWindowViewModel();
        vm.StatusMessage.Should().NotBeNull();
    }
    
    [Fact]
    public void MainWindowViewModel_CurrentServer_ShouldHaveDefaultValue()
    {
        var vm = new MainWindowViewModel();
        vm.CurrentServer.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public void MainWindowViewModel_PacketsPerSecond_ShouldBeNonNegative()
    {
        var vm = new MainWindowViewModel();
        vm.PacketsPerSecond.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void MainWindowViewModel_MemoryUsageMb_ShouldBeNonNegative()
    {
        var vm = new MainWindowViewModel();
        vm.MemoryUsageMb.Should().BeGreaterOrEqualTo(0);
    }
    
    #endregion
    
    #region SettingsViewModel Tests
    
    [Fact]
    public void SettingsViewModel_ShouldNotBeNull()
    {
        var vm = new SettingsViewModel();
        vm.Should().NotBeNull();
    }
    
    [Fact]
    public void SettingsViewModel_Theme_ShouldHaveDefaultValue()
    {
        var vm = new SettingsViewModel();
        vm.Theme.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public void SettingsViewModel_Language_ShouldHaveDefaultValue()
    {
        var vm = new SettingsViewModel();
        vm.Language.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public void SettingsViewModel_SetTheme_ShouldRaiseEvent()
    {
        var vm = new SettingsViewModel();
        var eventRaised = false;
        
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.Theme))
                eventRaised = true;
        };
        
        vm.Theme = "Light";
        
        eventRaised.Should().BeTrue();
    }
    
    [Fact]
    public void SettingsViewModel_EnableFlash_ShouldBeTrue()
    {
        var vm = new SettingsViewModel();
        vm.EnableFlash.Should().BeTrue();
    }
    
    [Fact]
    public void SettingsViewModel_EnableGpu_ShouldBeTrue()
    {
        var vm = new SettingsViewModel();
        vm.EnableGpu.Should().BeTrue();
    }
    
    [Fact]
    public void SettingsViewModel_ProxyEnabled_ShouldBeFalse()
    {
        var vm = new SettingsViewModel();
        vm.ProxyEnabled.Should().BeFalse();
    }
    
    [Fact]
    public void SettingsViewModel_McpEnabled_ShouldBeTrue()
    {
        var vm = new SettingsViewModel();
        vm.McpEnabled.Should().BeTrue();
    }
    
    [Fact]
    public void SettingsViewModel_LlmEnabled_ShouldBeTrue()
    {
        var vm = new SettingsViewModel();
        vm.LlmEnabled.Should().BeTrue();
    }
    
    [Fact]
    public void SettingsViewModel_FiddlerEnabled_ShouldBeFalse()
    {
        var vm = new SettingsViewModel();
        vm.FiddlerEnabled.Should().BeFalse();
    }
    
    [Fact]
    public void SettingsViewModel_CacheSizeMb_ShouldHaveDefaultValue()
    {
        var vm = new SettingsViewModel();
        vm.CacheSizeMb.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public void SettingsViewModel_DefaultZoom_ShouldBe100()
    {
        var vm = new SettingsViewModel();
        vm.DefaultZoom.Should().Be(100);
    }
    
    [Fact]
    public void SettingsViewModel_Temperature_ShouldBeInRange()
    {
        var vm = new SettingsViewModel();
        vm.Temperature.Should().BeInRange(0, 2);
    }
    
    [Fact]
    public void SettingsViewModel_MaxTokens_ShouldBePositive()
    {
        var vm = new SettingsViewModel();
        vm.MaxTokens.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public void SettingsViewModel_IsDirty_ShouldBeFalse()
    {
        var vm = new SettingsViewModel();
        vm.IsDirty.Should().BeFalse();
    }
    
    [Fact]
    public void SettingsViewModel_SetProperty_ShouldSetDirty()
    {
        var vm = new SettingsViewModel();
        vm.Theme = "Light";
        vm.IsDirty.Should().BeTrue();
    }
    
    #endregion
}

/// <summary>
/// Test ViewModel for testing ViewModelBase functionality.
/// </summary>
public class TestViewModel : ViewModelBase
{
    private string _testProperty = "";
    
    public string TestProperty
    {
        get => _testProperty;
        set => SetProperty(ref _testProperty, value);
    }
}
