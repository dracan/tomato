using FluentAssertions;
using NSubstitute;
using Tomato.Models;
using Tomato.Services;
using Tomato.ViewModels;

namespace Tomato.Tests.Unit;

public class TimerViewModelTests
{
    private readonly ISessionManager _sessionManager;
    private readonly IDialogService _dialogService;
    private readonly IStatisticsReportService _statisticsReportService;
    private readonly TimerViewModel _sut;

    public TimerViewModelTests()
    {
        _sessionManager = Substitute.For<ISessionManager>();
        _sessionManager.Cycle.Returns(new PomodoroCycle());
        _sessionManager.TodayStatistics.Returns(DailyStatistics.Create(DateOnly.FromDateTime(DateTime.Now)));

        _dialogService = Substitute.For<IDialogService>();
        _dialogService.ShowGoalDialogAsync().Returns(new GoalDialogResult(true, null));

        _statisticsReportService = Substitute.For<IStatisticsReportService>();

        _sut = new TimerViewModel(_sessionManager, _dialogService, _statisticsReportService);
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
    public async Task StartFocusCommand_WhenConfirmed_StartsSessionWithGoal()
    {
        // Arrange
        var goal = "Complete feature";
        _dialogService.ShowGoalDialogAsync().Returns(new GoalDialogResult(true, goal));

        // Act
        await _sut.StartFocusCommand.ExecuteAsync(null);

        // Assert
        _sessionManager.Received(1).StartFocus(goal);
    }

    [Fact]
    public async Task StartFocusCommand_WhenCancelled_DoesNotStartSession()
    {
        // Arrange
        _dialogService.ShowGoalDialogAsync().Returns(new GoalDialogResult(false, null));

        // Act
        await _sut.StartFocusCommand.ExecuteAsync(null);

        // Assert
        _sessionManager.DidNotReceive().StartFocus(Arg.Any<string?>());
    }

    [Fact]
    public async Task StartFocusCommand_WhenConfirmed_SetsCurrentGoal()
    {
        // Arrange
        var goal = "Complete feature";
        _dialogService.ShowGoalDialogAsync().Returns(new GoalDialogResult(true, goal));

        // Act
        await _sut.StartFocusCommand.ExecuteAsync(null);

        // Assert
        _sut.CurrentGoal.Should().Be(goal);
    }

    [Fact]
    public async Task StartFocusCommand_WhenCancelled_DoesNotSetCurrentGoal()
    {
        // Arrange
        _dialogService.ShowGoalDialogAsync().Returns(new GoalDialogResult(false, null));
        _sut.CurrentGoal.Should().BeNull(); // Initial state

        // Act
        await _sut.StartFocusCommand.ExecuteAsync(null);

        // Assert
        _sut.CurrentGoal.Should().BeNull();
    }

    [Fact]
    public async Task StartFocusWithDurationCommand_WhenConfirmed_StartsSessionWithDurationAndGoal()
    {
        // Arrange
        var goal = "Quick task";
        _dialogService.ShowGoalDialogAsync().Returns(new GoalDialogResult(true, goal));

        // Act
        await _sut.StartFocusWithDurationCommand.ExecuteAsync("15");

        // Assert
        _sessionManager.Received(1).StartFocus(TimeSpan.FromMinutes(15), goal);
    }

    [Fact]
    public async Task StartFocusWithDurationCommand_WhenCancelled_DoesNotStartSession()
    {
        // Arrange
        _dialogService.ShowGoalDialogAsync().Returns(new GoalDialogResult(false, null));

        // Act
        await _sut.StartFocusWithDurationCommand.ExecuteAsync("15");

        // Assert
        _sessionManager.DidNotReceive().StartFocus(Arg.Any<TimeSpan>(), Arg.Any<string?>());
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
        var sut = new TimerViewModel(_sessionManager, _dialogService, _statisticsReportService);

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
        var sut = new TimerViewModel(_sessionManager, _dialogService, _statisticsReportService);

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

    #region Results Recording

    [Fact]
    public void WhenFocusSessionCompletes_ShowsResultsDialog()
    {
        // Arrange
        var session = Session.CreateFocus("Test goal");
        session.Status = SessionStatus.Completed;
        _sessionManager.CurrentSession.Returns(session);
        _dialogService.ShowResultsDialogAsync(Arg.Any<string?>()).Returns(new ResultsDialogResult(true, "Some results"));

        // Act
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Completed));

        // Assert - dialog should be shown
        _dialogService.Received(1).ShowResultsDialogAsync(Arg.Any<string?>());
    }

    [Fact]
    public void WhenBreakSessionCompletes_DoesNotShowResultsDialog()
    {
        // Arrange
        var session = Session.CreateShortBreak();
        session.Status = SessionStatus.Completed;
        _sessionManager.CurrentSession.Returns(session);

        // Act
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Completed));

        // Assert - dialog should not be shown for breaks
        _dialogService.DidNotReceive().ShowResultsDialogAsync(Arg.Any<string?>());
    }

    [Fact]
    public async Task WhenResultsDialogConfirmed_RecordsSessionResults()
    {
        // Arrange
        _sut.CurrentGoal.Should().BeNull();
        _dialogService.ShowResultsDialogAsync(Arg.Any<string?>()).Returns(new ResultsDialogResult(true, "Completed task"));

        // Simulate focus session completion
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Completed;
        _sessionManager.CurrentSession.Returns(session);
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Completed));

        // Allow async operations to complete
        await Task.Delay(50);

        // Assert
        _sessionManager.Received(1).RecordSessionResults("Completed task");
    }

    [Fact]
    public async Task WhenResultsDialogCancelled_DoesNotRecordSessionResults()
    {
        // Arrange
        _dialogService.ShowResultsDialogAsync(Arg.Any<string?>()).Returns(new ResultsDialogResult(false, null));

        // Simulate focus session completion
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Completed;
        _sessionManager.CurrentSession.Returns(session);
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Completed));

        // Allow async operations to complete
        await Task.Delay(50);

        // Assert
        _sessionManager.DidNotReceive().RecordSessionResults(Arg.Any<string?>());
    }

    [Fact]
    public async Task WhenResultsDialogSubmittedEmpty_DoesNotRecordSessionResults()
    {
        // Arrange
        _dialogService.ShowResultsDialogAsync(Arg.Any<string?>()).Returns(new ResultsDialogResult(true, ""));

        // Simulate focus session completion
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Completed;
        _sessionManager.CurrentSession.Returns(session);
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Completed));

        // Allow async operations to complete
        await Task.Delay(50);

        // Assert - empty results should not be recorded
        _sessionManager.DidNotReceive().RecordSessionResults(Arg.Any<string?>());
    }

    #endregion

    #region Restart Command

    [Fact]
    public void RestartCommand_CallsSessionManagerRestart()
    {
        // Act
        _sut.RestartCommand.Execute(null);

        // Assert
        _sessionManager.Received(1).Restart();
    }

    [Fact]
    public void CanRestart_ReturnsTrueWhenRunning()
    {
        // Arrange
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);

        // Act
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Assert
        _sut.CanRestart.Should().BeTrue();
        _sut.RestartCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CanRestart_ReturnsTrueWhenPaused()
    {
        // Arrange
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Paused;
        _sessionManager.CurrentSession.Returns(session);

        // Act
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Paused));

        // Assert
        _sut.CanRestart.Should().BeTrue();
        _sut.RestartCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CanRestart_ReturnsFalseWhenIdle()
    {
        // Assert - initial state
        _sut.CanRestart.Should().BeFalse();
        _sut.RestartCommand.CanExecute(null).Should().BeFalse();
    }

    #endregion
}
