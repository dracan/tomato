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

    #region Rating Tests

    [Fact]
    public void Rating_InitiallyNull()
    {
        // Arrange
        var sut = new ResultsDialogViewModel();

        // Assert
        sut.Rating.Should().BeNull();
    }

    [Fact]
    public void SetRatingCommand_SetsRating()
    {
        // Arrange
        var sut = new ResultsDialogViewModel();

        // Act
        sut.SetRatingCommand.Execute("3");

        // Assert
        sut.Rating.Should().Be(3);
    }

    [Fact]
    public void SetRatingCommand_WithSameValue_TogglesOff()
    {
        // Arrange
        var sut = new ResultsDialogViewModel();
        sut.SetRatingCommand.Execute("4");

        // Act
        sut.SetRatingCommand.Execute("4");

        // Assert
        sut.Rating.Should().BeNull();
    }

    [Fact]
    public void SetRatingCommand_WithDifferentValue_ChangesRating()
    {
        // Arrange
        var sut = new ResultsDialogViewModel();
        sut.SetRatingCommand.Execute("2");

        // Act
        sut.SetRatingCommand.Execute("5");

        // Assert
        sut.Rating.Should().Be(5);
    }

    [Theory]
    [InlineData("1", true, false, false, false, false)]
    [InlineData("2", true, true, false, false, false)]
    [InlineData("3", true, true, true, false, false)]
    [InlineData("4", true, true, true, true, false)]
    [InlineData("5", true, true, true, true, true)]
    public void IsStarSelected_ReflectsRatingCorrectly(
        string rating,
        bool star1, bool star2, bool star3, bool star4, bool star5)
    {
        // Arrange
        var sut = new ResultsDialogViewModel();

        // Act
        sut.SetRatingCommand.Execute(rating);

        // Assert
        sut.IsStar1Selected.Should().Be(star1);
        sut.IsStar2Selected.Should().Be(star2);
        sut.IsStar3Selected.Should().Be(star3);
        sut.IsStar4Selected.Should().Be(star4);
        sut.IsStar5Selected.Should().Be(star5);
    }

    [Fact]
    public void IsStarSelected_WhenNoRating_AllFalse()
    {
        // Arrange
        var sut = new ResultsDialogViewModel();

        // Assert
        sut.IsStar1Selected.Should().BeFalse();
        sut.IsStar2Selected.Should().BeFalse();
        sut.IsStar3Selected.Should().BeFalse();
        sut.IsStar4Selected.Should().BeFalse();
        sut.IsStar5Selected.Should().BeFalse();
    }

    [Fact]
    public void Rating_RaisesPropertyChangedForStarProperties()
    {
        // Arrange
        var sut = new ResultsDialogViewModel();
        var changedProperties = new List<string>();
        sut.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        sut.SetRatingCommand.Execute("3");

        // Assert
        changedProperties.Should().Contain(nameof(ResultsDialogViewModel.Rating));
        changedProperties.Should().Contain(nameof(ResultsDialogViewModel.IsStar1Selected));
        changedProperties.Should().Contain(nameof(ResultsDialogViewModel.IsStar2Selected));
        changedProperties.Should().Contain(nameof(ResultsDialogViewModel.IsStar3Selected));
        changedProperties.Should().Contain(nameof(ResultsDialogViewModel.IsStar4Selected));
        changedProperties.Should().Contain(nameof(ResultsDialogViewModel.IsStar5Selected));
    }

    #endregion
}
