using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tomato.Converters;

/// <summary>
/// Converts boolean values to Visibility.
/// Supports "Inverse" parameter to invert the logic.
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;
        var inverse = parameter is string s && s.Equals("Inverse", StringComparison.OrdinalIgnoreCase);

        if (inverse)
        {
            boolValue = !boolValue;
        }

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visibility = value is Visibility v && v == Visibility.Visible;
        var inverse = parameter is string s && s.Equals("Inverse", StringComparison.OrdinalIgnoreCase);

        return inverse ? !visibility : visibility;
    }
}
