# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Tomato is a Windows desktop Pomodoro timer application built with C# .NET 8.0 and WPF using MVVM architecture.

## Build and Test Commands

```bash
# Build the solution
dotnet build src/Tomato.sln

# Run the application
dotnet run --project src/Tomato/Tomato.csproj

# Run all tests
dotnet test src/Tomato.sln

# Run a specific test class
dotnet test src/Tomato.Tests --filter "FullyQualifiedName~SessionManagerTests"

# Run a specific test method
dotnet test src/Tomato.Tests --filter "FullyQualifiedName~SessionManagerTests.StartSession_WhenNotStarted_SetsStatusToRunning"
```

## Architecture

### Core Patterns

- **MVVM with Source Generators**: ViewModels use `[ObservableProperty]` and `[RelayCommand]` attributes from CommunityToolkit.Mvvm
- **Manual Dependency Injection**: Services are instantiated and wired in `App.xaml.cs` OnStartup
- **Event-Driven Updates**: `ISessionManager` exposes `SessionStateChanged` and `TimerTick` events that ViewModels observe

### Key Services (`src/Tomato/Services/`)

- `ISessionManager` / `SessionManager`: State machine orchestrating session lifecycle, statistics, and cycle progression
- `ITimerService` / `TimerService`: High-accuracy timer using Stopwatch for drift compensation
- `IPersistenceService` / `PersistenceService`: JSON state persistence to `%LOCALAPPDATA%\Tomato\state.json`
- `INotificationService` / `NotificationService`: NAudio-based completion sounds
- `IDateTimeProvider`: DateTime abstraction for testability

### State Management

- `Session`: Single Pomodoro session (focus or break) with factory methods
- `PomodoroCycle`: Tracks 4-session cycles (Focus→Break→Focus→Break→Focus→Break→Focus→LongBreak)
- `DailyStatistics`: Today's completed sessions and total time
- `AppState`: Serializable state model with nested `SessionState`, `CycleState`, `DailyStatisticsState`

### Testing (`src/Tomato.Tests/`)

- xUnit with FluentAssertions for readable assertions
- NSubstitute for mocking service interfaces
- Tests are organized by the class they test (e.g., `SessionManagerTests`, `TimerViewModelTests`)

## Development Principles

From the project constitution (`.specify/memory/constitution.md`):

- **Test-Driven Development**: Tests before implementation, Red-Green-Refactor cycle
- **Simplicity Over Features**: YAGNI applies; don't add complexity to basic start/pause/stop workflow
- **Reliability & Accuracy**: Timer accuracy must be maintained; session data must persist across restarts
- **Conventional Commits**: Use format `feat:`, `fix:`, `docs:`, `refactor:`, `test:`
- **Feature Branches**: Follow format `###-feature-name`

## Feature Specifications

Feature specifications live in `specs/###-feature-name/` directories with:
- `spec.md`: Requirements and user stories
- `plan.md`: Implementation approach
- `tasks.md`: Breakdown of implementation tasks
- `research.md`: Technical research and decisions
