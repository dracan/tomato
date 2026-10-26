using System.Globalization;
using System.Windows.Data;

namespace Tomato.Converters;

/// <summary>
/// Converts TimeSpan to a display string in mm:ss format, with a minus sign in Overtime.
/// </summary>
public class TimeSpanToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            return TimerDisplay.Format(timeSpan);
        }
        return "00:00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && TimeSpan.TryParse(str, culture, out var result))
        {
            return result;
        }
        return TimeSpan.Zero;
    }
}
