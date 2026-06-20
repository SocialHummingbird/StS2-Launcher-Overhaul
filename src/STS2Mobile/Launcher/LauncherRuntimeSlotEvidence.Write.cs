using System;
using System.IO;
using System.Text.Json;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherRuntimeSlotEvidence
{
    internal static void Write(string dataDir, string branch, bool filesReady, string readinessProblem)
    {
        try
        {
            branch = SteamGameBranch.Normalize(branch);
            var slot = GameRuntimeSlot.Inspect(dataDir, branch);
            Write(dataDir, slot, filesReady, readinessProblem);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write runtime slot evidence marker: {ex.Message}");
        }
    }

    internal static void Write(string dataDir, GameRuntimeSlot slot, bool filesReady, string readinessProblem)
    {
        try
        {
            var branch = SteamGameBranch.Normalize(slot.Branch);
            File.WriteAllText(
                MarkerPath(dataDir),
                JsonSerializer.Serialize(
                    BuildPayload(dataDir, branch, slot, filesReady, readinessProblem),
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );
            PatchHelper.Log($"[Launcher] Runtime slot evidence marker written: branch={branch} slot={slot.RuntimeSlotId} playable={slot.Playable} path={MarkerPath(dataDir)}");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write runtime slot evidence marker: {ex.Message}");
        }
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
            utc = DateTime.UtcNow.ToString("O"),
            branch,
            selectedVersion = SteamGameBranch.DisplayName(branch),
            selectedVersionSlotKind = SteamGameInstallPaths.VersionSlotKind(branch),
            selectedVersionSlotDirectory = SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch),
            runtimeSlotId = slot.RuntimeSlotId,
            runtimeSlotIdentity = slot.RuntimeSlotIdentity,
            filesReady,
            readinessProblem = string.IsNullOrWhiteSpace(readinessProblem) ? string.Empty : readinessProblem,
            playable = slot.Playable,
            runtimeCompatible = slot.RuntimeCompatible,
            patchCompatible = slot.PatchCompatible,
            branchMatchedAndroidRuntimePrepared = slot.BranchMatchedAndroidRuntimePrepared,
            requiresUsableRuntimePack = slot.RequiresRuntimePackOrPreparedCache,
            requiresRuntimePackOrPreparedCache = slot.RequiresRuntimePackOrPreparedCache,
            runtimePairingStatus = slot.RuntimePairingStatus,
            pckPath = slot.PckPath,
            pckSha256 = slot.PckSha256,
            sourceAssemblyPath = slot.SourceAssemblyPath,
            sourceAssemblySha256 = slot.SourceAssemblySha256,
            activeAndroidAssemblyPath = slot.ActiveAndroidAssemblyPath,
            activeAndroidAssemblySha256 = slot.ActiveAndroidAssemblySha256,
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
            runtimePackSourceRuntimeSlotId = slot.RuntimePack.SourceRuntimeSlotId,
            runtimePackSourceRuntimeSlotIdMatchesSelected = slot.RuntimePackSlotIdMatches,
            patchCompatibilitySource = slot.PatchCompatibility.Source,
            patchCompatibilityStatus = slot.PatchCompatibility.Status,
            patchCompatibilityDetail = slot.PatchCompatibility.Detail,
            patchCompatibilityMarkerPath = slot.PatchCompatibility.MarkerPath,
            patchCompatibilityValidationMode = slot.PatchCompatibility.ValidationMode,
            patchCompatibilityValidationSurfaceVersion = slot.PatchCompatibility.ValidationSurfaceVersion,
            patchCompatibilityMissingSymbolCount = slot.PatchCompatibility.MissingSymbolCount
        };
}
