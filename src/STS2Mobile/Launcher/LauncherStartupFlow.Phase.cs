using Godot;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private static void SetStartupPhase(StartupContext startup, string phase, string status)
    {
        LauncherLaunchMarkers.WriteStartupPhase(phase);
        LauncherStartupStatus.Set(startup.Status, status);
    }
}
