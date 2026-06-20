using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static string ReadBranchMarkerBranch(string markerPath)
        => ReadBranchMarkerValue(markerPath, "Branch:");

    private static string ReadBranchMarkerValue(string markerPath, string prefix)
        => ValueOrMissing(LauncherMarkerFile.ReadValue(
            markerPath,
            prefix,
            missingFileValue: MissingDiagnosticValue,
            missingLineValue: $"<missing {prefix.TrimEnd(':')} line>"
        ));

    private static bool BranchMarkerHasDepotManifestProvenance(string markerPath)
        => BranchMarkerDepotManifestCount(markerPath) > 0;

    private static bool BranchMarkerHasIntegrityProvenance(string markerPath)
        => ReadMarkerInt(markerPath, "Depot manifests matching public count:").HasValue
        && ReadMarkerInt(markerPath, "Depot manifests differing from public count:").HasValue
        && ReadMarkerInt(markerPath, "Depot manifests without public comparison count:").HasValue
        && ReadMarkerInt(markerPath, "Depot manifests inherited from public count:").HasValue
        && ReadMarkerInt(markerPath, "Depot manifests missing selected branch manifest count:").HasValue;

    private static bool BranchMarkerHasInstallSlotProvenance(string markerPath, string expectedSlotKind, string expectedSlotDirectory)
        => string.Equals(
            ReadBranchMarkerValue(markerPath, "Install slot kind:"),
            expectedSlotKind,
            System.StringComparison.OrdinalIgnoreCase
        )
        && string.Equals(
            NormalizeMarkerPath(ReadBranchMarkerValue(markerPath, "Install slot directory:")),
            NormalizeMarkerPath(expectedSlotDirectory),
            System.StringComparison.OrdinalIgnoreCase
        );

    private static string NormalizeMarkerPath(string path)
        => string.IsNullOrWhiteSpace(path) || path.StartsWith("<", System.StringComparison.Ordinal)
            ? string.Empty
            : path.Trim().Replace('\\', '/').TrimEnd('/');

    private static int BranchMarkerDepotManifestCount(string markerPath)
        => LauncherMarkerFile.CountLines(markerPath, "Depot manifest:");

    private static string BranchMarkerPartialSteamBranchEvidence(string markerPath)
    {
        var matching = ReadMarkerInt(markerPath, "Depot manifests matching public count:");
        var differing = ReadMarkerInt(markerPath, "Depot manifests differing from public count:");
        var inherited = ReadMarkerInt(markerPath, "Depot manifests inherited from public count:");
        var selectedMissing = ReadMarkerInt(markerPath, "Depot manifests missing selected branch manifest count:");
        if (!matching.HasValue || !differing.HasValue)
            return MissingDiagnosticValue;

        if ((inherited ?? 0) > 0 && differing.Value > 0)
            return "selected branch inherits public depot manifests and overrides other depots";

        if ((selectedMissing ?? 0) > 0 && differing.Value > 0)
            return "selected branch has missing explicit branch manifests and branch-specific depot manifests";

        if ((inherited ?? 0) > 0 && differing.Value == 0)
            return "selected branch inherits public depot manifests only";

        if (matching.Value > 0 && differing.Value > 0)
            return "selected branch has both public-identical and branch-specific depot manifests";

        if (matching.Value > 0 && differing.Value == 0)
            return "selected branch depot manifests all match public";

        if (matching.Value == 0 && differing.Value > 0)
            return "selected branch depot manifests all differ from public";

        return "selected branch has no public comparison evidence";
    }

    private static string ReadBranchMarkerValues(string markerPath, string prefix, int maxValues)
        => LauncherMarkerFile.ReadJoinedValues(
            markerPath,
            prefix,
            " | ",
            MissingDiagnosticValue,
            $"<missing {prefix.TrimEnd(':')} lines>",
            maxValues: maxValues,
            valueFormatter: ValueOrMissing
        );

    private static int? ReadMarkerInt(string markerPath, string prefix)
        => LauncherMarkerFile.ReadInt(markerPath, prefix);

    private static bool CachedBranchMarkerReady(string cacheDirectoryName, string markerBranch, string markerPath, string cachePath)
    {
        if (string.IsNullOrWhiteSpace(markerBranch) || markerBranch.StartsWith("<"))
            return false;

        return string.Equals(
            cacheDirectoryName,
            SteamGameBranch.StateDirectoryName(markerBranch),
            System.StringComparison.OrdinalIgnoreCase
        )
        && BranchMarkerHasDepotManifestProvenance(markerPath)
        && BranchMarkerHasInstallSlotProvenance(markerPath, SteamGameInstallPaths.VersionSlotKind(markerBranch), cachePath)
        && (
            string.Equals(markerBranch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase)
            || BranchMarkerHasIntegrityProvenance(markerPath)
        );
    }
}
