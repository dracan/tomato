using System.Text.Json.Serialization;

namespace Tomato.Models;

/// <summary>
/// Represents the complete application state for persistence.
/// Saved to %LOCALAPPDATA%\Tomato\state.json
/// </summary>
public sealed class AppState
{
    /// <summary>
    /// Gets or sets the current session, if any.
    /// </summary>
    public SessionState? CurrentSession { get; set; }

    /// <summary>
    /// Gets or sets the current cycle progress.
    /// </summary>
    public CycleState Cycle { get; set; } = new();

    /// <summary>
    /// Gets or sets the daily statistics.
    /// </summary>
    public DailyStatisticsState? TodayStatistics { get; set; }

    /// <summary>
    /// Gets or sets when this state was last saved.
    /// </summary>
    public DateTime LastSavedAt { get; set; }

    /// <summary>
    /// Gets or sets the historical daily statistics.
    /// </summary>
    public List<DailyStatisticsState> StatisticsHistory { get; set; } = new();

    /// <summary>
    /// Gets or sets whether new focus sessions keep running past their Estimate (Overtime).
    /// </summary>
    public bool OvertimeEnabled { get; set; }

    /// <summary>
    /// Represents a serializable session state.
    /// </summary>
    public sealed class SessionState
    {
        public SessionType Type { get; set; }
        public SessionStatus Status { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Goal { get; set; }
        public bool AllowsOvertime { get; set; }
    }

    /// <summary>
    /// Represents a serializable cycle state.
    /// </summary>
    public sealed class CycleState
    {
        public int CompletedFocusSessions { get; set; }
    }

    /// <summary>
    /// Represents serializable daily statistics.
    /// </summary>
    public sealed class DailyStatisticsState
    {
        public DateOnly Date { get; set; }
        public int FocusSessionsCompleted { get; set; }
        public TimeSpan TotalFocusTime { get; set; }
        public TimeSpan TotalBreakTime { get; set; }
        public int CyclesCompleted { get; set; }
        public List<SessionRecordState> SessionRecords { get; set; } = new();
        public List<SupplementalActivityState> SupplementalActivities { get; set; } = new();
    }

    /// <summary>
    /// Represents a serializable session record.
    /// </summary>
    public sealed class SessionRecordState
    {
        public string? Goal { get; set; }
        public string? Results { get; set; }
        public int? Rating { get; set; }

        /// <summary>
        /// The Estimate; missing from records saved before Estimates were tracked.
        /// </summary>
        public TimeSpan? Estimate { get; set; }

        /// <summary>
        /// The Time Spent. Kept under its original name so older saved data still reads.
        /// </summary>
        public TimeSpan Duration { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    /// <summary>
    /// Represents a serializable supplemental activity.
    /// </summary>
    public sealed class SupplementalActivityState
    {
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Creates an AppState from the current application objects.
    /// </summary>
    public static AppState FromCurrentState(
        Session? currentSession,
        PomodoroCycle cycle,
        DailyStatistics? todayStatistics,
        IReadOnlyList<DailyStatistics> statisticsHistory,
        DateTime savedAt,
        bool overtimeEnabled = false)
    {
        var state = new AppState
        {
            LastSavedAt = savedAt,
            OvertimeEnabled = overtimeEnabled,
            Cycle = new CycleState
            {
                CompletedFocusSessions = cycle.CompletedFocusSessions
            }
        };

        if (currentSession != null)
        {
            state.CurrentSession = new SessionState
            {
                Type = currentSession.Type,
                Status = currentSession.Status,
                Duration = currentSession.Duration,
                TimeRemaining = currentSession.TimeRemaining,
                StartedAt = currentSession.StartedAt,
                CompletedAt = currentSession.CompletedAt,
                Goal = currentSession.Goal,
                AllowsOvertime = currentSession.AllowsOvertime
            };
        }

        if (todayStatistics != null)
        {
            state.TodayStatistics = new DailyStatisticsState
            {
                Date = todayStatistics.Date,
                FocusSessionsCompleted = todayStatistics.FocusSessionsCompleted,
                TotalFocusTime = todayStatistics.TotalFocusTime,
                TotalBreakTime = todayStatistics.TotalBreakTime,
                CyclesCompleted = todayStatistics.CyclesCompleted,
                SessionRecords = todayStatistics.SessionRecords
                    .Select(r => new SessionRecordState
                    {
                        Goal = r.Goal,
                        Results = r.Results,
                        Rating = r.Rating,
                        Estimate = r.Estimate,
                        Duration = r.TimeSpent,
                        StartedAt = r.StartedAt,
                        CompletedAt = r.CompletedAt
                    })
                    .ToList(),
                SupplementalActivities = todayStatistics.SupplementalActivities
                    .Select(a => new SupplementalActivityState
                    {
                        Description = a.Description
                    })
                    .ToList()
            };
        }

        // Merge today's statistics into history
        state.StatisticsHistory = MergeStatisticsHistory(statisticsHistory, todayStatistics);

        return state;
    }

    private static List<DailyStatisticsState> MergeStatisticsHistory(
        IReadOnlyList<DailyStatistics> history,
        DailyStatistics? today)
    {
        var result = new List<DailyStatisticsState>();

        // Add historical entries (excluding today if present)
        foreach (var stat in history)
        {
            if (today == null || stat.Date != today.Date)
            {
                result.Add(new DailyStatisticsState
                {
                    Date = stat.Date,
                    FocusSessionsCompleted = stat.FocusSessionsCompleted,
                    TotalFocusTime = stat.TotalFocusTime,
                    TotalBreakTime = stat.TotalBreakTime,
                    CyclesCompleted = stat.CyclesCompleted,
                    SessionRecords = stat.SessionRecords
                        .Select(r => new SessionRecordState
                        {
                            Goal = r.Goal,
                            Results = r.Results,
                            Rating = r.Rating,
                            Estimate = r.Estimate,
                            Duration = r.TimeSpent,
                            StartedAt = r.StartedAt,
                            CompletedAt = r.CompletedAt
                        })
                        .ToList(),
                    SupplementalActivities = stat.SupplementalActivities
                        .Select(a => new SupplementalActivityState
                        {
                            Description = a.Description
                        })
                        .ToList()
                });
            }
        }

        // Add today's statistics if present and has data
        if (today is { HasData: true })
        {
            result.Add(new DailyStatisticsState
            {
                Date = today.Date,
                FocusSessionsCompleted = today.FocusSessionsCompleted,
                TotalFocusTime = today.TotalFocusTime,
                TotalBreakTime = today.TotalBreakTime,
                CyclesCompleted = today.CyclesCompleted,
                SessionRecords = today.SessionRecords
                    .Select(r => new SessionRecordState
                    {
                        Goal = r.Goal,
                        Results = r.Results,
                        Rating = r.Rating,
                        Estimate = r.Estimate,
                        Duration = r.TimeSpent,
                        StartedAt = r.StartedAt,
                        CompletedAt = r.CompletedAt
                    })
                    .ToList(),
                SupplementalActivities = today.SupplementalActivities
                    .Select(a => new SupplementalActivityState
                    {
                        Description = a.Description
                    })
                    .ToList()
            });
        }

        // Sort by date descending (most recent first)
        result.Sort((a, b) => b.Date.CompareTo(a.Date));

        return result;
    }

    /// <summary>
    /// Restores a Session from the saved state.
    /// </summary>
    public Session? RestoreSession()
    {
        if (CurrentSession == null) return null;

        var session = CurrentSession.Type switch
        {
            SessionType.Focus => Session.CreateFocus(CurrentSession.Duration, CurrentSession.Goal, CurrentSession.AllowsOvertime),
            SessionType.ShortBreak => Session.CreateShortBreak(CurrentSession.Duration),
            SessionType.LongBreak => Session.CreateLongBreak(CurrentSession.Duration),
            _ => Session.CreateFocus(CurrentSession.Duration, CurrentSession.Goal)
        };

        session.Status = CurrentSession.Status;
        session.SetTimeRemaining(CurrentSession.TimeRemaining);
        session.StartedAt = CurrentSession.StartedAt;
        session.CompletedAt = CurrentSession.CompletedAt;

        return session;
    }

    /// <summary>
    /// Restores a PomodoroCycle from the saved state.
    /// </summary>
    public PomodoroCycle RestoreCycle()
    {
        var cycle = new PomodoroCycle();
        cycle.SetCompletedFocusSessions(Cycle.CompletedFocusSessions);
        return cycle;
    }

    /// <summary>
    /// Restores DailyStatistics from the saved state.
    /// </summary>
    public DailyStatistics? RestoreStatistics()
    {
        if (TodayStatistics == null) return null;

        var stats = new DailyStatistics
        {
            Date = TodayStatistics.Date,
            FocusSessionsCompleted = TodayStatistics.FocusSessionsCompleted,
            TotalFocusTime = TodayStatistics.TotalFocusTime,
            TotalBreakTime = TodayStatistics.TotalBreakTime,
            CyclesCompleted = TodayStatistics.CyclesCompleted
        };

        foreach (var recordState in TodayStatistics.SessionRecords)
        {
            stats.AddSessionRecord(new SessionRecord
            {
                Goal = recordState.Goal,
                Results = recordState.Results,
                Rating = recordState.Rating,
                Estimate = recordState.Estimate,
                TimeSpent = recordState.Duration,
                StartedAt = recordState.StartedAt,
                CompletedAt = recordState.CompletedAt
            });
        }

        foreach (var activityState in TodayStatistics.SupplementalActivities)
        {
            stats.AddSupplementalActivity(new SupplementalActivity
            {
                Description = activityState.Description
            });
        }

        return stats;
    }

    /// <summary>
    /// Restores the statistics history from the saved state.
    /// </summary>
    public List<DailyStatistics> RestoreStatisticsHistory()
    {
        var result = new List<DailyStatistics>();

        foreach (var state in StatisticsHistory)
        {
            var stats = new DailyStatistics
            {
                Date = state.Date,
                FocusSessionsCompleted = state.FocusSessionsCompleted,
                TotalFocusTime = state.TotalFocusTime,
                TotalBreakTime = state.TotalBreakTime,
                CyclesCompleted = state.CyclesCompleted
            };

            foreach (var recordState in state.SessionRecords)
            {
                stats.AddSessionRecord(new SessionRecord
                {
                    Goal = recordState.Goal,
                    Results = recordState.Results,
                    Rating = recordState.Rating,
                    Estimate = recordState.Estimate,
                    TimeSpent = recordState.Duration,
                    StartedAt = recordState.StartedAt,
                    CompletedAt = recordState.CompletedAt
                });
            }

            foreach (var activityState in state.SupplementalActivities)
            {
                stats.AddSupplementalActivity(new SupplementalActivity
                {
                    Description = activityState.Description
                });
            }

            result.Add(stats);
        }

        return result;
    }
}
