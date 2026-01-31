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
    private void StartButton_Click(object sender, RoutedEventArgs e)
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
            vm.StartFocusCommand.Execute(null);
        }
    }

    /// <summary>
    /// Handles the close button click to exit the application.
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
