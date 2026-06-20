using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private CloudSafetyControls BuildCloudSafetyControls(
        VBoxContainer cloudGroup,
        float scale,
        bool compact
    )
    {
        var cloudSafetyLabel = new StyledLabel(
            "",
            scale,
            fontSize: compact
                ? CompactCloudSafetyDetailFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        cloudSafetyLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        cloudSafetyLabel.ClipText = compact;
        cloudSafetyLabel.VerticalAlignment = VerticalAlignment.Center;
        if (compact)
        {
            cloudSafetyLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            cloudSafetyLabel.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactCloudSafetyDetailHeight, scale)
            );
        }
        cloudSafetyLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        cloudSafetyLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.OrangeHot
        );
        cloudGroup.AddChild(cloudSafetyLabel);

        var cloudSafetyToggle = new StyledButton(
            CompactCloudSafetySummary(),
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactDetailButtonFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            height: compact
                ? LauncherSectionMetrics.CompactDrawerToggleHeight
                : LauncherSectionMetrics.SecondaryButtonHeight
        );
        LauncherButtonStyles.ApplySupportAction(cloudSafetyToggle, scale);
        SetCompactActionButtonText(cloudSafetyToggle, cloudSafetyToggle.Text);
        cloudSafetyToggle.Visible = compact;
        cloudSafetyToggle.Pressed += ToggleCloudSafety;
        cloudGroup.AddChild(cloudSafetyToggle);

        return new CloudSafetyControls(cloudSafetyLabel, cloudSafetyToggle);
    }
}
