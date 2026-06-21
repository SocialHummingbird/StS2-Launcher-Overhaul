using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    internal const string CacheCleanupMarkerFileName = "last_game_version_cache_cleanup.txt";

    internal static string CacheCleanupMarkerPath(string dataDir)
        => Path.Combine(dataDir, CacheCleanupMarkerFileName);

    internal static string CacheCleanupMarkerUtc(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), CacheCleanupMarkerUtcPrefix);

    internal static bool CacheCleanupMarkerUtcParseable(string dataDir)
        => MarkerUtcParseable(CacheCleanupMarkerPath(dataDir));

    internal static string CacheCleanupMarkerSelectedBranch(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), CacheCleanupMarkerSelectedBranchPrefix);

    internal static string CacheCleanupMarkerSelectedVersion(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), CacheCleanupMarkerSelectedVersionPrefix);

    internal static string CacheCleanupMarkerVersionSlotKind(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), CacheCleanupMarkerVersionSlotKindPrefix);

    internal static string CacheCleanupMarkerVersionSlotDirectory(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), CacheCleanupMarkerVersionSlotDirectoryPrefix);

    internal static string CacheCleanupMarkerGameVersionsDirectoryPresent(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), CacheCleanupMarkerGameVersionsDirectoryPresentPrefix);

    internal static string CacheCleanupMarkerRuntimePacksDirectoryPresent(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), CacheCleanupMarkerRuntimePacksDirectoryPresentPrefix);

    internal static string CacheCleanupMarkerSelectedRuntimePackDirectory(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), CacheCleanupMarkerSelectedRuntimePackDirectoryPrefix);

    internal static string CacheCleanupMarkerSelectedRuntimePackPresentBeforeCleanup(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), CacheCleanupMarkerSelectedRuntimePackPresentBeforeCleanupPrefix);

    internal static string CacheCleanupMarkerRemovedCount(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), CacheCleanupMarkerRemovedCountPrefix);

    internal static string CacheCleanupMarkerRemovedRuntimePackCount(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), CacheCleanupMarkerRemovedRuntimePackCountPrefix);

    internal static bool CacheCleanupMarkerSelectedCachePreservedWhereApplicable(string dataDir)
    {
        var markerPath = CacheCleanupMarkerPath(dataDir);
        var selectedBranch = CacheCleanupMarkerSelectedBranch(dataDir);
        if (string.IsNullOrWhiteSpace(selectedBranch) || selectedBranch.StartsWith("<", System.StringComparison.Ordinal))
            return false;

        selectedBranch = SteamGameBranch.Normalize(selectedBranch);
        if (string.Equals(selectedBranch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.Equals(CacheCleanupMarkerGameVersionsDirectoryPresent(dataDir), "true", System.StringComparison.OrdinalIgnoreCase))
            return true;

        return MarkerHasLine(markerPath, CacheCleanupMarkerPreservedSelectedCachePrefix);
    }

    internal static bool CacheCleanupMarkerSelectedRuntimePackPreservedWhereApplicable(string dataDir)
    {
        var markerPath = CacheCleanupMarkerPath(dataDir);
        var selectedBranch = CacheCleanupMarkerSelectedBranch(dataDir);
        if (string.IsNullOrWhiteSpace(selectedBranch) || selectedBranch.StartsWith("<", System.StringComparison.Ordinal))
            return false;

        selectedBranch = SteamGameBranch.Normalize(selectedBranch);
        if (string.Equals(selectedBranch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.Equals(CacheCleanupMarkerSelectedRuntimePackPresentBeforeCleanup(dataDir), "true", System.StringComparison.OrdinalIgnoreCase))
            return true;

        return MarkerHasLine(markerPath, CacheCleanupMarkerPreservedSelectedRuntimePackPrefix);
    }

    private static System.Collections.Generic.List<string> NewCacheCleanupMarkerLines(string dataDir, string selectedBranch)
    {
        var runtimePacksDir = Path.Combine(dataDir, "runtime_packs");
        var selectedRuntimePackDirectory = RuntimePackDirectoryPathForStateDirectory(
            dataDir,
            SteamGameBranch.StateDirectoryName(selectedBranch)
        );

        return new()
        {
            $"{CacheCleanupMarkerUtcPrefix} {System.DateTime.UtcNow:O}",
            $"{CacheCleanupMarkerSelectedBranchPrefix} {selectedBranch}",
            $"{CacheCleanupMarkerSelectedVersionPrefix} {SteamGameBranch.DisplayName(selectedBranch)}",
            $"{CacheCleanupMarkerVersionSlotKindPrefix} {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}",
            $"{CacheCleanupMarkerVersionSlotDirectoryPrefix} {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}",
            $"{CacheCleanupMarkerRuntimePacksDirectoryPresentPrefix} {Directory.Exists(runtimePacksDir).ToString().ToLowerInvariant()}",
            $"{CacheCleanupMarkerSelectedRuntimePackDirectoryPrefix} {selectedRuntimePackDirectory}",
            $"{CacheCleanupMarkerSelectedRuntimePackPresentBeforeCleanupPrefix} {Directory.Exists(selectedRuntimePackDirectory).ToString().ToLowerInvariant()}",
        };
    }

    private static void WriteCacheCleanupMarker(string dataDir, System.Collections.Generic.IEnumerable<string> lines)
    {
        try
        {
            File.WriteAllLines(CacheCleanupMarkerPath(dataDir), lines);
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write game version cache cleanup marker: {ex.Message}");
        }
    }
}
