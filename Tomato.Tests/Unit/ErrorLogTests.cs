using System.IO;
using FluentAssertions;
using Tomato.Services;

namespace Tomato.Tests.Unit;

public class ErrorLogTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ErrorLog _sut;

    public ErrorLogTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "Tomato.Tests", Guid.NewGuid().ToString());
        _sut = new ErrorLog(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void Log_WritesContextAndExceptionDetails()
    {
        // Act
        _sut.Log(new InvalidOperationException("Something broke"), "Startup");

        // Assert
        var text = File.ReadAllText(_sut.LogFilePath);
        text.Should().Contain("Startup");
        text.Should().Contain("InvalidOperationException");
        text.Should().Contain("Something broke");
    }

    [Fact]
    public void Log_AppendsToExistingLog()
    {
        // Act
        _sut.Log(new Exception("First"), "A");
        _sut.Log(new Exception("Second"), "B");

        // Assert
        var text = File.ReadAllText(_sut.LogFilePath);
        text.Should().Contain("First").And.Contain("Second");
    }

    [Fact]
    public void Log_WhenLogIsLarge_StartsANewFile()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(_sut.LogFilePath, new string('x', (int)ErrorLog.MaxLogFileBytes + 1));

        // Act
        _sut.Log(new Exception("Fresh"), "C");

        // Assert
        new FileInfo(_sut.LogFilePath).Length.Should().BeLessThan(ErrorLog.MaxLogFileBytes);
        File.ReadAllText(_sut.LogFilePath).Should().Contain("Fresh");
    }

    [Fact]
    public void Log_WhenDirectoryCannotBeCreated_DoesNotThrow()
    {
        // Arrange - a file where the directory should be
        var blocked = Path.Combine(Path.GetTempPath(), "Tomato.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.GetDirectoryName(blocked)!);
        File.WriteAllText(blocked, "");
        var sut = new ErrorLog(blocked);

        try
        {
            // Act & Assert
            sut.Invoking(s => s.Log(new Exception("x"), "D")).Should().NotThrow();
        }
        finally
        {
            File.Delete(blocked);
        }
    }
}
