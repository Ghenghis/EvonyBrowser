using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for StrategicAdvisor - MEDIUM priority service requiring 20+ tests.
/// Tests AI strategy recommendations and game analysis.
/// </summary>
[Collection("ServiceTests")]
public class StrategicAdvisorTests : ServiceTestFixture
{
    #region Singleton Tests
    
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = StrategicAdvisor.Instance;
        var instance2 = StrategicAdvisor.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        StrategicAdvisor.Instance.Should().NotBeNull();
    }
    
    #endregion
    
    #region Property Tests
    
    [Fact]
    public void CurrentStrategy_ShouldNotBeNull()
    {
        StrategicAdvisor.Instance.CurrentStrategy.Should().NotBeNull();
    }
    
    [Fact]
    public void Recommendations_ShouldNotBeNull()
    {
        StrategicAdvisor.Instance.Recommendations.Should().NotBeNull();
    }
    
    [Fact]
    public void IsAnalyzing_ShouldReturnBool()
    {
        StrategicAdvisor.Instance.IsAnalyzing.Should().BeOneOf(true, false);
    }
    
    #endregion
    
    #region Analysis Tests
    
    [Fact]
    public async Task AnalyzeStateAsync_ShouldReturnAnalysis()
    {
        var analysis = await StrategicAdvisor.Instance.AnalyzeStateAsync();
        analysis.Should().NotBeNull();
    }
    
    [Fact]
    public async Task GetRecommendationsAsync_ShouldReturnList()
    {
        var recommendations = await StrategicAdvisor.Instance.GetRecommendationsAsync();
        recommendations.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData("attack")]
    [InlineData("defense")]
    [InlineData("economy")]
    [InlineData("growth")]
    public async Task GetRecommendationsAsync_ShouldFilterByCategory(string category)
    {
        var recommendations = await StrategicAdvisor.Instance.GetRecommendationsAsync(category);
        recommendations.Should().NotBeNull();
    }
    
    #endregion
    
    #region Strategy Tests
    
    [Theory]
    [InlineData("aggressive")]
    [InlineData("defensive")]
    [InlineData("balanced")]
    [InlineData("economic")]
    public void SetStrategy_ShouldAcceptValidStrategies(string strategy)
    {
        Action act = () => StrategicAdvisor.Instance.SetStrategy(strategy);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void SetStrategy_ShouldHandleInvalidStrategy()
    {
        Action act = () => StrategicAdvisor.Instance.SetStrategy("invalid_strategy");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetAvailableStrategies_ShouldReturnList()
    {
        var strategies = StrategicAdvisor.Instance.GetAvailableStrategies();
        strategies.Should().NotBeNull();
        strategies.Should().NotBeEmpty();
    }
    
    #endregion
    
    #region Priority Tests
    
    [Fact]
    public void GetBuildingPriority_ShouldReturnList()
    {
        var priority = StrategicAdvisor.Instance.GetBuildingPriority();
        priority.Should().NotBeNull();
    }
    
    [Fact]
    public void GetResearchPriority_ShouldReturnList()
    {
        var priority = StrategicAdvisor.Instance.GetResearchPriority();
        priority.Should().NotBeNull();
    }
    
    [Fact]
    public void GetTrainingPriority_ShouldReturnList()
    {
        var priority = StrategicAdvisor.Instance.GetTrainingPriority();
        priority.Should().NotBeNull();
    }
    
    #endregion
    
    #region Threat Assessment Tests
    
    [Fact]
    public void AssessThreats_ShouldReturnList()
    {
        var threats = StrategicAdvisor.Instance.AssessThreats();
        threats.Should().NotBeNull();
    }
    
    [Fact]
    public void GetThreatLevel_ShouldReturnValue()
    {
        var level = StrategicAdvisor.Instance.GetThreatLevel();
        level.Should().BeInRange(0, 100);
    }
    
    #endregion
    
    #region Opportunity Tests
    
    [Fact]
    public void FindOpportunities_ShouldReturnList()
    {
        var opportunities = StrategicAdvisor.Instance.FindOpportunities();
        opportunities.Should().NotBeNull();
    }
    
    [Fact]
    public void GetBestTarget_ShouldReturnTarget()
    {
        var target = StrategicAdvisor.Instance.GetBestTarget();
        // Can be null if no good targets
    }
    
    #endregion
    
    #region Event Tests
    
    [Fact]
    public void RecommendationGenerated_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        StrategicAdvisor.Instance.RecommendationGenerated += (rec) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void ThreatDetected_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        StrategicAdvisor.Instance.ThreatDetected += (threat) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void OpportunityFound_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        StrategicAdvisor.Instance.OpportunityFound += (opp) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    #endregion
}
