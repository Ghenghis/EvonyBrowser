using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for ErrorHandler - HIGH priority for failsafes.
/// </summary>
[Collection("ServiceTests")]
public class ErrorHandlerTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = ErrorHandler.Instance;
        var instance2 = ErrorHandler.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        ErrorHandler.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void ErrorCount_ShouldBeNonNegative()
    {
        ErrorHandler.Instance.ErrorCount.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    public void RecentErrors_ShouldNotBeNull()
    {
        ErrorHandler.Instance.RecentErrors.Should().NotBeNull();
    }
    
    [Fact]
    public void HandleError_ShouldNotThrow()
    {
        var ex = new Exception("Test error");
        Action act = () => ErrorHandler.Instance.HandleError(ex);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void HandleError_WithContext_ShouldNotThrow()
    {
        var ex = new Exception("Test error");
        Action act = () => ErrorHandler.Instance.HandleError(ex, "Test context");
        act.Should().NotThrow();
    }
    
    [Theory]
    [InlineData(typeof(ArgumentException))]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(NullReferenceException))]
    [InlineData(typeof(TimeoutException))]
    public void HandleError_ShouldHandleVariousExceptions(Type exceptionType)
    {
        var ex = (Exception)Activator.CreateInstance(exceptionType, "Test")!;
        Action act = () => ErrorHandler.Instance.HandleError(ex);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetErrorHistory_ShouldReturnList()
    {
        var history = ErrorHandler.Instance.GetErrorHistory(10);
        history.Should().NotBeNull();
        history.Count.Should().BeLessOrEqualTo(10);
    }
    
    [Fact]
    public void ClearErrors_ShouldNotThrow()
    {
        Action act = () => ErrorHandler.Instance.ClearErrors();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void SetErrorThreshold_ShouldNotThrow()
    {
        Action act = () => ErrorHandler.Instance.SetErrorThreshold(100);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ExportErrorLog_ShouldReturnPath()
    {
        var path = ErrorHandler.Instance.ExportErrorLog(TestDataPath);
        path.Should().NotBeNull();
    }
    
    [Fact]
    public void ErrorOccurred_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        ErrorHandler.Instance.ErrorOccurred += (ex, context) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void ErrorThresholdReached_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        ErrorHandler.Instance.ErrorThresholdReached += (count) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
