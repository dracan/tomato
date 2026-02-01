using System.Windows;
using System.Windows.Input;
using Tomato.ViewModels;

namespace Tomato.Views;

/// <summary>
/// Interaction logic for ResultsDialog.xaml
/// </summary>
public partial class ResultsDialog : Window
{
    public ResultsDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set the window reference on the ViewModel
        if (DataContext is ResultsDialogViewModel vm)
        {
            vm.Window = this;
        }

        // Focus the text box
        ResultsTextBox.Focus();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Allow dragging the window
        DragMove();
    }
}
