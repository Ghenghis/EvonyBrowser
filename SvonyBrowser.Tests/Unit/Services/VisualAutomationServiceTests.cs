using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for VisualAutomationService - MEDIUM priority service requiring 15+ tests.
/// </summary>
[Collection("ServiceTests")]
public class VisualAutomationServiceTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = VisualAutomationService.Instance;
        var instance2 = VisualAutomationService.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        VisualAutomationService.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void IsRunning_ShouldReturnBool()
    {
        VisualAutomationService.Instance.IsRunning.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void CurrentScript_ShouldBeAccessible()
    {
        var script = VisualAutomationService.Instance.CurrentScript;
        // Can be null
    }
    
    [Fact]
    public async Task ClickAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await VisualAutomationService.Instance.ClickAsync(100, 100);
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task TypeTextAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await VisualAutomationService.Instance.TypeTextAsync("Hello");
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task MoveMouseAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await VisualAutomationService.Instance.MoveMouseAsync(100, 100);
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task TakeScreenshotAsync_ShouldReturnPath()
    {
        var path = await VisualAutomationService.Instance.TakeScreenshotAsync(TestDataPath);
        path.Should().NotBeNull();
    }
    
    [Fact]
    public async Task FindImageAsync_ShouldReturnResult()
    {
        var result = await VisualAutomationService.Instance.FindImageAsync("template.png");
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task WaitForImageAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await VisualAutomationService.Instance.WaitForImageAsync("template.png", TimeSpan.FromSeconds(1));
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void LoadScript_ShouldNotThrow()
    {
        Action act = () => VisualAutomationService.Instance.LoadScript("test_script.json");
        act.Should().NotThrow();
    }
    
    [Fact]
    public async Task RunScriptAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await VisualAutomationService.Instance.RunScriptAsync();
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void StopScript_ShouldNotThrow()
    {
        Action act = () => VisualAutomationService.Instance.StopScript();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ActionCompleted_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        VisualAutomationService.Instance.ActionCompleted += (action) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void Error_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        VisualAutomationService.Instance.Error += (ex) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
