using FluentAssertions;
using SvonyBrowser.Services;
using Xunit;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for DebugService.
/// Tests debugging utilities, profiling, and diagnostics.
/// </summary>
public class DebugServiceTests : IDisposable
{
    private readonly DebugService _sut;

    public DebugServiceTests()
    {
        _sut = DebugService.Instance;
    }

    [Fact]
    public void Instance_ShouldReturnSingleton()
    {
        var instance1 = DebugService.Instance;
        var instance2 = DebugService.Instance;
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void EnableDebugMode_ShouldSetIsDebugModeTrue()
    {
        _sut.EnableDebugMode();
        _sut.IsDebugMode.Should().BeTrue();
    }

    [Fact]
    public void DisableDebugMode_ShouldSetIsDebugModeFalse()
    {
        _sut.DisableDebugMode();
        _sut.IsDebugMode.Should().BeFalse();
    }

    [Fact]
    public void StartTimer_ShouldNotThrow()
    {
        var act = () => _sut.StartTimer("test-timer");
        act.Should().NotThrow();
    }

    [Fact]
    public void StopTimer_ShouldReturnElapsedTime()
    {
        _sut.StartTimer("test-timer-2");
        Thread.Sleep(10);
        var elapsed = _sut.StopTimer("test-timer-2");
        
        elapsed.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void StopTimer_ForNonExistentTimer_ShouldReturnZero()
    {
        var elapsed = _sut.StopTimer("non-existent-timer");
        elapsed.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Measure_ShouldExecuteActionAndReturnResult()
    {
        var result = _sut.Measure("test-measure", () => 42);
        result.Should().Be(42);
    }

    [Fact]
    public async Task MeasureAsync_ShouldExecuteActionAndReturnResult()
    {
        var result = await _sut.MeasureAsync("test-measure-async", async () =>
        {
            await Task.Delay(1);
            return 42;
        });
        result.Should().Be(42);
    }

    [Fact]
    public void Log_WhenDebugModeEnabled_ShouldLogMessage()
    {
        _sut.EnableDebugMode();
        var act = () => _sut.Log("Test message {0}", "arg");
        act.Should().NotThrow();
    }

    [Fact]
    public void LogVariable_ShouldNotThrow()
    {
        _sut.EnableDebugMode();
        var act = () => _sut.LogVariable("testVar", 42);
        act.Should().NotThrow();
    }

    [Fact]
    public void GetMetrics_ShouldReturnMetrics()
    {
        _sut.StartTimer("metrics-test");
        _sut.StopTimer("metrics-test");
        
        var metrics = _sut.GetMetrics();
        metrics.Should().NotBeNull();
    }

    [Fact]
    public void GetMetric_ShouldReturnSpecificMetric()
    {
        var name = $"metric-{Guid.NewGuid()}";
        _sut.StartTimer(name);
        _sut.StopTimer(name);
        
        var metric = _sut.GetMetric(name);
        metric.Should().NotBeNull();
        metric!.CallCount.Should().Be(1);
    }

    [Fact]
    public void ClearMetrics_ShouldRemoveAllMetrics()
    {
        _sut.StartTimer("clear-test");
        _sut.StopTimer("clear-test");
        
        _sut.ClearMetrics();
        
        var metric = _sut.GetMetric("clear-test");
        metric.Should().BeNull();
    }

    [Fact]
    public void ClearLog_ShouldClearDebugLog()
    {
        _sut.EnableDebugMode();
        _sut.Log("Test message");
        _sut.ClearLog();
        
        var log = _sut.GetDebugLog();
        log.Should().BeEmpty();
    }

    [Fact]
    public void GetDebugLog_ShouldReturnLogContents()
    {
        _sut.EnableDebugMode();
        _sut.ClearLog();
        _sut.Log("Test message");
        
        var log = _sut.GetDebugLog();
        log.Should().Contain("Test message");
    }

    [Fact]
    public void Assert_WhenConditionTrue_ShouldNotThrow()
    {
        _sut.EnableDebugMode();
        var act = () => _sut.Assert(true, "Should not throw");
        act.Should().NotThrow();
    }

    [Fact]
    public void Assert_WhenConditionFalse_ShouldThrow()
    {
        _sut.EnableDebugMode();
        var act = () => _sut.Assert(false, "Should throw");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WriteDiagnostics_ShouldNotThrow()
    {
        var act = () => _sut.WriteDiagnostics();
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        _sut.DisableDebugMode();
        _sut.ClearMetrics();
        _sut.ClearLog();
    }
}
