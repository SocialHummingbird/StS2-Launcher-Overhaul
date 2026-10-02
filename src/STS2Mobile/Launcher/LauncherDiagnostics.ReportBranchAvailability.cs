using System.IO;
using System.Text;
using STS2Mobile.Steam;
using System;
using System.Globalization;
using System.Linq;

namespace STS2Mobile.Launcher;
internal static partial class LauncherDiagnostics
{
    private static void AppendBranchAvailability(StringBuilder sb, string dataDir)
    {
        var markerPath = SteamGameInstallPaths.BranchAvailabilityMarkerPath(dataDir);
        sb.AppendLine($"Steam branch availability marker filename: {SteamGameInstallPaths.BranchAvailabilityMarkerFileName}");
        sb.AppendLine($"Steam branch availability marker path: {markerPath}");
        sb.AppendLine($"Steam branch availability marker present: {BoolText(File.Exists(markerPath))}");
        sb.AppendLine($"Steam branch availability UTC: {ReadBranchAvailabilityMarkerValue(dataDir, SteamBranchAvailabilityMarkerFields.Utc)}");
        sb.AppendLine($"Steam branch availability selected branch: {ReadBranchAvailabilityMarkerValue(dataDir, SteamBranchAvailabilityMarkerFields.SelectedBranch)}");
        sb.AppendLine($"Steam branch availability matches current selected branch: {BoolText(BranchAvailabilityMarkerMatchesSelectedBranch(dataDir))}");
        sb.AppendLine($"Steam branch availability selected branch visibility: {ReadBranchAvailabilityMarkerValue(dataDir, SteamBranchAvailabilityMarkerFields.SelectedBranchVisibility)}");
        sb.AppendLine($"Steam branch availability selected branch Windows depot manifests: {ReadBranchAvailabilityMarkerValue(dataDir, SteamBranchAvailabilityMarkerFields.SelectedBranchWindowsDepotManifests)}");
        sb.AppendLine($"Steam branch availability selected branch downloadable: {BranchAvailabilitySelectedBranchDownloadable(dataDir)}");
        sb.AppendLine($"Steam branch availability selected branch problem: {BranchAvailabilitySelectedBranchProblem(dataDir)}");
        sb.AppendLine($"Steam branch availability visible branch count: {ReadBranchAvailabilityMarkerValue(dataDir, SteamBranchAvailabilityMarkerFields.VisibleBranchCount)}");
        sb.AppendLine($"Steam branch availability visible branches: {ReadBranchAvailabilityMarkerValues(dataDir, SteamBranchAvailabilityMarkerFields.VisibleBranch)}");
    }

    private static string ReadBranchAvailabilityMarkerValue(string dataDir, string prefix) => ValueOrMissing(SteamBranchAvailabilityMarkerFile.ReadValue(dataDir, prefix, missingFileValue: MissingDiagnosticValue, missingLineValue: $"<missing {prefix.TrimEnd(':')} line>", readFailedValue: LauncherMarkerFile.ReadFailedValue));
    private static string ReadBranchAvailabilityMarkerValues(string dataDir, string prefix)
    {
        var values = SteamBranchAvailabilityMarkerFile.ReadValues(dataDir, prefix);
        if (values.Count == 0)
        {
            return SteamBranchAvailabilityMarkerFile.Exists(dataDir) ? $"<missing {prefix.TrimEnd(':')} lines>" : MissingDiagnosticValue;
        }

        return string.Join(" | ", values.Select(ValueOrMissing));
    }

    private static bool BranchAvailabilityMarkerMatchesSelectedBranch(string dataDir)
    {
        var markerBranch = ReadBranchAvailabilityMarkerValue(dataDir, SteamBranchAvailabilityMarkerFields.SelectedBranch);
        if (markerBranch.StartsWith("<", StringComparison.Ordinal))
            return false;
        return string.Equals(SteamGameBranch.Normalize(markerBranch), SteamGameBranch.Normalize(LauncherPreferences.ReadGameBranch()), StringComparison.OrdinalIgnoreCase);
    }

    private static string BranchAvailabilitySelectedBranchDownloadable(string dataDir) => BranchAvailabilitySelectedBranchPasswordProtected(dataDir) ? "false" : BranchAvailabilitySelectedBranchManifestCount(dataDir) > 0 ? "true" : "false";
    private static string BranchAvailabilitySelectedBranchProblem(string dataDir)
    {
        if (BranchAvailabilitySelectedBranchPasswordProtected(dataDir))
            return "selected branch is password-protected";
        var manifestCount = BranchAvailabilitySelectedBranchManifestCount(dataDir);
        if (manifestCount > 0)
            return "downloadable";
        var visibility = ReadBranchAvailabilityMarkerValue(dataDir, SteamBranchAvailabilityMarkerFields.SelectedBranchVisibility);
        if (visibility.StartsWith("<", StringComparison.Ordinal))
            return visibility;
        return visibility.Contains("not listed", StringComparison.OrdinalIgnoreCase) ? "selected branch was not listed in Steam branch metadata and has no Windows depot manifest" : "selected branch was listed but has no Windows depot manifest";
    }

    private static int BranchAvailabilitySelectedBranchManifestCount(string dataDir) => int.TryParse(ReadBranchAvailabilityMarkerValue(dataDir, SteamBranchAvailabilityMarkerFields.SelectedBranchWindowsDepotManifests), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ? count : 0;
    private static bool BranchAvailabilitySelectedBranchPasswordProtected(string dataDir)
    {
        var selectedBranch = ReadBranchAvailabilityMarkerValue(dataDir, SteamBranchAvailabilityMarkerFields.SelectedBranch);
        if (string.IsNullOrWhiteSpace(selectedBranch) || selectedBranch.StartsWith("<", StringComparison.Ordinal))
            return false;
        foreach (var row in SteamBranchAvailabilityMarkerFile.ReadVisibleRows(dataDir))
        {
            if (!row.BranchMatches(selectedBranch))
                continue;
            return row.PasswordProtected;
        }

        return false;
    }

    private static string ReadBranchMarkerBranch(string markerPath) => ReadBranchMarkerValue(markerPath, LauncherBranchMarkerFields.Branch);
    private static string ReadBranchMarkerValue(string markerPath, string prefix) => ValueOrMissing(LauncherMarkerFile.ReadValue(markerPath, prefix, missingFileValue: MissingDiagnosticValue, missingLineValue: $"<missing {prefix.TrimEnd(':')} line>"));
    private static bool BranchMarkerHasDepotManifestProvenance(string markerPath) => BranchMarkerDepotManifestCount(markerPath) > 0;
    private static bool BranchMarkerHasIntegrityProvenance(string markerPath) => LauncherBranchMarkerIntegrityProvenance.Read(markerPath).IsComplete;
    private static bool BranchMarkerHasInstallSlotProvenance(string markerPath, string expectedSlotKind, string expectedSlotDirectory) => string.Equals(ReadBranchMarkerValue(markerPath, LauncherBranchMarkerFields.InstallSlotKind), expectedSlotKind, System.StringComparison.OrdinalIgnoreCase) && LauncherAndroidAppPrivatePath.NormalizedMarkerPathsEqual(ReadBranchMarkerValue(markerPath, LauncherBranchMarkerFields.InstallSlotDirectory), expectedSlotDirectory);
    private static int BranchMarkerDepotManifestCount(string markerPath) => LauncherMarkerFile.CountLines(markerPath, LauncherBranchMarkerFields.DepotManifestRow);
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

    private static string ReadBranchMarkerValues(string markerPath, string prefix, int maxValues) => LauncherMarkerFile.ReadJoinedValues(markerPath, prefix, " | ", MissingDiagnosticValue, $"<missing {prefix.TrimEnd(':')} lines>", maxValues: maxValues, valueFormatter: ValueOrMissing);
    private static int? ReadMarkerInt(string markerPath, string prefix) => LauncherMarkerFile.ReadInt(markerPath, prefix);
    private static bool CachedBranchMarkerReady(string cacheDirectoryName, string markerBranch, string markerPath, string cachePath)
    {
        if (string.IsNullOrWhiteSpace(markerBranch) || markerBranch.StartsWith("<"))
            return false;
        return string.Equals(cacheDirectoryName, SteamGameBranch.StateDirectoryName(markerBranch), System.StringComparison.OrdinalIgnoreCase) && BranchMarkerHasDepotManifestProvenance(markerPath) && BranchMarkerHasInstallSlotProvenance(markerPath, SteamGameInstallPaths.VersionSlotKind(markerBranch), cachePath) && (string.Equals(markerBranch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase) || BranchMarkerHasIntegrityProvenance(markerPath));
    }
}
