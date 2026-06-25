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

        if (!CanPushWithWorkshopModSafety(pushContext))
            return false;

        if (!CanPushWithBaselineEvidence(pushContext))
            return false;

        if (!CanPushAfterBranchSwitch(pushContext))
            return false;

        return true;
    }

    private bool CanPushWithWorkshopModSafety(CloudPushSafetyContext pushContext)
    {
        var selectedMods = LauncherWorkshopModSafety.ActiveSelectedModCount();
        if (selectedMods <= 0)
            return true;

        var reason = $"Manual Push blocked: {selectedMods} selected mod(s) are active; modded-save Steam Cloud upload is not supported.";
        pushContext.WriteBlockedMarker(reason);
        _view.SetStatus("Push blocked: selected mods are active. Steam Cloud upload stays locked for modded saves.");
        _view.AppendLog("Push blocked: selected Android mods are enabled for launch. Pull/download/sync remain available, but Push to Steam Cloud is blocked to protect unmodded cloud saves.");
        return false;
    }
}
