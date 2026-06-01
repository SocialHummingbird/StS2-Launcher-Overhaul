using System;
using System.IO;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLaunchMarkers
{
    internal static bool PreviousGameLaunchIncomplete(out string phase)
    {
        return TryReadStartupPhase(logFailure: false, out phase);
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
        TryReadStartupPhase(logFailure: true, out var phase);
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

    private static bool TryReadStartupPhase(bool logFailure, out string phase)
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
        catch (Exception ex)
        {
            if (logFailure)
                PatchHelper.Log($"Failed to read startup marker: {ex.Message}");

            return false;
        }
    }
}
