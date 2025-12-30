<!--
  SYNC IMPACT REPORT
  ==================
  Version change: N/A → 1.0.0 (initial ratification)

  Modified principles: None (initial version)

  Added sections:
  - Core Principles (5 principles)
  - User Experience Standards
  - Development Workflow
  - Governance

  Removed sections: None (initial version)

  Templates requiring updates:
  - ✅ plan-template.md - Constitution Check section already present
  - ✅ spec-template.md - User stories with priorities already aligned
  - ✅ tasks-template.md - Task categories already aligned

  Follow-up TODOs: None
-->

# Tomato Constitution

A Pomodoro Timer application designed for focused productivity.

## Core Principles

### I. User Focus First

All features MUST prioritize the user's productivity and focus experience. Timer interactions MUST be minimal and non-disruptive during focus sessions. Notifications and alerts MUST respect the user's current session state. The interface MUST communicate remaining time and session state at a glance without requiring cognitive effort.

**Rationale**: A productivity tool that distracts users defeats its own purpose.

### II. Simplicity Over Features

The core timer experience MUST remain simple and accessible. New features MUST NOT add complexity to the basic start/pause/stop workflow. Configuration options SHOULD have sensible defaults that work for most users. YAGNI (You Aren't Gonna Need It) applies—implement only what's needed now.

**Rationale**: Pomodoro technique effectiveness relies on simplicity; feature bloat undermines focus.

### III. Reliability & Accuracy

Timer accuracy MUST be maintained regardless of device state (background, sleep, etc.). Session data MUST persist across application restarts and crashes. Audio/visual notifications MUST fire reliably when sessions end. Users MUST be able to trust the timer without monitoring it.

**Rationale**: Users cannot focus if they're worried about timer reliability.

### IV. Test-Driven Development

Tests MUST be written before implementation code. All timer logic MUST have unit tests covering edge cases (pause, resume, device sleep). Integration tests MUST verify notification delivery and session transitions. Red-Green-Refactor cycle strictly enforced.

**Rationale**: Timer logic errors directly impact user trust and productivity.

### V. Progressive Enhancement

Core timer functionality MUST work without network connectivity. Advanced features (sync, statistics, cloud backup) MUST NOT break core functionality. The application SHOULD degrade gracefully when optional dependencies are unavailable.

**Rationale**: Productivity tools must be available when users need them, regardless of environment.

## User Experience Standards

- **Session feedback**: Users MUST always know: current session type (work/break), time remaining, and session count
- **Minimal interactions**: Starting a focus session MUST require at most 2 taps/clicks from app launch
- **Interruption handling**: Pausing/stopping MUST be one-tap operations; accidental stops SHOULD be recoverable
- **Accessibility**: All interactive elements MUST be accessible; color MUST NOT be the only indicator of state
- **Sound options**: Users MUST be able to customize or disable audio notifications

## Development Workflow

- **Branching**: Feature branches MUST follow format `###-feature-name`
- **Documentation**: All features MUST have specifications before implementation begins
- **Code review**: All changes MUST be reviewed before merge
- **Quality gates**: All tests MUST pass; no regressions allowed
- **Commits**: Use conventional commits format (feat:, fix:, docs:, refactor:, test:)

## Governance

This constitution supersedes all other development practices for the Tomato project. Amendments require:
1. Written proposal documenting the change and rationale
2. Impact assessment on existing features
3. Version increment following semantic versioning (MAJOR.MINOR.PATCH)

All pull requests and code reviews MUST verify compliance with these principles. Any complexity beyond minimal viable solution MUST be justified against Principle II (Simplicity Over Features).

**Version**: 1.0.0 | **Ratified**: 2025-12-30 | **Last Amended**: 2025-12-30
