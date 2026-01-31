using Tomato.Models;

namespace Tomato.Services;

/// <summary>
/// Manages the lifecycle of Pomodoro sessions and cycles.
/// </summary>
public sealed class SessionManager : ISessionManager
{
    private readonly ITimerService _timerService;
    private readonly INotificationService _notificationService;
    private readonly IPersistenceService _persistenceService;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <inheritdoc />
    public event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;

    /// <inheritdoc />
    public event EventHandler<TimerTickEventArgs>? TimerTick;

    /// <inheritdoc />
    public Session? CurrentSession { get; private set; }

    /// <inheritdoc />
    public PomodoroCycle Cycle { get; private set; }

    /// <inheritdoc />
    public DailyStatistics TodayStatistics { get; private set; }

    public SessionManager(
        ITimerService timerService,
        INotificationService notificationService,
        IPersistenceService persistenceService,
        IDateTimeProvider dateTimeProvider)
    {
        _timerService = timerService;
        _notificationService = notificationService;
        _persistenceService = persistenceService;
        _dateTimeProvider = dateTimeProvider;

        Cycle = new PomodoroCycle();
        TodayStatistics = DailyStatistics.Create(_dateTimeProvider.Today);

        _timerService.Tick += OnTimerTick;
        _timerService.Completed += OnTimerCompleted;
    }

    /// <inheritdoc />
    public void StartFocus()
    {
        // Cancel any existing session
        if (CurrentSession?.Status == SessionStatus.Running)
        {
            CancelInternal();
        }

        CurrentSession = Session.CreateFocus();
        CurrentSession.Status = SessionStatus.Running;
        CurrentSession.StartedAt = _dateTimeProvider.Now;

        _timerService.Start(CurrentSession.Duration);

        RaiseSessionStateChanged(SessionStatus.NotStarted, SessionStatus.Running);
        PersistState();
    }

    /// <inheritdoc />
    public void StartFocus(TimeSpan duration)
    {
        // Cancel any existing session
        if (CurrentSession?.Status == SessionStatus.Running)
        {
            CancelInternal();
        }

        CurrentSession = Session.CreateFocus(duration);
        CurrentSession.Status = SessionStatus.Running;
        CurrentSession.StartedAt = _dateTimeProvider.Now;

        _timerService.Start(CurrentSession.Duration);

        RaiseSessionStateChanged(SessionStatus.NotStarted, SessionStatus.Running);
        PersistState();
    }

    /// <inheritdoc />
    public void StartBreak()
    {
        // Cancel any existing session
        if (CurrentSession?.Status == SessionStatus.Running)
        {
            CancelInternal();
        }

        // Determine break type based on cycle
        CurrentSession = Cycle.IsCycleComplete
            ? Session.CreateLongBreak()
            : Session.CreateShortBreak();

        CurrentSession.Status = SessionStatus.Running;
        CurrentSession.StartedAt = _dateTimeProvider.Now;

        _timerService.Start(CurrentSession.Duration);

        RaiseSessionStateChanged(SessionStatus.NotStarted, SessionStatus.Running);
        PersistState();
    }

    /// <inheritdoc />
    public void Pause()
    {
        if (CurrentSession?.Status != SessionStatus.Running)
            return;

        var previousStatus = CurrentSession.Status;
        _timerService.Pause();
        CurrentSession.SetTimeRemaining(_timerService.Remaining);
        CurrentSession.Status = SessionStatus.Paused;

        RaiseSessionStateChanged(previousStatus, SessionStatus.Paused);
        PersistState();
    }

    /// <inheritdoc />
    public void Resume()
    {
        if (CurrentSession?.Status != SessionStatus.Paused)
            return;

        var previousStatus = CurrentSession.Status;
        _timerService.Resume(CurrentSession.TimeRemaining);
        CurrentSession.Status = SessionStatus.Running;

        RaiseSessionStateChanged(previousStatus, SessionStatus.Running);
        PersistState();
    }

    /// <inheritdoc />
    public void Cancel()
    {
        CancelInternal();
        PersistState();
    }

    /// <inheritdoc />
    public void Skip()
    {
        if (CurrentSession == null)
            return;

        _timerService.Stop();
        CompleteSession();
    }

    /// <summary>
    /// Restores state from persistence.
    /// </summary>
    public async Task RestoreStateAsync()
    {
        var state = await _persistenceService.LoadAsync();
        if (state == null)
            return;

        // Restore cycle
        Cycle = state.RestoreCycle();

        // Restore statistics if for today
        var restoredStats = state.RestoreStatistics();
        if (restoredStats != null && restoredStats.Date == _dateTimeProvider.Today)
        {
            TodayStatistics = restoredStats;
        }

        // Restore session if it was paused
        var restoredSession = state.RestoreSession();
        if (restoredSession != null && restoredSession.Status == SessionStatus.Paused)
        {
            CurrentSession = restoredSession;
            RaiseSessionStateChanged(SessionStatus.NotStarted, SessionStatus.Paused);
        }
    }

    private void CancelInternal()
    {
        if (CurrentSession == null)
            return;

        var previousStatus = CurrentSession.Status;
        _timerService.Stop();
        CurrentSession.Status = SessionStatus.Cancelled;

        RaiseSessionStateChanged(previousStatus, SessionStatus.Cancelled);
    }

    private void OnTimerTick(object? sender, TimerTickEventArgs e)
    {
        if (CurrentSession != null)
        {
            CurrentSession.SetTimeRemaining(e.Remaining);
        }
        TimerTick?.Invoke(this, e);
    }

    private void OnTimerCompleted(object? sender, EventArgs e)
    {
        CompleteSession();
    }

    private void CompleteSession()
    {
        if (CurrentSession == null)
            return;

        var previousStatus = CurrentSession.Status;
        CurrentSession.Status = SessionStatus.Completed;
        CurrentSession.CompletedAt = _dateTimeProvider.Now;
        CurrentSession.SetTimeRemaining(TimeSpan.Zero);

        // Update statistics and cycle based on session type
        if (CurrentSession.Type == SessionType.Focus)
        {
            TodayStatistics.RecordFocusSession(CurrentSession.Duration);
            Cycle.IncrementFocusCount();

            if (Cycle.IsCycleComplete)
            {
                TodayStatistics.RecordCycleCompleted();
            }
        }
        else
        {
            TodayStatistics.RecordBreakSession(CurrentSession.Duration);

            // Reset cycle after long break
            if (CurrentSession.Type == SessionType.LongBreak)
            {
                Cycle.Reset();
            }
        }

        // Play notification sound
        _ = _notificationService.PlayCompletionSoundAsync(CurrentSession.Type);

        RaiseSessionStateChanged(previousStatus, SessionStatus.Completed);
        PersistState();
    }

    private void RaiseSessionStateChanged(SessionStatus previousStatus, SessionStatus newStatus)
    {
        if (CurrentSession != null)
        {
            SessionStateChanged?.Invoke(this,
                new SessionStateChangedEventArgs(CurrentSession, previousStatus, newStatus));
        }
    }

    private void PersistState()
    {
        var state = AppState.FromCurrentState(
            CurrentSession,
            Cycle,
            TodayStatistics,
            _dateTimeProvider.Now);

        _ = _persistenceService.SaveAsync(state);
    }
}
