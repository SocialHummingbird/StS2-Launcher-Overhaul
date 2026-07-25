using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private static StyledLabel BuildCloudPushEligibilityLabel(
        float scale,
        bool compact
    )
    {
        var label = new StyledLabel(
            "Upload availability is being checked.",
            scale,
            fontSize: compact
                ? CompactCloudSafetyDetailFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.ClipText = false;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        label.Visible = false;
        return label;
    }
}
