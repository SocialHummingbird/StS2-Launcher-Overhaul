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
            => new(
                PushConfirmationMessage(dataDir, selectedBranch),
                "Push to Cloud",
                "Cancel Push",
                "Push",
                "Pushing Android local saves to Steam Cloud...",
                false,
                cancellationToken =>
                    LauncherCloudSaveState.ManualPushAllAsync(
                        progress,
                        cancellationToken
                    ),
                prepareOperation: () =>
                    EnsureCloudPushStillEligible(
                        dataDir,
                        selectedBranch
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

        internal static ManualCloudSyncRequest Pull(
            string dataDir,
            string selectedBranch,
            CloudOperationProgressTracker progress
        )
            => new(
                "Pull Steam Cloud saves to Android?\nCloud modded saves are preferred. Where Steam Cloud has only vanilla saves, the launcher copies the freshly downloaded files into the matching modded profile. Existing local modded files are backed up inside the app before replacement.",
                "Pull from Cloud",
                "Cancel Pull",
                "Pull",
                "Pulling Steam Cloud saves and preparing Android modded profiles...",
                true,
                cancellationToken =>
                    LauncherCloudSaveState.ManualPullAllAsync(
                        progress,
                        cancellationToken
                    ),
                prepareOperation: () =>
                    LauncherCloudSyncEvidence.BeginManualPull(
                        dataDir,
                        selectedBranch
                    ),
                completionEvidenceRequired: true,
                recordCompletionEvidence: () =>
                    LauncherCloudSyncEvidence.WriteManualPullMarker(
                        dataDir,
                        selectedBranch
                    ),
                recordIncompleteResult: result =>
                    LauncherCloudSyncEvidence.WriteManualPullIncompleteMarker(
                        dataDir,
                        selectedBranch,
                        result.Completion
                            == ManualCloudSyncCompletion.PartialSuccess
                            ? "partial-success"
                            : "failure",
                        $"completed={result.CompletedPathCount}; "
                            + $"skipped={result.SkippedPathCount}; "
                            + $"failed={result.FailedPathCount}; "
                            + $"timedOut={result.TimedOutPathCount}; "
                            + $"unfinished={result.UnprocessedPathCount}; "
                            + $"postErrors={result.PostProcessingErrorCount}"
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
