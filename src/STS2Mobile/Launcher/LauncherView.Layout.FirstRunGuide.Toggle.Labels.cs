using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static LauncherViewSafeFlowToggleLabels EnsureCompactSafeFlowToggleLabels(
        Button toggle,
        float scale
    )
    {
        var body = toggle.GetNodeOrNull<VBoxContainer>(new NodePath(CompactSafeFlowToggleBodyName));
        if (body != null)
        {
            return new LauncherViewSafeFlowToggleLabels(
                body,
                body.GetNode<StyledLabel>(new NodePath(CompactSafeFlowToggleTitleName)),
                body.GetNode<StyledLabel>(new NodePath(CompactSafeFlowToggleDetailName))
            );
        }

        body = BuildCompactSafeFlowToggleBody(scale);
        var title = BuildCompactSafeFlowToggleTitle(scale);
        body.AddChild(title);

        var detailLabel = BuildCompactSafeFlowToggleDetail(scale);
        body.AddChild(detailLabel);

        toggle.AddChild(body);
        return new LauncherViewSafeFlowToggleLabels(body, title, detailLabel);
    }

    private static VBoxContainer BuildCompactSafeFlowToggleBody(float scale)
    {
        var body = new VBoxContainer
        {
            Name = CompactSafeFlowToggleBodyName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowToggleHorizontalMargin, scale);
        body.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowToggleHorizontalMargin, scale);
        body.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowToggleVerticalMargin, scale);
        body.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowToggleVerticalMargin, scale);
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            0
        );
        return body;
    }

    private static StyledLabel BuildCompactSafeFlowToggleTitle(float scale)
    {
        var title = new StyledLabel(
            "",
            scale,
            fontSize: CompactSafeFlowToggleTitleFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactSafeFlowToggleTitleName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        return title;
    }

    private static StyledLabel BuildCompactSafeFlowToggleDetail(float scale)
    {
        var detailLabel = new StyledLabel(
            "",
            scale,
            fontSize: CompactSafeFlowToggleDetailFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactSafeFlowToggleDetailName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        detailLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        return detailLabel;
    }
}
