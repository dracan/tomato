using FluentAssertions;
using Tomato.Services;

namespace Tomato.Tests.Unit;

public class TimerServiceTests : IDisposable
{
    private readonly TimerService _sut;

    public TimerServiceTests()
    {
        // Use ThreadPoolIntervalTimer for tests since DispatcherTimer requires WPF message loop
        _sut = new TimerService(new ThreadPoolIntervalTimer());
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    [Fact]
    public void Start_SetsIsRunningToTrue()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act
        _sut.Start(duration);

        // Assert
        _sut.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void Start_SetsRemainingToDuration()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(25);

        // Act
        _sut.Start(duration);

        // Assert
        _sut.Remaining.Should().Be(duration);
    }

    [Fact]
    public void Pause_SetsIsRunningToFalse()
    {
        // Arrange
        _sut.Start(TimeSpan.FromSeconds(5));

        // Act
        _sut.Pause();

        // Assert
        _sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Pause_PreservesRemainingTime()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(25);
        _sut.Start(duration);

        // Act
        _sut.Pause();

        // Assert
        _sut.Remaining.Should().BeCloseTo(duration, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Stop_SetsIsRunningToFalse()
    {
        // Arrange
        _sut.Start(TimeSpan.FromSeconds(5));

        // Act
        _sut.Stop();

        // Assert
        _sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Stop_ResetsRemainingToZero()
    {
        // Arrange
        _sut.Start(TimeSpan.FromSeconds(5));

        // Act
        _sut.Stop();

        // Assert
        _sut.Remaining.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Resume_SetsIsRunningToTrue()
    {
        // Arrange
        var remaining = TimeSpan.FromMinutes(10);

        // Act
        _sut.Resume(remaining);

        // Assert
        _sut.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void Resume_SetsRemainingToProvidedValue()
    {
        // Arrange
        var remaining = TimeSpan.FromMinutes(10);

        // Act
        _sut.Resume(remaining);

        // Assert
        _sut.Remaining.Should().Be(remaining);
    }

    [Fact]
    public async Task Tick_RaisesEventEverySecond()
    {
        // Arrange
        var tickCount = 0;
        _sut.Tick += (_, _) => tickCount++;

        // Act
        _sut.Start(TimeSpan.FromSeconds(3));
        await Task.Delay(2500, TestContext.Current.CancellationToken); // Wait for ~2 ticks

        // Assert
        tickCount.Should().BeGreaterThanOrEqualTo(2);
        _sut.Stop();
    }

    [Fact]
    public async Task Completed_RaisesEventWhenTimerReachesZero()
    {
        // Arrange
        var completed = false;
        _sut.Completed += (_, _) => completed = true;

        // Act
        _sut.Start(TimeSpan.FromSeconds(1));
        await Task.Delay(1500, TestContext.Current.CancellationToken); // Wait for completion

        // Assert
        completed.Should().BeTrue();
    }

    [Fact]
    public async Task Tick_ProvidesCorrectElapsedAndRemainingTimes()
    {
        // Arrange
        TimeSpan? lastElapsed = null;
        TimeSpan? lastRemaining = null;
        _sut.Tick += (_, e) =>
        {
            lastElapsed = e.Elapsed;
            lastRemaining = e.Remaining;
        };

        var duration = TimeSpan.FromSeconds(3);

        // Act
        _sut.Start(duration);
        await Task.Delay(1500, TestContext.Current.CancellationToken);
        _sut.Stop();

        // Assert
        lastElapsed.Should().NotBeNull();
        lastRemaining.Should().NotBeNull();
        lastElapsed!.Value.Should().BeGreaterThan(TimeSpan.Zero);
        lastRemaining!.Value.Should().BeLessThan(duration);
    }

    [Fact]
    public async Task CountPastZero_RaisesCompletedOnceAndKeepsCountingBelowZero()
    {
        // Arrange
        var completedCount = 0;
        TimeSpan? lastRemaining = null;
        _sut.Completed += (_, _) => completedCount++;
        _sut.Tick += (_, e) => lastRemaining = e.Remaining;

        // Act
        _sut.Start(TimeSpan.FromMilliseconds(300), countPastZero: true);
        await Task.Delay(1500, TestContext.Current.CancellationToken);

        // Assert
        completedCount.Should().Be(1);
        _sut.IsRunning.Should().BeTrue();
        lastRemaining.Should().BeLessThan(TimeSpan.Zero);
        _sut.Stop();
    }

    [Fact]
    public async Task CountPastZero_Pause_KeepsNegativeRemaining()
    {
        // Arrange
        _sut.Start(TimeSpan.FromMilliseconds(100), countPastZero: true);
        await Task.Delay(700, TestContext.Current.CancellationToken);

        // Act
        _sut.Pause();

        // Assert
        _sut.Remaining.Should().BeLessThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task CountPastZero_ResumeAlreadyBelowZero_DoesNotRaiseCompletedAgain()
    {
        // Arrange
        var completed = false;
        TimeSpan? lastRemaining = null;
        _sut.Completed += (_, _) => completed = true;
        _sut.Tick += (_, e) => lastRemaining = e.Remaining;

        // Act
        _sut.Resume(TimeSpan.FromMinutes(-3), countPastZero: true);
        await Task.Delay(700, TestContext.Current.CancellationToken);

        // Assert
        completed.Should().BeFalse();
        lastRemaining.Should().BeLessThan(TimeSpan.FromMinutes(-3));
        _sut.Stop();
    }

    [Fact]
    public void IsRunning_IsFalseInitially()
    {
        // Assert
        _sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Remaining_IsZeroInitially()
    {
        // Assert
        _sut.Remaining.Should().Be(TimeSpan.Zero);
    }
}
