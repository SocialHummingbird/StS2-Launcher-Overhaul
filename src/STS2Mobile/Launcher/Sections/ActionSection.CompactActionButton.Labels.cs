using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private ActionSectionCompactActionButtonLabels EnsureCompactActionButtonLabels(Button button)
    {
        var body = button.GetNodeOrNull<VBoxContainer>(new NodePath(CompactActionButtonBodyName));
        if (body != null)
        {
            return new ActionSectionCompactActionButtonLabels(
                body,
                body.GetNode<StyledLabel>(new NodePath(CompactActionButtonTitleName)),
                body.GetNode<StyledLabel>(new NodePath(CompactActionButtonDetailName))
            );
        }

        body = BuildCompactActionButtonBody();
        var title = BuildCompactActionButtonTitle();
        body.AddChild(title);
        var detail = BuildCompactActionButtonDetail();
        body.AddChild(detail);

        button.AddChild(body);
        return new ActionSectionCompactActionButtonLabels(body, title, detail);
    }

    private VBoxContainer BuildCompactActionButtonBody()
    {
        var body = new VBoxContainer
        {
            Name = CompactActionButtonBodyName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactActionButtonHorizontalMargin, _scale);
        body.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactActionButtonHorizontalMargin, _scale);
        body.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactActionButtonVerticalMargin, _scale);
        body.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactActionButtonVerticalMargin, _scale);
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            0
        );
        return body;
    }

    private StyledLabel BuildCompactActionButtonTitle()
    {
        var title = new StyledLabel(
            "",
            _scale,
            fontSize: CompactActionButtonTitleFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactActionButtonTitleName,
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

    private StyledLabel BuildCompactActionButtonDetail()
    {
        var detail = new StyledLabel(
            "",
            _scale,
            fontSize: CompactActionButtonDetailFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactActionButtonDetailName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        detail.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        return detail;
    }
}
