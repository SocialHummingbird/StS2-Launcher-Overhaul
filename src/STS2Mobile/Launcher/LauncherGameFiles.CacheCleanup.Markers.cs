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
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), "UTC:");

    internal static bool CacheCleanupMarkerUtcParseable(string dataDir)
        => MarkerUtcParseable(CacheCleanupMarkerPath(dataDir));

    internal static string CacheCleanupMarkerSelectedBranch(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), "Selected branch:");

    internal static string CacheCleanupMarkerSelectedVersion(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), "Selected version:");

    internal static string CacheCleanupMarkerVersionSlotKind(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), "Selected version slot kind:");

    internal static string CacheCleanupMarkerVersionSlotDirectory(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), "Selected version slot directory:");

    internal static string CacheCleanupMarkerGameVersionsDirectoryPresent(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), "Game versions directory present:");

    internal static string CacheCleanupMarkerRuntimePacksDirectoryPresent(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), "Runtime packs directory present:");

    internal static string CacheCleanupMarkerSelectedRuntimePackDirectory(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), "Selected runtime pack directory:");

    internal static string CacheCleanupMarkerSelectedRuntimePackPresentBeforeCleanup(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), "Selected runtime pack present before cleanup:");

    internal static string CacheCleanupMarkerRemovedCount(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), "Removed count:");

    internal static string CacheCleanupMarkerRemovedRuntimePackCount(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), "Removed runtime pack count:");

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

        return MarkerHasLine(markerPath, "Preserved selected cache:");
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

        return MarkerHasLine(markerPath, "Preserved selected runtime pack:");
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
            $"UTC: {System.DateTime.UtcNow:O}",
            $"Selected branch: {selectedBranch}",
            $"Selected version: {SteamGameBranch.DisplayName(selectedBranch)}",
            $"Selected version slot kind: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}",
            $"Selected version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}",
            $"Runtime packs directory present: {Directory.Exists(runtimePacksDir).ToString().ToLowerInvariant()}",
            $"Selected runtime pack directory: {selectedRuntimePackDirectory}",
            $"Selected runtime pack present before cleanup: {Directory.Exists(selectedRuntimePackDirectory).ToString().ToLowerInvariant()}",
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
