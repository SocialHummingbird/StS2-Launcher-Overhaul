using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    private readonly partial struct ManualCloudSyncRequest
    {
        internal static ManualCloudSyncRequest Push(
            string dataDir,
            string selectedBranch,
            CloudOperationProgressTracker progress
        )
        {
            var saveNamespace = LauncherModSelectionState.IsModdedMode
                ? SaveNamespace.Modded
                : SaveNamespace.Vanilla;
            var modSetFingerprint =
                LauncherModSelectionState.EnabledModSetFingerprint() ?? "";
            var runtimeIdentity = SteamGameBranch.StorageIdentity(selectedBranch);
            return new ManualCloudSyncRequest(
                PushConfirmationMessage(dataDir, selectedBranch),
                "Push to Cloud",
                "Cancel Push",
                "Push",
                "Pushing Android local saves to Steam Cloud...",
                false,
                cancellationToken =>
                    LauncherCloudSaveState.ManualPushAllAsync(
                        saveNamespace,
                        runtimeIdentity,
                        modSetFingerprint,
                        progress,
                        cancellationToken
                    ),
                prepareOperation: () =>
                    EnsureCloudPushStillEligible(
                        dataDir,
                        selectedBranch,
                        saveNamespace,
                        modSetFingerprint
                    ),
                onSuccessfulCompletion: () =>
                    LauncherCloudSyncEvidence.WriteManualPushMarker(
                        dataDir,
                        selectedBranch
                    ),
                onFailed: ex =>
                    LauncherCloudSyncEvidence.WriteManualPushBlockedMarker(
                        dataDir,
                        selectedBranch,
                        ex
                    ),
                operationProgress: progress
            );
        }

        internal static ManualCloudSyncRequest Pull(
            string dataDir,
            string selectedBranch,
            CloudOperationProgressTracker progress
        )
        {
            var saveNamespace = LauncherModSelectionState.IsModdedMode
                ? SaveNamespace.Modded
                : SaveNamespace.Vanilla;
            var modSetFingerprint =
                LauncherModSelectionState.EnabledModSetFingerprint() ?? "";
            var runtimeIdentity = SteamGameBranch.StorageIdentity(selectedBranch);
            return new ManualCloudSyncRequest(
                $"Pull {saveNamespace.ToString().ToLowerInvariant()} Steam Cloud saves for {SteamGameBranch.DisplayName(selectedBranch)} to Android?\nMatching Android local save files may be replaced after app-private backups are verified.",
                "Pull from Cloud",
                "Cancel Pull",
                "Pull",
                "Pulling Steam Cloud saves to Android local storage...",
                true,
                cancellationToken =>
                    LauncherCloudSaveState.ManualPullAllAsync(
                        saveNamespace,
                        runtimeIdentity,
                        modSetFingerprint,
                        progress,
                        cancellationToken
                    ),
                prepareOperation: () =>
                    EnsureSaveContextStillSelected(
                        selectedBranch,
                        saveNamespace,
                        modSetFingerprint
                    ),
                onSuccessfulCompletion: () =>
                    LauncherCloudSyncEvidence.WriteManualPullMarker(
                        dataDir,
                        selectedBranch
                    ),
                recordTerminalFailure: (outcome, detail) =>
                    LauncherCloudSyncEvidence.WriteManualPullIncompleteMarker(
                        dataDir,
                        selectedBranch,
                        outcome,
                        detail
                    ),
                operationProgress: progress
            );
        }
    }
}
