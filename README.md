# Tomato

A minimal Windows desktop Pomodoro timer.

![Banner](Assets/ReadmeImages/TomatoBanner.jpg)

## Features

- 25-minute focus sessions with short and long breaks
- 4-session cycles (Focus → Break → Focus → Break → Focus → Break → Focus → Long Break)
- Custom-length sessions from the right-click or tray menu: 5/10/15/20-minute focus, a 10-minute break, or a 15-minute long break
- Goal setting dialog when starting a focus session
- Quick-capture todos during focus sessions (right-click menu)
- Results dialog when a focus session completes (includes captured todos)
- Session statistics tracking with historical data
- View Stats report (right-click → View Stats) - generates an HTML report with:
  - Today's stats (sessions, focus time, break time, cycles)
  - Today's individual sessions with goals and results
  - All-time totals
  - Daily history with expandable session details
- Sound notifications on session completion
- State persistence across restarts (a session running when the app closes is restored as paused)
- Compact, always-on-top window
- System tray icon with color-coded session state and context menu
- Slack integration - automatically sets status and enables Do Not Disturb during focus sessions

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

### Quick-capture todos

During a focus session, right-click and select "Add Todo" to quickly capture a thought or task. Todos are displayed:
- When the session completes normally (in the feedback dialog)
- When you stop a session early that has captured todos (in a dedicated todos dialog)

Todos are formatted as markdown checkboxes for easy copy/paste:

```markdown
- [ ] Buy groceries
- [ ] Call mom
- [ ] Review PR
```

Todos are temporary and cleared after you start a new focus session.

### Report generation

![Stats report](Assets/ReadmeImages/Report.png)

## Slack Integration

Tomato can automatically update your Slack status and enable Do Not Disturb when you start a focus session.

### Setup

1. Create a Slack App at [api.slack.com/apps](https://api.slack.com/apps)
2. Add the following OAuth scopes under "User Token Scopes":
   - `users.profile:write` - to set your status
   - `dnd:write` - to enable Do Not Disturb
3. Install the app to your workspace and copy the "User OAuth Token" (starts with `xoxp-`)
4. Run `Tomato.exe --setup-slack` and paste your token
5. Click "Test Connection" to verify it works, then "Save"

### Behavior

- **Focus session starts or resumes**: Status changes to 🍅 "Focus time" and DND is enabled, both set to expire when the session would end
- **Focus session is paused, ends, or the app exits**: Status is cleared and DND is disabled
- **Breaks**: No changes to Slack status

Your token is stored encrypted using Windows DPAPI at `%LOCALAPPDATA%\Tomato\slack.json`. If Slack rejects an update, the reason is written to `%LOCALAPPDATA%\Tomato\error.log`.

## Saved data

Tomato keeps its data in `%LOCALAPPDATA%\Tomato`:

- `state.json`: the current session, cycle progress and all statistics history. A session running when the app closes is saved as paused, so you can resume it next time.
- `error.log`: unexpected errors, and Slack updates that were rejected. It starts a new file once it passes 1 MB.
- `slack.json` / `luxafor.json`: integration settings, encrypted with Windows DPAPI.

To avoid losing history:

- `state.json` is written to a temporary file first and then swapped in, so a crash or power cut during a save can't leave it half-written.
- If `state.json` can't be read as valid data, it's renamed to `state.corrupt-<date>-<time>.json` (so the history in it can be recovered by hand) and the app starts with no history.
- If `state.json` exists but is locked by another program (for example antivirus or a sync client) when Tomato starts, Tomato starts with no history and doesn't save anything until it's restarted, so the file isn't overwritten. Sessions from that run aren't kept.

## Download

Download the latest release from the [Releases](https://github.com/dracan/tomato/releases) page.

Extract the zip and run `Tomato.exe`.

## Building from Source

Requires [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet build Tomato.sln
dotnet run --project Tomato/Tomato.csproj
```

## Running Tests

```bash
dotnet test Tomato.sln
```
