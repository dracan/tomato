using System.Diagnostics;
using System.IO;
using System.Text;
using Tomato.Models;

namespace Tomato.Services;

/// <summary>
/// Generates HTML statistics reports and opens them in the default browser.
/// </summary>
public sealed class StatisticsReportService : IStatisticsReportService
{
    private readonly ISessionManager _sessionManager;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly string _reportsDirectory;

    // Base64-encoded tomato.ico favicon for embedding in HTML reports
    private const string FaviconBase64 = "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AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD///////8AAP///////wAA////////AAD//4AD//8AAP/8AAB//wAA//AAAB//AAD/4AAAD/8AAP/AAAAH/wAA/4AAAAP/AAD/AAAAAf8AAP4AAAAA/wAA/gAAAAD/AAD8AAAAAH8AAPwAAAAAfwAA/AAAAAB/AAD8AAAAAD8AAPgAAAAAPwAA+AAAAAA/AAD4AAAAAD8AAPgAAAAAPwAA+AAAAAA/AAD4AAAAAD8AAPgAAAAAPwAA+AAAAAA/AAD8AAAAAD8AAPwAAAAAfwAA/AAAAAB/AAD8AAAAAH8AAP4AAAAA/wAA/gAAAAD/AAD/AAAAAf8AAP+AAAAD/wAA/8AAAAf/AAD/4AAAD/8AAP/4AAAf/wAA//AAAB//AAD/4AAAD/8AAP/gAAAP/wAA/+IAAI//AAD///gf//8AAP//+A///wAA///4B///AAD///wH//8AAP///A///wAA///+D///AAD///////8AAP///////wAA////////AAA=";

    public StatisticsReportService(
        ISessionManager sessionManager,
        IDateTimeProvider dateTimeProvider)
        : this(sessionManager, dateTimeProvider, GetDefaultReportsDirectory())
    {
    }

    public StatisticsReportService(
        ISessionManager sessionManager,
        IDateTimeProvider dateTimeProvider,
        string reportsDirectory)
    {
        _sessionManager = sessionManager;
        _dateTimeProvider = dateTimeProvider;
        _reportsDirectory = reportsDirectory;
    }

    /// <inheritdoc />
    public void GenerateAndOpenReport()
    {
        var reportPath = GenerateReport();
        OpenInBrowser(reportPath);
    }

    /// <summary>
    /// Generates the HTML report and returns the file path.
    /// </summary>
    public string GenerateReport()
    {
        EnsureDirectoryExists();

        var html = GenerateHtml();
        var reportPath = GetReportPath();
        File.WriteAllText(reportPath, html);

        return reportPath;
    }

    /// <summary>
    /// Gets the path where the report will be saved.
    /// </summary>
    public string GetReportPath() => Path.Combine(_reportsDirectory, "stats.html");

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_reportsDirectory))
        {
            Directory.CreateDirectory(_reportsDirectory);
        }
    }

    private string GenerateHtml()
    {
        var today = _dateTimeProvider.Today;
        var now = _dateTimeProvider.Now;
        var todayStats = _sessionManager.TodayStatistics;
        var history = _sessionManager.StatisticsHistory;

        // Calculate all-time totals
        var allTimeStats = CalculateAllTimeTotals(todayStats, history);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("    <title>Tomato Statistics</title>");
        sb.AppendLine($"    <link rel=\"icon\" type=\"image/x-icon\" href=\"data:image/x-icon;base64,{FaviconBase64}\">");
        sb.AppendLine(GetStyles());
        sb.AppendLine(GetScript());
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"container\">");
        sb.AppendLine("        <header>");
        sb.AppendLine("            <h1>Tomato Statistics</h1>");
        sb.AppendLine($"            <p class=\"timestamp\">Generated {now:MMMM d, yyyy} at {now:h:mm tt}</p>");
        sb.AppendLine("        </header>");
        sb.AppendLine();

        // Today's Stats Section
        sb.AppendLine("        <section class=\"stats-section today-section\">");
        sb.AppendLine("            <h2>Today</h2>");
        sb.AppendLine("            <div class=\"stats-grid\">");
        sb.AppendLine($"                <div class=\"stat-card primary\">");
        sb.AppendLine($"                    <span class=\"stat-value\">{todayStats.FocusSessionsCompleted}</span>");
        sb.AppendLine($"                    <span class=\"stat-label\">Focus Sessions</span>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"                <div class=\"stat-card\">");
        sb.AppendLine($"                    <span class=\"stat-value\">{FormatDuration(todayStats.TotalFocusTime)}</span>");
        sb.AppendLine($"                    <span class=\"stat-label\">Focus Time</span>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"                <div class=\"stat-card\">");
        sb.AppendLine($"                    <span class=\"stat-value\">{FormatDuration(todayStats.TotalBreakTime)}</span>");
        sb.AppendLine($"                    <span class=\"stat-label\">Break Time</span>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"                <div class=\"stat-card\">");
        sb.AppendLine($"                    <span class=\"stat-value\">{todayStats.CyclesCompleted}</span>");
        sb.AppendLine($"                    <span class=\"stat-label\">Cycles Completed</span>");
        sb.AppendLine($"                </div>");
        sb.AppendLine("            </div>");
        sb.AppendLine(GenerateTodaySessionsSection(todayStats));
        sb.AppendLine(GenerateTodaySupplementalActivitiesSection(todayStats));
        sb.AppendLine("        </section>");
        sb.AppendLine();

        // All-Time Totals Section
        sb.AppendLine("        <section class=\"stats-section alltime-section\">");
        sb.AppendLine("            <h2>All-Time Totals</h2>");
        sb.AppendLine("            <div class=\"stats-grid\">");
        sb.AppendLine($"                <div class=\"stat-card\">");
        sb.AppendLine($"                    <span class=\"stat-value\">{allTimeStats.TotalSessions}</span>");
        sb.AppendLine($"                    <span class=\"stat-label\">Total Sessions</span>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"                <div class=\"stat-card\">");
        sb.AppendLine($"                    <span class=\"stat-value\">{FormatDuration(allTimeStats.TotalFocusTime)}</span>");
        sb.AppendLine($"                    <span class=\"stat-label\">Total Focus Time</span>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"                <div class=\"stat-card\">");
        sb.AppendLine($"                    <span class=\"stat-value\">{FormatDuration(allTimeStats.TotalBreakTime)}</span>");
        sb.AppendLine($"                    <span class=\"stat-label\">Total Break Time</span>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"                <div class=\"stat-card\">");
        sb.AppendLine($"                    <span class=\"stat-value\">{allTimeStats.TotalCycles}</span>");
        sb.AppendLine($"                    <span class=\"stat-label\">Total Cycles</span>");
        sb.AppendLine($"                </div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </section>");
        sb.AppendLine();

        // Daily History Table
        sb.AppendLine("        <section class=\"stats-section history-section\">");
        sb.AppendLine("            <h2>Daily History</h2>");
        sb.AppendLine(GenerateHistoryTable(todayStats, history, today));
        sb.AppendLine("        </section>");
        sb.AppendLine();

        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string GetStyles()
    {
        return @"    <style>
        :root {
            --tomato-red: #e74c3c;
            --tomato-dark: #c0392b;
            --bg-color: #1a1a2e;
            --card-bg: #16213e;
            --text-primary: #eaeaea;
            --text-secondary: #a0a0a0;
            --border-color: #2a2a4a;
        }

        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: var(--bg-color);
            color: var(--text-primary);
            line-height: 1.6;
        }

        .container {
            max-width: 900px;
            margin: 0 auto;
            padding: 2rem;
        }

        header {
            text-align: center;
            margin-bottom: 2rem;
            padding-bottom: 1rem;
            border-bottom: 3px solid var(--tomato-red);
        }

        header h1 {
            color: var(--tomato-red);
            font-size: 2.5rem;
            margin-bottom: 0.5rem;
        }

        .timestamp {
            color: var(--text-secondary);
            font-size: 0.9rem;
        }

        .stats-section {
            margin-bottom: 2rem;
        }

        .stats-section h2 {
            color: var(--text-primary);
            font-size: 1.5rem;
            margin-bottom: 1rem;
            padding-left: 0.5rem;
            border-left: 4px solid var(--tomato-red);
        }

        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
            gap: 1rem;
        }

        .stat-card {
            background: var(--card-bg);
            border-radius: 12px;
            padding: 1.5rem;
            text-align: center;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
            transition: transform 0.2s ease;
            border: 1px solid var(--border-color);
        }

        .stat-card:hover {
            transform: translateY(-2px);
        }

        .stat-card.primary {
            background: var(--tomato-red);
            color: white;
            border-color: var(--tomato-dark);
        }

        .stat-card.primary .stat-label {
            color: rgba(255, 255, 255, 0.9);
        }

        .stat-value {
            display: block;
            font-size: 2rem;
            font-weight: bold;
            margin-bottom: 0.25rem;
        }

        .stat-label {
            display: block;
            font-size: 0.85rem;
            color: var(--text-secondary);
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }

        .history-table {
            width: 100%;
            border-collapse: collapse;
            background: var(--card-bg);
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
            border: 1px solid var(--border-color);
        }

        .history-table th,
        .history-table td {
            padding: 1rem;
            text-align: center;
        }

        .history-table th {
            background: var(--tomato-red);
            color: white;
            font-weight: 600;
            text-transform: uppercase;
            font-size: 0.8rem;
            letter-spacing: 0.5px;
        }

        .history-table tr:nth-child(even) {
            background: rgba(255, 255, 255, 0.03);
        }

        .history-table tr:hover {
            background: rgba(231, 76, 60, 0.1);
        }

        .history-table tr.today-row {
            background: rgba(231, 76, 60, 0.2);
            font-weight: 600;
        }

        .history-table tr.today-row:hover {
            background: rgba(231, 76, 60, 0.25);
        }

        .no-data {
            text-align: center;
            padding: 2rem;
            color: var(--text-secondary);
            background: var(--card-bg);
            border-radius: 12px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
            border: 1px solid var(--border-color);
        }

        .sessions-header {
            color: var(--text-secondary);
            font-size: 1rem;
            margin-top: 1.5rem;
            margin-bottom: 0.75rem;
            font-weight: 500;
        }

        .sessions-list {
            display: flex;
            flex-direction: column;
            gap: 0.75rem;
        }

        .session-card {
            background: var(--card-bg);
            border-radius: 8px;
            padding: 1rem;
            border: 1px solid var(--border-color);
        }

        .session-time {
            color: var(--tomato-red);
            font-weight: 600;
            margin-bottom: 0.5rem;
        }

        .session-goal,
        .session-results {
            color: var(--text-secondary);
            font-size: 0.9rem;
            margin-bottom: 0.25rem;
        }

        .session-goal strong,
        .session-results strong,
        .session-rating strong {
            color: var(--text-primary);
        }

        .session-rating {
            color: var(--text-secondary);
            font-size: 0.9rem;
            margin-bottom: 0.25rem;
        }

        .stars {
            color: #FFD700;
            letter-spacing: 2px;
        }

        .expand-btn {
            background: none;
            border: none;
            color: var(--tomato-red);
            cursor: pointer;
            font-size: 0.9rem;
            padding: 0.25rem 0.5rem;
            margin-right: 0.5rem;
        }

        .expand-btn:hover {
            color: var(--tomato-dark);
        }

        .history-sessions {
            display: none;
            padding: 0.75rem 1rem;
            background: rgba(0, 0, 0, 0.2);
        }

        .history-sessions.expanded {
            display: table-row;
        }

        .history-sessions td {
            padding: 0;
        }

        .history-sessions-content {
            padding: 0.75rem;
        }

        .history-session-item {
            background: var(--card-bg);
            border-radius: 6px;
            padding: 0.75rem;
            margin-bottom: 0.5rem;
            border: 1px solid var(--border-color);
        }

        .history-session-item:last-child {
            margin-bottom: 0;
        }

        .activity-card {
            background: var(--card-bg);
            border-radius: 8px;
            padding: 1rem;
            border: 1px solid var(--border-color);
            border-left: 3px solid #9b59b6;
        }

        .activity-description {
            color: var(--text-primary);
            font-size: 0.9rem;
        }

        .history-activity-item {
            background: var(--card-bg);
            border-radius: 6px;
            padding: 0.75rem;
            margin-bottom: 0.5rem;
            border: 1px solid var(--border-color);
            border-left: 3px solid #9b59b6;
        }

        .history-activity-item:last-child {
            margin-bottom: 0;
        }

        @media (max-width: 600px) {
            .container {
                padding: 1rem;
            }

            header h1 {
                font-size: 1.8rem;
            }

            .stats-grid {
                grid-template-columns: repeat(2, 1fr);
            }

            .stat-value {
                font-size: 1.5rem;
            }

            .history-table th,
            .history-table td {
                padding: 0.75rem 0.5rem;
                font-size: 0.9rem;
            }
        }
    </style>";
    }

    private string GenerateHistoryTable(DailyStatistics todayStats, IReadOnlyList<DailyStatistics> history, DateOnly today)
    {
        var sb = new StringBuilder();

        // Combine today and history, sort by date descending
        var allDays = new List<DailyStatistics>();

        // Add today if it has data
        if (todayStats.FocusSessionsCompleted > 0 || todayStats.TotalFocusTime > TimeSpan.Zero || todayStats.SupplementalActivities.Count > 0)
        {
            allDays.Add(todayStats);
        }

        // Add history (excluding today to avoid duplicates)
        allDays.AddRange(history.Where(h => h.Date != today));

        // Sort by date descending
        allDays = allDays.OrderByDescending(d => d.Date).Take(30).ToList();

        if (allDays.Count == 0)
        {
            sb.AppendLine("            <div class=\"no-data\">");
            sb.AppendLine("                <p>No history yet. Complete some focus sessions to start tracking!</p>");
            sb.AppendLine("            </div>");
            return sb.ToString();
        }

        sb.AppendLine("            <table class=\"history-table\">");
        sb.AppendLine("                <thead>");
        sb.AppendLine("                    <tr>");
        sb.AppendLine("                        <th>Date</th>");
        sb.AppendLine("                        <th>Sessions</th>");
        sb.AppendLine("                        <th>Focus Time</th>");
        sb.AppendLine("                        <th>Break Time</th>");
        sb.AppendLine("                        <th>Cycles</th>");
        sb.AppendLine("                    </tr>");
        sb.AppendLine("                </thead>");
        sb.AppendLine("                <tbody>");

        var rowIndex = 0;
        foreach (var day in allDays)
        {
            var isToday = day.Date == today;
            var hasSessionRecords = day.SessionRecords.Count > 0;
            var hasSupplementalActivities = day.SupplementalActivities.Count > 0;
            var hasExpandableContent = hasSessionRecords || hasSupplementalActivities;
            var rowClass = isToday ? " class=\"today-row\"" : "";

            sb.AppendLine($"                    <tr{rowClass}>");

            // Date column - include expand button if there are session records or activities
            if (hasExpandableContent)
            {
                sb.AppendLine($"                        <td><button class=\"expand-btn\" onclick=\"toggleRow({rowIndex})\">+</button>{day.Date:yyyy-MM-dd}</td>");
            }
            else
            {
                sb.AppendLine($"                        <td>{day.Date:yyyy-MM-dd}</td>");
            }

            sb.AppendLine($"                        <td>{day.FocusSessionsCompleted}</td>");
            sb.AppendLine($"                        <td>{FormatDuration(day.TotalFocusTime)}</td>");
            sb.AppendLine($"                        <td>{FormatDuration(day.TotalBreakTime)}</td>");
            sb.AppendLine($"                        <td>{day.CyclesCompleted}</td>");
            sb.AppendLine("                    </tr>");

            // Add expandable row with session details and supplemental activities
            if (hasExpandableContent)
            {
                sb.AppendLine($"                    <tr class=\"history-sessions\" id=\"sessions-{rowIndex}\">");
                sb.AppendLine("                        <td colspan=\"5\">");
                sb.AppendLine("                            <div class=\"history-sessions-content\">");

                foreach (var record in day.SessionRecords)
                {
                    sb.AppendLine("                                <div class=\"history-session-item\">");
                    sb.AppendLine($"                                    <div class=\"session-time\">{record.StartedAt:h:mm tt} - {record.CompletedAt:h:mm tt} ({FormatDuration(record.Duration)})</div>");
                    sb.AppendLine($"                                    <div class=\"session-rating\"><strong>Rating:</strong> {FormatRating(record.Rating)}</div>");
                    sb.AppendLine($"                                    <div class=\"session-goal\"><strong>Goal:</strong> {(string.IsNullOrWhiteSpace(record.Goal) ? "No goal set" : HtmlEncode(record.Goal))}</div>");
                    sb.AppendLine($"                                    <div class=\"session-results\"><strong>Results:</strong> {(string.IsNullOrWhiteSpace(record.Results) ? "No results recorded" : HtmlEncode(record.Results))}</div>");
                    sb.AppendLine("                                </div>");
                }

                foreach (var activity in day.SupplementalActivities)
                {
                    sb.AppendLine("                                <div class=\"history-activity-item\">");
                    sb.AppendLine($"                                    <div class=\"activity-description\">{HtmlEncode(activity.Description)}</div>");
                    sb.AppendLine("                                </div>");
                }

                sb.AppendLine("                            </div>");
                sb.AppendLine("                        </td>");
                sb.AppendLine("                    </tr>");
            }

            rowIndex++;
        }

        sb.AppendLine("                </tbody>");
        sb.AppendLine("            </table>");

        return sb.ToString();
    }

    private static string GetScript()
    {
        return @"    <script>
        function toggleRow(index) {
            var row = document.getElementById('sessions-' + index);
            var btn = event.target;
            if (row.classList.contains('expanded')) {
                row.classList.remove('expanded');
                btn.textContent = '+';
            } else {
                row.classList.add('expanded');
                btn.textContent = '-';
            }
        }
    </script>";
    }

    private static string GenerateTodaySessionsSection(DailyStatistics todayStats)
    {
        var sb = new StringBuilder();

        if (todayStats.SessionRecords.Count == 0)
        {
            return string.Empty;
        }

        sb.AppendLine();
        sb.AppendLine("            <h3 class=\"sessions-header\">Today's Sessions</h3>");
        sb.AppendLine("            <div class=\"sessions-list\">");

        foreach (var record in todayStats.SessionRecords)
        {
            sb.AppendLine("                <div class=\"session-card\">");
            sb.AppendLine($"                    <div class=\"session-time\">{record.StartedAt:h:mm tt} - {record.CompletedAt:h:mm tt} ({FormatDuration(record.Duration)})</div>");
            sb.AppendLine($"                    <div class=\"session-rating\"><strong>Rating:</strong> {FormatRating(record.Rating)}</div>");
            sb.AppendLine($"                    <div class=\"session-goal\"><strong>Goal:</strong> {(string.IsNullOrWhiteSpace(record.Goal) ? "No goal set" : HtmlEncode(record.Goal))}</div>");
            sb.AppendLine($"                    <div class=\"session-results\"><strong>Results:</strong> {(string.IsNullOrWhiteSpace(record.Results) ? "No results recorded" : HtmlEncode(record.Results))}</div>");
            sb.AppendLine("                </div>");
        }

        sb.AppendLine("            </div>");

        return sb.ToString();
    }

    private static string GenerateTodaySupplementalActivitiesSection(DailyStatistics todayStats)
    {
        var sb = new StringBuilder();

        if (todayStats.SupplementalActivities.Count == 0)
        {
            return string.Empty;
        }

        sb.AppendLine();
        sb.AppendLine("            <h3 class=\"sessions-header\">Supplemental Activities</h3>");
        sb.AppendLine("            <div class=\"sessions-list\">");

        foreach (var activity in todayStats.SupplementalActivities)
        {
            sb.AppendLine("                <div class=\"activity-card\">");
            sb.AppendLine($"                    <div class=\"activity-description\">{HtmlEncode(activity.Description)}</div>");
            sb.AppendLine("                </div>");
        }

        sb.AppendLine("            </div>");

        return sb.ToString();
    }

    private static string HtmlEncode(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private static (int TotalSessions, TimeSpan TotalFocusTime, TimeSpan TotalBreakTime, int TotalCycles) CalculateAllTimeTotals(
        DailyStatistics todayStats,
        IReadOnlyList<DailyStatistics> history)
    {
        var totalSessions = todayStats.FocusSessionsCompleted;
        var totalFocusTime = todayStats.TotalFocusTime;
        var totalBreakTime = todayStats.TotalBreakTime;
        var totalCycles = todayStats.CyclesCompleted;

        foreach (var day in history)
        {
            totalSessions += day.FocusSessionsCompleted;
            totalFocusTime += day.TotalFocusTime;
            totalBreakTime += day.TotalBreakTime;
            totalCycles += day.CyclesCompleted;
        }

        return (totalSessions, totalFocusTime, totalBreakTime, totalCycles);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMinutes < 1)
        {
            return "0m";
        }

        var hours = (int)duration.TotalHours;
        var minutes = duration.Minutes;

        if (hours > 0)
        {
            return $"{hours}h {minutes}m";
        }

        return $"{minutes}m";
    }

    private static string FormatRating(int? rating)
    {
        if (!rating.HasValue)
        {
            return "No rating";
        }

        var filled = new string('\u2605', rating.Value);  // ★
        var empty = new string('\u2606', 5 - rating.Value); // ☆
        return $"<span class=\"stars\">{filled}{empty}</span>";
    }

    private static void OpenInBrowser(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch
        {
            // Silently fail if browser cannot be opened
        }
    }

    private static string GetDefaultReportsDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Tomato", "reports");
    }
}
