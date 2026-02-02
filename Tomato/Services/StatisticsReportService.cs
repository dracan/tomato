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
        if (todayStats.FocusSessionsCompleted > 0 || todayStats.TotalFocusTime > TimeSpan.Zero)
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
            var rowClass = isToday ? " class=\"today-row\"" : "";

            sb.AppendLine($"                    <tr{rowClass}>");

            // Date column - include expand button if there are session records
            if (hasSessionRecords)
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

            // Add expandable row with session details
            if (hasSessionRecords)
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
