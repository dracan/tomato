namespace Tomato.Services;

/// <summary>
/// Result of an update check containing version information and release URL.
/// </summary>
/// <param name="CurrentVersion">The currently installed version.</param>
/// <param name="LatestVersion">The latest available version.</param>
/// <param name="ReleaseUrl">URL to the GitHub release page.</param>
public record UpdateCheckResult(
    Version CurrentVersion,
    Version LatestVersion,
    string ReleaseUrl);

/// <summary>
/// Service for checking if application updates are available.
/// </summary>
public interface IUpdateCheckService : IDisposable
{
    /// <summary>
    /// Checks GitHub for the latest release and compares against the current version.
    /// </summary>
    /// <returns>
    /// An <see cref="UpdateCheckResult"/> if a newer version is available; otherwise, null.
    /// Returns null on network errors or if the current version is up to date.
    /// </returns>
    Task<UpdateCheckResult?> CheckForUpdateAsync();
}
