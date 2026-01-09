# Feature Specification: Dark Title Bar

**Feature Branch**: `002-dark-title-bar`
**Created**: 2026-01-09
**Status**: Draft
**Input**: User description: "The title bar's colouring should match the main window. At the moment it's grey, so conflicts with the dark-theme of the app. This has been seen on a Windows machine where dark mode is enabled for the entire OS, and all other windows do have dark title bars - so I'm unsure why this would have a grey title bar."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consistent Dark Theme Experience (Priority: P1)

As a user running the Tomato app on Windows with dark mode enabled, I expect the window title bar to match the dark theme of the application content, providing a seamless visual experience without a jarring grey title bar contrasting against the dark UI.

**Why this priority**: This is the core visual consistency issue. A grey title bar against a dark (#1E1E1E) application background creates an obvious visual disconnect that affects every user session from the moment the app launches.

**Independent Test**: Launch the Tomato application on Windows with OS dark mode enabled and verify the title bar displays in dark colors matching the application theme.

**Acceptance Scenarios**:

1. **Given** the Tomato app is installed on Windows with OS dark mode enabled, **When** the user launches the application, **Then** the window title bar should display with a dark background color that visually complements the app's dark theme
2. **Given** the app is running with a dark title bar, **When** the user interacts with the window (moving, resizing, focusing/unfocusing), **Then** the title bar should maintain appropriate dark styling throughout all states
3. **Given** the app has a dark title bar, **When** the user views the title bar text and window controls (minimize, maximize, close buttons), **Then** they should remain clearly visible and readable against the dark background

---

### User Story 2 - Respect OS Theme Preference (Priority: P2)

As a user who may switch between light and dark modes on Windows, I expect the Tomato app's title bar to respect and adapt to my OS theme preference where technically feasible.

**Why this priority**: While the primary issue is the dark theme mismatch, users who occasionally use light mode should still have a reasonable experience. However, given the app itself has a fixed dark theme, this is secondary to ensuring dark mode works correctly.

**Independent Test**: Toggle Windows between light and dark mode settings while the app is not running, then launch the app and observe the title bar appearance.

**Acceptance Scenarios**:

1. **Given** Windows is set to dark mode, **When** the user launches the Tomato app, **Then** the title bar should display in dark mode
2. **Given** Windows is set to light mode, **When** the user launches the Tomato app, **Then** the title bar should still display in dark mode (matching the app's fixed dark theme, since the app UI is always dark)

---

### Edge Cases

- **Runtime theme change**: If Windows theme changes while the app is running, the title bar will NOT update dynamically. User must restart the app to see the new theme applied. This is acceptable behavior for a simple productivity app.
- **Inactive/unfocused window**: Windows automatically handles inactive window title bar styling. The dark title bar will remain dark but may appear slightly dimmed per Windows conventions.
- **Older Windows versions**: On Windows 10 versions before 1809 (Build 17763) or Windows 8.1 and earlier, the app will silently fall back to the default system title bar styling. No errors or warnings will be shown.
- **High contrast themes**: When Windows high contrast mode is enabled, the system overrides application title bar styling. The app should not interfere with accessibility settings.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application window MUST display a dark title bar when running on Windows with dark mode enabled
- **FR-002**: The title bar color MUST visually complement the application's dark background (#1E1E1E or similar dark shade)
- **FR-003**: The title bar text (window title) MUST remain legible against the dark title bar background
- **FR-004**: The window control buttons (minimize, maximize, close) MUST remain visible and functional with the dark title bar
- **FR-005**: The title bar styling MUST persist across all window states (active, inactive, maximized, restored)
- **FR-006**: The application MUST gracefully handle systems where dark title bars are not supported by falling back to default system styling

### Non-Functional Requirements

- **NFR-001**: The title bar theming SHOULD have negligible impact on application startup time (less than 50ms additional delay)
- **NFR-002**: The solution SHOULD work on Windows 10 version 1809 and later (where dark title bar support is available)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of users on Windows with dark mode enabled see a dark title bar when launching the application
- **SC-002**: The title bar appearance is consistent with other native Windows applications using dark mode
- **SC-003**: Zero user reports of visual inconsistency between title bar and main application content after implementation
- **SC-004**: Application remains fully functional with no regressions in window management behavior (moving, resizing, minimizing, maximizing, closing)
- **SC-005**: Window title text remains readable with a contrast ratio of at least 4.5:1 against the title bar background

## Assumptions

- The application targets Windows 10/11 where modern title bar theming is supported
- The existing application framework supports title bar customization via Windows-provided mechanisms
- Users experiencing this issue have Windows dark mode enabled but the app is not properly signaling its preference to the system
- The fix will use standard Windows capabilities rather than custom-drawn title bars to maintain native look and feel

