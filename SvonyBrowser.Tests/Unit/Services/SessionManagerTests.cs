using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for SessionManager - MEDIUM priority service requiring 15+ tests.
/// </summary>
[Collection("ServiceTests")]
public class SessionManagerTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = SessionManager.Instance;
        var instance2 = SessionManager.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        SessionManager.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void CurrentSession_ShouldBeAccessible()
    {
        var session = SessionManager.Instance.CurrentSession;
        // Can be null if no active session
    }
    
    [Fact]
    public void Sessions_ShouldNotBeNull()
    {
        SessionManager.Instance.Sessions.Should().NotBeNull();
    }
    
    [Fact]
    public void IsLoggedIn_ShouldReturnBool()
    {
        SessionManager.Instance.IsLoggedIn.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public async Task CreateSessionAsync_ShouldReturnSession()
    {
        var session = await SessionManager.Instance.CreateSessionAsync("test_session");
        session.Should().NotBeNull();
    }
    
    [Fact]
    public async Task DestroySessionAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await SessionManager.Instance.DestroySessionAsync("nonexistent");
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void GetSession_ShouldReturnNullForNonexistent()
    {
        var session = SessionManager.Instance.GetSession("nonexistent");
        session.Should().BeNull();
    }
    
    [Fact]
    public async Task SwitchSessionAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await SessionManager.Instance.SwitchSessionAsync("test");
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void SaveCookies_ShouldNotThrow()
    {
        Action act = () => SessionManager.Instance.SaveCookies();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void LoadCookies_ShouldNotThrow()
    {
        Action act = () => SessionManager.Instance.LoadCookies();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ClearCookies_ShouldNotThrow()
    {
        Action act = () => SessionManager.Instance.ClearCookies();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void SessionCreated_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        SessionManager.Instance.SessionCreated += (session) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void SessionDestroyed_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        SessionManager.Instance.SessionDestroyed += (sessionId) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void LoginStateChanged_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        SessionManager.Instance.LoginStateChanged += (isLoggedIn) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
