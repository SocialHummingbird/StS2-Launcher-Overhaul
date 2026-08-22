using System;
using System.IO;
using System.Text.Json;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class GameIdentityPckCache
{
    internal const string FileName = "pck_identity_cache.json";
    private const int SchemaVersion = 1;

    internal static string PathFor(string dataDir, string branch)
        => Path.Combine(
            SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch),
            FileName
        );

    internal static bool TryRead(
        string dataDir,
        string branch,
        string installGeneration,
        GameIdentityFileSnapshot pck,
        out string pckSha256
    )
    {
        pckSha256 = null;
        try
        {
            var path = PathFor(dataDir, branch);
            if (!File.Exists(path))
                return false;

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (!TryReadInt(root, "schemaVersion", out var schemaVersion)
                || schemaVersion != SchemaVersion
                || !TryReadString(root, "branch", out var cachedBranch)
                || !TryReadString(root, "installGeneration", out var cachedGeneration)
                || !TryReadString(root, "pckPath", out var cachedPath)
                || !TryReadLong(root, "pckLength", out var cachedLength)
                || !TryReadLong(root, "pckLastWriteTimeUtcTicks", out var cachedMtime)
                || !TryReadString(root, "pckSha256", out var cachedSha256))
            {
                return false;
            }

            if (!string.Equals(
                    SteamGameBranch.StorageIdentity(cachedBranch),
                    SteamGameBranch.StorageIdentity(branch),
                    StringComparison.Ordinal
                )
                || !string.Equals(cachedGeneration, installGeneration, StringComparison.OrdinalIgnoreCase)
                || !PathsEqual(cachedPath, pck.NormalizedPath)
                || cachedLength != pck.Length
                || cachedMtime != pck.LastWriteTimeUtcTicks
                || !GameIdentity.IsSha256(cachedSha256))
            {
                return false;
            }

            pckSha256 = cachedSha256.ToLowerInvariant();
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static bool TryWrite(
        string dataDir,
        GameIdentity identity,
        GameIdentityFileSnapshot pck
    )
    {
        var path = PathFor(dataDir, identity.Branch);
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
            return false;

        try
        {
            var payload = new
            {
                schemaVersion = SchemaVersion,
                branch = identity.Branch,
                installGeneration = identity.InstallGeneration,
                pckPath = pck.NormalizedPath,
                pckLength = pck.Length,
                pckLastWriteTimeUtcTicks = pck.LastWriteTimeUtcTicks,
                pckSha256 = identity.PckSha256,
                writtenUtc = DateTime.UtcNow.ToString("O"),
            };
            var json = JsonSerializer.SerializeToUtf8Bytes(
                payload,
                new JsonSerializerOptions { WriteIndented = true }
            );

            new AtomicFileWriter().WriteAllBytes(path, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static string NormalizePath(string path)
    {
        var normalized = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Replace(Path.DirectorySeparatorChar, '/');
        return OperatingSystem.IsWindows()
            ? normalized.ToLowerInvariant()
            : normalized;
    }

    private static bool PathsEqual(string left, string right)
        => string.Equals(
            NormalizePath(left),
            NormalizePath(right),
            StringComparison.Ordinal
        );

    private static bool TryReadString(JsonElement root, string property, out string value)
    {
        value = null;
        if (!root.TryGetProperty(property, out var element) || element.ValueKind != JsonValueKind.String)
            return false;

        value = element.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryReadInt(JsonElement root, string property, out int value)
    {
        value = default;
        return root.TryGetProperty(property, out var element)
            && element.ValueKind == JsonValueKind.Number
            && element.TryGetInt32(out value);
    }

    private static bool TryReadLong(JsonElement root, string property, out long value)
    {
        value = default;
        return root.TryGetProperty(property, out var element)
            && element.ValueKind == JsonValueKind.Number
            && element.TryGetInt64(out value);
    }
}
