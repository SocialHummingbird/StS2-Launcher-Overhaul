namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    private readonly partial struct ManualCloudSyncRequest
    {
        internal static ManualCloudSyncRequest Push(string dataDir, string selectedBranch)
            => new(
                PushConfirmationMessage(dataDir, selectedBranch),
                "Push to Cloud",
                "Cancel Push",
                "Push",
                "Pushing Android local saves to Steam Cloud...",
                "Push complete. Steam Cloud now reflects Android local saves.",
                false,
                LauncherCloudSaveState.ManualPushAllAsync,
                () => LauncherCloudSyncEvidence.WriteManualPushMarker(dataDir, selectedBranch),
                ex => LauncherCloudSyncEvidence.WriteManualPushBlockedMarker(dataDir, selectedBranch, ex)
            );

        internal static ManualCloudSyncRequest Pull(string dataDir, string selectedBranch)
            => new(
                "Pull Steam Cloud saves to Android?\nCloud modded saves are preferred. Where Steam Cloud has only vanilla saves, the launcher copies the freshly downloaded files into the matching modded profile. Existing local modded files are backed up inside the app before replacement.",
                "Pull from Cloud",
                "Cancel Pull",
                "Pull",
                "Pulling Steam Cloud saves and preparing Android modded profiles...",
                "Pull complete. Cloud modded saves were preferred; missing modded profiles were seeded from the fresh vanilla download.",
                true,
                LauncherCloudSaveState.ManualPullAllAsync,
                () => LauncherCloudSyncEvidence.WriteManualPullMarker(dataDir, selectedBranch)
            );
    }
}
