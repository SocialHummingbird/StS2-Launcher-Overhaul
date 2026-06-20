namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
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
                "Pull Steam Cloud saves to Android local storage?\nThis overwrites Android local saves with the current Steam Cloud state.",
                "Pull from Cloud",
                "Cancel Pull",
                "Pull",
                "Pulling Steam Cloud saves to Android local storage...",
                "Pull complete. Android local saves now reflect Steam Cloud.",
                true,
                LauncherCloudSaveState.ManualPullAllAsync,
                () => LauncherCloudSyncEvidence.WriteManualPullMarker(dataDir, selectedBranch)
            );
    }
}
