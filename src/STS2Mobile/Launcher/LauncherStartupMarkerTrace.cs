using System;
using System.IO;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class LauncherStartupMarkerTrace
{
    internal static void Write(string phase)
    {
        try
        {
            File.WriteAllText(
                LauncherLaunchMarkers.StartupMarkerPath,
                $"{DateTime.UtcNow:O}\n{phase}\n"
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to write startup marker: {ex.Message}");
        }
    }

    internal static string ReadPhase()
    {
        try
        {
            if (!File.Exists(LauncherLaunchMarkers.StartupMarkerPath))
                return null;

            var lines = File.ReadAllLines(LauncherLaunchMarkers.StartupMarkerPath);
            return lines.Length >= 2 ? lines[1].Trim() : null;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to read startup marker: {ex.Message}");
            return null;
        }
    }

    internal static void Clear()
    {
        try
        {
            if (File.Exists(LauncherLaunchMarkers.StartupMarkerPath))
                File.Delete(LauncherLaunchMarkers.StartupMarkerPath);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to clear startup marker: {ex.Message}");
        }
    }
}
