using FluentAssertions;
using SvonyBrowser.Services;
using Xunit;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for MemoryGuard service.
/// Tests memory monitoring, garbage collection, and pressure detection.
/// </summary>
public class MemoryGuardTests : IDisposable
{
    private readonly MemoryGuard _sut;

    public MemoryGuardTests()
    {
        _sut = MemoryGuard.Instance;
    }

    [Fact]
    public void Instance_ShouldReturnSingleton()
    {
        var instance1 = MemoryGuard.Instance;
        var instance2 = MemoryGuard.Instance;
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithNormalPressure()
    {
        _sut.PressureLevel.Should().Be(MemoryPressureLevel.Normal);
    }

    [Fact]
    public void StartMonitoring_ShouldNotThrow()
    {
        var act = () => _sut.StartMonitoring();
        act.Should().NotThrow();
    }

    [Fact]
    public void StopMonitoring_ShouldNotThrow()
    {
        _sut.StartMonitoring();
        var act = () => _sut.StopMonitoring();
        act.Should().NotThrow();
    }

    [Fact]
    public void PerformLightCleanup_ShouldNotThrow()
    {
        var act = () => _sut.PerformLightCleanup();
        act.Should().NotThrow();
    }

    [Fact]
    public void PerformAggressiveCleanup_ShouldNotThrow()
    {
        var act = () => _sut.PerformAggressiveCleanup();
        act.Should().NotThrow();
    }

    [Fact]
    public void PerformEmergencyCleanup_ShouldNotThrow()
    {
        var act = () => _sut.PerformEmergencyCleanup();
        act.Should().NotThrow();
    }

    [Fact]
    public void ForceGC_ShouldPerformCleanup()
    {
        var act = () => _sut.ForceGC();
        act.Should().NotThrow();
    }

    [Fact]
    public void GetStatistics_ShouldReturnValidData()
    {
        var stats = _sut.GetStatistics();
        
        stats.Should().NotBeNull();
        stats.ManagedMemoryMB.Should().BeGreaterOrEqualTo(0);
        stats.WorkingSetMB.Should().BeGreaterOrEqualTo(0);
        stats.Gen0Collections.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void CurrentMemoryMB_ShouldBeNonNegative()
    {
        _sut.CurrentMemoryMB.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void PeakMemoryMB_ShouldBeNonNegative()
    {
        _sut.PeakMemoryMB.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void GCCollectionCount_ShouldBeNonNegative()
    {
        _sut.GCCollectionCount.Should().BeGreaterOrEqualTo(0);
    }

    public void Dispose()
    {
        _sut.StopMonitoring();
    }
}
