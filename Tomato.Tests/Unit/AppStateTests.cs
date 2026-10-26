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
    public void SessionRecord_SavedAndRestored_KeepsEstimateAndTimeSpent()
    {
        // Arrange
        var today = DailyStatistics.Create(new DateOnly(2024, 1, 15));
        today.RecordFocusSession(TimeSpan.FromMinutes(18));
        today.AddSessionRecord(new SessionRecord
        {
            Goal = "Fix bug",
            Estimate = TimeSpan.FromMinutes(15),
            TimeSpent = TimeSpan.FromMinutes(18)
        });

        // Act
        var restored = AppState.FromCurrentState(null, new PomodoroCycle(), today, [], DateTime.Now)
            .RestoreStatistics()!;

        // Assert
        var record = restored.SessionRecords.Should().ContainSingle().Subject;
        record.Estimate.Should().Be(TimeSpan.FromMinutes(15));
        record.TimeSpent.Should().Be(TimeSpan.FromMinutes(18));
    }

    [Fact]
    public void SessionRecord_SavedBeforeEstimates_RestoresItsLengthAsTimeSpentWithNoEstimate()
    {
        // Arrange - a record saved by an older version only has a Duration
        var state = new AppState
        {
            StatisticsHistory =
            [
                new AppState.DailyStatisticsState
                {
                    Date = new DateOnly(2024, 1, 14),
                    FocusSessionsCompleted = 1,
                    TotalFocusTime = TimeSpan.FromMinutes(25),
                    SessionRecords = [new AppState.SessionRecordState { Duration = TimeSpan.FromMinutes(25) }]
                }
            ]
        };

        // Act
        var record = state.RestoreStatisticsHistory().Single().SessionRecords.Single();

        // Assert
        record.Estimate.Should().BeNull();
        record.TimeSpent.Should().Be(TimeSpan.FromMinutes(25));
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
