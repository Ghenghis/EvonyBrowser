using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for ThemeManager.
/// </summary>
[Collection("ServiceTests")]
public class ThemeManagerTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = ThemeManager.Instance;
        var instance2 = ThemeManager.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        ThemeManager.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void CurrentTheme_ShouldNotBeNull()
    {
        ThemeManager.Instance.CurrentTheme.Should().NotBeNull();
    }
    
    [Fact]
    public void AvailableThemes_ShouldNotBeEmpty()
    {
        ThemeManager.Instance.AvailableThemes.Should().NotBeEmpty();
    }
    
    [Theory]
    [InlineData("dark")]
    [InlineData("light")]
    [InlineData("system")]
    public void SetTheme_ShouldNotThrow(string theme)
    {
        Action act = () => ThemeManager.Instance.SetTheme(theme);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetColor_ShouldReturnColor()
    {
        var color = ThemeManager.Instance.GetColor("primary");
        color.Should().NotBeNull();
    }
    
    [Fact]
    public void RegisterCustomTheme_ShouldNotThrow()
    {
        var theme = new JObject { ["name"] = "custom", ["primary"] = "#FF0000" };
        Action act = () => ThemeManager.Instance.RegisterCustomTheme(theme);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ThemeChanged_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        ThemeManager.Instance.ThemeChanged += (theme) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
