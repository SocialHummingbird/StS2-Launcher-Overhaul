using System;
using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal static class LauncherSectionSetup
{
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
            LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.SectionSeparation, scale)
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
        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildHeaderStyle(scale)
        );

        var body = new VBoxContainer();
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(4, scale)
        );
        panel.AddChild(body);

        var accentLine = new ColorRect();
        accentLine.Color = accent;
        accentLine.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(3, scale)
        );
        body.AddChild(accentLine);

        var titleLabel = new StyledLabel(title, scale, fontSize: 13, align: HorizontalAlignment.Left);
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

    private static StyleBoxFlat BuildHeaderStyle(float scale)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(0.035f, 0.06f, 0.08f, 0.92f),
            LauncherViewLayoutMetrics.ScaleInt(8, scale)
        );
        style.BorderColor = new Color(0.08f, 0.36f, 0.42f, 0.65f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(10, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(10, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(8, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(9, scale);
        return style;
    }
}
