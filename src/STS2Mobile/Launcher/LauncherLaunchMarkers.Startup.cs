using System;
using System.IO;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLaunchMarkers
{
    internal static bool PreviousGameLaunchIncomplete(out string phase)
    {
        return TryReadStartupPhaseQuietly(out phase);
    }

    internal static void WriteStartupPhase(string phase)
    {
        try
        {
            File.WriteAllText(
                StartupMarkerPath,
                $"{DateTime.UtcNow:O}\n{phase}\n"
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to write startup marker: {ex.Message}");
        }
    }

    internal static string ReadStartupPhase()
    {
        TryReadStartupPhaseWithLogging(out var phase);
        return phase;
    }

    internal static void ClearStartupMarker()
    {
        try
        {
            if (File.Exists(StartupMarkerPath))
                File.Delete(StartupMarkerPath);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to clear startup marker: {ex.Message}");
        }
    }

    private static bool TryReadStartupPhaseQuietly(out string phase)
    {
        try
        {
            return TryReadStartupPhaseFile(out phase);
        }
        catch
        {
            phase = null;
            return false;
        }
    }

    private static bool TryReadStartupPhaseWithLogging(out string phase)
    {
        try
        {
            return TryReadStartupPhaseFile(out phase);
        }
        catch (Exception ex)
        {
            phase = null;
            PatchHelper.Log($"Failed to read startup marker: {ex.Message}");
            return false;
        }
    }

    private static bool TryReadStartupPhaseFile(out string phase)
    {
        phase = null;
        if (!File.Exists(StartupMarkerPath))
            return false;

        var lines = File.ReadAllLines(StartupMarkerPath);
        phase = lines.Length >= 2 ? lines[1].Trim() : null;
        return true;
    }
}
