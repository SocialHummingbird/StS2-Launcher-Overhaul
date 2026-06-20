namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    internal void SetPushPullDisabled(bool disabled)
    {
        if (disabled)
        {
            ResetCloudPushArm(_pushPullRow.Visible);
        }

        _pushButton.Disabled = disabled;
        _cloudPushToggle.Disabled = disabled;
        _confirmPushButton.Disabled = disabled;
        _pullButton.Disabled = disabled;
    }

    private void SetCloudControlsVisible(bool visible)
    {
        _cloudGroup.Visible = visible;
        ApplyCloudOptionVisibility(visible);
        _pushPullRow.Visible = visible;
        if (!visible)
        {
            _cloudPushExpanded = false;
            _cloudSafetyExpanded = false;
        }
        ResetCloudPushArm(visible);
        UpdateBranchHelpText();
    }
}
