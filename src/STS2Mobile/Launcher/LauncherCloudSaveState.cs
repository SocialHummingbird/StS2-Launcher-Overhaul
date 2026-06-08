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

        internal Task<string> RunManualSyncAsync(Func<string, string, Task<string>> sync)
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
            return TryCreateAndroidLocalSaveManager(out saveManager);

        return credentials.TryCreateSaveManager(out saveManager);
    }

    private static bool TryCreateAndroidLocalSaveManager(out SaveManager saveManager)
    {
        saveManager = null;

        if (!OperatingSystem.IsAndroid())
            return false;

        try
        {
            saveManager = new SaveManager(CloudSaveStoreFactory.CreateLocalOnlyCloudSaveStore());
            PatchHelper.Log("[Cloud] Created Android local-only SaveManager");
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Cloud] Android local-only SaveManager failed: {ex.Message}");
            return false;
        }
    }

    private static bool TryGetSavedCredentialsForCloudSync(
        out SavedSteamCredentials credentials
    )
    {
        credentials = default;

        if (!_cloudSyncEnabled)
        {
            PatchHelper.Log("[Cloud] Cloud sync disabled - using Android local-only SaveManager when available");
            return false;
        }

        var savedCredentials = _savedCredentials;
        if (!savedCredentials.HasValue)
        {
            PatchHelper.Log("[Cloud] No saved credentials - using Android local-only SaveManager when available");
            return false;
        }

        credentials = savedCredentials.Value;
        return true;
    }

    internal static Task<string> ManualPushAllAsync()
        => RunManualSyncAsync(CloudSyncCoordinator.ManualPushAllAsync);

    internal static Task<string> ManualPullAllAsync()
        => RunManualSyncAsync(CloudSyncCoordinator.ManualPullAllAsync);

    internal static void SetCloudSyncEnabled(bool enabled)
    {
        _cloudSyncEnabled = enabled;
    }

    internal static void DisableCloudSyncForLaunch()
    {
        _cloudSyncEnabled = false;
    }

    private static bool SaveCredentials(string accountName, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            return false;

        _savedCredentials = SavedSteamCredentials.FromLogin(accountName, refreshToken);
        return true;
    }

    private static SavedSteamCredentials RequireSavedCredentials()
    {
        var credentials = _savedCredentials;
        if (!credentials.HasValue)
            throw new InvalidOperationException("No saved Steam credentials. Log in again before pulling cloud saves.");

        return credentials.Value;
    }

    private static Task<string> RunManualSyncAsync(Func<string, string, Task<string>> sync)
        => RequireSavedCredentials().RunManualSyncAsync(sync);

    internal static bool SaveCredentials(SteamCredentialStore credentialStore)
    {
        var saved = false;
        credentialStore.TryUseCredentials(
            (accountName, refreshToken) =>
            {
                saved = SaveCredentials(accountName, refreshToken);
            }
        );
        if (!saved)
            _savedCredentials = null;

        PatchHelper.Log(saved
            ? "[Cloud] Saved Steam credentials available for cloud sync"
            : "[Cloud] Saved Steam credentials unavailable for cloud sync"
        );
        return saved;
    }

    internal static void ClearCredentials()
    {
        _savedCredentials = null;
    }
}
