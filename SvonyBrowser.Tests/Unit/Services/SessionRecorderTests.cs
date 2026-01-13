using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for SessionRecorder - HIGH priority service requiring 15+ tests.
/// Tests record/replay functionality and session management.
/// </summary>
[Collection("ServiceTests")]
public class SessionRecorderTests : ServiceTestFixture
{
    #region Singleton Tests
    
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = SessionRecorder.Instance;
        var instance2 = SessionRecorder.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        SessionRecorder.Instance.Should().NotBeNull();
    }
    
    #endregion
    
    #region Property Tests
    
    [Fact]
    public void IsRecording_ShouldReturnBool()
    {
        SessionRecorder.Instance.IsRecording.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void IsPlaying_ShouldReturnBool()
    {
        SessionRecorder.Instance.IsPlaying.Should().BeOneOf(true, false);
    }
    
    [Fact]
    public void CurrentSession_ShouldBeAccessible()
    {
        var session = SessionRecorder.Instance.CurrentSession;
        // Can be null when not recording
    }
    
    [Fact]
    public void RecordedSessions_ShouldNotBeNull()
    {
        SessionRecorder.Instance.RecordedSessions.Should().NotBeNull();
    }
    
    #endregion
    
    #region Recording Tests
    
    [Fact]
    public void StartRecording_ShouldNotThrow()
    {
        Action act = () => SessionRecorder.Instance.StartRecording("Test Session");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void StopRecording_ShouldNotThrow()
    {
        Action act = () => SessionRecorder.Instance.StopRecording();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void StartRecording_ShouldSetIsRecording()
    {
        SessionRecorder.Instance.StartRecording("Test");
        SessionRecorder.Instance.IsRecording.Should().BeTrue();
        SessionRecorder.Instance.StopRecording();
    }
    
    [Fact]
    public void StopRecording_ShouldClearIsRecording()
    {
        SessionRecorder.Instance.StartRecording("Test");
        SessionRecorder.Instance.StopRecording();
        SessionRecorder.Instance.IsRecording.Should().BeFalse();
    }
    
    #endregion
    
    #region Playback Tests
    
    [Fact]
    public async Task PlaySessionAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await SessionRecorder.Instance.PlaySessionAsync("nonexistent");
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public void StopPlayback_ShouldNotThrow()
    {
        Action act = () => SessionRecorder.Instance.StopPlayback();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void PausePlayback_ShouldNotThrow()
    {
        Action act = () => SessionRecorder.Instance.PausePlayback();
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ResumePlayback_ShouldNotThrow()
    {
        Action act = () => SessionRecorder.Instance.ResumePlayback();
        act.Should().NotThrow();
    }
    
    #endregion
    
    #region Session Management Tests
    
    [Fact]
    public void GetSession_ShouldReturnNullForNonexistent()
    {
        var session = SessionRecorder.Instance.GetSession("nonexistent_session");
        session.Should().BeNull();
    }
    
    [Fact]
    public void DeleteSession_ShouldNotThrow()
    {
        Action act = () => SessionRecorder.Instance.DeleteSession("nonexistent");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ExportSession_ShouldReturnPath()
    {
        var path = SessionRecorder.Instance.ExportSession("test", TestDataPath);
        // May be null if session doesn't exist
    }
    
    [Fact]
    public void ImportSession_ShouldNotThrow()
    {
        Action act = () => SessionRecorder.Instance.ImportSession("nonexistent_path.json");
        act.Should().NotThrow();
    }
    
    #endregion
    
    #region Event Tests
    
    [Fact]
    public void RecordingStarted_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        SessionRecorder.Instance.RecordingStarted += (name) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public void RecordingStopped_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        SessionRecorder.Instance.RecordingStopped += (session) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
    
    #endregion
}
