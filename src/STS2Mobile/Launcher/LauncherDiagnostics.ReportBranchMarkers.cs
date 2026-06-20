using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static string ReadBranchMarkerBranch(string markerPath)
        => ReadBranchMarkerValue(markerPath, LauncherBranchMarkerFields.Branch);

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
        => LauncherBranchMarkerIntegrityProvenance.Read(markerPath).IsComplete;

    private static bool BranchMarkerHasInstallSlotProvenance(string markerPath, string expectedSlotKind, string expectedSlotDirectory)
        => string.Equals(
            ReadBranchMarkerValue(markerPath, LauncherBranchMarkerFields.InstallSlotKind),
            expectedSlotKind,
            System.StringComparison.OrdinalIgnoreCase
        )
        && LauncherAndroidAppPrivatePath.NormalizedMarkerPathsEqual(
            ReadBranchMarkerValue(markerPath, LauncherBranchMarkerFields.InstallSlotDirectory),
            expectedSlotDirectory
        );

    private static int BranchMarkerDepotManifestCount(string markerPath)
        => LauncherMarkerFile.CountLines(markerPath, LauncherBranchMarkerFields.DepotManifestRow);

    private static string BranchMarkerPartialSteamBranchEvidence(string markerPath)
    {
        var matching = ReadMarkerInt(markerPath, LauncherBranchMarkerFields.DepotsMatchingPublic);
        var differing = ReadMarkerInt(markerPath, LauncherBranchMarkerFields.DepotsDifferingFromPublic);
        var inherited = ReadMarkerInt(markerPath, LauncherBranchMarkerFields.DepotsInheritedFromPublic);
        var selectedMissing = ReadMarkerInt(markerPath, LauncherBranchMarkerFields.DepotsMissingSelectedManifest);
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
