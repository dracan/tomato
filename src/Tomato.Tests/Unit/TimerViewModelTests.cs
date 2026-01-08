using FluentAssertions;
using NSubstitute;
using Tomato.Models;
using Tomato.Services;
using Tomato.ViewModels;

namespace Tomato.Tests.Unit;

public class TimerViewModelTests
{
    private readonly ISessionManager _sessionManager;
    private readonly TimerViewModel _sut;

    public TimerViewModelTests()
    {
        _sessionManager = Substitute.For<ISessionManager>();
        _sessionManager.Cycle.Returns(new PomodoroCycle());
        _sessionManager.TodayStatistics.Returns(DailyStatistics.Create(DateOnly.FromDateTime(DateTime.Now)));

        _sut = new TimerViewModel(_sessionManager);
    }

    #region Initial State

    [Fact]
    public void TimeRemaining_InitiallyShowsDefaultDuration()
    {
        // Assert
        _sut.TimeRemaining.Should().Be(TimeSpan.FromMinutes(25));
    }

    [Fact]
    public void SessionLabel_InitiallyShowsFocus()
    {
        // Assert
        _sut.SessionLabel.Should().Be("Focus");
    }

    [Fact]
    public void IsRunning_InitiallyFalse()
    {
        // Assert
        _sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void CanStart_InitiallyTrue()
    {
        // Assert
        _sut.StartFocusCommand.CanExecute(null).Should().BeTrue();
    }

    #endregion

    #region Start Command

    [Fact]
    public void StartFocusCommand_CallsSessionManagerStartFocus()
    {
        // Act
        _sut.StartFocusCommand.Execute(null);

        // Assert
        _sessionManager.Received(1).StartFocus();
    }

    [Fact]
    public void StartFocusCommand_SetsIsRunningTrue_WhenSessionStarts()
    {
        // Arrange
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);

        // Act
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Assert
        _sut.IsRunning.Should().BeTrue();
    }

    #endregion

    #region Timer Updates

    [Fact]
    public void TimeRemaining_UpdatesOnTimerTick()
    {
        // Arrange
        var remaining = TimeSpan.FromMinutes(20);

        // Act
        _sessionManager.TimerTick += Raise.EventWith(
            new TimerTickEventArgs(TimeSpan.FromMinutes(5), remaining));

        // Assert
        _sut.TimeRemaining.Should().Be(remaining);
    }

    [Fact]
    public void SessionLabel_UpdatesOnSessionStateChanged()
    {
        // Arrange
        var session = Session.CreateShortBreak();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);

        // Act
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Assert
        _sut.SessionLabel.Should().Be("Short Break");
    }

    [Fact]
    public void CurrentSessionType_UpdatesOnSessionStart()
    {
        // Arrange
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);

        // Act
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Assert
        _sut.CurrentSessionType.Should().Be(SessionType.Focus);
    }

    #endregion

    #region Session Completion

    [Fact]
    public void IsRunning_SetsFalse_WhenSessionCompletes()
    {
        // Arrange
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Completed;
        _sessionManager.CurrentSession.Returns(session);

        // Act
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Completed));

        // Assert
        _sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void IsSessionComplete_SetsTrueWhenSessionCompletes()
    {
        // Arrange
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Completed;
        _sessionManager.CurrentSession.Returns(session);

        // Act
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Completed));

        // Assert
        _sut.IsSessionComplete.Should().BeTrue();
    }

    #endregion

    #region Pause/Resume Commands

    [Fact]
    public void PauseCommand_CallsSessionManagerPause()
    {
        // Arrange
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);

        // Simulate running state
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Act
        _sut.PauseCommand.Execute(null);

        // Assert
        _sessionManager.Received(1).Pause();
    }

    [Fact]
    public void ResumeCommand_CallsSessionManagerResume()
    {
        // Arrange
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Paused;
        _sessionManager.CurrentSession.Returns(session);

        // Simulate paused state
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Paused));

        // Act
        _sut.ResumeCommand.Execute(null);

        // Assert
        _sessionManager.Received(1).Resume();
    }

    [Fact]
    public void IsPaused_SetsTrueWhenSessionPaused()
    {
        // Arrange
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Paused;
        _sessionManager.CurrentSession.Returns(session);

        // Act
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Paused));

        // Assert
        _sut.IsPaused.Should().BeTrue();
    }

    #endregion

    #region Cancel Command

    [Fact]
    public void CancelCommand_CallsSessionManagerCancel()
    {
        // Arrange
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);

        // Act
        _sut.CancelCommand.Execute(null);

        // Assert
        _sessionManager.Received(1).Cancel();
    }

    #endregion

    #region Statistics Display

    [Fact]
    public void CompletedSessions_ReflectsStatistics()
    {
        // Arrange
        var stats = DailyStatistics.Create(DateOnly.FromDateTime(DateTime.Now));
        stats.FocusSessionsCompleted = 5;
        _sessionManager.TodayStatistics.Returns(stats);

        // Create new ViewModel to pick up updated stats
        var sut = new TimerViewModel(_sessionManager);

        // Assert
        sut.CompletedSessionsToday.Should().Be(5);
    }

    [Fact]
    public void CycleProgress_ReflectsCycleState()
    {
        // Arrange
        var cycle = new PomodoroCycle();
        cycle.IncrementFocusCount();
        cycle.IncrementFocusCount();
        _sessionManager.Cycle.Returns(cycle);

        // Create new ViewModel
        var sut = new TimerViewModel(_sessionManager);

        // Assert
        sut.CycleProgress.Should().Be("2/4");
    }

    #endregion

    #region US2: Break Session Display

    [Fact]
    public void StartBreakCommand_CallsSessionManagerStartBreak()
    {
        // Act
        _sut.StartBreakCommand.Execute(null);

        // Assert
        _sessionManager.Received(1).StartBreak();
    }

    [Fact]
    public void SessionLabel_ShowsShortBreak_ForShortBreakSession()
    {
        // Arrange
        var session = Session.CreateShortBreak();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);

        // Act
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Assert
        _sut.SessionLabel.Should().Be("Short Break");
        _sut.CurrentSessionType.Should().Be(SessionType.ShortBreak);
    }

    [Fact]
    public void SessionLabel_ShowsLongBreak_ForLongBreakSession()
    {
        // Arrange
        var session = Session.CreateLongBreak();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);

        // Act
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Assert
        _sut.SessionLabel.Should().Be("Long Break");
        _sut.CurrentSessionType.Should().Be(SessionType.LongBreak);
    }

    [Fact]
    public void TimeRemaining_Shows5Minutes_ForShortBreak()
    {
        // Arrange
        var session = Session.CreateShortBreak();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);

        // Act
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Assert
        _sut.TimeRemaining.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void TimeRemaining_Shows15Minutes_ForLongBreak()
    {
        // Arrange
        var session = Session.CreateLongBreak();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);

        // Act
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Assert
        _sut.TimeRemaining.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void IsSessionComplete_AfterBreakCompletion_ShowsStartFocusAgain()
    {
        // Arrange
        var session = Session.CreateShortBreak();
        session.Status = SessionStatus.Completed;
        _sessionManager.CurrentSession.Returns(session);

        // Act
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Completed));

        // Assert
        _sut.IsSessionComplete.Should().BeTrue();
        _sut.IsRunning.Should().BeFalse();
    }

    #endregion
}
