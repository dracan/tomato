using NAudio.Wave;
using Tomato.Models;

namespace Tomato.Services;

/// <summary>
/// Provides audio notifications using NAudio and Windows toast notifications.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private IWavePlayer? _wavePlayer;
    private AudioFileReader? _audioReader;
    private readonly string _soundsDirectory;
    private bool _disposed;

    /// <inheritdoc />
    public bool IsSoundEnabled { get; set; } = true;

    public NotificationService()
    {
        // Sounds are embedded in Resources/Sounds/
        _soundsDirectory = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Resources",
            "Sounds");
    }

    /// <summary>
    /// Creates a NotificationService with a custom sounds directory (for testing).
    /// </summary>
    public NotificationService(string soundsDirectory)
    {
        _soundsDirectory = soundsDirectory;
    }

    /// <inheritdoc />
    public async Task PlayCompletionSoundAsync(SessionType sessionType)
    {
        if (!IsSoundEnabled)
            return;

        try
        {
            // Clean up previous playback
            DisposeAudioResources();

            var soundFile = GetSoundFile(sessionType);
            if (!System.IO.File.Exists(soundFile))
            {
                // Fall back to system beep if sound file doesn't exist
                System.Media.SystemSounds.Asterisk.Play();
                return;
            }

            _audioReader = new AudioFileReader(soundFile);
            _wavePlayer = new WaveOutEvent();
            _wavePlayer.Init(_audioReader);
            _wavePlayer.Play();

            // Wait for playback to complete
            while (_wavePlayer?.PlaybackState == PlaybackState.Playing)
            {
                await Task.Delay(100);
            }
        }
        catch
        {
            // Fall back to system sound on any error
            System.Media.SystemSounds.Asterisk.Play();
        }
    }

    /// <inheritdoc />
    public void ShowNotification(string title, string message)
    {
        // Use Windows toast notification via WPF
        // For now, we'll use system tray balloon or message box as fallback
        // Full toast notification would require Windows.UI.Notifications
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null)
            {
                // Flash the window to get attention
                FlashWindow(mainWindow);
            }
        });
    }

    private string GetSoundFile(SessionType sessionType)
    {
        var fileName = sessionType switch
        {
            SessionType.Focus => "focus-complete.wav",
            SessionType.ShortBreak => "break-complete.wav",
            SessionType.LongBreak => "break-complete.wav",
            _ => "notification.wav"
        };

        var specificFile = System.IO.Path.Combine(_soundsDirectory, fileName);
        if (System.IO.File.Exists(specificFile))
            return specificFile;

        // Fall back to generic notification sound
        return System.IO.Path.Combine(_soundsDirectory, "notification.wav");
    }

    private static void FlashWindow(System.Windows.Window window)
    {
        // Simple attention mechanism - activate the window
        if (window.WindowState == System.Windows.WindowState.Minimized)
        {
            window.WindowState = System.Windows.WindowState.Normal;
        }
        window.Activate();
        window.Topmost = true;
        window.Topmost = false;
    }

    private void DisposeAudioResources()
    {
        _wavePlayer?.Stop();
        _wavePlayer?.Dispose();
        _wavePlayer = null;

        _audioReader?.Dispose();
        _audioReader = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DisposeAudioResources();
    }
}
