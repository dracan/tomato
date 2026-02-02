using System.Globalization;
using System.Windows.Data;

namespace Tomato.Converters;

/// <summary>
/// Converts a boolean value to a star character.
/// True returns a filled star, false returns an empty star.
/// </summary>
public class BoolToStarConverter : IValueConverter
{
    private const string FilledStar = "\u2605"; // ★
    private const string EmptyStar = "\u2606";  // ☆

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? FilledStar : EmptyStar;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
