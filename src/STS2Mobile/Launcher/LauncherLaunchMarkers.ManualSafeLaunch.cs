using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLaunchMarkers
{
    internal static void SaveManualSafeLaunchMarker()
        => TryWriteMarker(
            ManualSafeLaunchPath,
            $"{DateTime.UtcNow:O}\n",
            "[Launcher] Failed to write manual safe launch marker"
        );

    internal static bool ConsumeManualSafeLaunchMarker()
    {
        if (!TryDeleteMarker(
            ManualSafeLaunchPath,
            "Failed to consume manual safe launch marker",
            out var existed
        ))
            return true;

        if (!existed)
            return false;

        PatchHelper.Log("Manual safe launch marker consumed");
        return true;
    }
}
