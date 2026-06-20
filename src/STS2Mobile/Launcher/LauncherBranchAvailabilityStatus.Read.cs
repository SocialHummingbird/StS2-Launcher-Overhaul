using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchAvailabilityStatus
{
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
            var selectedBranch = ReadValue(lines, SelectedBranchPrefix);
            if (!MarkerBranchMatchesCurrentSelection(selectedBranch))
                return null;

            var visibility = ReadValue(lines, SelectedBranchVisibilityPrefix);
            var manifestCount = ReadValue(lines, SelectedBranchWindowsDepotManifestsPrefix);
            var visibleBranches = ReadValues(lines, VisibleBranchPrefix)
                .Take(MaxVisibleBranchesInStatus)
                .Select(VisibleBranchStatus)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
            var selectedBranchMarker = ReadValues(lines, VisibleBranchPrefix)
                .FirstOrDefault(value => MarkerValueMatchesBranch(value, selectedBranch));
            var overflow = ReadValue(lines, VisibleBranchOverflowCountPrefix);

            var selectedStatus = SelectedStatus(selectedBranch, visibility, manifestCount, selectedBranchMarker);
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
