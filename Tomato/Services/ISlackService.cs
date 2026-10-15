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
    Task SetFocusStatusAsync(TimeSpan duration);

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
    /// Tests whether the given token works, without saving it or switching to it.
    /// </summary>
    /// <returns>Whether the token works, and a message to show if it doesn't.</returns>
    /// <param name="token">The token to test.</param>
    Task<ConnectionTestResult> TestConnectionAsync(string token);
}

/// <summary>
/// The outcome of testing an integration's credentials.
/// </summary>
/// <param name="Success">Whether the credentials work.</param>
/// <param name="Message">What went wrong, when they don't.</param>
public sealed record ConnectionTestResult(bool Success, string? Message)
{
    public static ConnectionTestResult Succeeded { get; } = new(true, null);

    public static ConnectionTestResult Failed(string message) => new(false, message);
}
