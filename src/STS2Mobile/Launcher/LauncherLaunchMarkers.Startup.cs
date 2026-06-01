using System;
using System.IO;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLaunchMarkers
{
    internal static string? ReadPreviousLaunchPhase()
        => ReadStartupPhaseQuietly();

    internal static void WriteStartupPhase(string phase)
        => TryWriteMarker(
            StartupMarkerPath,
            $"{DateTime.UtcNow:O}\n{phase}\n",
            "Failed to write startup marker"
        );

    internal static string? ReadStartupPhase()
    {
        return ReadStartupPhaseWithLogging();
    }

    internal static void ClearStartupMarker()
        => TryDeleteMarker(
            StartupMarkerPath,
            "Failed to clear startup marker",
            out _
        );

    private static string? ReadStartupPhaseQuietly()
        => ReadStartupPhase(failureMessage: null);

    private static string? ReadStartupPhaseWithLogging()
        => ReadStartupPhase("Failed to read startup marker");

    private static string? ReadStartupPhase(string? failureMessage)
    {
        try
        {
            return ReadStartupPhaseFile();
        }
        catch (Exception ex)
        {
            if (failureMessage != null)
                PatchHelper.Log($"{failureMessage}: {ex.Message}");
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
