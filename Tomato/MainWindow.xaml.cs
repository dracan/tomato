using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Tomato.Helpers;
using Tomato.ViewModels;

namespace Tomato;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        KeyDown += MainWindow_KeyDown;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        DwmHelper.SetDarkTitleBar(hwnd, enable: true);
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel)
            return;

        var timerVm = mainViewModel.TimerViewModel;

        switch (e.Key)
        {
            case Key.Space:
                // Space: Start if idle, Pause if running, Resume if paused
                if (timerVm.IsPaused)
                {
                    timerVm.ResumeCommand.Execute(null);
                }
                else if (timerVm.IsRunning)
                {
                    timerVm.PauseCommand.Execute(null);
                }
                else if (!timerVm.IsSessionComplete)
                {
                    timerVm.StartFocusCommand.Execute(null);
                }
                e.Handled = true;
                break;

            case Key.Escape:
                // Escape: Cancel current session
                if (timerVm.IsRunning || timerVm.IsPaused)
                {
                    timerVm.CancelCommand.Execute(null);
                }
                e.Handled = true;
                break;

            case Key.B:
                // B: Start Break (when session complete)
                if (timerVm.IsSessionComplete)
                {
                    timerVm.StartBreakCommand.Execute(null);
                }
                e.Handled = true;
                break;

            case Key.F:
                // F: Start Focus (when idle or session complete)
                if (!timerVm.IsRunning && !timerVm.IsPaused)
                {
                    timerVm.StartFocusCommand.Execute(null);
                }
                e.Handled = true;
                break;
        }
    }
}
