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

    /// <summary>
    /// Collection of todos captured during the current focus session.
    /// </summary>
    public ObservableCollection<TodoItem> CapturedTodos { get; } = new();

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
        RestartCommand.NotifyCanExecuteChanged();
        ShowTodoInputCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsPausedChanged(bool value)
    {
        OnPropertyChanged(nameof(GoalTooltip));
        OnPropertyChanged(nameof(CanShowTodoInput));
        RestartCommand.NotifyCanExecuteChanged();
        ShowTodoInputCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentGoalChanged(string? value) => OnPropertyChanged(nameof(GoalTooltip));

    partial void OnCurrentSessionTypeChanged(SessionType value)
    {
        OnPropertyChanged(nameof(CanShowTodoInput));
        ShowTodoInputCommand.NotifyCanExecuteChanged();
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
        UpdateFromSessionManager();
    }

    [RelayCommand]
    private async Task StartFocusAsync()
    {
        var result = await _dialogService.ShowGoalDialogAsync();
        if (result.Confirmed)
        {
            ClearCapturedTodos();
            CurrentGoal = result.Goal;
            _sessionManager.StartFocus(result.Goal);
        }
    }

    [RelayCommand]
    private async Task StartFocusWithDurationAsync(string minutesString)
    {
        if (!int.TryParse(minutesString, out var minutes))
        {
            return;
        }

        var result = await _dialogService.ShowGoalDialogAsync();
        if (result.Confirmed)
        {
            ClearCapturedTodos();
            CurrentGoal = result.Goal;
            _sessionManager.StartFocus(TimeSpan.FromMinutes(minutes), result.Goal);
        }
    }

    [RelayCommand]
    private void StartBreak()
    {
        _sessionManager.StartBreak();
    }

    [RelayCommand]
    private void StartLongBreak()
    {
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
        // If cancelling a focus session with captured todos, show them first
        if (CurrentSessionType == SessionType.Focus && CapturedTodos.Count > 0)
        {
            var todos = CapturedTodos.ToList().AsReadOnly();
            await _dialogService.ShowTodosDialogAsync(todos);
        }

        _sessionManager.Cancel();
        ClearCapturedTodos();
        ResetToDefault();
    }

    [RelayCommand]
    private void Skip()
    {
        _sessionManager.Skip();
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

    [RelayCommand(CanExecute = nameof(CanShowTodoInput))]
    private void ShowTodoInput()
    {
        IsTodoInputVisible = true;
    }

    [RelayCommand]
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
    }

    private void OnSessionStateChanged(object? sender, SessionStateChangedEventArgs e)
    {
        UpdateFromSession(e.Session, e.NewStatus);
        UpdateStatistics();

        // Show results dialog when a focus session completes
        if (e.NewStatus == SessionStatus.Completed && e.Session.Type == SessionType.Focus)
        {
            _ = ShowResultsDialogAsync();
        }
    }

    private async Task ShowResultsDialogAsync()
    {
        var todos = CapturedTodos.Count > 0 ? CapturedTodos.ToList().AsReadOnly() : null;
        var result = await _dialogService.ShowResultsDialogAsync(CurrentGoal, todos);
        if (result.Confirmed && (!string.IsNullOrWhiteSpace(result.Results) || result.Rating.HasValue))
        {
            _sessionManager.RecordSessionResults(result.Results, result.Rating);
        }

        // Clear the goal and todos after the session is complete
        CurrentGoal = null;
        ClearCapturedTodos();
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
        TimeRemaining = session.TimeRemaining;

        IsRunning = status == SessionStatus.Running;
        IsPaused = status == SessionStatus.Paused;
        IsSessionComplete = status == SessionStatus.Completed;
    }

    private void UpdateFromSessionManager()
    {
        var session = _sessionManager.CurrentSession;
        if (session != null)
        {
            UpdateFromSession(session, session.Status);
        }
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        CompletedSessionsToday = _sessionManager.TodayStatistics.FocusSessionsCompleted;
        CycleProgress = $"{_sessionManager.Cycle.CompletedFocusSessions}/{PomodoroCycle.FocusSessionsPerCycle}";
    }

    private void ResetToDefault()
    {
        TimeRemaining = TimeSpan.FromMinutes(25);
        SessionLabel = "Focus";
        CurrentSessionType = SessionType.Focus;
        IsRunning = false;
        IsPaused = false;
        IsSessionComplete = false;
    }

    private static string GetSessionLabel(SessionType type) => type switch
    {
        SessionType.Focus => "Focus",
        SessionType.ShortBreak => "Short Break",
        SessionType.LongBreak => "Long Break",
        _ => "Focus"
    };
}
