using System;
using System.IO;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLaunchMarkers
{
    internal static string? ReadPreviousLaunchPhase()
        => ReadStartupPhase(logFailures: false);

    internal static void WriteStartupPhase(string phase)
        => TryWriteMarker(
            StartupMarkerPath,
            $"{DateTime.UtcNow:O}\n{phase}\n",
            "Failed to write startup marker"
        );

    internal static string? ReadStartupPhase()
        => ReadStartupPhase(logFailures: true);

    internal static void ClearStartupMarker()
        => TryDeleteMarker(
            StartupMarkerPath,
            "Failed to clear startup marker",
            out _
        );

    private static string? ReadStartupPhase(bool logFailures)
    {
        try
        {
            return ReadStartupPhaseFile();
        }
        catch (Exception ex)
        {
            if (logFailures)
                PatchHelper.Log($"Failed to read startup marker: {ex.Message}");
            return null;
        }
    }

    private static string? ReadStartupPhaseFile()
    {
        if (!File.Exists(StartupMarkerPath))
            return null;

        var lines = File.ReadAllLines(StartupMarkerPath);
        var phase = lines.Length >= 2 ? lines[1].Trim() : null;
        return string.IsNullOrWhiteSpace(phase) ? null : phase;
    }
}
