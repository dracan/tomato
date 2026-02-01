using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace Tomato.ViewModels;

/// <summary>
/// ViewModel for the goal entry dialog.
/// </summary>
public partial class GoalDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _goal = string.Empty;

    /// <summary>
    /// Gets or sets the dialog result (true = confirmed, false = cancelled).
    /// </summary>
    public bool? DialogResult { get; private set; }

    /// <summary>
    /// Gets or sets the window associated with this ViewModel.
    /// </summary>
    public Window? Window { get; set; }

    [RelayCommand]
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
}
