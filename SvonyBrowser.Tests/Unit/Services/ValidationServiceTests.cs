using SvonyBrowser.Services;
using SvonyBrowser.Tests.Fixtures;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for ValidationService - centralized input validation.
/// </summary>
[Collection("ServiceTests")]
public class ValidationServiceTests : ServiceTestFixture
{
    #region Singleton Tests

    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = ValidationService.Instance;
        var instance2 = ValidationService.Instance;
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        ValidationService.Instance.Should().NotBeNull();
    }

    #endregion

    #region URL Validation Tests

    [Theory]
    [InlineData("http://example.com", true)]
    [InlineData("https://example.com", true)]
    [InlineData("https://example.com/path", true)]
    [InlineData("https://example.com:8080", true)]
    [InlineData("ftp://example.com", false)]
    [InlineData("not-a-url", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ValidateUrl_ShouldValidateCorrectly(string url, bool expectedValid)
    {
        var result = ValidationService.Instance.ValidateUrl(url);
        result.IsValid.Should().Be(expectedValid);
    }

    #endregion

    #region Port Validation Tests

    [Theory]
    [InlineData("80", true)]
    [InlineData("443", true)]
    [InlineData("8080", true)]
    [InlineData("65535", true)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("65536", false)]
    [InlineData("-1", false)]
    [InlineData("abc", false)]
    [InlineData("", false)]
    public void ValidatePort_ShouldValidateCorrectly(string port, bool expectedValid)
    {
        var result = ValidationService.Instance.ValidatePort(port);
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void ValidatePort_PrivilegedPort_ShouldWarn()
    {
        var result = ValidationService.Instance.ValidatePort("80");
        result.IsValid.Should().BeTrue();
        result.Severity.Should().Be(ValidationSeverity.Warning);
    }

    #endregion

    #region IP Address Validation Tests

    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("127.0.0.1", true)]
    [InlineData("localhost", true)]
    [InlineData("::1", true)]
    [InlineData("256.1.1.1", false)]
    [InlineData("not-an-ip", false)]
    [InlineData("", false)]
    public void ValidateIpAddress_ShouldValidateCorrectly(string ip, bool expectedValid)
    {
        var result = ValidationService.Instance.ValidateIpAddress(ip);
        result.IsValid.Should().Be(expectedValid);
    }

    #endregion

    #region Hostname Validation Tests

    [Theory]
    [InlineData("example.com", true)]
    [InlineData("sub.example.com", true)]
    [InlineData("localhost", true)]
    [InlineData("my-host.example.com", true)]
    [InlineData("", false)]
    public void ValidateHostname_ShouldValidateCorrectly(string hostname, bool expectedValid)
    {
        var result = ValidationService.Instance.ValidateHostname(hostname);
        result.IsValid.Should().Be(expectedValid);
    }

    #endregion

    #region Integer Range Validation Tests

    [Theory]
    [InlineData("50", 0, 100, true)]
    [InlineData("0", 0, 100, true)]
    [InlineData("100", 0, 100, true)]
    [InlineData("-1", 0, 100, false)]
    [InlineData("101", 0, 100, false)]
    [InlineData("abc", 0, 100, false)]
    [InlineData("", 0, 100, false)]
    public void ValidateIntRange_ShouldValidateCorrectly(string value, int min, int max, bool expectedValid)
    {
        var result = ValidationService.Instance.ValidateIntRange(value, min, max);
        result.IsValid.Should().Be(expectedValid);
    }

    #endregion

    #region Decimal Range Validation Tests

    [Theory]
    [InlineData("0.5", 0, 1, true)]
    [InlineData("0", 0, 1, true)]
    [InlineData("1", 0, 1, true)]
    [InlineData("0.7", 0, 1, true)]
    [InlineData("-0.1", 0, 1, false)]
    [InlineData("1.1", 0, 1, false)]
    [InlineData("abc", 0, 1, false)]
    public void ValidateDecimalRange_ShouldValidateCorrectly(string value, double min, double max, bool expectedValid)
    {
        var result = ValidationService.Instance.ValidateDecimalRange(value, min, max);
        result.IsValid.Should().Be(expectedValid);
    }

    #endregion

    #region File Path Validation Tests

    [Fact]
    public void ValidateFilePath_EmptyPath_ShouldFail()
    {
        var result = ValidationService.Instance.ValidateFilePath("");
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateFilePath_InvalidChars_ShouldFail()
    {
        var result = ValidationService.Instance.ValidateFilePath("file<>name.txt");
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateFilePath_WrongExtension_ShouldFail()
    {
        var result = ValidationService.Instance.ValidateFilePath("file.txt", "File", new[] { ".dll", ".exe" });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateFilePath_NonExistentFile_ShouldWarn()
    {
        var result = ValidationService.Instance.ValidateFilePath(@"C:\nonexistent\file.dll", "File", new[] { ".dll" });
        result.Severity.Should().Be(ValidationSeverity.Warning);
    }

    #endregion

    #region Directory Path Validation Tests

    [Fact]
    public void ValidateDirectoryPath_EmptyPath_ShouldFail()
    {
        var result = ValidationService.Instance.ValidateDirectoryPath("");
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateDirectoryPath_NonExistentDir_ShouldWarn()
    {
        var result = ValidationService.Instance.ValidateDirectoryPath(@"C:\nonexistent\directory");
        result.Severity.Should().Be(ValidationSeverity.Warning);
    }

    #endregion

    #region Webhook URL Validation Tests

    [Theory]
    [InlineData("", true)] // Optional field
    [InlineData("https://discord.com/api/webhooks/123/abc", true)]
    [InlineData("https://hooks.slack.com/services/T00/B00/xxx", true)]
    [InlineData("https://api.telegram.org/bot123/sendMessage", true)]
    [InlineData("not-a-url", false)]
    public void ValidateWebhookUrl_ShouldValidateCorrectly(string url, bool expectedValid)
    {
        var result = ValidationService.Instance.ValidateWebhookUrl(url);
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void ValidateWebhookUrl_UnknownPattern_ShouldWarn()
    {
        var result = ValidationService.Instance.ValidateWebhookUrl("https://custom-webhook.example.com/hook");
        result.Severity.Should().Be(ValidationSeverity.Warning);
    }

    #endregion

    #region API Key Validation Tests

    [Theory]
    [InlineData("", true)] // Optional field
    [InlineData("sk-1234567890abcdef", true)]
    [InlineData("short", false)]
    public void ValidateApiKey_ShouldValidateCorrectly(string key, bool expectedValid)
    {
        var result = ValidationService.Instance.ValidateApiKey(key);
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void ValidateApiKey_Placeholder_ShouldWarn()
    {
        var result = ValidationService.Instance.ValidateApiKey("your-api-key-here");
        result.Severity.Should().Be(ValidationSeverity.Warning);
    }

    #endregion

    #region Server Address Validation Tests

    [Theory]
    [InlineData("localhost:8080", true)]
    [InlineData("192.168.1.1:443", true)]
    [InlineData("example.com:80", true)]
    [InlineData("example.com", false)] // Missing port
    [InlineData(":8080", false)] // Missing host
    [InlineData("", false)]
    public void ValidateServerAddress_ShouldValidateCorrectly(string address, bool expectedValid)
    {
        var result = ValidationService.Instance.ValidateServerAddress(address);
        result.IsValid.Should().Be(expectedValid);
    }

    #endregion

    #region Required Validation Tests

    [Theory]
    [InlineData("value", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void ValidateRequired_ShouldValidateCorrectly(string value, bool expectedValid)
    {
        var result = ValidationService.Instance.ValidateRequired(value);
        result.IsValid.Should().Be(expectedValid);
    }

    #endregion

    #region Length Validation Tests

    [Theory]
    [InlineData("hello", 1, 10, true)]
    [InlineData("", 0, 10, true)]
    [InlineData("", 1, 10, false)]
    [InlineData("hello world", 1, 5, false)]
    public void ValidateLength_ShouldValidateCorrectly(string value, int min, int max, bool expectedValid)
    {
        var result = ValidationService.Instance.ValidateLength(value, min, max);
        result.IsValid.Should().Be(expectedValid);
    }

    #endregion

    #region Pattern Validation Tests

    [Theory]
    [InlineData("abc123", @"^[a-z0-9]+$", true)]
    [InlineData("ABC", @"^[a-z]+$", false)]
    [InlineData("", @"^[a-z]+$", true)] // Optional by default
    public void ValidatePattern_ShouldValidateCorrectly(string value, string pattern, bool expectedValid)
    {
        var result = ValidationService.Instance.ValidatePattern(value, pattern);
        result.IsValid.Should().Be(expectedValid);
    }

    #endregion

    #region Batch Validation Tests

    [Fact]
    public void ValidateAll_ShouldReturnAllResults()
    {
        var results = ValidationService.Instance.ValidateAll(
            (() => ValidationService.Instance.ValidateUrl("https://example.com"), "URL"),
            (() => ValidationService.Instance.ValidatePort("8080"), "Port"),
            (() => ValidationService.Instance.ValidateRequired("value"), "Field")
        );

        results.Should().HaveCount(3);
        results.Should().OnlyContain(r => r.IsValid);
    }

    [Fact]
    public void IsAllValid_AllValid_ShouldReturnTrue()
    {
        var results = new[]
        {
            ValidationResult.Success("Field1"),
            ValidationResult.Success("Field2")
        };

        ValidationService.Instance.IsAllValid(results).Should().BeTrue();
    }

    [Fact]
    public void IsAllValid_HasError_ShouldReturnFalse()
    {
        var results = new[]
        {
            ValidationResult.Success("Field1"),
            ValidationResult.Error("Field2", "Error")
        };

        ValidationService.Instance.IsAllValid(results).Should().BeFalse();
    }

    [Fact]
    public void GetErrorMessages_ShouldReturnOnlyErrors()
    {
        var results = new[]
        {
            ValidationResult.Success("Field1"),
            ValidationResult.Error("Field2", "Error message"),
            ValidationResult.Warning("Field3", "Warning message")
        };

        var errors = ValidationService.Instance.GetErrorMessages(results);
        errors.Should().HaveCount(1);
        errors.Should().Contain("Error message");
    }

    [Fact]
    public void GetWarningMessages_ShouldReturnOnlyWarnings()
    {
        var results = new[]
        {
            ValidationResult.Success("Field1"),
            ValidationResult.Error("Field2", "Error message"),
            ValidationResult.Warning("Field3", "Warning message")
        };

        var warnings = ValidationService.Instance.GetWarningMessages(results);
        warnings.Should().HaveCount(1);
        warnings.Should().Contain("Warning message");
    }

    #endregion

    #region Event Tests

    [Fact]
    public void ValidationFailed_ShouldRaiseEvent()
    {
        var eventRaised = false;
        ValidationService.Instance.ValidationFailed += (s, e) => eventRaised = true;

        ValidationService.Instance.ValidateAll(
            (() => ValidationService.Instance.ValidateUrl("not-a-url"), "URL")
        );

        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void ValidationPassed_ShouldRaiseEvent()
    {
        var eventRaised = false;
        ValidationService.Instance.ValidationPassed += (s, e) => eventRaised = true;

        ValidationService.Instance.ValidateAll(
            (() => ValidationService.Instance.ValidateUrl("https://example.com"), "URL")
        );

        eventRaised.Should().BeTrue();
    }

    #endregion
}
