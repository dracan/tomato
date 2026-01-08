using System.Text.Json.Serialization;

namespace Tomato.Models;

/// <summary>
/// Represents the complete application state for persistence.
/// Saved to %LOCALAPPDATA%\Tomato\state.json
/// </summary>
public sealed class AppState
{
    /// <summary>
    /// Gets or sets the current session, if any.
    /// </summary>
    public SessionState? CurrentSession { get; set; }

    /// <summary>
    /// Gets or sets the current cycle progress.
    /// </summary>
    public CycleState Cycle { get; set; } = new();

    /// <summary>
    /// Gets or sets the daily statistics.
    /// </summary>
    public DailyStatisticsState? TodayStatistics { get; set; }

    /// <summary>
    /// Gets or sets when this state was last saved.
    /// </summary>
    public DateTime LastSavedAt { get; set; }

    /// <summary>
    /// Represents a serializable session state.
    /// </summary>
    public sealed class SessionState
    {
        public SessionType Type { get; set; }
        public SessionStatus Status { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// Represents a serializable cycle state.
    /// </summary>
    public sealed class CycleState
    {
        public int CompletedFocusSessions { get; set; }
    }

    /// <summary>
    /// Represents serializable daily statistics.
    /// </summary>
    public sealed class DailyStatisticsState
    {
        public DateOnly Date { get; set; }
        public int FocusSessionsCompleted { get; set; }
        public TimeSpan TotalFocusTime { get; set; }
        public TimeSpan TotalBreakTime { get; set; }
        public int CyclesCompleted { get; set; }
    }

    /// <summary>
    /// Creates an AppState from the current application objects.
    /// </summary>
    public static AppState FromCurrentState(
        Session? currentSession,
        PomodoroCycle cycle,
        DailyStatistics? statistics,
        DateTime savedAt)
    {
        var state = new AppState
        {
            LastSavedAt = savedAt,
            Cycle = new CycleState
            {
                CompletedFocusSessions = cycle.CompletedFocusSessions
            }
        };

        if (currentSession != null)
        {
            state.CurrentSession = new SessionState
            {
                Type = currentSession.Type,
                Status = currentSession.Status,
                Duration = currentSession.Duration,
                TimeRemaining = currentSession.TimeRemaining,
                StartedAt = currentSession.StartedAt,
                CompletedAt = currentSession.CompletedAt
            };
        }

        if (statistics != null)
        {
            state.TodayStatistics = new DailyStatisticsState
            {
                Date = statistics.Date,
                FocusSessionsCompleted = statistics.FocusSessionsCompleted,
                TotalFocusTime = statistics.TotalFocusTime,
                TotalBreakTime = statistics.TotalBreakTime,
                CyclesCompleted = statistics.CyclesCompleted
            };
        }

        return state;
    }

    /// <summary>
    /// Restores a Session from the saved state.
    /// </summary>
    public Session? RestoreSession()
    {
        if (CurrentSession == null) return null;

        var session = CurrentSession.Type switch
        {
            SessionType.Focus => Session.CreateFocus(CurrentSession.Duration),
            SessionType.ShortBreak => Session.CreateShortBreak(CurrentSession.Duration),
            SessionType.LongBreak => Session.CreateLongBreak(CurrentSession.Duration),
            _ => Session.CreateFocus(CurrentSession.Duration)
        };

        session.Status = CurrentSession.Status;
        session.SetTimeRemaining(CurrentSession.TimeRemaining);
        session.StartedAt = CurrentSession.StartedAt;
        session.CompletedAt = CurrentSession.CompletedAt;

        return session;
    }

    /// <summary>
    /// Restores a PomodoroCycle from the saved state.
    /// </summary>
    public PomodoroCycle RestoreCycle()
    {
        var cycle = new PomodoroCycle();
        cycle.SetCompletedFocusSessions(Cycle.CompletedFocusSessions);
        return cycle;
    }

    /// <summary>
    /// Restores DailyStatistics from the saved state.
    /// </summary>
    public DailyStatistics? RestoreStatistics()
    {
        if (TodayStatistics == null) return null;

        return new DailyStatistics
        {
            Date = TodayStatistics.Date,
            FocusSessionsCompleted = TodayStatistics.FocusSessionsCompleted,
            TotalFocusTime = TodayStatistics.TotalFocusTime,
            TotalBreakTime = TodayStatistics.TotalBreakTime,
            CyclesCompleted = TodayStatistics.CyclesCompleted
        };
    }
}
