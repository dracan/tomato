using System.Windows;
using System.Windows.Input;
using Tomato.ViewModels;

namespace Tomato.Views;

/// <summary>
/// Interaction logic for SupplementalActivityDialog.xaml
/// </summary>
public partial class SupplementalActivityDialog : Window
{
    public SupplementalActivityDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set the window reference on the ViewModel
        if (DataContext is SupplementalActivityDialogViewModel vm)
        {
            vm.Window = this;
        }

        // Focus the text box
        DescriptionTextBox.Focus();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Allow dragging the window
        DragMove();
    }
}
