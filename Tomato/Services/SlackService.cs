using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tomato.Models;

namespace Tomato.Services;

/// <summary>
/// Manages Slack status and Do Not Disturb during focus sessions.
/// Subscribes to ISessionManager events to automatically update status.
/// </summary>
public sealed class SlackService : ISlackService
{
    private const string SlackApiBaseUrl = "https://slack.com/api";
    private static readonly string[] RequiredScopes = ["users.profile:write", "dnd:write"];
    private static readonly TimeSpan ShutdownWaitTimeout = TimeSpan.FromSeconds(3);

    private readonly ISlackConfigurationService _configService;
    private readonly ISessionManager _sessionManager;
    private readonly HttpClient _httpClient;
    private readonly ErrorLog? _errorLog;

    private readonly object _updateLock = new();
    private Task _pendingUpdate = Task.CompletedTask;

    private string? _token;
    private bool _isEnabled = true;
    private bool _disposed;

    public SlackService(
        ISlackConfigurationService configService,
        ISessionManager sessionManager)
        : this(configService, sessionManager, HttpClients.Create(), new ErrorLog())
    {
    }

    public SlackService(
        ISlackConfigurationService configService,
        ISessionManager sessionManager,
        HttpClient httpClient,
        ErrorLog? errorLog = null)
    {
        _configService = configService;
        _sessionManager = sessionManager;
        _httpClient = httpClient;
        _errorLog = errorLog;

        // Load token from configuration
        _token = _configService.LoadToken();

        // Subscribe to session state changes
        _sessionManager.SessionStateChanged += OnSessionStateChanged;
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_token);

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public async Task SetFocusStatusAsync(TimeSpan duration)
    {
        if (!IsConfigured || !IsEnabled)
        {
            return;
        }

        try
        {
            // Both expire when the session would end, so nothing is left behind if
            // the app crashes or the machine sleeps before the status is cleared
            var expiresAt = DateTimeOffset.UtcNow.Add(duration).ToUnixTimeSeconds();
            var dndMinutes = Math.Max(1, (int)Math.Ceiling(duration.TotalMinutes));

            var setStatusTask = SetStatusAsync(":tomato:", "Focus time", expiresAt);
            var setDndTask = SetDndSnoozeAsync(dndMinutes);

            await Task.WhenAll(setStatusTask, setDndTask).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Don't interrupt the focus session, but leave a trace
            _errorLog?.Log(ex, "Slack: setting focus status");
        }
    }

    public async Task ClearStatusAsync()
    {
        if (!IsConfigured || !IsEnabled)
        {
            return;
        }

        try
        {
            // Clear status and end DND in parallel
            var clearStatusTask = SetStatusAsync("", "", 0);
            var endDndTask = EndDndSnoozeAsync();

            await Task.WhenAll(clearStatusTask, endDndTask).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _errorLog?.Log(ex, "Slack: clearing focus status");
        }
    }

    public async Task ConfigureAsync(string token)
    {
        _token = token;
        await _configService.SaveTokenAsync(token);
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return ConnectionTestResult.Failed("Please enter a token.");
        }

        try
        {
            using var response = await SendAsync("auth.test", token, EmptyForm()).ConfigureAwait(false);

            // auth.test succeeds for any valid token, so also check it can do what Tomato needs.
            // Slack lists a user token's scopes in this header.
            if (response.Headers.TryGetValues("x-oauth-scopes", out var values))
            {
                var granted = string.Join(",", values).Split(',', StringSplitOptions.TrimEntries);
                var missing = RequiredScopes.Except(granted).ToList();
                if (missing.Count > 0)
                {
                    return ConnectionTestResult.Failed($"The token is missing the {string.Join(" and ", missing)} scope.");
                }
            }

            return ConnectionTestResult.Succeeded;
        }
        catch (SlackApiException ex)
        {
            return ConnectionTestResult.Failed($"Slack rejected the token ({ex.Error}).");
        }
        catch (Exception)
        {
            return ConnectionTestResult.Failed("Couldn't reach Slack. Check your connection and try again.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _sessionManager.SessionStateChanged -= OnSessionStateChanged;

        // Let a pending update (e.g. clearing the status on exit) finish first
        Task pending;
        lock (_updateLock)
        {
            pending = _pendingUpdate;
        }
        pending.Wait(ShutdownWaitTimeout);

        _httpClient.Dispose();
        _disposed = true;
    }

    private void OnSessionStateChanged(object? sender, SessionStateChangedEventArgs e)
    {
        // Only handle focus sessions
        if (e.Session.Type != SessionType.Focus)
        {
            return;
        }

        if (e.NewStatus == SessionStatus.Running)
        {
            var remaining = e.Session.TimeRemaining;
            QueueUpdate(() => SetFocusStatusAsync(remaining));
        }
        else if (e.NewStatus is SessionStatus.Paused or SessionStatus.Completed or SessionStatus.Cancelled)
        {
            QueueUpdate(ClearStatusAsync);
        }
    }

    /// <summary>
    /// Runs status updates one at a time, in order, so a quick start-then-cancel
    /// can't leave the focus status set.
    /// </summary>
    private void QueueUpdate(Func<Task> update)
    {
        lock (_updateLock)
        {
            _pendingUpdate = _pendingUpdate
                .ContinueWith(_ => update(), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default)
                .Unwrap();
        }
    }

    private async Task SetStatusAsync(string emoji, string text, long expiresAtUnixSeconds)
    {
        var profile = new
        {
            status_emoji = emoji,
            status_text = text,
            status_expiration = expiresAtUnixSeconds
        };

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("profile", JsonSerializer.Serialize(profile))
        });

        using var _ = await SendAsync("users.profile.set", _token!, content).ConfigureAwait(false);
    }

    private async Task SetDndSnoozeAsync(int minutes)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("num_minutes", minutes.ToString())
        });

        using var _ = await SendAsync("dnd.setSnooze", _token!, content).ConfigureAwait(false);
    }

    private async Task EndDndSnoozeAsync()
    {
        using var _ = await SendAsync("dnd.endSnooze", _token!, EmptyForm()).ConfigureAwait(false);
    }

    private static StringContent EmptyForm() =>
        new("", Encoding.UTF8, "application/x-www-form-urlencoded");

    /// <summary>
    /// Calls a Slack API method. Slack reports most errors as HTTP 200 with "ok": false,
    /// so both are turned into a <see cref="SlackApiException"/>.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(string method, string token, HttpContent content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{SlackApiBaseUrl}/{method}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = content;

        var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        try
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new SlackApiException(method, $"HTTP {(int)response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<SlackApiResponse>(json);
            if (result?.Ok != true)
            {
                throw new SlackApiException(method, result?.Error ?? "unknown_error");
            }

            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private sealed class SlackApiResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    private sealed class SlackApiException(string method, string error)
        : Exception($"Slack {method} failed: {error}")
    {
        public string Error { get; } = error;
    }
}
