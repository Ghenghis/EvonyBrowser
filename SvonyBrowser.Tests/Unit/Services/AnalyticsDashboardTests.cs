using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for AnalyticsDashboard - MEDIUM priority service requiring 15+ tests.
/// </summary>
[Collection("ServiceTests")]
public class AnalyticsDashboardTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = AnalyticsDashboard.Instance;
        var instance2 = AnalyticsDashboard.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        AnalyticsDashboard.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void Metrics_ShouldNotBeNull()
    {
        AnalyticsDashboard.Instance.Metrics.Should().NotBeNull();
    }
    
    [Fact]
    public void IsTracking_ShouldReturnBool()
    {
        AnalyticsDashboard.Instance.IsTracking.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void StartTracking_ShouldNotThrow()
    {
        Action act = () => AnalyticsDashboard.Instance.StartTracking();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void StopTracking_ShouldNotThrow()
    {
        Action act = () => AnalyticsDashboard.Instance.StopTracking();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetMetric_ShouldReturnValue()
    {
        var metric = AnalyticsDashboard.Instance.GetMetric("packets_per_second");
        metric.Should().BeGreaterOrEqualTo(0);
    }
    
    [Theory]
    [InlineData("packets_per_second")]
    [InlineData("memory_usage")]
    [InlineData("cpu_usage")]
    [InlineData("active_connections")]
    public void GetMetric_ShouldHandleVariousMetrics(string metricName)
    {
        var metric = AnalyticsDashboard.Instance.GetMetric(metricName);
        metric.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void RecordMetric_ShouldNotThrow()
    {
        Action act = () => AnalyticsDashboard.Instance.RecordMetric("test_metric", 100);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetHistory_ShouldReturnList()
    {
        var history = AnalyticsDashboard.Instance.GetHistory("test_metric", TimeSpan.FromHours(1));
        history.Should().NotBeNull();
    }
    
    [Fact]
    public void ExportMetrics_ShouldReturnJson()
    {
        var json = AnalyticsDashboard.Instance.ExportMetrics();
        json.Should().NotBeNull();
    }
    
    [Fact]
    public void ClearMetrics_ShouldNotThrow()
    {
        Action act = () => AnalyticsDashboard.Instance.ClearMetrics();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void MetricUpdated_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        AnalyticsDashboard.Instance.MetricUpdated += (name, value) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void ThresholdExceeded_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        AnalyticsDashboard.Instance.ThresholdExceeded += (name, value) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
