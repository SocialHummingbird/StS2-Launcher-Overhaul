using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class DownloadSection
{
    private Button BuildCompactSelectedVersionPanel(float scale, bool compact)
    {
        var button = new Button
        {
            Text = "",
            ClipText = true,
            Visible = compact,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            TooltipText = "Change game version for local files",
            CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(
                    _compactStackedActionRows
                        ? LauncherSectionMetrics.CompactStackedVersionSummaryHeight
                        : LauncherSectionMetrics.CompactVersionSummaryHeight,
                    scale
                )
            ),
        };
        ApplySelectedVersionSummaryButtonStyle(button, scale, compact);
        button.Pressed += OpenCompactBranchDetailsFromSelectedVersion;
        return button;
    }

    private Label BuildCompactSelectedVersionLabel(float scale, bool compact)
    {
        var label = new StyledLabel(
            "",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactVersionSummaryFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        )
        {
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.AutowrapMode = _compactStackedActionRows
            ? TextServer.AutowrapMode.WordSmart
            : compact
                ? TextServer.AutowrapMode.Off
                : TextServer.AutowrapMode.WordSmart;
        label.ClipText = compact && !_compactStackedActionRows;
        if (compact && !_compactStackedActionRows)
            label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        if (compact)
        {
            label.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(
                    _compactStackedActionRows
                        ? LauncherSectionMetrics.CompactStackedVersionSummaryHeight
                        : LauncherSectionMetrics.CompactVersionSummaryHeight,
                    scale
                )
            );
        }
        label.MouseFilter = MouseFilterEnum.Ignore;
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        label.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin,
            scale
        );
        label.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin,
            scale
        );
        label.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryVerticalMargin,
            scale
        );
        label.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryVerticalMargin,
            scale
        );
        label.Visible = compact;
        label.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        return label;
    }
}
