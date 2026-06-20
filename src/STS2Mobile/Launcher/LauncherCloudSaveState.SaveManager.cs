using System;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSaveState
{
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
}
