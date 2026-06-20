using System.IO;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    private static GameRuntimeSlot BuildIncompleteRuntimeSlot(
        GameRuntimeSlotInspectionContext context,
        string pckSha256
    )
    {
        var metadata = RuntimeSlotMetadata.Inspect(
            context.ReleaseInfoPath,
            context.BranchMarkerPath
        );
        var runtimePack = RuntimePackManifest.NotInstalled(context.RuntimePackManifestPath, context.Branch);
        var patchCompatibility = PatchCompatibilityEvidence.Missing(
            context.Branch,
            Path.Combine(context.GameDirectory, PatchCompatibilityEvidence.GameDirectoryMarkerFileName),
            "selected game directory validation marker"
        );
        var runtimeSlotIdentity = BuildRuntimeSlotIdentity(
            context.Branch,
            metadata,
            runtimePack,
            false,
            patchCompatibility,
            pckSha256,
            "<missing>"
        );
        var runtimeSlotId = BuildRuntimeSlotId(context.Branch, runtimeSlotIdentity);
        return BuildRuntimeSlot(
            context,
            metadata,
            runtimePack,
            patchCompatibility,
            runtimePackSlotIdMatches: false,
            runtimeSlotId,
            runtimeSlotIdentity,
            pckSha256,
            "<missing>",
            "<missing>"
        );
    }

    private static GameRuntimeSlot BuildRuntimeSlot(
        GameRuntimeSlotInspectionContext context,
        RuntimeSlotMetadata metadata,
        RuntimePackManifest runtimePack,
        PatchCompatibilityEvidence patchCompatibility,
        bool runtimePackSlotIdMatches,
        string runtimeSlotId,
        string runtimeSlotIdentity,
        string pckSha256,
        string sourceAssemblySha256,
        string activeAndroidAssemblySha256
    )
        => new GameRuntimeSlot(
            context.Branch,
            context.DisplayName,
            context.SlotKind,
            context.SlotDirectory,
            context.GameDirectory,
            context.PckPath,
            context.ReleaseInfoPath,
            context.SourceAssemblyPath,
            context.ActiveAndroidAssemblyPath,
            context.RuntimePackManifestPath,
            metadata,
            runtimePack,
            patchCompatibility,
            runtimePackSlotIdMatches,
            runtimeSlotId,
            runtimeSlotIdentity,
            pckSha256,
            sourceAssemblySha256,
            activeAndroidAssemblySha256,
            File.Exists(context.SourceAssemblyPath),
            File.Exists(context.ActiveAndroidAssemblyPath),
            File.Exists(context.RuntimePackManifestPath)
        );
}
