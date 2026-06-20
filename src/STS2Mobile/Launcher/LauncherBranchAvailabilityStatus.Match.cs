using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchAvailabilityStatus
{
    private static bool MarkerBranchMatchesCurrentSelection(string selectedBranch)
    {
        if (string.IsNullOrWhiteSpace(selectedBranch))
            return false;

        return string.Equals(
            SteamGameBranch.Normalize(selectedBranch),
            SteamGameBranch.Normalize(LauncherPreferences.ReadGameBranch()),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static bool MarkerValueMatchesBranch(string markerValue, string branch)
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

    private static bool MarkerValuePasswordProtected(string markerValue)
        => !string.IsNullOrWhiteSpace(markerValue)
            && markerValue.Contains(PasswordRequiredTrueMarker, StringComparison.OrdinalIgnoreCase);
}
