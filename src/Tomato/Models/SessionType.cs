namespace Tomato.Models;

/// <summary>
/// Represents the type of Pomodoro session.
/// </summary>
public enum SessionType
{
    /// <summary>
    /// A 25-minute focus/work session.
    /// </summary>
    Focus,

    /// <summary>
    /// A 5-minute short break between focus sessions.
    /// </summary>
    ShortBreak,

    /// <summary>
    /// A 15-minute long break after completing 4 focus sessions.
    /// </summary>
    LongBreak
}
