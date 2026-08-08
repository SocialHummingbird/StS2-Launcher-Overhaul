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
        panel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildFirstRunGuideStyle(scale, compact)
        );

        var body = new VBoxContainer();
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(5, scale)
        );
        panel.AddChild(body);

        var title = new StyledLabel(
            "Quick start guide",
            scale,
            fontSize: compact ? CompactSafeFlowGuideTitleFontSize : 12,
            align: HorizontalAlignment.Left
        );
        if (compact)
        {
            title.AutowrapMode = TextServer.AutowrapMode.Off;
            title.ClipText = true;
            title.VerticalAlignment = VerticalAlignment.Center;
            title.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            title.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideTitleHeight, scale)
            );
        }
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.CyanAccent
        );
        body.AddChild(title);

        if (compact)
        {
            AddCompactSafeFlowSteps(body, scale);
            return panel;
        }

        var guidance = new StyledLabel(
            "Sign in, choose a game version, get Steam saves, then start the game. Upload stays locked until you deliberately open it after checking local saves.",
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

    private static StyleBoxFlat BuildFirstRunGuideStyle(float scale, bool compact)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(0.025f, 0.045f, 0.06f, 0.88f),
            LauncherViewLayoutMetrics.ScaleInt(8, scale)
        );
        style.BorderColor = new Color(0.05f, 0.5f, 0.58f, 0.35f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 10, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 10, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(compact ? 6 : 8, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(compact ? 6 : 8, scale);
        return style;
    }
}
