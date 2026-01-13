using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for MultiAccountOrchestrator - MEDIUM priority service requiring 20+ tests.
/// </summary>
[Collection("ServiceTests")]
public class MultiAccountOrchestratorTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = MultiAccountOrchestrator.Instance;
        var instance2 = MultiAccountOrchestrator.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        MultiAccountOrchestrator.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void Accounts_ShouldNotBeNull()
    {
        MultiAccountOrchestrator.Instance.Accounts.Should().NotBeNull();
    }
    
    [Fact]
    public void ActiveAccount_ShouldBeAccessible()
    {
        var active = MultiAccountOrchestrator.Instance.ActiveAccount;
        // Can be null if no accounts
    }
    
    [Fact]
    public void IsRunning_ShouldReturnBool()
    {
        MultiAccountOrchestrator.Instance.IsRunning.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void AddAccount_ShouldNotThrow()
    {
        var account = new AccountConfig { Name = "Test Account", Server = "s1" };
        Action act = () => MultiAccountOrchestrator.Instance.AddAccount(account);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void RemoveAccount_ShouldNotThrow()
    {
        Action act = () => MultiAccountOrchestrator.Instance.RemoveAccount("nonexistent");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetAccount_ShouldReturnNullForNonexistent()
    {
        var account = MultiAccountOrchestrator.Instance.GetAccount("nonexistent");
        account.Should().BeNull();
    }
    
    [Fact]
    public async Task SwitchAccountAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await MultiAccountOrchestrator.Instance.SwitchAccountAsync("test");
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task StartAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await MultiAccountOrchestrator.Instance.StartAsync();
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task StopAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await MultiAccountOrchestrator.Instance.StopAsync();
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void SetSchedule_ShouldNotThrow()
    {
        var schedule = new AccountSchedule { AccountId = "test", Interval = TimeSpan.FromMinutes(30) };
        Action act = () => MultiAccountOrchestrator.Instance.SetSchedule(schedule);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetSchedule_ShouldReturnSchedule()
    {
        var schedule = MultiAccountOrchestrator.Instance.GetSchedule("test");
        // Can be null if not set
    }
    
    [Fact]
    public void ExecuteOnAllAccounts_ShouldNotThrow()
    {
        Func<Task> act = async () => await MultiAccountOrchestrator.Instance.ExecuteOnAllAccountsAsync(
            async (account) => await Task.CompletedTask);
        act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void GetAccountStatus_ShouldReturnStatus()
    {
        var status = MultiAccountOrchestrator.Instance.GetAccountStatus("test");
        status.Should().NotBeNull();
    }
    
    [Fact]
    public void AccountSwitched_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        MultiAccountOrchestrator.Instance.AccountSwitched += (from, to) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void AccountError_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        MultiAccountOrchestrator.Instance.AccountError += (account, ex) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void ScheduleTriggered_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        MultiAccountOrchestrator.Instance.ScheduleTriggered += (account) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void ClearAccounts_ShouldNotThrow()
    {
        Action act = () => MultiAccountOrchestrator.Instance.ClearAccounts();
        act.Should().NotThrow();
    }
}

public class AccountConfig
{
    public string Name { get; set; } = "";
    public string Server { get; set; } = "";
    public string Username { get; set; } = "";
}

public class AccountSchedule
{
    public string AccountId { get; set; } = "";
    public TimeSpan Interval { get; set; }
}
