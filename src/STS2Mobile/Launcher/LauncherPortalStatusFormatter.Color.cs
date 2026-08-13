using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalStatusFormatter
{
    internal static Color ColorFor(LauncherStatusSeverity severity)
        => severity switch
        {
            LauncherStatusSeverity.Ready => new Color(0.36f, 0.9f, 0.42f),
            LauncherStatusSeverity.Working => LauncherComponentTheme.CyanAccent,
            LauncherStatusSeverity.Warning => LauncherComponentTheme.OrangeAccent,
            LauncherStatusSeverity.Error => LauncherComponentTheme.OrangeHot,
            _ => LauncherComponentTheme.TextSecondary,
        };
}
