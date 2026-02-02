using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Tomato.Services;

namespace Tomato.Views;

/// <summary>
/// Dialog shown when a newer version of the application is available.
/// </summary>
public partial class UpdateAvailableDialog : Window
{
    private readonly string _releaseUrl;

    public UpdateAvailableDialog(UpdateCheckResult updateResult)
    {
        InitializeComponent();

        _releaseUrl = updateResult.ReleaseUrl;

        CurrentVersionText.Text = FormatVersion(updateResult.CurrentVersion);
        LatestVersionText.Text = FormatVersion(updateResult.LatestVersion);

        MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void OnReleaseLinkClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _releaseUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // Silent failure - don't crash if browser can't open
        }
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static string FormatVersion(Version version)
    {
        // Format as major.minor.patch, ignoring revision if zero
        if (version.Revision > 0)
        {
            return version.ToString();
        }
        if (version.Build > 0)
        {
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
        return $"{version.Major}.{version.Minor}";
    }
}
