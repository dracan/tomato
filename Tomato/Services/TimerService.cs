using System.Diagnostics;
using System.Windows.Threading;

namespace Tomato.Services;

/// <summary>
/// Abstraction for interval-based timers to allow testability.
/// </summary>
public interface IIntervalTimer : IDisposable
{
    /// <summary>
    /// Occurs when the timer interval elapses.
    /// </summary>
    event EventHandler? Elapsed;

    /// <summary>
    /// Starts the timer.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the timer.
    /// </summary>
    void Stop();
}

/// <summary>
/// DispatcherTimer-based interval timer for WPF UI thread execution.
/// </summary>
internal sealed class DispatcherIntervalTimer : IIntervalTimer
{
    private readonly DispatcherTimer _timer;

    public event EventHandler? Elapsed;

    public DispatcherIntervalTimer()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _timer.Tick += (s, e) => Elapsed?.Invoke(this, EventArgs.Empty);
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();
    public void Dispose() => _timer.Stop();
}

/// <summary>
/// System.Timers.Timer-based interval timer for testing (fires on thread pool).
/// </summary>
public sealed class ThreadPoolIntervalTimer : IIntervalTimer
{
    private readonly System.Timers.Timer _timer;

    public event EventHandler? Elapsed;

    public ThreadPoolIntervalTimer()
    {
        _timer = new System.Timers.Timer(250)
        {
            AutoReset = true
        };
        _timer.Elapsed += (s, e) => Elapsed?.Invoke(this, EventArgs.Empty);
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}

/// <summary>
/// High-accuracy timer service using DispatcherTimer with Stopwatch for drift correction.
/// DispatcherTimer fires on the UI thread, ensuring smooth display updates.
/// </summary>
public sealed class TimerService : ITimerService
{
    private readonly IIntervalTimer _timer;
    private readonly Stopwatch _stopwatch;
    private TimeSpan _duration;
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler<TimerTickEventArgs>? Tick;

    /// <inheritdoc />
    public event EventHandler? Completed;

    /// <inheritdoc />
    public bool IsRunning { get; private set; }

    /// <inheritdoc />
    public TimeSpan Remaining { get; private set; }

    /// <summary>
    /// Creates a new TimerService using DispatcherTimer for UI thread execution.
    /// </summary>
    public TimerService() : this(new DispatcherIntervalTimer())
    {
    }

    /// <summary>
    /// Creates a new TimerService with a custom interval timer (for testing).
    /// </summary>
    /// <param name="timer">The interval timer to use.</param>
    public TimerService(IIntervalTimer timer)
    {
        _timer = timer;
        _timer.Elapsed += OnTimerTick;
        _stopwatch = new Stopwatch();
    }

    /// <inheritdoc />
    public void Start(TimeSpan duration)
    {
        _duration = duration;
        Remaining = duration;
        _stopwatch.Restart();
        _timer.Start();
        IsRunning = true;
    }

    /// <inheritdoc />
    public void Resume(TimeSpan remaining)
    {
        _duration = remaining;
        Remaining = remaining;
        _stopwatch.Restart();
        _timer.Start();
        IsRunning = true;
    }

    /// <inheritdoc />
    public void Pause()
    {
        _timer.Stop();
        _stopwatch.Stop();
        // Update remaining time based on actual elapsed time
        Remaining = _duration - _stopwatch.Elapsed;
        if (Remaining < TimeSpan.Zero)
        {
            Remaining = TimeSpan.Zero;
        }
        IsRunning = false;
    }

    /// <inheritdoc />
    public void Stop()
    {
        _timer.Stop();
        _stopwatch.Stop();
        _stopwatch.Reset();
        Remaining = TimeSpan.Zero;
        IsRunning = false;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var elapsed = _stopwatch.Elapsed;
        Remaining = _duration - elapsed;

        if (Remaining <= TimeSpan.Zero)
        {
            Remaining = TimeSpan.Zero;
            Stop();
            Completed?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Tick?.Invoke(this, new TimerTickEventArgs(elapsed, Remaining));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Elapsed -= OnTimerTick;
        _timer.Dispose();
    }
}
