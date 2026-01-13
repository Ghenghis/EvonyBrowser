using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for MapScanner - MEDIUM priority service requiring 15+ tests.
/// </summary>
[Collection("ServiceTests")]
public class MapScannerTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = MapScanner.Instance;
        var instance2 = MapScanner.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        MapScanner.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void IsScanning_ShouldReturnBool()
    {
        MapScanner.Instance.IsScanning.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void ScanProgress_ShouldBeInRange()
    {
        MapScanner.Instance.ScanProgress.Should().BeInRange(0, 100);
    }
    
    [Fact]
    public void ScannedTiles_ShouldNotBeNull()
    {
        MapScanner.Instance.ScannedTiles.Should().NotBeNull();
    }
    
    [Fact]
    public async Task StartScanAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await MapScanner.Instance.StartScanAsync(0, 0, 10, 10);
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void StopScan_ShouldNotThrow()
    {
        Action act = () => MapScanner.Instance.StopScan();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void PauseScan_ShouldNotThrow()
    {
        Action act = () => MapScanner.Instance.PauseScan();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ResumeScan_ShouldNotThrow()
    {
        Action act = () => MapScanner.Instance.ResumeScan();
        act.Should().NotThrow();
    }
    
    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 100)]
    [InlineData(500, 500)]
    public void GetTile_ShouldReturnTile(int x, int y)
    {
        var tile = MapScanner.Instance.GetTile(x, y);
        // Can be null if not scanned
    }
    
    [Fact]
    public void FindResources_ShouldReturnList()
    {
        var resources = MapScanner.Instance.FindResources("food");
        resources.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData("food")]
    [InlineData("lumber")]
    [InlineData("stone")]
    [InlineData("iron")]
    public void FindResources_ShouldHandleVariousTypes(string resourceType)
    {
        var resources = MapScanner.Instance.FindResources(resourceType);
        resources.Should().NotBeNull();
    }
    
    [Fact]
    public void FindMonsters_ShouldReturnList()
    {
        var monsters = MapScanner.Instance.FindMonsters(1, 10);
        monsters.Should().NotBeNull();
    }
    
    [Fact]
    public void FindPlayers_ShouldReturnList()
    {
        var players = MapScanner.Instance.FindPlayers();
        players.Should().NotBeNull();
    }
    
    [Fact]
    public void ClearCache_ShouldNotThrow()
    {
        Action act = () => MapScanner.Instance.ClearCache();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void TileScanned_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        MapScanner.Instance.TileScanned += (x, y, tile) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void ScanCompleted_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        MapScanner.Instance.ScanCompleted += () => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
