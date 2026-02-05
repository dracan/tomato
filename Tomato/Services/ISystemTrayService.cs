using System.Windows;
using Tomato.ViewModels;

namespace Tomato.Services;

/// <summary>
/// Provides system tray icon functionality for the application.
/// </summary>
public interface ISystemTrayService : IDisposable
{
    /// <summary>
    /// Initializes the system tray icon with the main window and timer view model.
    /// </summary>
    /// <param name="mainWindow">The main application window.</param>
    /// <param name="timerViewModel">The timer view model for executing commands.</param>
    void Initialize(Window mainWindow, TimerViewModel timerViewModel);
}
