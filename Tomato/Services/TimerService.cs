using System.Diagnostics;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Tomato.Services;

/// <summary>
/// High-accuracy timer service using System.Timers.Timer with Stopwatch for drift correction.
/// </summary>
public sealed class TimerService : ITimerService
{
    private readonly Timer _timer;
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

    public TimerService()
    {
        _timer = new Timer(1000); // 1 second interval
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
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

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
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
        _timer.Elapsed -= OnTimerElapsed;
        _timer.Dispose();
    }
}
