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
    private const int DndDurationMinutes = 120;

    private readonly ISlackConfigurationService _configService;
    private readonly ISessionManager _sessionManager;
    private readonly HttpClient _httpClient;

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

    public async Task SetFocusStatusAsync()
    {
        if (!IsConfigured || !IsEnabled)
        {
            return;
        }

        try
        {
            // Set status and enable DND in parallel
            var setStatusTask = SetStatusAsync(":tomato:", "Focus time");
            var setDndTask = SetDndSnoozeAsync(DndDurationMinutes);

            await Task.WhenAll(setStatusTask, setDndTask);
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
            var clearStatusTask = SetStatusAsync("", "");
            var endDndTask = EndDndSnoozeAsync();

            await Task.WhenAll(clearStatusTask, endDndTask);
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

    public async Task<bool> TestConnectionAsync()
    {
        if (!IsConfigured)
        {
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{SlackApiBaseUrl}/auth.test");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            request.Content = new StringContent("", Encoding.UTF8, "application/x-www-form-urlencoded");

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var json = await response.Content.ReadAsStringAsync();
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

        // Focus session started
        if (e.NewStatus == SessionStatus.Running && e.PreviousStatus == SessionStatus.NotStarted)
        {
            _ = SetFocusStatusAsync();
        }
        // Focus session ended (completed or cancelled)
        else if (e.NewStatus == SessionStatus.Completed || e.NewStatus == SessionStatus.Cancelled)
        {
            _ = ClearStatusAsync();
        }
    }

    private async Task SetStatusAsync(string emoji, string text)
    {
        var profile = new
        {
            status_emoji = emoji,
            status_text = text,
            status_expiration = 0
        };

        var profileJson = JsonSerializer.Serialize(profile);
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("profile", profileJson)
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{SlackApiBaseUrl}/users.profile.set");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Content = content;

        using var response = await _httpClient.SendAsync(request);
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

        using var response = await _httpClient.SendAsync(request);
        // We don't check the response - fire and forget
    }

    private async Task EndDndSnoozeAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{SlackApiBaseUrl}/dnd.endSnooze");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Content = new StringContent("", Encoding.UTF8, "application/x-www-form-urlencoded");

        using var response = await _httpClient.SendAsync(request);
        // We don't check the response - fire and forget
    }

    private sealed class SlackApiResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }
    }
}
