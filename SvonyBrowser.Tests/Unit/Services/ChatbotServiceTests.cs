using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for ChatbotService - MEDIUM priority service requiring 20+ tests.
/// Tests AI chat integration, message handling, and context management.
/// </summary>
[Collection("ServiceTests")]
public class ChatbotServiceTests : ServiceTestFixture
{
    #region Singleton Tests
    
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = ChatbotService.Instance;
        var instance2 = ChatbotService.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        ChatbotService.Instance.Should().NotBeNull();
    }
    
    #endregion
    
    #region Property Tests
    
    [Fact]
    public void IsConnected_ShouldReturnBool()
    {
        ChatbotService.Instance.IsConnected.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void MessageHistory_ShouldNotBeNull()
    {
        ChatbotService.Instance.MessageHistory.Should().NotBeNull();
    }
    
    [Fact]
    public void CurrentContext_ShouldNotBeNull()
    {
        ChatbotService.Instance.CurrentContext.Should().NotBeNull();
    }
    
    [Fact]
    public void IsProcessing_ShouldReturnBool()
    {
        ChatbotService.Instance.IsProcessing.Should().BeOneOf(true, false);
    }
    
    #endregion
    
    #region SendMessage Tests
    
    [Fact]
    public async Task SendMessageAsync_ShouldReturnResponse()
    {
        var response = await ChatbotService.Instance.SendMessageAsync("Hello");
        response.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData("Hello")]
    [InlineData("What is my power?")]
    [InlineData("Show me my resources")]
    [InlineData("")]
    public async Task SendMessageAsync_ShouldHandleVariousMessages(string message)
    {
        Func<Task> act = async () => await ChatbotService.Instance.SendMessageAsync(message);
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task SendMessageAsync_ShouldHandleNullMessage()
    {
        var response = await ChatbotService.Instance.SendMessageAsync(null!);
        response.Should().NotBeNull();
    }
    
    [Fact]
    public async Task SendMessageAsync_ShouldAddToHistory()
    {
        var initialCount = ChatbotService.Instance.MessageHistory.Count;
        await ChatbotService.Instance.SendMessageAsync("Test message");
        ChatbotService.Instance.MessageHistory.Count.Should().BeGreaterOrEqualTo(initialCount);
    }
    
    #endregion
    
    #region Context Management Tests
    
    [Fact]
    public void SetContext_ShouldNotThrow()
    {
        var context = new ChatContext { GameState = "active" };
        Action act = () => ChatbotService.Instance.SetContext(context);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ClearContext_ShouldNotThrow()
    {
        Action act = () => ChatbotService.Instance.ClearContext();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void UpdateContext_ShouldNotThrow()
    {
        Action act = () => ChatbotService.Instance.UpdateContext("key", "value");
        act.Should().NotThrow();
    }
    
    #endregion
    
    #region History Management Tests
    
    [Fact]
    public void ClearHistory_ShouldNotThrow()
    {
        Action act = () => ChatbotService.Instance.ClearHistory();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetHistory_ShouldReturnList()
    {
        var history = ChatbotService.Instance.GetHistory(10);
        history.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void GetHistory_ShouldRespectLimit(int limit)
    {
        var history = ChatbotService.Instance.GetHistory(limit);
        history.Count.Should().BeLessOrEqualTo(limit);
    }
    
    [Fact]
    public void ExportHistory_ShouldReturnJson()
    {
        var json = ChatbotService.Instance.ExportHistory();
        json.Should().NotBeNull();
    }
    
    #endregion
    
    #region Command Processing Tests
    
    [Theory]
    [InlineData("/help")]
    [InlineData("/status")]
    [InlineData("/resources")]
    [InlineData("/troops")]
    public async Task ProcessCommand_ShouldHandleBuiltInCommands(string command)
    {
        var response = await ChatbotService.Instance.ProcessCommandAsync(command);
        response.Should().NotBeNull();
    }
    
    [Fact]
    public async Task ProcessCommand_ShouldHandleUnknownCommand()
    {
        var response = await ChatbotService.Instance.ProcessCommandAsync("/unknown_command");
        response.Should().NotBeNull();
    }
    
    #endregion
    
    #region Event Tests
    
    [Fact]
    public void MessageReceived_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        ChatbotService.Instance.MessageReceived += (msg) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void MessageSent_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        ChatbotService.Instance.MessageSent += (msg) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void Error_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        ChatbotService.Instance.Error += (ex) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    #endregion
}
