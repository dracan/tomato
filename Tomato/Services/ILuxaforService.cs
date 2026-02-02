namespace Tomato.Services;

/// <summary>
/// Manages Luxafor LED light during focus sessions.
/// </summary>
public interface ILuxaforService : IDisposable
{
    /// <summary>
    /// Gets whether a Luxafor user ID is configured.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Gets or sets whether Luxafor integration is enabled.
    /// When disabled, no API calls will be made.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Sets the Luxafor LED to red (focus mode).
    /// </summary>
    Task SetFocusColorAsync();

    /// <summary>
    /// Turns off the Luxafor LED.
    /// </summary>
    Task TurnOffAsync();

    /// <summary>
    /// Configures the service with a new user ID.
    /// </summary>
    /// <param name="userId">The Luxafor webhook user ID.</param>
    Task ConfigureAsync(string userId);

    /// <summary>
    /// Tests the connection to Luxafor using the configured user ID.
    /// </summary>
    /// <returns>True if the connection is successful, false otherwise.</returns>
    Task<bool> TestConnectionAsync();
}
