using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherCloudSaveState
{
    private readonly struct SavedSteamCredentials
    {
        internal SavedSteamCredentials(string accountName, string refreshToken)
        {
            AccountName = accountName;
            RefreshToken = refreshToken;
        }

        internal string AccountName { get; }
        internal string RefreshToken { get; }
    }

    private static bool _cloudSyncEnabled = true;
    private static SavedSteamCredentials? _savedCredentials;

    internal static string StatusSummary
        => $"HasToken={HasSavedCredentials}, CloudSync={_cloudSyncEnabled}";

    private static bool HasSavedCredentials
        => _savedCredentials.HasValue;

    internal static bool TryCreateEnabledSaveManager(
        out SaveManager saveManager
    )
    {
        saveManager = null;

        var credentials = GetSavedCredentialsForCloudSync();
        if (!credentials.HasValue)
            return false;

        try
        {
            saveManager = CreateSaveManager(credentials.Value);
            PatchHelper.Log("[Cloud] Created SaveManager with SteamKit2 cloud store");
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Cloud store injection failed, falling back to local: {ex.Message}"
            );
            return false;
        }
    }

    private static SavedSteamCredentials? GetSavedCredentialsForCloudSync()
    {
        if (!_cloudSyncEnabled)
        {
            PatchHelper.Log("[Cloud] Cloud sync disabled by user - using local-only SaveManager");
            return null;
        }

        var credentials = _savedCredentials;
        if (!credentials.HasValue)
        {
            PatchHelper.Log("[Cloud] No saved credentials - using local-only SaveManager");
            return null;
        }

        return credentials;
    }

    internal static Task ManualPushAllAsync()
        => RunManualSyncAsync(CloudSyncCoordinator.ManualPushAllAsync);

    internal static Task ManualPullAllAsync()
        => RunManualSyncAsync(CloudSyncCoordinator.ManualPullAllAsync);

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

        _savedCredentials = new SavedSteamCredentials(accountName, refreshToken);
    }

    private static SavedSteamCredentials RequireSavedCredentials()
    {
        var credentials = _savedCredentials;
        if (!credentials.HasValue)
            throw new InvalidOperationException("No saved Steam credentials");

        return credentials.Value;
    }

    private static Task RunManualSyncAsync(Func<string, string, Task> sync)
    {
        var credentials = RequireSavedCredentials();
        return sync(credentials.AccountName, credentials.RefreshToken);
    }

    private static SaveManager CreateSaveManager(
        SavedSteamCredentials credentials
    )
        => new(
            CloudSaveStoreFactory.CreateCloudSaveStore(
                credentials.AccountName,
                credentials.RefreshToken
            )
        );

    internal static void SaveCredentials(SteamCredentialStore credentialStore)
    {
        credentialStore.TryUseCredentials(SaveCredentials);
    }
}
