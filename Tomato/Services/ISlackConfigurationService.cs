namespace Tomato.Services;

/// <summary>
/// Manages Slack token configuration with encrypted storage.
/// </summary>
public interface ISlackConfigurationService
{
    /// <summary>
    /// Loads the saved Slack token from encrypted storage.
    /// </summary>
    /// <returns>The decrypted token, or null if not configured.</returns>
    string? LoadToken();

    /// <summary>
    /// Saves a Slack token to encrypted storage.
    /// </summary>
    /// <param name="token">The token to save.</param>
    Task SaveTokenAsync(string token);

    /// <summary>
    /// Clears the saved Slack token.
    /// </summary>
    Task ClearTokenAsync();
}
