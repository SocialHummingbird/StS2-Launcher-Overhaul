using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static StyledLabel BuildCompactSafeFlowStepTitle(float scale, string title)
    {
        var titleLabel = new StyledLabel(
            title,
            scale,
            fontSize: CompactSafeFlowGuideStepTitleFontSize,
            align: HorizontalAlignment.Left
        )
        {
            ClipText = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        titleLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        return titleLabel;
    }

    private static StyledLabel BuildCompactSafeFlowStepDetail(float scale, string detail)
    {
        var detailLabel = new StyledLabel(
            detail,
            scale,
            fontSize: CompactSafeFlowGuideStepDetailFontSize,
            align: HorizontalAlignment.Left
        )
        {
            ClipText = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            TooltipText = detail,
            VerticalAlignment = VerticalAlignment.Center,
        };
        detailLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        return detailLabel;
    }
}
