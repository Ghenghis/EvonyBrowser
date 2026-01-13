using System.Net;
using System.Net.Sockets;
using SvonyBrowser.Services;
using SvonyBrowser.Tests.Fixtures;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for UserFriendlyMessages - error message translation.
/// </summary>
[Collection("ServiceTests")]
public class UserFriendlyMessagesTests : ServiceTestFixture
{
    #region Exception Type Mapping Tests
    
    [Fact]
    public void GetFriendlyMessage_NullException_ShouldReturnDefault()
    {
        var message = UserFriendlyMessages.GetFriendlyMessage(null!);
        message.Should().Be("An unknown error occurred.");
    }
    
    [Fact]
    public void GetFriendlyMessage_FileNotFoundException_ShouldReturnFriendly()
    {
        var ex = new FileNotFoundException("File not found", "test.dll");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("not found");
        message.Should().Contain("test.dll");
    }
    
    [Fact]
    public void GetFriendlyMessage_DirectoryNotFoundException_ShouldReturnFriendly()
    {
        var ex = new DirectoryNotFoundException("Directory not found");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("folder");
        message.Should().Contain("missing");
    }
    
    [Fact]
    public void GetFriendlyMessage_UnauthorizedAccessException_ShouldReturnFriendly()
    {
        var ex = new UnauthorizedAccessException("Access denied");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("Access denied");
        message.Should().Contain("administrator");
    }
    
    [Fact]
    public void GetFriendlyMessage_TimeoutException_ShouldReturnFriendly()
    {
        var ex = new TimeoutException("Operation timed out");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("timed out");
    }
    
    [Fact]
    public void GetFriendlyMessage_OutOfMemoryException_ShouldReturnFriendly()
    {
        var ex = new OutOfMemoryException("Out of memory");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("memory");
        message.Should().Contain("close");
    }
    
    [Fact]
    public void GetFriendlyMessage_InvalidOperationException_ShouldReturnFriendly()
    {
        var ex = new InvalidOperationException("Invalid operation");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("could not be completed");
    }
    
    [Fact]
    public void GetFriendlyMessage_ArgumentException_ShouldReturnFriendly()
    {
        var ex = new ArgumentException("Invalid argument");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("Invalid input");
    }
    
    [Fact]
    public void GetFriendlyMessage_FormatException_ShouldReturnFriendly()
    {
        var ex = new FormatException("Invalid format");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("Invalid format");
    }
    
    [Fact]
    public void GetFriendlyMessage_TaskCanceledException_ShouldReturnFriendly()
    {
        var ex = new TaskCanceledException("Task was cancelled");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("cancelled");
    }
    
    #endregion
    
    #region Socket Error Tests
    
    [Fact]
    public void GetFriendlyMessage_SocketException_ConnectionRefused_ShouldReturnFriendly()
    {
        var ex = new SocketException((int)SocketError.ConnectionRefused);
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("refused");
    }
    
    [Fact]
    public void GetFriendlyMessage_SocketException_TimedOut_ShouldReturnFriendly()
    {
        var ex = new SocketException((int)SocketError.TimedOut);
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("timed out");
    }
    
    [Fact]
    public void GetFriendlyMessage_SocketException_HostNotFound_ShouldReturnFriendly()
    {
        var ex = new SocketException((int)SocketError.HostNotFound);
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("not found");
    }
    
    #endregion
    
    #region Pattern Matching Tests
    
    [Fact]
    public void GetFriendlyMessage_FlashPluginError_ShouldReturnFriendly()
    {
        var ex = new Exception("Failed to load pepflashplayer.dll");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("Flash plugin");
    }
    
    [Fact]
    public void GetFriendlyMessage_CefSharpError_ShouldReturnFriendly()
    {
        var ex = new Exception("CefSharp browser subprocess failed");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("Browser component");
    }
    
    [Fact]
    public void GetFriendlyMessage_McpServerError_ShouldReturnFriendly()
    {
        var ex = new Exception("MCP server connection failed");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("MCP server");
    }
    
    [Fact]
    public void GetFriendlyMessage_LlmError_ShouldReturnFriendly()
    {
        var ex = new Exception("LLM inference failed");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("AI");
    }
    
    [Fact]
    public void GetFriendlyMessage_FiddlerError_ShouldReturnFriendly()
    {
        var ex = new Exception("Fiddler proxy port 8888 unavailable");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("Proxy");
    }
    
    [Fact]
    public void GetFriendlyMessage_SettingsError_ShouldReturnFriendly()
    {
        var ex = new Exception("Settings file corrupted, JSON parse failed");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("Settings");
    }
    
    [Fact]
    public void GetFriendlyMessage_EvonyError_ShouldReturnFriendly()
    {
        var ex = new Exception("Evony game server connection lost");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("Game server");
    }
    
    [Fact]
    public void GetFriendlyMessage_SslError_ShouldReturnFriendly()
    {
        var ex = new Exception("SSL certificate validation failed");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("Secure connection");
    }
    
    [Fact]
    public void GetFriendlyMessage_DnsError_ShouldReturnFriendly()
    {
        var ex = new Exception("DNS name resolution failed");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        message.Should().Contain("not found");
    }
    
    #endregion
    
    #region Suggested Actions Tests
    
    [Fact]
    public void GetSuggestedActions_SocketException_ShouldReturnNetworkActions()
    {
        var ex = new SocketException((int)SocketError.ConnectionRefused);
        var actions = UserFriendlyMessages.GetSuggestedActions(ex);
        
        actions.Should().NotBeEmpty();
        actions.Should().Contain(a => a.Contains("internet"));
    }
    
    [Fact]
    public void GetSuggestedActions_FileNotFoundException_ShouldReturnFileActions()
    {
        var ex = new FileNotFoundException("File not found", "test.dll");
        var actions = UserFriendlyMessages.GetSuggestedActions(ex);
        
        actions.Should().NotBeEmpty();
        actions.Should().Contain(a => a.Contains("installation") || a.Contains("Reinstall"));
    }
    
    [Fact]
    public void GetSuggestedActions_UnauthorizedAccessException_ShouldReturnPermissionActions()
    {
        var ex = new UnauthorizedAccessException("Access denied");
        var actions = UserFriendlyMessages.GetSuggestedActions(ex);
        
        actions.Should().NotBeEmpty();
        actions.Should().Contain(a => a.Contains("administrator") || a.Contains("permission"));
    }
    
    [Fact]
    public void GetSuggestedActions_TimeoutException_ShouldReturnRetryActions()
    {
        var ex = new TimeoutException("Operation timed out");
        var actions = UserFriendlyMessages.GetSuggestedActions(ex);
        
        actions.Should().NotBeEmpty();
        actions.Should().Contain(a => a.Contains("try again") || a.Contains("Wait"));
    }
    
    [Fact]
    public void GetSuggestedActions_OutOfMemoryException_ShouldReturnMemoryActions()
    {
        var ex = new OutOfMemoryException("Out of memory");
        var actions = UserFriendlyMessages.GetSuggestedActions(ex);
        
        actions.Should().NotBeEmpty();
        actions.Should().Contain(a => a.Contains("Close") || a.Contains("Restart"));
    }
    
    [Fact]
    public void GetSuggestedActions_GenericException_ShouldReturnDefaultActions()
    {
        var ex = new Exception("Some error");
        var actions = UserFriendlyMessages.GetSuggestedActions(ex);
        
        actions.Should().NotBeEmpty();
        actions.Should().Contain(a => a.Contains("try") || a.Contains("Restart"));
    }
    
    #endregion
    
    #region GetFriendlyMessageWithActions Tests
    
    [Fact]
    public void GetFriendlyMessageWithActions_ShouldReturnBoth()
    {
        var ex = new FileNotFoundException("File not found", "test.dll");
        var (message, actions) = UserFriendlyMessages.GetFriendlyMessageWithActions(ex);
        
        message.Should().NotBeNullOrEmpty();
        actions.Should().NotBeEmpty();
    }
    
    #endregion
    
    #region Inner Exception Tests
    
    [Fact]
    public void GetFriendlyMessage_WithInnerException_ShouldCheckInner()
    {
        var inner = new FileNotFoundException("Inner file not found", "inner.dll");
        var outer = new Exception("Outer exception", inner);
        
        var message = UserFriendlyMessages.GetFriendlyMessage(outer);
        
        // Should get the friendly message from inner exception
        message.Should().NotBe(outer.Message);
    }
    
    #endregion
    
    #region Generic Message Formatting Tests
    
    [Fact]
    public void GetFriendlyMessage_UnknownException_ShouldFormatNicely()
    {
        var ex = new Exception("some random error message");
        var message = UserFriendlyMessages.GetFriendlyMessage(ex);
        
        // Should capitalize first letter
        message[0].Should().Be(char.ToUpper(message[0]));
        
        // Should end with period
        message.Should().EndWith(".");
    }
    
    #endregion
}
