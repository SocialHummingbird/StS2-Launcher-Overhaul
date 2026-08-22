namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    internal static GameRuntimeSlot RefreshDerivedEvidence(
        string dataDir,
        GameRuntimeSlot current
    )
    {
        if (current?.GameIdentity == null)
            return current;

        var context = new GameRuntimeSlotInspectionContext(dataDir, current.Branch);
        var metadata = RuntimeSlotMetadata.Inspect(
            context.ReleaseInfoPath,
            context.BranchMarkerPath
        );
        var runtimePack = RuntimePackManifest.Inspect(
            context.RuntimePackManifestPath,
            current.GameIdentity
        );
        var patchCompatibility = PatchCompatibilityEvidence.Inspect(
            current.GameIdentity,
            runtimePack
        );
        return BuildRuntimeSlot(
            context,
            metadata,
            runtimePack,
            patchCompatibility,
            current.GameIdentity,
            current.GameIdentityProblem,
            Sha256OrMissing(context.ActiveAndroidAssemblyPath)
        );
    }
}
