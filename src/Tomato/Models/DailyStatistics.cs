namespace Tomato.Models;

/// <summary>
/// Aggregates daily productivity statistics.
/// </summary>
public sealed class DailyStatistics
{
    /// <summary>
    /// Gets or sets the date these statistics are for.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Gets or sets the total number of focus sessions completed today.
    /// </summary>
    public int FocusSessionsCompleted { get; set; }

    /// <summary>
    /// Gets or sets the total focus time accumulated today.
    /// </summary>
    public TimeSpan TotalFocusTime { get; set; }

    /// <summary>
    /// Gets or sets the total break time taken today.
    /// </summary>
    public TimeSpan TotalBreakTime { get; set; }

    /// <summary>
    /// Gets or sets the number of full cycles completed today.
    /// </summary>
    public int CyclesCompleted { get; set; }

    /// <summary>
    /// Creates a new DailyStatistics instance for the specified date.
    /// </summary>
    /// <param name="date">The date for these statistics.</param>
    public static DailyStatistics Create(DateOnly date) => new()
    {
        Date = date,
        FocusSessionsCompleted = 0,
        TotalFocusTime = TimeSpan.Zero,
        TotalBreakTime = TimeSpan.Zero,
        CyclesCompleted = 0
    };

    /// <summary>
    /// Records a completed focus session.
    /// </summary>
    /// <param name="duration">The duration of the completed focus session.</param>
    public void RecordFocusSession(TimeSpan duration)
    {
        FocusSessionsCompleted++;
        TotalFocusTime += duration;
    }

    /// <summary>
    /// Records a completed break session.
    /// </summary>
    /// <param name="duration">The duration of the completed break session.</param>
    public void RecordBreakSession(TimeSpan duration)
    {
        TotalBreakTime += duration;
    }

    /// <summary>
    /// Records completion of a full pomodoro cycle.
    /// </summary>
    public void RecordCycleCompleted()
    {
        CyclesCompleted++;
    }
}
