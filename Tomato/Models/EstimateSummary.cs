namespace Tomato.Models;

/// <summary>
/// Describes how a focus session's Time Spent compared with its Estimate.
/// </summary>
public static class EstimateSummary
{
    /// <summary>
    /// Describes the comparison, e.g. "Estimated 15m, took 18m (3m over)".
    /// Both times are rounded to the nearest minute.
    /// </summary>
    public static string Describe(TimeSpan estimate, TimeSpan timeSpent)
    {
        var estimateMinutes = RoundToMinutes(estimate);
        var spentMinutes = RoundToMinutes(timeSpent);
        var difference = spentMinutes - estimateMinutes;

        var verdict = difference switch
        {
            > 0 => $"{FormatMinutes(difference)} over",
            < 0 => $"{FormatMinutes(-difference)} under",
            _ => "on time"
        };

        return $"Estimated {FormatMinutes(estimateMinutes)}, took {FormatMinutes(spentMinutes)} ({verdict})";
    }

    private static int RoundToMinutes(TimeSpan time) =>
        (int)Math.Round(time.TotalMinutes, MidpointRounding.AwayFromZero);

    private static string FormatMinutes(int minutes) =>
        minutes >= 60 ? $"{minutes / 60}h {minutes % 60}m" : $"{minutes}m";
}
