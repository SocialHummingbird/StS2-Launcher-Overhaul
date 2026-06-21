using System;
using System.IO;
using System.Linq;
using STS2Mobile.Patches;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private sealed partial class BranchAvailabilityReport
    {
        internal void WriteMarker(string dataDir)
        {
            var path = SteamGameInstallPaths.BranchAvailabilityMarkerPath(dataDir);
            try
            {
                var text =
                    $"{SteamBranchAvailabilityMarkerFields.Utc} {DateTime.UtcNow:O}\n"
                    + $"{SteamBranchAvailabilityMarkerFields.SelectedBranch} {SelectedBranch}\n"
                    + $"{SteamBranchAvailabilityMarkerFields.SelectedBranchVisibility} {SelectedBranchAvailability.VisibilityText}\n"
                    + $"{SteamBranchAvailabilityMarkerFields.SelectedBranchWindowsDepotManifests} {SelectedBranchAvailability.WindowsManifestDepotCount}\n"
                    + $"{SteamBranchAvailabilityMarkerFields.VisibleBranchCount} {_branches.Count}\n";

                foreach (var branch in _branches.Take(MaxBranchAvailabilityMarkerBranches))
                    text += $"{SteamBranchAvailabilityMarkerFields.VisibleBranch} {branch.Summary()}\n";

                if (_branches.Count > MaxBranchAvailabilityMarkerBranches)
                    text += $"{SteamBranchAvailabilityMarkerFields.VisibleBranchOverflowCount} {_branches.Count - MaxBranchAvailabilityMarkerBranches}\n";

                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? dataDir);
                File.WriteAllText(path, text);
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Depot] Failed to write Steam branch availability marker: {ex.Message}");
            }
        }
    }
}
