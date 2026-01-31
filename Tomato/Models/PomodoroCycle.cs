namespace Tomato.Models;

/// <summary>
/// Tracks progress through a cycle of 4 focus sessions with breaks.
/// A complete cycle: Focus → Short Break → Focus → Short Break → Focus → Short Break → Focus → Long Break
/// </summary>
public sealed class PomodoroCycle
{
    /// <summary>
    /// Number of focus sessions in a complete cycle before a long break.
    /// </summary>
    public const int FocusSessionsPerCycle = 4;

    /// <summary>
    /// Gets the number of focus sessions completed in the current cycle (0-4).
    /// </summary>
    public int CompletedFocusSessions { get; private set; }

    /// <summary>
    /// Gets whether the current cycle is complete (4 focus sessions done).
    /// </summary>
    public bool IsCycleComplete => CompletedFocusSessions >= FocusSessionsPerCycle;

    /// <summary>
    /// Gets the type of break that should follow the next completed focus session.
    /// </summary>
    public SessionType NextBreakType => IsCycleComplete ? SessionType.LongBreak : SessionType.ShortBreak;

    /// <summary>
    /// Increments the completed focus session count.
    /// </summary>
    public void IncrementFocusCount()
    {
        CompletedFocusSessions++;
    }

    /// <summary>
    /// Resets the cycle after a long break.
    /// </summary>
    public void Reset()
    {
        CompletedFocusSessions = 0;
    }

    /// <summary>
    /// Sets the completed focus sessions count (used for restoration from saved state).
    /// </summary>
    /// <param name="count">The number of completed focus sessions.</param>
    public void SetCompletedFocusSessions(int count)
    {
        CompletedFocusSessions = Math.Clamp(count, 0, FocusSessionsPerCycle);
    }
}
