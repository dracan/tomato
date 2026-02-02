namespace Tomato.Models;

/// <summary>
/// Represents a completed focus session with its goal and results.
/// </summary>
public sealed class SessionRecord
{
    /// <summary>
    /// Gets or sets the goal set for this session.
    /// </summary>
    public string? Goal { get; set; }

    /// <summary>
    /// Gets or sets the results recorded after the session completed.
    /// </summary>
    public string? Results { get; set; }

    /// <summary>
    /// Gets or sets the optional 1-5 star rating for this session.
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// Gets the duration of this session.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets when this session started.
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// Gets when this session completed.
    /// </summary>
    public DateTime CompletedAt { get; init; }
}
