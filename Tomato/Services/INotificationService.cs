using Tomato.Models;

namespace Tomato.Services;

/// <summary>
/// Provides audio and visual notifications.
/// </summary>
public interface INotificationService : IDisposable
{
    /// <summary>
    /// Plays the notification sound for session completion.
    /// </summary>
    /// <param name="sessionType">The type of session that completed.</param>
    Task PlayCompletionSoundAsync(SessionType sessionType);

    /// <summary>
    /// Shows a Windows notification toast.
    /// </summary>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    void ShowNotification(string title, string message);

    /// <summary>
    /// Gets or sets whether sound notifications are enabled.
    /// </summary>
    bool IsSoundEnabled { get; set; }
}
