using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    internal CloudPostOperationSnapshot CaptureCurrentState()
    {
        var importantSaveCount =
            LauncherLocalSaveEvidence.CountImportantSaveEvidence(
                _model.DataDir
            );
        var context = CloudPushSafetyContext.Create(_model.DataDir);
        var eligibility = EvaluateCloudPushEligibility(context);
        return new CloudPostOperationSnapshot(
            importantSaveCount,
            LauncherBackupEvidence.CurrentMirrorSaveCount(),
            eligibility
        );
    }

    internal void LocalBackupRecoveryCompleted(
        LocalBackupRefreshResult result,
        bool reportNoChanges = true
    )
    {
        if (_disposed)
            return;

        RunOnMainThread(() =>
        {
            var snapshot = CaptureCurrentState();
            _view.ApplyCloudPostOperationSnapshot(snapshot);

            if (
                !reportNoChanges
                && (
                    result.Completion
                        is LocalBackupRefreshCompletion.Skipped
                            or LocalBackupRefreshCompletion.Success
                )
                && result.Errors == 0
            )
                return;

            var presentation =
                LocalBackupRecoveryPresentation.Create(result);
            _view.SetStatus(presentation.StatusText);
            _view.AppendLog(presentation.StatusText);
        });
    }

    private void ApplyPostOperationSnapshot(
        CloudPostOperationSnapshot snapshot
    )
        => _view.ApplyCloudPostOperationSnapshot(snapshot);
}
