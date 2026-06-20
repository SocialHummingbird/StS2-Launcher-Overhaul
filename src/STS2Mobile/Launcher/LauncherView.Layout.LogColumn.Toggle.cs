using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const string CompactDiagnosticsToggleBodyName = "CompactDiagnosticsToggleBody";
    private const string CompactDiagnosticsToggleTitleName = "CompactDiagnosticsToggleTitle";
    private const string CompactDiagnosticsToggleDetailName = "CompactDiagnosticsToggleDetail";
    private const int CompactDiagnosticsToggleTitleFontSize = LauncherSectionMetrics.CompactDetailButtonFontSize;
    private const int CompactDiagnosticsToggleDetailFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactDiagnosticsToggleHorizontalMargin = 6;
    private const int CompactDiagnosticsToggleVerticalMargin = 4;

    private static void SetDiagnosticsToggleText(
        Button toggle,
        LauncherLayoutProfile profile,
        bool visible
    )
    {
        if (!profile.Compact)
        {
            toggle.Text = DiagnosticsToggleText(visible);
            return;
        }

        toggle.Text = "";
        var labels = EnsureCompactDiagnosticsToggleLabels(toggle, profile.Scale);
        labels.Body.Visible = true;
        labels.Title.Text = visible ? "Hide Help" : "Help & Reports";
        labels.Detail.Text = visible ? "Back to launcher" : "Private until opened";
    }

    private static string DiagnosticsToggleText(bool visible)
        => visible ? "Hide Help & Reports" : "Show Help & Reports";

    private static (
        VBoxContainer Body,
        StyledLabel Title,
        StyledLabel Detail
    ) EnsureCompactDiagnosticsToggleLabels(Button toggle, float scale)
    {
        var body = toggle.GetNodeOrNull<VBoxContainer>(new NodePath(CompactDiagnosticsToggleBodyName));
        if (body != null)
        {
            return (
                body,
                body.GetNode<StyledLabel>(new NodePath(CompactDiagnosticsToggleTitleName)),
                body.GetNode<StyledLabel>(new NodePath(CompactDiagnosticsToggleDetailName))
            );
        }

        body = new VBoxContainer
        {
            Name = CompactDiagnosticsToggleBodyName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactDiagnosticsToggleHorizontalMargin, scale);
        body.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactDiagnosticsToggleHorizontalMargin, scale);
        body.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactDiagnosticsToggleVerticalMargin, scale);
        body.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactDiagnosticsToggleVerticalMargin, scale);
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            0
        );

        var title = new StyledLabel(
            "",
            scale,
            fontSize: CompactDiagnosticsToggleTitleFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactDiagnosticsToggleTitleName,
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
            scale,
            fontSize: CompactDiagnosticsToggleDetailFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactDiagnosticsToggleDetailName,
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

        toggle.AddChild(body);
        return (body, title, detail);
    }
}
