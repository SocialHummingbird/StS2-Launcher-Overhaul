using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal static partial class LauncherSectionSetup
{
    private static Control BuildSectionHeader(
        string title,
        string subtitle,
        float scale,
        Color accent,
        bool compact,
        string compactCue
    )
    {
        if (compact)
            return BuildCompactSectionHeader(title, CompactCueText(compactCue, subtitle), subtitle, scale, accent);

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
            LauncherViewLayoutMetrics.ScaleInt(4, scale)
        );
        panel.AddChild(body);

        body.AddChild(BuildDesktopSectionAccent(accent, scale));
        body.AddChild(BuildDesktopSectionTitle(title, scale));
        AddDesktopSectionSubtitle(body, subtitle, scale);
        return panel;
    }

    private static ColorRect BuildDesktopSectionAccent(Color accent, float scale)
        => new()
        {
            Color = accent,
            CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(3, scale)
            ),
        };

    private static StyledLabel BuildDesktopSectionTitle(string title, float scale)
    {
        var titleLabel = new StyledLabel(
            title,
            scale,
            fontSize: 13,
            align: HorizontalAlignment.Left
        );
        titleLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        return titleLabel;
    }

    private static void AddDesktopSectionSubtitle(VBoxContainer body, string subtitle, float scale)
    {
        if (string.IsNullOrWhiteSpace(subtitle))
            return;

        var subtitleLabel = new StyledLabel(subtitle, scale, fontSize: 11, align: HorizontalAlignment.Left);
        subtitleLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        subtitleLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        body.AddChild(subtitleLabel);
    }
}
