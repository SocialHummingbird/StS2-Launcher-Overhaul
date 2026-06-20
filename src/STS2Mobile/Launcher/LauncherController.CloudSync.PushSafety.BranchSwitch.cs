namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private bool CanPushAfterBranchSwitch(CloudPushSafetyContext pushContext)
    {
        if (!LauncherBranchSwitchSafety.HasMarker(pushContext.DataDir))
            return true;

        LauncherPreferences.SaveLocalBackupEnabled(true);
        _view.SetActionPreferences(LauncherPreferences.ReadActionPreferences());

        if (!LauncherBranchSwitchSafety.HasRequiredEvidence(pushContext.DataDir, pushContext.SelectedBranch))
        {
            const string reason = "Manual Push blocked: branch-switch safety marker is incomplete, unreadable, or does not match the selected game version.";
            pushContext.WriteBlockedMarker(reason);
            _view.SetStatus("Push blocked: branch switch marker is missing required safety evidence.");
            _view.AppendLog("Push blocked after branch switch: branch-switch safety marker is incomplete, unreadable, or does not match the selected game version; switch versions again or rebuild validation evidence before pushing.");
            return false;
        }

        if (!LauncherCloudSyncEvidence.HasManualPullAfterBranchSwitch(pushContext.DataDir, pushContext.SelectedBranch))
        {
            const string reason = "Manual Push blocked: no current Pull-after-switch evidence exists for the selected game version.";
            pushContext.WriteBlockedMarker(reason);
            _view.SetStatus("Push blocked: branch switch detected. Pull from Cloud must complete after this game-version switch before Push.");
            _view.AppendLog("Push blocked after branch switch: no current manual Pull evidence marker exists for the selected game version.");
            return false;
        }

        if (!LauncherLocalSaveEvidence.HasImportantSaveEvidence(pushContext.DataDir))
        {
            const string reason = "Manual Push blocked: no Android local save evidence exists after branch switch.";
            pushContext.WriteBlockedMarker(reason);
            _view.SetStatus("Push blocked: branch switch detected but no Android local save files were found.");
            _view.AppendLog("Push blocked after branch switch: Pull from Cloud first, launch or inspect the game until Android local saves exist, then retry Push.");
            return false;
        }

        if (
            !LauncherSaveOriginEvidence.CurrentLocalSavesMatchSelectedRuntime(
                pushContext.DataDir,
                pushContext.SelectedBranch
            )
        )
        {
            const string reason = "Manual Push blocked: save-origin evidence is missing or belongs to a different selected runtime after branch switch.";
            pushContext.WriteBlockedMarker(reason);
            _view.SetStatus("Push blocked: branch switch detected but Android local save origin is not verified for the selected runtime.");
            _view.AppendLog("Push blocked after branch switch: Pull from Cloud for the selected version must complete against this exact PCK/runtime assembly before Push can upload Android local saves.");
            return false;
        }

        if (STS2Mobile.AppPaths.HasStoragePermission())
        {
            STS2Mobile.AppPaths.EnsureExternalDirectories();
            return true;
        }

        STS2Mobile.AppPaths.RequestStoragePermission();
        pushContext.WriteBlockedMarker(
            "Manual Push blocked: backup storage permission is unavailable after branch switch."
        );
        _view.SetStatus("Push blocked: branch switch detected. Grant local backup storage permission before pushing to Steam Cloud.");
        _view.AppendLog("Push blocked after branch switch: local-pre-push backup evidence cannot be written until storage permission is granted.");
        return false;
    }
}
