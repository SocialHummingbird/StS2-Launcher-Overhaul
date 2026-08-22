using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    internal static GameRuntimeSlot Inspect(string dataDir, string branch)
    {
        var context = new GameRuntimeSlotInspectionContext(dataDir, branch);
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase: paths for branch '{branch}'");
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: paths pck='{context.PckPath}' source='{context.SourceAssemblyPath}' active='{context.ActiveAndroidAssemblyPath}' manifest='{context.RuntimePackManifestPath}'");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: authoritative installed game identity");
        GameIdentity gameIdentity;
        try
        {
            gameIdentity = GameIdentityReader.ReadInstalled(dataDir, context.Branch);
        }
        catch (GameIdentityException ex)
        {
            PatchHelper.Log($"[Launcher] Runtime slot inspect phase failed: authoritative installed game identity -> {ex.Kind}: {ex.Message}");
            var incompleteSlot = BuildIncompleteRuntimeSlot(context, ex.Message);
            PatchHelper.Log("[Launcher] Runtime slot inspect phase complete: current game identity unavailable");
            return incompleteSlot;
        }

        return Inspect(dataDir, context, gameIdentity);
    }

    internal static GameRuntimeSlot Inspect(
        string dataDir,
        GameIdentity gameIdentity
    )
    {
        if (gameIdentity == null)
            throw new System.ArgumentNullException(nameof(gameIdentity));

        var context = new GameRuntimeSlotInspectionContext(
            dataDir,
            gameIdentity.Branch
        );
        PatchHelper.Log(
            $"[Launcher] Runtime slot inspect using supplied authoritative identity {gameIdentity.Id} for branch '{gameIdentity.Branch}'"
        );
        return Inspect(dataDir, context, gameIdentity);
    }

    private static GameRuntimeSlot Inspect(
        string dataDir,
        GameRuntimeSlotInspectionContext context,
        GameIdentity gameIdentity
    )
    {
        var pckSha256 = gameIdentity.PckSha256;
        var sourceAssemblySha256 = gameIdentity.SourceAssemblySha256;
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: authoritative installed game identity -> {gameIdentity.Id} generation={gameIdentity.InstallGeneration} PCK={pckSha256} source={sourceAssemblySha256}");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: active Android assembly SHA256");
        var activeAndroidAssemblySha256 = Sha256OrMissing(context.ActiveAndroidAssemblyPath);
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
            gameIdentity
        );
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: runtime pack manifest -> {runtimePack?.Status ?? "<none>"}");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: patch compatibility");
        var patchCompatibility = PatchCompatibilityEvidence.Inspect(
            gameIdentity,
            runtimePack
        );
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: patch compatibility -> {patchCompatibility?.Status ?? "<none>"}");

        return BuildRuntimeSlot(
            context,
            metadata,
            runtimePack,
            patchCompatibility,
            gameIdentity,
            string.Empty,
            activeAndroidAssemblySha256
        );
    }
}
