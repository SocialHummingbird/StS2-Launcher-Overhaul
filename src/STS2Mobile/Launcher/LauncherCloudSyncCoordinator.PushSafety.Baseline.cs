namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    private bool CanPushWithBaselineEvidence(CloudPushSafetyContext pushContext)
    {
        if (
            !LauncherCloudSyncEvidence.LastManualPullCompletionRecorded(pushContext.DataDir)
            || !LauncherCloudSyncEvidence.LastManualPullMatchesSelectedBranch(
                pushContext.DataDir,
                pushContext.SelectedBranch
            )
        )
        {
            const string reason = "Manual Push blocked: Pull from Cloud must complete for the selected game version before Push.";
            pushContext.WriteBlockedMarker(reason);
            _view.SetStatus($"Push blocked: Pull from Cloud must complete for selected game version {pushContext.SelectedVersion} before Push.");
            _view.AppendLog($"Push blocked: no current Pull from Cloud evidence exists for selected game version {pushContext.SelectedVersion}.");
            return false;
        }

        if (!LauncherLocalSaveEvidence.HasImportantSaveEvidence(pushContext.DataDir))
        {
            const string reason = "Manual Push blocked: no Android local save evidence exists before Push.";
            pushContext.WriteBlockedMarker(reason);
            _view.SetStatus($"Push blocked: no Android local save files were found for selected game version {pushContext.SelectedVersion}.");
            _view.AppendLog($"Push blocked: Pull from Cloud first for {pushContext.SelectedVersion}, launch or inspect the game until Android local saves exist, then retry Push.");
            return false;
        }

        if (
            !LauncherSaveOriginEvidence.CurrentLocalSavesMatchSelectedRuntime(
                pushContext.DataDir,
                pushContext.SelectedBranch
            )
        )
        {
            const string reason = "Manual Push blocked: Android local save origin evidence does not match the selected runtime.";
            pushContext.WriteBlockedMarker(reason);
            _view.SetStatus($"Push blocked: Android local save origin is not verified for the selected {pushContext.SelectedVersion} runtime.");
            _view.AppendLog($"Push blocked: Pull from Cloud for {pushContext.SelectedVersion} must complete against this exact PCK/runtime assembly before Push can upload Android local saves.");
            return false;
        }

        return true;
    }
}
