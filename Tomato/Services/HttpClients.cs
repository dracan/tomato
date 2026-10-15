using System.Net.Http;

namespace Tomato.Services;

/// <summary>
/// Creates the HttpClients used for Slack, Luxafor and update checks.
/// </summary>
internal static class HttpClients
{
    /// <summary>
    /// Short enough that a dead network doesn't leave Test Connection waiting,
    /// or hold up clearing the focus status when the app exits.
    /// </summary>
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    public static HttpClient Create() => new() { Timeout = RequestTimeout };
}
