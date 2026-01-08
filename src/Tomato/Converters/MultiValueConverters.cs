using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tomato.Converters;

/// <summary>
/// Multi-value converter for Start button visibility.
/// Shows start button only when not running, not paused, and session not complete.
/// </summary>
public class StartButtonVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3)
            return Visibility.Collapsed;

        var isRunning = values[0] is bool running && running;
        var isPaused = values[1] is bool paused && paused;
        var isComplete = values[2] is bool complete && complete;

        // Show start button only when idle
        return (!isRunning && !isPaused && !isComplete)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Multi-value converter that returns Visible if ANY of the input booleans is true.
/// </summary>
public class OrBooleanToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        foreach (var value in values)
        {
            if (value is bool b && b)
                return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
