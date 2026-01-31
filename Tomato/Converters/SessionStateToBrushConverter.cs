using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Tomato.Models;

namespace Tomato.Converters;

/// <summary>
/// Converts session state (type, running, paused) to a background color.
/// Shows idle color when not running, session type color when active.
/// </summary>
public class SessionStateToBrushConverter : IMultiValueConverter
{
    /// <summary>
    /// Color for Focus sessions (tomato red).
    /// </summary>
    public SolidColorBrush FocusColor { get; set; } = new(Color.FromRgb(220, 53, 69));

    /// <summary>
    /// Color for Short Break sessions (green).
    /// </summary>
    public SolidColorBrush ShortBreakColor { get; set; } = new(Color.FromRgb(40, 167, 69));

    /// <summary>
    /// Color for Long Break sessions (blue).
    /// </summary>
    public SolidColorBrush LongBreakColor { get; set; } = new(Color.FromRgb(0, 123, 255));

    /// <summary>
    /// Color for idle state (dark gray).
    /// </summary>
    public SolidColorBrush IdleColor { get; set; } = new(Color.FromRgb(74, 74, 74));

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3)
            return IdleColor;

        var sessionType = values[0] as SessionType? ?? SessionType.Focus;
        var isRunning = values[1] as bool? ?? false;
        var isPaused = values[2] as bool? ?? false;

        // If not running and not paused, show idle color
        if (!isRunning && !isPaused)
            return IdleColor;

        // Show session type color when active (running or paused)
        return sessionType switch
        {
            SessionType.Focus => FocusColor,
            SessionType.ShortBreak => ShortBreakColor,
            SessionType.LongBreak => LongBreakColor,
            _ => IdleColor
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ConvertBack is not supported for SessionStateToBrushConverter.");
    }
}
