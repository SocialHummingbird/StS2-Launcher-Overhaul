using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    private readonly struct CloudPushSafetyContext
    {
        private CloudPushSafetyContext(string dataDir, string selectedBranch)
        {
            DataDir = dataDir;
            SelectedBranch = selectedBranch;
        }

        internal string DataDir { get; }
        internal string SelectedBranch { get; }

        internal static CloudPushSafetyContext Create(string dataDir)
            => new(dataDir, LauncherPreferences.ReadGameBranch());

        internal CloudPushEligibilityState CaptureEligibilityState()
            => new(
                CloudSyncCoordinator.HasTransferableLocalSaveContent(
                    LauncherModSelectionState.IsModdedMode
                        ? SaveNamespace.Modded
                        : SaveNamespace.Vanilla
                ),
                CloudSyncCoordinator.HasIncompletePullMarker(),
                CloudSyncCoordinator.HasSaveRecoverySyncHold()
            );
    }
}
