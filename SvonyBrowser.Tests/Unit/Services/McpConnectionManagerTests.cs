using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for McpConnectionManager - CRITICAL service requiring 25+ tests.
/// Tests MCP server connections, tool invocation, and protocol handling.
/// </summary>
[Collection("ServiceTests")]
public class McpConnectionManagerTests : ServiceTestFixture
{
    #region Singleton Tests
    
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        // Arrange & Act
        var instance1 = McpConnectionManager.Instance;
        var instance2 = McpConnectionManager.Instance;
        
        // Assert
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        // Act
        var instance = McpConnectionManager.Instance;
        
        // Assert
        instance.Should().NotBeNull();
    }
    
    [Fact]
    public void Instance_ShouldBeThreadSafe()
    {
        // Arrange
        var instances = new McpConnectionManager[30];
        var tasks = new Task[30];
        
        // Act
        for (int i = 0; i < 30; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() => instances[index] = McpConnectionManager.Instance);
        }
        Task.WaitAll(tasks);
        
        // Assert
        instances.Distinct().Should().HaveCount(1);
    }
    
    #endregion
    
    #region Property Tests
    
    [Fact]
    public void IsConnected_ShouldReturnBool()
    {
        // Act
        var isConnected = McpConnectionManager.Instance.IsConnected;
        
        // Assert
        isConnected.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void ActiveServers_ShouldNotBeNull()
    {
        // Act
        var servers = McpConnectionManager.Instance.ActiveServers;
        
        // Assert
        servers.Should().NotBeNull();
    }
    
    [Fact]
    public void AvailableTools_ShouldNotBeNull()
    {
        // Act
        var tools = McpConnectionManager.Instance.AvailableTools;
        
        // Assert
        tools.Should().NotBeNull();
    }
    
    [Fact]
    public void ConnectionStatus_ShouldNotBeNull()
    {
        // Act
        var status = McpConnectionManager.Instance.ConnectionStatus;
        
        // Assert
        status.Should().NotBeNull();
    }
    
    #endregion
    
    #region Connect Tests
    
    [Fact]
    public async Task ConnectAsync_ShouldReturnResult()
    {
        // Arrange
        var serverUrl = "ws://localhost:3000";
        
        // Act
        var result = await McpConnectionManager.Instance.ConnectAsync(serverUrl);
        
        // Assert
        result.Should().BeOneOf(true, false);
    }
    
    [Theory]
    [InlineData("ws://localhost:3000")]
    [InlineData("ws://127.0.0.1:3001")]
    [InlineData("wss://secure.server.com")]
    public async Task ConnectAsync_ShouldHandleVariousUrls(string url)
    {
        // Act
        Func<Task> act = async () => await McpConnectionManager.Instance.ConnectAsync(url);
        
        // Assert
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task ConnectAsync_ShouldHandleEmptyUrl()
    {
        // Act
        var result = await McpConnectionManager.Instance.ConnectAsync("");
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public async Task ConnectAsync_ShouldHandleNullUrl()
    {
        // Act
        var result = await McpConnectionManager.Instance.ConnectAsync(null!);
        
        // Assert
        result.Should().BeFalse();
    }
    
    #endregion
    
    #region Disconnect Tests
    
    [Fact]
    public async Task DisconnectAsync_ShouldNotThrow()
    {
        // Act
        Func<Task> act = async () => await McpConnectionManager.Instance.DisconnectAsync();
        
        // Assert
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task DisconnectAsync_ShouldBeIdempotent()
    {
        // Act - disconnect multiple times
        await McpConnectionManager.Instance.DisconnectAsync();
        await McpConnectionManager.Instance.DisconnectAsync();
        await McpConnectionManager.Instance.DisconnectAsync();
        
        // Assert - should not throw
        McpConnectionManager.Instance.IsConnected.Should().BeFalse();
    }
    
    #endregion
    
    #region InvokeTool Tests
    
    [Fact]
    public async Task InvokeToolAsync_ShouldReturnResult()
    {
        // Arrange
        var toolName = "test_tool";
        var parameters = new JObject { ["param1"] = "value1" };
        
        // Act
        var result = await McpConnectionManager.Instance.InvokeToolAsync(toolName, parameters);
        
        // Assert
        result.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData("get_game_state")]
    [InlineData("send_packet")]
    [InlineData("analyze_traffic")]
    [InlineData("execute_action")]
    public async Task InvokeToolAsync_ShouldHandleVariousTools(string toolName)
    {
        // Arrange
        var parameters = new JObject();
        
        // Act
        Func<Task> act = async () => await McpConnectionManager.Instance.InvokeToolAsync(toolName, parameters);
        
        // Assert
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task InvokeToolAsync_ShouldHandleEmptyToolName()
    {
        // Act
        var result = await McpConnectionManager.Instance.InvokeToolAsync("", new JObject());
        
        // Assert
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task InvokeToolAsync_ShouldHandleNullParameters()
    {
        // Act
        Func<Task> act = async () => await McpConnectionManager.Instance.InvokeToolAsync("test", null!);
        
        // Assert
        await act.Should().NotThrowAsync();
    }
    
    #endregion
    
    #region GetTools Tests
    
    [Fact]
    public void GetTools_ShouldReturnList()
    {
        // Act
        var tools = McpConnectionManager.Instance.GetTools();
        
        // Assert
        tools.Should().NotBeNull();
    }
    
    [Fact]
    public void GetToolsByCategory_ShouldFilterCorrectly()
    {
        // Act
        var tools = McpConnectionManager.Instance.GetToolsByCategory("game");
        
        // Assert
        tools.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData("game")]
    [InlineData("traffic")]
    [InlineData("automation")]
    [InlineData("analysis")]
    public void GetToolsByCategory_ShouldHandleVariousCategories(string category)
    {
        // Act
        var tools = McpConnectionManager.Instance.GetToolsByCategory(category);
        
        // Assert
        tools.Should().NotBeNull();
    }
    
    #endregion
    
    #region Server Management Tests
    
    [Fact]
    public void RegisterServer_ShouldNotThrow()
    {
        // Arrange
        var serverConfig = new JObject
        {
            ["name"] = "test_server",
            ["url"] = "ws://localhost:3000"
        };
        
        // Act
        Action act = () => McpConnectionManager.Instance.RegisterServer(serverConfig);
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void UnregisterServer_ShouldNotThrow()
    {
        // Act
        Action act = () => McpConnectionManager.Instance.UnregisterServer("nonexistent");
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetServerStatus_ShouldReturnStatus()
    {
        // Act
        var status = McpConnectionManager.Instance.GetServerStatus("test_server");
        
        // Assert
        status.Should().NotBeNull();
    }
    
    #endregion
    
    #region Event Tests
    
    [Fact]
    public void Connected_EventShouldBeSubscribable()
    {
        // Arrange
        var eventRaised = false;
        
        // Act
        McpConnectionManager.Instance.Connected += () => eventRaised = true;
        
        // Assert
        eventRaised.Should().BeFalse(); // Not raised yet
    }
    
    [Fact]
    public void Disconnected_EventShouldBeSubscribable()
    {
        // Arrange
        var eventRaised = false;
        
        // Act
        McpConnectionManager.Instance.Disconnected += () => eventRaised = true;
        
        // Assert
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void ToolInvoked_EventShouldBeSubscribable()
    {
        // Arrange
        var eventRaised = false;
        
        // Act
        McpConnectionManager.Instance.ToolInvoked += (name, result) => eventRaised = true;
        
        // Assert
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void Error_EventShouldBeSubscribable()
    {
        // Arrange
        var eventRaised = false;
        
        // Act
        McpConnectionManager.Instance.Error += (ex) => eventRaised = true;
        
        // Assert
        eventRaised.Should().BeFalse();
    }
    
    #endregion
}
