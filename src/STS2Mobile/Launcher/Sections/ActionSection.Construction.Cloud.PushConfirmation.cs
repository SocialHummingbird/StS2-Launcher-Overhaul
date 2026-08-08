using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private static StyledLabel BuildCloudPushConfirmationLabel(float scale, bool compact)
    {
        var pushConfirmationLabel = new StyledLabel(
            compact
                ? CompactCloudPushWarningText()
                : "Confirming Upload sends Android saves to Steam Cloud and can overwrite remote saves. Confirm the selected game version and play mode before continuing.",
            scale,
            fontSize: compact
                ? CompactCloudPushWarningFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        pushConfirmationLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        pushConfirmationLabel.ClipText = compact;
        pushConfirmationLabel.VerticalAlignment = VerticalAlignment.Center;
        if (compact)
        {
            pushConfirmationLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            pushConfirmationLabel.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactCloudPushWarningHeight, scale)
            );
        }
        pushConfirmationLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        pushConfirmationLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.OrangeHot
        );
        pushConfirmationLabel.Visible = false;
        return pushConfirmationLabel;
    }
}
