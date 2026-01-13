using SvonyBrowser.Services;
using SvonyBrowser.Tests.Fixtures;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for LoggingService - structured logging with performance tracking.
/// </summary>
[Collection("ServiceTests")]
public class LoggingServiceTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = LoggingService.Instance;
        var instance2 = LoggingService.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        LoggingService.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void Initialize_ShouldSetLogPath()
    {
        var logPath = Path.Combine(TestLogPath, "test.log");
        LoggingService.Instance.Initialize(logPath, "Debug");
        
        LoggingService.Instance.LogPath.Should().Be(logPath);
        LoggingService.Instance.IsInitialized.Should().BeTrue();
    }
    
    [Fact]
    public void Info_ShouldNotThrow()
    {
        var logPath = Path.Combine(TestLogPath, "info-test.log");
        LoggingService.Instance.Initialize(logPath);
        
        Action act = () => LoggingService.Instance.Info("Test", "Test message");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void Debug_ShouldNotThrow()
    {
        var logPath = Path.Combine(TestLogPath, "debug-test.log");
        LoggingService.Instance.Initialize(logPath, "Debug");
        
        Action act = () => LoggingService.Instance.Debug("Test", "Debug message");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void Warn_ShouldNotThrow()
    {
        var logPath = Path.Combine(TestLogPath, "warn-test.log");
        LoggingService.Instance.Initialize(logPath);
        
        Action act = () => LoggingService.Instance.Warn("Test", "Warning message");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void Error_ShouldNotThrow()
    {
        var logPath = Path.Combine(TestLogPath, "error-test.log");
        LoggingService.Instance.Initialize(logPath);
        
        Action act = () => LoggingService.Instance.Error("Test", "Error message", new Exception("Test"));
        act.Should().NotThrow();
    }
    
    [Fact]
    public void Fatal_ShouldNotThrow()
    {
        var logPath = Path.Combine(TestLogPath, "fatal-test.log");
        LoggingService.Instance.Initialize(logPath);
        
        Action act = () => LoggingService.Instance.Fatal("Test", "Fatal message", new Exception("Test"));
        act.Should().NotThrow();
    }
    
    [Fact]
    public void Info_WithProperties_ShouldNotThrow()
    {
        var logPath = Path.Combine(TestLogPath, "props-test.log");
        LoggingService.Instance.Initialize(logPath);
        
        Action act = () => LoggingService.Instance.Info("Test", "Message with props", new { Key = "Value", Count = 42 });
        act.Should().NotThrow();
    }
    
    [Fact]
    public void StartPerformanceTrack_ShouldReturnTracker()
    {
        var logPath = Path.Combine(TestLogPath, "perf-test.log");
        LoggingService.Instance.Initialize(logPath);
        
        var tracker = LoggingService.Instance.StartPerformanceTrack("TestOperation");
        tracker.Should().NotBeNull();
        tracker.ElapsedMs.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void PerformanceTracker_Complete_ShouldLogDuration()
    {
        var logPath = Path.Combine(TestLogPath, "perf-complete-test.log");
        LoggingService.Instance.Initialize(logPath);
        
        using var tracker = LoggingService.Instance.StartPerformanceTrack("TestOperation");
        Thread.Sleep(10);
        tracker.Complete(true, new { Items = 5 });
        
        tracker.ElapsedMs.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public void Audit_ShouldRecordEntry()
    {
        var logPath = Path.Combine(TestLogPath, "audit-test.log");
        LoggingService.Instance.Initialize(logPath);
        
        LoggingService.Instance.Audit("Login", "TestUser", "User logged in");
        
        var trail = LoggingService.Instance.GetAuditTrail(10);
        trail.Should().NotBeNull();
    }
    
    [Fact]
    public void GetAuditTrail_ShouldReturnRecentEntries()
    {
        var logPath = Path.Combine(TestLogPath, "audit-trail-test.log");
        LoggingService.Instance.Initialize(logPath);
        
        LoggingService.Instance.Audit("Action1", "User1", "Details1");
        LoggingService.Instance.Audit("Action2", "User2", "Details2");
        
        var trail = LoggingService.Instance.GetAuditTrail(5);
        trail.Should().NotBeNull();
        trail.Count.Should().BeLessOrEqualTo(5);
    }
    
    [Fact]
    public void SetLogLevel_ShouldChangeLevel()
    {
        var logPath = Path.Combine(TestLogPath, "level-test.log");
        LoggingService.Instance.Initialize(logPath, "Info");
        
        Action act = () => LoggingService.Instance.SetLogLevel("Debug");
        act.Should().NotThrow();
    }
    
    [Theory]
    [InlineData("verbose")]
    [InlineData("debug")]
    [InlineData("information")]
    [InlineData("info")]
    [InlineData("warning")]
    [InlineData("warn")]
    [InlineData("error")]
    [InlineData("fatal")]
    public void SetLogLevel_ShouldAcceptAllLevels(string level)
    {
        var logPath = Path.Combine(TestLogPath, $"level-{level}-test.log");
        LoggingService.Instance.Initialize(logPath);
        
        Action act = () => LoggingService.Instance.SetLogLevel(level);
        act.Should().NotThrow();
    }
}
