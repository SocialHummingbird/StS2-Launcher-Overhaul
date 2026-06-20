namespace STS2Mobile.Launcher;

internal sealed partial class RuntimeSlotMetadata
{
    internal static RuntimeSlotMetadata Inspect(string releaseInfoPath, string branchMarkerPath)
    {
        var release = ReadReleaseInfo(releaseInfoPath);
        return new RuntimeSlotMetadata(
            releaseInfoPath,
            release.Version,
            release.Commit,
            release.BuildId,
            branchMarkerPath,
            ReadMarkerValue(branchMarkerPath, DepotManifestCountPrefix),
            ReadMarkerValue(branchMarkerPath, DepotsMatchingPublicPrefix),
            ReadMarkerValue(branchMarkerPath, DepotsDifferingFromPublicPrefix),
            ReadMarkerValue(branchMarkerPath, DepotsInheritedFromPublicPrefix),
            ReadMarkerValue(branchMarkerPath, DepotsMissingSelectedManifestPrefix),
            BuildDepotManifestFingerprint(branchMarkerPath)
        );
    }
}
