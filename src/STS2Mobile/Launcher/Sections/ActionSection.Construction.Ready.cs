using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private (Button Panel, Label Label) BuildReadyVersionSummaryControls(float scale, bool compact)
    {
        var readyVersionSummaryPanel = new Button
        {
            Text = "",
            ClipText = true,
            Visible = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            TooltipText = "Open save safety check",
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
        ApplyReadyVersionSummaryButtonStyle(readyVersionSummaryPanel, scale, compact);
        readyVersionSummaryPanel.Pressed += OpenCompactCloudSafetyFromReadySummary;
        AddChild(readyVersionSummaryPanel);

        var readyVersionSummaryLabel = new StyledLabel(
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
        readyVersionSummaryLabel.AutowrapMode = _compactStackedActionRows
            ? TextServer.AutowrapMode.WordSmart
            : compact
            ? TextServer.AutowrapMode.Off
            : TextServer.AutowrapMode.WordSmart;
        readyVersionSummaryLabel.ClipText = compact && !_compactStackedActionRows;
        if (compact && !_compactStackedActionRows)
            readyVersionSummaryLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        if (compact)
        {
            readyVersionSummaryLabel.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(
                    _compactStackedActionRows
                        ? LauncherSectionMetrics.CompactStackedVersionSummaryHeight
                        : LauncherSectionMetrics.CompactVersionSummaryHeight,
                    scale
                )
            );
        }
        readyVersionSummaryLabel.MouseFilter = MouseFilterEnum.Ignore;
        readyVersionSummaryLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        readyVersionSummaryLabel.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin,
            scale
        );
        readyVersionSummaryLabel.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin,
            scale
        );
        readyVersionSummaryLabel.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryVerticalMargin,
            scale
        );
        readyVersionSummaryLabel.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryVerticalMargin,
            scale
        );
        readyVersionSummaryLabel.Visible = true;
        readyVersionSummaryLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        readyVersionSummaryPanel.AddChild(readyVersionSummaryLabel);

        return (readyVersionSummaryPanel, readyVersionSummaryLabel);
    }
}
