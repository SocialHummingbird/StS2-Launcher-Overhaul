using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    private static bool BranchMarkerHasDepotManifestProvenance(string markerPath)
    {
        try
        {
            foreach (var line in File.ReadLines(markerPath))
            {
                if (line.StartsWith(BranchMarkerDepotManifestRowPrefix, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to read Steam branch marker provenance: {ex.Message}");
        }

        return false;
    }

    private static bool BranchMarkerHasIntegrityProvenance(string markerPath)
        => ReadMarkerInt(markerPath, BranchMarkerDepotsMatchingPublicPrefix).HasValue
        && ReadMarkerInt(markerPath, BranchMarkerDepotsDifferingFromPublicPrefix).HasValue
        && ReadMarkerInt(markerPath, BranchMarkerDepotsWithoutPublicComparisonPrefix).HasValue
        && ReadMarkerInt(markerPath, BranchMarkerDepotsInheritedFromPublicPrefix).HasValue
        && ReadMarkerInt(markerPath, BranchMarkerDepotsMissingSelectedManifestPrefix).HasValue;

    private static bool BranchMarkerHasInstallSlotProvenance(string markerPath, string dataDir, string branch)
        => string.Equals(
            ReadMarkerValue(markerPath, BranchMarkerInstallSlotKindPrefix),
            SteamGameInstallPaths.VersionSlotKind(branch),
            System.StringComparison.OrdinalIgnoreCase
        )
        && MarkerPathsEquivalent(
            ReadMarkerValue(markerPath, BranchMarkerInstallSlotDirectoryPrefix),
            SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch),
            dataDir
        );
}
