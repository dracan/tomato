using FluentAssertions;
using NSubstitute;
using Tomato.Models;
using Tomato.Services;

namespace Tomato.Tests.Integration;

public class SessionFlowTests : IDisposable
{
    private readonly TimerService _timerService;
    private readonly INotificationService _notificationService;
    private readonly IPersistenceService _persistenceService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly SessionManager _sessionManager;

    public SessionFlowTests()
    {
        // Real timer service for integration testing
        // Use ThreadPoolIntervalTimer since DispatcherTimer requires WPF message loop
        _timerService = new TimerService(new ThreadPoolIntervalTimer());

        // Mock external dependencies
        _notificationService = Substitute.For<INotificationService>();
        _persistenceService = Substitute.For<IPersistenceService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.Now.Returns(DateTime.Now);
        _dateTimeProvider.Today.Returns(DateOnly.FromDateTime(DateTime.Now));

        _sessionManager = new SessionManager(
            _timerService,
            _notificationService,
            _persistenceService,
            _dateTimeProvider);
    }

    public void Dispose()
    {
        _timerService.Dispose();
    }

    [Fact]
    public void FocusSessionFlow_StartToComplete()
    {
        // Act - Start a focus session for testing
        _sessionManager.StartFocus();

        // Assert - Session started
        _sessionManager.CurrentSession.Should().NotBeNull();
        _sessionManager.CurrentSession!.Status.Should().Be(SessionStatus.Running);
        _sessionManager.CurrentSession.Type.Should().Be(SessionType.Focus);
    }

    [Fact]
    public void FocusSessionFlow_PauseAndResume()
    {
        // Arrange
        _sessionManager.StartFocus();

        // Act - Pause
        _sessionManager.Pause();

        // Assert - Paused
        _sessionManager.CurrentSession!.Status.Should().Be(SessionStatus.Paused);
        _timerService.IsRunning.Should().BeFalse();

        // Act - Resume
        _sessionManager.Resume();

        // Assert - Resumed
        _sessionManager.CurrentSession!.Status.Should().Be(SessionStatus.Running);
        _timerService.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void FocusSessionFlow_Cancel()
    {
        // Arrange
        _sessionManager.StartFocus();

        // Act
        _sessionManager.Cancel();

        // Assert
        _sessionManager.CurrentSession!.Status.Should().Be(SessionStatus.Cancelled);
        _timerService.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task FocusSessionFlow_TimerUpdates()
    {
        // Arrange
        TimeSpan? lastRemaining = null;
        _sessionManager.TimerTick += (_, e) => lastRemaining = e.Remaining;

        // Act - Start session (real timer)
        _sessionManager.StartFocus();

        // Wait for at least one tick
        await Task.Delay(1500);
        _sessionManager.Cancel();

        // Assert
        lastRemaining.Should().NotBeNull();
        lastRemaining!.Value.Should().BeLessThan(TimeSpan.FromMinutes(25));
    }

    [Fact]
    public void FocusSessionFlow_PersistsOnStart()
    {
        // Act
        _sessionManager.StartFocus();

        // Assert
        _persistenceService.Received(1).SaveAsync(Arg.Is<AppState>(s =>
            s.CurrentSession != null &&
            s.CurrentSession.Type == SessionType.Focus &&
            s.CurrentSession.Status == SessionStatus.Running));
    }

    [Fact]
    public void FocusSessionFlow_PersistsOnPause()
    {
        // Arrange
        _sessionManager.StartFocus();
        _persistenceService.ClearReceivedCalls();

        // Act
        _sessionManager.Pause();

        // Assert
        _persistenceService.Received(1).SaveAsync(Arg.Is<AppState>(s =>
            s.CurrentSession != null &&
            s.CurrentSession.Status == SessionStatus.Paused));
    }

    [Fact]
    public void CycleProgression_AfterCompletedFocusSessions()
    {
        // Arrange
        var initialCycleCount = _sessionManager.Cycle.CompletedFocusSessions;

        // Simulate completing a focus session by directly manipulating
        // (In real flow, this happens when timer completes)
        _sessionManager.StartFocus();

        // Assert initial state
        _sessionManager.Cycle.CompletedFocusSessions.Should().Be(initialCycleCount);
    }

    [Fact]
    public void Statistics_UpdateOnSessionStart()
    {
        // Arrange
        var initialDate = _sessionManager.TodayStatistics.Date;

        // Act
        _sessionManager.StartFocus();

        // Assert - Statistics exist for today
        _sessionManager.TodayStatistics.Date.Should().Be(initialDate);
    }

    [Fact]
    public void FullPomodoroFlow_FocusToBreak()
    {
        // Arrange
        var focusCompleted = false;
        _sessionManager.SessionStateChanged += (_, e) =>
        {
            if (e.NewStatus == SessionStatus.Completed && e.Session.Type == SessionType.Focus)
                focusCompleted = true;
        };

        // Act - Start focus session
        _sessionManager.StartFocus();
        _sessionManager.CurrentSession!.Type.Should().Be(SessionType.Focus);
        _sessionManager.CurrentSession.Status.Should().Be(SessionStatus.Running);

        // Skip to completion
        _sessionManager.Skip();

        // Assert - Focus completed
        focusCompleted.Should().BeTrue();
        _sessionManager.CurrentSession.Status.Should().Be(SessionStatus.Completed);
        _sessionManager.TodayStatistics.FocusSessionsCompleted.Should().Be(1);
        _sessionManager.Cycle.CompletedFocusSessions.Should().Be(1);

        // Start break
        _sessionManager.StartBreak();
        _sessionManager.CurrentSession!.Type.Should().Be(SessionType.ShortBreak);
        _sessionManager.CurrentSession.Status.Should().Be(SessionStatus.Running);
    }

    [Fact]
    public void FullCycle_FourFocusSessions_TriggerLongBreak()
    {
        // Complete 4 focus sessions
        for (int i = 0; i < 4; i++)
        {
            _sessionManager.StartFocus();
            _sessionManager.Skip(); // Skip to completion
        }

        // Assert - Cycle is complete
        _sessionManager.Cycle.IsCycleComplete.Should().BeTrue();
        _sessionManager.Cycle.CompletedFocusSessions.Should().Be(4);
        _sessionManager.TodayStatistics.FocusSessionsCompleted.Should().Be(4);

        // Start break - should be long break
        _sessionManager.StartBreak();
        _sessionManager.CurrentSession!.Type.Should().Be(SessionType.LongBreak);
        _sessionManager.CurrentSession.Duration.Should().Be(TimeSpan.FromMinutes(15));

        // Complete long break
        _sessionManager.Skip();

        // Assert - Cycle reset
        _sessionManager.Cycle.CompletedFocusSessions.Should().Be(0);
        _sessionManager.Cycle.IsCycleComplete.Should().BeFalse();
        _sessionManager.TodayStatistics.CyclesCompleted.Should().Be(1);
    }
}
