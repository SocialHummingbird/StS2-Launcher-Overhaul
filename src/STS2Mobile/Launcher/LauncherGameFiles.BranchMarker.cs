using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    internal static bool HasBranchMetadataProblem(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        return IsValidPck(PckPath(dataDir, branch))
            && !BranchMarkerReady(dataDir, branch);
    }

    internal static bool BranchMarkerReady(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        var markerPath = SteamGameInstallPaths.BranchMarkerPath(dataDir, branch);
        if (!File.Exists(markerPath))
            return string.Equals(branch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase);

        try
        {
            foreach (var line in File.ReadLines(markerPath))
            {
                if (!line.StartsWith(BranchMarkerBranchPrefix, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var markerBranch = SteamGameBranch.Normalize(line.Substring(BranchMarkerBranchPrefix.Length));
                if (!string.Equals(markerBranch, branch, System.StringComparison.OrdinalIgnoreCase))
                    return false;

                return string.Equals(branch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase)
                    || (
                        BranchMarkerHasDepotManifestProvenance(markerPath)
                        && BranchMarkerHasInstallSlotProvenance(markerPath, dataDir, branch)
                        && BranchMarkerHasIntegrityProvenance(markerPath)
                    );
            }
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to read Steam branch marker: {ex.Message}");
        }

        return false;
    }
}
