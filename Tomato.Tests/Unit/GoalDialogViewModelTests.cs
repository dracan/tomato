using FluentAssertions;
using Tomato.ViewModels;

namespace Tomato.Tests.Unit;

public class GoalDialogViewModelTests
{
    [Fact]
    public void Goal_InitiallyEmpty()
    {
        // Arrange
        var sut = new GoalDialogViewModel();

        // Assert
        sut.Goal.Should().Be(string.Empty);
    }

    [Fact]
    public void DialogResult_InitiallyNull()
    {
        // Arrange
        var sut = new GoalDialogViewModel();

        // Assert
        sut.DialogResult.Should().BeNull();
    }

    [Fact]
    public void ConfirmCommand_WithoutWindow_SetsDialogResultTrue()
    {
        // Arrange
        var sut = new GoalDialogViewModel();
        // Note: Window is null, so Close() won't be called, but DialogResult should still be set

        // Act
        sut.ConfirmCommand.Execute(null);

        // Assert
        sut.DialogResult.Should().BeTrue();
    }

    [Fact]
    public void CancelCommand_WithoutWindow_SetsDialogResultFalse()
    {
        // Arrange
        var sut = new GoalDialogViewModel();
        // Note: Window is null, so Close() won't be called, but DialogResult should still be set

        // Act
        sut.CancelCommand.Execute(null);

        // Assert
        sut.DialogResult.Should().BeFalse();
    }

    [Fact]
    public void Goal_CanBeSet()
    {
        // Arrange
        var sut = new GoalDialogViewModel();
        var goal = "Complete the feature";

        // Act
        sut.Goal = goal;

        // Assert
        sut.Goal.Should().Be(goal);
    }

    [Fact]
    public void Goal_RaisesPropertyChanged()
    {
        // Arrange
        var sut = new GoalDialogViewModel();
        var propertyChangedRaised = false;
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(GoalDialogViewModel.Goal))
                propertyChangedRaised = true;
        };

        // Act
        sut.Goal = "New goal";

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }
}
