using System.IO;

namespace STS2Mobile.Steam;

internal sealed partial class SteamCredentialStore
{
    private const string DecryptionFailedLogMessage = "[Credentials] Decryption failed, deleting stale file";
    private const string EncryptionFailedLogMessage = "[Credentials] Encryption returned null";

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

    private bool SaveCredentials(SteamCredentials credentials)
    {
        if (!AndroidEncryptedJsonFile.Save(_credentialsPath, credentials))
        {
            PatchHelper.Log(EncryptionFailedLogMessage);
            return false;
        }

        return true;
    }

    private void DeleteCredentialsFile()
    {
        AndroidEncryptedJsonFile.DeleteQuietly(_credentialsPath);
    }
}
