using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tomato.Services;

/// <summary>
/// Manages Slack token configuration with DPAPI-encrypted storage.
/// Token is stored at %LOCALAPPDATA%\Tomato\slack.json
/// </summary>
public sealed class SlackConfigurationService : ISlackConfigurationService
{
    private readonly string _configFilePath;

    public SlackConfigurationService()
        : this(GetDefaultDataDirectory())
    {
    }

    public SlackConfigurationService(string dataDirectory)
    {
        _configFilePath = Path.Combine(dataDirectory, "slack.json");
    }

    public string? LoadToken()
    {
        if (!File.Exists(_configFilePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<SlackConfig>(json);

            if (config?.EncryptedToken == null)
            {
                return null;
            }

            var encryptedBytes = Convert.FromBase64String(config.EncryptedToken);
            var decryptedBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch
        {
            // File is corrupted or can't be decrypted
            return null;
        }
    }

    public async Task SaveTokenAsync(string token)
    {
        EnsureDirectoryExists();

        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var encryptedBytes = ProtectedData.Protect(
            tokenBytes,
            null,
            DataProtectionScope.CurrentUser);

        var config = new SlackConfig
        {
            EncryptedToken = Convert.ToBase64String(encryptedBytes)
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_configFilePath, json);
    }

    public Task ClearTokenAsync()
    {
        if (File.Exists(_configFilePath))
        {
            File.Delete(_configFilePath);
        }
        return Task.CompletedTask;
    }

    private void EnsureDirectoryExists()
    {
        var directory = Path.GetDirectoryName(_configFilePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string GetDefaultDataDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Tomato");
    }

    private sealed class SlackConfig
    {
        public string? EncryptedToken { get; set; }
    }
}
