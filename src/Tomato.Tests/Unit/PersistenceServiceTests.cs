using System.IO;
using FluentAssertions;
using Tomato.Models;
using Tomato.Services;

namespace Tomato.Tests.Unit;

public class PersistenceServiceTests : IAsyncLifetime
{
    private readonly string _testDirectory;
    private readonly PersistenceService _sut;

    public PersistenceServiceTests()
    {
        // Use a unique test directory to avoid conflicts
        _testDirectory = Path.Combine(Path.GetTempPath(), "Tomato.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _sut = new PersistenceService(_testDirectory);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _sut.ClearAsync();
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task SaveAsync_CreatesStateFile()
    {
        // Arrange
        var state = CreateTestState();

        // Act
        await _sut.SaveAsync(state);

        // Assert
        File.Exists(_sut.StateFilePath).Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_ReturnsNullWhenNoFileExists()
    {
        // Act
        var result = await _sut.LoadAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_ReturnsSavedState()
    {
        // Arrange
        var state = CreateTestState();
        await _sut.SaveAsync(state);

        // Act
        var result = await _sut.LoadAsync();

        // Assert
        result.Should().NotBeNull();
        result!.LastSavedAt.Should().BeCloseTo(state.LastSavedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task LoadAsync_RestoresSessionState()
    {
        // Arrange
        var state = CreateTestState();
        state.CurrentSession = new AppState.SessionState
        {
            Type = SessionType.Focus,
            Status = SessionStatus.Paused,
            Duration = TimeSpan.FromMinutes(25),
            TimeRemaining = TimeSpan.FromMinutes(15),
            StartedAt = DateTime.Now.AddMinutes(-10)
        };
        await _sut.SaveAsync(state);

        // Act
        var result = await _sut.LoadAsync();

        // Assert
        result.Should().NotBeNull();
        result!.CurrentSession.Should().NotBeNull();
        result.CurrentSession!.Type.Should().Be(SessionType.Focus);
        result.CurrentSession.Status.Should().Be(SessionStatus.Paused);
        result.CurrentSession.TimeRemaining.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task LoadAsync_RestoresCycleState()
    {
        // Arrange
        var state = CreateTestState();
        state.Cycle.CompletedFocusSessions = 3;
        await _sut.SaveAsync(state);

        // Act
        var result = await _sut.LoadAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Cycle.CompletedFocusSessions.Should().Be(3);
    }

    [Fact]
    public async Task LoadAsync_RestoresDailyStatistics()
    {
        // Arrange
        var state = CreateTestState();
        state.TodayStatistics = new AppState.DailyStatisticsState
        {
            Date = DateOnly.FromDateTime(DateTime.Now),
            FocusSessionsCompleted = 5,
            TotalFocusTime = TimeSpan.FromHours(2),
            TotalBreakTime = TimeSpan.FromMinutes(30),
            CyclesCompleted = 1
        };
        await _sut.SaveAsync(state);

        // Act
        var result = await _sut.LoadAsync();

        // Assert
        result.Should().NotBeNull();
        result!.TodayStatistics.Should().NotBeNull();
        result.TodayStatistics!.FocusSessionsCompleted.Should().Be(5);
        result.TodayStatistics.TotalFocusTime.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public async Task ClearAsync_DeletesStateFile()
    {
        // Arrange
        var state = CreateTestState();
        await _sut.SaveAsync(state);
        File.Exists(_sut.StateFilePath).Should().BeTrue();

        // Act
        await _sut.ClearAsync();

        // Assert
        File.Exists(_sut.StateFilePath).Should().BeFalse();
    }

    [Fact]
    public async Task ClearAsync_DoesNotThrowWhenFileDoesNotExist()
    {
        // Act & Assert
        var act = async () => await _sut.ClearAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void StateFilePath_ReturnsCorrectPath()
    {
        // Assert
        _sut.StateFilePath.Should().Be(Path.Combine(_testDirectory, "state.json"));
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingFile()
    {
        // Arrange
        var state1 = CreateTestState();
        state1.Cycle.CompletedFocusSessions = 1;
        await _sut.SaveAsync(state1);

        var state2 = CreateTestState();
        state2.Cycle.CompletedFocusSessions = 2;

        // Act
        await _sut.SaveAsync(state2);
        var result = await _sut.LoadAsync();

        // Assert
        result!.Cycle.CompletedFocusSessions.Should().Be(2);
    }

    [Fact]
    public async Task LoadAsync_ReturnsNullForCorruptedFile()
    {
        // Arrange
        await File.WriteAllTextAsync(_sut.StateFilePath, "not valid json {{{");

        // Act
        var result = await _sut.LoadAsync();

        // Assert
        result.Should().BeNull();
    }

    private static AppState CreateTestState()
    {
        return new AppState
        {
            LastSavedAt = DateTime.Now,
            Cycle = new AppState.CycleState { CompletedFocusSessions = 0 }
        };
    }
}
