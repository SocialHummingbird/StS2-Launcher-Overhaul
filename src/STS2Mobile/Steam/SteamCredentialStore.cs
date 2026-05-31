using System;
using System.IO;
namespace STS2Mobile.Steam;

// Persists Steam account credentials encrypted with Android Keystore (AES-256-GCM).
// Reads and writes a single encrypted JSON file via the Java bridge to GodotApp.
internal sealed class SteamCredentialStore
{
    private const string CredentialsFileName = "steam_credentials.enc";
    private const string DecryptionFailedLogMessage = "[Credentials] Decryption failed, deleting stale file";
    private const string EncryptionFailedLogMessage = "[Credentials] Encryption returned null";

    private readonly string _credentialsPath;
    private SteamCredentials _credentials;

    internal string AccountName => _credentials?.AccountName;
    internal string RefreshToken => _credentials?.RefreshToken;
    internal string GuardData => _credentials?.GuardData;

    internal bool HasCredentials =>
        _credentials?.RefreshToken != null && _credentials?.AccountName != null;

    internal SteamCredentialStore(string dataDir)
    {
        _credentialsPath = Path.Combine(dataDir, CredentialsFileName);
    }

    internal void Load()
    {
        try
        {
            _credentials = LoadCredentials();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Credentials] Load failed: {ex.Message}");
            _credentials = null;
        }
    }

    internal void Save(string accountName, string refreshToken, string guardData)
    {
        _credentials = new SteamCredentials
        {
            AccountName = accountName,
            RefreshToken = refreshToken,
            GuardData = guardData,
        };

        try
        {
            SaveCredentials(_credentials);
            PatchHelper.Log("[Credentials] Saved (Android Keystore encrypted)");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Credentials] Save failed: {ex.Message}");
        }
    }

    private SteamCredentials LoadCredentials()
    {
        if (!File.Exists(_credentialsPath))
            return null;

        var credentials = AndroidEncryptedJsonFile.Load<SteamCredentials>(_credentialsPath);
        if (credentials != null)
            return credentials;

        PatchHelper.Log(DecryptionFailedLogMessage);
        DeleteCredentialsFile();
        return null;
    }

    private void SaveCredentials(SteamCredentials credentials)
    {
        if (!AndroidEncryptedJsonFile.Save(_credentialsPath, credentials))
            PatchHelper.Log(EncryptionFailedLogMessage);
    }

    private void DeleteCredentialsFile()
    {
        AndroidEncryptedJsonFile.DeleteQuietly(_credentialsPath);
    }

    private sealed class SteamCredentials
    {
        public string AccountName { get; set; }
        public string RefreshToken { get; set; }
        public string GuardData { get; set; }
    }
}
