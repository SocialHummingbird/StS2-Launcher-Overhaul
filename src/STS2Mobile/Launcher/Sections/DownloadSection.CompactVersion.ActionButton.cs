using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class DownloadSection
{
    private void SetCompactVersionActionButtonText(Button button, string title, string detail)
    {
        if (!_compact)
        {
            button.Text = title;
            return;
        }

        var labels = EnsureCompactVersionActionButtonLabels(button);
        button.Text = "";
        labels.Body.Visible = true;
        labels.Title.Text = title;
        labels.Detail.Text = detail;
    }

    private (VBoxContainer Body, StyledLabel Title, StyledLabel Detail) EnsureCompactVersionActionButtonLabels(Button button)
    {
        var body = button.GetNodeOrNull<VBoxContainer>(new NodePath(CompactVersionActionBodyName));
        if (body != null)
        {
            return (
                body,
                body.GetNode<StyledLabel>(new NodePath(CompactVersionActionTitleName)),
                body.GetNode<StyledLabel>(new NodePath(CompactVersionActionDetailName))
            );
        }

        body = new VBoxContainer
        {
            Name = CompactVersionActionBodyName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactVersionActionHorizontalMargin, _scale);
        body.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactVersionActionHorizontalMargin, _scale);
        body.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactVersionActionVerticalMargin, _scale);
        body.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactVersionActionVerticalMargin, _scale);
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            0
        );

        var title = new StyledLabel(
            "",
            _scale,
            fontSize: CompactVersionActionTitleFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactVersionActionTitleName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        body.AddChild(title);

        var detail = new StyledLabel(
            "",
            _scale,
            fontSize: CompactVersionActionDetailFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactVersionActionDetailName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        detail.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        body.AddChild(detail);

        button.AddChild(body);
        return (body, title, detail);
    }
}
