using System.IO;
using STS2Mobile.Patches;
using System.Text.Json;
using STS2Mobile.Steam;
using System;

namespace STS2Mobile.Launcher;
internal static partial class LauncherRuntimeSlotEvidence
{
    internal const string MarkerFileName = "current_runtime_slot.json";
    internal static string MarkerPath(string dataDir) => Path.Combine(dataDir, MarkerFileName);
    internal static bool MarkerPresent(string dataDir) => File.Exists(MarkerPath(dataDir));
    internal static void Revoke(string dataDir)
    {
        var path = MarkerPath(dataDir);
        if (!File.Exists(path))
            return;
        File.Delete(path);
        if (File.Exists(path))
            throw new IOException($"Failed to revoke launch authorization marker: {path}.");
        PatchHelper.Log($"[Launcher] Revoked runtime-slot launch authorization: {path}");
    }

    private const string GameIdentityIdProperty = "gameIdentityId";
    private const string BranchProperty = "branch";
    private const string FilesReadyProperty = "filesReady";
    private const string ReadinessProblemProperty = "readinessProblem";
    private const string RuntimePackUsabilityStatusProperty = "runtimePackUsabilityStatus";
    private const string PatchCompatibilityStatusProperty = "patchCompatibilityStatus";
    private const string PckSha256Property = "pckSha256";
    private const string SourceAssemblySha256Property = "sourceAssemblySha256";
    internal static bool IsAuthorized(string dataDir, GameIdentity expectedIdentity, string expectedPackId, out string problem)
    {
        problem = string.Empty;
        if (expectedIdentity == null)
        {
            problem = "Launch authorization requires an authoritative GameIdentity.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(expectedPackId))
        {
            problem = "Launch authorization requires an exact runtime-pack ID.";
            return false;
        }

        var path = MarkerPath(dataDir);
        if (!File.Exists(path))
        {
            problem = "Launch authorization marker is missing.";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("schemaVersion", out var schema) || schema.ValueKind != JsonValueKind.Number || !schema.TryGetInt32(out var schemaVersion) || schemaVersion != AuthorizationSchemaVersion)
            {
                problem = "Launch authorization marker has an unknown or missing schema.";
                return false;
            }

            var branch = RequiredString(root, BranchProperty);
            var gameIdentityId = RequiredString(root, GameIdentityIdProperty);
            var runtimePackId = RequiredString(root, "runtimePackId");
            var installGeneration = RequiredString(root, "installGeneration");
            var pckSha256 = RequiredString(root, PckSha256Property);
            var sourceAssemblySha256 = RequiredString(root, SourceAssemblySha256Property);
            var filesReady = RequiredTrue(root, FilesReadyProperty);
            var playable = RequiredTrue(root, "playable");
            var runtimeCompatible = RequiredTrue(root, "runtimeCompatible");
            var patchCompatible = RequiredTrue(root, "patchCompatible");
            var exact = filesReady && playable && runtimeCompatible && patchCompatible && SteamGameBranch.StorageIdentity(branch) == expectedIdentity.Branch && string.Equals(gameIdentityId, expectedIdentity.Id, System.StringComparison.Ordinal) && string.Equals(runtimePackId, expectedPackId, System.StringComparison.Ordinal) && string.Equals(installGeneration, expectedIdentity.InstallGeneration, System.StringComparison.Ordinal) && string.Equals(pckSha256, expectedIdentity.PckSha256, System.StringComparison.Ordinal) && string.Equals(sourceAssemblySha256, expectedIdentity.SourceAssemblySha256, System.StringComparison.Ordinal);
            if (exact)
                return true;
            problem = "Launch authorization marker does not exactly match the current GameIdentity and runtime pack.";
            return false;
        }
        catch (System.Exception ex)
        {
            problem = $"Launch authorization marker is unreadable: {ex.GetBaseException().Message}";
            return false;
        }
    }

    internal static Snapshot ReadSnapshot(string dataDir) => new(LauncherMarkerFile.ReadJsonSnapshot(MarkerPath(dataDir)));
    internal sealed class Snapshot
    {
        private readonly LauncherMarkerFile.JsonSnapshot _marker;
        internal Snapshot(LauncherMarkerFile.JsonSnapshot marker) => _marker = marker;
        internal bool Present => _marker.Present;
        internal string GameIdentityId => _marker.ReadString(GameIdentityIdProperty);
        internal string Branch => _marker.ReadString(BranchProperty);
        internal string FilesReady => _marker.ReadString(FilesReadyProperty);
        internal string ReadinessProblem => _marker.ReadString(ReadinessProblemProperty);
        internal string RuntimePackUsabilityStatus => _marker.ReadString(RuntimePackUsabilityStatusProperty);
        internal string PatchCompatibilityStatus => _marker.ReadString(PatchCompatibilityStatusProperty);
    }

    private static string RequiredString(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new InvalidDataException($"Launch authorization property '{property}' is missing.");
        }

        return value.GetString().Trim();
    }

    private static bool RequiredTrue(JsonElement root, string property) => root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;
    private const int AuthorizationSchemaVersion = 1;
    internal static void WriteAuthorization(string dataDir, GameRuntimeSlot slot)
    {
        if (slot?.GameIdentity == null)
            throw new ArgumentException("A complete runtime slot is required for launch authorization.", nameof(slot));
        if (!slot.Playable || slot.RuntimePack?.Usable != true || slot.RuntimePack.SourceGameIdentity != slot.GameIdentity)
        {
            throw new InvalidDataException("Only a playable runtime slot with a validated exact-identity runtime pack can authorize launch.");
        }

        var branch = SteamGameBranch.Normalize(slot.Branch);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(BuildPayload(dataDir, branch, slot, filesReady: true, readinessProblem: string.Empty), new JsonSerializerOptions { WriteIndented = true });
        new AtomicFileWriter().WriteAllBytes(MarkerPath(dataDir), bytes);
        PatchHelper.Log($"[Launcher] Runtime slot authorization marker written last: branch={branch} gameIdentity={slot.GameIdentityId} pack={slot.RuntimePack.PackId} path={MarkerPath(dataDir)}");
    }

    private static object BuildPayload(string dataDir, string branch, GameRuntimeSlot slot, bool filesReady, string readinessProblem) => new
    {
        schemaVersion = AuthorizationSchemaVersion,
        utc = DateTime.UtcNow.ToString("O"),
        branch,
        selectedVersion = SteamGameBranch.DisplayName(branch),
        selectedVersionSlotKind = SteamGameInstallPaths.VersionSlotKind(branch),
        selectedVersionSlotDirectory = SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch),
        gameIdentityId = slot.GameIdentityId,
        filesReady,
        readinessProblem = string.IsNullOrWhiteSpace(readinessProblem) ? string.Empty : readinessProblem,
        playable = slot.Playable,
        runtimeCompatible = slot.RuntimeCompatible,
        patchCompatible = slot.PatchCompatible,
        requiresUsableRuntimePack = slot.RequiresRuntimePackOrPreparedCache,
        requiresRuntimePackOrPreparedCache = slot.RequiresRuntimePackOrPreparedCache,
        runtimePairingStatus = slot.RuntimePairingStatus,
        installGeneration = slot.InstallGeneration,
        pckPath = slot.PckPath,
        pckSha256 = slot.PckSha256,
        sourceAssemblyPath = slot.SourceAssemblyPath,
        sourceAssemblySha256 = slot.SourceAssemblySha256,
        activeAndroidAssemblyPath = slot.ActiveAndroidAssemblyPath,
        activeAndroidAssemblySha256 = slot.ActiveAndroidAssemblySha256,
        preparedAndroidAssemblySha256 = slot.PreparedAndroidAssemblySha256,
        activeAndroidAssemblyMatchesPreparedRuntime = slot.ActiveAndroidAssemblyMatchesPreparedRuntime,
        requiresProcessRestartForPreparedRuntime = slot.RequiresProcessRestartForPreparedRuntime,
        releaseVersion = slot.Metadata.ReleaseVersion,
        releaseCommit = slot.Metadata.ReleaseCommit,
        releaseBuildId = slot.Metadata.ReleaseBuildId,
        depotManifestCount = slot.Metadata.DepotManifestCount,
        depotManifestFingerprint = slot.Metadata.DepotManifestFingerprint,
        runtimePackManifestPath = slot.RuntimePackManifestPath,
        runtimePackManifestPresent = slot.RuntimePackManifestExists,
        runtimePackId = slot.RuntimePack.PackId,
        runtimePackStatus = slot.RuntimePack.Status,
        runtimePackUsabilityStatus = slot.RuntimePackUsabilityStatus,
        runtimePackUsable = slot.RuntimePackUsable,
        runtimePackGameIdentityId = slot.RuntimePack.GameIdentityId,
        patchCompatibilitySource = slot.PatchCompatibility.Source,
        patchCompatibilityStatus = slot.PatchCompatibility.Status,
        patchCompatibilityDetail = slot.PatchCompatibility.Detail,
        patchCompatibilityMarkerPath = slot.PatchCompatibility.MarkerPath,
        patchCompatibilityValidationMode = slot.PatchCompatibility.ValidationMode,
        patchCompatibilityValidationSurfaceVersion = slot.PatchCompatibility.ValidationSurfaceVersion,
        patchCompatibilityMissingSymbolCount = slot.PatchCompatibility.MissingSymbolCount
    };
}
