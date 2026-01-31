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
    public async Task SaveAsync(AppState state)
    {
        EnsureDirectoryExists();

        var json = JsonSerializer.Serialize(state, JsonOptions);
        await File.WriteAllTextAsync(StateFilePath, json);
    }

    /// <inheritdoc />
    public async Task<AppState?> LoadAsync()
    {
        if (!File.Exists(StateFilePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(StateFilePath);
            return JsonSerializer.Deserialize<AppState>(json, JsonOptions);
        }
        catch (JsonException)
        {
            // File is corrupted, return null
            return null;
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
