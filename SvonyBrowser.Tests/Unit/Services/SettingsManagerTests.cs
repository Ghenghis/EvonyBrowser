using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for SettingsManager - managing application settings.
/// </summary>
[Collection("ServiceTests")]
public class SettingsManagerTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = SettingsManager.Instance;
        var instance2 = SettingsManager.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        SettingsManager.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void Settings_ShouldNotBeNull()
    {
        SettingsManager.Instance.Settings.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData("theme", "dark")]
    [InlineData("language", "en")]
    [InlineData("autoSave", "true")]
    public void GetSetting_ShouldReturnValue(string key, string defaultValue)
    {
        var value = SettingsManager.Instance.GetSetting(key, defaultValue);
        value.Should().NotBeNull();
    }
    
    [Fact]
    public void SetSetting_ShouldNotThrow()
    {
        Action act = () => SettingsManager.Instance.SetSetting("test_key", "test_value");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void SetSetting_ShouldPersistValue()
    {
        var testKey = $"test_{Guid.NewGuid()}";
        SettingsManager.Instance.SetSetting(testKey, "test_value");
        var value = SettingsManager.Instance.GetSetting(testKey, "");
        value.Should().Be("test_value");
    }
    
    [Fact]
    public void RemoveSetting_ShouldNotThrow()
    {
        Action act = () => SettingsManager.Instance.RemoveSetting("nonexistent");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void Save_ShouldNotThrow()
    {
        Action act = () => SettingsManager.Instance.Save();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void Load_ShouldNotThrow()
    {
        Action act = () => SettingsManager.Instance.Load();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void Reset_ShouldNotThrow()
    {
        Action act = () => SettingsManager.Instance.Reset();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetAllSettings_ShouldReturnDictionary()
    {
        var settings = SettingsManager.Instance.GetAllSettings();
        settings.Should().NotBeNull();
    }
    
    [Fact]
    public void SettingChanged_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        SettingsManager.Instance.SettingChanged += (key, value) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
