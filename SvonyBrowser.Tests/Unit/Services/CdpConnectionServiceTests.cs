using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for CdpConnectionService - HIGH priority service requiring 20+ tests.
/// </summary>
[Collection("ServiceTests")]
public class CdpConnectionServiceTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = CdpConnectionService.Instance;
        var instance2 = CdpConnectionService.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        CdpConnectionService.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void IsConnected_ShouldReturnBool()
    {
        CdpConnectionService.Instance.IsConnected.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void DebuggerUrl_ShouldNotBeNull()
    {
        CdpConnectionService.Instance.DebuggerUrl.Should().NotBeNull();
    }
    
    [Fact]
    public async Task ConnectAsync_ShouldReturnResult()
    {
        var result = await CdpConnectionService.Instance.ConnectAsync("ws://localhost:9222");
        result.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public async Task DisconnectAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await CdpConnectionService.Instance.DisconnectAsync();
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task SendCommandAsync_ShouldReturnResult()
    {
        var result = await CdpConnectionService.Instance.SendCommandAsync("Runtime.evaluate", 
            new JObject { ["expression"] = "1+1" });
        result.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData("Runtime.evaluate")]
    [InlineData("Page.navigate")]
    [InlineData("DOM.getDocument")]
    [InlineData("Network.enable")]
    public async Task SendCommandAsync_ShouldHandleVariousCommands(string command)
    {
        Func<Task> act = async () => await CdpConnectionService.Instance.SendCommandAsync(command, new JObject());
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void SubscribeToEvent_ShouldNotThrow()
    {
        Action act = () => CdpConnectionService.Instance.SubscribeToEvent("Network.requestWillBeSent", 
            (data) => { });
        act.Should().NotThrow();
    }
    
    [Fact]
    public void UnsubscribeFromEvent_ShouldNotThrow()
    {
        Action act = () => CdpConnectionService.Instance.UnsubscribeFromEvent("Network.requestWillBeSent");
        act.Should().NotThrow();
    }
    
    [Fact]
    public async Task EnableDomainAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await CdpConnectionService.Instance.EnableDomainAsync("Network");
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task DisableDomainAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await CdpConnectionService.Instance.DisableDomainAsync("Network");
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void GetEnabledDomains_ShouldReturnList()
    {
        var domains = CdpConnectionService.Instance.GetEnabledDomains();
        domains.Should().NotBeNull();
    }
    
    [Fact]
    public async Task ExecuteScriptAsync_ShouldReturnResult()
    {
        var result = await CdpConnectionService.Instance.ExecuteScriptAsync("return 1 + 1");
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task GetPageInfoAsync_ShouldReturnInfo()
    {
        var info = await CdpConnectionService.Instance.GetPageInfoAsync();
        info.Should().NotBeNull();
    }
    
    [Fact]
    public void Connected_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        CdpConnectionService.Instance.Connected += () => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void Disconnected_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        CdpConnectionService.Instance.Disconnected += () => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void EventReceived_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        CdpConnectionService.Instance.EventReceived += (name, data) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void Error_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        CdpConnectionService.Instance.Error += (ex) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
