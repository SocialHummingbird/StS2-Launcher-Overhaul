using System.IO;
using System.Text;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendCachedGameVersions(StringBuilder sb, string dataDir)
    {
        var selectedBranch = SteamGameBranch.Normalize(LauncherPreferences.ReadGameBranch());
        var versionsDir = Path.Combine(dataDir, LauncherStorageNames.GameVersionsDirectory);
        sb.AppendLine($"Current selected branch for version marker comparison: {selectedBranch}");
        if (!Directory.Exists(versionsDir))
        {
            sb.AppendLine("Cached non-public game versions: 0");
            return;
        }

        var caches = LauncherGameVersionCache.Enumerate(dataDir, selectedBranch);
        sb.AppendLine($"Cached non-public game versions: {caches.Count}");
        foreach (var cache in caches)
        {
            var selected = cache.Selected ? "true" : "false";
            var inactive = !cache.Selected
                || string.Equals(selectedBranch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase);
            var markerPath = Path.Combine(
                cache.Path,
                SteamGameInstallPaths.LegacyPublicGameDirectory,
                SteamGameInstallPaths.BranchMarkerFileName
            );
            var markerBranch = ReadBranchMarkerBranch(markerPath);
            sb.AppendLine(
                $"Cached game version dir: {cache.DirectoryName} "
                    + $"selected={selected} "
                    + $"inactive={BoolText(inactive)} "
                    + $"branchMarkerPresent={BoolText(File.Exists(markerPath))} "
                    + $"branchMarkerBranch={markerBranch} "
                    + $"branchMarkerExpectedInstallSlotKind={SteamGameInstallPaths.VersionSlotKind(markerBranch)} "
                    + $"branchMarkerExpectedInstallSlotDirectory={cache.Path} "
                    + $"branchMarkerMatchingInstallSlotProvenance={BoolText(BranchMarkerHasInstallSlotProvenance(markerPath, SteamGameInstallPaths.VersionSlotKind(markerBranch), cache.Path))} "
                    + $"branchMarkerDepotManifests={BranchMarkerDepotManifestCount(markerPath)} "
                    + $"branchMarkerIntegrityProvenance={BoolText(BranchMarkerHasIntegrityProvenance(markerPath))} "
                    + $"branchMarkerDepotsMatchingPublic={ReadBranchMarkerValue(markerPath, LauncherBranchMarkerFields.DepotsMatchingPublic)} "
                    + $"branchMarkerDepotsDifferingFromPublic={ReadBranchMarkerValue(markerPath, LauncherBranchMarkerFields.DepotsDifferingFromPublic)} "
                    + $"branchMarkerDepotsInheritedFromPublic={ReadBranchMarkerValue(markerPath, LauncherBranchMarkerFields.DepotsInheritedFromPublic)} "
                    + $"branchMarkerDepotsMissingSelectedManifest={ReadBranchMarkerValue(markerPath, LauncherBranchMarkerFields.DepotsMissingSelectedManifest)} "
                    + $"branchMarkerReady={BoolText(CachedBranchMarkerReady(cache.DirectoryName, markerBranch, markerPath, cache.Path))}"
            );
        }
    }
}
