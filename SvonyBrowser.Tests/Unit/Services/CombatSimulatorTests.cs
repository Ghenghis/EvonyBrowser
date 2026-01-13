using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for CombatSimulator - HIGH priority service requiring 25+ tests.
/// Tests battle calculations, troop simulations, and combat predictions.
/// </summary>
[Collection("ServiceTests")]
public class CombatSimulatorTests : ServiceTestFixture
{
    #region Singleton Tests
    
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = CombatSimulator.Instance;
        var instance2 = CombatSimulator.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        var instance = CombatSimulator.Instance;
        instance.Should().NotBeNull();
    }
    
    #endregion
    
    #region SimulateBattle Tests
    
    [Fact]
    public void SimulateBattle_ShouldReturnResult()
    {
        var attacker = CreateTestArmy(10000, 5000, 5000, 1000);
        var defender = CreateTestArmy(8000, 4000, 4000, 500);
        
        var result = CombatSimulator.Instance.SimulateBattle(attacker, defender);
        
        result.Should().NotBeNull();
    }
    
    [Fact]
    public void SimulateBattle_ShouldDetermineWinner()
    {
        var attacker = CreateTestArmy(10000, 5000, 5000, 1000);
        var defender = CreateTestArmy(1000, 500, 500, 100);
        
        var result = CombatSimulator.Instance.SimulateBattle(attacker, defender);
        
        result.Winner.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public void SimulateBattle_ShouldCalculateLosses()
    {
        var attacker = CreateTestArmy(10000, 5000, 5000, 1000);
        var defender = CreateTestArmy(8000, 4000, 4000, 500);
        
        var result = CombatSimulator.Instance.SimulateBattle(attacker, defender);
        
        result.AttackerLosses.Should().NotBeNull();
        result.DefenderLosses.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData(10000, 1000)]
    [InlineData(5000, 5000)]
    [InlineData(1000, 10000)]
    public void SimulateBattle_ShouldHandleVariousArmySizes(int attackerSize, int defenderSize)
    {
        var attacker = CreateTestArmy(attackerSize, 0, 0, 0);
        var defender = CreateTestArmy(defenderSize, 0, 0, 0);
        
        var result = CombatSimulator.Instance.SimulateBattle(attacker, defender);
        
        result.Should().NotBeNull();
    }
    
    [Fact]
    public void SimulateBattle_ShouldHandleEmptyArmies()
    {
        var attacker = CreateTestArmy(0, 0, 0, 0);
        var defender = CreateTestArmy(0, 0, 0, 0);
        
        Action act = () => CombatSimulator.Instance.SimulateBattle(attacker, defender);
        
        act.Should().NotThrow();
    }
    
    #endregion
    
    #region CalculatePower Tests
    
    [Fact]
    public void CalculatePower_ShouldReturnNonNegative()
    {
        var army = CreateTestArmy(10000, 5000, 5000, 1000);
        
        var power = CombatSimulator.Instance.CalculatePower(army);
        
        power.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void CalculatePower_ShouldScaleWithTroops()
    {
        var smallArmy = CreateTestArmy(1000, 500, 500, 100);
        var largeArmy = CreateTestArmy(10000, 5000, 5000, 1000);
        
        var smallPower = CombatSimulator.Instance.CalculatePower(smallArmy);
        var largePower = CombatSimulator.Instance.CalculatePower(largeArmy);
        
        largePower.Should().BeGreaterThan(smallPower);
    }
    
    [Fact]
    public void CalculatePower_ShouldHandleEmptyArmy()
    {
        var army = CreateTestArmy(0, 0, 0, 0);
        
        var power = CombatSimulator.Instance.CalculatePower(army);
        
        power.Should().Be(0);
    }
    
    #endregion
    
    #region PredictOutcome Tests
    
    [Fact]
    public void PredictOutcome_ShouldReturnPrediction()
    {
        var attacker = CreateTestArmy(10000, 5000, 5000, 1000);
        var defender = CreateTestArmy(8000, 4000, 4000, 500);
        
        var prediction = CombatSimulator.Instance.PredictOutcome(attacker, defender);
        
        prediction.Should().NotBeNull();
    }
    
    [Fact]
    public void PredictOutcome_ShouldIncludeWinProbability()
    {
        var attacker = CreateTestArmy(10000, 5000, 5000, 1000);
        var defender = CreateTestArmy(8000, 4000, 4000, 500);
        
        var prediction = CombatSimulator.Instance.PredictOutcome(attacker, defender);
        
        prediction.WinProbability.Should().BeInRange(0, 100);
    }
    
    [Fact]
    public void PredictOutcome_ShouldEstimateLosses()
    {
        var attacker = CreateTestArmy(10000, 5000, 5000, 1000);
        var defender = CreateTestArmy(8000, 4000, 4000, 500);
        
        var prediction = CombatSimulator.Instance.PredictOutcome(attacker, defender);
        
        prediction.EstimatedLosses.Should().BeGreaterOrEqualTo(0);
    }
    
    #endregion
    
    #region GetTroopStats Tests
    
    [Theory]
    [InlineData("infantry")]
    [InlineData("cavalry")]
    [InlineData("archers")]
    [InlineData("siege")]
    public void GetTroopStats_ShouldReturnStats(string troopType)
    {
        var stats = CombatSimulator.Instance.GetTroopStats(troopType);
        
        stats.Should().NotBeNull();
    }
    
    [Fact]
    public void GetTroopStats_ShouldHandleUnknownType()
    {
        var stats = CombatSimulator.Instance.GetTroopStats("unknown_type");
        
        stats.Should().NotBeNull();
    }
    
    [Fact]
    public void GetAllTroopStats_ShouldReturnDictionary()
    {
        var allStats = CombatSimulator.Instance.GetAllTroopStats();
        
        allStats.Should().NotBeNull();
        allStats.Should().NotBeEmpty();
    }
    
    #endregion
    
    #region Hero Bonus Tests
    
    [Fact]
    public void CalculateHeroBonus_ShouldReturnBonus()
    {
        var hero = TestHelpers.CreateMockHero(1, "Test Hero", 30, 5);
        
        var bonus = CombatSimulator.Instance.CalculateHeroBonus(hero);
        
        bonus.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void CalculateHeroBonus_ShouldScaleWithLevel()
    {
        var lowLevelHero = TestHelpers.CreateMockHero(1, "Low Level", 10, 1);
        var highLevelHero = TestHelpers.CreateMockHero(2, "High Level", 50, 5);
        
        var lowBonus = CombatSimulator.Instance.CalculateHeroBonus(lowLevelHero);
        var highBonus = CombatSimulator.Instance.CalculateHeroBonus(highLevelHero);
        
        highBonus.Should().BeGreaterThan(lowBonus);
    }
    
    #endregion
    
    #region Terrain Tests
    
    [Theory]
    [InlineData("plains")]
    [InlineData("forest")]
    [InlineData("mountain")]
    [InlineData("desert")]
    public void GetTerrainModifier_ShouldReturnModifier(string terrain)
    {
        var modifier = CombatSimulator.Instance.GetTerrainModifier(terrain);
        
        modifier.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public void GetTerrainModifier_ShouldHandleUnknownTerrain()
    {
        var modifier = CombatSimulator.Instance.GetTerrainModifier("unknown");
        
        modifier.Should().Be(1.0); // Default modifier
    }
    
    #endregion
    
    #region Event Tests
    
    [Fact]
    public void SimulationCompleted_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        CombatSimulator.Instance.SimulationCompleted += (result) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    #endregion
    
    #region Helper Methods
    
    private static Dictionary<string, int> CreateTestArmy(int infantry, int cavalry, int archers, int siege)
    {
        return new Dictionary<string, int>
        {
            ["infantry"] = infantry,
            ["cavalry"] = cavalry,
            ["archers"] = archers,
            ["siege"] = siege
        };
    }
    
    #endregion
}

#region Supporting Types

public class BattleResult
{
    public string Winner { get; set; } = "";
    public Dictionary<string, int> AttackerLosses { get; set; } = new();
    public Dictionary<string, int> DefenderLosses { get; set; } = new();
    public double BattleDuration { get; set; }
}

public class BattlePrediction
{
    public double WinProbability { get; set; }
    public int EstimatedLosses { get; set; }
    public string Recommendation { get; set; } = "";
}

public class TroopStats
{
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Health { get; set; }
    public int Speed { get; set; }
}

#endregion
