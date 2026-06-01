namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private static void ApplyStartupSaveMode(StartupContext startup)
    {
        LauncherPreferences.LoadAndApplyCloudSyncEnabled();
        if (!startup.ForceLocalSaves)
            return;

        LauncherCloudSaveState.DisableCloudSyncForLaunch();
        startup.LogLocalSavesReason();
    }
}
