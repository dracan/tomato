using System.IO;

namespace Tomato.Services;

/// <summary>
/// Appends unexpected errors to %LOCALAPPDATA%\Tomato\error.log so failures can be diagnosed.
/// Never throws: logging must not cause a second failure.
/// </summary>
public sealed class ErrorLog
{
    /// <summary>
    /// When the log grows past this size it is started afresh.
    /// </summary>
    public const long MaxLogFileBytes = 1024 * 1024;

    private readonly object _lock = new();
    private readonly string _directory;

    /// <summary>
    /// Gets the full path of the log file.
    /// </summary>
    public string LogFilePath { get; }

    /// <summary>
    /// Creates an ErrorLog using the default data directory.
    /// </summary>
    public ErrorLog()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tomato"))
    {
    }

    /// <summary>
    /// Creates an ErrorLog using a custom directory (for testing).
    /// </summary>
    public ErrorLog(string directory)
    {
        _directory = directory;
        LogFilePath = Path.Combine(directory, "error.log");
    }

    /// <summary>
    /// Appends the exception, with where it happened, to the log.
    /// </summary>
    public void Log(Exception exception, string context)
    {
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(_directory);

                var file = new FileInfo(LogFilePath);
                if (file.Exists && file.Length > MaxLogFileBytes)
                {
                    file.Delete();
                }

                File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
            }
        }
        catch
        {
            // Nowhere left to report a logging failure
        }
    }
}
