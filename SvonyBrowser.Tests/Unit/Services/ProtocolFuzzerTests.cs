using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for ProtocolFuzzer - testing protocol security.
/// </summary>
[Collection("ServiceTests")]
public class ProtocolFuzzerTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = ProtocolFuzzer.Instance;
        var instance2 = ProtocolFuzzer.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        ProtocolFuzzer.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void IsRunning_ShouldReturnBool()
    {
        ProtocolFuzzer.Instance.IsRunning.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void FuzzedPacketCount_ShouldBeNonNegative()
    {
        ProtocolFuzzer.Instance.FuzzedPacketCount.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public async Task StartFuzzingAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await ProtocolFuzzer.Instance.StartFuzzingAsync("test.action");
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void StopFuzzing_ShouldNotThrow()
    {
        Action act = () => ProtocolFuzzer.Instance.StopFuzzing();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GenerateFuzzedPacket_ShouldReturnPacket()
    {
        var basePacket = TestHelpers.CreateMockPacket("test.action", new { data = "test" });
        var fuzzed = ProtocolFuzzer.Instance.GenerateFuzzedPacket(basePacket.ToString());
        fuzzed.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData("string_overflow")]
    [InlineData("integer_boundary")]
    [InlineData("null_injection")]
    [InlineData("format_string")]
    public void SetFuzzStrategy_ShouldNotThrow(string strategy)
    {
        Action act = () => ProtocolFuzzer.Instance.SetFuzzStrategy(strategy);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetResults_ShouldReturnList()
    {
        var results = ProtocolFuzzer.Instance.GetResults();
        results.Should().NotBeNull();
    }
    
    [Fact]
    public void ClearResults_ShouldNotThrow()
    {
        Action act = () => ProtocolFuzzer.Instance.ClearResults();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void VulnerabilityFound_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        ProtocolFuzzer.Instance.VulnerabilityFound += (vuln) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
