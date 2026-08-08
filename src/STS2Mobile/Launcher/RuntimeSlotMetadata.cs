using System;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimeSlotMetadata
{
    private RuntimeSlotMetadata(
        string releaseInfoPath,
        string releaseVersion,
        string releaseCommit,
        string releaseBuildId,
        string branchMarkerPath,
        string depotManifestCount,
        string depotsMatchingPublic,
        string depotsDifferingFromPublic,
        string depotsInheritedFromPublic,
        string depotsMissingSelectedManifest,
        string depotManifestFingerprint
    )
    {
        ReleaseInfoPath = releaseInfoPath;
        ReleaseVersion = releaseVersion;
        ReleaseCommit = releaseCommit;
        ReleaseBuildId = releaseBuildId;
        BranchMarkerPath = branchMarkerPath;
        DepotManifestCount = depotManifestCount;
        DepotsMatchingPublic = depotsMatchingPublic;
        DepotsDifferingFromPublic = depotsDifferingFromPublic;
        DepotsInheritedFromPublic = depotsInheritedFromPublic;
        DepotsMissingSelectedManifest = depotsMissingSelectedManifest;
        DepotManifestFingerprint = depotManifestFingerprint;
    }

    internal string ReleaseInfoPath { get; }
    internal string ReleaseVersion { get; }
    internal string ReleaseCommit { get; }
    internal string ReleaseBuildId { get; }
    internal string BranchMarkerPath { get; }
    internal string DepotManifestCount { get; }
    internal string DepotsMatchingPublic { get; }
    internal string DepotsDifferingFromPublic { get; }
    internal string DepotsInheritedFromPublic { get; }
    internal string DepotsMissingSelectedManifest { get; }
    internal string DepotManifestFingerprint { get; }

    internal string IdentitySummary =>
        $"release={ValueOrUnknown(ReleaseVersion)} "
        + $"commit={ValueOrUnknown(ReleaseCommit)} "
        + $"build={ValueOrUnknown(ReleaseBuildId)} "
        + $"depotManifests={ValueOrUnknown(DepotManifestCount)} "
        + $"depotFingerprint={ValueOrUnknown(DepotManifestFingerprint)}";

    private static string ValueOrUnknown(string value)
        => string.IsNullOrWhiteSpace(value) || value.StartsWith("<", StringComparison.Ordinal)
            ? "unknown"
            : value;
}
