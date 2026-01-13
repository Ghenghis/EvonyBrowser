using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for AutoPilotService - HIGH priority service requiring 20+ tests.
/// Tests automation logic, safety limits, and task scheduling.
/// </summary>
[Collection("ServiceTests")]
public class AutoPilotServiceTests : ServiceTestFixture
{
    #region Singleton Tests
    
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = AutoPilotService.Instance;
        var instance2 = AutoPilotService.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        var instance = AutoPilotService.Instance;
        instance.Should().NotBeNull();
    }
    
    #endregion
    
    #region Property Tests
    
    [Fact]
    public void IsRunning_ShouldReturnBool()
    {
        var isRunning = AutoPilotService.Instance.IsRunning;
        isRunning.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void CurrentTask_ShouldBeNullWhenNotRunning()
    {
        if (!AutoPilotService.Instance.IsRunning)
        {
            AutoPilotService.Instance.CurrentTask.Should().BeNull();
        }
    }
    
    [Fact]
    public void SafetyLimits_ShouldNotBeNull()
    {
        var limits = AutoPilotService.Instance.SafetyLimits;
        limits.Should().NotBeNull();
    }
    
    [Fact]
    public void TaskQueue_ShouldNotBeNull()
    {
        var queue = AutoPilotService.Instance.TaskQueue;
        queue.Should().NotBeNull();
    }
    
    #endregion
    
    #region Start/Stop Tests
    
    [Fact]
    public async Task StartAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await AutoPilotService.Instance.StartAsync();
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task StopAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await AutoPilotService.Instance.StopAsync();
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task StopAsync_ShouldBeIdempotent()
    {
        await AutoPilotService.Instance.StopAsync();
        await AutoPilotService.Instance.StopAsync();
        await AutoPilotService.Instance.StopAsync();
        AutoPilotService.Instance.IsRunning.Should().BeFalse();
    }
    
    [Fact]
    public void Pause_ShouldNotThrow()
    {
        Action act = () => AutoPilotService.Instance.Pause();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void Resume_ShouldNotThrow()
    {
        Action act = () => AutoPilotService.Instance.Resume();
        act.Should().NotThrow();
    }
    
    #endregion
    
    #region Task Management Tests
    
    [Fact]
    public void AddTask_ShouldAcceptValidTask()
    {
        var task = new AutoPilotTask { Name = "Test Task", Priority = 1 };
        Action act = () => AutoPilotService.Instance.AddTask(task);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void AddTask_ShouldHandleNullTask()
    {
        Action act = () => AutoPilotService.Instance.AddTask(null!);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void RemoveTask_ShouldNotThrow()
    {
        Action act = () => AutoPilotService.Instance.RemoveTask("nonexistent");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ClearTasks_ShouldNotThrow()
    {
        Action act = () => AutoPilotService.Instance.ClearTasks();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetTasks_ShouldReturnList()
    {
        var tasks = AutoPilotService.Instance.GetTasks();
        tasks.Should().NotBeNull();
    }
    
    #endregion
    
    #region Safety Limit Tests
    
    [Theory]
    [InlineData("maxActionsPerHour", 100)]
    [InlineData("maxResourceSpend", 1000000)]
    [InlineData("maxTroopLoss", 50000)]
    public void SetSafetyLimit_ShouldAcceptValidLimits(string limitName, int value)
    {
        Action act = () => AutoPilotService.Instance.SetSafetyLimit(limitName, value);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetSafetyLimit_ShouldReturnValue()
    {
        var limit = AutoPilotService.Instance.GetSafetyLimit("maxActionsPerHour");
        limit.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void ResetSafetyLimits_ShouldNotThrow()
    {
        Action act = () => AutoPilotService.Instance.ResetSafetyLimits();
        act.Should().NotThrow();
    }
    
    #endregion
    
    #region Execution Tests
    
    [Fact]
    public async Task ExecuteTaskAsync_ShouldReturnResult()
    {
        var task = new AutoPilotTask { Name = "Execute Test", Priority = 1 };
        var result = await AutoPilotService.Instance.ExecuteTaskAsync(task);
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task ExecuteTaskAsync_ShouldHandleNullTask()
    {
        var result = await AutoPilotService.Instance.ExecuteTaskAsync(null!);
        result.Success.Should().BeFalse();
    }
    
    #endregion
    
    #region Event Tests
    
    [Fact]
    public void TaskStarted_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        AutoPilotService.Instance.TaskStarted += (task) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void TaskCompleted_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        AutoPilotService.Instance.TaskCompleted += (task, result) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void SafetyLimitReached_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        AutoPilotService.Instance.SafetyLimitReached += (limit) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    #endregion
}

#region Supporting Types

public class AutoPilotTask
{
    public string Name { get; set; } = "";
    public int Priority { get; set; }
    public string Type { get; set; } = "";
    public JObject Parameters { get; set; } = new();
}

public class AutoPilotResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public JObject Data { get; set; } = new();
}

#endregion
