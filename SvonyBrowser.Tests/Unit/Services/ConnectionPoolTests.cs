using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for ConnectionPool - HIGH priority for failsafes.
/// </summary>
[Collection("ServiceTests")]
public class ConnectionPoolTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = ConnectionPool.Instance;
        var instance2 = ConnectionPool.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        ConnectionPool.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void ActiveConnections_ShouldBeNonNegative()
    {
        ConnectionPool.Instance.ActiveConnections.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void MaxConnections_ShouldBePositive()
    {
        ConnectionPool.Instance.MaxConnections.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public void AvailableConnections_ShouldBeNonNegative()
    {
        ConnectionPool.Instance.AvailableConnections.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public async Task GetConnectionAsync_ShouldReturnConnection()
    {
        var connection = await ConnectionPool.Instance.GetConnectionAsync();
        connection.Should().NotBeNull();
    }
    
    [Fact]
    public void ReleaseConnection_ShouldNotThrow()
    {
        Action act = () => ConnectionPool.Instance.ReleaseConnection("test_connection");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void SetMaxConnections_ShouldNotThrow()
    {
        Action act = () => ConnectionPool.Instance.SetMaxConnections(100);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetStatistics_ShouldReturnStats()
    {
        var stats = ConnectionPool.Instance.GetStatistics();
        stats.Should().NotBeNull();
    }
    
    [Fact]
    public void ClearPool_ShouldNotThrow()
    {
        Action act = () => ConnectionPool.Instance.ClearPool();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ConnectionAcquired_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        ConnectionPool.Instance.ConnectionAcquired += (id) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void ConnectionReleased_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        ConnectionPool.Instance.ConnectionReleased += (id) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void PoolExhausted_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        ConnectionPool.Instance.PoolExhausted += () => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
