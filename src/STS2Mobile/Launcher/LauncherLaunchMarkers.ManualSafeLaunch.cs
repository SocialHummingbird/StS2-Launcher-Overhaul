using System;
using System.IO;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLaunchMarkers
{
    internal static void SaveManualSafeLaunchMarker()
    {
        try
        {
            File.WriteAllText(ManualSafeLaunchPath, $"{DateTime.UtcNow:O}\n");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write manual safe launch marker: {ex.Message}");
        }
    }

    internal static bool ConsumeManualSafeLaunchMarker()
    {
        try
        {
            if (!File.Exists(ManualSafeLaunchPath))
                return false;

            File.Delete(ManualSafeLaunchPath);
            PatchHelper.Log("Manual safe launch marker consumed");
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to consume manual safe launch marker: {ex.Message}");
            return true;
        }
    }
}
