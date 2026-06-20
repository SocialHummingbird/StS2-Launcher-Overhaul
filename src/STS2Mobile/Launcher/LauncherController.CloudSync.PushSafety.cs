namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void CloudPushPressed()
    {
        if (!CanArmCloudPush())
            return;

        var pushContext = CloudPushSafetyContext.Create(_model.DataDir);
        RequestCloudSync(ManualCloudSyncRequest.Push(
            pushContext.DataDir,
            pushContext.SelectedBranch
        ));
    }

    private bool CanArmCloudPush()
    {
        var pushContext = CloudPushSafetyContext.Create(_model.DataDir);

        if (!CanPushWithBaselineEvidence(pushContext))
            return false;

        if (!CanPushAfterBranchSwitch(pushContext))
            return false;

        return true;
    }
}
