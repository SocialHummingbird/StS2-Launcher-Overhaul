namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchAvailabilityStatus
{
    private const int MaxVisibleBranchesInStatus = 4;

    private const string SelectedBranchPrefix = "Selected branch:";
    private const string SelectedBranchVisibilityPrefix = "Selected branch visibility:";
    private const string SelectedBranchWindowsDepotManifestsPrefix = "Windows depot manifests for selected branch:";
    private const string VisibleBranchPrefix = "Visible branch:";
    private const string VisibleBranchOverflowCountPrefix = "Visible branch overflow count:";
    private const string RawSelectedBranchVisibilitySummaryMarker = " Selected branch visibility:";
    private const string PasswordRequiredTrueMarker = "passwordRequired=true";
    private const string ZeroWindowsManifestsMarker = "windowsManifestDepots=0";
}
