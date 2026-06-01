using Godot;
using STS2Mobile.Launcher;

namespace STS2Mobile.Launcher.Sections;

internal static class LauncherSectionSetup
{
    internal static void ConfigureHiddenSection(VBoxContainer section, float scale)
    {
        section.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.SectionSeparation, scale)
        );
        section.Visible = false;
    }
}
