using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Tomato.Services;

namespace Tomato.Views;

/// <summary>
/// Dialog for configuring Luxafor integration user ID.
/// </summary>
public partial class LuxaforSetupDialog : Window
{
    private readonly ILuxaforService _luxaforService;

    public LuxaforSetupDialog(ILuxaforService luxaforService)
    {
        InitializeComponent();
        _luxaforService = luxaforService;

        Loaded += OnLoaded;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    /// <summary>
    /// Gets whether the user saved a valid user ID.
    /// </summary>
    public bool UserIdSaved { get; private set; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UserIdTextBox.Focus();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private async void OnTestConnection(object sender, RoutedEventArgs e)
    {
        var userId = UserIdTextBox.Text.Trim();
        if (string.IsNullOrEmpty(userId))
        {
            ShowStatus("Please enter a user ID first.", isError: true);
            return;
        }

        TestButton.IsEnabled = false;
        ShowStatus("Testing connection...", isError: false);

        try
        {
            // Temporarily configure the service with the new user ID
            await _luxaforService.ConfigureAsync(userId);
            var success = await _luxaforService.TestConnectionAsync();

            if (success)
            {
                ShowStatus("Connection successful! LED flashed green.", isError: false, isSuccess: true);
            }
            else
            {
                ShowStatus("Connection failed. Check your user ID and try again.", isError: true);
            }
        }
        catch
        {
            ShowStatus("Connection failed. Check your user ID and try again.", isError: true);
        }
        finally
        {
            TestButton.IsEnabled = true;
        }
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        var userId = UserIdTextBox.Text.Trim();
        if (string.IsNullOrEmpty(userId))
        {
            ShowStatus("Please enter a user ID.", isError: true);
            return;
        }

        SaveButton.IsEnabled = false;
        ShowStatus("Saving...", isError: false);

        try
        {
            await _luxaforService.ConfigureAsync(userId);
            UserIdSaved = true;
            DialogResult = true;
            Close();
        }
        catch
        {
            ShowStatus("Failed to save user ID.", isError: true);
            SaveButton.IsEnabled = true;
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowStatus(string message, bool isError, bool isSuccess = false)
    {
        StatusText.Text = message;
        if (isError)
        {
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
        }
        else if (isSuccess)
        {
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Green
        }
        else
        {
            StatusText.Foreground = (Brush)FindResource("TextSecondaryBrush");
        }
    }
}
