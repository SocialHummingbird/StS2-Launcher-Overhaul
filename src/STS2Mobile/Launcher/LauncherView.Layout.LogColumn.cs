using System;
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
    private const float CompactDiagnosticsLogViewportHeightRatio = 0.28f;
    private const int CompactDiagnosticsLogMinHeight = 220;
    private const int CompactDiagnosticsLogMaxHeight = 340;

    private static (
        RichTextLabel Log,
        VBoxContainer Drawer,
        Button Toggle
    ) BuildLogColumn(
        LauncherLayoutProfile profile,
        VBoxContainer root,
        Action<InputEvent> dismissKeyboard
    )
    {
        var scale = profile.Scale;
        var drawer = new VBoxContainer();
        drawer.Visible = false;
        drawer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        drawer.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(6, scale)
        );

        var toggle = new StyledButton(
            profile.Compact ? "" : DiagnosticsToggleText(visible: false),
            scale,
            fontSize: profile.Compact
                ? LauncherSectionMetrics.CompactDetailButtonFontSize
                : 14,
            height: profile.Compact
                ? LauncherSectionMetrics.CompactDrawerToggleHeight
                : 48
        );
        LauncherButtonStyles.ApplySupportAction(toggle, scale);
        SetDiagnosticsToggleText(toggle, profile, visible: false);
        toggle.Pressed += () =>
        {
            drawer.Visible = !drawer.Visible;
            SetDiagnosticsToggleText(toggle, profile, drawer.Visible);
        };
        root.AddChild(toggle);

        var title = new StyledLabel(
            "Help & Reports",
            scale,
            fontSize: profile.Compact
                ? LauncherSectionMetrics.PromptFontSize
                : LauncherViewLayoutMetrics.LogTitleFontSize,
            align: HorizontalAlignment.Left
        );
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        drawer.AddChild(title);

        var help = new StyledLabel(
            profile.Compact
                ? "Problem details and help reports. Review before sharing."
                : "Hidden by default. Create a help report when sharing launcher issue details.",
            scale,
            fontSize: profile.Compact
                ? LauncherSectionMetrics.ProgressFontSize
                : 11,
            align: HorizontalAlignment.Left
        );
        help.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        help.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextMuted
        );
        drawer.AddChild(help);

        var log = BuildLogView(profile);
        log.CustomMinimumSize = new Vector2(0, DiagnosticsLogHeight(profile));
        log.GuiInput += input => dismissKeyboard(input);
        drawer.AddChild(log);
        root.AddChild(drawer);
        return (log, drawer, toggle);
    }

    private static int DiagnosticsLogHeight(LauncherLayoutProfile profile)
    {
        if (!profile.Compact)
            return LauncherViewLayoutMetrics.ScaleInt(180, profile.Scale);

        var viewportHeight = (int)MathF.Round(
            profile.ViewportSize.Y * CompactDiagnosticsLogViewportHeightRatio,
            MidpointRounding.AwayFromZero
        );
        var minHeight = LauncherViewLayoutMetrics.ScaleInt(CompactDiagnosticsLogMinHeight, profile.Scale);
        var maxHeight = LauncherViewLayoutMetrics.ScaleInt(CompactDiagnosticsLogMaxHeight, profile.Scale);
        return Math.Clamp(viewportHeight, minHeight, maxHeight);
    }

    private void UpdateDiagnosticsLogViewport(Vector2 viewportSize)
    {
        if (!GodotObject.IsInstanceValid(Log))
            return;

        var profile = viewportSize.X > 0f && viewportSize.Y > 0f
            ? LauncherLayoutProfile.ForViewport(viewportSize)
            : _profile;
        Log.CustomMinimumSize = new Vector2(0, DiagnosticsLogHeight(profile));
        if (_profile.Compact && DiagnosticsDrawer.Visible)
            ScrollCompactPrimaryTo(DiagnosticsDrawer);
    }

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
