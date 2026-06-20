using System;
using System.IO;
using System.Text.Json;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    private static string ValidatedGameDirectoryPckSha256(string gameDirectory, string branch)
        => ValidatedGameDirectoryHash(
            gameDirectory,
            branch,
            "pckSha256",
            "pck_sha256",
            "sourcePckSha256",
            "source_pck_sha256"
        );

    private static string ValidatedGameDirectorySourceAssemblySha256(string gameDirectory, string branch)
        => ValidatedGameDirectoryHash(
            gameDirectory,
            branch,
            "sourceAssemblySha256",
            "source_assembly_sha256",
            "desktopAssemblySha256",
            "desktop_assembly_sha256"
        );

    private static string ValidatedGameDirectoryHash(string gameDirectory, string branch, params string[] hashProperties)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(gameDirectory))
                return null;

            var markerPath = Path.Combine(gameDirectory, PatchCompatibilityEvidence.GameDirectoryMarkerFileName);
            if (!File.Exists(markerPath))
                return null;

            using var document = JsonDocument.Parse(File.ReadAllText(markerPath));
            var root = document.RootElement;
            var status = ReadJsonString(root, "status", "result", "patchValidationStatus", "patch_validation_status");
            if (!string.Equals(status, "passed", StringComparison.OrdinalIgnoreCase))
                return null;

            var validatedBranch = ReadJsonString(root, "branch", "sourceBranch", "source_branch", "validatedBranch", "validated_branch");
            if (!string.Equals(SteamGameBranch.Normalize(validatedBranch), SteamGameBranch.Normalize(branch), StringComparison.OrdinalIgnoreCase))
                return null;

            var hash = ReadJsonString(root, hashProperties);
            return HasUsableHash(hash)
                ? hash.ToLowerInvariant()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string RuntimePackSourcePckSha256(string runtimePackManifestPath, string branch)
        => RuntimePackSourceHash(
            runtimePackManifestPath,
            branch,
            "sourcePckSha256",
            "source_pck_sha256",
            "pckSha256",
            "pck_sha256"
        );

    private static string RuntimePackSourceAssemblySha256(string runtimePackManifestPath, string branch)
        => RuntimePackSourceHash(
            runtimePackManifestPath,
            branch,
            "sourceAssemblySha256",
            "source_assembly_sha256",
            "desktopAssemblySha256",
            "desktop_assembly_sha256"
        );

    private static string RuntimePackSourceHash(string runtimePackManifestPath, string branch, params string[] hashProperties)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(runtimePackManifestPath) || !File.Exists(runtimePackManifestPath))
                return null;

            using var document = JsonDocument.Parse(File.ReadAllText(runtimePackManifestPath));
            var root = document.RootElement;
            var sourceBranch = ReadJsonString(root, "sourceBranch", "source_branch", "branch");
            if (!string.Equals(SteamGameBranch.Normalize(sourceBranch), SteamGameBranch.Normalize(branch), StringComparison.OrdinalIgnoreCase))
                return null;

            var patchStatus = ReadJsonString(root, "patchValidationStatus", "patch_validation_status", "patchStatus", "patch_status");
            if (!string.Equals(patchStatus, "passed", StringComparison.OrdinalIgnoreCase))
                return null;

            var hash = ReadJsonString(root, hashProperties);
            return HasUsableHash(hash)
                ? hash.ToLowerInvariant()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string ReadJsonString(JsonElement root, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
