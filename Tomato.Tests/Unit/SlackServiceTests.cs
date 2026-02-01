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
        await _sut.SetFocusStatusAsync();

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
        await _sut.SetFocusStatusAsync();

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
        await _sut.SetFocusStatusAsync();

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
        await _sut.Invoking(s => s.SetFocusStatusAsync()).Should().NotThrowAsync();
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
    public async Task TestConnectionAsync_WhenNotConfigured_ReturnsFalse()
    {
        // Act
        var result = await _sut.TestConnectionAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionAsync_WhenApiReturnsOk_ReturnsTrue()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""ok"":true}");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        // Act
        var result = await _sut.TestConnectionAsync();

        // Assert
        result.Should().BeTrue();
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
        var result = await _sut.TestConnectionAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionAsync_WhenApiCallFails_ReturnsFalse()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.InternalServerError, "Server Error");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        // Act
        var result = await _sut.TestConnectionAsync();

        // Assert
        result.Should().BeFalse();
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
    public void OnFocusSessionPaused_DoesNotTriggerStatusChange()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""ok"":true}");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        var session = Session.CreateFocus();
        session.Status = SessionStatus.Paused;

        // Act - simulate focus session pause
        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Paused));

        // Assert - give async operation time to complete
        Thread.Sleep(100);
        _httpHandler.RequestCount.Should().Be(0);
    }

    [Fact]
    public void OnFocusSessionResumed_DoesNotTriggerStatusChange()
    {
        // Arrange
        _configService.LoadToken().Returns("xoxp-test-token");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""ok"":true}");
        _sut = new SlackService(_configService, _sessionManager, _httpClient);

        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;

        // Act - simulate focus session resume (Running after Paused is not NotStarted -> Running)
        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.Paused, SessionStatus.Running));

        // Assert - give async operation time to complete
        Thread.Sleep(100);
        _httpHandler.RequestCount.Should().Be(0);
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

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent)
            };
            return Task.FromResult(response);
        }
    }
}
