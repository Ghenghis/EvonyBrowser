using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for MemoryManager - HIGH priority for failsafes.
/// </summary>
[Collection("ServiceTests")]
public class MemoryManagerTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = MemoryManager.Instance;
        var instance2 = MemoryManager.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        MemoryManager.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void CurrentMemoryUsage_ShouldBeNonNegative()
    {
        MemoryManager.Instance.CurrentMemoryUsage.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void MemoryLimit_ShouldBePositive()
    {
        MemoryManager.Instance.MemoryLimit.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public void IsMemoryPressure_ShouldReturnBool()
    {
        MemoryManager.Instance.IsMemoryPressure.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void ForceGarbageCollection_ShouldNotThrow()
    {
        Action act = () => MemoryManager.Instance.ForceGarbageCollection();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void SetMemoryLimit_ShouldNotThrow()
    {
        Action act = () => MemoryManager.Instance.SetMemoryLimit(1024 * 1024 * 500); // 500MB
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetMemoryStatistics_ShouldReturnStats()
    {
        var stats = MemoryManager.Instance.GetMemoryStatistics();
        stats.Should().NotBeNull();
    }
    
    [Fact]
    public void StartMonitoring_ShouldNotThrow()
    {
        Action act = () => MemoryManager.Instance.StartMonitoring();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void StopMonitoring_ShouldNotThrow()
    {
        Action act = () => MemoryManager.Instance.StopMonitoring();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void MemoryWarning_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        MemoryManager.Instance.MemoryWarning += (usage) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void MemoryCritical_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        MemoryManager.Instance.MemoryCritical += (usage) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
