using Tomato.Models;

namespace Tomato.Services;

/// <summary>
/// Event arguments for session state changes.
/// </summary>
public class SessionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the session that changed.
    /// </summary>
    public Session Session { get; }

    /// <summary>
    /// Gets the previous status.
    /// </summary>
    public SessionStatus PreviousStatus { get; }

    /// <summary>
    /// Gets the new status.
    /// </summary>
    public SessionStatus NewStatus { get; }

    public SessionStateChangedEventArgs(Session session, SessionStatus previousStatus, SessionStatus newStatus)
    {
        Session = session;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
    }
}

/// <summary>
/// Manages the lifecycle of Pomodoro sessions and cycles.
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Occurs when the session state changes. Also raised, from Running to Running,
    /// when a focus session goes into Overtime.
    /// </summary>
    event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;

    /// <summary>
    /// Occurs every second with updated time remaining.
    /// </summary>
    event EventHandler<TimerTickEventArgs>? TimerTick;

    /// <summary>
    /// Gets the current session, if any.
    /// </summary>
    Session? CurrentSession { get; }

    /// <summary>
    /// Gets the current cycle progress.
    /// </summary>
    PomodoroCycle Cycle { get; }

    /// <summary>
    /// Gets today's statistics.
    /// </summary>
    DailyStatistics TodayStatistics { get; }

    /// <summary>
    /// Gets the historical daily statistics.
    /// </summary>
    IReadOnlyList<DailyStatistics> StatisticsHistory { get; }

    /// <summary>
    /// Gets or sets whether new focus sessions keep running past their Estimate (Overtime)
    /// until marked done. A change applies from the next focus session. Saved across restarts.
    /// </summary>
    bool OvertimeEnabled { get; set; }

    /// <summary>
    /// Starts a new focus session.
    /// </summary>
    void StartFocus();

    /// <summary>
    /// Starts a new focus session with the specified goal.
    /// </summary>
    /// <param name="goal">The goal for this focus session.</param>
    void StartFocus(string? goal);

    /// <summary>
    /// Starts a new focus session with the specified duration.
    /// </summary>
    /// <param name="duration">The duration for the focus session.</param>
    void StartFocus(TimeSpan duration);

    /// <summary>
    /// Starts a new focus session with the specified duration and goal.
    /// </summary>
    /// <param name="duration">The duration for the focus session.</param>
    /// <param name="goal">The goal for this focus session.</param>
    void StartFocus(TimeSpan duration, string? goal);

    /// <summary>
    /// Starts a short break session (5 minutes).
    /// </summary>
    void StartBreak();

    /// <summary>
    /// Starts a short break session with the specified duration.
    /// </summary>
    /// <param name="duration">The duration for the break session.</param>
    void StartBreak(TimeSpan duration);

    /// <summary>
    /// Starts a long break session (15 minutes).
    /// </summary>
    void StartLongBreak();

    /// <summary>
    /// Pauses the current session.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes the current paused session.
    /// </summary>
    void Resume();

    /// <summary>
    /// Cancels the current session.
    /// </summary>
    void Cancel();

    /// <summary>
    /// Skips the remaining time in the current session. For a focus session this is
    /// the same as <see cref="MarkDone"/>.
    /// </summary>
    void Skip();

    /// <summary>
    /// Ends the current focus session now and counts it as completed, recording its
    /// Time Spent. Does nothing during a break or when no focus session is under way.
    /// </summary>
    void MarkDone();

    /// <summary>
    /// Records the results of the most recently completed session.
    /// </summary>
    /// <param name="results">The results text to record.</param>
    /// <param name="rating">The optional 1-5 star rating.</param>
    void RecordSessionResults(string? results, int? rating);

    /// <summary>
    /// Restarts the current session from the beginning, clearing any Overtime.
    /// The session type, duration, goal and whether it allows Overtime are preserved.
    /// Only valid when a session is running or paused.
    /// </summary>
    void Restart();

    /// <summary>
    /// Pauses a running session and saves state, waiting for the save to finish,
    /// so the session can be resumed after the app restarts.
    /// </summary>
    void PrepareForShutdown();

    /// <summary>
    /// Adds a supplemental activity (non-Pomodoro work like meetings) to today's statistics.
    /// </summary>
    /// <param name="description">A description of the activity.</param>
    void AddSupplementalActivity(string description);
}
