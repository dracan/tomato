using System.Windows;
using System.Windows.Input;
using Tomato.ViewModels;

namespace Tomato.Views;

/// <summary>
/// Interaction logic for GoalDialog.xaml
/// </summary>
public partial class GoalDialog : Window
{
    public GoalDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set the window reference on the ViewModel
        if (DataContext is GoalDialogViewModel vm)
        {
            vm.Window = this;
        }

        // Focus the text box
        GoalTextBox.Focus();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Allow dragging the window
        DragMove();
    }
}
