using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    internal static GameRuntimeSlot Inspect(string dataDir, string branch)
    {
        var context = new GameRuntimeSlotInspectionContext(dataDir, branch);
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase: paths for branch '{branch}'");
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: paths pck='{context.PckPath}' source='{context.SourceAssemblyPath}' active='{context.ActiveAndroidAssemblyPath}' manifest='{context.RuntimePackManifestPath}'");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: PCK SHA256");
        var pckSha256 = PckSha256OrMissing(
            dataDir,
            context.Branch,
            context.GameDirectory,
            context.PckPath,
            context.RuntimePackManifestPath
        );
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: PCK SHA256 -> {pckSha256}");
        if (!HasUsableHash(pckSha256))
        {
            PatchHelper.Log("[Launcher] Runtime slot inspect phase: selected PCK missing or invalid; skipping source/runtime file probes");
            var incompleteSlot = BuildIncompleteRuntimeSlot(context, pckSha256);
            PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: incomplete files -> {incompleteSlot.RuntimeSlotId}");
            return incompleteSlot;
        }
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: source assembly SHA256");
        var sourceAssemblySha256 = SourceAssemblySha256OrMissing(
            dataDir,
            context.Branch,
            context.SourceAssemblyPath
        );
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: source assembly SHA256 -> {sourceAssemblySha256}");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: active Android assembly SHA256");
        var activeAndroidAssemblySha256 = ActiveAndroidAssemblySha256OrMissing(
            dataDir,
            context.Branch,
            context.ActiveAndroidAssemblyPath
        );
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: active Android assembly SHA256 -> {activeAndroidAssemblySha256}");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: metadata");
        var metadata = RuntimeSlotMetadata.Inspect(
            context.ReleaseInfoPath,
            context.BranchMarkerPath
        );
        PatchHelper.Log("[Launcher] Runtime slot inspect phase complete: metadata");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: runtime pack manifest");
        var runtimePack = RuntimePackManifest.Inspect(
            context.RuntimePackManifestPath,
            context.Branch,
            pckSha256,
            sourceAssemblySha256,
            context.PckPath
        );
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: runtime pack manifest -> {runtimePack?.Status ?? "<none>"}");
        pckSha256 = CanonicalizeRuntimePackSourcePckSha256(pckSha256, runtimePack);
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: runtime pack slot ID");
        var runtimePackSlotIdMatches = RuntimePackSlotIdMatchesFor(
            metadata,
            runtimePack,
            context.Branch,
            pckSha256,
            sourceAssemblySha256
        );
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: runtime pack slot ID -> {runtimePackSlotIdMatches}");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: patch compatibility");
        var patchCompatibility = PatchCompatibilityEvidence.Inspect(
            dataDir,
            context.Branch,
            context.GameDirectory,
            pckSha256,
            sourceAssemblySha256,
            runtimePack,
            runtimePackSlotIdMatches
        );
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: patch compatibility -> {patchCompatibility?.Status ?? "<none>"}");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: runtime slot identity");
        var runtimeSlotIdentity = BuildRuntimeSlotIdentity(
            context.Branch,
            metadata,
            runtimePack,
            runtimePackSlotIdMatches,
            patchCompatibility,
            pckSha256,
            sourceAssemblySha256
        );
        var runtimeSlotId = BuildRuntimeSlotId(context.Branch, runtimeSlotIdentity);
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: runtime slot identity -> {runtimeSlotId}");

        return BuildRuntimeSlot(
            context,
            metadata,
            runtimePack,
            patchCompatibility,
            runtimePackSlotIdMatches,
            runtimeSlotId,
            runtimeSlotIdentity,
            pckSha256,
            sourceAssemblySha256,
            activeAndroidAssemblySha256
        );
    }
}
