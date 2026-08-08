using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static StyledLabel BuildCompactWorkflowNumberLabel(int index, float scale)
    {
        var numberLabel = new StyledLabel(
            CompactWorkflowStepNumbers[index],
            scale,
            fontSize: CompactWorkflowStepNumberFontSize,
            align: HorizontalAlignment.Center
        );
        numberLabel.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepNumberMinWidth, scale),
            0
        );
        numberLabel.VerticalAlignment = VerticalAlignment.Center;
        numberLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        numberLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextMuted
        );
        return numberLabel;
    }

    private static StyledLabel BuildCompactWorkflowLabel(int index, float scale)
    {
        var label = new StyledLabel(
            CompactWorkflowStepNames[index],
            scale,
            fontSize: CompactWorkflowStepLabelFontSize,
            align: HorizontalAlignment.Left
        );
        label.VerticalAlignment = VerticalAlignment.Center;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.ClipText = true;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        label.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextMuted
        );
        return label;
    }

    private static StyledLabel BuildCompactWorkflowDetailLabel(int index, float scale)
    {
        var detail = new StyledLabel(
            CompactWorkflowStepDetails[index],
            scale,
            fontSize: CompactWorkflowStepDetailFontSize,
            align: HorizontalAlignment.Center
        );
        detail.VerticalAlignment = VerticalAlignment.Center;
        detail.MouseFilter = Control.MouseFilterEnum.Ignore;
        detail.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        detail.ClipText = true;
        detail.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        detail.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextMuted
        );
        return detail;
    }
}
