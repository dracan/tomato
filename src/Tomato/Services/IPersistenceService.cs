using Tomato.Models;

namespace Tomato.Services;

/// <summary>
/// Handles persistence of application state to disk.
/// State file location: %LOCALAPPDATA%\Tomato\state.json
/// </summary>
public interface IPersistenceService
{
    /// <summary>
    /// Saves the current application state.
    /// </summary>
    /// <param name="state">The state to save.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SaveAsync(AppState state);

    /// <summary>
    /// Loads the saved application state.
    /// </summary>
    /// <returns>The loaded state, or null if no state exists or loading fails.</returns>
    Task<AppState?> LoadAsync();

    /// <summary>
    /// Deletes the saved state file.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    Task ClearAsync();

    /// <summary>
    /// Gets the path to the state file.
    /// </summary>
    string StateFilePath { get; }
}
