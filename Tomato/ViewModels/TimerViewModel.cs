using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tomato.Models;
using Tomato.Services;

namespace Tomato.ViewModels;

/// <summary>
/// ViewModel for the timer display and controls.
/// </summary>
public partial class TimerViewModel : ObservableObject
{
    private readonly ISessionManager _sessionManager;
    private readonly IDialogService _dialogService;
    private readonly IStatisticsReportService _statisticsReportService;

    [ObservableProperty]
    private TimeSpan _timeRemaining = TimeSpan.FromMinutes(25);

    [ObservableProperty]
    private string _sessionLabel = "Focus";

    [ObservableProperty]
    private SessionType _currentSessionType = SessionType.Focus;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private bool _isSessionComplete;

    [ObservableProperty]
    private int _completedSessionsToday;

    [ObservableProperty]
    private string _cycleProgress = "0/4";

    [ObservableProperty]
    private string? _currentGoal;

    [ObservableProperty]
    private bool _isTodoInputVisible;

    [ObservableProperty]
    private string _newTodoText = string.Empty;

    [ObservableProperty]
    private bool _isInOvertime;

    [ObservableProperty]
    private bool _isOvertimeEnabled;

    /// <summary>
    /// Collection of todos captured during the current focus session.
    /// </summary>
    public ObservableCollection<TodoItem> CapturedTodos { get; } = new();

    /// <summary>
    /// Collection of completed session records for today, used to display completion dots.
    /// </summary>
    public ObservableCollection<SessionRecord> CompletedSessionRecords { get; } = new();

    /// <summary>
    /// Gets whether there are any captured todos.
    /// </summary>
    public bool HasCapturedTodos => CapturedTodos.Count > 0;

    /// <summary>
    /// Gets the count of captured todos for display.
    /// </summary>
    public string CapturedTodosCountText => CapturedTodos.Count == 1 ? "1 todo" : $"{CapturedTodos.Count} todos";

    /// <summary>
    /// Gets whether we can show the todo input (only during focus session).
    /// </summary>
    public bool CanShowTodoInput => (IsRunning || IsPaused) && CurrentSessionType == SessionType.Focus;

    /// <summary>
    /// Gets whether the current focus session can be marked done.
    /// </summary>
    public bool CanMarkDone => (IsRunning || IsPaused) && CurrentSessionType == SessionType.Focus;

    /// <summary>
    /// Returns the goal text for tooltip display, or null if no tooltip should be shown.
    /// Tooltip is only shown when a session is active (running or paused) and has a non-empty goal.
    /// </summary>
    public string? GoalTooltip =>
        (IsRunning || IsPaused) && !string.IsNullOrWhiteSpace(CurrentGoal)
            ? CurrentGoal
            : null;

    public bool CanRestart => IsRunning || IsPaused;

    partial void OnIsRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(GoalTooltip));
        OnPropertyChanged(nameof(CanShowTodoInput));
        OnPropertyChanged(nameof(CanMarkDone));
        MarkDoneCommand.NotifyCanExecuteChanged();
        RestartCommand.NotifyCanExecuteChanged();
        ShowTodoInputCommand.NotifyCanExecuteChanged();
        AddTodoCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsPausedChanged(bool value)
    {
        OnPropertyChanged(nameof(GoalTooltip));
        OnPropertyChanged(nameof(CanShowTodoInput));
        OnPropertyChanged(nameof(CanMarkDone));
        MarkDoneCommand.NotifyCanExecuteChanged();
        RestartCommand.NotifyCanExecuteChanged();
        ShowTodoInputCommand.NotifyCanExecuteChanged();
        AddTodoCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentGoalChanged(string? value) => OnPropertyChanged(nameof(GoalTooltip));

    partial void OnIsOvertimeEnabledChanged(bool value) => _sessionManager.OvertimeEnabled = value;

    partial void OnCurrentSessionTypeChanged(SessionType value)
    {
        OnPropertyChanged(nameof(CanShowTodoInput));
        OnPropertyChanged(nameof(CanMarkDone));
        MarkDoneCommand.NotifyCanExecuteChanged();
        ShowTodoInputCommand.NotifyCanExecuteChanged();
        AddTodoCommand.NotifyCanExecuteChanged();
    }

    public TimerViewModel(ISessionManager sessionManager, IDialogService dialogService, IStatisticsReportService statisticsReportService)
    {
        _sessionManager = sessionManager;
        _dialogService = dialogService;
        _statisticsReportService = statisticsReportService;

        // Subscribe to session manager events
        _sessionManager.TimerTick += OnTimerTick;
        _sessionManager.SessionStateChanged += OnSessionStateChanged;

        // Initialize from current state
        _isOvertimeEnabled = _sessionManager.OvertimeEnabled;
        UpdateFromSessionManager();
    }

    [RelayCommand]
    private Task StartFocusAsync() => StartFocusFromGoalDialogAsync(25, focusEstimate: false);

    [RelayCommand]
    private async Task StartFocusWithDurationAsync(string minutesString)
    {
        if (!int.TryParse(minutesString, out var minutes))
        {
            return;
        }

        await StartFocusFromGoalDialogAsync(minutes, focusEstimate: false);
    }

    [RelayCommand]
    private Task StartFocusCustomAsync() => StartFocusFromGoalDialogAsync(25, focusEstimate: true);

    /// <summary>
    /// Asks for the goal and Estimate, with the Estimate filled in, then starts the focus session.
    /// </summary>
    private async Task StartFocusFromGoalDialogAsync(int estimateMinutes, bool focusEstimate)
    {
        var result = await _dialogService.ShowGoalDialogAsync(estimateMinutes, focusEstimate);
        if (result.Confirmed)
        {
            await FinishPendingFocusWorkAsync();
            CurrentGoal = result.Goal;
            _sessionManager.StartFocus(TimeSpan.FromMinutes(result.EstimateMinutes), result.Goal);
        }
    }

    [RelayCommand]
    private async Task StartBreakAsync()
    {
        await FinishPendingFocusWorkAsync();
        _sessionManager.StartBreak();
    }

    [RelayCommand]
    private async Task StartBreakWithDurationAsync(string minutesString)
    {
        if (!int.TryParse(minutesString, out var minutes))
        {
            return;
        }

        await FinishPendingFocusWorkAsync();
        _sessionManager.StartBreak(TimeSpan.FromMinutes(minutes));
    }

    [RelayCommand]
    private async Task StartLongBreakAsync()
    {
        await FinishPendingFocusWorkAsync();
        _sessionManager.StartLongBreak();
    }

    [RelayCommand]
    private void Pause()
    {
        _sessionManager.Pause();
    }

    [RelayCommand]
    private void Resume()
    {
        _sessionManager.Resume();
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await FinishPendingFocusWorkAsync();
        _sessionManager.Cancel();
        ResetToDefault();
    }

    /// <summary>
    /// Called before the current session is cancelled or replaced: shows any todos
    /// captured during an unfinished focus session so they aren't lost, then clears
    /// them and the goal.
    /// </summary>
    private async Task FinishPendingFocusWorkAsync()
    {
        if (CapturedTodos.Count > 0)
        {
            await _dialogService.ShowTodosDialogAsync(CapturedTodos.ToList().AsReadOnly());
        }

        ClearCapturedTodos();
        CurrentGoal = null;
    }

    [RelayCommand]
    private void Skip()
    {
        _sessionManager.Skip();
    }

    [RelayCommand(CanExecute = nameof(CanMarkDone))]
    private void MarkDone()
    {
        _sessionManager.MarkDone();
    }

    [RelayCommand(CanExecute = nameof(CanRestart))]
    private void Restart()
    {
        _sessionManager.Restart();
    }

    [RelayCommand]
    private void ViewStats()
    {
        _statisticsReportService.GenerateAndOpenReport();
    }

    [RelayCommand]
    private async Task AddSupplementalActivityAsync()
    {
        var result = await _dialogService.ShowSupplementalActivityDialogAsync();
        if (result.Confirmed && !string.IsNullOrWhiteSpace(result.Description))
        {
            _sessionManager.AddSupplementalActivity(result.Description);
        }
    }

    [RelayCommand(CanExecute = nameof(CanShowTodoInput))]
    private void ShowTodoInput()
    {
        IsTodoInputVisible = true;
    }

    [RelayCommand(CanExecute = nameof(CanShowTodoInput))]
    private void AddTodo()
    {
        if (string.IsNullOrWhiteSpace(NewTodoText))
        {
            CancelTodoInput();
            return;
        }

        CapturedTodos.Add(new TodoItem(NewTodoText.Trim(), DateTime.Now));
        OnPropertyChanged(nameof(HasCapturedTodos));
        OnPropertyChanged(nameof(CapturedTodosCountText));
        NewTodoText = string.Empty;
        IsTodoInputVisible = false;
    }

    [RelayCommand]
    private void CancelTodoInput()
    {
        NewTodoText = string.Empty;
        IsTodoInputVisible = false;
    }

    private void OnTimerTick(object? sender, TimerTickEventArgs e)
    {
        TimeRemaining = e.Remaining;
        IsInOvertime = _sessionManager.CurrentSession?.IsInOvertime == true;
    }

    private void OnSessionStateChanged(object? sender, SessionStateChangedEventArgs e)
    {
        UpdateFromSession(e.Session, e.NewStatus);
        UpdateStatistics();

        // Show results dialog when a focus session completes
        if (e.NewStatus == SessionStatus.Completed && e.Session.Type == SessionType.Focus)
        {
            var estimateSummary = EstimateSummary.Describe(e.Session.Duration, e.Session.TimeSpent);
            ShowResultsDialogAfterCurrentEvent(estimateSummary);
        }
    }

    private void ShowResultsDialogAfterCurrentEvent(string estimateSummary)
    {
        // The dialog is modal, so show it once this event has finished dispatching;
        // otherwise later subscribers (e.g. the tray icon) wait until it is closed.
        var context = SynchronizationContext.Current;
        if (context != null)
        {
            context.Post(_ => _ = ShowResultsDialogAsync(estimateSummary), null);
        }
        else
        {
            _ = ShowResultsDialogAsync(estimateSummary);
        }
    }

    private async Task ShowResultsDialogAsync(string estimateSummary)
    {
        var todos = CapturedTodos.Count > 0 ? CapturedTodos.ToList().AsReadOnly() : null;
        var result = await _dialogService.ShowResultsDialogAsync(CurrentGoal, todos, estimateSummary);
        if (result.Confirmed && (!string.IsNullOrWhiteSpace(result.Results) || result.Rating.HasValue))
        {
            _sessionManager.RecordSessionResults(result.Results, result.Rating);
            SyncCompletedSessionRecords(forceRefresh: true);
        }

        // Clear the goal and todos, unless a new session was started while the dialog was open
        if (_sessionManager.CurrentSession is not { Status: SessionStatus.Running or SessionStatus.Paused })
        {
            CurrentGoal = null;
            ClearCapturedTodos();
        }
    }

    private void ClearCapturedTodos()
    {
        CapturedTodos.Clear();
        OnPropertyChanged(nameof(HasCapturedTodos));
        OnPropertyChanged(nameof(CapturedTodosCountText));
    }

    private void UpdateFromSession(Session session, SessionStatus status)
    {
        CurrentSessionType = session.Type;
        SessionLabel = GetSessionLabel(session.Type);

        // A session marked done early still has time left; once finished, show zero
        TimeRemaining = status == SessionStatus.Completed ? TimeSpan.Zero : session.TimeRemaining;

        IsRunning = status == SessionStatus.Running;
        IsPaused = status == SessionStatus.Paused;
        IsSessionComplete = status == SessionStatus.Completed;
        IsInOvertime = session.AllowsOvertime &&
            session.TimeRemaining <= TimeSpan.Zero &&
            status is SessionStatus.Running or SessionStatus.Paused;

        if (!CanShowTodoInput)
        {
            CancelTodoInput();
        }
    }

    private void UpdateFromSessionManager()
    {
        var session = _sessionManager.CurrentSession;
        if (session != null)
        {
            UpdateFromSession(session, session.Status);
            if (session.Status is SessionStatus.Running or SessionStatus.Paused)
            {
                CurrentGoal = session.Goal;
            }
        }
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        CompletedSessionsToday = _sessionManager.TodayStatistics.FocusSessionsCompleted;
        CycleProgress = $"{_sessionManager.Cycle.CompletedFocusSessions}/{PomodoroCycle.FocusSessionsPerCycle}";
        SyncCompletedSessionRecords();
    }

    private void SyncCompletedSessionRecords(bool forceRefresh = false)
    {
        var records = _sessionManager.TodayStatistics.SessionRecords;

        // Update if counts differ (new sessions added) or forced (e.g. after rating update)
        if (forceRefresh || CompletedSessionRecords.Count != records.Count)
        {
            CompletedSessionRecords.Clear();
            foreach (var record in records)
            {
                CompletedSessionRecords.Add(record);
            }
        }
    }

    private void ResetToDefault()
    {
        TimeRemaining = TimeSpan.FromMinutes(25);
        SessionLabel = "Focus";
        CurrentSessionType = SessionType.Focus;
        IsRunning = false;
        IsPaused = false;
        IsSessionComplete = false;
        IsInOvertime = false;
    }

    private static string GetSessionLabel(SessionType type) => type switch
    {
        SessionType.Focus => "Focus",
        SessionType.ShortBreak => "Short Break",
        SessionType.LongBreak => "Long Break",
        _ => "Focus"
    };
}
