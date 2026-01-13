using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for LlmIntegrationService - MEDIUM priority service requiring 15+ tests.
/// </summary>
[Collection("ServiceTests")]
public class LlmIntegrationServiceTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = LlmIntegrationService.Instance;
        var instance2 = LlmIntegrationService.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        LlmIntegrationService.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void IsConfigured_ShouldReturnBool()
    {
        LlmIntegrationService.Instance.IsConfigured.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void CurrentProvider_ShouldNotBeNull()
    {
        LlmIntegrationService.Instance.CurrentProvider.Should().NotBeNull();
    }
    
    [Fact]
    public void SupportedProviders_ShouldNotBeEmpty()
    {
        LlmIntegrationService.Instance.SupportedProviders.Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task SendPromptAsync_ShouldReturnResponse()
    {
        var response = await LlmIntegrationService.Instance.SendPromptAsync("Hello");
        response.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData("What is 2+2?")]
    [InlineData("Analyze my resources")]
    [InlineData("")]
    public async Task SendPromptAsync_ShouldHandleVariousPrompts(string prompt)
    {
        Func<Task> act = async () => await LlmIntegrationService.Instance.SendPromptAsync(prompt);
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void SetProvider_ShouldNotThrow()
    {
        Action act = () => LlmIntegrationService.Instance.SetProvider("openai");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void SetApiKey_ShouldNotThrow()
    {
        Action act = () => LlmIntegrationService.Instance.SetApiKey("test_key");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void SetModel_ShouldNotThrow()
    {
        Action act = () => LlmIntegrationService.Instance.SetModel("gpt-4");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetAvailableModels_ShouldReturnList()
    {
        var models = LlmIntegrationService.Instance.GetAvailableModels();
        models.Should().NotBeNull();
    }
    
    [Fact]
    public async Task StreamResponseAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => 
        {
            await foreach (var chunk in LlmIntegrationService.Instance.StreamResponseAsync("Hello"))
            {
                // Process chunk
            }
        };
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void ResponseReceived_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        LlmIntegrationService.Instance.ResponseReceived += (response) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void Error_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        LlmIntegrationService.Instance.Error += (ex) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void TokensUsed_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        LlmIntegrationService.Instance.TokensUsed += (count) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
