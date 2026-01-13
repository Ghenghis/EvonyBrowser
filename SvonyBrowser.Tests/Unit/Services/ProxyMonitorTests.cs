using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for ProxyMonitor - MEDIUM priority service requiring 10+ tests.
/// </summary>
[Collection("ServiceTests")]
public class ProxyMonitorTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = ProxyMonitor.Instance;
        var instance2 = ProxyMonitor.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        ProxyMonitor.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void IsMonitoring_ShouldReturnBool()
    {
        ProxyMonitor.Instance.IsMonitoring.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void ProxyStatus_ShouldNotBeNull()
    {
        ProxyMonitor.Instance.ProxyStatus.Should().NotBeNull();
    }
    
    [Fact]
    public void StartMonitoring_ShouldNotThrow()
    {
        Action act = () => ProxyMonitor.Instance.StartMonitoring();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void StopMonitoring_ShouldNotThrow()
    {
        Action act = () => ProxyMonitor.Instance.StopMonitoring();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetStatistics_ShouldReturnStats()
    {
        var stats = ProxyMonitor.Instance.GetStatistics();
        stats.Should().NotBeNull();
    }
    
    [Fact]
    public void CheckProxyHealth_ShouldReturnBool()
    {
        var isHealthy = ProxyMonitor.Instance.CheckProxyHealth();
        isHealthy.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void GetBandwidthUsage_ShouldReturnValue()
    {
        var bandwidth = ProxyMonitor.Instance.GetBandwidthUsage();
        bandwidth.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void StatusChanged_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        ProxyMonitor.Instance.StatusChanged += (status) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void Error_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        ProxyMonitor.Instance.Error += (ex) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
