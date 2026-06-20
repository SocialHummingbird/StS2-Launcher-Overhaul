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
        sb.AppendLine($"Game version redownload marker filename: {LauncherGameFiles.RedownloadMarkerFileName}");
        sb.AppendLine($"Game version redownload marker path: {LauncherGameFiles.RedownloadMarkerPath(dataDir)}");
        sb.AppendLine($"Game version redownload marker present: {BoolText(File.Exists(LauncherGameFiles.RedownloadMarkerPath(dataDir)))}");
        sb.AppendLine($"Game version redownload marker UTC: {LauncherGameFiles.RedownloadMarkerUtc(dataDir)}");
        sb.AppendLine($"Game version redownload marker UTC parseable: {BoolText(LauncherGameFiles.RedownloadMarkerUtcParseable(dataDir))}");
        sb.AppendLine($"Game version redownload marker selected branch: {LauncherGameFiles.RedownloadMarkerSelectedBranch(dataDir)}");
        sb.AppendLine($"Game version redownload marker matches selected branch: {BoolText(MarkerBranchMatchesSelected(LauncherGameFiles.RedownloadMarkerSelectedBranch(dataDir), selectedBranch))}");
        sb.AppendLine($"Game version redownload marker selected version: {LauncherGameFiles.RedownloadMarkerSelectedVersion(dataDir)}");
        sb.AppendLine($"Game version redownload marker selected version slot kind: {LauncherGameFiles.RedownloadMarkerVersionSlotKind(dataDir)}");
        sb.AppendLine($"Game version redownload marker selected version slot directory: {LauncherGameFiles.RedownloadMarkerVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Game version redownload marker game directory: {LauncherGameFiles.RedownloadMarkerGameDirectory(dataDir)}");
        sb.AppendLine($"Game version redownload marker game directory existed before delete: {LauncherGameFiles.RedownloadMarkerGameDirectoryExisted(dataDir)}");
        sb.AppendLine($"Game version redownload marker game directory exists after delete: {LauncherGameFiles.RedownloadMarkerGameDirectoryExistsAfterDelete(dataDir)}");
        sb.AppendLine($"Game version redownload marker download state directory: {LauncherGameFiles.RedownloadMarkerDownloadStateDirectory(dataDir)}");
        sb.AppendLine($"Game version redownload marker download state directory existed before delete: {LauncherGameFiles.RedownloadMarkerDownloadStateDirectoryExisted(dataDir)}");
        sb.AppendLine($"Game version redownload marker download state directory exists after delete: {LauncherGameFiles.RedownloadMarkerDownloadStateDirectoryExistsAfterDelete(dataDir)}");
        sb.AppendLine($"Game version redownload marker selected directories cleared: {BoolText(LauncherGameFiles.RedownloadMarkerSelectedDirectoriesCleared(dataDir))}");
        sb.AppendLine($"Game version cache cleanup marker filename: {LauncherGameFiles.CacheCleanupMarkerFileName}");
        sb.AppendLine($"Game version cache cleanup marker path: {LauncherGameFiles.CacheCleanupMarkerPath(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker present: {BoolText(File.Exists(LauncherGameFiles.CacheCleanupMarkerPath(dataDir)))}");
        sb.AppendLine($"Game version cache cleanup marker UTC: {LauncherGameFiles.CacheCleanupMarkerUtc(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker UTC parseable: {BoolText(LauncherGameFiles.CacheCleanupMarkerUtcParseable(dataDir))}");
        sb.AppendLine($"Game version cache cleanup marker selected branch: {LauncherGameFiles.CacheCleanupMarkerSelectedBranch(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker matches selected branch: {BoolText(MarkerBranchMatchesSelected(LauncherGameFiles.CacheCleanupMarkerSelectedBranch(dataDir), selectedBranch))}");
        sb.AppendLine($"Game version cache cleanup marker selected version: {LauncherGameFiles.CacheCleanupMarkerSelectedVersion(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker selected version slot kind: {LauncherGameFiles.CacheCleanupMarkerVersionSlotKind(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker selected version slot directory: {LauncherGameFiles.CacheCleanupMarkerVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker game_versions present: {LauncherGameFiles.CacheCleanupMarkerGameVersionsDirectoryPresent(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker runtime_packs present: {LauncherGameFiles.CacheCleanupMarkerRuntimePacksDirectoryPresent(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker selected runtime pack directory: {LauncherGameFiles.CacheCleanupMarkerSelectedRuntimePackDirectory(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker selected runtime pack present before cleanup: {LauncherGameFiles.CacheCleanupMarkerSelectedRuntimePackPresentBeforeCleanup(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker removed count: {LauncherGameFiles.CacheCleanupMarkerRemovedCount(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker removed runtime pack count: {LauncherGameFiles.CacheCleanupMarkerRemovedRuntimePackCount(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker selected cache preserved where applicable: {BoolText(LauncherGameFiles.CacheCleanupMarkerSelectedCachePreservedWhereApplicable(dataDir))}");
        sb.AppendLine($"Game version cache cleanup marker selected runtime pack preserved where applicable: {BoolText(LauncherGameFiles.CacheCleanupMarkerSelectedRuntimePackPreservedWhereApplicable(dataDir))}");
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
