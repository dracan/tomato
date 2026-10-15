using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Tomato.Services;

namespace Tomato.Views;

/// <summary>
/// Dialog for configuring Slack integration token.
/// </summary>
public partial class SlackSetupDialog : Window
{
    private readonly ISlackService _slackService;

    public SlackSetupDialog(ISlackService slackService)
    {
        InitializeComponent();
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                OnCancel(this, new RoutedEventArgs());
            }
        };
        _slackService = slackService;

        Loaded += OnLoaded;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    /// <summary>
    /// Gets whether the user saved a valid token.
    /// </summary>
    public bool TokenSaved { get; private set; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TokenTextBox.Focus();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private async void OnTestConnection(object sender, RoutedEventArgs e)
    {
        var token = TokenTextBox.Text.Trim();
        if (string.IsNullOrEmpty(token))
        {
            ShowStatus("Please enter a token first.", isError: true);
            return;
        }

        TestButton.IsEnabled = false;
        ShowStatus("Testing connection...", isError: false);

        try
        {
            // Only test the token here; it is saved when the user clicks Save
            var result = await _slackService.TestConnectionAsync(token);

            if (result.Success)
            {
                ShowStatus("Connection successful!", isError: false, isSuccess: true);
            }
            else
            {
                ShowStatus(result.Message ?? "Connection failed. Check your token and try again.", isError: true);
            }
        }
        catch
        {
            ShowStatus("Connection failed. Check your token and try again.", isError: true);
        }
        finally
        {
            TestButton.IsEnabled = true;
        }
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        var token = TokenTextBox.Text.Trim();
        if (string.IsNullOrEmpty(token))
        {
            ShowStatus("Please enter a token.", isError: true);
            return;
        }

        SaveButton.IsEnabled = false;
        ShowStatus("Saving...", isError: false);

        try
        {
            await _slackService.ConfigureAsync(token);
            TokenSaved = true;
            DialogResult = true;
            Close();
        }
        catch
        {
            ShowStatus("Failed to save token.", isError: true);
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
