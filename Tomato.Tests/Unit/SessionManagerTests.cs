using FluentAssertions;
using NSubstitute;
using Tomato.Models;
using Tomato.Services;

namespace Tomato.Tests.Unit;

public class SessionManagerTests
{
    private readonly ITimerService _timerService;
    private readonly INotificationService _notificationService;
    private readonly IPersistenceService _persistenceService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly SessionManager _sut;

    public SessionManagerTests()
    {
        _timerService = Substitute.For<ITimerService>();
        _notificationService = Substitute.For<INotificationService>();
        _persistenceService = Substitute.For<IPersistenceService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.Now.Returns(DateTime.Now);
        _dateTimeProvider.Today.Returns(DateOnly.FromDateTime(DateTime.Now));

        _sut = new SessionManager(
            _timerService,
            _notificationService,
            _persistenceService,
            _dateTimeProvider);
    }

    #region US1: Start Focus Session

    [Fact]
    public void StartFocus_CreatesNewFocusSession()
    {
        // Act
        _sut.StartFocus();

        // Assert
        _sut.CurrentSession.Should().NotBeNull();
        _sut.CurrentSession!.Type.Should().Be(SessionType.Focus);
    }

    [Fact]
    public void StartFocus_SetsSessionStatusToRunning()
    {
        // Act
        _sut.StartFocus();

        // Assert
        _sut.CurrentSession!.Status.Should().Be(SessionStatus.Running);
    }

    [Fact]
    public void StartFocus_StartsTimerWith25Minutes()
    {
        // Act
        _sut.StartFocus();

        // Assert
        _timerService.Received(1).Start(TimeSpan.FromMinutes(25));
    }

    [Fact]
    public void StartFocus_SetsSessionStartedAt()
    {
        // Arrange
        var expectedTime = new DateTime(2024, 1, 15, 10, 0, 0);
        _dateTimeProvider.Now.Returns(expectedTime);

        // Act
        _sut.StartFocus();

        // Assert
        _sut.CurrentSession!.StartedAt.Should().Be(expectedTime);
    }

    [Fact]
    public void StartFocus_RaisesSessionStateChangedEvent()
    {
        // Arrange
        SessionStateChangedEventArgs? eventArgs = null;
        _sut.SessionStateChanged += (_, e) => eventArgs = e;

        // Act
        _sut.StartFocus();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.NewStatus.Should().Be(SessionStatus.Running);
    }

    [Fact]
    public void StartFocus_PersistsState()
    {
        // Act
        _sut.StartFocus();

        // Assert
        _persistenceService.Received(1).SaveAsync(Arg.Any<AppState>());
    }

    [Fact]
    public void StartFocus_WhenSessionAlreadyRunning_CancelsExisting()
    {
        // Arrange
        _sut.StartFocus();
        var firstSession = _sut.CurrentSession;

        // Act
        _sut.StartFocus();

        // Assert
        _sut.CurrentSession.Should().NotBeSameAs(firstSession);
    }

    [Fact]
    public void StartFocus_WithCustomDuration_SetsCorrectDuration()
    {
        // Arrange
        var customDuration = TimeSpan.FromMinutes(15);

        // Act
        _sut.StartFocus(customDuration);

        // Assert
        _sut.CurrentSession.Should().NotBeNull();
        _sut.CurrentSession!.Duration.Should().Be(customDuration);
    }

    [Fact]
    public void StartFocus_WithCustomDuration_SetsStatusToRunning()
    {
        // Arrange
        var customDuration = TimeSpan.FromMinutes(10);

        // Act
        _sut.StartFocus(customDuration);

        // Assert
        _sut.CurrentSession!.Status.Should().Be(SessionStatus.Running);
    }

    [Fact]
    public void StartFocus_WithCustomDuration_StartsTimerWithCorrectDuration()
    {
        // Arrange
        var customDuration = TimeSpan.FromMinutes(20);

        // Act
        _sut.StartFocus(customDuration);

        // Assert
        _timerService.Received(1).Start(customDuration);
    }

    #endregion

    #region US1: Complete Session

    [Fact]
    public void OnTimerCompleted_SetsSessionStatusToCompleted()
    {
        // Arrange
        _sut.StartFocus();

        // Act - simulate timer completion
        _timerService.Completed += Raise.Event();

        // Assert
        _sut.CurrentSession!.Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public void OnTimerCompleted_PlaysNotificationSound()
    {
        // Arrange
        _sut.StartFocus();

        // Act
        _timerService.Completed += Raise.Event();

        // Assert
        _notificationService.Received(1).PlayCompletionSoundAsync(SessionType.Focus);
    }

    [Fact]
    public void OnTimerCompleted_IncrementsStatistics()
    {
        // Arrange
        _sut.StartFocus();
        var initialCount = _sut.TodayStatistics.FocusSessionsCompleted;

        // Act
        _timerService.Completed += Raise.Event();

        // Assert
        _sut.TodayStatistics.FocusSessionsCompleted.Should().Be(initialCount + 1);
    }

    [Fact]
    public void OnTimerCompleted_RaisesSessionStateChangedEvent()
    {
        // Arrange
        _sut.StartFocus();
        SessionStateChangedEventArgs? eventArgs = null;
        _sut.SessionStateChanged += (_, e) => eventArgs = e;

        // Act
        _timerService.Completed += Raise.Event();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.NewStatus.Should().Be(SessionStatus.Completed);
    }

    #endregion

    #region Timer Tick Forwarding

    [Fact]
    public void OnTimerTick_UpdatesSessionTimeRemaining()
    {
        // Arrange
        _sut.StartFocus();
        var elapsed = TimeSpan.FromMinutes(5);
        var remaining = TimeSpan.FromMinutes(20);

        // Act
        _timerService.Tick += Raise.EventWith(new TimerTickEventArgs(elapsed, remaining));

        // Assert
        _sut.CurrentSession!.TimeRemaining.Should().Be(remaining);
    }

    [Fact]
    public void OnTimerTick_RaisesTimerTickEvent()
    {
        // Arrange
        _sut.StartFocus();
        TimerTickEventArgs? receivedArgs = null;
        _sut.TimerTick += (_, e) => receivedArgs = e;

        var elapsed = TimeSpan.FromMinutes(5);
        var remaining = TimeSpan.FromMinutes(20);

        // Act
        _timerService.Tick += Raise.EventWith(new TimerTickEventArgs(elapsed, remaining));

        // Assert
        receivedArgs.Should().NotBeNull();
        receivedArgs!.Remaining.Should().Be(remaining);
    }

    #endregion

    #region Pause/Resume

    [Fact]
    public void Pause_SetsSessionStatusToPaused()
    {
        // Arrange
        _sut.StartFocus();

        // Act
        _sut.Pause();

        // Assert
        _sut.CurrentSession!.Status.Should().Be(SessionStatus.Paused);
    }

    [Fact]
    public void Pause_PausesTimer()
    {
        // Arrange
        _sut.StartFocus();

        // Act
        _sut.Pause();

        // Assert
        _timerService.Received(1).Pause();
    }

    [Fact]
    public void Pause_PersistsState()
    {
        // Arrange
        _sut.StartFocus();
        _persistenceService.ClearReceivedCalls();

        // Act
        _sut.Pause();

        // Assert
        _persistenceService.Received(1).SaveAsync(Arg.Any<AppState>());
    }

    [Fact]
    public void Resume_SetsSessionStatusToRunning()
    {
        // Arrange
        _sut.StartFocus();
        _sut.Pause();

        // Act
        _sut.Resume();

        // Assert
        _sut.CurrentSession!.Status.Should().Be(SessionStatus.Running);
    }

    [Fact]
    public void Resume_ResumesTimerWithRemainingTime()
    {
        // Arrange
        _sut.StartFocus();
        var remaining = TimeSpan.FromMinutes(20);
        _timerService.Remaining.Returns(remaining);
        _sut.Pause();

        // Act
        _sut.Resume();

        // Assert
        _timerService.Received(1).Resume(remaining);
    }

    #endregion

    #region Cancel

    [Fact]
    public void Cancel_SetsSessionStatusToCancelled()
    {
        // Arrange
        _sut.StartFocus();

        // Act
        _sut.Cancel();

        // Assert
        _sut.CurrentSession!.Status.Should().Be(SessionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_StopsTimer()
    {
        // Arrange
        _sut.StartFocus();

        // Act
        _sut.Cancel();

        // Assert
        _timerService.Received(1).Stop();
    }

    #endregion

    #region Cycle Tracking

    [Fact]
    public void StartFocus_IncrementsCycleCountOnCompletion()
    {
        // Arrange
        var initialCount = _sut.Cycle.CompletedFocusSessions;
        _sut.StartFocus();

        // Act
        _timerService.Completed += Raise.Event();

        // Assert
        _sut.Cycle.CompletedFocusSessions.Should().Be(initialCount + 1);
    }

    [Fact]
    public void TodayStatistics_InitializesWithCurrentDate()
    {
        // Arrange
        var today = new DateOnly(2024, 1, 15);
        _dateTimeProvider.Today.Returns(today);

        // Act - create new session manager
        var sut = new SessionManager(
            _timerService, _notificationService, _persistenceService, _dateTimeProvider);

        // Assert
        sut.TodayStatistics.Date.Should().Be(today);
    }

    #endregion

    #region US2: Start Break Session

    [Fact]
    public void StartBreak_CreatesShortBreakSession_WhenCycleNotComplete()
    {
        // Arrange - cycle not complete (0/4 focus sessions)

        // Act
        _sut.StartBreak();

        // Assert
        _sut.CurrentSession.Should().NotBeNull();
        _sut.CurrentSession!.Type.Should().Be(SessionType.ShortBreak);
    }

    [Fact]
    public void StartBreak_SetsSessionStatusToRunning()
    {
        // Act
        _sut.StartBreak();

        // Assert
        _sut.CurrentSession!.Status.Should().Be(SessionStatus.Running);
    }

    [Fact]
    public void StartBreak_StartsTimerWith5Minutes_ForShortBreak()
    {
        // Act
        _sut.StartBreak();

        // Assert
        _timerService.Received(1).Start(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void StartBreak_AlwaysCreatesShortBreak()
    {
        // Arrange - complete a cycle (4 focus sessions)
        for (int i = 0; i < 4; i++)
        {
            _sut.StartFocus();
            _timerService.Completed += Raise.Event();
        }

        // Act
        _sut.StartBreak();

        // Assert - should still be short break (cycle no longer affects break type)
        _sut.CurrentSession.Should().NotBeNull();
        _sut.CurrentSession!.Type.Should().Be(SessionType.ShortBreak);
    }

    [Fact]
    public void StartLongBreak_CreatesLongBreakSession()
    {
        // Act
        _sut.StartLongBreak();

        // Assert
        _sut.CurrentSession.Should().NotBeNull();
        _sut.CurrentSession!.Type.Should().Be(SessionType.LongBreak);
        _timerService.Received(1).Start(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void StartBreak_RaisesSessionStateChangedEvent()
    {
        // Arrange
        SessionStateChangedEventArgs? eventArgs = null;
        _sut.SessionStateChanged += (_, e) => eventArgs = e;

        // Act
        _sut.StartBreak();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.NewStatus.Should().Be(SessionStatus.Running);
        eventArgs.Session.Type.Should().Be(SessionType.ShortBreak);
    }

    [Fact]
    public void StartBreak_PersistsState()
    {
        // Act
        _sut.StartBreak();

        // Assert
        _persistenceService.Received(1).SaveAsync(Arg.Any<AppState>());
    }

    [Fact]
    public void OnBreakCompleted_RecordsBreakTimeInStatistics()
    {
        // Arrange
        _sut.StartBreak();
        var initialBreakTime = _sut.TodayStatistics.TotalBreakTime;

        // Act
        _timerService.Completed += Raise.Event();

        // Assert
        _sut.TodayStatistics.TotalBreakTime.Should().BeGreaterThan(initialBreakTime);
    }

    [Fact]
    public void OnLongBreakCompleted_ResetsCycle()
    {
        // Arrange - complete 4 focus sessions
        for (int i = 0; i < 4; i++)
        {
            _sut.StartFocus();
            _timerService.Completed += Raise.Event();
        }
        _sut.Cycle.CompletedFocusSessions.Should().Be(4);

        // Start and complete long break (explicitly)
        _sut.StartLongBreak();

        // Act
        _timerService.Completed += Raise.Event();

        // Assert
        _sut.Cycle.CompletedFocusSessions.Should().Be(0);
    }

    #endregion

    #region Goal Support

    [Fact]
    public void StartFocus_WithGoal_StoresGoalOnSession()
    {
        // Arrange
        var goal = "Complete code review";

        // Act
        _sut.StartFocus(goal);

        // Assert
        _sut.CurrentSession.Should().NotBeNull();
        _sut.CurrentSession!.Goal.Should().Be(goal);
    }

    [Fact]
    public void StartFocus_WithDurationAndGoal_StoresBoth()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(15);
        var goal = "Write unit tests";

        // Act
        _sut.StartFocus(duration, goal);

        // Assert
        _sut.CurrentSession.Should().NotBeNull();
        _sut.CurrentSession!.Duration.Should().Be(duration);
        _sut.CurrentSession!.Goal.Should().Be(goal);
    }

    [Fact]
    public void StartFocus_WithNullGoal_HasNullGoalOnSession()
    {
        // Act
        _sut.StartFocus(goal: null);

        // Assert
        _sut.CurrentSession.Should().NotBeNull();
        _sut.CurrentSession!.Goal.Should().BeNull();
    }

    [Fact]
    public void StartFocus_WithoutGoal_HasNullGoalOnSession()
    {
        // Act
        _sut.StartFocus();

        // Assert
        _sut.CurrentSession.Should().NotBeNull();
        _sut.CurrentSession!.Goal.Should().BeNull();
    }

    #endregion
}
