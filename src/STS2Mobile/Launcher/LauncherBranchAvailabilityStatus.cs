using System;
using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchAvailabilityStatus
{
    internal static string CompactFailureMessage(string dataDir, string message)
    {
        var diagnosis = ReadDiagnosis(dataDir);
        if (string.IsNullOrWhiteSpace(diagnosis))
            return message;

        var compactMessage = RemoveRawBranchAvailabilitySummary(message);
        return string.IsNullOrWhiteSpace(compactMessage)
            ? diagnosis
            : $"{compactMessage} {diagnosis}";
    }

    internal static void Clear(string dataDir)
    {
        if (string.IsNullOrWhiteSpace(dataDir))
            return;

        var markerPath = SteamGameInstallPaths.BranchAvailabilityMarkerPath(dataDir);
        try
        {
            if (File.Exists(markerPath))
                File.Delete(markerPath);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to clear Steam branch availability marker: {ex.Message}");
        }
    }
}
