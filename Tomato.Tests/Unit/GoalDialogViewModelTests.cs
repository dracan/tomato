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
    public void Estimate_DefaultsTo25Minutes()
    {
        var sut = new GoalDialogViewModel();

        sut.Estimate.Should().Be("25");
        sut.EstimateMinutes.Should().Be(25);
    }

    [Fact]
    public void Estimate_StartsWithTheGivenMinutes()
    {
        var sut = new GoalDialogViewModel(estimateMinutes: 15);

        sut.Estimate.Should().Be("15");
        sut.EstimateMinutes.Should().Be(15);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("45", 45)]
    [InlineData(" 120 ", 120)]
    public void Estimate_WholeMinutesFrom1To120_CanBeConfirmed(string text, int minutes)
    {
        var sut = new GoalDialogViewModel { Estimate = text };

        sut.ConfirmCommand.CanExecute(null).Should().BeTrue();
        sut.EstimateMinutes.Should().Be(minutes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("121")]
    [InlineData("-5")]
    [InlineData("12.5")]
    [InlineData("abc")]
    public void Estimate_OutsideTheAllowedRange_CannotBeConfirmed(string text)
    {
        var sut = new GoalDialogViewModel { Estimate = text };

        sut.ConfirmCommand.CanExecute(null).Should().BeFalse();
        sut.IsEstimateValid.Should().BeFalse();
    }

    [Fact]
    public void Estimate_WhenItBecomesValid_RaisesConfirmCanExecuteChanged()
    {
        var sut = new GoalDialogViewModel { Estimate = "" };
        var raised = false;
        sut.ConfirmCommand.CanExecuteChanged += (_, _) => raised = true;

        sut.Estimate = "10";

        raised.Should().BeTrue();
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
