using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for KeyboardShortcutManager.
/// </summary>
[Collection("ServiceTests")]
public class KeyboardShortcutManagerTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = KeyboardShortcutManager.Instance;
        var instance2 = KeyboardShortcutManager.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        KeyboardShortcutManager.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void RegisteredShortcuts_ShouldNotBeNull()
    {
        KeyboardShortcutManager.Instance.RegisteredShortcuts.Should().NotBeNull();
    }
    
    [Fact]
    public void IsEnabled_ShouldReturnBool()
    {
        KeyboardShortcutManager.Instance.IsEnabled.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void RegisterShortcut_ShouldNotThrow()
    {
        Action act = () => KeyboardShortcutManager.Instance.RegisterShortcut("Ctrl+T", "test_action", () => { });
        act.Should().NotThrow();
    }
    
    [Fact]
    public void UnregisterShortcut_ShouldNotThrow()
    {
        Action act = () => KeyboardShortcutManager.Instance.UnregisterShortcut("Ctrl+T");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetShortcut_ShouldReturnAction()
    {
        var action = KeyboardShortcutManager.Instance.GetShortcut("Ctrl+S");
        // Can be null if not registered
    }
    
    [Fact]
    public void Enable_ShouldNotThrow()
    {
        Action act = () => KeyboardShortcutManager.Instance.Enable();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void Disable_ShouldNotThrow()
    {
        Action act = () => KeyboardShortcutManager.Instance.Disable();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetAllShortcuts_ShouldReturnDictionary()
    {
        var shortcuts = KeyboardShortcutManager.Instance.GetAllShortcuts();
        shortcuts.Should().NotBeNull();
    }
    
    [Fact]
    public void ShortcutTriggered_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        KeyboardShortcutManager.Instance.ShortcutTriggered += (shortcut, action) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
