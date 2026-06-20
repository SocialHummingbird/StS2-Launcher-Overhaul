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
            ReadMarkerValue(branchMarkerPath, LauncherBranchMarkerFields.DepotManifestCount),
            ReadMarkerValue(branchMarkerPath, LauncherBranchMarkerFields.DepotsMatchingPublic),
            ReadMarkerValue(branchMarkerPath, LauncherBranchMarkerFields.DepotsDifferingFromPublic),
            ReadMarkerValue(branchMarkerPath, LauncherBranchMarkerFields.DepotsInheritedFromPublic),
            ReadMarkerValue(branchMarkerPath, LauncherBranchMarkerFields.DepotsMissingSelectedManifest),
            BuildDepotManifestFingerprint(branchMarkerPath)
        );
    }
}
