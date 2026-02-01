namespace Tomato.Services;

/// <summary>
/// Manages Slack status and Do Not Disturb during focus sessions.
/// </summary>
public interface ISlackService : IDisposable
{
    /// <summary>
    /// Gets whether a Slack token is configured.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Gets or sets whether Slack integration is enabled.
    /// When disabled, no API calls will be made.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Sets the Slack status to focus mode with DND enabled.
    /// </summary>
    Task SetFocusStatusAsync();

    /// <summary>
    /// Clears the Slack status and disables DND.
    /// </summary>
    Task ClearStatusAsync();

    /// <summary>
    /// Configures the service with a new token.
    /// </summary>
    /// <param name="token">The Slack API token.</param>
    Task ConfigureAsync(string token);

    /// <summary>
    /// Tests the connection to Slack using the configured token.
    /// </summary>
    /// <returns>True if the connection is successful, false otherwise.</returns>
    Task<bool> TestConnectionAsync();
}
