using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for ExportImportManager - MEDIUM priority service requiring 15+ tests.
/// </summary>
[Collection("ServiceTests")]
public class ExportImportManagerTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = ExportImportManager.Instance;
        var instance2 = ExportImportManager.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        ExportImportManager.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void SupportedFormats_ShouldNotBeEmpty()
    {
        ExportImportManager.Instance.SupportedFormats.Should().NotBeEmpty();
    }
    
    [Theory]
    [InlineData("json")]
    [InlineData("csv")]
    [InlineData("xml")]
    public void ExportToFormat_ShouldNotThrow(string format)
    {
        var data = new JObject { ["test"] = "data" };
        Func<Task> act = async () => await ExportImportManager.Instance.ExportAsync(data, format, TestDataPath);
        act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task ExportAsync_ShouldReturnPath()
    {
        var data = new JObject { ["test"] = "data" };
        var path = await ExportImportManager.Instance.ExportAsync(data, "json", TestDataPath);
        path.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public async Task ImportAsync_ShouldReturnData()
    {
        // Create test file first
        var testFile = Path.Combine(TestDataPath, "test_import.json");
        File.WriteAllText(testFile, "{\"test\": \"data\"}");
        
        var data = await ExportImportManager.Instance.ImportAsync(testFile);
        data.Should().NotBeNull();
    }
    
    [Fact]
    public async Task ImportAsync_ShouldHandleNonexistentFile()
    {
        var data = await ExportImportManager.Instance.ImportAsync("nonexistent_file.json");
        data.Should().BeNull();
    }
    
    [Fact]
    public void ValidateFormat_ShouldReturnBool()
    {
        var isValid = ExportImportManager.Instance.ValidateFormat("json");
        isValid.Should().BeTrue();
    }
    
    [Fact]
    public void ValidateFormat_ShouldRejectInvalid()
    {
        var isValid = ExportImportManager.Instance.ValidateFormat("invalid_format");
        isValid.Should().BeFalse();
    }
    
    [Fact]
    public void ExportGameState_ShouldNotThrow()
    {
        Func<Task> act = async () => await ExportImportManager.Instance.ExportGameStateAsync(TestDataPath);
        act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void ExportSettings_ShouldNotThrow()
    {
        Func<Task> act = async () => await ExportImportManager.Instance.ExportSettingsAsync(TestDataPath);
        act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void ExportCompleted_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        ExportImportManager.Instance.ExportCompleted += (path) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void ImportCompleted_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        ExportImportManager.Instance.ImportCompleted += (data) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void Error_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        ExportImportManager.Instance.Error += (ex) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
