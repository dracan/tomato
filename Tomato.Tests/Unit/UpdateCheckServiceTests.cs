using System.Net;
using System.Net.Http;
using FluentAssertions;
using Tomato.Services;

namespace Tomato.Tests.Unit;

public class UpdateCheckServiceTests : IDisposable
{
    private readonly MockHttpMessageHandler _httpHandler;
    private readonly HttpClient _httpClient;
    private UpdateCheckService _sut;

    public UpdateCheckServiceTests()
    {
        _httpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler);
        _sut = new UpdateCheckService(_httpClient);
    }

    public void Dispose()
    {
        _sut.Dispose();
        _httpClient.Dispose();
    }

    #region CheckForUpdateAsync

    [Fact]
    public async Task CheckForUpdateAsync_WhenNewerVersionAvailable_ReturnsResult()
    {
        // Arrange
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{
            ""tag_name"": ""v99.0.0"",
            ""html_url"": ""https://github.com/dracan/tomato/releases/tag/v99.0.0""
        }");

        // Act
        var result = await _sut.CheckForUpdateAsync();

        // Assert
        result.Should().NotBeNull();
        result!.LatestVersion.Should().Be(new Version(99, 0, 0));
        result.ReleaseUrl.Should().Be("https://github.com/dracan/tomato/releases/tag/v99.0.0");
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenNewerSingleNumberVersion_ReturnsResult()
    {
        // Arrange - simulates GitHub releases like "v24"
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{
            ""tag_name"": ""v99"",
            ""html_url"": ""https://github.com/dracan/tomato/releases/tag/v99""
        }");

        // Act
        var result = await _sut.CheckForUpdateAsync();

        // Assert
        result.Should().NotBeNull();
        result!.LatestVersion.Should().Be(new Version(99, 0));
        result.ReleaseUrl.Should().Be("https://github.com/dracan/tomato/releases/tag/v99");
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenCurrentVersionIsLatest_ReturnsNull()
    {
        // Arrange - use v0.0.0 which is the lowest possible version
        // MinVer defaults to 0.0.0-alpha.0 (assembly version 0.0.0.0) when no tags exist
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{
            ""tag_name"": ""v0.0.0"",
            ""html_url"": ""https://github.com/dracan/tomato/releases/tag/v0.0.0""
        }");

        // Act
        var result = await _sut.CheckForUpdateAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenNetworkError_ReturnsNull()
    {
        // Arrange
        _httpHandler.SetupResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _sut.CheckForUpdateAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenExceptionThrown_ReturnsNull()
    {
        // Arrange
        _httpHandler.SetupException(new HttpRequestException("Network error"));

        // Act
        var result = await _sut.CheckForUpdateAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenInvalidJson_ReturnsNull()
    {
        // Arrange
        _httpHandler.SetupResponse(HttpStatusCode.OK, "not valid json");

        // Act
        var result = await _sut.CheckForUpdateAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenMissingTagName_ReturnsNull()
    {
        // Arrange
        _httpHandler.SetupResponse(HttpStatusCode.OK, @"{
            ""html_url"": ""https://github.com/dracan/tomato/releases/tag/v1.0.0""
        }");

        // Act
        var result = await _sut.CheckForUpdateAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ParseVersion

    [Theory]
    [InlineData("v1.0.0", 1, 0, 0)]
    [InlineData("V1.0.0", 1, 0, 0)]
    [InlineData("1.0.0", 1, 0, 0)]
    [InlineData("v2.5.3", 2, 5, 3)]
    [InlineData("v10.20.30", 10, 20, 30)]
    [InlineData("1.2.3.4", 1, 2, 3, 4)]
    public void ParseVersion_WithValidVersionString_ReturnsVersion(
        string tagName,
        int major,
        int minor,
        int build,
        int revision = -1)
    {
        // Act
        var result = UpdateCheckService.ParseVersion(tagName);

        // Assert
        result.Should().NotBeNull();
        result!.Major.Should().Be(major);
        result.Minor.Should().Be(minor);
        result.Build.Should().Be(build);
        if (revision >= 0)
        {
            result.Revision.Should().Be(revision);
        }
    }

    [Theory]
    [InlineData("v24", 24, 0)]
    [InlineData("V24", 24, 0)]
    [InlineData("24", 24, 0)]
    [InlineData("v1", 1, 0)]
    [InlineData("100", 100, 0)]
    public void ParseVersion_WithSingleNumberVersion_ReturnsVersionWithMinorZero(
        string tagName,
        int major,
        int minor)
    {
        // Act
        var result = UpdateCheckService.ParseVersion(tagName);

        // Assert
        result.Should().NotBeNull();
        result!.Major.Should().Be(major);
        result.Minor.Should().Be(minor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("v")]
    [InlineData("vABC")]
    public void ParseVersion_WithInvalidVersionString_ReturnsNull(string tagName)
    {
        // Act
        var result = UpdateCheckService.ParseVersion(tagName);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    /// <summary>
    /// Mock HTTP message handler for testing HTTP calls.
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private HttpStatusCode _statusCode = HttpStatusCode.OK;
        private string _responseContent = "";
        private Exception? _exception;

        public void SetupResponse(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _responseContent = content;
            _exception = null;
        }

        public void SetupException(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_exception != null)
            {
                throw _exception;
            }

            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent)
            };
            return Task.FromResult(response);
        }
    }
}
