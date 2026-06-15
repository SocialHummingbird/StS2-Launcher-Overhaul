using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static (
        StyledLabel StatusPhase,
        StyledLabel StatusAction,
        StyledLabel Status,
        ColorRect StatusAccent,
        LoginSection Login,
        CodeSection Code,
        DownloadSection Download,
        ActionSection Actions
    ) BuildPrimaryColumn(LauncherLayoutProfile profile, VBoxContainer root)
    {
        var scale = profile.Scale;
        var leftScroll = new ScrollContainer();
        leftScroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        leftScroll.FollowFocus = true;
        root.AddChild(leftScroll);

        var leftFrame = new MarginContainer();
        leftFrame.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftFrame.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        leftScroll.AddChild(leftFrame);

        var left = new VBoxContainer();
        left.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        left.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        left.CustomMinimumSize = new Vector2(
            profile.ContentMaxWidth,
            0
        );
        left.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(LauncherViewLayoutMetrics.PrimaryColumnSeparation, scale)
        );
        leftFrame.AddChild(left);

        var initialPhase = LauncherPortalStatusFormatter.PhaseFor("Initializing...");
        var statusPhaseLabel = new StyledLabel(
            initialPhase,
            scale,
            fontSize: 11,
            align: HorizontalAlignment.Center
        );
        statusPhaseLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherPortalStatusFormatter.ColorFor(initialPhase)
        );

        var statusActionLabel = new StyledLabel(
            LauncherPortalStatusFormatter.ActionFor("Initializing..."),
            scale,
            fontSize: 10,
            align: HorizontalAlignment.Center
        );
        statusActionLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );

        var statusLabel = new StyledLabel(
            LauncherPortalStatusFormatter.MessageFor("Initializing..."),
            scale,
            fontSize: 14,
            align: HorizontalAlignment.Left
        );
        statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        statusLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        var statusAccent = new ColorRect();
        statusAccent.Color = LauncherPortalStatusFormatter.ColorFor(initialPhase);
        left.AddChild(BuildStatusCapsule(statusPhaseLabel, statusActionLabel, statusLabel, statusAccent, scale, profile.Compact));
        left.AddChild(BuildFirstRunGuide(scale, profile.Compact));

        var login = new LoginSection(scale, profile.Compact);
        left.AddChild(login);

        var code = new CodeSection(scale, profile.Compact);
        left.AddChild(code);

        var download = new DownloadSection(scale, profile.Compact);
        left.AddChild(download);

        var actions = new ActionSection(scale, profile.Compact);
        left.AddChild(actions);

        left.AddChild(BuildFmodAttributionSection(scale));

        return (statusPhaseLabel, statusActionLabel, statusLabel, statusAccent, login, code, download, actions);
    }

    private static Control BuildStatusCapsule(
        StyledLabel statusPhaseLabel,
        StyledLabel statusActionLabel,
        StyledLabel statusLabel,
        ColorRect statusAccent,
        float scale,
        bool compact
    )
    {
        if (compact)
            return BuildCompactStatusCapsule(statusPhaseLabel, statusActionLabel, statusLabel, statusAccent, scale);

        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildStatusStyle(scale, compact: false)
        );

        var body = new HBoxContainer();
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(8, scale)
        );
        panel.AddChild(body);

        statusAccent.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(4, scale),
            LauncherViewLayoutMetrics.ScaleInt(30, scale)
        );
        body.AddChild(statusAccent);

        var phasePanel = new PanelContainer();
        phasePanel.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(96, scale),
            0
        );
        phasePanel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildStatusPhaseStyle(scale)
        );
        var phaseBody = new VBoxContainer();
        phaseBody.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        phaseBody.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(1, scale)
        );
        statusPhaseLabel.VerticalAlignment = VerticalAlignment.Center;
        phaseBody.AddChild(statusPhaseLabel);
        phaseBody.AddChild(statusActionLabel);
        phasePanel.AddChild(phaseBody);
        body.AddChild(phasePanel);

        statusLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddChild(statusLabel);
        return panel;
    }

    private static Control BuildCompactStatusCapsule(
        StyledLabel statusPhaseLabel,
        StyledLabel statusActionLabel,
        StyledLabel statusLabel,
        ColorRect statusAccent,
        float scale
    )
    {
        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildStatusStyle(scale, compact: true)
        );

        var body = new VBoxContainer();
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(7, scale)
        );
        panel.AddChild(body);

        statusAccent.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(4, scale)
        );
        body.AddChild(statusAccent);

        var header = new HBoxContainer();
        header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(8, scale)
        );
        body.AddChild(header);

        statusActionLabel.HorizontalAlignment = HorizontalAlignment.Left;
        statusActionLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        statusActionLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(statusActionLabel);

        var phasePanel = new PanelContainer();
        phasePanel.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(112, scale),
            0
        );
        phasePanel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildStatusPhaseStyle(scale)
        );
        statusPhaseLabel.VerticalAlignment = VerticalAlignment.Center;
        phasePanel.AddChild(statusPhaseLabel);
        header.AddChild(phasePanel);

        statusLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        statusLabel.HorizontalAlignment = HorizontalAlignment.Left;
        body.AddChild(statusLabel);

        return panel;
    }

    private static StyleBoxFlat BuildStatusStyle(float scale, bool compact)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(0.02f, 0.04f, 0.06f, 0.92f),
            LauncherViewLayoutMetrics.ScaleInt(8, scale)
        );
        style.BorderColor = new Color(0.05f, 0.5f, 0.58f, 0.7f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 10, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 10, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(compact ? 7 : 8, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 8, scale);
        return style;
    }

    private static StyleBoxFlat BuildStatusPhaseStyle(float scale)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(0.045f, 0.075f, 0.095f, 0.95f),
            LauncherViewLayoutMetrics.ScaleInt(7, scale)
        );
        style.BorderColor = new Color(0.08f, 0.36f, 0.42f, 0.65f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(8, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(8, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(5, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(5, scale);
        return style;
    }

    private static Control BuildFirstRunGuide(float scale, bool compact)
    {
        if (compact)
            return BuildCollapsedFirstRunGuide(scale);

        return BuildFirstRunGuidePanel(scale, compact: false);
    }

    private static Control BuildCollapsedFirstRunGuide(float scale)
    {
        var wrapper = new VBoxContainer();
        wrapper.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        wrapper.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(5, scale)
        );

        var toggle = new StyledButton(
            "SHOW SAFE FLOW",
            scale,
            fontSize: 12,
            height: 34
        );
        LauncherButtonStyles.ApplySupportAction(toggle, scale);
        wrapper.AddChild(toggle);

        var guide = BuildFirstRunGuidePanel(scale, compact: true);
        guide.Visible = false;
        wrapper.AddChild(guide);

        toggle.Pressed += () =>
        {
            guide.Visible = !guide.Visible;
            toggle.Text = guide.Visible ? "HIDE SAFE FLOW" : "SHOW SAFE FLOW";
        };

        return wrapper;
    }

    private static Control BuildFirstRunGuidePanel(float scale, bool compact)
    {
        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildFirstRunGuideStyle(scale)
        );

        var body = new VBoxContainer();
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(5, scale)
        );
        panel.AddChild(body);

        var title = new StyledLabel(
            "Safe first-run flow",
            scale,
            fontSize: 12,
            align: HorizontalAlignment.Left
        );
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.CyanAccent
        );
        body.AddChild(title);

        var guidance = new StyledLabel(
            compact
                ? "Safe flow: Sign in -> Download version -> Pull saves -> Launch. Push only after Android local saves are verified."
                : "1. Sign in with Steam.  2. Choose/download a game version.  3. Pull saves before any Push.  4. Launch when the Play and Sync section is ready.",
            scale,
            fontSize: 11,
            align: HorizontalAlignment.Left
        );
        guidance.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        guidance.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        body.AddChild(guidance);

        return panel;
    }

    private static StyleBoxFlat BuildFirstRunGuideStyle(float scale)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(0.025f, 0.045f, 0.06f, 0.88f),
            LauncherViewLayoutMetrics.ScaleInt(8, scale)
        );
        style.BorderColor = new Color(0.05f, 0.5f, 0.58f, 0.35f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(10, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(10, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(8, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(8, scale);
        return style;
    }
}
