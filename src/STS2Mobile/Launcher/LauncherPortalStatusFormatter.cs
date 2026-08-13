namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalStatusFormatter
{
    internal static string LabelFor(LauncherStatusSeverity severity)
        => severity switch
        {
            LauncherStatusSeverity.Ready => "Ready",
            LauncherStatusSeverity.Working => "Working",
            LauncherStatusSeverity.Warning => "Warning",
            LauncherStatusSeverity.Error => "Error",
            _ => "Information",
        };
}
