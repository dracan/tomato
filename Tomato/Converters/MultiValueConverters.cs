using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tomato.Converters;

/// <summary>
/// Multi-value converter for Start/Resume button visibility.
/// Shows when idle (!IsRunning) or paused.
/// </summary>
public class StartButtonVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return Visibility.Collapsed;

        var isRunning = values[0] is bool running && running;
        var isPaused = values[1] is bool paused && paused;

        // Show start button when not running (idle) or paused
        return (!isRunning || isPaused)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Multi-value converter for Pause button visibility.
/// Shows when running and not paused.
/// </summary>
public class PauseButtonVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return Visibility.Collapsed;

        var isRunning = values[0] is bool running && running;
        var isPaused = values[1] is bool paused && paused;

        // Show pause button when running and not paused
        return (isRunning && !isPaused)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Multi-value converter for Stop button visibility.
/// Shows when running or paused (i.e., there's an active session to cancel).
/// </summary>
public class StopButtonVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return Visibility.Collapsed;

        var isRunning = values[0] is bool running && running;
        var isPaused = values[1] is bool paused && paused;

        // Show stop button when running or paused
        return (isRunning || isPaused)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
