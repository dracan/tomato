namespace Tomato.Services;

/// <summary>
/// Service for generating and displaying statistics reports.
/// </summary>
public interface IStatisticsReportService
{
    /// <summary>
    /// Generates an HTML statistics report and opens it in the default browser.
    /// </summary>
    void GenerateAndOpenReport();
}
