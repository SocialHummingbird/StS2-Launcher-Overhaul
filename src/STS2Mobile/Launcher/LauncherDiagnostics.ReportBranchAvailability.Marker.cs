using System;
using System.Collections.Generic;
using System.Globalization;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static string ReadBranchAvailabilityMarkerValue(string dataDir, string prefix)
        => ReadBranchMarkerValue(SteamGameInstallPaths.BranchAvailabilityMarkerPath(dataDir), prefix);

    private static string ReadBranchAvailabilityMarkerValues(string dataDir, string prefix)
        => LauncherMarkerFile.ReadJoinedValues(
            SteamGameInstallPaths.BranchAvailabilityMarkerPath(dataDir),
            prefix,
            " | ",
            MissingDiagnosticValue,
            $"<missing {prefix.TrimEnd(':')} lines>",
            valueFormatter: ValueOrMissing
        );

    private static bool BranchAvailabilityMarkerMatchesSelectedBranch(string dataDir)
    {
        var markerBranch = ReadBranchAvailabilityMarkerValue(dataDir, "Selected branch:");
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

        var visibility = ReadBranchAvailabilityMarkerValue(dataDir, "Selected branch visibility:");
        if (visibility.StartsWith("<", StringComparison.Ordinal))
            return visibility;

        return visibility.Contains("not listed", StringComparison.OrdinalIgnoreCase)
            ? "selected branch was not listed in Steam branch metadata and has no Windows depot manifest"
            : "selected branch was listed but has no Windows depot manifest";
    }

    private static int BranchAvailabilitySelectedBranchManifestCount(string dataDir)
        => int.TryParse(
            ReadBranchAvailabilityMarkerValue(dataDir, "Windows depot manifests for selected branch:"),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var count
        )
            ? count
            : 0;

    private static bool BranchAvailabilitySelectedBranchPasswordProtected(string dataDir)
    {
        var selectedBranch = ReadBranchAvailabilityMarkerValue(dataDir, "Selected branch:");
        if (string.IsNullOrWhiteSpace(selectedBranch) || selectedBranch.StartsWith("<", StringComparison.Ordinal))
            return false;

        foreach (var value in ReadBranchAvailabilityMarkerRawValues(dataDir, "Visible branch:"))
        {
            if (!BranchAvailabilityMarkerValueMatchesBranch(value, selectedBranch))
                continue;

            return value.Contains("passwordRequired=true", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool BranchAvailabilityMarkerValueMatchesBranch(string markerValue, string branch)
    {
        if (string.IsNullOrWhiteSpace(markerValue) || string.IsNullOrWhiteSpace(branch))
            return false;

        var nameEnd = markerValue.IndexOf(" [", StringComparison.Ordinal);
        var name = nameEnd > 0 ? markerValue[..nameEnd] : markerValue;
        return string.Equals(
            SteamGameBranch.Normalize(name),
            SteamGameBranch.Normalize(branch),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static IEnumerable<string> ReadBranchAvailabilityMarkerRawValues(string dataDir, string prefix)
        => LauncherMarkerFile.ReadValues(SteamGameInstallPaths.BranchAvailabilityMarkerPath(dataDir), prefix);
}
