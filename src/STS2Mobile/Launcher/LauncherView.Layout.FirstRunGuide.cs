using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;
internal sealed partial class LauncherView
{
    private const int CompactSafeFlowGuideTitleHeight = 24;
    private const int CompactSafeFlowGuideTitleFontSize = LauncherSectionMetrics.CompactDetailButtonFontSize;
    private static Control BuildFirstRunGuide(float scale, bool compact)
    {
        if (compact)
            return BuildCollapsedFirstRunGuide(scale);
        return BuildFirstRunGuidePanel(scale, compact: false);
    }

    private static Control BuildFirstRunGuidePanel(float scale, bool compact)
    {
        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride(LauncherComponentTheme.Panel, BuildFirstRunGuideStyle(scale, compact));
        var body = new VBoxContainer();
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(5, scale));
        panel.AddChild(body);
        var title = new StyledLabel("Quick start guide", scale, fontSize: compact ? CompactSafeFlowGuideTitleFontSize : 12, align: HorizontalAlignment.Left);
        if (compact)
        {
            title.AutowrapMode = TextServer.AutowrapMode.Off;
            title.ClipText = true;
            title.VerticalAlignment = VerticalAlignment.Center;
            title.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            title.CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideTitleHeight, scale));
        }

        title.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, LauncherComponentTheme.CyanAccent);
        body.AddChild(title);
        if (compact)
        {
            AddCompactSafeFlowSteps(body, scale);
            return panel;
        }

        var guidance = new StyledLabel("Sign in, choose a game version, download its files, then start the game.", scale, fontSize: 11, align: HorizontalAlignment.Left);
        guidance.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        guidance.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, LauncherComponentTheme.TextSecondary);
        body.AddChild(guidance);
        return panel;
    }

    private static StyleBoxFlat BuildFirstRunGuideStyle(float scale, bool compact)
    {
        var style = LauncherStyleBoxes.MakeFilled(new Color(0.025f, 0.045f, 0.06f, 0.88f), LauncherViewLayoutMetrics.ScaleInt(8, scale));
        style.BorderColor = new Color(0.05f, 0.5f, 0.58f, 0.35f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 10, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 10, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(compact ? 6 : 8, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(compact ? 6 : 8, scale);
        return style;
    }

    private static Control BuildCompactSafeFlowStep(float scale, CompactSafeFlowStepSpec step)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepHeight, scale)),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride(LauncherComponentTheme.Panel, BuildCompactSafeFlowStepStyle(scale, step.Accent));
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        row.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(7, scale));
        row.AddChild(BuildCompactSafeFlowStepAccent(scale, step.Accent));
        row.AddChild(BuildCompactSafeFlowStepMarker(scale, step));
        row.AddChild(BuildCompactSafeFlowStepText(scale, step));
        panel.AddChild(row);
        return panel;
    }

    private static VBoxContainer BuildCompactSafeFlowStepText(float scale, CompactSafeFlowStepSpec step)
    {
        var textColumn = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        textColumn.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, 0);
        textColumn.AddChild(BuildCompactSafeFlowStepTitle(scale, step.Title));
        textColumn.AddChild(BuildCompactSafeFlowStepDetail(scale, step.Detail));
        return textColumn;
    }

    private static ColorRect BuildCompactSafeFlowStepAccent(float scale, Color accent) => new()
    {
        Color = accent,
        CustomMinimumSize = new Vector2(LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepAccentWidth, scale), 0),
        SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
    private static StyledLabel BuildCompactSafeFlowStepMarker(float scale, CompactSafeFlowStepSpec step)
    {
        var markerLabel = new StyledLabel(step.Marker, scale, fontSize: CompactSafeFlowGuideStepNumberFontSize, align: HorizontalAlignment.Center)
        {
            CustomMinimumSize = new Vector2(LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepNumberWidth, scale), 0),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
        };
        markerLabel.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, step.Accent);
        return markerLabel;
    }

    private static StyledLabel BuildCompactSafeFlowStepTitle(float scale, string title)
    {
        var titleLabel = new StyledLabel(title, scale, fontSize: CompactSafeFlowGuideStepTitleFontSize, align: HorizontalAlignment.Left)
        {
            ClipText = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        titleLabel.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, LauncherComponentTheme.TextPrimary);
        return titleLabel;
    }

    private static StyledLabel BuildCompactSafeFlowStepDetail(float scale, string detail)
    {
        var detailLabel = new StyledLabel(detail, scale, fontSize: CompactSafeFlowGuideStepDetailFontSize, align: HorizontalAlignment.Left)
        {
            ClipText = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            TooltipText = detail,
            VerticalAlignment = VerticalAlignment.Center,
        };
        detailLabel.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, LauncherComponentTheme.TextSecondary);
        return detailLabel;
    }

    private const int CompactSafeFlowGuideStepHeight = 42;
    private const int CompactSafeFlowGuideStepAccentWidth = 3;
    private const int CompactSafeFlowGuideStepNumberWidth = 26;
    private const int CompactSafeFlowGuideStepNumberFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactSafeFlowGuideStepTitleFontSize = LauncherSectionMetrics.CompactDetailButtonFontSize;
    private const int CompactSafeFlowGuideStepDetailFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactSafeFlowGuideStepRadius = 6;
    private const int CompactSafeFlowGuideStepHorizontalMargin = 7;
    private const int CompactSafeFlowGuideStepVerticalMargin = 4;
    private readonly record struct CompactSafeFlowStepSpec(string Marker, string Title, string Detail, Color Accent);
    private static readonly CompactSafeFlowStepSpec[] CompactSafeFlowSteps =
    {
        new("1", "Sign in", "Steam account", LauncherComponentTheme.OrangeAccent),
        new("2", "Get files", "Version on Android", LauncherComponentTheme.CyanAccent),
        new("3", "Play", "Game and saves", LauncherComponentTheme.OrangeHot),
    };
    private static void AddCompactSafeFlowSteps(VBoxContainer body, float scale)
    {
        foreach (var step in CompactSafeFlowSteps)
            body.AddChild(BuildCompactSafeFlowStep(scale, step));
    }

    private static StyleBoxFlat BuildCompactSafeFlowStepStyle(float scale, Color accent)
    {
        var style = LauncherStyleBoxes.MakeFilled(new Color(0.03f, 0.055f, 0.07f, 0.88f), LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepRadius, scale));
        style.BorderColor = new Color(accent.R, accent.G, accent.B, 0.3f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepHorizontalMargin, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepHorizontalMargin, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepVerticalMargin, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepVerticalMargin, scale);
        return style;
    }

    private const string CompactSafeFlowToggleBodyName = "CompactSafeFlowToggleBody";
    private const string CompactSafeFlowToggleTitleName = "CompactSafeFlowToggleTitle";
    private const string CompactSafeFlowToggleDetailName = "CompactSafeFlowToggleDetail";
    private static readonly CompactButtonDetailLabelSpec CompactSafeFlowToggleLabels = CompactButtonDetailLabelSpec.Default(CompactSafeFlowToggleBodyName, CompactSafeFlowToggleTitleName, CompactSafeFlowToggleDetailName);
    private static Control BuildCollapsedFirstRunGuide(float scale)
    {
        var wrapper = new VBoxContainer();
        wrapper.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        wrapper.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(5, scale));
        var toggle = new StyledButton("", scale, fontSize: LauncherSectionMetrics.CompactDetailButtonFontSize, height: LauncherSectionMetrics.CompactDrawerToggleHeight);
        LauncherButtonStyles.ApplySupportAction(toggle, scale);
        SetCompactSafeFlowToggleText(toggle, scale, "Quick Start", "Sign in and play");
        wrapper.AddChild(toggle);
        var guide = BuildFirstRunGuidePanel(scale, compact: true);
        guide.Visible = false;
        wrapper.AddChild(guide);
        toggle.Pressed += () =>
        {
            guide.Visible = !guide.Visible;
            if (guide.Visible)
                SetCompactSafeFlowToggleText(toggle, scale, "Hide Guide", "Safe order");
            else
                SetCompactSafeFlowToggleText(toggle, scale, "Quick Start", "Sign in and play");
        };
        return wrapper;
    }

    private static void SetCompactSafeFlowToggleText(Button toggle, float scale, string title, string detail) => CompactButtonDetailLabels.Apply(toggle, $"{title}\n{detail}", scale, enabled: true, CompactSafeFlowToggleLabels);
}
