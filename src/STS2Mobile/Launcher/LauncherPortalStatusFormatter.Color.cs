using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalStatusFormatter
{
    internal static Color ColorFor(string phase)
        => phase switch
        {
            "Attention" => LauncherComponentTheme.OrangeHot,
            "Steam" => LauncherComponentTheme.CyanAccent,
            "Version" => LauncherComponentTheme.CyanAccent,
            "Install" => LauncherComponentTheme.OrangeAccent,
            "Cloud" => LauncherComponentTheme.CyanAccent,
            "Ready" => new Color(0.36f, 0.9f, 0.42f),
            "Details" => LauncherComponentTheme.TextSecondary,
            _ => LauncherComponentTheme.TextSecondary,
        };
}
