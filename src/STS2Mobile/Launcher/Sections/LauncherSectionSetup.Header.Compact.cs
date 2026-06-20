using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal static partial class LauncherSectionSetup
{
    private const int CompactSectionHeaderAccentWidth = 3;
    private const int CompactSectionHeaderCueFontSize = 12;
    private const int CompactSectionHeaderMinHeight = 42;
    private const int CompactSectionHeaderTitleFontSize = 14;
    private const int CompactSectionHeaderTitleMinWidth = 106;

    private static Control BuildCompactSectionHeader(
        string title,
        string cue,
        string tooltip,
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

        body.AddChild(BuildCompactSectionAccent(accent, scale));
        body.AddChild(BuildCompactSectionTitle(title, scale));
        body.AddChild(BuildCompactSectionCue(cue, tooltip, scale));
        return panel;
    }

    private static ColorRect BuildCompactSectionAccent(Color accent, float scale)
        => new()
        {
            Color = accent,
            CustomMinimumSize = new Vector2(
                LauncherViewLayoutMetrics.ScaleInt(CompactSectionHeaderAccentWidth, scale),
                0
            ),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };

    private static StyledLabel BuildCompactSectionTitle(string title, float scale)
    {
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
        return titleLabel;
    }

    private static StyledLabel BuildCompactSectionCue(string cue, string tooltip, float scale)
    {
        var cueLabel = new StyledLabel(
            cue,
            scale,
            fontSize: CompactSectionHeaderCueFontSize,
            align: HorizontalAlignment.Left
        );
        cueLabel.VerticalAlignment = VerticalAlignment.Center;
        cueLabel.AutowrapMode = TextServer.AutowrapMode.Off;
        cueLabel.ClipText = true;
        cueLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        cueLabel.TooltipText = tooltip;
        cueLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        cueLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        return cueLabel;
    }

    private static string CompactCueText(string compactCue, string fallback)
        => string.IsNullOrWhiteSpace(compactCue) ? fallback : compactCue;
}
