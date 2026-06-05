using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherCloudSaveState
{
    private readonly struct SavedSteamCredentials
    {
        private SavedSteamCredentials(string accountName, string refreshToken)
        {
            AccountName = accountName;
            RefreshToken = refreshToken;
        }

        private string AccountName { get; }
        private string RefreshToken { get; }

        internal static SavedSteamCredentials FromLogin(
            string accountName,
            string refreshToken
        )
            => new(accountName, refreshToken);

        internal bool TryCreateSaveManager(out SaveManager saveManager)
        {
            saveManager = null;

            try
            {
                saveManager = new SaveManager(
                    CloudSaveStoreFactory.CreateCloudSaveStore(
                        AccountName,
                        RefreshToken
                    )
                );
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

        internal Task RunManualSyncAsync(Func<string, string, Task> sync)
            => sync(AccountName, RefreshToken);
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

        if (!TryGetSavedCredentialsForCloudSync(out var credentials))
            return false;

        return credentials.TryCreateSaveManager(out saveManager);
    }

    private static bool TryGetSavedCredentialsForCloudSync(
        out SavedSteamCredentials credentials
    )
    {
        credentials = default;

        if (!_cloudSyncEnabled)
        {
            PatchHelper.Log("[Cloud] Cloud sync disabled by user - using local-only SaveManager");
            return false;
        }

        var savedCredentials = _savedCredentials;
        if (!savedCredentials.HasValue)
        {
            PatchHelper.Log("[Cloud] No saved credentials - using local-only SaveManager");
            return false;
        }

        credentials = savedCredentials.Value;
        return true;
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

        _savedCredentials = SavedSteamCredentials.FromLogin(accountName, refreshToken);
    }

    private static SavedSteamCredentials RequireSavedCredentials()
    {
        var credentials = _savedCredentials;
        if (!credentials.HasValue)
            throw new InvalidOperationException("No saved Steam credentials");

        return credentials.Value;
    }

    private static Task RunManualSyncAsync(Func<string, string, Task> sync)
        => RequireSavedCredentials().RunManualSyncAsync(sync);

    internal static void SaveCredentials(SteamCredentialStore credentialStore)
    {
        credentialStore.TryUseCredentials(SaveCredentials);
    }
}
