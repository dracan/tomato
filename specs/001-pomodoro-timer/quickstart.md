# Quickstart: Pomodoro Timer

**Date**: 2026-01-07
**Feature**: Pomodoro Timer Desktop Application

---

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Windows 10 version 1809 or later (for WPF support)
- Visual Studio 2022 (17.8+) or VS Code with C# extension

---

## Quick Setup

### 1. Clone and Navigate

```powershell
cd C:\Code\Tomato
git checkout 001-pomodoro-timer
```

### 2. Create Project Structure

```powershell
# Create solution and projects
dotnet new sln -n Tomato -o src
dotnet new wpf -n Tomato -o src/Tomato
dotnet new xunit -n Tomato.Tests -o src/Tomato.Tests

# Add projects to solution
dotnet sln src/Tomato.sln add src/Tomato/Tomato.csproj
dotnet sln src/Tomato.sln add src/Tomato.Tests/Tomato.Tests.csproj

# Add project reference for tests
dotnet add src/Tomato.Tests/Tomato.Tests.csproj reference src/Tomato/Tomato.csproj
```

### 3. Install Dependencies

```powershell
# Navigate to main project
cd src/Tomato

# MVVM Toolkit
dotnet add package CommunityToolkit.Mvvm --version 8.2.2

# Audio notifications
dotnet add package NAudio --version 2.2.1

# Navigate to test project
cd ../Tomato.Tests

# Testing packages
dotnet add package FluentAssertions --version 6.12.0
dotnet add package NSubstitute --version 5.1.0

# Return to repo root
cd ../..
```

### 4. Build and Run

```powershell
# Build solution
dotnet build src/Tomato.sln

# Run application
dotnet run --project src/Tomato/Tomato.csproj

# Run tests
dotnet test src/Tomato.sln
```

---

## Project Structure After Setup

```
src/
├── Tomato.sln
├── Tomato/
│   ├── Tomato.csproj
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   └── (additional folders to be created during implementation)
└── Tomato.Tests/
    ├── Tomato.Tests.csproj
    └── UnitTest1.cs (placeholder)
```

---

## Development Workflow

### Run with Hot Reload

```powershell
dotnet watch run --project src/Tomato/Tomato.csproj
```

### Run Specific Tests

```powershell
# Run all tests
dotnet test src/Tomato.sln

# Run tests with output
dotnet test src/Tomato.sln --logger "console;verbosity=detailed"

# Run specific test class
dotnet test src/Tomato.sln --filter "FullyQualifiedName~TimerServiceTests"
```

### Build for Release

```powershell
dotnet publish src/Tomato/Tomato.csproj -c Release -r win-x64 --self-contained false
```

---

## Key Files to Create First

After project setup, create these files in order:

1. **Models** (no dependencies):
   - `Models/SessionType.cs`
   - `Models/SessionStatus.cs`
   - `Models/Session.cs`
   - `Models/PomodoroCycle.cs`
   - `Models/DailyStatistics.cs`
   - `Models/AppState.cs`

2. **Service Interfaces**:
   - `Services/IDateTimeProvider.cs`
   - `Services/ITimerService.cs`
   - `Services/ISessionManager.cs`
   - `Services/INotificationService.cs`
   - `Services/IPersistenceService.cs`

3. **Service Implementations** (with tests):
   - `Services/DateTimeProvider.cs`
   - `Services/TimerService.cs` + tests
   - `Services/SessionManager.cs` + tests
   - `Services/PersistenceService.cs` + tests
   - `Services/NotificationService.cs`

4. **ViewModels**:
   - `ViewModels/TimerViewModel.cs`
   - `ViewModels/MainViewModel.cs`

5. **Views**:
   - `MainWindow.xaml` (update)
   - `Views/TimerView.xaml`

---

## Configuration

### Target Framework (Tomato.csproj)

Ensure your project file uses .NET 8:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

### Data Storage Location

Application state is stored at:
```
%LOCALAPPDATA%\Tomato\state.json
```

On Windows, this typically resolves to:
```
C:\Users\{username}\AppData\Local\Tomato\state.json
```

---

## Troubleshooting

### Build Errors

| Error | Solution |
|-------|----------|
| "WPF is only supported on Windows" | Ensure you're running on Windows with .NET 8 Windows SDK |
| "Could not load file or assembly 'NAudio'" | Run `dotnet restore src/Tomato.sln` |
| "Target framework 'net8.0-windows' not found" | Install .NET 8 SDK from https://dotnet.microsoft.com |

### Runtime Issues

| Issue | Solution |
|-------|----------|
| No sound on notification | Check Windows sound settings; verify SoundEnabled = true |
| State not persisting | Check write permissions to %LOCALAPPDATA%\Tomato |
| Timer drifts | Ensure no other CPU-intensive tasks during testing |

---

## Next Steps

After completing setup:

1. Review [data-model.md](data-model.md) for entity definitions
2. Review [contracts/service-interfaces.md](contracts/service-interfaces.md) for service contracts
3. Run `/speckit.tasks` to generate the implementation task list
4. Start with Phase 1: Models and Interfaces (no external dependencies)
