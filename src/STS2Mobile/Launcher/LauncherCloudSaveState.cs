using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSaveState
{
    private static bool _cloudSyncEnabled = true;
    private static SavedSteamCredentials? _savedCredentials;

    internal static string StatusSummary
        => $"HasToken={HasSavedCredentials}, CloudSync={_cloudSyncEnabled}";

    internal static bool CloudSyncEnabled
        => _cloudSyncEnabled;

    internal static bool HasSavedCredentials
        => _savedCredentials.HasValue;

    internal static bool HasAutomaticSyncPending()
        => CloudSaveStoreFactory.CreateLocalStore().FileExists(
            CloudSyncCoordinator.AutomaticSyncPendingPath
        );

    internal static void SetCloudSyncEnabled(bool enabled)
    {
        _cloudSyncEnabled = enabled;
    }

    internal static void DisableCloudSyncForLaunch()
    {
        _cloudSyncEnabled = false;
    }
}
