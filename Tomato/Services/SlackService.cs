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

    private readonly ISlackConfigurationService _configService;
    private readonly ISessionManager _sessionManager;
    private readonly HttpClient _httpClient;
    private static readonly TimeSpan ShutdownWaitTimeout = TimeSpan.FromSeconds(3);

    private readonly object _updateLock = new();
    private Task _pendingUpdate = Task.CompletedTask;

    private string? _token;
    private bool _isEnabled = true;
    private bool _disposed;

    public SlackService(
        ISlackConfigurationService configService,
        ISessionManager sessionManager)
        : this(configService, sessionManager, new HttpClient())
    {
    }

    public SlackService(
        ISlackConfigurationService configService,
        ISessionManager sessionManager,
        HttpClient httpClient)
    {
        _configService = configService;
        _sessionManager = sessionManager;
        _httpClient = httpClient;

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
        catch
        {
            // Silent failure - don't interrupt user's focus session
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
        catch
        {
            // Silent failure - don't interrupt user's workflow
        }
    }

    public async Task ConfigureAsync(string token)
    {
        _token = token;
        await _configService.SaveTokenAsync(token);
    }

    public async Task<bool> TestConnectionAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{SlackApiBaseUrl}/auth.test");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent("", Encoding.UTF8, "application/x-www-form-urlencoded");

            using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<SlackApiResponse>(json);
            return result?.Ok ?? false;
        }
        catch
        {
            return false;
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

        var profileJson = JsonSerializer.Serialize(profile);
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("profile", profileJson)
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{SlackApiBaseUrl}/users.profile.set");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Content = content;

        using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        // We don't check the response - fire and forget
    }

    private async Task SetDndSnoozeAsync(int minutes)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("num_minutes", minutes.ToString())
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{SlackApiBaseUrl}/dnd.setSnooze");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Content = content;

        using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        // We don't check the response - fire and forget
    }

    private async Task EndDndSnoozeAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{SlackApiBaseUrl}/dnd.endSnooze");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Content = new StringContent("", Encoding.UTF8, "application/x-www-form-urlencoded");

        using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        // We don't check the response - fire and forget
    }

    private sealed class SlackApiResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }
    }
}
