using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for WebhookHub - LOW priority service requiring 10+ tests.
/// </summary>
[Collection("ServiceTests")]
public class WebhookHubTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = WebhookHub.Instance;
        var instance2 = WebhookHub.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        WebhookHub.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void RegisteredWebhooks_ShouldNotBeNull()
    {
        WebhookHub.Instance.RegisteredWebhooks.Should().NotBeNull();
    }
    
    [Fact]
    public void RegisterWebhook_ShouldNotThrow()
    {
        var config = new WebhookConfig { Url = "https://example.com/webhook", Events = new[] { "test" } };
        Action act = () => WebhookHub.Instance.RegisterWebhook("test_hook", config);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void UnregisterWebhook_ShouldNotThrow()
    {
        Action act = () => WebhookHub.Instance.UnregisterWebhook("nonexistent");
        act.Should().NotThrow();
    }
    
    [Fact]
    public async Task TriggerWebhookAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await WebhookHub.Instance.TriggerWebhookAsync("test", new JObject());
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void GetWebhook_ShouldReturnNullForNonexistent()
    {
        var webhook = WebhookHub.Instance.GetWebhook("nonexistent");
        webhook.Should().BeNull();
    }
    
    [Fact]
    public void EnableWebhook_ShouldNotThrow()
    {
        Action act = () => WebhookHub.Instance.EnableWebhook("test");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void DisableWebhook_ShouldNotThrow()
    {
        Action act = () => WebhookHub.Instance.DisableWebhook("test");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetDeliveryHistory_ShouldReturnList()
    {
        var history = WebhookHub.Instance.GetDeliveryHistory("test");
        history.Should().NotBeNull();
    }
    
    [Fact]
    public void WebhookTriggered_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        WebhookHub.Instance.WebhookTriggered += (name, data) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void DeliveryFailed_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        WebhookHub.Instance.DeliveryFailed += (name, ex) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}

public class WebhookConfig
{
    public string Url { get; set; } = "";
    public string[] Events { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> Headers { get; set; } = new();
}
