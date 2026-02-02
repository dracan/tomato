using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tomato.Services;

/// <summary>
/// Manages Luxafor user ID configuration with DPAPI-encrypted storage.
/// User ID is stored at %LOCALAPPDATA%\Tomato\luxafor.json
/// </summary>
public sealed class LuxaforConfigurationService : ILuxaforConfigurationService
{
    private readonly string _configFilePath;

    public LuxaforConfigurationService()
        : this(GetDefaultDataDirectory())
    {
    }

    public LuxaforConfigurationService(string dataDirectory)
    {
        _configFilePath = Path.Combine(dataDirectory, "luxafor.json");
    }

    public string? LoadUserId()
    {
        if (!File.Exists(_configFilePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<LuxaforConfig>(json);

            if (config?.EncryptedUserId == null)
            {
                return null;
            }

            var encryptedBytes = Convert.FromBase64String(config.EncryptedUserId);
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

    public async Task SaveUserIdAsync(string userId)
    {
        EnsureDirectoryExists();

        var userIdBytes = Encoding.UTF8.GetBytes(userId);
        var encryptedBytes = ProtectedData.Protect(
            userIdBytes,
            null,
            DataProtectionScope.CurrentUser);

        var config = new LuxaforConfig
        {
            EncryptedUserId = Convert.ToBase64String(encryptedBytes)
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_configFilePath, json);
    }

    public Task ClearUserIdAsync()
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

    private sealed class LuxaforConfig
    {
        public string? EncryptedUserId { get; set; }
    }
}
