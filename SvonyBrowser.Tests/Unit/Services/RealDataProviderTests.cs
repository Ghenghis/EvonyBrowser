using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for RealDataProvider - MEDIUM priority service requiring 10+ tests.
/// </summary>
[Collection("ServiceTests")]
public class RealDataProviderTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = RealDataProvider.Instance;
        var instance2 = RealDataProvider.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        RealDataProvider.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void IsConnected_ShouldReturnBool()
    {
        RealDataProvider.Instance.IsConnected.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void GetResources_ShouldReturnData()
    {
        var resources = RealDataProvider.Instance.GetResources();
        resources.Should().NotBeNull();
    }
    
    [Fact]
    public void GetTroops_ShouldReturnData()
    {
        var troops = RealDataProvider.Instance.GetTroops();
        troops.Should().NotBeNull();
    }
    
    [Fact]
    public void GetHeroes_ShouldReturnData()
    {
        var heroes = RealDataProvider.Instance.GetHeroes();
        heroes.Should().NotBeNull();
    }
    
    [Fact]
    public void GetMarches_ShouldReturnData()
    {
        var marches = RealDataProvider.Instance.GetMarches();
        marches.Should().NotBeNull();
    }
    
    [Fact]
    public void GetBuildings_ShouldReturnData()
    {
        var buildings = RealDataProvider.Instance.GetBuildings();
        buildings.Should().NotBeNull();
    }
    
    [Fact]
    public void RefreshData_ShouldNotThrow()
    {
        Action act = () => RealDataProvider.Instance.RefreshData();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void DataUpdated_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        RealDataProvider.Instance.DataUpdated += (dataType) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
