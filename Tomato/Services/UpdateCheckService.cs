using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tomato.Services;

/// <summary>
/// Checks GitHub for application updates by comparing against the latest release.
/// </summary>
public sealed class UpdateCheckService : IUpdateCheckService
{
    private const string GitHubApiUrl = "https://api.github.com/repos/dracan/tomato/releases/latest";
    private const string UserAgent = "Tomato-App";

    private readonly HttpClient _httpClient;
    private bool _disposed;

    public UpdateCheckService()
        : this(new HttpClient())
    {
    }

    public UpdateCheckService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    }

    public async Task<UpdateCheckResult?> CheckForUpdateAsync()
    {
        try
        {
            var currentVersion = GetCurrentVersion();
            if (currentVersion == null)
            {
                return null;
            }

            using var response = await _httpClient.GetAsync(GitHubApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json);

            if (release == null || string.IsNullOrEmpty(release.TagName))
            {
                return null;
            }

            var latestVersion = ParseVersion(release.TagName);
            if (latestVersion == null)
            {
                return null;
            }

            // Only return result if latest version is newer
            if (latestVersion > currentVersion)
            {
                return new UpdateCheckResult(
                    currentVersion,
                    latestVersion,
                    release.HtmlUrl ?? $"https://github.com/dracan/tomato/releases/tag/{release.TagName}");
            }

            return null;
        }
        catch
        {
            // Silent failure - don't block app startup
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _httpClient.Dispose();
        _disposed = true;
    }

    private static Version? GetCurrentVersion()
    {
        // MinVer sets the semantic version in AssemblyInformationalVersionAttribute
        var infoVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrEmpty(infoVersion))
        {
            return null;
        }

        // MinVer may append "+commitHash" metadata, strip it for version comparison
        var plusIndex = infoVersion.IndexOf('+');
        if (plusIndex >= 0)
        {
            infoVersion = infoVersion.Substring(0, plusIndex);
        }

        // Also strip any prerelease suffix like "-alpha.0.1"
        var dashIndex = infoVersion.IndexOf('-');
        if (dashIndex >= 0)
        {
            infoVersion = infoVersion.Substring(0, dashIndex);
        }

        return Version.TryParse(infoVersion, out var version) ? version : null;
    }

    internal static Version? ParseVersion(string tagName)
    {
        // Strip leading 'v' if present (e.g., "v1.1.0" -> "1.1.0", "v24" -> "24")
        var versionString = tagName.TrimStart('v', 'V');

        // Handle single-number versions (e.g., "24" -> "24.0")
        // Version.TryParse requires at least major.minor format
        if (int.TryParse(versionString, out var majorOnly))
        {
            return new Version(majorOnly, 0);
        }

        if (Version.TryParse(versionString, out var version))
        {
            return version;
        }

        return null;
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }
    }
}
