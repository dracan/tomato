using System.Windows;
using System.Windows.Controls;
using Tomato.ViewModels;

namespace Tomato.Views;

/// <summary>
/// Interaction logic for TimerView.xaml
/// </summary>
public partial class TimerView : UserControl
{
    public TimerView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the Start/Resume button click.
    /// Starts a focus session when idle, or resumes when paused.
    /// </summary>
    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not TimerViewModel vm)
            return;

        if (vm.IsPaused)
        {
            vm.ResumeCommand.Execute(null);
        }
        else
        {
            // Idle state - start a focus session
            await vm.StartFocusCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Handles the close button click to exit the application.
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    /// <summary>
    /// Auto-focuses the todo input TextBox when it becomes visible.
    /// </summary>
    private void TodoInputTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox textBox && textBox.IsVisible)
        {
            textBox.Focus();
            textBox.SelectAll();
        }
    }
}
