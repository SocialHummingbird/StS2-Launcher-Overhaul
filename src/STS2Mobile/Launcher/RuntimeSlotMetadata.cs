using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal sealed class RuntimeSlotMetadata
{
    private RuntimeSlotMetadata(
        string releaseInfoPath,
        string releaseVersion,
        string releaseCommit,
        string releaseBuildId,
        string branchMarkerPath,
        string depotManifestCount,
        string depotsMatchingPublic,
        string depotsDifferingFromPublic,
        string depotsInheritedFromPublic,
        string depotsMissingSelectedManifest,
        string depotManifestFingerprint
    )
    {
        ReleaseInfoPath = releaseInfoPath;
        ReleaseVersion = releaseVersion;
        ReleaseCommit = releaseCommit;
        ReleaseBuildId = releaseBuildId;
        BranchMarkerPath = branchMarkerPath;
        DepotManifestCount = depotManifestCount;
        DepotsMatchingPublic = depotsMatchingPublic;
        DepotsDifferingFromPublic = depotsDifferingFromPublic;
        DepotsInheritedFromPublic = depotsInheritedFromPublic;
        DepotsMissingSelectedManifest = depotsMissingSelectedManifest;
        DepotManifestFingerprint = depotManifestFingerprint;
    }

    internal string ReleaseInfoPath { get; }
    internal string ReleaseVersion { get; }
    internal string ReleaseCommit { get; }
    internal string ReleaseBuildId { get; }
    internal string BranchMarkerPath { get; }
    internal string DepotManifestCount { get; }
    internal string DepotsMatchingPublic { get; }
    internal string DepotsDifferingFromPublic { get; }
    internal string DepotsInheritedFromPublic { get; }
    internal string DepotsMissingSelectedManifest { get; }
    internal string DepotManifestFingerprint { get; }

    internal string IdentitySummary =>
        $"release={ValueOrUnknown(ReleaseVersion)} "
        + $"commit={ValueOrUnknown(ReleaseCommit)} "
        + $"build={ValueOrUnknown(ReleaseBuildId)} "
        + $"depotManifests={ValueOrUnknown(DepotManifestCount)} "
        + $"depotFingerprint={ValueOrUnknown(DepotManifestFingerprint)}";

    internal static RuntimeSlotMetadata Inspect(string releaseInfoPath, string branchMarkerPath)
    {
        var release = ReadReleaseInfo(releaseInfoPath);
        return new RuntimeSlotMetadata(
            releaseInfoPath,
            release.Version,
            release.Commit,
            release.BuildId,
            branchMarkerPath,
            ReadMarkerValue(branchMarkerPath, "Depot manifest count:"),
            ReadMarkerValue(branchMarkerPath, "Depot manifests matching public count:"),
            ReadMarkerValue(branchMarkerPath, "Depot manifests differing from public count:"),
            ReadMarkerValue(branchMarkerPath, "Depot manifests inherited from public count:"),
            ReadMarkerValue(branchMarkerPath, "Depot manifests missing selected branch manifest count:"),
            BuildDepotManifestFingerprint(branchMarkerPath)
        );
    }

    private static (string Version, string Commit, string BuildId) ReadReleaseInfo(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return ("<missing>", "<missing>", "<missing>");

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            return (
                ReadString(root, "version", "Version", "gameVersion", "game_version", "releaseVersion", "release_version"),
                ReadString(root, "commit", "Commit", "gitCommit", "git_commit", "sha", "revision"),
                ReadString(root, "buildId", "build_id", "build", "Build", "steamBuildId", "steam_build_id")
            );
        }
        catch (Exception ex)
        {
            return ($"<unreadable:{ex.GetType().Name}>", "<unreadable>", "<unreadable>");
        }
    }

    private static string ReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value))
            {
                if (value.ValueKind == JsonValueKind.String)
                    return value.GetString() ?? string.Empty;
                if (value.ValueKind == JsonValueKind.Number || value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
                    return value.ToString();
            }
        }

        return "<missing>";
    }

    private static string ReadMarkerValue(string path, string prefix)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return "<missing>";

            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return line.Substring(prefix.Length).Trim();
            }
        }
        catch
        {
            return "<read failed>";
        }

        return "<missing>";
    }

    private static string BuildDepotManifestFingerprint(string branchMarkerPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(branchMarkerPath) || !File.Exists(branchMarkerPath))
                return "<missing>";

            var manifestRows = File.ReadLines(branchMarkerPath)
                .Where(line => line.StartsWith("Depot manifest:", StringComparison.OrdinalIgnoreCase))
                .OrderBy(line => line, StringComparer.Ordinal)
                .ToArray();
            if (manifestRows.Length == 0)
                return "<missing>";

            return StableHash16(string.Join("\n", manifestRows));
        }
        catch (Exception ex)
        {
            return $"<unavailable:{ex.GetType().Name}>";
        }
    }

    private static string StableHash16(string value)
    {
        unchecked
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            var hash = offset;
            foreach (var b in Encoding.UTF8.GetBytes(value ?? string.Empty))
            {
                hash ^= b;
                hash *= prime;
            }

            return hash.ToString("x16");
        }
    }

    private static string ValueOrUnknown(string value)
        => string.IsNullOrWhiteSpace(value) || value.StartsWith("<", StringComparison.Ordinal)
            ? "unknown"
            : value;
}
