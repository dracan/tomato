# Tasks: Pomodoro Timer

**Input**: Design documents from `/specs/001-pomodoro-timer/`  
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**Tests**: Included per Constitution Principle IV (Test-Driven Development)

**Organization**: Tasks grouped by user story for independent implementation and testing

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story (US1-US5) this task belongs to
- Exact file paths included in descriptions

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Create .NET solution structure and install dependencies

- [ ] T001 Create solution and project structure: `dotnet new sln -n Tomato -o src`, `dotnet new wpf -n Tomato -o src/Tomato`, `dotnet new xunit -n Tomato.Tests -o src/Tomato.Tests`
- [ ] T002 Add projects to solution and create project reference from Tomato.Tests to Tomato
- [ ] T003 [P] Install CommunityToolkit.Mvvm package in src/Tomato/Tomato.csproj
- [ ] T004 [P] Install NAudio package in src/Tomato/Tomato.csproj
- [ ] T005 [P] Install xUnit, FluentAssertions, NSubstitute packages in src/Tomato.Tests/Tomato.Tests.csproj
- [ ] T006 [P] Create folder structure: Models/, Services/, ViewModels/, Views/, Converters/, Resources/ in src/Tomato/
- [ ] T007 [P] Create folder structure: Unit/, Integration/ in src/Tomato.Tests/
- [ ] T008 Configure Tomato.csproj for .NET 8.0-windows with nullable enabled and ImplicitUsings

**Checkpoint**: Solution builds successfully with `dotnet build src/Tomato.sln`

---

## Phase 2: Foundational (Core Infrastructure)

**Purpose**: Models, interfaces, and services that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Models (No Dependencies)

- [ ] T009 [P] Create SessionType enum in src/Tomato/Models/SessionType.cs
- [ ] T010 [P] Create SessionStatus enum in src/Tomato/Models/SessionStatus.cs
- [ ] T011 [P] Create Session class with factory methods in src/Tomato/Models/Session.cs
- [ ] T012 [P] Create PomodoroCycle class in src/Tomato/Models/PomodoroCycle.cs
- [ ] T013 [P] Create DailyStatistics class in src/Tomato/Models/DailyStatistics.cs
- [ ] T014 Create AppState class in src/Tomato/Models/AppState.cs

### Service Interfaces

- [ ] T015 [P] Create IDateTimeProvider interface in src/Tomato/Services/IDateTimeProvider.cs
- [ ] T016 [P] Create ITimerService interface with TimerTickEventArgs in src/Tomato/Services/ITimerService.cs
- [ ] T017 [P] Create ISessionManager interface with SessionStateChangedEventArgs in src/Tomato/Services/ISessionManager.cs
- [ ] T018 [P] Create INotificationService interface in src/Tomato/Services/INotificationService.cs
- [ ] T019 [P] Create IPersistenceService interface in src/Tomato/Services/IPersistenceService.cs

### Core Service Implementations

- [ ] T020 Create DateTimeProvider implementation in src/Tomato/Services/DateTimeProvider.cs
- [ ] T021 Write unit tests for TimerService in src/Tomato.Tests/Unit/TimerServiceTests.cs (TDD: tests first, expect failures)
- [ ] T022 Implement TimerService with System.Timers.Timer + Stopwatch in src/Tomato/Services/TimerService.cs
- [ ] T023 Write unit tests for PersistenceService in src/Tomato.Tests/Unit/PersistenceServiceTests.cs (TDD: tests first)
- [ ] T024 Implement PersistenceService with System.Text.Json in src/Tomato/Services/PersistenceService.cs

### WPF Infrastructure

- [ ] T025 [P] Create TimeSpanToStringConverter in src/Tomato/Converters/TimeSpanToStringConverter.cs
- [ ] T026 [P] Create SessionTypeToColorConverter in src/Tomato/Converters/SessionTypeToColorConverter.cs
- [ ] T027 Create Styles.xaml with base application styles in src/Tomato/Resources/Styles.xaml
- [ ] T028 Update App.xaml to reference Styles.xaml and register converters

**Checkpoint**: All models, interfaces, and core services compile. TimerService and PersistenceService tests pass.

---

## Phase 3: User Story 1 - Start a Focus Session (Priority: P1) 🎯 MVP

**Goal**: User can start a 25-minute focus timer and see countdown with notification on completion

**Independent Test**: Launch app → Click Start → Timer counts down → Notification plays at 0:00

### Tests for User Story 1 (TDD)

- [ ] T029 [P] [US1] Write SessionManager tests for StartFocusSession in src/Tomato.Tests/Unit/SessionManagerTests.cs
- [ ] T030 [P] [US1] Write TimerViewModel tests for start command and display updates in src/Tomato.Tests/Unit/TimerViewModelTests.cs
- [ ] T031 [P] [US1] Write integration test for focus session flow in src/Tomato.Tests/Integration/SessionFlowTests.cs

### Implementation for User Story 1

- [ ] T032 [US1] Implement SessionManager.StartFocusSession() and CompleteSession() in src/Tomato/Services/SessionManager.cs
- [ ] T033 [US1] Create NotificationService with NAudio playback in src/Tomato/Services/NotificationService.cs
- [ ] T034 [P] [US1] Add notification.wav sound file to src/Tomato/Resources/Sounds/notification.wav
- [ ] T035 [US1] Create TimerViewModel with StartFocusCommand and timer display properties in src/Tomato/ViewModels/TimerViewModel.cs
- [ ] T036 [US1] Create MainViewModel to host TimerViewModel in src/Tomato/ViewModels/MainViewModel.cs
- [ ] T037 [US1] Create TimerView.xaml with timer display (remaining time, session type) in src/Tomato/Views/TimerView.xaml
- [ ] T038 [US1] Update MainWindow.xaml to host TimerView with Start button in src/Tomato/MainWindow.xaml
- [ ] T039 [US1] Wire up dependency injection in App.xaml.cs (services → viewmodels → views)
- [ ] T040 [US1] Handle timer completion: play notification and update session status

**Checkpoint**: App launches, Start button begins 25-minute countdown, notification plays at completion. US1 tests pass.

---

## Phase 4: User Story 2 - Take Breaks Between Sessions (Priority: P2)

**Goal**: After focus session completes, user can start a 5-minute short break with distinct visual

**Independent Test**: Complete focus session → Click "Start Break" → 5-min break countdown → Visual shows "Break"

### Tests for User Story 2 (TDD)

- [ ] T041 [P] [US2] Write SessionManager tests for StartBreakSession in src/Tomato.Tests/Unit/SessionManagerTests.cs
- [ ] T042 [P] [US2] Write TimerViewModel tests for break session display in src/Tomato.Tests/Unit/TimerViewModelTests.cs

### Implementation for User Story 2

- [ ] T043 [US2] Implement SessionManager.StartBreakSession() in src/Tomato/Services/SessionManager.cs
- [ ] T044 [US2] Add StartBreakCommand to TimerViewModel in src/Tomato/ViewModels/TimerViewModel.cs
- [ ] T045 [US2] Update TimerView.xaml to show session type with color coding (Focus=Red, Break=Green) in src/Tomato/Views/TimerView.xaml
- [ ] T046 [US2] Add "Start Break" button that appears after focus completion in src/Tomato/Views/TimerView.xaml
- [ ] T047 [US2] After break completes, prompt to start next focus session

**Checkpoint**: Focus → Break → Focus flow works. Session types are visually distinct. US2 tests pass.

---

## Phase 5: User Story 3 - Pause and Resume Timer (Priority: P3)

**Goal**: User can pause running timer and resume from where it stopped

**Independent Test**: Start timer → Pause → Verify countdown stopped → Resume → Verify continues from paused time

### Tests for User Story 3 (TDD)

- [ ] T048 [P] [US3] Write SessionManager tests for PauseSession/ResumeSession in src/Tomato.Tests/Unit/SessionManagerTests.cs
- [ ] T049 [P] [US3] Write TimerService tests for Pause/Resume accuracy in src/Tomato.Tests/Unit/TimerServiceTests.cs
- [ ] T050 [P] [US3] Write TimerViewModel tests for pause/resume commands in src/Tomato.Tests/Unit/TimerViewModelTests.cs

### Implementation for User Story 3

- [ ] T051 [US3] Implement SessionManager.PauseSession() and ResumeSession() in src/Tomato/Services/SessionManager.cs
- [ ] T052 [US3] Add PauseCommand and ResumeCommand to TimerViewModel in src/Tomato/ViewModels/TimerViewModel.cs
- [ ] T053 [US3] Update TimerView.xaml with Pause/Resume buttons (show based on state) in src/Tomato/Views/TimerView.xaml
- [ ] T054 [US3] Add visual indicator for paused state (e.g., pulsing or grayed timer) in src/Tomato/Views/TimerView.xaml
- [ ] T055 [US3] Implement SessionManager.CancelSession() for Stop button in src/Tomato/Services/SessionManager.cs
- [ ] T056 [US3] Add StopCommand to TimerViewModel in src/Tomato/ViewModels/TimerViewModel.cs

**Checkpoint**: Pause/Resume works correctly. Remaining time preserved. Paused state is visually clear. US3 tests pass.

---

## Phase 6: User Story 4 - Long Break After Multiple Sessions (Priority: P4)

**Goal**: After 4 completed focus sessions, offer 15-minute long break instead of 5-minute short break

**Independent Test**: Complete 4 focus sessions → Verify "Long Break (15 min)" offered instead of "Short Break (5 min)"

### Tests for User Story 4 (TDD)

- [ ] T057 [P] [US4] Write PomodoroCycle tests for cycle tracking in src/Tomato.Tests/Unit/PomodoroCycleTests.cs
- [ ] T058 [P] [US4] Write SessionManager tests for GetNextBreakType logic in src/Tomato.Tests/Unit/SessionManagerTests.cs
- [ ] T059 [P] [US4] Write integration test for full 4-session cycle in src/Tomato.Tests/Integration/SessionFlowTests.cs

### Implementation for User Story 4

- [ ] T060 [US4] Implement PomodoroCycle tracking in SessionManager in src/Tomato/Services/SessionManager.cs
- [ ] T061 [US4] Implement GetNextBreakType() returning ShortBreak or LongBreak in src/Tomato/Services/SessionManager.cs
- [ ] T062 [US4] Update TimerViewModel to show correct break type after focus completion in src/Tomato/ViewModels/TimerViewModel.cs
- [ ] T063 [US4] Add cycle position indicator to UI (e.g., "Session 2 of 4") in src/Tomato/Views/TimerView.xaml
- [ ] T064 [US4] Reset cycle after long break completes in src/Tomato/Services/SessionManager.cs

**Checkpoint**: 4 focus sessions trigger long break. Cycle resets after long break. US4 tests pass.

---

## Phase 7: User Story 5 - Track Completed Sessions (Priority: P5)

**Goal**: Display count of focus sessions completed today, reset at midnight

**Independent Test**: Complete multiple focus sessions → Verify count increases → Close/reopen app → Verify count persists → Next day → Verify reset to 0

### Tests for User Story 5 (TDD)

- [ ] T065 [P] [US5] Write DailyStatistics tests for increment and reset in src/Tomato.Tests/Unit/DailyStatisticsTests.cs
- [ ] T066 [P] [US5] Write SessionManager tests for daily stats update in src/Tomato.Tests/Unit/SessionManagerTests.cs
- [ ] T067 [P] [US5] Write persistence tests for state recovery in src/Tomato.Tests/Integration/PersistenceTests.cs

### Implementation for User Story 5

- [ ] T068 [US5] Implement DailyStatistics tracking in SessionManager in src/Tomato/Services/SessionManager.cs
- [ ] T069 [US5] Add midnight reset check using IDateTimeProvider in src/Tomato/Services/SessionManager.cs
- [ ] T070 [US5] Add CompletedSessionsToday property to TimerViewModel in src/Tomato/ViewModels/TimerViewModel.cs
- [ ] T071 [US5] Display session count in TimerView.xaml (e.g., "🍅 3 sessions today") in src/Tomato/Views/TimerView.xaml
- [ ] T072 [US5] Persist AppState on session completion via IPersistenceService in src/Tomato/Services/SessionManager.cs
- [ ] T073 [US5] Load persisted state on app startup in App.xaml.cs
- [ ] T074 [US5] Handle app restart mid-session: restore timer state or show completion

**Checkpoint**: Session count displays and persists. Midnight reset works. App restart recovers state. US5 tests pass.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that span multiple user stories

- [ ] T075 [P] Add toggle for sound notifications (FR-017) in src/Tomato/Views/TimerView.xaml
- [ ] T076 Implement sound toggle in NotificationService and persist preference in src/Tomato/Services/NotificationService.cs
- [ ] T077 [P] Add accessibility: keyboard shortcuts (Space=Start/Pause, Esc=Stop) in src/Tomato/MainWindow.xaml.cs
- [ ] T078 [P] Add window always-on-top option for timer visibility
- [ ] T079 Handle edge case: rapid button clicks with command debouncing in src/Tomato/ViewModels/TimerViewModel.cs
- [ ] T080 Add error handling for audio playback failures (graceful fallback) in src/Tomato/Services/NotificationService.cs
- [ ] T081 [P] Create app icon and add to src/Tomato/Resources/
- [ ] T082 Run quickstart.md validation: verify all setup commands work
- [ ] T083 Final integration test: complete full Pomodoro cycle (4 focus + 3 short breaks + 1 long break)

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup ──────────────────────┐
                                     │
Phase 2: Foundational ◄──────────────┘
         (Models, Interfaces, Core Services)
              │
              │  BLOCKS ALL USER STORIES
              ▼
     ┌────────┴────────┬────────────┬────────────┬────────────┐
     │                 │            │            │            │
Phase 3: US1 ◄────────►  Phase 4: US2          Phase 5: US3 │
(P1 - MVP)              (P2)        │           (P3)         │
     │                              │            │            │
     │                 ┌────────────┘            │            │
     ▼                 ▼                         │            │
             Phase 6: US4 ◄──────────────────────┘            │
             (P4 - depends on US1/US2 for session flow)       │
                       │                                      │
                       ▼                                      │
             Phase 7: US5 ◄───────────────────────────────────┘
             (P5 - depends on completion tracking from all stories)
                       │
                       ▼
             Phase 8: Polish
```

### User Story Dependencies

| Story | Can Start After | Dependencies |
|-------|-----------------|--------------|
| US1 (P1) | Phase 2 complete | None - core MVP |
| US2 (P2) | Phase 2 complete | Shares timer with US1, independently testable |
| US3 (P3) | Phase 2 complete | Shares timer with US1, independently testable |
| US4 (P4) | Phase 2 complete | Needs focus session flow from US1 |
| US5 (P5) | Phase 2 complete | Needs session completion events |

### Within Each User Story

1. Tests written FIRST (TDD) - expect failures
2. Models before services (if any new models)
3. Services before ViewModels
4. ViewModels before Views
5. Verify tests pass before marking story complete

---

## Parallel Execution Examples

### Phase 2: Foundational Tasks

```
# All models can be created in parallel:
T009, T010, T011, T012, T013 → then T014 (AppState depends on others)

# All interfaces can be created in parallel:
T015, T016, T017, T018, T019

# Converters in parallel:
T025, T026
```

### User Story 1: Implementation

```
# Tests in parallel (all marked [P]):
T029, T030, T031

# After tests fail, implementation:
T032 (SessionManager) → T035 (ViewModel) → T037 (View)
T033 (NotificationService) in parallel with T032
T034 (sound file) anytime
```

### Multi-Developer Scenario

```
Developer A: US1 (MVP) → US4 (Long breaks)
Developer B: US2 (Breaks) → US5 (Tracking)
Developer C: US3 (Pause/Resume) → Polish

All start after Phase 2 is complete.
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. ✅ Complete Phase 1: Setup
2. ✅ Complete Phase 2: Foundational
3. ✅ Complete Phase 3: User Story 1
4. **VALIDATE**: Start timer → countdown → notification
5. **DEPLOY/DEMO**: Minimal viable Pomodoro timer

### Incremental Delivery

| Milestone | Stories Included | User Value |
|-----------|------------------|------------|
| MVP | US1 | Can time focus sessions |
| v0.2 | US1 + US2 | Focus + breaks |
| v0.3 | US1-US3 | Pause/resume for interruptions |
| v0.4 | US1-US4 | Full Pomodoro cycle |
| v1.0 | US1-US5 + Polish | Complete app with tracking |

---

## Summary

| Phase | Task Count | Parallel Opportunities |
|-------|------------|------------------------|
| Setup | 8 | T003-T007 parallel |
| Foundational | 20 | T009-T013, T015-T019, T025-T026 parallel |
| US1 (MVP) | 12 | T029-T031, T034 parallel |
| US2 | 7 | T041-T042 parallel |
| US3 | 9 | T048-T050 parallel |
| US4 | 8 | T057-T059 parallel |
| US5 | 10 | T065-T067 parallel |
| Polish | 9 | T075, T077, T078, T081 parallel |

**Total Tasks**: 83  
**MVP Tasks (US1 only)**: 40 (Setup + Foundational + US1)
