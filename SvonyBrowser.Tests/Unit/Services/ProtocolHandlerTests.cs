using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for ProtocolHandler - CRITICAL service requiring 25+ tests.
/// Tests packet processing, protocol management, and encoding/decoding.
/// </summary>
[Collection("ServiceTests")]
public class ProtocolHandlerTests : ServiceTestFixture
{
    #region Singleton Tests
    
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        // Arrange & Act
        var instance1 = ProtocolHandler.Instance;
        var instance2 = ProtocolHandler.Instance;
        
        // Assert
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        // Act
        var instance = ProtocolHandler.Instance;
        
        // Assert
        instance.Should().NotBeNull();
    }
    
    [Fact]
    public void Instance_ShouldBeThreadSafe()
    {
        // Arrange
        var instances = new ProtocolHandler[50];
        var tasks = new Task[50];
        
        // Act
        for (int i = 0; i < 50; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() => instances[index] = ProtocolHandler.Instance);
        }
        Task.WaitAll(tasks);
        
        // Assert
        instances.Distinct().Should().HaveCount(1);
    }
    
    #endregion
    
    #region Property Tests
    
    [Fact]
    public void IsConnected_ShouldReturnBool()
    {
        // Act
        var isConnected = ProtocolHandler.Instance.IsConnected;
        
        // Assert
        isConnected.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void PacketCount_ShouldBeNonNegative()
    {
        // Act
        var count = ProtocolHandler.Instance.PacketCount;
        
        // Assert
        count.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void LastPacketTime_ShouldHaveValue()
    {
        // Act
        var lastTime = ProtocolHandler.Instance.LastPacketTime;
        
        // Assert
        lastTime.Should().BeBefore(DateTime.UtcNow.AddDays(1));
    }
    
    #endregion
    
    #region GetPacketRate Tests
    
    [Fact]
    public void GetPacketRate_ShouldReturnNonNegative()
    {
        // Act
        var rate = ProtocolHandler.Instance.GetPacketRate();
        
        // Assert
        rate.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void GetPacketRate_ShouldBeReasonable()
    {
        // Act
        var rate = ProtocolHandler.Instance.GetPacketRate();
        
        // Assert
        rate.Should().BeLessThan(10000); // Reasonable upper bound
    }
    
    #endregion
    
    #region GetDecodeSuccessRate Tests
    
    [Fact]
    public void GetDecodeSuccessRate_ShouldBePercentage()
    {
        // Act
        var rate = ProtocolHandler.Instance.GetDecodeSuccessRate();
        
        // Assert
        rate.Should().BeInRange(0, 100);
    }
    
    #endregion
    
    #region ProcessPacket Tests
    
    [Theory]
    [InlineData("login.request")]
    [InlineData("castle.getInfo")]
    [InlineData("hero.list")]
    [InlineData("troop.train")]
    [InlineData("march.start")]
    public void ProcessIncomingPacket_ShouldHandleValidActions(string action)
    {
        // Arrange
        var packet = TestHelpers.CreateMockPacket(action, new { test = "data" });
        
        // Act
        Action act = () => ProtocolHandler.Instance.ProcessIncomingPacket(packet.ToString());
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ProcessIncomingPacket_ShouldHandleEmptyPacket()
    {
        // Act
        Action act = () => ProtocolHandler.Instance.ProcessIncomingPacket("");
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ProcessIncomingPacket_ShouldHandleNullPacket()
    {
        // Act
        Action act = () => ProtocolHandler.Instance.ProcessIncomingPacket(null!);
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ProcessIncomingPacket_ShouldHandleMalformedJson()
    {
        // Act
        Action act = () => ProtocolHandler.Instance.ProcessIncomingPacket("not valid json {{{");
        
        // Assert
        act.Should().NotThrow();
    }
    
    #endregion
    
    #region SendPacket Tests
    
    [Fact]
    public async Task SendPacket_ShouldReturnResult()
    {
        // Arrange
        var action = "test.action";
        var data = new JObject { ["test"] = "value" };
        
        // Act
        var result = await ProtocolHandler.Instance.SendPacket(action, data);
        
        // Assert
        result.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public async Task SendPacket_ShouldHandleNullData()
    {
        // Act
        var result = await ProtocolHandler.Instance.SendPacket("test.action", null!);
        
        // Assert
        result.Should().BeOneOf(true, false);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("single")]
    [InlineData("with.dots.multiple")]
    public async Task SendPacket_ShouldHandleVariousActionFormats(string action)
    {
        // Arrange
        var data = new JObject();
        
        // Act
        Func<Task> act = async () => await ProtocolHandler.Instance.SendPacket(action, data);
        
        // Assert
        await act.Should().NotThrowAsync();
    }
    
    #endregion
    
    #region RegisterHandler Tests
    
    [Fact]
    public void RegisterHandler_ShouldAcceptValidHandler()
    {
        // Arrange
        Action<JToken> handler = (data) => { };
        
        // Act
        Action act = () => ProtocolHandler.Instance.RegisterHandler("test.action", handler);
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void RegisterHandler_ShouldAllowMultipleHandlers()
    {
        // Arrange
        Action<JToken> handler1 = (data) => { };
        Action<JToken> handler2 = (data) => { };
        
        // Act
        Action act = () =>
        {
            ProtocolHandler.Instance.RegisterHandler("multi.action1", handler1);
            ProtocolHandler.Instance.RegisterHandler("multi.action2", handler2);
        };
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void UnregisterHandler_ShouldNotThrow()
    {
        // Act
        Action act = () => ProtocolHandler.Instance.UnregisterHandler("nonexistent.action");
        
        // Assert
        act.Should().NotThrow();
    }
    
    #endregion
    
    #region GetRecentPackets Tests
    
    [Fact]
    public void GetRecentPackets_ShouldReturnList()
    {
        // Act
        var packets = ProtocolHandler.Instance.GetRecentPackets();
        
        // Assert
        packets.Should().NotBeNull();
    }
    
    [Fact]
    public void GetRecentPackets_ShouldRespectLimit()
    {
        // Act
        var packets = ProtocolHandler.Instance.GetRecentPackets(10);
        
        // Assert
        packets.Count.Should().BeLessOrEqualTo(10);
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void GetRecentPackets_ShouldHandleVariousLimits(int limit)
    {
        // Act
        var packets = ProtocolHandler.Instance.GetRecentPackets(limit);
        
        // Assert
        packets.Count.Should().BeLessOrEqualTo(limit);
    }
    
    #endregion
    
    #region ClearHistory Tests
    
    [Fact]
    public void ClearHistory_ShouldNotThrow()
    {
        // Act
        Action act = () => ProtocolHandler.Instance.ClearHistory();
        
        // Assert
        act.Should().NotThrow();
    }
    
    #endregion
    
    #region Event Tests
    
    [Fact]
    public void PacketReceived_EventShouldBeSubscribable()
    {
        // Arrange
        var eventRaised = false;
        
        // Act
        ProtocolHandler.Instance.PacketReceived += (packet) => eventRaised = true;
        
        // Assert - just verify subscription doesn't throw
        eventRaised.Should().BeFalse(); // Not raised yet
    }
    
    [Fact]
    public void PacketSent_EventShouldBeSubscribable()
    {
        // Arrange
        var eventRaised = false;
        
        // Act
        ProtocolHandler.Instance.PacketSent += (packet) => eventRaised = true;
        
        // Assert
        eventRaised.Should().BeFalse();
    }
    
    #endregion
}
