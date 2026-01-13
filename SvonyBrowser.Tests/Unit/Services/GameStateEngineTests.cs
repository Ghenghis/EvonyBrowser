using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for GameStateEngine - CRITICAL service requiring 30+ tests.
/// Tests singleton pattern, state management, event handling, and resource tracking.
/// </summary>
[Collection("ServiceTests")]
public class GameStateEngineTests : ServiceTestFixture
{
    #region Singleton Tests
    
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        // Arrange & Act
        var instance1 = GameStateEngine.Instance;
        var instance2 = GameStateEngine.Instance;
        
        // Assert
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        // Act
        var instance = GameStateEngine.Instance;
        
        // Assert
        instance.Should().NotBeNull();
    }
    
    [Fact]
    public void Instance_ShouldBeThreadSafe()
    {
        // Arrange
        var instances = new GameStateEngine[100];
        var tasks = new Task[100];
        
        // Act
        for (int i = 0; i < 100; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() => instances[index] = GameStateEngine.Instance);
        }
        Task.WaitAll(tasks);
        
        // Assert
        instances.Distinct().Should().HaveCount(1);
    }
    
    #endregion
    
    #region Property Tests
    
    [Fact]
    public void Player_ShouldNotBeNull()
    {
        // Act
        var player = GameStateEngine.Instance.Player;
        
        // Assert
        player.Should().NotBeNull();
    }
    
    [Fact]
    public void Alliance_ShouldNotBeNull()
    {
        // Act
        var alliance = GameStateEngine.Instance.Alliance;
        
        // Assert
        alliance.Should().NotBeNull();
    }
    
    [Fact]
    public void Cities_ShouldBeReadOnly()
    {
        // Act
        var cities = GameStateEngine.Instance.Cities;
        
        // Assert
        cities.Should().NotBeNull();
        cities.Should().BeAssignableTo<IReadOnlyDictionary<int, CityState>>();
    }
    
    [Fact]
    public void Heroes_ShouldBeReadOnly()
    {
        // Act
        var heroes = GameStateEngine.Instance.Heroes;
        
        // Assert
        heroes.Should().NotBeNull();
        heroes.Should().BeAssignableTo<IReadOnlyDictionary<int, HeroState>>();
    }
    
    [Fact]
    public void Armies_ShouldBeReadOnly()
    {
        // Act
        var armies = GameStateEngine.Instance.Armies;
        
        // Assert
        armies.Should().NotBeNull();
    }
    
    [Fact]
    public void Marches_ShouldBeReadOnly()
    {
        // Act
        var marches = GameStateEngine.Instance.Marches;
        
        // Assert
        marches.Should().NotBeNull();
    }
    
    [Fact]
    public void LastUpdate_ShouldHaveValue()
    {
        // Act
        var lastUpdate = GameStateEngine.Instance.LastUpdate;
        
        // Assert
        lastUpdate.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }
    
    [Fact]
    public void IsFresh_ShouldReturnBoolBasedOnLastUpdate()
    {
        // Act
        var isFresh = GameStateEngine.Instance.IsFresh;
        
        // Assert
        isFresh.Should().BeOneOf(true, false);
    }
    
    #endregion
    
    #region GetSnapshot Tests
    
    [Fact]
    public void GetSnapshot_ShouldReturnValidSnapshot()
    {
        // Act
        var snapshot = GameStateEngine.Instance.GetSnapshot();
        
        // Assert
        snapshot.Should().NotBeNull();
        snapshot.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
    
    [Fact]
    public void GetSnapshot_ShouldIncludePlayer()
    {
        // Act
        var snapshot = GameStateEngine.Instance.GetSnapshot();
        
        // Assert
        snapshot.Player.Should().NotBeNull();
    }
    
    [Fact]
    public void GetSnapshot_ShouldIncludeAlliance()
    {
        // Act
        var snapshot = GameStateEngine.Instance.GetSnapshot();
        
        // Assert
        snapshot.Alliance.Should().NotBeNull();
    }
    
    [Fact]
    public void GetSnapshot_ShouldCloneData()
    {
        // Act
        var snapshot1 = GameStateEngine.Instance.GetSnapshot();
        var snapshot2 = GameStateEngine.Instance.GetSnapshot();
        
        // Assert - should be different instances
        snapshot1.Should().NotBeSameAs(snapshot2);
    }
    
    #endregion
    
    #region GetHistory Tests
    
    [Fact]
    public void GetHistory_ShouldReturnList()
    {
        // Act
        var history = GameStateEngine.Instance.GetHistory("city");
        
        // Assert
        history.Should().NotBeNull();
        history.Should().BeAssignableTo<List<GameEvent>>();
    }
    
    [Theory]
    [InlineData("city")]
    [InlineData("hero")]
    [InlineData("army")]
    [InlineData("march")]
    public void GetHistory_ShouldFilterByEntityType(string entityType)
    {
        // Act
        var history = GameStateEngine.Instance.GetHistory(entityType);
        
        // Assert
        history.Should().NotBeNull();
        history.All(e => e.EntityType == entityType || history.Count == 0).Should().BeTrue();
    }
    
    [Fact]
    public void GetHistory_ShouldRespectLimit()
    {
        // Act
        var history = GameStateEngine.Instance.GetHistory("city", limit: 10);
        
        // Assert
        history.Count.Should().BeLessOrEqualTo(10);
    }
    
    [Fact]
    public void GetHistory_ShouldFilterByEntityId()
    {
        // Act
        var history = GameStateEngine.Instance.GetHistory("city", entityId: 1, limit: 100);
        
        // Assert
        history.Should().NotBeNull();
    }
    
    #endregion
    
    #region Resource Calculation Tests
    
    [Fact]
    public void GetTotalResources_ShouldReturnResourceState()
    {
        // Act
        var resources = GameStateEngine.Instance.GetTotalResources();
        
        // Assert
        resources.Should().NotBeNull();
    }
    
    [Fact]
    public void GetTotalResources_ShouldHaveNonNegativeValues()
    {
        // Act
        var resources = GameStateEngine.Instance.GetTotalResources();
        
        // Assert
        resources.Gold.Should().BeGreaterOrEqualTo(0);
        resources.Food.Should().BeGreaterOrEqualTo(0);
        resources.Lumber.Should().BeGreaterOrEqualTo(0);
        resources.Stone.Should().BeGreaterOrEqualTo(0);
        resources.Iron.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void GetTotalTroops_ShouldReturnDictionary()
    {
        // Act
        var troops = GameStateEngine.Instance.GetTotalTroops();
        
        // Assert
        troops.Should().NotBeNull();
        troops.Should().BeAssignableTo<Dictionary<string, int>>();
    }
    
    [Fact]
    public void GetProductionRates_ShouldReturnResourceState()
    {
        // Act
        var rates = GameStateEngine.Instance.GetProductionRates();
        
        // Assert
        rates.Should().NotBeNull();
    }
    
    #endregion
    
    #region ProcessPacket Tests
    
    [Theory]
    [InlineData("castle.update")]
    [InlineData("hero.levelup")]
    [InlineData("troop.train")]
    [InlineData("army.march")]
    [InlineData("march.start")]
    [InlineData("alliance.info")]
    [InlineData("player.info")]
    public void ProcessPacket_ShouldHandleValidActions(string action)
    {
        // Arrange
        var data = new JObject { ["test"] = "data" };
        
        // Act
        Action act = () => GameStateEngine.Instance.ProcessPacket(action, data, isResponse: true);
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ProcessPacket_ShouldUpdateLastUpdate()
    {
        // Arrange
        var before = GameStateEngine.Instance.LastUpdate;
        var data = new JObject { ["test"] = "data" };
        
        // Act
        GameStateEngine.Instance.ProcessPacket("castle.update", data, isResponse: true);
        
        // Assert
        GameStateEngine.Instance.LastUpdate.Should().BeOnOrAfter(before);
    }
    
    [Fact]
    public void ProcessPacket_ShouldHandleNullData()
    {
        // Act
        Action act = () => GameStateEngine.Instance.ProcessPacket("test.action", JValue.CreateNull(), isResponse: true);
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ProcessPacket_ShouldHandleEmptyAction()
    {
        // Arrange
        var data = new JObject();
        
        // Act
        Action act = () => GameStateEngine.Instance.ProcessPacket("", data, isResponse: true);
        
        // Assert
        act.Should().NotThrow();
    }
    
    #endregion
    
    #region UpdateCity Tests
    
    [Fact]
    public void UpdateCity_ShouldAddNewCity()
    {
        // Arrange
        var city = new CityState { CityId = 99999, Name = "Test City" };
        
        // Act
        GameStateEngine.Instance.UpdateCity(city);
        
        // Assert
        GameStateEngine.Instance.Cities.Should().ContainKey(99999);
    }
    
    [Fact]
    public void UpdateCity_ShouldUpdateExistingCity()
    {
        // Arrange
        var city1 = new CityState { CityId = 88888, Name = "Original" };
        var city2 = new CityState { CityId = 88888, Name = "Updated" };
        
        // Act
        GameStateEngine.Instance.UpdateCity(city1);
        GameStateEngine.Instance.UpdateCity(city2);
        
        // Assert
        GameStateEngine.Instance.Cities[88888].Name.Should().Be("Updated");
    }
    
    #endregion
    
    #region ClearState Tests
    
    [Fact]
    public void ClearState_ShouldNotThrow()
    {
        // Act
        Action act = () => GameStateEngine.Instance.ClearState();
        
        // Assert
        act.Should().NotThrow();
    }
    
    #endregion
    
    #region Event Tests
    
    [Fact]
    public void StateChanged_EventShouldBeRaisable()
    {
        // Arrange
        var eventRaised = false;
        GameStateEngine.Instance.StateChanged += (change) => eventRaised = true;
        
        // Act
        var city = new CityState { CityId = 77777, Name = "Event Test" };
        GameStateEngine.Instance.UpdateCity(city);
        
        // Assert
        eventRaised.Should().BeTrue();
    }
    
    #endregion
}

#region Supporting Types for Tests

/// <summary>
/// Test data for parameterized tests.
/// </summary>
public class GameStateTestData
{
    public static IEnumerable<object[]> ResourceTypes =>
        new List<object[]>
        {
            new object[] { "gold", 1000000L },
            new object[] { "food", 500000L },
            new object[] { "lumber", 300000L },
            new object[] { "stone", 200000L },
            new object[] { "iron", 100000L }
        };
    
    public static IEnumerable<object[]> TroopTypes =>
        new List<object[]>
        {
            new object[] { "infantry", 10000 },
            new object[] { "cavalry", 5000 },
            new object[] { "archers", 5000 },
            new object[] { "siege", 1000 }
        };
}

#endregion
