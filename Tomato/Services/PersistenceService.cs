using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tomato.Models;

namespace Tomato.Services;

/// <summary>
/// Handles persistence of application state to JSON file.
/// Default location: %LOCALAPPDATA%\Tomato\state.json
/// </summary>
public sealed class PersistenceService : IPersistenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _dataDirectory;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private long _latestSaveVersion;
    private long _writtenSaveVersion;
    private bool _savingDisabled;

    /// <inheritdoc />
    public string StateFilePath { get; }

    /// <summary>
    /// Creates a PersistenceService using the default data directory.
    /// </summary>
    public PersistenceService()
        : this(GetDefaultDataDirectory())
    {
    }

    /// <summary>
    /// Creates a PersistenceService using a custom data directory (for testing).
    /// </summary>
    public PersistenceService(string dataDirectory)
    {
        _dataDirectory = dataDirectory;
        StateFilePath = Path.Combine(_dataDirectory, "state.json");
    }

    /// <inheritdoc />
    /// <remarks>
    /// Saves are serialized, and a save that was overtaken by a newer one is skipped.
    /// The file is written to a temporary file first and then moved over the old one,
    /// so a crash mid-save never leaves a truncated state file.
    /// </remarks>
    public async Task SaveAsync(AppState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOptions);
        var version = Interlocked.Increment(ref _latestSaveVersion);

        await _saveLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_savingDisabled || version < _writtenSaveVersion)
                return;

            EnsureDirectoryExists();

            var tempPath = StateFilePath + ".tmp";
            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(json).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, StateFilePath, overwrite: true);
            _writtenSaveVersion = version;
        }
        finally
        {
            _saveLock.Release();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// A corrupt file is renamed to state.corrupt-{timestamp}.json so its history can be
    /// recovered by hand. If the file exists but cannot be read, saving is disabled for
    /// this run so the unread history is not overwritten.
    /// </remarks>
    public async Task<AppState?> LoadAsync()
    {
        if (!File.Exists(StateFilePath))
        {
            return null;
        }

        string json;
        try
        {
            json = await File.ReadAllTextAsync(StateFilePath).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _savingDisabled = true;
            return null;
        }

        try
        {
            var state = JsonSerializer.Deserialize<AppState>(json, JsonOptions);
            if (state != null)
                return state;
        }
        catch (JsonException)
        {
        }

        MoveCorruptFileAside();
        return null;
    }

    private void MoveCorruptFileAside()
    {
        var backupPath = Path.Combine(_dataDirectory, $"state.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        try
        {
            File.Move(StateFilePath, backupPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _savingDisabled = true;
        }
    }

    /// <inheritdoc />
    public Task ClearAsync()
    {
        if (File.Exists(StateFilePath))
        {
            File.Delete(StateFilePath);
        }
        return Task.CompletedTask;
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
    }

    private static string GetDefaultDataDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Tomato");
    }
}
