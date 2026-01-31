using FluentAssertions;
using Tomato.Models;

namespace Tomato.Tests.Unit;

public class DailyStatisticsTests
{
    [Fact]
    public void Create_InitializesWithZeroValues()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 15);

        // Act
        var stats = DailyStatistics.Create(date);

        // Assert
        stats.Date.Should().Be(date);
        stats.FocusSessionsCompleted.Should().Be(0);
        stats.TotalFocusTime.Should().Be(TimeSpan.Zero);
        stats.TotalBreakTime.Should().Be(TimeSpan.Zero);
        stats.CyclesCompleted.Should().Be(0);
    }

    [Fact]
    public void RecordFocusSession_IncrementsFocusSessionsCompleted()
    {
        // Arrange
        var stats = DailyStatistics.Create(DateOnly.FromDateTime(DateTime.Now));

        // Act
        stats.RecordFocusSession(TimeSpan.FromMinutes(25));

        // Assert
        stats.FocusSessionsCompleted.Should().Be(1);
    }

    [Fact]
    public void RecordFocusSession_AddsTotalFocusTime()
    {
        // Arrange
        var stats = DailyStatistics.Create(DateOnly.FromDateTime(DateTime.Now));
        var duration = TimeSpan.FromMinutes(25);

        // Act
        stats.RecordFocusSession(duration);

        // Assert
        stats.TotalFocusTime.Should().Be(duration);
    }

    [Fact]
    public void RecordFocusSession_AccumulatesMultipleSessions()
    {
        // Arrange
        var stats = DailyStatistics.Create(DateOnly.FromDateTime(DateTime.Now));
        var duration = TimeSpan.FromMinutes(25);

        // Act
        stats.RecordFocusSession(duration);
        stats.RecordFocusSession(duration);
        stats.RecordFocusSession(duration);

        // Assert
        stats.FocusSessionsCompleted.Should().Be(3);
        stats.TotalFocusTime.Should().Be(TimeSpan.FromMinutes(75));
    }

    [Fact]
    public void RecordBreakSession_AddsTotalBreakTime()
    {
        // Arrange
        var stats = DailyStatistics.Create(DateOnly.FromDateTime(DateTime.Now));
        var duration = TimeSpan.FromMinutes(5);

        // Act
        stats.RecordBreakSession(duration);

        // Assert
        stats.TotalBreakTime.Should().Be(duration);
    }

    [Fact]
    public void RecordBreakSession_AccumulatesMultipleSessions()
    {
        // Arrange
        var stats = DailyStatistics.Create(DateOnly.FromDateTime(DateTime.Now));

        // Act
        stats.RecordBreakSession(TimeSpan.FromMinutes(5));
        stats.RecordBreakSession(TimeSpan.FromMinutes(5));
        stats.RecordBreakSession(TimeSpan.FromMinutes(15));

        // Assert
        stats.TotalBreakTime.Should().Be(TimeSpan.FromMinutes(25));
    }

    [Fact]
    public void RecordCycleCompleted_IncrementsCyclesCompleted()
    {
        // Arrange
        var stats = DailyStatistics.Create(DateOnly.FromDateTime(DateTime.Now));

        // Act
        stats.RecordCycleCompleted();

        // Assert
        stats.CyclesCompleted.Should().Be(1);
    }

    [Fact]
    public void RecordCycleCompleted_AccumulatesMultipleCycles()
    {
        // Arrange
        var stats = DailyStatistics.Create(DateOnly.FromDateTime(DateTime.Now));

        // Act
        stats.RecordCycleCompleted();
        stats.RecordCycleCompleted();

        // Assert
        stats.CyclesCompleted.Should().Be(2);
    }
}
