using System.Net;
using System.Net.Http;
using FluentAssertions;
using NSubstitute;
using Tomato.Models;
using Tomato.Services;

namespace Tomato.Tests.Unit;

public class LuxaforServiceTests : IDisposable
{
    private readonly ILuxaforConfigurationService _configService;
    private readonly ISessionManager _sessionManager;
    private readonly MockHttpMessageHandler _httpHandler;
    private readonly HttpClient _httpClient;
    private LuxaforService _sut;

    public LuxaforServiceTests()
    {
        _configService = Substitute.For<ILuxaforConfigurationService>();
        _sessionManager = Substitute.For<ISessionManager>();
        _httpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler);

        // Default: no user ID configured
        _configService.LoadUserId().Returns((string?)null);

        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);
    }

    public void Dispose()
    {
        _sut.Dispose();
        _httpClient.Dispose();
    }

    #region IsConfigured

    [Fact]
    public void IsConfigured_WhenNoUserId_ReturnsFalse()
    {
        // Assert
        _sut.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WhenUserIdLoaded_ReturnsTrue()
    {
        // Arrange
        _configService.LoadUserId().Returns("test-user-id");
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);

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

    #region SetFocusColorAsync

    [Fact]
    public async Task SetFocusColorAsync_WhenNotConfigured_DoesNotMakeApiCall()
    {
        // Act
        await _sut.SetFocusColorAsync();

        // Assert
        _httpHandler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task SetFocusColorAsync_WhenDisabled_DoesNotMakeApiCall()
    {
        // Arrange
        _configService.LoadUserId().Returns("test-user-id");
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);
        _sut.IsEnabled = false;

        // Act
        await _sut.SetFocusColorAsync();

        // Assert
        _httpHandler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task SetFocusColorAsync_WhenConfiguredAndEnabled_MakesApiCallWithRedColor()
    {
        // Arrange
        _configService.LoadUserId().Returns("test-user-id");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""success"":true}");
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);

        // Act
        await _sut.SetFocusColorAsync();

        // Assert
        _httpHandler.RequestCount.Should().Be(1);
        _httpHandler.Requests[0].RequestUri!.ToString().Should().Contain("solid_color");
        var content = _httpHandler.Bodies[0];
        content.Should().Contain("\"color\":\"red\"");
        content.Should().Contain("\"userId\":\"test-user-id\"");
    }

    [Fact]
    public async Task SetFocusColorAsync_WhenApiCallFails_DoesNotThrow()
    {
        // Arrange
        _configService.LoadUserId().Returns("test-user-id");
        _httpHandler.SetupResponse(HttpStatusCode.InternalServerError, "Server Error");
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);

        // Act & Assert - should not throw
        await _sut.Invoking(s => s.SetFocusColorAsync()).Should().NotThrowAsync();
    }

    #endregion

    #region TurnOffAsync

    [Fact]
    public async Task TurnOffAsync_WhenNotConfigured_DoesNotMakeApiCall()
    {
        // Act
        await _sut.TurnOffAsync();

        // Assert
        _httpHandler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task TurnOffAsync_WhenConfiguredAndEnabled_MakesApiCallWithCustomColorBlack()
    {
        // Arrange
        _configService.LoadUserId().Returns("test-user-id");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""success"":true}");
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);

        // Act
        await _sut.TurnOffAsync();

        // Assert
        _httpHandler.RequestCount.Should().Be(1);
        var content = _httpHandler.Bodies[0];
        content.Should().Contain("\"color\":\"custom\"");
        content.Should().Contain("\"custom_color\":\"000000\"");
    }

    #endregion

    #region ConfigureAsync

    [Fact]
    public async Task ConfigureAsync_SavesUserIdToConfigService()
    {
        // Act
        await _sut.ConfigureAsync("new-user-id");

        // Assert
        await _configService.Received(1).SaveUserIdAsync("new-user-id");
    }

    [Fact]
    public async Task ConfigureAsync_UpdatesIsConfigured()
    {
        // Arrange
        _sut.IsConfigured.Should().BeFalse();

        // Act
        await _sut.ConfigureAsync("new-user-id");

        // Assert
        _sut.IsConfigured.Should().BeTrue();
    }

    #endregion

    #region TestConnectionAsync

    [Fact]
    public async Task TestConnectionAsync_WithEmptyUserId_ReturnsFalse()
    {
        // Act
        var result = await _sut.TestConnectionAsync("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionAsync_WhenApiReturnsOk_ReturnsTrue()
    {
        // Arrange
        _configService.LoadUserId().Returns("test-user-id");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""success"":true}");
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);

        // Act
        var result = await _sut.TestConnectionAsync("test-user-id");

        // Assert
        result.Should().BeTrue();
        // Should make two calls: green flash then off
        _httpHandler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenApiCallFails_ReturnsFalse()
    {
        // Arrange
        _configService.LoadUserId().Returns("test-user-id");
        _httpHandler.SetupResponse(HttpStatusCode.InternalServerError, "Server Error");
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);

        // Act
        var result = await _sut.TestConnectionAsync("test-user-id");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionAsync_DoesNotSaveOrSwitchToTheTestedUserId()
    {
        // Arrange - a working user ID is already saved
        _configService.LoadUserId().Returns("good-user-id");
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);

        // Act - test a different (mistyped) user ID, then the dialog is cancelled
        await _sut.TestConnectionAsync("typo-user-id");
        await _sut.SetFocusColorAsync();

        // Assert
        await _configService.DidNotReceive().SaveUserIdAsync(Arg.Any<string>());
        _httpHandler.Bodies.Take(2).Should().OnlyContain(b => b.Contains("typo-user-id"));
        _httpHandler.Bodies.Last().Should().Contain("good-user-id");
    }

    #endregion

    #region Event Handling

    [Fact]
    public void OnFocusSessionStart_TriggersSetFocusColor()
    {
        // Arrange
        _configService.LoadUserId().Returns("test-user-id");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""success"":true}");
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);

        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;

        // Act - simulate focus session start
        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Assert - give async operation time to complete
        Thread.Sleep(100);
        _httpHandler.RequestCount.Should().Be(1);
        _httpHandler.Requests[0].RequestUri!.ToString().Should().Contain("solid_color");
    }

    [Fact]
    public void OnFocusSessionCompleted_TriggersTurnOff()
    {
        // Arrange
        _configService.LoadUserId().Returns("test-user-id");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""success"":true}");
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);

        var session = Session.CreateFocus();
        session.Status = SessionStatus.Completed;

        // Act - simulate focus session completion
        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Completed));

        // Assert - give async operation time to complete
        Thread.Sleep(100);
        _httpHandler.RequestCount.Should().Be(1);
        _httpHandler.Requests[0].RequestUri!.ToString().Should().Contain("solid_color");
    }

    [Fact]
    public void OnFocusSessionCancelled_TriggersTurnOff()
    {
        // Arrange
        _configService.LoadUserId().Returns("test-user-id");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""success"":true}");
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);

        var session = Session.CreateFocus();
        session.Status = SessionStatus.Cancelled;

        // Act - simulate focus session cancellation
        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Cancelled));

        // Assert - give async operation time to complete
        Thread.Sleep(100);
        _httpHandler.RequestCount.Should().Be(1);
        _httpHandler.Requests[0].RequestUri!.ToString().Should().Contain("solid_color");
    }

    [Fact]
    public void OnBreakSessionStart_DoesNotTriggerColorChange()
    {
        // Arrange
        _configService.LoadUserId().Returns("test-user-id");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""success"":true}");
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);

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
    public void OnFocusSessionPaused_TurnsLightOff()
    {
        // Arrange
        _configService.LoadUserId().Returns("test-user-id");
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);

        var session = Session.CreateFocus();
        session.Status = SessionStatus.Paused;

        // Act - simulate focus session pause
        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Paused));

        // Assert - give async operation time to complete
        Thread.Sleep(100);
        _httpHandler.Bodies.Should().ContainSingle().Which.Should().Contain("000000");
    }

    [Fact]
    public void OnFocusSessionResumed_SetsFocusColor()
    {
        // Arrange
        _configService.LoadUserId().Returns("test-user-id");
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);

        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;

        // Act - simulate resuming a paused session (e.g. one restored after a restart)
        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.Paused, SessionStatus.Running));

        // Assert - give async operation time to complete
        Thread.Sleep(100);
        _httpHandler.Bodies.Should().ContainSingle().Which.Should().Contain("\"color\":\"red\"");
    }

    [Fact]
    public void Dispose_WaitsForPendingTurnOff()
    {
        // Arrange - a slow network
        _configService.LoadUserId().Returns("test-user-id");
        _httpHandler.Delay = TimeSpan.FromMilliseconds(300);
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);

        var session = Session.CreateFocus();
        session.Status = SessionStatus.Paused;
        _sessionManager.SessionStateChanged += Raise.EventWith(
            _sessionManager,
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Paused));

        // Act - the app exits straight away
        _sut.Dispose();

        // Assert
        _httpHandler.CompletedCount.Should().Be(1);
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Arrange
        _configService.LoadUserId().Returns("test-user-id");
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{""success"":true}");
        _sut = new LuxaforService(_configService, _sessionManager, _httpClient);

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
        private string _responseContent = @"{""success"":true}";

        public List<HttpRequestMessage> Requests { get; } = new();
        public int RequestCount => Requests.Count;

        public void SetupResponse(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _responseContent = content;
        }

        public List<string> Bodies { get; } = new();
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
            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent)
            };
        }
    }
}
