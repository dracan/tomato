using System.Globalization;

namespace Tomato.Converters;

/// <summary>
/// Formats the time remaining for display. In Overtime the time remaining is
/// negative and is shown with a minus sign, e.g. "-04:12".
/// </summary>
public static class TimerDisplay
{
    public static string Format(TimeSpan remaining)
    {
        // Whole seconds, dropping any fraction, so the first second of Overtime still shows 00:00
        var totalSeconds = (long)Math.Truncate(remaining.TotalSeconds);
        var sign = totalSeconds < 0 ? "-" : "";
        var time = TimeSpan.FromSeconds(Math.Abs(totalSeconds));

        var text = time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
            : time.ToString(@"mm\:ss", CultureInfo.InvariantCulture);

        return sign + text;
    }
}
