using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    internal static int DeleteInactiveVersionCaches(string dataDir, string selectedBranch)
        => DeleteInactiveVersionCaches(dataDir, selectedBranch, out _);

    internal static int DeleteInactiveVersionCaches(string dataDir, string selectedBranch, out int removedRuntimePacks)
    {
        selectedBranch = SteamGameBranch.Normalize(selectedBranch);
        var markerLines = NewCacheCleanupMarkerLines(dataDir, selectedBranch);
        var versionsDir = Path.Combine(dataDir, LauncherStorageNames.GameVersionsDirectory);
        if (!Directory.Exists(versionsDir))
        {
            var removedRuntimePacksWithoutVersions = DeleteInactiveRuntimePacks(dataDir, selectedBranch, markerLines);
            markerLines.Add("Game versions directory present: false");
            markerLines.Add("Removed count: 0");
            markerLines.Add($"Removed runtime pack count: {removedRuntimePacksWithoutVersions}");
            WriteCacheCleanupMarker(dataDir, markerLines);
            removedRuntimePacks = removedRuntimePacksWithoutVersions;
            PatchHelper.Log($"[Launcher] Removed 0 inactive game version cache(s) and {removedRuntimePacks} runtime pack cache(s); selected branch '{selectedBranch}' preserved");
            return 0;
        }

        var removed = 0;
        removedRuntimePacks = 0;
        foreach (var cache in LauncherGameVersionCache.Enumerate(dataDir, selectedBranch))
        {
            if (!cache.Selected || string.Equals(selectedBranch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase))
            {
                PatchHelper.Log($"[Launcher] Removing inactive game version cache: {cache.DirectoryName} -> {cache.Path}");
                markerLines.Add($"Removed cache: {cache.DirectoryName} -> {cache.Path}");
                DeleteDirectory(cache.Path);
                var runtimePackDirectory = RuntimePackDirectoryPathForStateDirectory(dataDir, cache.DirectoryName);
                var runtimePackExisted = Directory.Exists(runtimePackDirectory);
                if (runtimePackExisted)
                {
                    DeleteDirectory(runtimePackDirectory);
                    removedRuntimePacks++;
                }
                markerLines.Add($"Removed runtime pack: {cache.DirectoryName} -> {runtimePackDirectory} existed={runtimePackExisted.ToString().ToLowerInvariant()} existsAfterDelete={Directory.Exists(runtimePackDirectory).ToString().ToLowerInvariant()}");
                removed++;
                continue;
            }

            PatchHelper.Log($"[Launcher] Preserving selected game version cache: {cache.DirectoryName} -> {cache.Path}");
            markerLines.Add($"Preserved selected cache: {cache.DirectoryName} -> {cache.Path}");
        }

        removedRuntimePacks += DeleteInactiveRuntimePacks(dataDir, selectedBranch, markerLines);
        markerLines.Add("Game versions directory present: true");
        markerLines.Add($"Removed count: {removed}");
        markerLines.Add($"Removed runtime pack count: {removedRuntimePacks}");
        WriteCacheCleanupMarker(dataDir, markerLines);
        PatchHelper.Log($"[Launcher] Removed {removed} inactive game version cache(s) and {removedRuntimePacks} runtime pack cache(s); selected branch '{selectedBranch}' preserved");
        return removed;
    }
}
