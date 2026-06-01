using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private static void ApplyStartupSaveMode(StartupContext startup)
    {
        LauncherPreferences.LoadAndApplyCloudSyncEnabled();
        if (!startup.Mode.ForceLocalSaves)
            return;

        LauncherCloudSaveState.DisableCloudSyncForLaunch();
        PatchHelper.Log(startup.Mode.LocalSavesReasonLog);
    }
}
