namespace Tomato.Services;

/// <summary>
/// Manages Luxafor user ID configuration with encrypted storage.
/// </summary>
public interface ILuxaforConfigurationService
{
    /// <summary>
    /// Loads the saved Luxafor user ID from encrypted storage.
    /// </summary>
    /// <returns>The decrypted user ID, or null if not configured.</returns>
    string? LoadUserId();

    /// <summary>
    /// Saves a Luxafor user ID to encrypted storage.
    /// </summary>
    /// <param name="userId">The user ID to save.</param>
    Task SaveUserIdAsync(string userId);

    /// <summary>
    /// Clears the saved Luxafor user ID.
    /// </summary>
    Task ClearUserIdAsync();
}
