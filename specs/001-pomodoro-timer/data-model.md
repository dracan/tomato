# Data Model: Pomodoro Timer

**Date**: 2026-01-07
**Feature**: Pomodoro Timer Desktop Application

---

## Entity Relationship Diagram

```
┌─────────────────────┐       ┌─────────────────────┐
│      Session        │       │    PomodoroCycle    │
├─────────────────────┤       ├─────────────────────┤
│ Id: Guid            │       │ CurrentPosition: int│
│ Type: SessionType   │◄──────│ CompletedSessions:  │
│ Duration: TimeSpan  │       │   int               │
│ RemainingTime:      │       │ LastUpdated:        │
│   TimeSpan          │       │   DateTime          │
│ Status: SessionStatus│      └─────────────────────┘
│ StartedAt: DateTime?│
│ PausedAt: DateTime? │
│ CompletedAt:        │
│   DateTime?         │
└─────────────────────┘
           │
           │ persisted with
           ▼
┌─────────────────────┐       ┌─────────────────────┐
│    AppState         │       │  DailyStatistics    │
├─────────────────────┤       ├─────────────────────┤
│ CurrentSession:     │       │ Date: DateOnly      │
│   Session?          │       │ CompletedFocusSessions│
│ Cycle: PomodoroCycle│       │   : int             │
│ TodayStats:         │◄──────│ TotalFocusMinutes:  │
│   DailyStatistics   │       │   int               │
│ SoundEnabled: bool  │       └─────────────────────┘
│ LastSaved: DateTime │
└─────────────────────┘
```

---

## Entities

### 1. SessionType (Enum)

Represents the type of timer session.

```csharp
public enum SessionType
{
    Focus = 0,        // 25-minute work session
    ShortBreak = 1,   // 5-minute break
    LongBreak = 2     // 15-minute break (after 4 focus sessions)
}
```

| Value | Duration | Description |
|-------|----------|-------------|
| Focus | 25 min | Work/concentration period |
| ShortBreak | 5 min | Rest between focus sessions |
| LongBreak | 15 min | Extended rest after 4 focus sessions |

---

### 2. SessionStatus (Enum)

Represents the current state of a session.

```csharp
public enum SessionStatus
{
    NotStarted = 0,   // Session created but not yet started
    Running = 1,      // Timer actively counting down
    Paused = 2,       // Timer paused, can be resumed
    Completed = 3,    // Timer reached zero naturally
    Cancelled = 4     // Timer stopped before completion
}
```

**State Transitions:**

```
                    ┌──────────────┐
                    │  NotStarted  │
                    └──────┬───────┘
                           │ Start
                           ▼
              ┌───────► Running ◄────────┐
              │            │              │
              │  Resume    │ Pause        │
              │            ▼              │
              └──────── Paused ───────────┘
                           │
                    Stop   │   (Timer hits 0)
                           ▼
              ┌─────────────────────────┐
              │  Cancelled  │ Completed │
              └─────────────────────────┘
```

---

### 3. Session

Represents a single timed interval (focus or break).

```csharp
public class Session
{
    public Guid Id { get; init; }
    public SessionType Type { get; init; }
    public TimeSpan Duration { get; init; }
    public TimeSpan RemainingTime { get; set; }
    public SessionStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

| Property | Type | Description | Validation |
|----------|------|-------------|------------|
| Id | Guid | Unique identifier | Required, auto-generated |
| Type | SessionType | Focus, ShortBreak, or LongBreak | Required |
| Duration | TimeSpan | Total session length | > 0, typically 25/5/15 min |
| RemainingTime | TimeSpan | Time left on timer | >= 0, <= Duration |
| Status | SessionStatus | Current session state | Required |
| StartedAt | DateTime? | When timer was first started | Set on first start |
| PausedAt | DateTime? | When timer was last paused | Null when running |
| CompletedAt | DateTime? | When session ended | Set on completion/cancel |

**Factory Methods:**
```csharp
public static Session CreateFocus() => new()
{
    Id = Guid.NewGuid(),
    Type = SessionType.Focus,
    Duration = TimeSpan.FromMinutes(25),
    RemainingTime = TimeSpan.FromMinutes(25),
    Status = SessionStatus.NotStarted
};

public static Session CreateShortBreak() => new() { /* 5 min */ };
public static Session CreateLongBreak() => new() { /* 15 min */ };
```

---

### 4. PomodoroCycle

Tracks position within the 4-session Pomodoro cycle.

```csharp
public class PomodoroCycle
{
    public int CurrentPosition { get; set; }      // 1-4
    public int CompletedInCycle { get; set; }     // 0-4
    public DateTime LastUpdated { get; set; }
}
```

| Property | Type | Description | Validation |
|----------|------|-------------|------------|
| CurrentPosition | int | Current session number in cycle | 1-4 |
| CompletedInCycle | int | Focus sessions completed in current cycle | 0-4 |
| LastUpdated | DateTime | Timestamp of last modification | Auto-updated |

**Business Rules:**
- After 4 completed focus sessions, offer LongBreak
- After LongBreak completes, reset CompletedInCycle to 0
- CurrentPosition cycles: 1 → 2 → 3 → 4 → 1

---

### 5. DailyStatistics

Aggregate data for a single calendar day.

```csharp
public class DailyStatistics
{
    public DateOnly Date { get; init; }
    public int CompletedFocusSessions { get; set; }
    public int TotalFocusMinutes { get; set; }
}
```

| Property | Type | Description | Validation |
|----------|------|-------------|------------|
| Date | DateOnly | Calendar date (local time) | Required |
| CompletedFocusSessions | int | Count of completed focus sessions | >= 0 |
| TotalFocusMinutes | int | Sum of focus time in minutes | >= 0 |

**Business Rules:**
- Reset to zero at midnight local time (FR-016)
- Increment CompletedFocusSessions when a Focus session completes
- Add 25 to TotalFocusMinutes for each completed focus session

---

### 6. AppState

Root persistence object containing all application state.

```csharp
public class AppState
{
    public Session? CurrentSession { get; set; }
    public PomodoroCycle Cycle { get; set; } = new();
    public DailyStatistics TodayStats { get; set; } = new();
    public bool SoundEnabled { get; set; } = true;
    public DateTime LastSaved { get; set; }
}
```

| Property | Type | Description |
|----------|------|-------------|
| CurrentSession | Session? | Active/paused session, null if none |
| Cycle | PomodoroCycle | Current position in Pomodoro cycle |
| TodayStats | DailyStatistics | Today's completed sessions count |
| SoundEnabled | bool | User preference for audio notifications |
| LastSaved | DateTime | Timestamp of last persistence |

**Persistence Location:** `%LOCALAPPDATA%\Tomato\state.json`

---

## Validation Rules Summary

| Entity | Rule | Error Message |
|--------|------|---------------|
| Session | Duration > TimeSpan.Zero | "Duration must be positive" |
| Session | RemainingTime >= TimeSpan.Zero | "Remaining time cannot be negative" |
| Session | RemainingTime <= Duration | "Remaining time cannot exceed duration" |
| PomodoroCycle | CurrentPosition in 1..4 | "Position must be between 1 and 4" |
| PomodoroCycle | CompletedInCycle in 0..4 | "Completed count must be between 0 and 4" |
| DailyStatistics | CompletedFocusSessions >= 0 | "Session count cannot be negative" |

---

## JSON Schema (for persistence)

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "currentSession": {
      "type": ["object", "null"],
      "properties": {
        "id": { "type": "string", "format": "uuid" },
        "type": { "type": "integer", "enum": [0, 1, 2] },
        "durationSeconds": { "type": "integer", "minimum": 1 },
        "remainingSeconds": { "type": "integer", "minimum": 0 },
        "status": { "type": "integer", "enum": [0, 1, 2, 3, 4] },
        "startedAt": { "type": ["string", "null"], "format": "date-time" },
        "pausedAt": { "type": ["string", "null"], "format": "date-time" },
        "completedAt": { "type": ["string", "null"], "format": "date-time" }
      }
    },
    "cycle": {
      "type": "object",
      "properties": {
        "currentPosition": { "type": "integer", "minimum": 1, "maximum": 4 },
        "completedInCycle": { "type": "integer", "minimum": 0, "maximum": 4 }
      }
    },
    "todayStats": {
      "type": "object",
      "properties": {
        "date": { "type": "string", "format": "date" },
        "completedFocusSessions": { "type": "integer", "minimum": 0 },
        "totalFocusMinutes": { "type": "integer", "minimum": 0 }
      }
    },
    "soundEnabled": { "type": "boolean" },
    "lastSaved": { "type": "string", "format": "date-time" }
  }
}
```
