# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Tomato is a Windows desktop Pomodoro timer application built with C# .NET 10.0 and WPF using MVVM architecture.

## Build and Test Commands

```bash
# Build the solution
dotnet build Tomato.sln

# Run the application
dotnet run --project Tomato/Tomato.csproj

# Run all tests
dotnet test Tomato.sln

# Run a specific test class
dotnet test Tomato.Tests --filter "FullyQualifiedName~SessionManagerTests"

# Run a specific test method
dotnet test Tomato.Tests --filter "FullyQualifiedName~SessionManagerTests.StartFocus_SetsSessionStatusToRunning"
```

## Running the App Locally

The app always reads and writes the real `%LOCALAPPDATA%\Tomato` folder; there is no separate dev data folder. On a machine where Slack or Luxafor is set up (`slack.json` / `luxafor.json` in that folder), starting a focus session changes the real Slack status, Do Not Disturb and the Luxafor light, and every session is added to the real statistics.

Before running the app to test a change:

1. Back up the whole `%LOCALAPPDATA%\Tomato` folder.
2. Move `slack.json` and `luxafor.json` out of it, so the integrations stay off.
3. After testing, quit the app and restore the folder from the backup (including those two files).

## Architecture

### Core Patterns

- **MVVM with Source Generators**: ViewModels use `[ObservableProperty]` and `[RelayCommand]` attributes from CommunityToolkit.Mvvm
- **Manual Dependency Injection**: Services are instantiated and wired in `App.xaml.cs` OnStartup
- **Event-Driven Updates**: `ISessionManager` exposes `SessionStateChanged` and `TimerTick` events that ViewModels observe

### Key Services (`Tomato/Services/`)

- `ISessionManager` / `SessionManager`: State machine orchestrating session lifecycle, statistics, and cycle progression
- `ITimerService` / `TimerService`: High-accuracy timer using Stopwatch for drift compensation
- `IPersistenceService` / `PersistenceService`: JSON state persistence to `%LOCALAPPDATA%\Tomato\state.json` (see "Saved data" in README.md for how it guards against losing history)
- `INotificationService` / `NotificationService`: NAudio-based completion sounds
- `IDateTimeProvider`: DateTime abstraction for testability
- `ErrorLog`: appends unexpected errors to `%LOCALAPPDATA%\Tomato\error.log`; App's global exception handlers use it
- `HttpClients`: creates the HttpClients for Slack, Luxafor and update checks with a 10-second timeout

### State Management

- `Session`: Single Pomodoro session (focus or break) with factory methods
- `PomodoroCycle`: Tracks 4-session cycles (Focus→Break→Focus→Break→Focus→Break→Focus→LongBreak)
- `DailyStatistics`: Today's completed sessions and total time
- `AppState`: Serializable state model with nested `SessionState`, `CycleState`, `DailyStatisticsState`

### Testing (`Tomato.Tests/`)

- xUnit v3 with FluentAssertions for readable assertions
- NSubstitute for mocking service interfaces
- Tests are organized by the class they test (e.g., `SessionManagerTests`, `TimerViewModelTests`)
- The test project references `xunit.v3.mtp-off`, not plain `xunit.v3`, on purpose. Plain `xunit.v3` uses Microsoft.Testing.Platform, and with the .NET 10 SDK `dotnet test` then fails unless the whole repo opts into the new test runner, which also changes the `--filter` syntax above and the CI test step. Keep `mtp-off` unless you're making that switch deliberately.
- With NSubstitute, don't read a substitute's property inside another substitute's `Returns(...)` (e.g. `sub.History.Returns(new[] { sub.Today })`); it reconfigures the wrong call. Read it into a local variable first.

## Development Principles

- **Test-Driven Development**: Tests before implementation, Red-Green-Refactor cycle
- **Simplicity Over Features**: YAGNI applies; don't add complexity to basic start/pause/stop workflow
- **Reliability & Accuracy**: Timer accuracy must be maintained; session data must persist across restarts
- **Conventional Commits**: Use format `feat:`, `fix:`, `docs:`, `refactor:`, `test:`
- **Feature Branches**: Follow format `###-feature-name`
- **Keep README Updated**: When adding or changing features, update README.md to reflect the changes
- **No Auto-Push**: Never run `git push` unless explicitly asked by the user
