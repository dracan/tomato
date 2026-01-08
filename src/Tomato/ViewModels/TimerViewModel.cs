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

    public TimerViewModel(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;

        // Subscribe to session manager events
        _sessionManager.TimerTick += OnTimerTick;
        _sessionManager.SessionStateChanged += OnSessionStateChanged;

        // Initialize from current state
        UpdateFromSessionManager();
    }

    [RelayCommand]
    private void StartFocus()
    {
        _sessionManager.StartFocus();
    }

    [RelayCommand]
    private void StartBreak()
    {
        _sessionManager.StartBreak();
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
