using System.IO;
using Godot;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLaunchMarkers
{
    private static string StartupMarkerPath =>
        Path.Combine(OS.GetDataDir(), LauncherStorageNames.StartupMarker);

    private static string ManualSafeLaunchPath =>
        Path.Combine(OS.GetDataDir(), LauncherStorageNames.ManualSafeLaunch);
}
