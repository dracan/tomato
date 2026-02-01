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
}
