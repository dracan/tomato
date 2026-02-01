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

    /// <summary>
    /// Returns the goal text for tooltip display, or null if no tooltip should be shown.
    /// Tooltip is only shown when a session is active (running or paused) and has a non-empty goal.
    /// </summary>
    public string? GoalTooltip =>
        (IsRunning || IsPaused) && !string.IsNullOrWhiteSpace(CurrentGoal)
            ? CurrentGoal
            : null;

    partial void OnIsRunningChanged(bool value) => OnPropertyChanged(nameof(GoalTooltip));
    partial void OnIsPausedChanged(bool value) => OnPropertyChanged(nameof(GoalTooltip));
    partial void OnCurrentGoalChanged(string? value) => OnPropertyChanged(nameof(GoalTooltip));

    public TimerViewModel(ISessionManager sessionManager, IDialogService dialogService)
    {
        _sessionManager = sessionManager;
        _dialogService = dialogService;

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
    private void Cancel()
    {
        _sessionManager.Cancel();
        ResetToDefault();
    }

    [RelayCommand]
    private void Skip()
    {
        _sessionManager.Skip();
    }

    private void OnTimerTick(object? sender, TimerTickEventArgs e)
    {
        TimeRemaining = e.Remaining;
    }

    private void OnSessionStateChanged(object? sender, SessionStateChangedEventArgs e)
    {
        UpdateFromSession(e.Session, e.NewStatus);
        UpdateStatistics();
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
