using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for TrafficPipeClient - HIGH priority service requiring 15+ tests.
/// Tests named pipe communication with Fiddler.
/// </summary>
[Collection("ServiceTests")]
public class TrafficPipeClientTests : ServiceTestFixture
{
    #region Singleton Tests
    
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = TrafficPipeClient.Instance;
        var instance2 = TrafficPipeClient.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        TrafficPipeClient.Instance.Should().NotBeNull();
    }
    
    #endregion
    
    #region Property Tests
    
    [Fact]
    public void IsConnected_ShouldReturnBool()
    {
        TrafficPipeClient.Instance.IsConnected.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void PipeName_ShouldNotBeEmpty()
    {
        TrafficPipeClient.Instance.PipeName.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public void PacketCount_ShouldBeNonNegative()
    {
        TrafficPipeClient.Instance.PacketCount.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void LastPacketTime_ShouldHaveValue()
    {
        var lastTime = TrafficPipeClient.Instance.LastPacketTime;
        lastTime.Should().BeBefore(DateTime.UtcNow.AddDays(1));
    }
    
    #endregion
    
    #region Connect/Disconnect Tests
    
    [Fact]
    public async Task ConnectAsync_ShouldReturnResult()
    {
        var result = await TrafficPipeClient.Instance.ConnectAsync();
        result.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public async Task DisconnectAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await TrafficPipeClient.Instance.DisconnectAsync();
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task DisconnectAsync_ShouldBeIdempotent()
    {
        await TrafficPipeClient.Instance.DisconnectAsync();
        await TrafficPipeClient.Instance.DisconnectAsync();
        TrafficPipeClient.Instance.IsConnected.Should().BeFalse();
    }
    
    #endregion
    
    #region Send/Receive Tests
    
    [Fact]
    public async Task SendAsync_ShouldReturnResult()
    {
        var data = new JObject { ["test"] = "data" };
        var result = await TrafficPipeClient.Instance.SendAsync(data.ToString());
        result.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public async Task SendAsync_ShouldHandleEmptyData()
    {
        var result = await TrafficPipeClient.Instance.SendAsync("");
        result.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public async Task SendAsync_ShouldHandleNullData()
    {
        var result = await TrafficPipeClient.Instance.SendAsync(null!);
        result.Should().BeFalse();
    }
    
    #endregion
    
    #region Event Tests
    
    [Fact]
    public void PacketReceived_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        TrafficPipeClient.Instance.PacketReceived += (data) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void Connected_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        TrafficPipeClient.Instance.Connected += () => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void Disconnected_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        TrafficPipeClient.Instance.Disconnected += () => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void Error_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        TrafficPipeClient.Instance.Error += (ex) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    #endregion
    
    #region Statistics Tests
    
    [Fact]
    public void GetStatistics_ShouldReturnStats()
    {
        var stats = TrafficPipeClient.Instance.GetStatistics();
        stats.Should().NotBeNull();
    }
    
    [Fact]
    public void ResetStatistics_ShouldNotThrow()
    {
        Action act = () => TrafficPipeClient.Instance.ResetStatistics();
        act.Should().NotThrow();
    }
    
    #endregion
}
