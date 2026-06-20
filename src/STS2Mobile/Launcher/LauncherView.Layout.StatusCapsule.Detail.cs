using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static Button BuildCompactStatusDetailButton(float scale)
    {
        var button = new Button
        {
            Text = "",
            ClipText = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = "Show full launcher status",
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailHeight, scale)
            ),
        };
        ApplyCompactStatusDetailButtonStyle(button, scale);
        return button;
    }

    private static HBoxContainer BuildCompactStatusDetailRow(StyledLabel statusLabel, float scale)
    {
        var detailRow = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        detailRow.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        detailRow.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailHorizontalMargin, scale);
        detailRow.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailHorizontalMargin, scale);
        detailRow.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailVerticalMargin, scale);
        detailRow.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailVerticalMargin, scale);
        detailRow.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailRowGap, scale)
        );

        statusLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        statusLabel.HorizontalAlignment = HorizontalAlignment.Left;
        statusLabel.VerticalAlignment = VerticalAlignment.Center;
        statusLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        statusLabel.AutowrapMode = TextServer.AutowrapMode.Off;
        statusLabel.ClipText = true;
        statusLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        detailRow.AddChild(statusLabel);
        return detailRow;
    }

    private static StyledLabel BuildCompactStatusDetailCue(float scale)
    {
        var detailCue = new StyledLabel(
            "Details",
            scale,
            fontSize: CompactStatusDetailCueFontSize,
            align: HorizontalAlignment.Center
        );
        detailCue.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailCueWidth, scale),
            0
        );
        detailCue.VerticalAlignment = VerticalAlignment.Center;
        detailCue.MouseFilter = Control.MouseFilterEnum.Ignore;
        detailCue.ClipText = true;
        detailCue.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        detailCue.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.CyanAccent
        );
        return detailCue;
    }
}
