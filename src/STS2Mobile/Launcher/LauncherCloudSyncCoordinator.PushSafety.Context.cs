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
            SelectedVersion = SteamGameBranch.DisplayName(selectedBranch);
        }

        internal string DataDir { get; }
        internal string SelectedBranch { get; }
        internal string SelectedVersion { get; }

        internal static CloudPushSafetyContext Create(string dataDir)
            => new(dataDir, LauncherPreferences.ReadGameBranch());

        internal CloudPushEligibilityState CaptureEligibilityState()
            => CaptureEligibilityState(
                LauncherLocalSaveEvidence.HasImportantSaveEvidence(DataDir)
            );

        internal CloudPushEligibilityState CaptureEligibilityState(
            bool hasImportantLocalSaveEvidence
        )
        {
            var hasBranchSwitchMarker = LauncherBranchSwitchSafety.HasMarker(DataDir);
            return new CloudPushEligibilityState(
                SelectedVersion,
                LauncherWorkshopModSafety.ActiveSelectedModCount(),
                LauncherCloudSyncEvidence.LastManualPullCompletionRecorded(DataDir),
                LauncherCloudSyncEvidence.LastManualPullMatchesSelectedBranch(
                    DataDir,
                    SelectedBranch
                ),
                hasImportantLocalSaveEvidence,
                LauncherSaveOriginEvidence.CurrentLocalSavesMatchSelectedRuntime(
                    DataDir,
                    SelectedBranch
                ),
                hasBranchSwitchMarker,
                !hasBranchSwitchMarker
                    || LauncherBranchSwitchSafety.HasRequiredEvidence(
                        DataDir,
                        SelectedBranch
                    ),
                !hasBranchSwitchMarker
                    || LauncherCloudSyncEvidence.HasManualPullAfterBranchSwitch(
                        DataDir,
                        SelectedBranch
                    ),
                !hasBranchSwitchMarker
                    || LauncherPreferences.ReadLocalBackupEnabled(),
                !hasBranchSwitchMarker || STS2Mobile.AppPaths.HasStoragePermission()
            );
        }
    }
}
