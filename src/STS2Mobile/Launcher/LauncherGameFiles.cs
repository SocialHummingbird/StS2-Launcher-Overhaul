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
        if (!DownloadedForValidation(dataDir, branch))
            return false;

        PatchHelper.Log($"[Launcher] Game files ready phase: inspect runtime slot for branch '{SteamGameBranch.Normalize(branch)}'");
        var slot = GameRuntimeSlot.Inspect(dataDir, branch);
        PatchHelper.Log($"[Launcher] Game files ready phase complete: inspect runtime slot -> playable={slot.Playable} runtime={slot.RuntimePairingStatus} patch={slot.PatchCompatibility?.Status ?? "<none>"} pck={slot.PckSha256} source={slot.SourceAssemblySha256}");
        return slot.Playable;
    }

    internal static bool DownloadedForValidation(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        PatchHelper.Log($"[Launcher] Game files ready phase: resolve PCK path for branch '{branch}'");
        var pckPath = PckPath(dataDir, branch);
        PatchHelper.Log($"[Launcher] Game files ready phase complete: resolve PCK path -> '{pckPath}' length={(pckPath == null ? -1 : pckPath.Length)} rooted={(!string.IsNullOrWhiteSpace(pckPath) && Path.IsPathRooted(pckPath))}");
        PatchHelper.Log("[Launcher] Game files ready phase: validate PCK");
        if (!IsValidPck(pckPath))
        {
            PatchHelper.Log("[Launcher] Game files ready phase complete: validate PCK -> false");
            return false;
        }

        PatchHelper.Log("[Launcher] Game files ready phase complete: validate PCK -> true");
        PatchHelper.Log("[Launcher] Game files ready phase: branch marker");
        if (!BranchMarkerReady(dataDir, branch))
        {
            PatchHelper.Log("[Launcher] Game files ready phase complete: branch marker -> false");
            return false;
        }

        PatchHelper.Log("[Launcher] Game files ready phase complete: branch marker -> true");
        PatchHelper.Log("[Launcher] Game files ready phase: source assembly exists");
        var sourceAssemblyExists = SourceAssemblyExists(GameDirectoryPath(dataDir, branch));
        PatchHelper.Log($"[Launcher] Game files ready phase complete: source assembly exists -> {sourceAssemblyExists}");
        return sourceAssemblyExists;
    }

    private static bool SourceAssemblyExists(string gameDirectory)
    {
        if (string.IsNullOrWhiteSpace(gameDirectory) || !Directory.Exists(gameDirectory))
            return false;

        foreach (var directory in Directory.EnumerateDirectories(gameDirectory, "data_*", SearchOption.TopDirectoryOnly))
        {
            if (File.Exists(Path.Combine(directory, "sts2.dll")))
                return true;
        }

        return false;
    }

    internal static string ReadinessProblem(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        if (!IsValidPck(PckPath(dataDir, branch)))
            return "Selected game version is not downloaded or the downloaded PCK is invalid. Download selected version to continue.";

        if (HasBranchMetadataProblem(dataDir, branch))
            return "Selected game version has missing or mismatched branch metadata. Redownload selected version to rebuild the cache safely.";

        var runtimeProblem = GameRuntimeSlot.Inspect(dataDir, branch).ReadinessProblem();
        if (!string.IsNullOrWhiteSpace(runtimeProblem))
            return runtimeProblem;

        return null;
    }

    internal static string BranchIntegritySummary(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        if (string.Equals(branch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase))
            return null;

        var markerPath = SteamGameInstallPaths.BranchMarkerPath(dataDir, branch);
        if (!File.Exists(markerPath))
            return "Selected branch integrity evidence is unavailable because steam_branch.txt is missing.";

        var total = ReadMarkerInt(markerPath, "Depot manifest count:");
        var matchingPublic = ReadMarkerInt(markerPath, "Depot manifests matching public count:");
        var differingPublic = ReadMarkerInt(markerPath, "Depot manifests differing from public count:");
        var inheritedPublic = ReadMarkerInt(markerPath, "Depot manifests inherited from public count:");
        var missingSelected = ReadMarkerInt(markerPath, "Depot manifests missing selected branch manifest count:");
        var withoutPublicComparison = ReadMarkerInt(markerPath, "Depot manifests without public comparison count:");

        if (!total.HasValue)
            return "Selected branch integrity evidence is incomplete; depot manifest count is missing.";
        if (!BranchMarkerHasIntegrityProvenance(markerPath))
            return "Selected branch integrity evidence is incomplete; public-vs-selected depot comparison fields are missing. Redownload selected version to rebuild beta integrity evidence.";

        if (inheritedPublic.GetValueOrDefault() > 0 && differingPublic.GetValueOrDefault() > 0)
        {
            return $"Selected branch appears partial: {differingPublic.Value} depot(s) differ from public, {inheritedPublic.Value} depot(s) inherit public manifests.";
        }

        if (matchingPublic.GetValueOrDefault() > 0 && differingPublic.GetValueOrDefault() > 0)
        {
            return $"Selected branch appears partial: {differingPublic.Value} depot(s) differ from public, {matchingPublic.Value} depot(s) match public.";
        }

        if (inheritedPublic.GetValueOrDefault() > 0)
        {
            return $"Selected branch inherits public content for {inheritedPublic.Value} depot(s). If beta assets look public, compare file hashes before treating this as a launcher bug.";
        }

        if (missingSelected.GetValueOrDefault() > 0)
        {
            return $"Selected branch is missing explicit branch manifests for {missingSelected.Value} depot(s). See diagnostics for public-inheritance evidence.";
        }

        if (withoutPublicComparison.GetValueOrDefault() > 0)
        {
            return $"Selected branch downloaded {total.Value} depot(s), but {withoutPublicComparison.Value} depot(s) could not be compared with public.";
        }

        if (differingPublic.GetValueOrDefault() > 0)
            return $"Selected branch downloaded {differingPublic.Value} branch-specific depot(s).";

        if (matchingPublic.GetValueOrDefault() > 0)
            return $"Selected branch depot manifests all match public ({matchingPublic.Value} depot(s)).";

        return $"Selected branch downloaded {total.Value} depot manifest(s).";
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

    private static bool BranchMarkerHasIntegrityProvenance(string markerPath)
        => ReadMarkerInt(markerPath, "Depot manifests matching public count:").HasValue
        && ReadMarkerInt(markerPath, "Depot manifests differing from public count:").HasValue
        && ReadMarkerInt(markerPath, "Depot manifests without public comparison count:").HasValue
        && ReadMarkerInt(markerPath, "Depot manifests inherited from public count:").HasValue
        && ReadMarkerInt(markerPath, "Depot manifests missing selected branch manifest count:").HasValue;

    private static bool BranchMarkerHasInstallSlotProvenance(string markerPath, string dataDir, string branch)
        => string.Equals(
            ReadMarkerValue(markerPath, "Install slot kind:"),
            SteamGameInstallPaths.VersionSlotKind(branch),
            System.StringComparison.OrdinalIgnoreCase
        )
        && MarkerPathsEquivalent(
            ReadMarkerValue(markerPath, "Install slot directory:"),
            SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch),
            dataDir
        );

    private static string NormalizeMarkerPath(string path)
        => string.IsNullOrWhiteSpace(path) || path.StartsWith("<", System.StringComparison.Ordinal)
            ? string.Empty
            : path.Trim().Replace('\\', '/').TrimEnd('/');

    private static bool MarkerPathsEquivalent(string markerPath, string expectedPath, string dataDir)
    {
        var marker = NormalizeMarkerPath(markerPath);
        var expected = NormalizeMarkerPath(expectedPath);
        if (string.Equals(marker, expected, System.StringComparison.OrdinalIgnoreCase))
            return true;

        var alternateExpected = AndroidAppPrivatePathAlias(expected, dataDir);
        return !string.IsNullOrWhiteSpace(alternateExpected)
            && string.Equals(marker, alternateExpected, System.StringComparison.OrdinalIgnoreCase);
    }

    private static string AndroidAppPrivatePathAlias(string path, string dataDir)
    {
        var normalizedDataDir = NormalizeMarkerPath(dataDir);
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(normalizedDataDir))
            return string.Empty;

        const string dataUserPrefix = "/data/user/0/";
        const string dataDataPrefix = "/data/data/";
        if (normalizedDataDir.StartsWith(dataUserPrefix, System.StringComparison.OrdinalIgnoreCase))
        {
            var packageEnd = normalizedDataDir.IndexOf('/', dataUserPrefix.Length);
            if (packageEnd < 0)
                return string.Empty;
            var packageName = normalizedDataDir.Substring(dataUserPrefix.Length, packageEnd - dataUserPrefix.Length);
            var dataDataRoot = dataDataPrefix + packageName;
            var dataUserRoot = dataUserPrefix + packageName;
            return path.StartsWith(dataUserRoot, System.StringComparison.OrdinalIgnoreCase)
                ? dataDataRoot + path.Substring(dataUserRoot.Length)
                : string.Empty;
        }

        if (normalizedDataDir.StartsWith(dataDataPrefix, System.StringComparison.OrdinalIgnoreCase))
        {
            var packageEnd = normalizedDataDir.IndexOf('/', dataDataPrefix.Length);
            if (packageEnd < 0)
                return string.Empty;
            var packageName = normalizedDataDir.Substring(dataDataPrefix.Length, packageEnd - dataDataPrefix.Length);
            var dataDataRoot = dataDataPrefix + packageName;
            var dataUserRoot = dataUserPrefix + packageName;
            return path.StartsWith(dataDataRoot, System.StringComparison.OrdinalIgnoreCase)
                ? dataUserRoot + path.Substring(dataDataRoot.Length)
                : string.Empty;
        }

        return string.Empty;
    }

    internal static void DeleteDownloadedState(string dataDir)
        => DeleteDownloadedState(dataDir, LauncherPreferences.ReadGameBranch());

    internal static void DeleteDownloadedState(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        var gameDirectory = GameDirectoryPath(dataDir, branch);
        var downloadStateDirectory = SteamGameInstallPaths.DownloadStateDirectoryPath(dataDir, branch);
        var runtimePackDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(dataDir, branch);
        var gameDirectoryExisted = Directory.Exists(gameDirectory);
        var downloadStateDirectoryExisted = Directory.Exists(downloadStateDirectory);
        var runtimePackDirectoryExisted = Directory.Exists(runtimePackDirectory);
        WriteRedownloadMarker(
            dataDir,
            branch,
            gameDirectory,
            gameDirectoryExisted,
            null,
            downloadStateDirectory,
            downloadStateDirectoryExisted,
            null,
            runtimePackDirectory,
            runtimePackDirectoryExisted,
            null
        );
        DeleteDirectory(gameDirectory);
        DeleteDirectory(downloadStateDirectory);
        DeleteDirectory(runtimePackDirectory);
        LauncherRuntimeSlotEvidence.Clear(dataDir);
        LauncherRuntimeCacheEvidence.Clear(dataDir);
        LauncherRuntimePatchValidationEvidence.Clear(dataDir);
        WriteRedownloadMarker(
            dataDir,
            branch,
            gameDirectory,
            gameDirectoryExisted,
            Directory.Exists(gameDirectory),
            downloadStateDirectory,
            downloadStateDirectoryExisted,
            Directory.Exists(downloadStateDirectory),
            runtimePackDirectory,
            runtimePackDirectoryExisted,
            Directory.Exists(runtimePackDirectory)
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

    internal static string RedownloadMarkerRuntimePackDirectory(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), "Deleted runtime pack directory:");

    internal static string RedownloadMarkerRuntimePackDirectoryExisted(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), "Runtime pack directory existed before delete:");

    internal static string RedownloadMarkerRuntimePackDirectoryExistsAfterDelete(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), "Runtime pack directory exists after delete:");

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
            var removedRuntimePacksWithoutVersions = DeleteInactiveRuntimePacks(dataDir, selectedBranch, markerLines);
            markerLines.Add("Game versions directory present: false");
            markerLines.Add("Removed count: 0");
            markerLines.Add($"Removed runtime pack count: {removedRuntimePacksWithoutVersions}");
            WriteCacheCleanupMarker(dataDir, markerLines);
            return 0;
        }

        var removed = 0;
        var removedRuntimePacks = 0;
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

    private static string RuntimePackDirectoryPathForStateDirectory(string dataDir, string stateDirectoryName)
        => Path.Combine(dataDir, "runtime_packs", stateDirectoryName ?? string.Empty);

    private static int DeleteInactiveRuntimePacks(
        string dataDir,
        string selectedBranch,
        System.Collections.Generic.ICollection<string> markerLines
    )
    {
        var runtimePacksDir = Path.Combine(dataDir, "runtime_packs");
        if (!Directory.Exists(runtimePacksDir))
            return 0;

        var selectedStateDirectoryName = SteamGameBranch.StateDirectoryName(selectedBranch);
        var removed = 0;
        foreach (var runtimePackDirectory in Directory.GetDirectories(runtimePacksDir))
        {
            var directoryName = Path.GetFileName(runtimePackDirectory);
            if (string.Equals(directoryName, selectedStateDirectoryName, System.StringComparison.OrdinalIgnoreCase))
            {
                markerLines.Add($"Preserved selected runtime pack: {directoryName} -> {runtimePackDirectory}");
                continue;
            }

            DeleteDirectory(runtimePackDirectory);
            markerLines.Add($"Removed orphan runtime pack: {directoryName} -> {runtimePackDirectory} existsAfterDelete={Directory.Exists(runtimePackDirectory).ToString().ToLowerInvariant()}");
            removed++;
        }

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
        bool? downloadStateDirectoryExistsAfterDelete,
        string runtimePackDirectory,
        bool runtimePackDirectoryExisted,
        bool? runtimePackDirectoryExistsAfterDelete
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
                $"Deleted runtime pack directory: {runtimePackDirectory}",
                $"Runtime pack directory existed before delete: {runtimePackDirectoryExisted.ToString().ToLowerInvariant()}",
            };

            if (gameDirectoryExistsAfterDelete.HasValue)
                lines.Add($"Game directory exists after delete: {gameDirectoryExistsAfterDelete.Value.ToString().ToLowerInvariant()}");

            if (downloadStateDirectoryExistsAfterDelete.HasValue)
                lines.Add($"Download state directory exists after delete: {downloadStateDirectoryExistsAfterDelete.Value.ToString().ToLowerInvariant()}");

            if (runtimePackDirectoryExistsAfterDelete.HasValue)
                lines.Add($"Runtime pack directory exists after delete: {runtimePackDirectoryExistsAfterDelete.Value.ToString().ToLowerInvariant()}");

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

    private static int? ReadMarkerInt(string path, string prefix)
    {
        return int.TryParse(
            ReadMarkerValue(path, prefix),
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out var value
        )
            ? value
            : null;
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
