using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tomato.Models;

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
    /// Gets or sets the captured todos from the session.
    /// </summary>
    public IReadOnlyList<TodoItem>? CapturedTodos { get; set; }

    /// <summary>
    /// Gets or sets whether this dialog is in "todos only" mode (no feedback section).
    /// </summary>
    public bool IsTodosOnlyMode { get; set; }

    /// <summary>
    /// Gets whether the feedback section should be shown.
    /// </summary>
    public bool ShowFeedbackSection => !IsTodosOnlyMode;

    /// <summary>
    /// Gets whether there are any captured todos.
    /// </summary>
    public bool HasCapturedTodos => CapturedTodos is { Count: > 0 };

    /// <summary>
    /// Gets the captured todos formatted as markdown checkboxes.
    /// </summary>
    public string CapturedTodosText
    {
        get
        {
            if (CapturedTodos is not { Count: > 0 })
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var todo in CapturedTodos)
            {
                sb.AppendLine($"- [ ] {todo.Text}");
            }
            return sb.ToString().TrimEnd();
        }
    }

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
    private void CopyTodos()
    {
        if (!HasCapturedTodos)
            return;

        Clipboard.SetText(CapturedTodosText);
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
