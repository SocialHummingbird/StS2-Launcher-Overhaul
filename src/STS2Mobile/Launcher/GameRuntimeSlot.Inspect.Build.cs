using System.IO;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    private static GameRuntimeSlot BuildIncompleteRuntimeSlot(
        GameRuntimeSlotInspectionContext context,
        string gameIdentityProblem
    )
    {
        var metadata = RuntimeSlotMetadata.Inspect(
            context.ReleaseInfoPath,
            context.BranchMarkerPath
        );
        var runtimePack = RuntimePackManifest.NotInstalled(context.RuntimePackManifestPath, context.Branch);
        var patchCompatibility = PatchCompatibilityEvidence.Missing(
            context.Branch,
            context.RuntimePackManifestPath,
            "runtime pack compatibility evidence"
        );
        return BuildRuntimeSlot(
            context,
            metadata,
            runtimePack,
            patchCompatibility,
            null,
            gameIdentityProblem,
            "<missing>"
        );
    }

    private static GameRuntimeSlot BuildRuntimeSlot(
        GameRuntimeSlotInspectionContext context,
        RuntimeSlotMetadata metadata,
        RuntimePackManifest runtimePack,
        PatchCompatibilityEvidence patchCompatibility,
        GameIdentity gameIdentity,
        string gameIdentityProblem,
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
            gameIdentity,
            gameIdentityProblem,
            runtimePack,
            patchCompatibility,
            activeAndroidAssemblySha256,
            File.Exists(context.SourceAssemblyPath),
            File.Exists(context.ActiveAndroidAssemblyPath),
            File.Exists(context.RuntimePackManifestPath)
        );
}
