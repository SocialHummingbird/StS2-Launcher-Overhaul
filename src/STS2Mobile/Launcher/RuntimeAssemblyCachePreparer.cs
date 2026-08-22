using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed class RuntimeAssemblyCachePreparer : IRuntimeAssemblyCachePreparer
{
    internal static readonly RuntimeAssemblyCachePreparer Current = new();

    private RuntimeAssemblyCachePreparer()
    {
    }

    public RuntimeAssemblyCachePreparationResult Prepare(
        string dataDir,
        GameIdentity gameIdentity,
        RuntimePackManifest manifest
    )
    {
        if (gameIdentity == null)
            return RuntimeAssemblyCachePreparationResult.Rejected(
                "An authoritative GameIdentity is required for active-cache preparation."
            );
        if (manifest?.Usable != true
            || manifest.SourceGameIdentity != gameIdentity
            || string.IsNullOrWhiteSpace(manifest.PackId))
        {
            return RuntimeAssemblyCachePreparationResult.Rejected(
                "A validated runtime pack for the exact GameIdentity is required for active-cache preparation."
            );
        }

        // Android owns the one active Mono assembly-cache promotion path because
        // it alone can rebuild the full cache from packaged assets. Other hosts
        // validate the runtime pack but do not use Android's publish cache.
        if (!OperatingSystem.IsAndroid())
            return RuntimeAssemblyCachePreparationResult.Success();

        try
        {
            var problem = AndroidGodotAppBridge.PrepareRuntimePackForLaunch(
                gameIdentity.Branch,
                gameIdentity.Id,
                manifest.PackId
            );
            if (!string.IsNullOrWhiteSpace(problem))
                return RuntimeAssemblyCachePreparationResult.Rejected(problem);

            return RuntimeAssemblyCachePreparationResult.Success();
        }
        catch (Exception ex)
        {
            var problem = $"Native Android assembly-cache preparation failed: {ex.GetBaseException().Message}";
            PatchHelper.Log($"[Launcher] {problem}");
            return RuntimeAssemblyCachePreparationResult.Rejected(problem);
        }
    }
}
