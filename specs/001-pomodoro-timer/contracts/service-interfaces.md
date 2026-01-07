# Service Contracts: Pomodoro Timer

**Date**: 2026-01-07
**Feature**: Pomodoro Timer Desktop Application

This document defines the internal service interfaces (contracts) for the Pomodoro Timer application. These interfaces enable testability and separation of concerns.

---

## 1. ITimerService

Manages the countdown timer with accurate time tracking.

```csharp
/// <summary>
/// Provides countdown timer functionality with pause/resume support.
/// </summary>
public interface ITimerService
{
    /// <summary>
    /// Raised every tick (approximately 100ms) while timer is running.
    /// </summary>
    event EventHandler<TimerTickEventArgs>? Tick;

    /// <summary>
    /// Raised when the timer reaches zero.
    /// </summary>
    event EventHandler? Completed;

    /// <summary>
    /// Gets the current remaining time.
    /// </summary>
    TimeSpan RemainingTime { get; }

    /// <summary>
    /// Gets whether the timer is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the timer with the specified duration.
    /// </summary>
    /// <param name="duration">Total countdown duration.</param>
    void Start(TimeSpan duration);

    /// <summary>
    /// Resumes the timer from the specified remaining time.
    /// </summary>
    /// <param name="remainingTime">Time remaining when paused.</param>
    void Resume(TimeSpan remainingTime);

    /// <summary>
    /// Pauses the timer and returns the remaining time.
    /// </summary>
    /// <returns>The remaining time at pause.</returns>
    TimeSpan Pause();

    /// <summary>
    /// Stops the timer completely.
    /// </summary>
    void Stop();
}

public class TimerTickEventArgs : EventArgs
{
    public TimeSpan RemainingTime { get; init; }
    public TimeSpan ElapsedTime { get; init; }
}
```

---

## 2. ISessionManager

Manages session lifecycle and Pomodoro cycle logic.

```csharp
/// <summary>
/// Manages session state transitions and Pomodoro cycle tracking.
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Gets the current session, or null if no session is active.
    /// </summary>
    Session? CurrentSession { get; }

    /// <summary>
    /// Gets the current Pomodoro cycle state.
    /// </summary>
    PomodoroCycle Cycle { get; }

    /// <summary>
    /// Gets today's statistics.
    /// </summary>
    DailyStatistics TodayStats { get; }

    /// <summary>
    /// Raised when session state changes.
    /// </summary>
    event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;

    /// <summary>
    /// Creates and starts a new focus session.
    /// </summary>
    /// <returns>The created session.</returns>
    Session StartFocusSession();

    /// <summary>
    /// Creates and starts the next break session (short or long based on cycle).
    /// </summary>
    /// <returns>The created break session.</returns>
    Session StartBreakSession();

    /// <summary>
    /// Pauses the current session.
    /// </summary>
    void PauseSession();

    /// <summary>
    /// Resumes the current paused session.
    /// </summary>
    void ResumeSession();

    /// <summary>
    /// Stops and cancels the current session.
    /// </summary>
    void CancelSession();

    /// <summary>
    /// Marks the current session as completed.
    /// Called when timer reaches zero.
    /// </summary>
    void CompleteSession();

    /// <summary>
    /// Determines what type of break should follow a completed focus session.
    /// </summary>
    /// <returns>ShortBreak or LongBreak based on cycle position.</returns>
    SessionType GetNextBreakType();
}

public class SessionStateChangedEventArgs : EventArgs
{
    public Session Session { get; init; } = null!;
    public SessionStatus OldStatus { get; init; }
    public SessionStatus NewStatus { get; init; }
}
```

---

## 3. INotificationService

Handles audio and visual notifications.

```csharp
/// <summary>
/// Provides notification capabilities for session events.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Gets or sets whether sound notifications are enabled.
    /// </summary>
    bool SoundEnabled { get; set; }

    /// <summary>
    /// Plays the session completion notification.
    /// </summary>
    /// <param name="sessionType">The type of session that completed.</param>
    Task PlayCompletionNotificationAsync(SessionType sessionType);

    /// <summary>
    /// Shows a toast/system notification.
    /// </summary>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification message.</param>
    Task ShowToastNotificationAsync(string title, string message);
}
```

---

## 4. IPersistenceService

Handles saving and loading application state.

```csharp
/// <summary>
/// Persists and restores application state.
/// </summary>
public interface IPersistenceService
{
    /// <summary>
    /// Saves the current application state to disk.
    /// </summary>
    /// <param name="state">The state to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveStateAsync(AppState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the application state from disk.
    /// Returns a new default state if no saved state exists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded or default state.</returns>
    Task<AppState> LoadStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the path where state is persisted.
    /// </summary>
    string StatePath { get; }
}
```

---

## 5. IDateTimeProvider

Abstracts system time for testability.

```csharp
/// <summary>
/// Provides current date and time. Abstracted for unit testing.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current local date and time.
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Gets the current local date.
    /// </summary>
    DateOnly Today { get; }
}
```

---

## ViewModel Contracts

### TimerViewModel

Primary ViewModel for the timer display and controls.

```csharp
public interface ITimerViewModel
{
    // Display Properties
    string RemainingTimeDisplay { get; }    // "25:00" format
    string SessionTypeDisplay { get; }       // "Focus", "Short Break", "Long Break"
    int CompletedSessionsToday { get; }
    int CyclePosition { get; }               // 1-4
    bool IsRunning { get; }
    bool IsPaused { get; }
    bool CanStart { get; }
    bool CanPause { get; }
    bool CanResume { get; }
    bool CanStop { get; }

    // Progress (for visual indicators)
    double ProgressPercent { get; }          // 0.0 to 1.0

    // Commands
    IRelayCommand StartFocusCommand { get; }
    IRelayCommand StartBreakCommand { get; }
    IRelayCommand PauseCommand { get; }
    IRelayCommand ResumeCommand { get; }
    IRelayCommand StopCommand { get; }
    IRelayCommand ToggleSoundCommand { get; }
}
```

---

## Event Flow Diagram

```
User Action              Service Layer                    ViewModel
    │                         │                               │
    │  Click Start            │                               │
    ├────────────────────────►│ StartFocusCommand            │
    │                         ├──────────────────────────────►│
    │                         │ SessionManager.StartFocusSession()
    │                         │ TimerService.Start(25min)     │
    │                         │◄──────────────────────────────┤
    │                         │                               │
    │                    Timer Tick (100ms)                   │
    │                         ├──────────────────────────────►│
    │                         │ RemainingTimeDisplay updated  │
    │                         │ PropertyChanged raised        │
    │◄────────────────────────┼───────────────────────────────┤
    │  UI Updates             │                               │
    │                         │                               │
    │                    Timer Completed                      │
    │                         ├──────────────────────────────►│
    │                         │ SessionManager.CompleteSession()
    │                         │ NotificationService.PlayNotification()
    │                         │ PersistenceService.SaveState()│
    │◄────────────────────────┼───────────────────────────────┤
    │  Notification           │                               │
```

---

## Dependency Injection Registration

```csharp
// In App.xaml.cs or ServiceCollection setup
services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
services.AddSingleton<ITimerService, TimerService>();
services.AddSingleton<ISessionManager, SessionManager>();
services.AddSingleton<INotificationService, NotificationService>();
services.AddSingleton<IPersistenceService, PersistenceService>();
services.AddTransient<TimerViewModel>();
services.AddTransient<MainViewModel>();
```
