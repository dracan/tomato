using FluentAssertions;
using Tomato.Services;

namespace Tomato.Tests.Unit;

public class HttpClientsTests
{
    [Fact]
    public void Create_UsesShortTimeout()
    {
        // Act
        using var client = HttpClients.Create();

        // Assert - the default of 100 seconds would hang Test Connection and exit cleanup
        client.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }
}
