# Tomato

A minimal Windows desktop Pomodoro timer.

![Banner](Assets/ReadmeImages/TomatoBanner.jpg)

## Features

- 25-minute focus sessions with short and long breaks
- 4-session cycles (Focus → Break → Focus → Break → Focus → Break → Focus → Long Break)
- Goal setting dialog when starting a focus session
- Results dialog when a focus session completes
- Session statistics tracking with historical data
- View Stats report (right-click → View Stats) - generates an HTML report with:
  - Today's stats (sessions, focus time, break time, cycles)
  - Today's individual sessions with goals and results
  - All-time totals
  - Daily history with expandable session details
- Sound notifications on session completion
- State persistence across restarts
- Compact, always-on-top window

## Screenshots

### Non-intrusive always-on-top display

![Idle](Assets/ReadmeImages/Idle.png)
![Active Pomodoro](Assets/ReadmeImages/ActivePomodoro.png)
![Short Break](Assets/ReadmeImages/ShortBreak.png)
![Long Break](Assets/ReadmeImages/LongBreak.png)

### Custom times

![Right-click menu](Assets/ReadmeImages/RightClickMenu.png)

### Pomodoro goal and feedback entry

![Goal entry](Assets/ReadmeImages/Goal.png)
![Feedback entry](Assets/ReadmeImages/Feedback.png)

### Report generation

![Stats report](Assets/ReadmeImages/Report.png)

## Download

Download the latest release from the [Releases](https://github.com/dracan/tomato/releases) page.

Extract the zip and run `Tomato.exe`.

## Building from Source

Requires [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet build Tomato.sln
dotnet run --project Tomato/Tomato.csproj
```

## Running Tests

```bash
dotnet test Tomato.sln
```
