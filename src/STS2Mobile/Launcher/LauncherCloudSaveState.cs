namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSaveState
{
    private static bool _cloudSyncEnabled = true;
    private static SavedSteamCredentials? _savedCredentials;

    internal static string StatusSummary
        => $"HasToken={HasSavedCredentials}, CloudSync={_cloudSyncEnabled}";

    private static bool HasSavedCredentials
        => _savedCredentials.HasValue;

    internal static void SetCloudSyncEnabled(bool enabled)
    {
        _cloudSyncEnabled = enabled;
    }

    internal static void DisableCloudSyncForLaunch()
    {
        _cloudSyncEnabled = false;
    }
}
