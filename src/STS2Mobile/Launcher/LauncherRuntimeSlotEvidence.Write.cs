using System;
using System.IO;
using System.Text.Json;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherRuntimeSlotEvidence
{
    private const int AuthorizationSchemaVersion = 1;

    internal static void WriteAuthorization(string dataDir, GameRuntimeSlot slot)
    {
        if (slot?.GameIdentity == null)
            throw new ArgumentException("A complete runtime slot is required for launch authorization.", nameof(slot));
        if (!slot.Playable
            || slot.RuntimePack?.Usable != true
            || slot.RuntimePack.SourceGameIdentity != slot.GameIdentity)
        {
            throw new InvalidDataException(
                "Only a playable runtime slot with a validated exact-identity runtime pack can authorize launch."
            );
        }

        var branch = SteamGameBranch.Normalize(slot.Branch);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            BuildPayload(dataDir, branch, slot, filesReady: true, readinessProblem: string.Empty),
            new JsonSerializerOptions { WriteIndented = true }
        );
        new AtomicFileWriter().WriteAllBytes(MarkerPath(dataDir), bytes);
        PatchHelper.Log(
            $"[Launcher] Runtime slot authorization marker written last: branch={branch} gameIdentity={slot.GameIdentityId} pack={slot.RuntimePack.PackId} path={MarkerPath(dataDir)}"
        );
    }

    private static object BuildPayload(
        string dataDir,
        string branch,
        GameRuntimeSlot slot,
        bool filesReady,
        string readinessProblem
    )
        => new
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
