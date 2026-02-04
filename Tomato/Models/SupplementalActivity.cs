namespace Tomato.Models;

/// <summary>
/// Represents a non-Pomodoro activity recorded for the day (meetings, etc.).
/// </summary>
public sealed class SupplementalActivity
{
    /// <summary>
    /// Gets or sets a description of the activity.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
