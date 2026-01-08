using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Tomato.Models;

namespace Tomato.Converters;

/// <summary>
/// Converts SessionType to a corresponding color for visual indication.
/// </summary>
public class SessionTypeToColorConverter : IValueConverter
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
    /// Default color when no session is active.
    /// </summary>
    public SolidColorBrush DefaultColor { get; set; } = new(Color.FromRgb(108, 117, 125));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SessionType sessionType)
        {
            return sessionType switch
            {
                SessionType.Focus => FocusColor,
                SessionType.ShortBreak => ShortBreakColor,
                SessionType.LongBreak => LongBreakColor,
                _ => DefaultColor
            };
        }
        return DefaultColor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ConvertBack is not supported for SessionTypeToColorConverter.");
    }
}
