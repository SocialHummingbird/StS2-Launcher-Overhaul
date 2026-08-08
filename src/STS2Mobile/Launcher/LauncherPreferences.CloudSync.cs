namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    internal static bool LoadAndApplyCloudSyncEnabled()
        => CloudSyncPreference.LoadAndApply();

    internal static void SaveCloudSyncEnabled(bool enabled)
        => CloudSyncPreference.Save(enabled);

    private static void ApplyCloudSync(bool enabled)
    {
        LauncherCloudSaveState.SetCloudSyncEnabled(enabled);
    }
}
