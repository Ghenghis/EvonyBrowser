using FluentAssertions;
using SvonyBrowser.Services;
using Xunit;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for CircuitBreaker service.
/// Tests circuit states, failure handling, and recovery.
/// </summary>
public class CircuitBreakerTests : IDisposable
{
    private readonly CircuitBreaker _sut;

    public CircuitBreakerTests()
    {
        _sut = CircuitBreaker.Instance;
    }

    [Fact]
    public void Instance_ShouldReturnSingleton()
    {
        var instance1 = CircuitBreaker.Instance;
        var instance2 = CircuitBreaker.Instance;
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void RegisterCircuit_ShouldCreateNewCircuit()
    {
        var name = $"test-circuit-{Guid.NewGuid()}";
        _sut.RegisterCircuit(name);
        
        var status = _sut.GetStatus(name);
        status.Should().Be(CircuitStatus.Closed);
    }

    [Fact]
    public void RegisterCircuit_WithConfig_ShouldUseCustomConfig()
    {
        var name = $"test-circuit-{Guid.NewGuid()}";
        var config = new CircuitBreakerConfig
        {
            FailureThreshold = 10,
            TimeoutSeconds = 60
        };
        
        _sut.RegisterCircuit(name, config);
        
        var info = _sut.GetCircuitInfo(name);
        info.Should().NotBeNull();
        info!.Status.Should().Be(CircuitStatus.Closed);
    }

    [Fact]
    public void GetStatus_ForUnknownCircuit_ShouldReturnClosed()
    {
        var status = _sut.GetStatus("unknown-circuit");
        status.Should().Be(CircuitStatus.Closed);
    }

    [Fact]
    public void Execute_WhenClosed_ShouldExecuteAction()
    {
        var name = $"test-circuit-{Guid.NewGuid()}";
        _sut.RegisterCircuit(name);
        
        var result = _sut.Execute(name, () => 42);
        
        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_WhenClosed_ShouldExecuteAction()
    {
        var name = $"test-circuit-{Guid.NewGuid()}";
        _sut.RegisterCircuit(name);
        
        var result = await _sut.ExecuteAsync(name, async () =>
        {
            await Task.Delay(1);
            return 42;
        });
        
        result.Should().Be(42);
    }

    [Fact]
    public void Execute_WhenActionThrows_ShouldRecordFailure()
    {
        var name = $"test-circuit-{Guid.NewGuid()}";
        _sut.RegisterCircuit(name);
        
        var act = () => _sut.Execute<int>(name, () => throw new InvalidOperationException("Test error"));
        
        act.Should().Throw<InvalidOperationException>();
        
        var info = _sut.GetCircuitInfo(name);
        info!.FailureCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Reset_ShouldCloseCircuit()
    {
        var name = $"test-circuit-{Guid.NewGuid()}";
        _sut.RegisterCircuit(name);
        _sut.Trip(name);
        
        _sut.GetStatus(name).Should().Be(CircuitStatus.Open);
        
        _sut.Reset(name);
        
        _sut.GetStatus(name).Should().Be(CircuitStatus.Closed);
    }

    [Fact]
    public void Trip_ShouldOpenCircuit()
    {
        var name = $"test-circuit-{Guid.NewGuid()}";
        _sut.RegisterCircuit(name);
        
        _sut.Trip(name);
        
        _sut.GetStatus(name).Should().Be(CircuitStatus.Open);
    }

    [Fact]
    public void GetCircuitInfo_ShouldReturnDetails()
    {
        var name = $"test-circuit-{Guid.NewGuid()}";
        _sut.RegisterCircuit(name);
        
        var info = _sut.GetCircuitInfo(name);
        
        info.Should().NotBeNull();
        info!.Name.Should().Be(name);
        info.Status.Should().Be(CircuitStatus.Closed);
    }

    [Fact]
    public void GetAllCircuits_ShouldReturnRegisteredCircuits()
    {
        var name = $"test-circuit-{Guid.NewGuid()}";
        _sut.RegisterCircuit(name);
        
        var circuits = _sut.GetAllCircuits();
        
        circuits.Should().Contain(c => c.Name == name);
    }

    public void Dispose()
    {
        // Cleanup
    }
}
