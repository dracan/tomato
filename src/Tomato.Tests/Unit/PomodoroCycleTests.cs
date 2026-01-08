using FluentAssertions;
using Tomato.Models;

namespace Tomato.Tests.Unit;

public class PomodoroCycleTests
{
    [Fact]
    public void FocusSessionsPerCycle_IsFour()
    {
        // Assert
        PomodoroCycle.FocusSessionsPerCycle.Should().Be(4);
    }

    [Fact]
    public void CompletedFocusSessions_InitiallyZero()
    {
        // Arrange
        var cycle = new PomodoroCycle();

        // Assert
        cycle.CompletedFocusSessions.Should().Be(0);
    }

    [Fact]
    public void IncrementFocusCount_IncreasesByOne()
    {
        // Arrange
        var cycle = new PomodoroCycle();

        // Act
        cycle.IncrementFocusCount();

        // Assert
        cycle.CompletedFocusSessions.Should().Be(1);
    }

    [Fact]
    public void IsCycleComplete_FalseWhenLessThanFour()
    {
        // Arrange
        var cycle = new PomodoroCycle();
        cycle.IncrementFocusCount();
        cycle.IncrementFocusCount();
        cycle.IncrementFocusCount();

        // Assert
        cycle.IsCycleComplete.Should().BeFalse();
    }

    [Fact]
    public void IsCycleComplete_TrueWhenFour()
    {
        // Arrange
        var cycle = new PomodoroCycle();
        for (int i = 0; i < 4; i++)
        {
            cycle.IncrementFocusCount();
        }

        // Assert
        cycle.IsCycleComplete.Should().BeTrue();
    }

    [Fact]
    public void NextBreakType_ShortBreak_WhenCycleNotComplete()
    {
        // Arrange
        var cycle = new PomodoroCycle();
        cycle.IncrementFocusCount();

        // Assert
        cycle.NextBreakType.Should().Be(SessionType.ShortBreak);
    }

    [Fact]
    public void NextBreakType_LongBreak_WhenCycleComplete()
    {
        // Arrange
        var cycle = new PomodoroCycle();
        for (int i = 0; i < 4; i++)
        {
            cycle.IncrementFocusCount();
        }

        // Assert
        cycle.NextBreakType.Should().Be(SessionType.LongBreak);
    }

    [Fact]
    public void Reset_SetsCompletedFocusSessionsToZero()
    {
        // Arrange
        var cycle = new PomodoroCycle();
        for (int i = 0; i < 4; i++)
        {
            cycle.IncrementFocusCount();
        }

        // Act
        cycle.Reset();

        // Assert
        cycle.CompletedFocusSessions.Should().Be(0);
        cycle.IsCycleComplete.Should().BeFalse();
    }

    [Fact]
    public void SetCompletedFocusSessions_RestoresValue()
    {
        // Arrange
        var cycle = new PomodoroCycle();

        // Act
        cycle.SetCompletedFocusSessions(3);

        // Assert
        cycle.CompletedFocusSessions.Should().Be(3);
    }
}
