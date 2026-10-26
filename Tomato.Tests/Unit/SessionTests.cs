using FluentAssertions;
using Tomato.Models;

namespace Tomato.Tests.Unit;

public class SessionTests
{
    #region Goal Property

    [Fact]
    public void CreateFocus_WithGoal_StoresGoal()
    {
        // Arrange
        var goal = "Complete code review";

        // Act
        var session = Session.CreateFocus(goal);

        // Assert
        session.Goal.Should().Be(goal);
    }

    [Fact]
    public void CreateFocus_WithDurationAndGoal_StoresBoth()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(15);
        var goal = "Write unit tests";

        // Act
        var session = Session.CreateFocus(duration, goal);

        // Assert
        session.Duration.Should().Be(duration);
        session.Goal.Should().Be(goal);
    }

    [Fact]
    public void CreateFocus_WithoutGoal_HasNullGoal()
    {
        // Act
        var session = Session.CreateFocus();

        // Assert
        session.Goal.Should().BeNull();
    }

    [Fact]
    public void CreateFocus_WithNullGoal_HasNullGoal()
    {
        // Act
        var session = Session.CreateFocus(goal: null);

        // Assert
        session.Goal.Should().BeNull();
    }

    [Fact]
    public void CreateFocus_WithEmptyGoal_StoresEmptyString()
    {
        // Act
        var session = Session.CreateFocus(string.Empty);

        // Assert
        session.Goal.Should().Be(string.Empty);
    }

    [Fact]
    public void CreateShortBreak_HasNullGoal()
    {
        // Act
        var session = Session.CreateShortBreak();

        // Assert
        session.Goal.Should().BeNull();
    }

    [Fact]
    public void CreateLongBreak_HasNullGoal()
    {
        // Act
        var session = Session.CreateLongBreak();

        // Assert
        session.Goal.Should().BeNull();
    }

    #endregion

    #region Overtime and Time Spent

    [Fact]
    public void SetTimeRemaining_BelowZero_WithoutOvertime_StopsAtZero()
    {
        var session = Session.CreateFocus(TimeSpan.FromMinutes(15));

        session.SetTimeRemaining(TimeSpan.FromMinutes(-2));

        session.TimeRemaining.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void SetTimeRemaining_BelowZero_WithOvertime_KeepsNegativeTime()
    {
        var session = Session.CreateFocus(TimeSpan.FromMinutes(15), allowsOvertime: true);

        session.SetTimeRemaining(TimeSpan.FromMinutes(-2));

        session.TimeRemaining.Should().Be(TimeSpan.FromMinutes(-2));
    }

    [Fact]
    public void TimeSpent_FinishedEarly_IsEstimateMinusTimeRemaining()
    {
        var session = Session.CreateFocus(TimeSpan.FromMinutes(15));

        session.SetTimeRemaining(TimeSpan.FromMinutes(3));

        session.TimeSpent.Should().Be(TimeSpan.FromMinutes(12));
    }

    [Fact]
    public void TimeSpent_InOvertime_IncludesTheOvertime()
    {
        var session = Session.CreateFocus(TimeSpan.FromMinutes(15), allowsOvertime: true);

        session.SetTimeRemaining(TimeSpan.FromMinutes(-4));

        session.TimeSpent.Should().Be(TimeSpan.FromMinutes(19));
    }

    [Fact]
    public void IsInOvertime_RunningPastZeroWithOvertime_IsTrue()
    {
        var session = Session.CreateFocus(TimeSpan.FromMinutes(15), allowsOvertime: true);
        session.Status = SessionStatus.Running;

        session.SetTimeRemaining(TimeSpan.FromSeconds(-1));

        session.IsInOvertime.Should().BeTrue();
    }

    [Fact]
    public void IsInOvertime_BeforeEstimateRunsOut_IsFalse()
    {
        var session = Session.CreateFocus(TimeSpan.FromMinutes(15), allowsOvertime: true);
        session.Status = SessionStatus.Running;

        session.SetTimeRemaining(TimeSpan.FromMinutes(1));

        session.IsInOvertime.Should().BeFalse();
    }

    [Fact]
    public void IsInOvertime_AfterSessionCompleted_IsFalse()
    {
        var session = Session.CreateFocus(TimeSpan.FromMinutes(15), allowsOvertime: true);
        session.SetTimeRemaining(TimeSpan.FromMinutes(-1));

        session.Status = SessionStatus.Completed;

        session.IsInOvertime.Should().BeFalse();
    }

    #endregion
}
