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

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
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
        await File.WriteAllTextAsync(_sut.StateFilePath, "not valid json {{{", TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.LoadAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_CorruptedFile_IsKeptAsBackupAndNotOverwritten()
    {
        // Arrange
        await File.WriteAllTextAsync(_sut.StateFilePath, "not valid json {{{", TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.LoadAsync();
        await _sut.SaveAsync(CreateTestState());

        // Assert
        result.Should().BeNull();
        var backups = Directory.GetFiles(_testDirectory, "state.corrupt-*.json");
        backups.Should().ContainSingle();
        (await File.ReadAllTextAsync(backups[0], TestContext.Current.CancellationToken))
            .Should().Be("not valid json {{{");
    }

    [Fact]
    public async Task LoadAsync_EmptyFile_IsKeptAsBackup()
    {
        // Arrange - e.g. power loss during a save
        await File.WriteAllTextAsync(_sut.StateFilePath, "", TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.LoadAsync();

        // Assert
        result.Should().BeNull();
        Directory.GetFiles(_testDirectory, "state.corrupt-*.json").Should().ContainSingle();
    }

    [Fact]
    public async Task LoadAsync_UnreadableFile_ReturnsNullAndDoesNotOverwriteItLater()
    {
        // Arrange - another process (antivirus, sync client) holds the file exclusively
        var original = CreateTestState();
        original.Cycle.CompletedFocusSessions = 3;
        await _sut.SaveAsync(original);

        AppState? result;
        await using (new FileStream(_sut.StateFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            // Act
            result = await _sut.LoadAsync();
        }

        await _sut.SaveAsync(CreateTestState());

        // Assert - nothing loaded, and the unread history was not replaced
        result.Should().BeNull();
        var reloaded = await new PersistenceService(_testDirectory).LoadAsync();
        reloaded!.Cycle.CompletedFocusSessions.Should().Be(3);
    }

    [Fact]
    public async Task SaveAsync_DoesNotLeaveTemporaryFiles()
    {
        // Act
        await _sut.SaveAsync(CreateTestState());

        // Assert
        Directory.GetFiles(_testDirectory).Should().ContainSingle()
            .Which.Should().Be(_sut.StateFilePath);
    }

    [Fact]
    public async Task SaveAsync_ConcurrentSaves_AllSucceedAndLastOneWins()
    {
        // Arrange
        var states = Enumerable.Range(1, 20)
            .Select(i => new AppState { Cycle = new AppState.CycleState { CompletedFocusSessions = i } })
            .ToList();

        // Act
        await Task.WhenAll(states.Select(_sut.SaveAsync));
        var result = await _sut.LoadAsync();

        // Assert
        result!.Cycle.CompletedFocusSessions.Should().Be(20);
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
