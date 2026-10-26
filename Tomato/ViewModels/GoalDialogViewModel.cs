using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace Tomato.ViewModels;

/// <summary>
/// ViewModel for the goal entry dialog.
/// </summary>
public partial class GoalDialogViewModel : ObservableObject
{
    /// <summary>
    /// The shortest Estimate, in minutes.
    /// </summary>
    public const int MinEstimateMinutes = 1;

    /// <summary>
    /// The longest Estimate, in minutes.
    /// </summary>
    public const int MaxEstimateMinutes = 120;

    [ObservableProperty]
    private string _goal = string.Empty;

    /// <summary>
    /// The Estimate as typed, in whole minutes.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEstimateValid))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string _estimate;

    public GoalDialogViewModel(int estimateMinutes = 25, bool focusEstimate = false)
    {
        _estimate = estimateMinutes.ToString(CultureInfo.InvariantCulture);
        FocusEstimate = focusEstimate;
    }

    /// <summary>
    /// Gets whether the dialog should open with the Estimate field focused instead of the goal.
    /// </summary>
    public bool FocusEstimate { get; }

    /// <summary>
    /// Gets whether the Estimate is a whole number of minutes in the allowed range.
    /// </summary>
    public bool IsEstimateValid => TryParseEstimate(out _);

    /// <summary>
    /// Gets the Estimate in minutes, or 0 when it isn't valid.
    /// </summary>
    public int EstimateMinutes => TryParseEstimate(out var minutes) ? minutes : 0;

    /// <summary>
    /// Gets or sets the dialog result (true = confirmed, false = cancelled).
    /// </summary>
    public bool? DialogResult { get; private set; }

    /// <summary>
    /// Gets or sets the window associated with this ViewModel.
    /// </summary>
    public Window? Window { get; set; }

    [RelayCommand(CanExecute = nameof(IsEstimateValid))]
    private void Confirm()
    {
        DialogResult = true;
        Window?.Close();
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        Window?.Close();
    }

    private bool TryParseEstimate(out int minutes) =>
        int.TryParse(Estimate.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out minutes) &&
        minutes is >= MinEstimateMinutes and <= MaxEstimateMinutes;
}
