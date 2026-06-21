using System;
using System.Globalization;
using System.Linq;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static string ReadBranchAvailabilityMarkerValue(string dataDir, string prefix)
        => ValueOrMissing(SteamBranchAvailabilityMarkerFile.ReadValue(
            dataDir,
            prefix,
            missingFileValue: MissingDiagnosticValue,
            missingLineValue: $"<missing {prefix.TrimEnd(':')} line>",
            readFailedValue: LauncherMarkerFile.ReadFailedValue
        ));

    private static string ReadBranchAvailabilityMarkerValues(string dataDir, string prefix)
    {
        var values = SteamBranchAvailabilityMarkerFile.ReadValues(dataDir, prefix);
        if (values.Count == 0)
        {
            return SteamBranchAvailabilityMarkerFile.Exists(dataDir)
                ? $"<missing {prefix.TrimEnd(':')} lines>"
                : MissingDiagnosticValue;
        }

        return string.Join(" | ", values.Select(ValueOrMissing));
    }

    private static bool BranchAvailabilityMarkerMatchesSelectedBranch(string dataDir)
    {
        var markerBranch = ReadBranchAvailabilityMarkerValue(dataDir, SteamBranchAvailabilityMarkerFields.SelectedBranch);
        if (markerBranch.StartsWith("<", StringComparison.Ordinal))
            return false;

        return string.Equals(
            SteamGameBranch.Normalize(markerBranch),
            SteamGameBranch.Normalize(LauncherPreferences.ReadGameBranch()),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static string BranchAvailabilitySelectedBranchDownloadable(string dataDir)
        => BranchAvailabilitySelectedBranchPasswordProtected(dataDir)
            ? "false"
            : BranchAvailabilitySelectedBranchManifestCount(dataDir) > 0
                ? "true"
                : "false";

    private static string BranchAvailabilitySelectedBranchProblem(string dataDir)
    {
        if (BranchAvailabilitySelectedBranchPasswordProtected(dataDir))
            return "selected branch is password-protected";

        var manifestCount = BranchAvailabilitySelectedBranchManifestCount(dataDir);
        if (manifestCount > 0)
            return "downloadable";

        var visibility = ReadBranchAvailabilityMarkerValue(
            dataDir,
            SteamBranchAvailabilityMarkerFields.SelectedBranchVisibility
        );
        if (visibility.StartsWith("<", StringComparison.Ordinal))
            return visibility;

        return visibility.Contains("not listed", StringComparison.OrdinalIgnoreCase)
            ? "selected branch was not listed in Steam branch metadata and has no Windows depot manifest"
            : "selected branch was listed but has no Windows depot manifest";
    }

    private static int BranchAvailabilitySelectedBranchManifestCount(string dataDir)
        => int.TryParse(
            ReadBranchAvailabilityMarkerValue(
                dataDir,
                SteamBranchAvailabilityMarkerFields.SelectedBranchWindowsDepotManifests
            ),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var count
        )
            ? count
            : 0;

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
}
