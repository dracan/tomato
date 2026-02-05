using System.Linq;
using System.Windows;
using Tomato.Services;
using Tomato.ViewModels;
using Tomato.Views;

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
    private IStatisticsReportService? _statisticsReportService;
    private ISlackConfigurationService? _slackConfigService;
    private ISlackService? _slackService;
    private ILuxaforConfigurationService? _luxaforConfigService;
    private ILuxaforService? _luxaforService;
    private IUpdateCheckService? _updateCheckService;
    private ISystemTrayService? _systemTrayService;

    // ViewModels
    private TimerViewModel? _timerViewModel;
    private MainViewModel? _mainViewModel;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Check for setup arguments
        var args = e.Args;
        var isSlackSetup = args.Contains("--setup-slack");
        var isLuxaforSetup = args.Contains("--setup-luxafor");

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

        // Create Slack services
        _slackConfigService = new SlackConfigurationService();
        _slackService = new SlackService(_slackConfigService, _sessionManager);

        // Create Luxafor services
        _luxaforConfigService = new LuxaforConfigurationService();
        _luxaforService = new LuxaforService(_luxaforConfigService, _sessionManager);

        // Handle --setup-slack argument
        if (isSlackSetup)
        {
            var setupDialog = new SlackSetupDialog(_slackService);
            setupDialog.ShowDialog();

            // Exit after setup dialog closes
            Shutdown();
            return;
        }

        // Handle --setup-luxafor argument
        if (isLuxaforSetup)
        {
            var setupDialog = new LuxaforSetupDialog(_luxaforService);
            setupDialog.ShowDialog();

            // Exit after setup dialog closes
            Shutdown();
            return;
        }

        // Create and show main window first (needed for DialogService owner)
        var mainWindow = new MainWindow();

        // Create DialogService with MainWindow as owner
        _dialogService = new DialogService(mainWindow);

        // Create StatisticsReportService
        _statisticsReportService = new StatisticsReportService(_sessionManager, _dateTimeProvider);

        // Create ViewModels
        _timerViewModel = new TimerViewModel(_sessionManager, _dialogService, _statisticsReportService);
        _mainViewModel = new MainViewModel(_timerViewModel);

        // Create and initialize system tray service
        _systemTrayService = new SystemTrayService(_sessionManager);
        _systemTrayService.Initialize(mainWindow, _timerViewModel);

        // Set DataContext and show window
        mainWindow.DataContext = _mainViewModel;
        mainWindow.Show();

        // Check for updates in the background after window is shown
        _updateCheckService = new UpdateCheckService();
        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var result = await _updateCheckService!.CheckForUpdateAsync();
            if (result != null)
            {
                // Dispatch to UI thread to show dialog
                await Dispatcher.InvokeAsync(() =>
                {
                    var dialog = new UpdateAvailableDialog(result);
                    dialog.ShowDialog();
                });
            }
        }
        catch
        {
            // Silent failure - don't interrupt app startup
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Dispose services
        (_timerService as IDisposable)?.Dispose();
        (_notificationService as IDisposable)?.Dispose();
        (_slackService as IDisposable)?.Dispose();
        (_luxaforService as IDisposable)?.Dispose();
        _updateCheckService?.Dispose();
        _systemTrayService?.Dispose();

        base.OnExit(e);
    }
}

