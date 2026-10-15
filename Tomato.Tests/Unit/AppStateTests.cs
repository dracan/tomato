using FluentAssertions;
using Tomato.Models;

namespace Tomato.Tests.Unit;

public class AppStateTests
{
    [Fact]
    public void FromCurrentState_BreakOnlyDay_IsKeptInHistory()
    {
        // Arrange - a day with only breaks recorded
        var today = DailyStatistics.Create(new DateOnly(2024, 1, 15));
        today.RecordBreakSession(TimeSpan.FromMinutes(10));

        // Act
        var state = AppState.FromCurrentState(null, new PomodoroCycle(), today, [], DateTime.Now);

        // Assert
        state.StatisticsHistory.Should().ContainSingle()
            .Which.TotalBreakTime.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void FromCurrentState_EmptyDay_IsNotAddedToHistory()
    {
        // Arrange
        var today = DailyStatistics.Create(new DateOnly(2024, 1, 15));

        // Act
        var state = AppState.FromCurrentState(null, new PomodoroCycle(), today, [], DateTime.Now);

        // Assert
        state.StatisticsHistory.Should().BeEmpty();
    }
}
