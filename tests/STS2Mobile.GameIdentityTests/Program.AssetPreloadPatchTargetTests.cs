namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    // Android runs a publicized sts2.dll. The preload budget must still find its
    // targets there, or it silently leaves the unbounded title-screen burst in place.
    private static void AssetPreloadPatchTargetsSurvivePublicizer()
    {
        using var publicized = new PublicizedGameAssembly();
        foreach (var path in new[] { ReferenceGameAssemblyPath(), publicized.AssemblyPath })
        {
            RunInGameAssemblyContext(path, (game, launcher) => InvokeLauncherStatic(
                launcher,
                "STS2Mobile.Patches.AndroidAssetPreloadPatches",
                "RequireSupportedSession",
                game.GetType("MegaCrit.Sts2.Core.Assets.AssetLoadingSession", throwOnError: true)!
            ));
        }
    }
}
