using System.IO;
using FluentAssertions;
using NSubstitute;
using Tomato.Models;
using Tomato.Services;

namespace Tomato.Tests.Unit;

public class StatisticsReportServiceTests : IDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly string _testReportsDirectory;
    private readonly StatisticsReportService _sut;

    public StatisticsReportServiceTests()
    {
        _sessionManager = Substitute.For<ISessionManager>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        // Use a test-specific temp directory
        _testReportsDirectory = Path.Combine(Path.GetTempPath(), $"TomatoTests_{Guid.NewGuid()}");

        // Set up default date
        _dateTimeProvider.Today.Returns(new DateOnly(2024, 1, 15));
        _dateTimeProvider.Now.Returns(new DateTime(2024, 1, 15, 14, 30, 0));

        // Set up default statistics
        var todayStats = new DailyStatistics
        {
            Date = new DateOnly(2024, 1, 15),
            FocusSessionsCompleted = 4,
            TotalFocusTime = TimeSpan.FromMinutes(100),
            TotalBreakTime = TimeSpan.FromMinutes(15),
            CyclesCompleted = 1
        };
        _sessionManager.TodayStatistics.Returns(todayStats);
        _sessionManager.StatisticsHistory.Returns(new List<DailyStatistics>());

        _sut = new StatisticsReportService(_sessionManager, _dateTimeProvider, _testReportsDirectory);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testReportsDirectory))
        {
            Directory.Delete(_testReportsDirectory, recursive: true);
        }
    }

    [Fact]
    public void GenerateAndOpenReport_CreatesReportFile()
    {
        // Act
        _sut.GenerateReport();

        // Assert
        var reportPath = Path.Combine(_testReportsDirectory, "stats.html");
        File.Exists(reportPath).Should().BeTrue();
    }

    [Fact]
    public void GenerateAndOpenReport_CreatesDirectoryIfNotExists()
    {
        // Arrange - ensure directory doesn't exist
        if (Directory.Exists(_testReportsDirectory))
        {
            Directory.Delete(_testReportsDirectory, recursive: true);
        }

        // Act
        _sut.GenerateReport();

        // Assert
        Directory.Exists(_testReportsDirectory).Should().BeTrue();
    }

    [Fact]
    public void GenerateAndOpenReport_HtmlContainsTodaysStats()
    {
        // Arrange
        var todayStats = new DailyStatistics
        {
            Date = new DateOnly(2024, 1, 15),
            FocusSessionsCompleted = 8,
            TotalFocusTime = TimeSpan.FromMinutes(200),
            TotalBreakTime = TimeSpan.FromMinutes(40),
            CyclesCompleted = 2
        };
        _sessionManager.TodayStatistics.Returns(todayStats);

        // Act
        _sut.GenerateReport();

        // Assert
        var reportPath = Path.Combine(_testReportsDirectory, "stats.html");
        var html = File.ReadAllText(reportPath);

        StatValue(html, "Focus Sessions").Should().Be("8");
        StatValue(html, "Focus Time").Should().Be("3h 20m");
        StatValue(html, "Break Time").Should().Be("40m");
        StatValue(html, "Cycles Completed").Should().Be("2");
    }

    [Fact]
    public void GenerateAndOpenReport_HtmlContainsTodaySection()
    {
        // Act
        _sut.GenerateReport();

        // Assert
        var reportPath = Path.Combine(_testReportsDirectory, "stats.html");
        var html = File.ReadAllText(reportPath);

        html.Should().Contain("Today");
    }

    [Fact]
    public void GenerateAndOpenReport_HtmlContainsAllTimeTotals()
    {
        // Arrange - set up history
        var history = new List<DailyStatistics>
        {
            new()
            {
                Date = new DateOnly(2024, 1, 14),
                FocusSessionsCompleted = 6,
                TotalFocusTime = TimeSpan.FromMinutes(150),
                TotalBreakTime = TimeSpan.FromMinutes(30),
                CyclesCompleted = 1
            },
            new()
            {
                Date = new DateOnly(2024, 1, 13),
                FocusSessionsCompleted = 4,
                TotalFocusTime = TimeSpan.FromMinutes(100),
                TotalBreakTime = TimeSpan.FromMinutes(20),
                CyclesCompleted = 1
            }
        };
        _sessionManager.StatisticsHistory.Returns(history);

        // Act
        _sut.GenerateReport();

        // Assert
        var reportPath = Path.Combine(_testReportsDirectory, "stats.html");
        var html = File.ReadAllText(reportPath);

        // Today (4 sessions, 100m focus, 15m break, 1 cycle) plus both history days
        html.Should().Contain("All-Time");
        StatValue(html, "Total Sessions").Should().Be("14");
        StatValue(html, "Total Focus Time").Should().Be("5h 50m");
        StatValue(html, "Total Break Time").Should().Be("1h 5m");
        StatValue(html, "Total Cycles").Should().Be("3");
    }

    [Fact]
    public void GenerateReport_HistoryAlsoContainingToday_DoesNotDoubleCountTotals()
    {
        // Arrange - today (4 sessions) also appears in history
        var today = _sessionManager.TodayStatistics;
        _sessionManager.StatisticsHistory.Returns(new List<DailyStatistics> { today });

        // Act
        _sut.GenerateReport();

        // Assert
        var html = File.ReadAllText(Path.Combine(_testReportsDirectory, "stats.html"));
        StatValue(html, "Total Sessions").Should().Be("4");
    }

    [Fact]
    public void GenerateAndOpenReport_HtmlContainsHistoryTable()
    {
        // Arrange
        var history = new List<DailyStatistics>
        {
            new()
            {
                Date = new DateOnly(2024, 1, 14),
                FocusSessionsCompleted = 6,
                TotalFocusTime = TimeSpan.FromMinutes(150),
                TotalBreakTime = TimeSpan.FromMinutes(30),
                CyclesCompleted = 1
            }
        };
        _sessionManager.StatisticsHistory.Returns(history);

        // Act
        _sut.GenerateReport();

        // Assert
        var reportPath = Path.Combine(_testReportsDirectory, "stats.html");
        var html = File.ReadAllText(reportPath);

        html.Should().Contain("Daily History");
        html.Should().Contain("2024-01-14"); // Yesterday's date
    }

    [Fact]
    public void GenerateAndOpenReport_HtmlContainsProperStyling()
    {
        // Act
        _sut.GenerateReport();

        // Assert
        var reportPath = Path.Combine(_testReportsDirectory, "stats.html");
        var html = File.ReadAllText(reportPath);

        html.Should().Contain("<style>");
        html.Should().Contain("#e74c3c"); // Tomato red color
        html.Should().Contain("Segoe UI"); // Font family
    }

    [Fact]
    public void GenerateAndOpenReport_HtmlContainsGenerationTimestamp()
    {
        // Arrange
        _dateTimeProvider.Now.Returns(new DateTime(2024, 1, 15, 14, 30, 0));

        // Act
        _sut.GenerateReport();

        // Assert
        var reportPath = Path.Combine(_testReportsDirectory, "stats.html");
        var html = File.ReadAllText(reportPath);

        html.Should().Contain("Generated January 15, 2024 at 2:30 PM");
    }

    [Fact]
    public void GenerateAndOpenReport_HtmlHighlightsTodayInTable()
    {
        // Arrange - today should be included in the table with special styling
        var todayStats = new DailyStatistics
        {
            Date = new DateOnly(2024, 1, 15),
            FocusSessionsCompleted = 4,
            TotalFocusTime = TimeSpan.FromMinutes(100),
            TotalBreakTime = TimeSpan.FromMinutes(15),
            CyclesCompleted = 1
        };
        _sessionManager.TodayStatistics.Returns(todayStats);

        // Act
        _sut.GenerateReport();

        // Assert
        var reportPath = Path.Combine(_testReportsDirectory, "stats.html");
        var html = File.ReadAllText(reportPath);

        html.Should().Contain("today-row"); // CSS class for highlighting today
    }

    [Fact]
    public void GetReportPath_ReturnsCorrectPath()
    {
        // Act
        var path = _sut.GetReportPath();

        // Assert
        path.Should().Be(Path.Combine(_testReportsDirectory, "stats.html"));
    }

    [Fact]
    public void GenerateAndOpenReport_HtmlContainsTodaySessionsSection()
    {
        // Arrange
        var todayStats = new DailyStatistics
        {
            Date = new DateOnly(2024, 1, 15),
            FocusSessionsCompleted = 2,
            TotalFocusTime = TimeSpan.FromMinutes(50),
            TotalBreakTime = TimeSpan.FromMinutes(10),
            CyclesCompleted = 0
        };
        todayStats.AddSessionRecord(new SessionRecord
        {
            Goal = "Write unit tests",
            Results = "Completed 5 tests",
            TimeSpent = TimeSpan.FromMinutes(25),
            StartedAt = new DateTime(2024, 1, 15, 9, 0, 0),
            CompletedAt = new DateTime(2024, 1, 15, 9, 25, 0)
        });
        _sessionManager.TodayStatistics.Returns(todayStats);

        // Act
        _sut.GenerateReport();

        // Assert
        var reportPath = Path.Combine(_testReportsDirectory, "stats.html");
        var html = File.ReadAllText(reportPath);

        html.Should().Contain("Today's Sessions");
        html.Should().Contain("Write unit tests");
        html.Should().Contain("Completed 5 tests");
    }

    [Fact]
    public void GenerateReport_SessionWithEstimate_ShowsHowTimeSpentCompared()
    {
        // Arrange
        var todayStats = DailyStatistics.Create(new DateOnly(2024, 1, 15));
        todayStats.RecordFocusSession(TimeSpan.FromMinutes(18));
        todayStats.AddSessionRecord(new SessionRecord
        {
            Goal = "Fix bug",
            Estimate = TimeSpan.FromMinutes(15),
            TimeSpent = TimeSpan.FromMinutes(18),
            StartedAt = new DateTime(2024, 1, 15, 9, 0, 0),
            CompletedAt = new DateTime(2024, 1, 15, 9, 18, 0)
        });
        _sessionManager.TodayStatistics.Returns(todayStats);

        // Act
        _sut.GenerateReport();

        // Assert
        var html = File.ReadAllText(Path.Combine(_testReportsDirectory, "stats.html"));
        html.Should().Contain("<div class=\"session-estimate\">Estimated 15m, took 18m (3m over)</div>");
    }

    [Fact]
    public void GenerateReport_SessionFromBeforeEstimates_HasNoComparison()
    {
        // Arrange
        var todayStats = DailyStatistics.Create(new DateOnly(2024, 1, 15));
        todayStats.RecordFocusSession(TimeSpan.FromMinutes(25));
        todayStats.AddSessionRecord(new SessionRecord
        {
            Goal = "Old session",
            TimeSpent = TimeSpan.FromMinutes(25),
            StartedAt = new DateTime(2024, 1, 15, 9, 0, 0),
            CompletedAt = new DateTime(2024, 1, 15, 9, 25, 0)
        });
        _sessionManager.TodayStatistics.Returns(todayStats);

        // Act
        _sut.GenerateReport();

        // Assert
        var html = File.ReadAllText(Path.Combine(_testReportsDirectory, "stats.html"));
        html.Should().Contain("Old session");
        html.Should().NotContain("<div class=\"session-estimate\">");
    }

    [Fact]
    public void GenerateReport_HistorySessionWithEstimate_ShowsHowTimeSpentCompared()
    {
        // Arrange
        var yesterday = DailyStatistics.Create(new DateOnly(2024, 1, 14));
        yesterday.RecordFocusSession(TimeSpan.FromMinutes(8));
        yesterday.AddSessionRecord(new SessionRecord
        {
            Goal = "Quick fix",
            Estimate = TimeSpan.FromMinutes(10),
            TimeSpent = TimeSpan.FromMinutes(8),
            StartedAt = new DateTime(2024, 1, 14, 9, 0, 0),
            CompletedAt = new DateTime(2024, 1, 14, 9, 8, 0)
        });
        _sessionManager.StatisticsHistory.Returns(new List<DailyStatistics> { yesterday });

        // Act
        _sut.GenerateReport();

        // Assert
        var html = File.ReadAllText(Path.Combine(_testReportsDirectory, "stats.html"));
        html.Should().Contain("<div class=\"session-estimate\">Estimated 10m, took 8m (2m under)</div>");
    }

    [Fact]
    public void GenerateReport_BreakOnlyDay_IsShownInHistoryTable()
    {
        // Arrange
        var todayStats = DailyStatistics.Create(new DateOnly(2024, 1, 15));
        todayStats.RecordBreakSession(TimeSpan.FromMinutes(10));
        _sessionManager.TodayStatistics.Returns(todayStats);

        // Act
        _sut.GenerateReport();

        // Assert
        var html = File.ReadAllText(Path.Combine(_testReportsDirectory, "stats.html"));
        html.Should().Contain("<td>2024-01-15</td>");
    }

    [Fact]
    public void GenerateReport_OnNonEnglishSystem_UsesEnglishDatesAndTimes()
    {
        // Arrange - German formats have no AM/PM and German month names
        var todayStats = DailyStatistics.Create(new DateOnly(2024, 1, 15));
        todayStats.RecordFocusSession(TimeSpan.FromMinutes(25));
        todayStats.AddSessionRecord(new SessionRecord
        {
            Goal = "Test",
            TimeSpent = TimeSpan.FromMinutes(25),
            StartedAt = new DateTime(2024, 1, 15, 14, 0, 0),
            CompletedAt = new DateTime(2024, 1, 15, 14, 25, 0)
        });
        _sessionManager.TodayStatistics.Returns(todayStats);

        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
        try
        {
            // Act
            _sut.GenerateReport();
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }

        // Assert
        var html = File.ReadAllText(Path.Combine(_testReportsDirectory, "stats.html"));
        html.Should().Contain("Generated January 15, 2024 at 2:30 PM");
        html.Should().Contain("2:00 PM - 2:25 PM");
    }

    [Fact]
    public void GenerateAndOpenReport_HtmlContainsSessionTime()
    {
        // Arrange
        var todayStats = new DailyStatistics
        {
            Date = new DateOnly(2024, 1, 15),
            FocusSessionsCompleted = 1,
            TotalFocusTime = TimeSpan.FromMinutes(25),
            TotalBreakTime = TimeSpan.Zero,
            CyclesCompleted = 0
        };
        todayStats.AddSessionRecord(new SessionRecord
        {
            Goal = "Test",
            TimeSpent = TimeSpan.FromMinutes(25),
            StartedAt = new DateTime(2024, 1, 15, 9, 0, 0),
            CompletedAt = new DateTime(2024, 1, 15, 9, 25, 0)
        });
        _sessionManager.TodayStatistics.Returns(todayStats);

        // Act
        _sut.GenerateReport();

        // Assert
        var reportPath = Path.Combine(_testReportsDirectory, "stats.html");
        var html = File.ReadAllText(reportPath);

        html.Should().Contain("9:00 AM");
        html.Should().Contain("9:25 AM");
        html.Should().Contain("25m");
    }

    [Fact]
    public void GenerateAndOpenReport_NoSessionRecords_DoesNotShowTodaySessionsHeader()
    {
        // Arrange - default todayStats has no session records
        var todayStats = new DailyStatistics
        {
            Date = new DateOnly(2024, 1, 15),
            FocusSessionsCompleted = 0,
            TotalFocusTime = TimeSpan.Zero,
            TotalBreakTime = TimeSpan.Zero,
            CyclesCompleted = 0
        };
        _sessionManager.TodayStatistics.Returns(todayStats);

        // Act
        _sut.GenerateReport();

        // Assert
        var reportPath = Path.Combine(_testReportsDirectory, "stats.html");
        var html = File.ReadAllText(reportPath);

        html.Should().NotContain("Today's Sessions");
    }

    [Fact]
    public void GenerateAndOpenReport_SessionWithNoGoal_ShowsNoGoalSet()
    {
        // Arrange
        var todayStats = new DailyStatistics
        {
            Date = new DateOnly(2024, 1, 15),
            FocusSessionsCompleted = 1,
            TotalFocusTime = TimeSpan.FromMinutes(25),
            TotalBreakTime = TimeSpan.Zero,
            CyclesCompleted = 0
        };
        todayStats.AddSessionRecord(new SessionRecord
        {
            Goal = null,
            Results = null,
            TimeSpent = TimeSpan.FromMinutes(25),
            StartedAt = new DateTime(2024, 1, 15, 9, 0, 0),
            CompletedAt = new DateTime(2024, 1, 15, 9, 25, 0)
        });
        _sessionManager.TodayStatistics.Returns(todayStats);

        // Act
        _sut.GenerateReport();

        // Assert
        var reportPath = Path.Combine(_testReportsDirectory, "stats.html");
        var html = File.ReadAllText(reportPath);

        html.Should().Contain("No goal set");
        html.Should().Contain("No results recorded");
    }

    [Fact]
    public void GenerateAndOpenReport_HistoryWithSessionRecords_HasExpandButton()
    {
        // Arrange
        var yesterdayStats = new DailyStatistics
        {
            Date = new DateOnly(2024, 1, 14),
            FocusSessionsCompleted = 2,
            TotalFocusTime = TimeSpan.FromMinutes(50),
            TotalBreakTime = TimeSpan.FromMinutes(10),
            CyclesCompleted = 0
        };
        yesterdayStats.AddSessionRecord(new SessionRecord
        {
            Goal = "Yesterday's task",
            Results = "Done",
            TimeSpent = TimeSpan.FromMinutes(25),
            StartedAt = new DateTime(2024, 1, 14, 10, 0, 0),
            CompletedAt = new DateTime(2024, 1, 14, 10, 25, 0)
        });

        _sessionManager.StatisticsHistory.Returns(new List<DailyStatistics> { yesterdayStats });

        // Act
        _sut.GenerateReport();

        // Assert
        var reportPath = Path.Combine(_testReportsDirectory, "stats.html");
        var html = File.ReadAllText(reportPath);

        html.Should().Contain("expand-btn");
        html.Should().Contain("toggleRow");
    }

    [Fact]
    public void GenerateAndOpenReport_HtmlEscapesSpecialCharacters()
    {
        // Arrange
        var todayStats = new DailyStatistics
        {
            Date = new DateOnly(2024, 1, 15),
            FocusSessionsCompleted = 1,
            TotalFocusTime = TimeSpan.FromMinutes(25),
            TotalBreakTime = TimeSpan.Zero,
            CyclesCompleted = 0
        };
        todayStats.AddSessionRecord(new SessionRecord
        {
            Goal = "<script>alert('xss')</script>",
            Results = "Fix bug & test",
            TimeSpent = TimeSpan.FromMinutes(25),
            StartedAt = new DateTime(2024, 1, 15, 9, 0, 0),
            CompletedAt = new DateTime(2024, 1, 15, 9, 25, 0)
        });
        _sessionManager.TodayStatistics.Returns(todayStats);

        // Act
        _sut.GenerateReport();

        // Assert
        var reportPath = Path.Combine(_testReportsDirectory, "stats.html");
        var html = File.ReadAllText(reportPath);

        // Should be escaped
        html.Should().Contain("&lt;script&gt;");
        html.Should().Contain("&amp;");
        // Should not contain unescaped script tag
        html.Should().NotContain("<script>alert");
    }

    [Fact]
    public void GenerateAndOpenReport_ContainsJavaScriptForToggle()
    {
        // Act
        _sut.GenerateReport();

        // Assert
        var reportPath = Path.Combine(_testReportsDirectory, "stats.html");
        var html = File.ReadAllText(reportPath);

        html.Should().Contain("function toggleRow");
    }

    /// <summary>
    /// Gets the value shown on the stat card with the given label.
    /// </summary>
    private static string StatValue(string html, string label)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            html,
            "<span class=\"stat-value\">([^<]*)</span>\\s*<span class=\"stat-label\">" + label + "</span>");
        match.Success.Should().BeTrue($"the report should have a \"{label}\" stat card");
        return match.Groups[1].Value;
    }
}
