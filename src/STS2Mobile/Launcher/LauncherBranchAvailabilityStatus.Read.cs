using System;
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

        if (!SteamBranchAvailabilityMarkerFile.Exists(dataDir))
            return null;

        try
        {
            var lines = SteamBranchAvailabilityMarkerFile.ReadAllLines(dataDir);
            var selectedBranch = SteamBranchAvailabilityMarkerFile.ReadValue(
                lines,
                SteamBranchAvailabilityMarkerFields.SelectedBranch
            );
            if (!MarkerBranchMatchesCurrentSelection(selectedBranch))
                return null;

            var visibility = SteamBranchAvailabilityMarkerFile.ReadValue(
                lines,
                SteamBranchAvailabilityMarkerFields.SelectedBranchVisibility
            );
            var manifestCount = SteamBranchAvailabilityMarkerFile.ReadValue(
                lines,
                SteamBranchAvailabilityMarkerFields.SelectedBranchWindowsDepotManifests
            );
            var visibleRows = SteamBranchAvailabilityMarkerFile.ReadVisibleRows(lines).ToArray();
            var visibleBranches = visibleRows
                .Take(MaxVisibleBranchesInStatus)
                .Select(VisibleBranchStatus)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
            var selectedBranchMarker = visibleRows
                .FirstOrDefault(row => MarkerValueMatchesBranch(row, selectedBranch));
            var overflow = SteamBranchAvailabilityMarkerFile.ReadValue(
                lines,
                SteamBranchAvailabilityMarkerFields.VisibleBranchOverflowCount
            );

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
}
