using FluentAssertions;
using Tomato.ViewModels;

namespace Tomato.Tests.Unit;

public class ResultsDialogViewModelTests
{
    [Fact]
    public void Results_InitiallyEmpty()
    {
        // Arrange
        var sut = new ResultsDialogViewModel();

        // Assert
        sut.Results.Should().Be(string.Empty);
    }

    [Fact]
    public void Goal_InitiallyNull()
    {
        // Arrange
        var sut = new ResultsDialogViewModel();

        // Assert
        sut.Goal.Should().BeNull();
    }

    [Fact]
    public void DialogResult_InitiallyNull()
    {
        // Arrange
        var sut = new ResultsDialogViewModel();

        // Assert
        sut.DialogResult.Should().BeNull();
    }

    [Fact]
    public void ConfirmCommand_WithoutWindow_SetsDialogResultTrue()
    {
        // Arrange
        var sut = new ResultsDialogViewModel();
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
        var sut = new ResultsDialogViewModel();
        // Note: Window is null, so Close() won't be called, but DialogResult should still be set

        // Act
        sut.CancelCommand.Execute(null);

        // Assert
        sut.DialogResult.Should().BeFalse();
    }

    [Fact]
    public void Results_CanBeSet()
    {
        // Arrange
        var sut = new ResultsDialogViewModel();
        var results = "Completed all tasks successfully";

        // Act
        sut.Results = results;

        // Assert
        sut.Results.Should().Be(results);
    }

    [Fact]
    public void Goal_CanBeSet()
    {
        // Arrange
        var sut = new ResultsDialogViewModel();
        var goal = "Complete the feature";

        // Act
        sut.Goal = goal;

        // Assert
        sut.Goal.Should().Be(goal);
    }

    [Fact]
    public void Results_RaisesPropertyChanged()
    {
        // Arrange
        var sut = new ResultsDialogViewModel();
        var propertyChangedRaised = false;
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ResultsDialogViewModel.Results))
                propertyChangedRaised = true;
        };

        // Act
        sut.Results = "New results";

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void Goal_RaisesPropertyChanged()
    {
        // Arrange
        var sut = new ResultsDialogViewModel();
        var propertyChangedRaised = false;
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ResultsDialogViewModel.Goal))
                propertyChangedRaised = true;
        };

        // Act
        sut.Goal = "New goal";

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }
}
