using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Tomato.Converters;

/// <summary>
/// Converts a nullable int rating (1-5) to a SolidColorBrush.
/// </summary>
public class RatingToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush GrayBrush = new(Color.FromRgb(0x80, 0x80, 0x80));     // No rating
    private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(0xE7, 0x4C, 0x3C));      // 1 star
    private static readonly SolidColorBrush OrangeBrush = new(Color.FromRgb(0xE6, 0x7E, 0x22));   // 2 stars
    private static readonly SolidColorBrush YellowBrush = new(Color.FromRgb(0xF1, 0xC4, 0x0F));   // 3 stars
    private static readonly SolidColorBrush LightGreenBrush = new(Color.FromRgb(0x2E, 0xCC, 0x71)); // 4 stars
    private static readonly SolidColorBrush BrightGreenBrush = new(Color.FromRgb(0x27, 0xAE, 0x60)); // 5 stars

    static RatingToBrushConverter()
    {
        // Freeze brushes for performance
        GrayBrush.Freeze();
        RedBrush.Freeze();
        OrangeBrush.Freeze();
        YellowBrush.Freeze();
        LightGreenBrush.Freeze();
        BrightGreenBrush.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var rating = value as int?;

        return rating switch
        {
            1 => RedBrush,
            2 => OrangeBrush,
            3 => YellowBrush,
            4 => LightGreenBrush,
            5 => BrightGreenBrush,
            _ => GrayBrush
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
