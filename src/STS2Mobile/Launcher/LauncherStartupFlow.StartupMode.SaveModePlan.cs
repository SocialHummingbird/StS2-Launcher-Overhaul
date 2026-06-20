using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private sealed partial class StartupMode
    {
        private readonly struct StartupSaveModePlan
        {
            private StartupSaveModePlan(bool forceLocalSaves, string reasonLog)
            {
                ForceLocalSaves = forceLocalSaves;
                ReasonLog = reasonLog;
            }

            private bool ForceLocalSaves { get; }
            private string ReasonLog { get; }

            internal string SettingsAndSavesStatus
                => ForceLocalSaves
                    ? "Loading settings and saves in local-only safe mode..."
                    : "Loading settings and saves...";

            internal static StartupSaveModePlan Create(
                bool forceLocalSaves,
                string reasonLog
            )
                => new(forceLocalSaves, reasonLog);

            internal void Apply()
            {
                LauncherPreferences.LoadAndApplyCloudSyncEnabled();
                if (!ForceLocalSaves)
                    return;

                LauncherCloudSaveState.DisableCloudSyncForLaunch();
                PatchHelper.Log(ReasonLog);
            }
        }
    }
}
