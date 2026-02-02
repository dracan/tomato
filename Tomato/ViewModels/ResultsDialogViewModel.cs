using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace Tomato.ViewModels;

/// <summary>
/// ViewModel for the session results dialog.
/// </summary>
public partial class ResultsDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _results = string.Empty;

    [ObservableProperty]
    private string? _goal;

    [ObservableProperty]
    private int? _rating;

    /// <summary>
    /// Gets or sets the dialog result (true = confirmed, false = cancelled).
    /// </summary>
    public bool? DialogResult { get; private set; }

    /// <summary>
    /// Gets or sets the window associated with this ViewModel.
    /// </summary>
    public Window? Window { get; set; }

    /// <summary>
    /// Gets whether star 1 should be filled.
    /// </summary>
    public bool IsStar1Selected => Rating >= 1;

    /// <summary>
    /// Gets whether star 2 should be filled.
    /// </summary>
    public bool IsStar2Selected => Rating >= 2;

    /// <summary>
    /// Gets whether star 3 should be filled.
    /// </summary>
    public bool IsStar3Selected => Rating >= 3;

    /// <summary>
    /// Gets whether star 4 should be filled.
    /// </summary>
    public bool IsStar4Selected => Rating >= 4;

    /// <summary>
    /// Gets whether star 5 should be filled.
    /// </summary>
    public bool IsStar5Selected => Rating >= 5;

    partial void OnRatingChanged(int? value)
    {
        OnPropertyChanged(nameof(IsStar1Selected));
        OnPropertyChanged(nameof(IsStar2Selected));
        OnPropertyChanged(nameof(IsStar3Selected));
        OnPropertyChanged(nameof(IsStar4Selected));
        OnPropertyChanged(nameof(IsStar5Selected));
    }

    [RelayCommand]
    private void SetRating(object? parameter)
    {
        if (parameter is not string str || !int.TryParse(str, out var value))
            return;

        // Toggle off if clicking the same star
        Rating = Rating == value ? null : value;
    }

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
