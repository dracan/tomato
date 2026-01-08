namespace Tomato.Models;

/// <summary>
/// Represents the current status of a session.
/// </summary>
public enum SessionStatus
{
    /// <summary>
    /// Session has not been started yet.
    /// </summary>
    NotStarted,

    /// <summary>
    /// Session is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Session has been paused by the user.
    /// </summary>
    Paused,

    /// <summary>
    /// Session has been completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Session was cancelled before completion.
    /// </summary>
    Cancelled
}
