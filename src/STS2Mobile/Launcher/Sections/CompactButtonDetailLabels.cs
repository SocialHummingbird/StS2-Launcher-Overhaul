using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal static class CompactButtonDetailLabels
{
    internal static void Apply(
        Button button,
        string text,
        float scale,
        bool enabled,
        CompactButtonDetailLabelSpec spec
    )
    {
        if (!enabled || !TrySplitText(text, out var title, out var detail))
        {
            Hide(button, spec);
            button.Text = text;
            return;
        }

        var labels = Ensure(button, scale, spec);
        button.Text = "";
        labels.Body.Visible = true;
        labels.Title.Text = title;
        labels.Detail.Text = detail;
    }

    private static bool TrySplitText(string text, out string title, out string detail)
    {
        title = text ?? "";
        detail = "";
        var separator = title.IndexOf('\n');
        if (separator < 0)
            return false;

        detail = title[(separator + 1)..].Trim();
        title = title[..separator].Trim();
        return title.Length > 0 && detail.Length > 0;
    }

    private static void Hide(Button button, CompactButtonDetailLabelSpec spec)
    {
        var body = button.GetNodeOrNull<VBoxContainer>(new NodePath(spec.BodyName));
        if (body != null)
            body.Visible = false;
    }

    private static (VBoxContainer Body, StyledLabel Title, StyledLabel Detail) Ensure(
        Button button,
        float scale,
        CompactButtonDetailLabelSpec spec
    )
    {
        var body = button.GetNodeOrNull<VBoxContainer>(new NodePath(spec.BodyName));
        if (body != null)
        {
            return (
                body,
                body.GetNode<StyledLabel>(new NodePath(spec.TitleName)),
                body.GetNode<StyledLabel>(new NodePath(spec.DetailName))
            );
        }

        body = BuildBody(scale, spec);
        var title = BuildLabel(spec.TitleName, spec.TitleFontSize, scale, LauncherComponentTheme.TextPrimary);
        var detail = BuildLabel(spec.DetailName, spec.DetailFontSize, scale, LauncherComponentTheme.TextSecondary);

        body.AddChild(title);
        body.AddChild(detail);
        button.AddChild(body);
        return (body, title, detail);
    }

    private static VBoxContainer BuildBody(float scale, CompactButtonDetailLabelSpec spec)
    {
        var body = new VBoxContainer
        {
            Name = spec.BodyName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(spec.HorizontalMargin, scale);
        body.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(spec.HorizontalMargin, scale);
        body.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(spec.VerticalMargin, scale);
        body.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(spec.VerticalMargin, scale);
        body.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, 0);
        return body;
    }

    private static StyledLabel BuildLabel(
        string name,
        int fontSize,
        float scale,
        Color color
    )
    {
        var label = new StyledLabel(
            "",
            scale,
            fontSize: fontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = name,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        label.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, color);
        return label;
    }
}
