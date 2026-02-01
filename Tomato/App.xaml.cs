using System.Windows;
using Tomato.Services;
using Tomato.ViewModels;

namespace Tomato;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // Services
    private ITimerService? _timerService;
    private INotificationService? _notificationService;
    private IPersistenceService? _persistenceService;
    private IDateTimeProvider? _dateTimeProvider;
    private ISessionManager? _sessionManager;
    private IDialogService? _dialogService;

    // ViewModels
    private TimerViewModel? _timerViewModel;
    private MainViewModel? _mainViewModel;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Create services
        _dateTimeProvider = new DateTimeProvider();
        _timerService = new TimerService();
        _notificationService = new NotificationService();
        _persistenceService = new PersistenceService();

        // Create session manager with dependencies
        _sessionManager = new SessionManager(
            _timerService,
            _notificationService,
            _persistenceService,
            _dateTimeProvider);

        // Restore state from disk
        await ((SessionManager)_sessionManager).RestoreStateAsync();

        // Create and show main window first (needed for DialogService owner)
        var mainWindow = new MainWindow();

        // Create DialogService with MainWindow as owner
        _dialogService = new DialogService(mainWindow);

        // Create ViewModels
        _timerViewModel = new TimerViewModel(_sessionManager, _dialogService);
        _mainViewModel = new MainViewModel(_timerViewModel);

        // Set DataContext and show window
        mainWindow.DataContext = _mainViewModel;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Dispose services
        (_timerService as IDisposable)?.Dispose();
        (_notificationService as IDisposable)?.Dispose();

        base.OnExit(e);
    }
}

