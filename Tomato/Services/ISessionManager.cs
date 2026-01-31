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
    /// Occurs when the session state changes.
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
    /// Starts a new focus session.
    /// </summary>
    void StartFocus();

    /// <summary>
    /// Starts a new focus session with the specified duration.
    /// </summary>
    /// <param name="duration">The duration for the focus session.</param>
    void StartFocus(TimeSpan duration);

    /// <summary>
    /// Starts a break session (short or long based on cycle).
    /// </summary>
    void StartBreak();

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
    /// Skips the remaining time in the current session.
    /// </summary>
    void Skip();
}
