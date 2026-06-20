namespace STS2Mobile.Launcher;

internal readonly record struct LauncherBranchMarkerIntegrityProvenance(
    int? MatchingPublic,
    int? DifferingFromPublic,
    int? WithoutPublicComparison,
    int? InheritedFromPublic,
    int? MissingSelectedManifest
)
{
    internal bool IsComplete =>
        MatchingPublic.HasValue
        && DifferingFromPublic.HasValue
        && WithoutPublicComparison.HasValue
        && InheritedFromPublic.HasValue
        && MissingSelectedManifest.HasValue;

    internal static LauncherBranchMarkerIntegrityProvenance Read(string markerPath)
        => new(
            LauncherMarkerFile.ReadInt(markerPath, LauncherBranchMarkerFields.DepotsMatchingPublic),
            LauncherMarkerFile.ReadInt(markerPath, LauncherBranchMarkerFields.DepotsDifferingFromPublic),
            LauncherMarkerFile.ReadInt(markerPath, LauncherBranchMarkerFields.DepotsWithoutPublicComparison),
            LauncherMarkerFile.ReadInt(markerPath, LauncherBranchMarkerFields.DepotsInheritedFromPublic),
            LauncherMarkerFile.ReadInt(markerPath, LauncherBranchMarkerFields.DepotsMissingSelectedManifest)
        );
}
