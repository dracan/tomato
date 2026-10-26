namespace Tomato.Models;

/// <summary>
/// Represents a single Pomodoro session (focus or break).
/// </summary>
public sealed class Session
{
    /// <summary>
    /// Gets the type of this session.
    /// </summary>
    public SessionType Type { get; }

    /// <summary>
    /// Gets or sets the current status of this session.
    /// </summary>
    public SessionStatus Status { get; set; }

    /// <summary>
    /// Gets the configured duration for this session. For a focus session this is its Estimate.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets the time remaining in the session. It goes below zero once a session
    /// that allows Overtime runs past its Estimate.
    /// </summary>
    public TimeSpan TimeRemaining { get; private set; }

    /// <summary>
    /// Gets whether this session keeps running past its Estimate (Overtime) instead of
    /// ending when the time runs out. Decided when the session starts.
    /// </summary>
    public bool AllowsOvertime { get; }

    /// <summary>
    /// Gets how long the session has actually run, not counting pauses.
    /// </summary>
    public TimeSpan TimeSpent => Duration - TimeRemaining;

    /// <summary>
    /// Gets whether the session is still going after its Estimate has run out.
    /// </summary>
    public bool IsInOvertime =>
        AllowsOvertime &&
        TimeRemaining <= TimeSpan.Zero &&
        Status is SessionStatus.Running or SessionStatus.Paused;

    /// <summary>
    /// Gets or sets the time when this session was started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the time when this session was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets the goal for this session (focus sessions only).
    /// </summary>
    public string? Goal { get; }

    private Session(SessionType type, TimeSpan duration, string? goal = null, bool allowsOvertime = false)
    {
        Type = type;
        Duration = duration;
        TimeRemaining = duration;
        Status = SessionStatus.NotStarted;
        Goal = goal;
        AllowsOvertime = allowsOvertime;
    }

    /// <summary>
    /// Creates a new Focus session with the default 25-minute duration.
    /// </summary>
    /// <param name="goal">Optional goal for this focus session.</param>
    public static Session CreateFocus(string? goal = null)
        => new(SessionType.Focus, TimeSpan.FromMinutes(25), goal);

    /// <summary>
    /// Creates a new Focus session with a custom duration.
    /// </summary>
    /// <param name="duration">The duration for this focus session.</param>
    /// <param name="goal">Optional goal for this focus session.</param>
    /// <param name="allowsOvertime">Whether the session keeps running past its Estimate.</param>
    public static Session CreateFocus(TimeSpan duration, string? goal = null, bool allowsOvertime = false)
        => new(SessionType.Focus, duration, goal, allowsOvertime);

    /// <summary>
    /// Creates a new Short Break session with the default 5-minute duration.
    /// </summary>
    public static Session CreateShortBreak()
        => new(SessionType.ShortBreak, TimeSpan.FromMinutes(5));

    /// <summary>
    /// Creates a new Short Break session with a custom duration.
    /// </summary>
    public static Session CreateShortBreak(TimeSpan duration)
        => new(SessionType.ShortBreak, duration);

    /// <summary>
    /// Creates a new Long Break session with the default 15-minute duration.
    /// </summary>
    public static Session CreateLongBreak()
        => new(SessionType.LongBreak, TimeSpan.FromMinutes(15));

    /// <summary>
    /// Creates a new Long Break session with a custom duration.
    /// </summary>
    public static Session CreateLongBreak(TimeSpan duration)
        => new(SessionType.LongBreak, duration);

    /// <summary>
    /// Updates the time remaining in this session.
    /// </summary>
    /// <param name="elapsed">The time that has elapsed since last update.</param>
    public void UpdateTimeRemaining(TimeSpan elapsed)
    {
        SetTimeRemaining(TimeRemaining - elapsed);
    }

    /// <summary>
    /// Sets the time remaining directly (used for restoration from saved state).
    /// </summary>
    /// <param name="remaining">The time remaining.</param>
    public void SetTimeRemaining(TimeSpan remaining)
    {
        TimeRemaining = remaining < TimeSpan.Zero && !AllowsOvertime ? TimeSpan.Zero : remaining;
    }

    /// <summary>
    /// Gets whether this session has completed (time remaining is zero or less).
    /// </summary>
    public bool IsComplete => TimeRemaining <= TimeSpan.Zero;
}
