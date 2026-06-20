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
                    $"UTC: {DateTime.UtcNow:O}\n"
                    + $"Selected branch: {SelectedBranch}\n"
                    + $"Selected branch visibility: {SelectedBranchAvailability.VisibilityText}\n"
                    + $"Windows depot manifests for selected branch: {SelectedBranchAvailability.WindowsManifestDepotCount}\n"
                    + $"Visible branch count: {_branches.Count}\n";

                foreach (var branch in _branches.Take(MaxBranchAvailabilityMarkerBranches))
                    text += $"Visible branch: {branch.Summary()}\n";

                if (_branches.Count > MaxBranchAvailabilityMarkerBranches)
                    text += $"Visible branch overflow count: {_branches.Count - MaxBranchAvailabilityMarkerBranches}\n";

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
