using System.Net.Http;
using System.Text;
using System.Text.Json;
using Tomato.Models;

namespace Tomato.Services;

/// <summary>
/// Manages Luxafor LED light during focus sessions.
/// Subscribes to ISessionManager events to automatically control the LED.
/// </summary>
public sealed class LuxaforService : ILuxaforService
{
    private const string LuxaforApiUrl = "https://api.luxafor.com/webhook/v1/actions/solid_color";

    private readonly ILuxaforConfigurationService _configService;
    private readonly ISessionManager _sessionManager;
    private readonly HttpClient _httpClient;
    private static readonly TimeSpan ShutdownWaitTimeout = TimeSpan.FromSeconds(3);

    private readonly object _updateLock = new();
    private Task _pendingUpdate = Task.CompletedTask;

    private string? _userId;
    private bool _isEnabled = true;
    private bool _disposed;

    public LuxaforService(
        ILuxaforConfigurationService configService,
        ISessionManager sessionManager)
        : this(configService, sessionManager, new HttpClient())
    {
    }

    public LuxaforService(
        ILuxaforConfigurationService configService,
        ISessionManager sessionManager,
        HttpClient httpClient)
    {
        _configService = configService;
        _sessionManager = sessionManager;
        _httpClient = httpClient;

        // Load user ID from configuration
        _userId = _configService.LoadUserId();

        // Subscribe to session state changes
        _sessionManager.SessionStateChanged += OnSessionStateChanged;
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_userId);

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public async Task SetFocusColorAsync()
    {
        if (!IsConfigured || !IsEnabled)
        {
            return;
        }

        try
        {
            await SetColorAsync(_userId!, "red").ConfigureAwait(false);
        }
        catch
        {
            // Silent failure - don't interrupt user's focus session
        }
    }

    public async Task TurnOffAsync()
    {
        if (!IsConfigured || !IsEnabled)
        {
            return;
        }

        try
        {
            await SetColorAsync(_userId!, "custom", "000000").ConfigureAwait(false);
        }
        catch
        {
            // Silent failure - don't interrupt user's workflow
        }
    }

    public async Task ConfigureAsync(string userId)
    {
        _userId = userId;
        await _configService.SaveUserIdAsync(userId);
    }

    public async Task<bool> TestConnectionAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        try
        {
            // Test by setting a brief green color then turning off
            var response = await SetColorAsync(userId, "green");
            if (response)
            {
                // Brief delay then turn off
                await Task.Delay(500);
                await SetColorAsync(userId, "custom", "000000");
            }
            return response;
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

        // Let a pending update (e.g. turning the light off on exit) finish first
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
            QueueUpdate(SetFocusColorAsync);
        }
        else if (e.NewStatus is SessionStatus.Paused or SessionStatus.Completed or SessionStatus.Cancelled)
        {
            QueueUpdate(TurnOffAsync);
        }
    }

    /// <summary>
    /// Runs light updates one at a time, in order, so a quick start-then-cancel
    /// can't leave the light red.
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

    private async Task<bool> SetColorAsync(string userId, string color, string? customColor = null)
    {
        var actionFields = new Dictionary<string, string> { ["color"] = color };
        if (customColor != null)
        {
            actionFields["custom_color"] = customColor;
        }

        var payload = new
        {
            userId,
            actionFields
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync(LuxaforApiUrl, content).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }
}
