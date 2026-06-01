using System;
using System.IO;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class LauncherLaunchMarkers
{
    internal static string StartupMarkerPath =>
        Path.Combine(OS.GetDataDir(), LauncherStorageNames.StartupMarker);

    internal static string ManualSafeLaunchPath =>
        Path.Combine(OS.GetDataDir(), LauncherStorageNames.ManualSafeLaunch);

    internal static bool PreviousGameLaunchIncomplete(out string phase)
    {
        phase = null;
        try
        {
            if (!File.Exists(StartupMarkerPath))
                return false;

            var lines = File.ReadAllLines(StartupMarkerPath);
            phase = lines.Length >= 2 ? lines[1].Trim() : null;
            return true;
        }
        catch
        {
            return false;
        }
    }

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
