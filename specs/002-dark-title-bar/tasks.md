# Tasks: Dark Title Bar

**Input**: Design documents from `/specs/002-dark-title-bar/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, quickstart.md ✅

**Tests**: Not requested - P/Invoke to Windows DWM API cannot be meaningfully unit tested (see Constitution Check in plan.md)

**Organization**: Tasks organized by user story for independent implementation and verification.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Create helper infrastructure for Windows API interop

- [X] T001 Create Helpers directory at src/Tomato/Helpers/
- [X] T002 Create DwmHelper.cs with P/Invoke declarations and version detection in src/Tomato/Helpers/DwmHelper.cs

---

## Phase 2: User Story 1 - Consistent Dark Theme Experience (Priority: P1) 🎯 MVP

**Goal**: Enable dark title bar when Windows dark mode is enabled

**Independent Test**: Launch app on Windows 10 1809+/Windows 11 with dark mode → title bar should be dark

### Implementation for User Story 1

- [X] T003 [US1] Add `SetDarkTitleBar` method to DwmHelper with Windows version detection in src/Tomato/Helpers/DwmHelper.cs
- [X] T004 [US1] Override `OnSourceInitialized` in MainWindow to call DwmHelper.SetDarkTitleBar in src/Tomato/MainWindow.xaml.cs
- [X] T005 [US1] Build and verify dark title bar appears on Windows with dark mode enabled

**Checkpoint**: User Story 1 complete - app displays dark title bar matching the dark theme

---

## Phase 3: User Story 2 - Respect OS Theme Preference (Priority: P2)

**Goal**: Verify graceful fallback on unsupported Windows versions

**Independent Test**: Launch app on older Windows → should show default title bar without errors

### Verification for User Story 2

> Note: Version detection and fallback logic is already implemented as part of T002-T003. These tasks verify the behavior.

- [X] T006 [US2] Verify `IsWindows10OrGreater` correctly detects Windows build version in src/Tomato/Helpers/DwmHelper.cs
- [X] T007 [US2] Verify `SetDarkTitleBar` returns false (no-op) on unsupported Windows versions
- [X] T008 [US2] Verify no exceptions thrown when HWND is invalid (IntPtr.Zero)

**Checkpoint**: User Story 2 complete - app gracefully falls back on unsupported systems

---

## Phase 4: Polish & Verification

**Purpose**: Final validation and documentation

- [X] T009 Run quickstart.md verification checklist
- [X] T010 [P] Verify window states (active, inactive, maximized, restored) all show dark title bar
- [X] T011 [P] Verify window controls (minimize, maximize, close) remain visible and functional
- [X] T012 [P] Verify high contrast mode does not cause errors (system overrides title bar styling)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies - start immediately
- **Phase 2 (US1)**: Depends on Phase 1 completion
- **Phase 3 (US2)**: Depends on Phase 2 completion (extends DwmHelper with fallback logic)
- **Phase 4 (Polish)**: Depends on all user stories complete

### Task Dependencies

```
T001 → T002 → T003 → T004 → T005
                ↓
        T006 → T007 → T008
                        ↓
                      T009
                        ↓
              T010 ─┬─ T011 ─┬─ T012  [parallel]
```

### Parallel Opportunities

- T010, T011, T012 can run in parallel (independent verification steps)

---

## Parallel Example: Verification Phase

```bash
# Launch verification tasks together:
Task T010: "Verify window states (active, inactive, maximized, restored)"
Task T011: "Verify window controls (minimize, maximize, close)"
Task T012: "Verify high contrast mode behavior"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 2: User Story 1 (T003-T005)
3. **STOP and VALIDATE**: Launch app, verify dark title bar appears
4. Deploy if ready - core issue resolved

### Full Implementation

1. Complete MVP (US1)
2. Add US2 fallback handling (T006-T008)
3. Run verification checklist (T009-T011)
4. All edge cases handled

---

## Notes

- This is a small, focused feature - approximately 50 lines of new code
- No unit tests required (P/Invoke cannot be meaningfully tested)
- Manual verification via quickstart.md checklist
- All tasks modify only 2 files: DwmHelper.cs (new) and MainWindow.xaml.cs (modified)
