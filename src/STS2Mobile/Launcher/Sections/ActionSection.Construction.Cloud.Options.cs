using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private CloudOptionControls BuildCloudOptionControls(
        VBoxContainer cloudGroup,
        float scale,
        bool compact
    )
    {
        var cloudOptionsToggle = new StyledButton(
            "Show Save Settings",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactDetailButtonFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            height: compact
                ? LauncherSectionMetrics.CompactDrawerToggleHeight
                : LauncherSectionMetrics.SecondaryButtonHeight
        );
        LauncherButtonStyles.ApplySupportAction(cloudOptionsToggle, scale);
        SetCompactActionButtonText(cloudOptionsToggle, cloudOptionsToggle.Text);
        cloudOptionsToggle.Visible = compact;
        cloudOptionsToggle.Pressed += ToggleCloudOptions;
        cloudGroup.AddChild(cloudOptionsToggle);

        Container cloudOptionsParent = cloudGroup;
        Container compactCloudOptionsRow = null;
        if (compact)
        {
            compactCloudOptionsRow = BuildCompactCloudOptionsRow(
                cloudGroup,
                scale,
                _compactStackedActionRows
            );
            cloudOptionsParent = compactCloudOptionsRow;
        }

        var localBackupToggle = compact
            ? AddCompactSupportToolButton(cloudOptionsParent, "Save Backup Off", scale, null)
            : AddSecondaryHiddenButton(cloudGroup, "Local Backup: Off", scale, null);
        var cloudSyncToggle = compact
            ? AddCompactSupportToolButton(cloudOptionsParent, "Cloud Sync Off", scale, null)
            : AddSecondaryHiddenButton(cloudGroup, "Game Cloud Sync: Off", scale, null);

        return new CloudOptionControls(
            cloudOptionsToggle,
            compactCloudOptionsRow,
            localBackupToggle,
            cloudSyncToggle
        );
    }
}
