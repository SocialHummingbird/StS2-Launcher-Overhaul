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

    private static bool MarkerValueMatchesBranch(SteamBranchAvailabilityMarkerRow row, string branch)
        => row.BranchMatches(branch);

    private static bool MarkerValuePasswordProtected(SteamBranchAvailabilityMarkerRow row)
        => row.PasswordProtected;
}
