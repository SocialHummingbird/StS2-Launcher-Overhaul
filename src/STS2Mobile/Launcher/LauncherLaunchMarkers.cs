using System.IO;
using System.Diagnostics;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLaunchMarkers
{
    private static string StartupMarkerPath =>
        MarkerPath(LauncherStorageNames.StartupMarker);

    private static string StartupContextPath =>
        MarkerPath(LauncherStorageNames.StartupContext);

    private static string StartupTimelinePath =>
        MarkerPath(LauncherStorageNames.StartupTimeline);

    private static string ManualSafeLaunchPath =>
        MarkerPath(LauncherStorageNames.ManualSafeLaunch);

    private static readonly Stopwatch ProcessTimer = Stopwatch.StartNew();
    private static int _phaseSequence;

    internal static long ElapsedMilliseconds
        => ProcessTimer.ElapsedMilliseconds;

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

    private static string MarkerPath(string fileName)
    {
        try
        {
            var dataDir = AppPaths.AppPrivateDataDir;
            return string.IsNullOrWhiteSpace(dataDir)
                ? fileName
                : Path.Combine(dataDir, fileName);
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"Failed to resolve launcher marker path for {fileName}: {ex.Message}");
            return fileName;
        }
    }
}
