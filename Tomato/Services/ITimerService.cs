namespace Tomato.Services;

/// <summary>
/// Event arguments for timer tick events.
/// </summary>
public class TimerTickEventArgs : EventArgs
{
    /// <summary>
    /// Gets the time elapsed since the timer started.
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    /// Gets the time remaining in the current session.
    /// </summary>
    public TimeSpan Remaining { get; }

    public TimerTickEventArgs(TimeSpan elapsed, TimeSpan remaining)
    {
        Elapsed = elapsed;
        Remaining = remaining;
    }
}

/// <summary>
/// Provides high-accuracy timer functionality for Pomodoro sessions.
/// Uses DispatcherTimer for UI thread execution with Stopwatch for drift correction.
/// </summary>
public interface ITimerService : IDisposable
{
    /// <summary>
    /// Occurs every second while the timer is running.
    /// </summary>
    event EventHandler<TimerTickEventArgs>? Tick;

    /// <summary>
    /// Occurs when the timer completes (reaches zero).
    /// </summary>
    event EventHandler? Completed;

    /// <summary>
    /// Gets whether the timer is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the time remaining.
    /// </summary>
    TimeSpan Remaining { get; }

    /// <summary>
    /// Starts the timer with the specified duration.
    /// </summary>
    /// <param name="duration">The duration to count down from.</param>
    void Start(TimeSpan duration);

    /// <summary>
    /// Resumes the timer with the specified remaining time.
    /// </summary>
    /// <param name="remaining">The time remaining to continue from.</param>
    void Resume(TimeSpan remaining);

    /// <summary>
    /// Pauses the timer.
    /// </summary>
    void Pause();

    /// <summary>
    /// Stops and resets the timer.
    /// </summary>
    void Stop();
}
