namespace STS2Mobile.Launcher;

internal static class LauncherCloudSaveState
{
    private readonly struct Credentials
    {
        internal Credentials(string accountName, string refreshToken)
        {
            AccountName = accountName;
            RefreshToken = refreshToken;
        }

        internal string AccountName { get; }
        internal string RefreshToken { get; }

        internal bool IsAvailable => AccountName != null && RefreshToken != null;
    }

    private static bool _cloudSyncEnabled = true;
    private static Credentials _savedCredentials;

    internal static string StatusSummary
        => $"HasToken={_savedCredentials.IsAvailable}, CloudSync={_cloudSyncEnabled}";

    private static bool TryGetSavedCredentials(out Credentials credentials)
    {
        credentials = _savedCredentials;
        return credentials.IsAvailable;
    }

    private static bool TryGetEnabledCredentials(
        out Credentials credentials,
        out string unavailableReason
    )
    {
        if (!_cloudSyncEnabled)
        {
            credentials = default;
            unavailableReason = "[Cloud] Cloud sync disabled by user - using local-only SaveManager";
            return false;
        }

        if (!TryGetSavedCredentials(out credentials))
        {
            unavailableReason = "[Cloud] No saved credentials - using local-only SaveManager";
            return false;
        }

        unavailableReason = null;
        return true;
    }

    internal static bool TryCreateEnabledSaveManager(
        out MegaCrit.Sts2.Core.Saves.SaveManager saveManager
    )
    {
        saveManager = null;

        if (!TryGetEnabledCredentials(out var credentials, out var unavailableReason))
        {
            PatchHelper.Log(unavailableReason);
            return false;
        }

        try
        {
            saveManager = new MegaCrit.Sts2.Core.Saves.SaveManager(
                STS2Mobile.Steam.CloudSaveStoreFactory.CreateCloudSaveStore(
                    credentials.AccountName,
                    credentials.RefreshToken
                )
            );
            PatchHelper.Log("[Cloud] Created SaveManager with SteamKit2 cloud store");
            return true;
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Cloud store injection failed, falling back to local: {ex.Message}"
            );
            return false;
        }
    }

    internal static System.Threading.Tasks.Task ManualPushAllAsync()
    {
        var credentials = RequireSavedCredentials();
        return STS2Mobile.Steam.CloudSyncCoordinator.ManualPushAllAsync(
            credentials.AccountName,
            credentials.RefreshToken
        );
    }

    internal static System.Threading.Tasks.Task ManualPullAllAsync()
    {
        var credentials = RequireSavedCredentials();
        return STS2Mobile.Steam.CloudSyncCoordinator.ManualPullAllAsync(
            credentials.AccountName,
            credentials.RefreshToken
        );
    }

    internal static void SetCloudSyncEnabled(bool enabled)
    {
        _cloudSyncEnabled = enabled;
    }

    internal static void DisableCloudSyncForLaunch()
    {
        _cloudSyncEnabled = false;
    }

    private static void SaveCredentials(string accountName, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            return;

        _savedCredentials = new Credentials(accountName, refreshToken);
    }

    private static Credentials RequireSavedCredentials()
    {
        if (!TryGetSavedCredentials(out var credentials))
            throw new System.InvalidOperationException("No saved Steam credentials");

        return credentials;
    }

    internal static void SaveCredentials(STS2Mobile.Steam.SteamCredentialStore credentialStore)
    {
        if (!credentialStore.HasCredentials)
            return;

        SaveCredentials(credentialStore.AccountName, credentialStore.RefreshToken);
    }
}

