using Godot;
using STS2Mobile.Launcher;

namespace STS2Mobile.Launcher.Sections;

internal static partial class LauncherSectionSetup
{
    internal static void ConfigureHiddenSection(
        VBoxContainer section,
        float scale,
        string title,
        string subtitle,
        Color accent,
        bool compact = false,
        string compactCue = null
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
        section.AddChild(BuildSectionHeader(title, subtitle, scale, accent, compact, compactCue));
    }
}
