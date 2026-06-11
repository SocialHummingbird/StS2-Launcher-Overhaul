using System.IO;
using Godot;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    internal static string PckPath(string dataDir) =>
        PckPath(dataDir, LauncherPreferences.ReadGameBranch());

    internal static string PckPath(string dataDir, string branch) =>
        Path.Combine(GameDirectoryPath(dataDir, branch), LauncherStorageNames.GamePck);

    internal static string GameDirectoryPath(string dataDir, string branch) =>
        SteamGameInstallPaths.GameDirectory(dataDir, branch);

    internal static bool Ready() => Ready(OS.GetDataDir());

    internal static bool Ready(string dataDir)
        => Ready(dataDir, LauncherPreferences.ReadGameBranch());

    internal static bool Ready(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        return IsValidPck(PckPath(dataDir, branch))
            && BranchMarkerReady(dataDir, branch);
    }

    internal static string ReadinessProblem(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        if (!IsValidPck(PckPath(dataDir, branch)))
            return "Selected game version is not downloaded or the downloaded PCK is invalid. Download selected version to continue.";

        if (HasBranchMetadataProblem(dataDir, branch))
            return "Selected game version has missing or mismatched branch metadata. Redownload selected version to rebuild the cache safely.";

        return null;
    }

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
                const string prefix = "Branch:";
                if (!line.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var markerBranch = SteamGameBranch.Normalize(line.Substring(prefix.Length));
                if (!string.Equals(markerBranch, branch, System.StringComparison.OrdinalIgnoreCase))
                    return false;

                return string.Equals(branch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase)
                    || (
                        BranchMarkerHasDepotManifestProvenance(markerPath)
                        && BranchMarkerHasInstallSlotProvenance(markerPath, dataDir, branch)
                    );
            }
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to read Steam branch marker: {ex.Message}");
        }

        return false;
    }

    private static bool BranchMarkerHasDepotManifestProvenance(string markerPath)
    {
        try
        {
            foreach (var line in File.ReadLines(markerPath))
            {
                if (line.StartsWith("Depot manifest:", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to read Steam branch marker provenance: {ex.Message}");
        }

        return false;
    }

    private static bool BranchMarkerHasInstallSlotProvenance(string markerPath, string dataDir, string branch)
        => string.Equals(
            ReadMarkerValue(markerPath, "Install slot kind:"),
            SteamGameInstallPaths.VersionSlotKind(branch),
            System.StringComparison.OrdinalIgnoreCase
        )
        && string.Equals(
            NormalizeMarkerPath(ReadMarkerValue(markerPath, "Install slot directory:")),
            NormalizeMarkerPath(SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch)),
            System.StringComparison.OrdinalIgnoreCase
        );

    private static string NormalizeMarkerPath(string path)
        => string.IsNullOrWhiteSpace(path) || path.StartsWith("<", System.StringComparison.Ordinal)
            ? string.Empty
            : path.Trim().Replace('\\', '/').TrimEnd('/');

    internal static void DeleteDownloadedState(string dataDir)
        => DeleteDownloadedState(dataDir, LauncherPreferences.ReadGameBranch());

    internal static void DeleteDownloadedState(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        var gameDirectory = GameDirectoryPath(dataDir, branch);
        var downloadStateDirectory = SteamGameInstallPaths.DownloadStateDirectoryPath(dataDir, branch);
        var gameDirectoryExisted = Directory.Exists(gameDirectory);
        var downloadStateDirectoryExisted = Directory.Exists(downloadStateDirectory);
        WriteRedownloadMarker(
            dataDir,
            branch,
            gameDirectory,
            gameDirectoryExisted,
            null,
            downloadStateDirectory,
            downloadStateDirectoryExisted,
            null
        );
        DeleteDirectory(gameDirectory);
        DeleteDirectory(downloadStateDirectory);
        WriteRedownloadMarker(
            dataDir,
            branch,
            gameDirectory,
            gameDirectoryExisted,
            Directory.Exists(gameDirectory),
            downloadStateDirectory,
            downloadStateDirectoryExisted,
            Directory.Exists(downloadStateDirectory)
        );
        LauncherLaunchMarkers.ClearStartupMarker();
        PatchHelper.Log($"[Launcher] Deleted downloaded game files and download state for branch '{branch}'");
    }

    internal const string RedownloadMarkerFileName = "last_game_version_redownload.txt";
    internal const string CacheCleanupMarkerFileName = "last_game_version_cache_cleanup.txt";

    internal static string RedownloadMarkerPath(string dataDir)
        => Path.Combine(dataDir, RedownloadMarkerFileName);

    internal static string RedownloadMarkerUtc(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), "UTC:");

    internal static bool RedownloadMarkerUtcParseable(string dataDir)
        => MarkerUtcParseable(RedownloadMarkerPath(dataDir));

    internal static string RedownloadMarkerSelectedBranch(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), "Selected branch:");

    internal static string RedownloadMarkerSelectedVersion(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), "Selected version:");

    internal static string RedownloadMarkerVersionSlotKind(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), "Selected version slot kind:");

    internal static string RedownloadMarkerVersionSlotDirectory(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), "Selected version slot directory:");

    internal static string RedownloadMarkerGameDirectory(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), "Deleted game directory:");

    internal static string RedownloadMarkerGameDirectoryExisted(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), "Game directory existed before delete:");

    internal static string RedownloadMarkerGameDirectoryExistsAfterDelete(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), "Game directory exists after delete:");

    internal static string RedownloadMarkerDownloadStateDirectory(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), "Deleted download state directory:");

    internal static string RedownloadMarkerDownloadStateDirectoryExisted(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), "Download state directory existed before delete:");

    internal static string RedownloadMarkerDownloadStateDirectoryExistsAfterDelete(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), "Download state directory exists after delete:");

    internal static bool RedownloadMarkerSelectedDirectoriesCleared(string dataDir)
        => string.Equals(
            RedownloadMarkerGameDirectoryExistsAfterDelete(dataDir),
            "false",
            System.StringComparison.OrdinalIgnoreCase
        )
        && string.Equals(
            RedownloadMarkerDownloadStateDirectoryExistsAfterDelete(dataDir),
            "false",
            System.StringComparison.OrdinalIgnoreCase
        );

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

    internal static string CacheCleanupMarkerRemovedCount(string dataDir)
        => ReadMarkerValue(CacheCleanupMarkerPath(dataDir), "Removed count:");

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

    internal static int DeleteInactiveVersionCaches(string dataDir, string selectedBranch)
    {
        selectedBranch = SteamGameBranch.Normalize(selectedBranch);
        var markerLines = NewCacheCleanupMarkerLines(dataDir, selectedBranch);
        var versionsDir = Path.Combine(dataDir, LauncherStorageNames.GameVersionsDirectory);
        if (!Directory.Exists(versionsDir))
        {
            markerLines.Add("Game versions directory present: false");
            markerLines.Add("Removed count: 0");
            WriteCacheCleanupMarker(dataDir, markerLines);
            return 0;
        }

        var removed = 0;
        foreach (var cache in LauncherGameVersionCache.Enumerate(dataDir, selectedBranch))
        {
            if (!cache.Selected || string.Equals(selectedBranch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase))
            {
                PatchHelper.Log($"[Launcher] Removing inactive game version cache: {cache.DirectoryName} -> {cache.Path}");
                markerLines.Add($"Removed cache: {cache.DirectoryName} -> {cache.Path}");
                DeleteDirectory(cache.Path);
                removed++;
                continue;
            }

            PatchHelper.Log($"[Launcher] Preserving selected game version cache: {cache.DirectoryName} -> {cache.Path}");
            markerLines.Add($"Preserved selected cache: {cache.DirectoryName} -> {cache.Path}");
        }

        markerLines.Add("Game versions directory present: true");
        markerLines.Add($"Removed count: {removed}");
        WriteCacheCleanupMarker(dataDir, markerLines);
        PatchHelper.Log($"[Launcher] Removed {removed} inactive game version cache(s); selected branch '{selectedBranch}' preserved");
        return removed;
    }

    private static System.Collections.Generic.List<string> NewCacheCleanupMarkerLines(string dataDir, string selectedBranch)
        => new()
        {
            $"UTC: {System.DateTime.UtcNow:O}",
            $"Selected branch: {selectedBranch}",
            $"Selected version: {SteamGameBranch.DisplayName(selectedBranch)}",
            $"Selected version slot kind: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}",
            $"Selected version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}",
        };

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

    private static void WriteRedownloadMarker(
        string dataDir,
        string selectedBranch,
        string gameDirectory,
        bool gameDirectoryExisted,
        bool? gameDirectoryExistsAfterDelete,
        string downloadStateDirectory,
        bool downloadStateDirectoryExisted,
        bool? downloadStateDirectoryExistsAfterDelete
    )
    {
        try
        {
            var lines = new System.Collections.Generic.List<string>
            {
                $"UTC: {System.DateTime.UtcNow:O}",
                $"Selected branch: {selectedBranch}",
                $"Selected version: {SteamGameBranch.DisplayName(selectedBranch)}",
                $"Selected version slot kind: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}",
                $"Selected version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}",
                $"Deleted game directory: {gameDirectory}",
                $"Game directory existed before delete: {gameDirectoryExisted.ToString().ToLowerInvariant()}",
                $"Deleted download state directory: {downloadStateDirectory}",
                $"Download state directory existed before delete: {downloadStateDirectoryExisted.ToString().ToLowerInvariant()}",
            };

            if (gameDirectoryExistsAfterDelete.HasValue)
                lines.Add($"Game directory exists after delete: {gameDirectoryExistsAfterDelete.Value.ToString().ToLowerInvariant()}");

            if (downloadStateDirectoryExistsAfterDelete.HasValue)
                lines.Add($"Download state directory exists after delete: {downloadStateDirectoryExistsAfterDelete.Value.ToString().ToLowerInvariant()}");

            File.WriteAllLines(RedownloadMarkerPath(dataDir), lines);
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write game version redownload marker: {ex.Message}");
        }
    }

    private static string ReadMarkerValue(string path, string prefix)
    {
        try
        {
            if (!File.Exists(path))
                return "<none>";

            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    return line.Substring(prefix.Length).Trim();
            }
        }
        catch
        {
            return "<read failed>";
        }

        return "<missing>";
    }

    private static bool MarkerUtcParseable(string path)
        => System.DateTime.TryParse(
            ReadMarkerValue(path, "UTC:"),
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal,
            out _
        );

    private static bool MarkerHasLine(string path, string prefix)
    {
        try
        {
            if (!File.Exists(path))
                return false;

            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

}
