using System;
using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal static class LauncherSectionSetup
{
    private const int CompactSectionHeaderAccentWidth = 3;
    private const int CompactSectionHeaderCueFontSize = 12;
    private const int CompactSectionHeaderMinHeight = 42;
    private const int CompactSectionHeaderTitleFontSize = 14;
    private const int CompactSectionHeaderTitleMinWidth = 106;

    internal static void ConfigureHiddenSection(
        VBoxContainer section,
        float scale,
        string title,
        string subtitle,
        Color accent,
        bool compact = false
    )
    {
        section.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(
                compact
                    ? LauncherSectionMetrics.CompactSectionSeparation
                    : LauncherSectionMetrics.SectionSeparation,
                scale
            )
        );
        section.Visible = false;
        section.AddChild(BuildSectionHeader(title, subtitle, scale, accent, compact));
    }

    private static Control BuildSectionHeader(
        string title,
        string subtitle,
        float scale,
        Color accent,
        bool compact
    )
    {
        if (compact)
            return BuildCompactSectionHeader(title, subtitle, scale, accent);

        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildHeaderStyle(scale, compact)
        );

        var body = new VBoxContainer();
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(compact ? 2 : 4, scale)
        );
        panel.AddChild(body);

        var accentLine = new ColorRect();
        accentLine.Color = accent;
        accentLine.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(compact ? 2 : 3, scale)
        );
        body.AddChild(accentLine);

        var titleLabel = new StyledLabel(
            title,
            scale,
            fontSize: compact ? 15 : 13,
            align: HorizontalAlignment.Left
        );
        titleLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        body.AddChild(titleLabel);

        if (!compact && !string.IsNullOrWhiteSpace(subtitle))
        {
            var subtitleLabel = new StyledLabel(subtitle, scale, fontSize: 11, align: HorizontalAlignment.Left);
            subtitleLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            subtitleLabel.AddThemeColorOverride(
                LauncherViewLayoutMetrics.ThemeFontColor,
                LauncherComponentTheme.TextSecondary
            );
            body.AddChild(subtitleLabel);
        }

        return panel;
    }

    private static Control BuildCompactSectionHeader(
        string title,
        string subtitle,
        float scale,
        Color accent
    )
    {
        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(CompactSectionHeaderMinHeight, scale)
        );
        panel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildHeaderStyle(scale, compact: true)
        );

        var body = new HBoxContainer();
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(6, scale)
        );
        panel.AddChild(body);

        var accentLine = new ColorRect();
        accentLine.Color = accent;
        accentLine.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(CompactSectionHeaderAccentWidth, scale),
            0
        );
        accentLine.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        body.AddChild(accentLine);

        var titleLabel = new StyledLabel(
            title,
            scale,
            fontSize: CompactSectionHeaderTitleFontSize,
            align: HorizontalAlignment.Left
        );
        titleLabel.VerticalAlignment = VerticalAlignment.Center;
        titleLabel.ClipText = true;
        titleLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        titleLabel.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(CompactSectionHeaderTitleMinWidth, scale),
            0
        );
        titleLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        body.AddChild(titleLabel);

        var cueLabel = new StyledLabel(
            subtitle,
            scale,
            fontSize: CompactSectionHeaderCueFontSize,
            align: HorizontalAlignment.Left
        );
        cueLabel.VerticalAlignment = VerticalAlignment.Center;
        cueLabel.AutowrapMode = TextServer.AutowrapMode.Off;
        cueLabel.ClipText = true;
        cueLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        cueLabel.TooltipText = subtitle;
        cueLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        cueLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        body.AddChild(cueLabel);

        return panel;
    }

    private static StyleBoxFlat BuildHeaderStyle(float scale, bool compact)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(0.035f, 0.06f, 0.08f, 0.92f),
            LauncherViewLayoutMetrics.ScaleInt(compact ? 6 : 8, scale)
        );
        style.BorderColor = new Color(0.08f, 0.36f, 0.42f, 0.65f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(compact ? 7 : 10, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(compact ? 7 : 10, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(compact ? 4 : 8, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(compact ? 4 : 9, scale);
        return style;
    }
}
