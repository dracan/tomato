using System.IO;
using System.Windows;
using System.Windows.Forms;
using Tomato.Models;
using Tomato.ViewModels;
using DrawingColor = System.Drawing.Color;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingGraphics = System.Drawing.Graphics;
using DrawingIcon = System.Drawing.Icon;
using DrawingSolidBrush = System.Drawing.SolidBrush;

namespace Tomato.Services;

/// <summary>
/// Manages the system tray icon, displaying session state via color and providing a context menu.
/// </summary>
public sealed class SystemTrayService : ISystemTrayService
{
    // Color values matching SessionStateToBrushConverter
    private static readonly DrawingColor FocusColor = DrawingColor.FromArgb(220, 53, 69);      // #DC3545
    private static readonly DrawingColor ShortBreakColor = DrawingColor.FromArgb(40, 167, 69); // #28A745
    private static readonly DrawingColor LongBreakColor = DrawingColor.FromArgb(0, 123, 255);  // #007BFF
    private static readonly DrawingColor IdleColor = DrawingColor.FromArgb(74, 74, 74);        // #4A4A4A

    private readonly ISessionManager _sessionManager;

    private NotifyIcon? _notifyIcon;
    private Window? _mainWindow;
    private TimerViewModel? _timerViewModel;

    // Cached colored icons
    private DrawingIcon? _focusIcon;
    private DrawingIcon? _shortBreakIcon;
    private DrawingIcon? _longBreakIcon;
    private DrawingIcon? _idleIcon;

    private bool _disposed;

    public SystemTrayService(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public void Initialize(Window mainWindow, TimerViewModel timerViewModel)
    {
        _mainWindow = mainWindow;
        _timerViewModel = timerViewModel;

        // Generate colored icons
        GenerateColoredIcons();

        // Create the notify icon
        _notifyIcon = new NotifyIcon
        {
            Icon = _idleIcon,
            Text = "Tomato - Ready",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        // Handle left-click to show/restore window
        _notifyIcon.Click += OnNotifyIconClick;

        // Subscribe to session manager events
        _sessionManager.SessionStateChanged += OnSessionStateChanged;
        _sessionManager.TimerTick += OnTimerTick;

        // Set initial state based on current session
        UpdateIconFromCurrentState();
    }

    private void GenerateColoredIcons()
    {
        // Load the original icon from resources
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "tomato.ico");

        // Try to load from file first, then from embedded resource
        DrawingIcon? originalIcon = null;
        if (File.Exists(iconPath))
        {
            originalIcon = new DrawingIcon(iconPath);
        }
        else
        {
            // Try to load from the application's embedded resources
            var resourceStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/tomato.ico"));
            if (resourceStream != null)
            {
                originalIcon = new DrawingIcon(resourceStream.Stream);
            }
        }

        if (originalIcon != null)
        {
            _focusIcon = AddStatusIndicator(originalIcon, FocusColor);
            _shortBreakIcon = AddStatusIndicator(originalIcon, ShortBreakColor);
            _longBreakIcon = AddStatusIndicator(originalIcon, LongBreakColor);
            // For idle, just use the original tomato icon without any indicator
            _idleIcon = (DrawingIcon)originalIcon.Clone();
            originalIcon.Dispose();
        }
        else
        {
            // Fallback: create simple colored square icons if tomato.ico is not found
            _focusIcon = CreateSimpleIcon(FocusColor);
            _shortBreakIcon = CreateSimpleIcon(ShortBreakColor);
            _longBreakIcon = CreateSimpleIcon(LongBreakColor);
            _idleIcon = CreateSimpleIcon(IdleColor);
        }
    }

    private static DrawingIcon AddStatusIndicator(DrawingIcon original, DrawingColor indicatorColor)
    {
        using var bitmap = original.ToBitmap();
        var result = new DrawingBitmap(bitmap.Width, bitmap.Height);
        using var graphics = DrawingGraphics.FromImage(result);

        // Draw the original icon
        graphics.DrawImage(bitmap, 0, 0);

        // Add a colored status indicator dot in the bottom-right corner
        int dotSize = Math.Max(4, bitmap.Width / 4);
        int dotX = bitmap.Width - dotSize - 1;
        int dotY = bitmap.Height - dotSize - 1;

        // Draw a white outline for visibility
        using (var outlineBrush = new DrawingSolidBrush(DrawingColor.White))
        {
            graphics.FillEllipse(outlineBrush, dotX - 1, dotY - 1, dotSize + 2, dotSize + 2);
        }

        // Draw the colored indicator
        using (var indicatorBrush = new DrawingSolidBrush(indicatorColor))
        {
            graphics.FillEllipse(indicatorBrush, dotX, dotY, dotSize, dotSize);
        }

        var iconHandle = result.GetHicon();
        var icon = DrawingIcon.FromHandle(iconHandle);
        return (DrawingIcon)icon.Clone();
    }

    private static DrawingIcon CreateSimpleIcon(DrawingColor color)
    {
        const int size = 16;
        using var bitmap = new DrawingBitmap(size, size);
        using var graphics = DrawingGraphics.FromImage(bitmap);
        using var brush = new DrawingSolidBrush(color);

        graphics.FillEllipse(brush, 1, 1, size - 2, size - 2);

        var iconHandle = bitmap.GetHicon();
        var icon = DrawingIcon.FromHandle(iconHandle);
        return (DrawingIcon)icon.Clone();
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        // Add Todo
        var addTodoItem = new ToolStripMenuItem("Add Todo");
        addTodoItem.Click += (_, _) => _mainWindow?.Dispatcher.Invoke(() =>
        {
            if (_timerViewModel?.ShowTodoInputCommand.CanExecute(null) == true)
            {
                ShowAndActivateWindow();
                _timerViewModel.ShowTodoInputCommand.Execute(null);
            }
        });
        menu.Items.Add(addTodoItem);

        // Add Supplemental Activity
        var addSupplementalItem = new ToolStripMenuItem("Add Supplemental Activity");
        addSupplementalItem.Click += (_, _) => _mainWindow?.Dispatcher.Invoke(() =>
        {
            _timerViewModel?.AddSupplementalActivityCommand.Execute(null);
        });
        menu.Items.Add(addSupplementalItem);

        menu.Items.Add(new ToolStripSeparator());

        // Restart
        var restartItem = new ToolStripMenuItem("Restart");
        restartItem.Click += (_, _) => _mainWindow?.Dispatcher.Invoke(() =>
        {
            if (_timerViewModel?.RestartCommand.CanExecute(null) == true)
            {
                _timerViewModel.RestartCommand.Execute(null);
            }
        });
        menu.Items.Add(restartItem);

        menu.Items.Add(new ToolStripSeparator());

        // Duration options
        AddDurationMenuItem(menu, "20 minutes", "20");
        AddDurationMenuItem(menu, "15 minutes", "15");
        AddDurationMenuItem(menu, "10 minutes", "10");
        AddDurationMenuItem(menu, "5 minutes", "5");

        menu.Items.Add(new ToolStripSeparator());

        // Long Break
        var longBreakItem = new ToolStripMenuItem("Long Break (15 min)");
        longBreakItem.Click += (_, _) => _mainWindow?.Dispatcher.Invoke(() =>
        {
            _timerViewModel?.StartLongBreakCommand.Execute(null);
        });
        menu.Items.Add(longBreakItem);

        menu.Items.Add(new ToolStripSeparator());

        // View Stats
        var viewStatsItem = new ToolStripMenuItem("View Stats");
        viewStatsItem.Click += (_, _) => _mainWindow?.Dispatcher.Invoke(() =>
        {
            _timerViewModel?.ViewStatsCommand.Execute(null);
        });
        menu.Items.Add(viewStatsItem);

        menu.Items.Add(new ToolStripSeparator());

        // Show Window
        var showWindowItem = new ToolStripMenuItem("Show Window");
        showWindowItem.Click += (_, _) => ShowAndActivateWindow();
        menu.Items.Add(showWindowItem);

        // Exit
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => _mainWindow?.Dispatcher.Invoke(() =>
        {
            Application.Current.Shutdown();
        });
        menu.Items.Add(exitItem);

        // Update menu item states when menu opens
        menu.Opening += (_, _) =>
        {
            addTodoItem.Enabled = _timerViewModel?.ShowTodoInputCommand.CanExecute(null) == true;
            restartItem.Enabled = _timerViewModel?.RestartCommand.CanExecute(null) == true;
        };

        return menu;
    }

    private void AddDurationMenuItem(ContextMenuStrip menu, string text, string minutes)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += (_, _) => _mainWindow?.Dispatcher.Invoke(() =>
        {
            _timerViewModel?.StartFocusWithDurationCommand.Execute(minutes);
        });
        menu.Items.Add(item);
    }

    private void OnNotifyIconClick(object? sender, EventArgs e)
    {
        // Only respond to left-click (not right-click which shows menu)
        if (e is MouseEventArgs mouseArgs && mouseArgs.Button == MouseButtons.Left)
        {
            ShowAndActivateWindow();
        }
    }

    private void ShowAndActivateWindow()
    {
        _mainWindow?.Dispatcher.Invoke(() =>
        {
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }

            _mainWindow.Show();
            _mainWindow.Activate();
        });
    }

    private void OnSessionStateChanged(object? sender, SessionStateChangedEventArgs e)
    {
        UpdateIcon(e.Session.Type, e.NewStatus);
        UpdateTooltip(e.Session, e.NewStatus);
    }

    private void OnTimerTick(object? sender, TimerTickEventArgs e)
    {
        if (_notifyIcon == null || _sessionManager.CurrentSession == null)
            return;

        var session = _sessionManager.CurrentSession;
        UpdateTooltip(session, session.Status);
    }

    private void UpdateIconFromCurrentState()
    {
        var session = _sessionManager.CurrentSession;
        if (session == null)
        {
            SetIcon(_idleIcon);
            UpdateTooltipText("Tomato - Ready");
            return;
        }

        UpdateIcon(session.Type, session.Status);
        UpdateTooltip(session, session.Status);
    }

    private void UpdateIcon(SessionType sessionType, SessionStatus status)
    {
        // Show idle icon when not running or paused
        if (status != SessionStatus.Running && status != SessionStatus.Paused)
        {
            SetIcon(_idleIcon);
            return;
        }

        // Show session type color when active
        var icon = sessionType switch
        {
            SessionType.Focus => _focusIcon,
            SessionType.ShortBreak => _shortBreakIcon,
            SessionType.LongBreak => _longBreakIcon,
            _ => _idleIcon
        };

        SetIcon(icon);
    }

    private void SetIcon(DrawingIcon? icon)
    {
        if (_notifyIcon != null && icon != null)
        {
            _notifyIcon.Icon = icon;
        }
    }

    private void UpdateTooltip(Session session, SessionStatus status)
    {
        if (status != SessionStatus.Running && status != SessionStatus.Paused)
        {
            UpdateTooltipText("Tomato - Ready");
            return;
        }

        var sessionLabel = session.Type switch
        {
            SessionType.Focus => "Focus",
            SessionType.ShortBreak => "Short Break",
            SessionType.LongBreak => "Long Break",
            _ => "Session"
        };

        var timeText = FormatTimeRemaining(session.TimeRemaining);
        var pausedText = status == SessionStatus.Paused ? " (Paused)" : "";

        // NotifyIcon.Text has 64-char limit
        var tooltip = $"Tomato - {sessionLabel}: {timeText}{pausedText}";
        if (tooltip.Length > 63)
        {
            tooltip = tooltip[..63];
        }

        UpdateTooltipText(tooltip);
    }

    private void UpdateTooltipText(string text)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Text = text;
        }
    }

    private static string FormatTimeRemaining(TimeSpan remaining)
    {
        if (remaining.TotalHours >= 1)
        {
            return $"{(int)remaining.TotalHours}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }
        return $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Unsubscribe from events
        _sessionManager.SessionStateChanged -= OnSessionStateChanged;
        _sessionManager.TimerTick -= OnTimerTick;

        // Dispose notify icon
        if (_notifyIcon != null)
        {
            _notifyIcon.Click -= OnNotifyIconClick;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        // Dispose cached icons
        _focusIcon?.Dispose();
        _shortBreakIcon?.Dispose();
        _longBreakIcon?.Dispose();
        _idleIcon?.Dispose();

        _disposed = true;
    }
}
