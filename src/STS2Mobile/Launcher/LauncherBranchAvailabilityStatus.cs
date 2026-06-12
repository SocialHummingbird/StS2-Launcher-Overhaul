using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherBranchAvailabilityStatus
{
    private const int MaxVisibleBranchesInStatus = 4;

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

    private static string ReadDiagnosis(string dataDir)
    {
        if (string.IsNullOrWhiteSpace(dataDir))
            return null;

        var markerPath = SteamGameInstallPaths.BranchAvailabilityMarkerPath(dataDir);
        if (!File.Exists(markerPath))
            return null;

        try
        {
            var lines = File.ReadAllLines(markerPath);
            var selectedBranch = ReadValue(lines, "Selected branch:");
            if (!MarkerBranchMatchesCurrentSelection(selectedBranch))
                return null;

            var visibility = ReadValue(lines, "Selected branch visibility:");
            var manifestCount = ReadValue(lines, "Windows depot manifests for selected branch:");
            var visibleBranches = ReadValues(lines, "Visible branch:")
                .Take(MaxVisibleBranchesInStatus)
                .Select(VisibleBranchStatus)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
            var overflow = ReadValue(lines, "Visible branch overflow count:");

            var selectedStatus = SelectedStatus(selectedBranch, visibility, manifestCount);
            var visibleStatus = visibleBranches.Length == 0
                ? "visible branches: none reported"
                : "visible branches: " + string.Join(", ", visibleBranches);

            if (!string.IsNullOrWhiteSpace(overflow) && overflow != "0")
                visibleStatus += $", +{overflow} more";

            return $"Branch availability: {selectedStatus}; {visibleStatus}.";
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to read Steam branch availability marker for status: {ex.Message}");
            return null;
        }
    }

    private static string SelectedStatus(string selectedBranch, string visibility, string manifestCount)
    {
        selectedBranch = string.IsNullOrWhiteSpace(selectedBranch) ? "selected branch" : selectedBranch.Trim();
        manifestCount = string.IsNullOrWhiteSpace(manifestCount) ? "0" : manifestCount.Trim();

        if (!int.TryParse(manifestCount, out var windowsManifests))
            windowsManifests = 0;

        if (windowsManifests > 0)
            return $"{selectedBranch} has {windowsManifests} Windows depot manifest(s)";

        if (!string.IsNullOrWhiteSpace(visibility)
            && visibility.IndexOf("not listed", StringComparison.OrdinalIgnoreCase) >= 0)
            return $"{selectedBranch} is not listed for this Steam account and has no Windows manifest";

        return $"{selectedBranch} is visible but has no Windows manifest";
    }

    private static bool MarkerBranchMatchesCurrentSelection(string selectedBranch)
    {
        if (string.IsNullOrWhiteSpace(selectedBranch))
            return false;

        return string.Equals(
            SteamGameBranch.Normalize(selectedBranch),
            SteamGameBranch.Normalize(LauncherPreferences.ReadGameBranch()),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static string VisibleBranchStatus(string markerValue)
    {
        if (string.IsNullOrWhiteSpace(markerValue))
            return null;

        var nameEnd = markerValue.IndexOf(" [", StringComparison.Ordinal);
        var name = nameEnd > 0 ? markerValue[..nameEnd] : markerValue;
        var downloadable = !markerValue.Contains("windowsManifestDepots=0", StringComparison.OrdinalIgnoreCase);
        return downloadable ? $"{name} (downloadable)" : $"{name} (no Windows manifest)";
    }

    private static string RemoveRawBranchAvailabilitySummary(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return message;

        var marker = " Selected branch visibility:";
        var markerIndex = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return markerIndex < 0
            ? message.Trim()
            : message[..markerIndex].TrimEnd();
    }

    private static string ReadValue(IEnumerable<string> lines, string prefix)
        => lines
            .Select(line => line.Trim())
            .Where(line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(line => line[prefix.Length..].Trim())
            .FirstOrDefault();

    private static IEnumerable<string> ReadValues(IEnumerable<string> lines, string prefix)
        => lines
            .Select(line => line.Trim())
            .Where(line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(line => line[prefix.Length..].Trim());
}
