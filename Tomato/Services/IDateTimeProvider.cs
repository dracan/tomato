namespace Tomato.Services;

/// <summary>
/// Provides abstraction over DateTime for testability.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current local date and time.
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Gets the current local date.
    /// </summary>
    DateOnly Today { get; }
}
