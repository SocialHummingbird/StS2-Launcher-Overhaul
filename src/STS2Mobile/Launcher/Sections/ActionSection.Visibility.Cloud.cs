namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    internal void SetPushPullDisabled(bool disabled)
    {
        _pushPullDisabled = disabled;
        ApplyCloudContextControlsDisabled();
        _cancelCloudOperationButton.Visible =
            disabled && _pushPullRow.Visible;
        _cancelCloudOperationButton.Disabled = false;
        SetCompactActionButtonText(
            _cancelCloudOperationButton,
            "Cancel Cloud Operation"
        );
        if (disabled)
        {
            ResetCloudPushArm(_pushPullRow.Visible);
        }

        ApplyCloudPushDisabledState();
    }

    private void ApplyCloudContextControlsDisabled()
    {
        var disabled = _pushPullDisabled || ContextControlsDisabled;
        _cloudOptionsToggle.Disabled = disabled;
        _localBackupToggle.Disabled = disabled;
        _cloudSyncToggle.Disabled = disabled;
        _branchDropdown.Disabled = disabled;
        _branchDetailsToggle.Disabled = disabled;
    }

    private void SetCloudControlsVisible(bool visible)
    {
        _cloudGroup.Visible = visible;
        ApplyCloudOptionVisibility(visible);
        _pushPullRow.Visible = visible;
        _cancelCloudOperationButton.Visible =
            visible && _pushPullDisabled;
        if (!visible)
        {
            _cloudPushExpanded = false;
            _cloudSafetyExpanded = false;
        }
        else
        {
            RefreshCloudPushEligibility();
        }
        ResetCloudPushArm(visible);
        UpdateBranchHelpText();
    }
}
