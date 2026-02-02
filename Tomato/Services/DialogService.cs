using System.Windows;
using Tomato.ViewModels;
using Tomato.Views;

namespace Tomato.Services;

/// <summary>
/// WPF implementation of IDialogService.
/// </summary>
public sealed class DialogService : IDialogService
{
    private readonly Window _owner;

    public DialogService(Window owner)
    {
        _owner = owner;
    }

    /// <inheritdoc />
    public Task<GoalDialogResult> ShowGoalDialogAsync()
    {
        var viewModel = new GoalDialogViewModel();
        var dialog = new GoalDialog
        {
            DataContext = viewModel,
            Owner = _owner
        };

        dialog.ShowDialog();

        var confirmed = viewModel.DialogResult == true;
        var goal = confirmed ? viewModel.Goal : null;

        return Task.FromResult(new GoalDialogResult(confirmed, goal));
    }

    /// <inheritdoc />
    public Task<ResultsDialogResult> ShowResultsDialogAsync(string? goal)
    {
        var viewModel = new ResultsDialogViewModel { Goal = goal };
        var dialog = new ResultsDialog
        {
            DataContext = viewModel,
            Owner = _owner
        };

        dialog.ShowDialog();

        var confirmed = viewModel.DialogResult == true;
        var results = confirmed ? viewModel.Results : null;
        var rating = confirmed ? viewModel.Rating : null;

        return Task.FromResult(new ResultsDialogResult(confirmed, results, rating));
    }
}
