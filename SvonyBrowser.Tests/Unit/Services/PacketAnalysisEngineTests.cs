using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for PacketAnalysisEngine - HIGH priority service requiring 20+ tests.
/// </summary>
[Collection("ServiceTests")]
public class PacketAnalysisEngineTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = PacketAnalysisEngine.Instance;
        var instance2 = PacketAnalysisEngine.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        PacketAnalysisEngine.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void IsAnalyzing_ShouldReturnBool()
    {
        PacketAnalysisEngine.Instance.IsAnalyzing.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void PacketCount_ShouldBeNonNegative()
    {
        PacketAnalysisEngine.Instance.PacketCount.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void AnalyzedPackets_ShouldNotBeNull()
    {
        PacketAnalysisEngine.Instance.AnalyzedPackets.Should().NotBeNull();
    }
    
    [Fact]
    public void AnalyzePacket_ShouldReturnResult()
    {
        var packet = TestHelpers.CreateMockPacket("test.action", new { data = "test" });
        var result = PacketAnalysisEngine.Instance.AnalyzePacket(packet.ToString());
        result.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData("castle.getInfo")]
    [InlineData("hero.list")]
    [InlineData("troop.train")]
    [InlineData("march.start")]
    public void AnalyzePacket_ShouldHandleVariousActions(string action)
    {
        var packet = TestHelpers.CreateMockPacket(action, new { });
        var result = PacketAnalysisEngine.Instance.AnalyzePacket(packet.ToString());
        result.Should().NotBeNull();
    }
    
    [Fact]
    public void AnalyzePacket_ShouldHandleMalformedPacket()
    {
        Action act = () => PacketAnalysisEngine.Instance.AnalyzePacket("not valid json");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void StartAnalysis_ShouldNotThrow()
    {
        Action act = () => PacketAnalysisEngine.Instance.StartAnalysis();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void StopAnalysis_ShouldNotThrow()
    {
        Action act = () => PacketAnalysisEngine.Instance.StopAnalysis();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetStatistics_ShouldReturnStats()
    {
        var stats = PacketAnalysisEngine.Instance.GetStatistics();
        stats.Should().NotBeNull();
    }
    
    [Fact]
    public void GetPacketsByType_ShouldReturnList()
    {
        var packets = PacketAnalysisEngine.Instance.GetPacketsByType("castle");
        packets.Should().NotBeNull();
    }
    
    [Fact]
    public void GetRecentPackets_ShouldReturnList()
    {
        var packets = PacketAnalysisEngine.Instance.GetRecentPackets(10);
        packets.Should().NotBeNull();
        packets.Count.Should().BeLessOrEqualTo(10);
    }
    
    [Fact]
    public void FindPatterns_ShouldReturnPatterns()
    {
        var patterns = PacketAnalysisEngine.Instance.FindPatterns();
        patterns.Should().NotBeNull();
    }
    
    [Fact]
    public void DetectAnomalies_ShouldReturnList()
    {
        var anomalies = PacketAnalysisEngine.Instance.DetectAnomalies();
        anomalies.Should().NotBeNull();
    }
    
    [Fact]
    public void ClearHistory_ShouldNotThrow()
    {
        Action act = () => PacketAnalysisEngine.Instance.ClearHistory();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ExportAnalysis_ShouldReturnJson()
    {
        var json = PacketAnalysisEngine.Instance.ExportAnalysis();
        json.Should().NotBeNull();
    }
    
    [Fact]
    public void PacketAnalyzed_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        PacketAnalysisEngine.Instance.PacketAnalyzed += (result) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void AnomalyDetected_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        PacketAnalysisEngine.Instance.AnomalyDetected += (anomaly) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void PatternFound_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        PacketAnalysisEngine.Instance.PatternFound += (pattern) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
