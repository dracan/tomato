using System.IO;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using NSubstitute;
using Tomato.Models;
using Tomato.Services;

namespace Tomato.Tests.Unit;

public class SlackServiceTests : IDisposable
{
    private readonly ISlackConfigurationService _configService;
    private readonly ISessionManager _sessionManager;
    private readonly MockHttpMessageHandler _httpHandler;
    private readonly HttpClient _httpClient;
    private SlackService _sut;

    public SlackServiceTests()
    {
        _configService = Substitute.For<ISlackConfigurationService>();
        _sessionManager = Substitute.For<ISessionManager>();
        _httpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler);

        // Default: no token configured
        _configService.LoadToken().Returns((string?)null);

        _sut = new SlackService(_configService, _sessionManager, _httpClient);
    }

    public void Dispose()
    {
        _sut.Dispose();
        _httpClient.Dispose();
    }

    #region IsConfigured

    [Fact]
    public void IsConfigured_WhenNoToken_ReturnsFalse()
    {
        // Assert
        _sut.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WhenTokenLoaded_ReturnsTrue()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        // Assert
        _sut.IsConfigured.Should().BeTrue();
    }

    #endregion

    #region IsEnabled

    [Fact]
    public void IsEnabled_DefaultsToTrue()
    {
        // Assert
        _sut.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_CanBeSetToFalse()
    {
        // Act
        _sut.IsEnabled = false;

        // Assert
        _sut.IsEnabled.Should().BeFalse();
    }

    #endregion

    #region SetFocusStatusAsync

    [Fact]
    public async Task SetFocusStatusAsync_WhenNotConfigured_DoesNotMakeApiCall()
    {
        // Act
        await _sut.SetFocusStatusAsync(TimeSpan.FromMinutes(25));

        // Assert
        _httpHandler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task SetFocusStatusAsync_WhenDisabled_DoesNotMakeApiCall()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);
        _sut.IsEnabled = false;

        // Act
        await _sut.SetFocusStatusAsync(TimeSpan.FromMinutes(25));

        // Assert
        _httpHandler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task SetFocusStatusAsync_WhenConfiguredAndEnabled_MakesStatusAndDndCalls()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""ok"":true}");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        // Act
        await _sut.SetFocusStatusAsync(TimeSpan.FromMinutes(25));

        // Assert
        _httpHandler.RequestCount.Should().Be(2);
        _httpHandler.Requests.Should().Contain(r => r.RequestUri!.ToString().Contains("users.profile.set"));
        _httpHandler.Requests.Should().Contain(r => r.RequestUri!.ToString().Contains("dnd.setSnooze"));
    }

    [Fact]
    public async Task SetFocusStatusAsync_WhenApiCallFails_DoesNotThrow()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.InternalServerError, "Server Error");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        // Act & Assert - should not throw
        await _sut.Invoking(s => s.SetFocusStatusAsync(TimeSpan.FromMinutes(25))).Should().NotThrowAsync();
    }

    #endregion

    #region ClearStatusAsync

    [Fact]
    public async Task ClearStatusAsync_WhenNotConfigured_DoesNotMakeApiCall()
    {
        // Act
        await _sut.ClearStatusAsync();

        // Assert
        _httpHandler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task ClearStatusAsync_WhenConfiguredAndEnabled_MakesStatusAndDndCalls()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""ok"":true}");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        // Act
        await _sut.ClearStatusAsync();

        // Assert
        _httpHandler.RequestCount.Should().Be(2);
        _httpHandler.Requests.Should().Contain(r => r.RequestUri!.ToString().Contains("users.profile.set"));
        _httpHandler.Requests.Should().Contain(r => r.RequestUri!.ToString().Contains("dnd.endSnooze"));
    }

    #endregion

    #region ConfigureAsync

    [Fact]
    public async Task ConfigureAsync_SavesTokenToConfigService()
    {
        // Act
        await _sut.ConfigureAsync("xoxp-new-token");

        // Assert
        await _configService.Received(1).SaveTokenAsync("xoxp-new-token");
    }

    [Fact]
    public async Task ConfigureAsync_UpdatesIsConfigured()
    {
        // Arrange
        _sut.IsConfigured.Should().BeFalse();

        // Act
        await _sut.ConfigureAsync("xoxp-new-token");

        // Assert
        _sut.IsConfigured.Should().BeTrue();
    }

    #endregion

    #region TestConnectionAsync

    [Fact]
    public async Task TestConnectionAsync_WithEmptyToken_ReturnsFalse()
    {
        // Act
        var result = await _sut.TestConnectionAsync("");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionAsync_WhenApiReturnsOk_ReturnsTrue()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""ok"":true}");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        // Act
        var result = await _sut.TestConnectionAsync("xoxp-test-token");

        // Assert
        result.Success.Should().BeTrue();
        _httpHandler.Requests.Should().Contain(r => r.RequestUri!.ToString().Contains("auth.test"));
    }

    [Fact]
    public async Task TestConnectionAsync_WhenApiReturnsNotOk_ReturnsFalse()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""ok"":false,""error"":""invalid_auth""}");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        // Act
        var result = await _sut.TestConnectionAsync("xoxp-test-token");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("invalid_auth");
    }

    [Fact]
    public async Task TestConnectionAsync_WhenApiCallFails_ReturnsFalse()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.InternalServerError, "Server Error");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        // Act
        var result = await _sut.TestConnectionAsync("xoxp-test-token");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionAsync_DoesNotSaveOrSwitchToTheTestedToken()
    {
        // Arrange - a working token is already saved
        _configService.LoadToken().Returns("xoxp-good-token");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        // Act - test a different (mistyped) token, then the dialog is cancelled
        await _sut.TestConnectionAsync("xoxp-typo");
        await _sut.SetFocusStatusAsync(TimeSpan.FromMinutes(25));

        // Assert
        await _configService.DidNotReceive().SaveTokenAsync(Arg.Any<string>());
        _httpHandler.Requests[0].Headers.Authorization!.Parameter.Should().Be("xoxp-typo");
        _httpHandler.Requests.Skip(1).Should().OnlyContain(r => r.Headers.Authorization!.Parameter == "xoxp-good-token");
    }

    [Fact]
    public async Task TestConnectionAsync_WhenTokenLacksRequiredScopes_FailsNamingThem()
    {
        // Arrange - a valid token without the scopes Tomato needs
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""ok"":true}");
        _httpHandler.ResponseHeaders["x-oauth-scopes"] = "users.profile:write,chat:write";
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        // Act
        var result = await _sut.TestConnectionAsync("xoxp-test-token");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("dnd:write");
        result.Message.Should().NotContain("users.profile:write");
    }

    [Fact]
    public async Task TestConnectionAsync_WhenTokenHasRequiredScopes_Succeeds()
    {
        // Arrange
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""ok"":true}");
        _httpHandler.ResponseHeaders["x-oauth-scopes"] = "dnd:write,users.profile:write";
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        // Act
        var result = await _sut.TestConnectionAsync("xoxp-test-token");

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region API Errors

    [Fact]
    public async Task SetFocusStatusAsync_WhenSlackReportsAnError_LogsIt()
    {
        // Arrange - Slack returns HTTP 200 with ok:false for API errors
        var logDirectory = Path.Combine(Path.GetTempPath(), "Tomato.Tests", Guid.NewGuid().ToString());
        var errorLog = new ErrorLog(logDirectory);
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""ok"":false,""error"":""missing_scope""}");
        _sut = new SlackService(_configService, _sessionManager, _httpClient, errorLog);

        try
        {
            // Act
            await _sut.SetFocusStatusAsync(TimeSpan.FromMinutes(25));

            // Assert
            File.ReadAllText(errorLog.LogFilePath).Should().Contain("missing_scope");
        }
        finally
        {
            Directory.Delete(logDirectory, true);
        }
    }

    #endregion

    #region Event Handling

    [Fact]
    public void OnFocusSessionStart_TriggersSetFocusStatus()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""ok"":true}");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;

        // Act - simulate focus session start
        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Assert - give async operation time to complete
        Thread.Sleep(100);
        _httpHandler.Requests.Should().Contain(r => r.RequestUri!.ToString().Contains("users.profile.set"));
        _httpHandler.Requests.Should().Contain(r => r.RequestUri!.ToString().Contains("dnd.setSnooze"));
    }

    [Fact]
    public void OnFocusSessionCompleted_TriggersClearStatus()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""ok"":true}");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        var session = Session.CreateFocus();
        session.Status = SessionStatus.Completed;

        // Act - simulate focus session completion
        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Completed));

        // Assert - give async operation time to complete
        Thread.Sleep(100);
        _httpHandler.Requests.Should().Contain(r => r.RequestUri!.ToString().Contains("users.profile.set"));
        _httpHandler.Requests.Should().Contain(r => r.RequestUri!.ToString().Contains("dnd.endSnooze"));
    }

    [Fact]
    public void OnFocusSessionCancelled_TriggersClearStatus()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""ok"":true}");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        var session = Session.CreateFocus();
        session.Status = SessionStatus.Cancelled;

        // Act - simulate focus session cancellation
        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Cancelled));

        // Assert - give async operation time to complete
        Thread.Sleep(100);
        _httpHandler.Requests.Should().Contain(r => r.RequestUri!.ToString().Contains("users.profile.set"));
        _httpHandler.Requests.Should().Contain(r => r.RequestUri!.ToString().Contains("dnd.endSnooze"));
    }

    [Fact]
    public void OnBreakSessionStart_DoesNotTriggerStatusChange()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""ok"":true}");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        var session = Session.CreateShortBreak();
        session.Status = SessionStatus.Running;

        // Act - simulate break session start
        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Assert - give async operation time to complete
        Thread.Sleep(100);
        _httpHandler.RequestCount.Should().Be(0);
    }

    [Fact]
    public void OnFocusSessionPaused_ClearsStatus()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        var session = Session.CreateFocus();
        session.Status = SessionStatus.Paused;

        // Act - simulate focus session pause
        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Paused));

        // Assert - give async operation time to complete
        Thread.Sleep(100);
        _httpHandler.Requests.Should().Contain(r => r.RequestUri!.ToString().Contains("dnd.endSnooze"));
    }

    [Fact]
    public void OnFocusSessionResumed_SetsFocusStatus()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;

        // Act - simulate resuming a paused session (e.g. one restored after a restart)
        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.Paused, SessionStatus.Running));

        // Assert - give async operation time to complete
        Thread.Sleep(100);
        _httpHandler.Requests.Should().Contain(r => r.RequestUri!.ToString().Contains("dnd.setSnooze"));
    }

    [Fact]
    public async Task SetFocusStatusAsync_StatusExpiresAndDndEndsWhenSessionDoes()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);
        var expectedExpiry = DateTimeOffset.UtcNow.AddMinutes(25).ToUnixTimeSeconds();

        // Act
        await _sut.SetFocusStatusAsync(TimeSpan.FromMinutes(25));

        // Assert
        var profileBody = Uri.UnescapeDataString(_httpHandler.Bodies.Single(b => b.StartsWith("profile=")));
        var expiry = long.Parse(System.Text.RegularExpressions.Regex.Match(profileBody, "\"status_expiration\":(\\d+)").Groups[1].Value);
        expiry.Should().BeCloseTo(expectedExpiry, 60);
        _httpHandler.Bodies.Should().Contain("num_minutes=25");
    }

    [Fact]
    public void Dispose_WaitsForPendingStatusClear()
    {
        // Arrange - a slow network
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.Delay = TimeSpan.FromMilliseconds(300);
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        var session = Session.CreateFocus();
        session.Status = SessionStatus.Paused;
        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Paused));

        // Act - the app exits straight away
        _sut.Dispose();

        // Assert - the clear requests finished before Dispose returned
        _httpHandler.CompletedCount.Should().Be(2);
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""ok"":true}");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        // Act
        _sut.Dispose();

        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;

        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Assert - give async operation time to complete
        Thread.Sleep(100);
        _httpHandler.RequestCount.Should().Be(0);
    }

    #endregion

    /// <summary>
    /// Mock HTTP message handler for testing HTTP calls.
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private HttpStatusCode _statusCode = HttpStatusCode.OK;
        private string _responseContent = @"{""ok"":true}";

        public List<HttpRequestMessage> Requests { get; } = new();
        public int RequestCount => Requests.Count;

        public void SetupResponse(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _responseContent = content;
        }

        public List<string> Bodies { get; } = new();
        public Dictionary<string, string> ResponseHeaders { get; } = new();
        public TimeSpan Delay { get; set; }
        private int _completedCount;
        public int CompletedCount => Volatile.Read(ref _completedCount);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            lock (Requests)
            {
                Requests.Add(request);
                Bodies.Add(request.Content?.ReadAsStringAsync(cancellationToken).Result ?? "");
            }

            if (Delay > TimeSpan.Zero)
                await Task.Delay(Delay, cancellationToken);

            Interlocked.Increment(ref _completedCount);
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent)
            };
            foreach (var header in ResponseHeaders)
                response.Headers.Add(header.Key, header.Value);
            return response;
        }
    }
}
