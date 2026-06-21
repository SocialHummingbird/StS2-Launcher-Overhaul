using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchAvailabilityStatus
{
    private const int MaxVisibleBranchesInStatus = 4;

    private const string RawSelectedBranchVisibilitySummaryMarker =
        " " + SteamBranchAvailabilityMarkerFields.SelectedBranchVisibility;
}
