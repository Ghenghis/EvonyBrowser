using SvonyBrowser.Services;
using SvonyBrowser.Tests.Fixtures;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for EnvironmentConfig - environment variable management.
/// </summary>
[Collection("ServiceTests")]
public class EnvironmentConfigTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = EnvironmentConfig.Instance;
        var instance2 = EnvironmentConfig.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        EnvironmentConfig.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void Get_ShouldReturnNullForMissingKey()
    {
        var value = EnvironmentConfig.Instance.Get("NONEXISTENT_KEY_12345");
        value.Should().BeNull();
    }
    
    [Fact]
    public void Get_WithDefault_ShouldReturnDefaultForMissingKey()
    {
        var value = EnvironmentConfig.Instance.Get("NONEXISTENT_KEY_12345", "default_value");
        value.Should().Be("default_value");
    }
    
    [Fact]
    public void GetInt_ShouldReturnDefaultForMissingKey()
    {
        var value = EnvironmentConfig.Instance.GetInt("NONEXISTENT_KEY_12345", 42);
        value.Should().Be(42);
    }
    
    [Fact]
    public void GetBool_ShouldReturnDefaultForMissingKey()
    {
        var value = EnvironmentConfig.Instance.GetBool("NONEXISTENT_KEY_12345", true);
        value.Should().BeTrue();
    }
    
    [Fact]
    public void Set_ShouldStoreValue()
    {
        EnvironmentConfig.Instance.Set("TEST_KEY_SET", "test_value");
        var value = EnvironmentConfig.Instance.Get("TEST_KEY_SET");
        value.Should().Be("test_value");
    }
    
    [Fact]
    public void Set_ShouldOverwriteExistingValue()
    {
        EnvironmentConfig.Instance.Set("TEST_KEY_OVERWRITE", "original");
        EnvironmentConfig.Instance.Set("TEST_KEY_OVERWRITE", "updated");
        var value = EnvironmentConfig.Instance.Get("TEST_KEY_OVERWRITE");
        value.Should().Be("updated");
    }
    
    [Fact]
    public void LoadFromFile_ShouldNotThrowForMissingFile()
    {
        var nonExistentPath = Path.Combine(TestConfigPath, "nonexistent.env");
        Action act = () => EnvironmentConfig.Instance.LoadFromFile(nonExistentPath);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void LoadFromFile_ShouldLoadValidEnvFile()
    {
        var envPath = Path.Combine(TestConfigPath, "test.env");
        File.WriteAllText(envPath, @"
# Comment line
TEST_ENV_VAR=hello_world
TEST_INT_VAR=123
TEST_BOOL_VAR=true
");
        
        EnvironmentConfig.Instance.LoadFromFile(envPath);
        
        EnvironmentConfig.Instance.Get("TEST_ENV_VAR").Should().Be("hello_world");
        EnvironmentConfig.Instance.GetInt("TEST_INT_VAR").Should().Be(123);
        EnvironmentConfig.Instance.GetBool("TEST_BOOL_VAR").Should().BeTrue();
    }
    
    [Fact]
    public void LoadFromFile_ShouldIgnoreComments()
    {
        var envPath = Path.Combine(TestConfigPath, "comments.env");
        File.WriteAllText(envPath, @"
# This is a comment
# COMMENTED_VAR=should_not_load
ACTUAL_VAR=should_load
");
        
        EnvironmentConfig.Instance.LoadFromFile(envPath);
        
        EnvironmentConfig.Instance.Get("COMMENTED_VAR").Should().BeNull();
        EnvironmentConfig.Instance.Get("ACTUAL_VAR").Should().Be("should_load");
    }
    
    [Fact]
    public void LoadFromFile_ShouldIgnoreEmptyLines()
    {
        var envPath = Path.Combine(TestConfigPath, "empty-lines.env");
        File.WriteAllText(envPath, @"
VAR1=value1

VAR2=value2

");
        
        EnvironmentConfig.Instance.LoadFromFile(envPath);
        
        EnvironmentConfig.Instance.Get("VAR1").Should().Be("value1");
        EnvironmentConfig.Instance.Get("VAR2").Should().Be("value2");
    }
    
    [Fact]
    public void LoadFromFile_ShouldHandleQuotedValues()
    {
        var envPath = Path.Combine(TestConfigPath, "quoted.env");
        File.WriteAllText(envPath, @"
QUOTED_VAR=""value with spaces""
SINGLE_QUOTED='single quoted'
");
        
        EnvironmentConfig.Instance.LoadFromFile(envPath);
        
        // Quotes should be stripped
        var value = EnvironmentConfig.Instance.Get("QUOTED_VAR");
        value.Should().NotBeNull();
    }
    
    [Fact]
    public void GetAll_ShouldReturnDictionary()
    {
        EnvironmentConfig.Instance.Set("TEST_GETALL_1", "value1");
        EnvironmentConfig.Instance.Set("TEST_GETALL_2", "value2");
        
        var all = EnvironmentConfig.Instance.GetAll();
        all.Should().NotBeNull();
        all.Should().ContainKey("TEST_GETALL_1");
        all.Should().ContainKey("TEST_GETALL_2");
    }
    
    [Fact]
    public void IsDebugMode_ShouldReturnBool()
    {
        var result = EnvironmentConfig.Instance.IsDebugMode;
        result.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void LogLevel_ShouldReturnString()
    {
        var result = EnvironmentConfig.Instance.LogLevel;
        result.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData("1", true)]
    [InlineData("yes", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("FALSE", false)]
    [InlineData("0", false)]
    [InlineData("no", false)]
    public void GetBool_ShouldParseVariousFormats(string value, bool expected)
    {
        EnvironmentConfig.Instance.Set("TEST_BOOL_PARSE", value);
        var result = EnvironmentConfig.Instance.GetBool("TEST_BOOL_PARSE");
        result.Should().Be(expected);
    }
}
