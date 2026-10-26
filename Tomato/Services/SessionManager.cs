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
    private List<DailyStatistics> _statisticsHistory = new();
    private DailyStatistics _todayStatistics;
    private SessionRecord? _lastCompletedSessionRecord;
    private bool _overtimeEnabled;

    /// <inheritdoc />
    public event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;

    /// <inheritdoc />
    public event EventHandler<TimerTickEventArgs>? TimerTick;

    /// <inheritdoc />
    public Session? CurrentSession { get; private set; }

    /// <inheritdoc />
    public PomodoroCycle Cycle { get; private set; }

    /// <inheritdoc />
    /// <remarks>
    /// When the date has changed since the statistics were created (the app ran past
    /// midnight), the previous day is moved into history and a fresh day is started.
    /// </remarks>
    public DailyStatistics TodayStatistics
    {
        get
        {
            RollOverToNewDayIfNeeded();
            return _todayStatistics;
        }
        private set => _todayStatistics = value;
    }

    /// <inheritdoc />
    public IReadOnlyList<DailyStatistics> StatisticsHistory => _statisticsHistory;

    /// <inheritdoc />
    public bool OvertimeEnabled
    {
        get => _overtimeEnabled;
        set
        {
            if (_overtimeEnabled == value)
                return;

            _overtimeEnabled = value;
            PersistState();
        }
    }

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
        _todayStatistics = DailyStatistics.Create(_dateTimeProvider.Today);

        _timerService.Tick += OnTimerTick;
        _timerService.Completed += OnTimerCompleted;
    }

    /// <inheritdoc />
    public void StartFocus() => StartFocus(goal: null);

    /// <inheritdoc />
    public void StartFocus(string? goal) => StartFocus(TimeSpan.FromMinutes(25), goal);

    /// <inheritdoc />
    public void StartFocus(TimeSpan duration) => StartFocus(duration, goal: null);

    /// <inheritdoc />
    public void StartFocus(TimeSpan duration, string? goal) =>
        StartSession(Session.CreateFocus(duration, goal, OvertimeEnabled));

    /// <inheritdoc />
    public void StartBreak() => StartSession(Session.CreateShortBreak());

    /// <inheritdoc />
    public void StartBreak(TimeSpan duration) => StartSession(Session.CreateShortBreak(duration));

    /// <inheritdoc />
    public void StartLongBreak() => StartSession(Session.CreateLongBreak());

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
        _timerService.Resume(CurrentSession.TimeRemaining, CurrentSession.AllowsOvertime);
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

        if (CurrentSession.Type == SessionType.Focus)
        {
            MarkDone();
            return;
        }

        _timerService.Stop();
        CompleteSession();
    }

    /// <inheritdoc />
    public void MarkDone()
    {
        if (CurrentSession is not { Type: SessionType.Focus, Status: SessionStatus.Running or SessionStatus.Paused })
            return;

        // A paused session already holds the time remaining at the pause
        if (CurrentSession.Status == SessionStatus.Running)
        {
            CurrentSession.SetTimeRemaining(_timerService.Remaining);
        }

        _timerService.Stop();
        CompleteSession();
    }

    /// <inheritdoc />
    public void RecordSessionResults(string? results, int? rating)
    {
        if (_lastCompletedSessionRecord == null)
            return;

        _lastCompletedSessionRecord.Results = results;
        _lastCompletedSessionRecord.Rating = rating;
        PersistState();
    }

    /// <inheritdoc />
    public void Restart()
    {
        if (CurrentSession == null)
            return;

        if (CurrentSession.Status != SessionStatus.Running &&
            CurrentSession.Status != SessionStatus.Paused)
            return;

        var type = CurrentSession.Type;
        var duration = CurrentSession.Duration;
        var goal = CurrentSession.Goal;
        var allowsOvertime = CurrentSession.AllowsOvertime;

        _timerService.Stop();

        CurrentSession = type switch
        {
            SessionType.Focus => Session.CreateFocus(duration, goal, allowsOvertime),
            SessionType.ShortBreak => Session.CreateShortBreak(duration),
            SessionType.LongBreak => Session.CreateLongBreak(duration),
            _ => Session.CreateFocus(duration, goal)
        };

        CurrentSession.Status = SessionStatus.Running;
        CurrentSession.StartedAt = _dateTimeProvider.Now;

        _timerService.Start(duration, allowsOvertime);

        RaiseSessionStateChanged(SessionStatus.NotStarted, SessionStatus.Running);
        PersistState();
    }

    /// <inheritdoc />
    public void AddSupplementalActivity(string description)
    {
        var activity = new SupplementalActivity
        {
            Description = description
        };

        TodayStatistics.AddSupplementalActivity(activity);
        PersistState();
    }

    /// <summary>
    /// Restores state from persistence.
    /// </summary>
    public async Task RestoreStateAsync()
    {
        var state = await _persistenceService.LoadAsync();
        if (state == null)
            return;

        _overtimeEnabled = state.OvertimeEnabled;

        // Restore cycle
        Cycle = state.RestoreCycle();

        // Restore statistics history (excluding today - today is loaded separately)
        var restoredHistory = state.RestoreStatisticsHistory();
        _statisticsHistory = restoredHistory
            .Where(s => s.Date != _dateTimeProvider.Today)
            .ToList();

        // Restore statistics if for today
        var restoredStats = state.RestoreStatistics();
        if (restoredStats != null && restoredStats.Date == _dateTimeProvider.Today)
        {
            TodayStatistics = restoredStats;
        }

        // Restore an unfinished session as paused. A session that was still running
        // (e.g. the app crashed) resumes from the time remaining when it was last saved.
        var restoredSession = state.RestoreSession();
        if (restoredSession is { Status: SessionStatus.Paused or SessionStatus.Running })
        {
            restoredSession.Status = SessionStatus.Paused;
            CurrentSession = restoredSession;
            RaiseSessionStateChanged(SessionStatus.NotStarted, SessionStatus.Paused);
        }
    }

    /// <inheritdoc />
    public void PrepareForShutdown()
    {
        Pause();
        _persistenceService.SaveAsync(CreateAppState()).GetAwaiter().GetResult();
    }

    private void RollOverToNewDayIfNeeded()
    {
        var today = _dateTimeProvider.Today;
        if (_todayStatistics.Date == today)
            return;

        if (_todayStatistics.HasData)
        {
            _statisticsHistory.RemoveAll(s => s.Date == _todayStatistics.Date);
            _statisticsHistory.Add(_todayStatistics);
        }

        _todayStatistics = DailyStatistics.Create(today);
    }

    private void StartSession(Session session)
    {
        // End any unfinished session so subscribers see it cancelled
        if (CurrentSession?.Status is SessionStatus.Running or SessionStatus.Paused)
        {
            CancelInternal();
        }

        CurrentSession = session;
        CurrentSession.Status = SessionStatus.Running;
        CurrentSession.StartedAt = _dateTimeProvider.Now;

        _timerService.Start(CurrentSession.Duration, CurrentSession.AllowsOvertime);

        RaiseSessionStateChanged(SessionStatus.NotStarted, SessionStatus.Running);
        PersistState();
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
        if (CurrentSession == null)
            return;

        if (CurrentSession.AllowsOvertime)
        {
            EnterOvertime();
            return;
        }

        CurrentSession.SetTimeRemaining(TimeSpan.Zero);
        CompleteSession();
    }

    /// <summary>
    /// The Estimate has run out, but the session carries on until it is marked done.
    /// </summary>
    private void EnterOvertime()
    {
        var remaining = _timerService.Remaining;
        CurrentSession!.SetTimeRemaining(remaining > TimeSpan.Zero ? TimeSpan.Zero : remaining);

        _ = _notificationService.PlayCompletionSoundAsync(CurrentSession.Type);

        PersistState();
        RaiseSessionStateChanged(SessionStatus.Running, SessionStatus.Running);
    }

    private void CompleteSession()
    {
        if (CurrentSession == null)
            return;

        var previousStatus = CurrentSession.Status;
        CurrentSession.Status = SessionStatus.Completed;
        CurrentSession.CompletedAt = _dateTimeProvider.Now;

        // Update statistics and cycle based on session type
        if (CurrentSession.Type == SessionType.Focus)
        {
            TodayStatistics.RecordFocusSession(CurrentSession.TimeSpent);
            Cycle.IncrementFocusCount();

            if (Cycle.IsCycleComplete)
            {
                TodayStatistics.RecordCycleCompleted();
            }

            // Create a session record for the completed focus session
            _lastCompletedSessionRecord = new SessionRecord
            {
                Goal = CurrentSession.Goal,
                Results = null,
                Estimate = CurrentSession.Duration,
                TimeSpent = CurrentSession.TimeSpent,
                StartedAt = CurrentSession.StartedAt ?? _dateTimeProvider.Now,
                CompletedAt = CurrentSession.CompletedAt ?? _dateTimeProvider.Now
            };
            TodayStatistics.AddSessionRecord(_lastCompletedSessionRecord);
        }
        else
        {
            CurrentSession.SetTimeRemaining(TimeSpan.Zero);
            TodayStatistics.RecordBreakSession(CurrentSession.Duration);

            // Reset cycle after long break
            if (CurrentSession.Type == SessionType.LongBreak)
            {
                Cycle.Reset();
            }
        }

        // Play notification sound
        _ = _notificationService.PlayCompletionSoundAsync(CurrentSession.Type);

        // Save before notifying: a subscriber may block (the results dialog is modal)
        PersistState();
        RaiseSessionStateChanged(previousStatus, SessionStatus.Completed);
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
        _ = _persistenceService.SaveAsync(CreateAppState());
    }

    private AppState CreateAppState() => AppState.FromCurrentState(
        CurrentSession,
        Cycle,
        TodayStatistics,
        _statisticsHistory,
        _dateTimeProvider.Now,
        OvertimeEnabled);
}
