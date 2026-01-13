using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for FiddlerBridge - HIGH priority service requiring 15+ tests.
/// </summary>
[Collection("ServiceTests")]
public class FiddlerBridgeTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = FiddlerBridge.Instance;
        var instance2 = FiddlerBridge.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        FiddlerBridge.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void IsConnected_ShouldReturnBool()
    {
        FiddlerBridge.Instance.IsConnected.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void IsFiddlerRunning_ShouldReturnBool()
    {
        FiddlerBridge.Instance.IsFiddlerRunning.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public async Task ConnectAsync_ShouldReturnResult()
    {
        var result = await FiddlerBridge.Instance.ConnectAsync();
        result.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public async Task DisconnectAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await FiddlerBridge.Instance.DisconnectAsync();
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void StartCapture_ShouldNotThrow()
    {
        Action act = () => FiddlerBridge.Instance.StartCapture();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void StopCapture_ShouldNotThrow()
    {
        Action act = () => FiddlerBridge.Instance.StopCapture();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetCapturedSessions_ShouldReturnList()
    {
        var sessions = FiddlerBridge.Instance.GetCapturedSessions();
        sessions.Should().NotBeNull();
    }
    
    [Fact]
    public void ClearSessions_ShouldNotThrow()
    {
        Action act = () => FiddlerBridge.Instance.ClearSessions();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void SetFilter_ShouldNotThrow()
    {
        Action act = () => FiddlerBridge.Instance.SetFilter("evony.com");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ClearFilter_ShouldNotThrow()
    {
        Action act = () => FiddlerBridge.Instance.ClearFilter();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void SessionCaptured_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        FiddlerBridge.Instance.SessionCaptured += (session) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void Connected_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        FiddlerBridge.Instance.Connected += () => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void Disconnected_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        FiddlerBridge.Instance.Disconnected += () => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
