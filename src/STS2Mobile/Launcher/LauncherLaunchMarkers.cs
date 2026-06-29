using System.IO;
using System.Diagnostics;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLaunchMarkers
{
    private static string StartupMarkerPath =>
        Path.Combine(OS.GetDataDir(), LauncherStorageNames.StartupMarker);

    private static string StartupContextPath =>
        Path.Combine(OS.GetDataDir(), LauncherStorageNames.StartupContext);

    private static string StartupTimelinePath =>
        Path.Combine(OS.GetDataDir(), LauncherStorageNames.StartupTimeline);

    private static string ManualSafeLaunchPath =>
        Path.Combine(OS.GetDataDir(), LauncherStorageNames.ManualSafeLaunch);

    private static readonly Stopwatch ProcessTimer = Stopwatch.StartNew();
    private static int _phaseSequence;

    private static bool TryWriteMarker(string path, string content, string failureMessage)
    {
        try
        {
            File.WriteAllText(path, content);
            return true;
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"{failureMessage}: {ex.Message}");
            return false;
        }
    }

    private static bool TryAppendMarker(string path, string content, string failureMessage)
    {
        try
        {
            File.AppendAllText(path, content);
            return true;
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"{failureMessage}: {ex.Message}");
            return false;
        }
    }

    private static bool TryDeleteMarker(string path, string failureMessage, out bool existed)
    {
        existed = false;
        try
        {
            if (!File.Exists(path))
                return true;

            File.Delete(path);
            existed = true;
            return true;
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"{failureMessage}: {ex.Message}");
            return false;
        }
    }
}
