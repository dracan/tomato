# Research: Pomodoro Timer

**Date**: 2026-01-07
**Feature**: Pomodoro Timer Desktop Application
**Status**: Complete

---

## 1. Desktop UI Framework Selection

### Decision: WPF (Windows Presentation Foundation)

### Rationale:
- **Maturity**: WPF is the most mature .NET desktop UI framework with 15+ years of production use
- **Documentation**: Extensive documentation and community resources
- **MVVM Support**: First-class support for MVVM pattern with data binding
- **.NET 8 LTS Support**: Fully supported on .NET 8 with modern C# features
- **Simplicity**: Single-window timer app doesn't need cross-platform or modern Fluent Design
- **Tooling**: Excellent Visual Studio and VS Code support for XAML editing

### Alternatives Considered:

| Framework | Pros | Cons | Verdict |
|-----------|------|------|---------|
| **WinUI 3** | Modern Fluent Design, Windows 11 native | Newer/less stable, requires Windows App SDK setup, more complex | Overkill for simple timer |
| **.NET MAUI** | Cross-platform (Win/Mac/iOS/Android) | Larger footprint, cross-platform complexity not needed | Windows-only is acceptable |
| **Windows Forms** | Simple, fast to develop | Dated appearance, poor scaling | Not modern enough |
| **Avalonia** | Cross-platform, WPF-like | Smaller ecosystem, additional learning | Not needed for Windows-only |

---

## 2. MVVM Toolkit Selection

### Decision: CommunityToolkit.Mvvm (v8.x)

### Rationale:
- **Official Microsoft Package**: Maintained by Microsoft as part of .NET Community Toolkit
- **Source Generators**: Uses source generators for minimal boilerplate (no runtime reflection)
- **Performance**: Zero-overhead abstractions with compile-time code generation
- **Modern C#**: Leverages latest C# features (partial properties, attributes)
- **Well-Documented**: Excellent documentation and samples

### Key Features Used:
- `[ObservableProperty]` attribute for bindable properties
- `[RelayCommand]` attribute for ICommand implementations
- `ObservableObject` base class for INotifyPropertyChanged
- `IMessenger` for loosely-coupled messaging (if needed)

### Alternatives Considered:
- **Prism**: More features but heavier, navigation framework overkill for single-window app
- **ReactiveUI**: Powerful but steep learning curve, reactive paradigm adds complexity
- **Manual INotifyPropertyChanged**: Too much boilerplate code

---

## 3. Timer Implementation Strategy

### Decision: System.Timers.Timer with UI Dispatcher

### Rationale:
- **Accuracy**: More accurate than DispatcherTimer for long-running intervals
- **Thread Safety**: Runs on thread pool, updates UI via Dispatcher
- **Testability**: Can be abstracted behind ITimerService interface
- **Drift Compensation**: Implementation will track elapsed time, not just intervals

### Implementation Notes:
```csharp
// Timer fires every 100ms for responsive UI updates
// Actual elapsed time tracked with Stopwatch for accuracy
// Remaining time = TotalDuration - StopwatchElapsed
```

### Alternatives Considered:
- **DispatcherTimer**: Simpler but can drift under UI load
- **Task.Delay loop**: Modern async pattern but harder to pause/resume accurately
- **Multimedia Timer**: Overkill for 1-second accuracy requirement

---

## 4. Audio Notification Strategy

### Decision: NAudio Library

### Rationale:
- **Reliability**: Industry-standard .NET audio library
- **Format Support**: WAV, MP3, and other formats supported
- **Cross-platform Potential**: Works on Windows, potential future expansion
- **Active Maintenance**: Actively maintained with .NET 8 support

### Implementation Notes:
- Bundle default notification sound as embedded resource
- Graceful fallback if audio unavailable (visual notification only)
- User preference to enable/disable sound

### Alternatives Considered:
- **System.Media.SoundPlayer**: Windows-only, limited format support, deprecated feel
- **Windows.Media.Playback**: UWP API, requires additional package references
- **No external library**: Limited functionality

---

## 5. Persistence Strategy

### Decision: JSON File Storage with System.Text.Json

### Rationale:
- **Built-in**: No external dependencies required
- **Performance**: Fast and efficient for small data
- **Human Readable**: Easy to debug and manually inspect
- **Serialization**: Source generators available for AOT support

### Storage Location:
- **Path**: `%LOCALAPPDATA%\Tomato\state.json`
- **Backup**: Write to temp file, then atomic rename

### Data Persisted:
- Current session state (for app restart recovery)
- Daily statistics (sessions completed, total focus time)
- User preferences (sound enabled/disabled)

---

## 6. Testing Strategy

### Decision: xUnit + FluentAssertions + NSubstitute

### Rationale:
- **xUnit**: Modern, extensible, excellent async support
- **FluentAssertions**: Readable assertions, better error messages
- **NSubstitute**: Simple mocking syntax, works well with interfaces

### Test Categories:
1. **Unit Tests**: Timer logic, session state machine, cycle tracking
2. **Integration Tests**: Persistence round-trip, session flow scenarios
3. **ViewModel Tests**: Command execution, property change notifications

### Key Test Scenarios:
- Timer accuracy over full 25-minute session (fast-forwarded)
- Session state transitions (focus → break → focus)
- Pause/resume preserves remaining time
- Long break triggers after 4 focus sessions
- State persistence and recovery

---

## 7. Project Template Selection

### Decision: dotnet new wpf with manual MVVM setup

### Rationale:
- **Simplicity**: Standard WPF template is well-understood
- **Control**: Full control over project structure and dependencies
- **Modern Setup**: Add CommunityToolkit.Mvvm via NuGet

### Commands:
```powershell
dotnet new sln -n Tomato
dotnet new wpf -n Tomato -o src/Tomato
dotnet new xunit -n Tomato.Tests -o src/Tomato.Tests
dotnet sln add src/Tomato src/Tomato.Tests
```

---

## Summary

All technical decisions have been resolved. The implementation will use:

| Aspect | Choice |
|--------|--------|
| UI Framework | WPF (.NET 8) |
| Architecture | MVVM with CommunityToolkit.Mvvm |
| Timer | System.Timers.Timer + Stopwatch for accuracy |
| Audio | NAudio for notifications |
| Persistence | System.Text.Json to LocalAppData |
| Testing | xUnit + FluentAssertions + NSubstitute |

No NEEDS CLARIFICATION items remain.
