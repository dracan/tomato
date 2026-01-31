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
    /// Gets the configured duration for this session.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets the time remaining in the session.
    /// </summary>
    public TimeSpan TimeRemaining { get; private set; }

    /// <summary>
    /// Gets or sets the time when this session was started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the time when this session was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    private Session(SessionType type, TimeSpan duration)
    {
        Type = type;
        Duration = duration;
        TimeRemaining = duration;
        Status = SessionStatus.NotStarted;
    }

    /// <summary>
    /// Creates a new Focus session with the default 25-minute duration.
    /// </summary>
    public static Session CreateFocus()
        => new(SessionType.Focus, TimeSpan.FromMinutes(25));

    /// <summary>
    /// Creates a new Focus session with a custom duration.
    /// </summary>
    public static Session CreateFocus(TimeSpan duration)
        => new(SessionType.Focus, duration);

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
        TimeRemaining = TimeRemaining - elapsed;
        if (TimeRemaining < TimeSpan.Zero)
        {
            TimeRemaining = TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Sets the time remaining directly (used for restoration from saved state).
    /// </summary>
    /// <param name="remaining">The time remaining.</param>
    public void SetTimeRemaining(TimeSpan remaining)
    {
        TimeRemaining = remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    /// <summary>
    /// Gets whether this session has completed (time remaining is zero or less).
    /// </summary>
    public bool IsComplete => TimeRemaining <= TimeSpan.Zero;
}
