using System;
using System.IO;
namespace STS2Mobile.Steam;

// Persists Steam account credentials encrypted with Android Keystore (AES-256-GCM).
// Reads and writes a single encrypted JSON file via the Java bridge to GodotApp.
internal sealed partial class SteamCredentialStore
{
    private const string CredentialsFileName = "steam_credentials.enc";

    private readonly string _credentialsPath;
    private SteamCredentials _credentials;

    private bool HasCredentials
        => _credentials?.HasUsableTokens() == true;

    internal bool HasUsableCredentials()
        => HasCredentials;

    internal string AccountNameOrEmpty()
        => _credentials?.AccountNameOrEmpty() ?? string.Empty;

    internal bool HasAccount()
        => _credentials?.HasAccount() == true;

    internal bool IsAccount(string accountName)
        => _credentials?.IsAccount(accountName) == true;

    internal bool TryGetAccountName(out string accountName)
    {
        accountName = null;
        return _credentials?.TryGetAccountName(out accountName) == true;
    }

    internal bool TryCreateConnection(out SteamConnection connection)
    {
        connection = null;
        if (!HasCredentials)
            return false;

        connection = _credentials.CreateConnection();
        return true;
    }

    internal bool TryUseCredentials(Action<string, string> useCredentials)
    {
        if (!HasCredentials)
            return false;

        _credentials.Use(useCredentials);
        return true;
    }

    internal string GuardDataOrEmpty()
        => _credentials?.GuardDataOrEmpty() ?? string.Empty;

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
        _credentials = new SteamCredentials(accountName, refreshToken, guardData);

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
}
