using System.Globalization;
using System.Windows.Data;

namespace Tomato.Converters;

/// <summary>
/// Converts TimeSpan to a display string in mm:ss format.
/// </summary>
public class TimeSpanToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
            {
                return timeSpan.ToString(@"h\:mm\:ss", culture);
            }
            return timeSpan.ToString(@"mm\:ss", culture);
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
