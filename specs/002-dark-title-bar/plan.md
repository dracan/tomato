# Implementation Plan: Dark Title Bar

**Branch**: `002-dark-title-bar` | **Date**: 2026-01-09 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-dark-title-bar/spec.md`

## Summary

Enable dark title bar styling for the WPF window to match the application's dark theme (#1E1E1E background). The solution uses the Windows DWM API (`DwmSetWindowAttribute` with `DWMWA_USE_IMMERSIVE_DARK_MODE`) to signal the OS that the application prefers dark chrome, ensuring visual consistency with the app content and other dark-mode-aware applications.

## Technical Context

**Language/Version**: C# / .NET 8.0-windows
**Primary Dependencies**: WPF, CommunityToolkit.Mvvm 8.4.0, NAudio 2.2.1
**Storage**: N/A (no data model changes)
**Testing**: xUnit (existing test project)
**Target Platform**: Windows 10 1809+ (Build 17763+), Windows 11
**Project Type**: Single WPF desktop application
**Performance Goals**: Title bar styling must complete during window initialization with < 50ms overhead
**Constraints**: Must gracefully fallback on unsupported Windows versions; native Windows chrome only (no custom-drawn title bar)
**Scale/Scope**: Single MainWindow requiring title bar styling

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Design Check (Phase 0)

| Principle | Status | Notes |
|-----------|--------|-------|
| I. User Focus First | ✅ PASS | Dark title bar reduces visual distraction; no disruption to focus sessions |
| II. Simplicity Over Features | ✅ PASS | Single P/Invoke call during initialization; no new UI complexity |
| III. Reliability & Accuracy | ✅ PASS | No impact on timer logic; graceful fallback on older systems |
| IV. Test-Driven Development | ⚠️ LIMITED | P/Invoke calls to DWM API cannot be unit tested; manual visual verification required |
| V. Progressive Enhancement | ✅ PASS | Core timer works regardless of title bar styling; degrades gracefully |

**Gate Result**: PASS - Proceed with implementation.

### Post-Design Check (Phase 1)

| Principle | Status | Notes |
|-----------|--------|-------|
| I. User Focus First | ✅ PASS | Design adds no user-facing complexity; single initialization call |
| II. Simplicity Over Features | ✅ PASS | One new file (DwmHelper.cs), ~50 lines; minimal MainWindow changes |
| III. Reliability & Accuracy | ✅ PASS | Graceful fallback via version check; no exceptions on unsupported systems |
| IV. Test-Driven Development | ⚠️ JUSTIFIED | P/Invoke to OS API cannot be unit tested; verification checklist in research.md |
| V. Progressive Enhancement | ✅ PASS | App fully functional if dark title bar fails; degradation is invisible |

**Final Gate Result**: PASS - TDD limitation justified as Windows API interaction cannot be meaningfully unit tested.

## Project Structure

### Documentation (this feature)

```text
specs/002-dark-title-bar/
├── plan.md              # This file
├── research.md          # Phase 0 output - Windows DWM API research
├── quickstart.md        # Phase 1 output - Implementation guide
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Tomato/
│   ├── MainWindow.xaml.cs       # Modified: Add dark title bar initialization
│   └── Helpers/                 # New: Add DwmHelper class for P/Invoke
│       └── DwmHelper.cs
└── Tomato.Tests/
    └── (no new tests - P/Invoke untestable)
```

**Structure Decision**: Single project structure maintained. New `Helpers` folder for Windows API interop code to keep concerns separated from view logic.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| TDD limitation | Windows P/Invoke calls cannot be meaningfully mocked | Alternative would be integration test framework with UI automation - excessive complexity for simple visual fix |
