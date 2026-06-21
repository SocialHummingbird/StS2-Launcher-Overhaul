namespace STS2Mobile.Steam;

internal static class SteamBranchAvailabilityMarkerFields
{
    internal const string Utc = "UTC:";
    internal const string SelectedBranch = "Selected branch:";
    internal const string SelectedBranchVisibility = "Selected branch visibility:";
    internal const string SelectedBranchWindowsDepotManifests = "Windows depot manifests for selected branch:";
    internal const string VisibleBranchCount = "Visible branch count:";
    internal const string VisibleBranch = "Visible branch:";
    internal const string VisibleBranchOverflowCount = "Visible branch overflow count:";

    internal const string WindowsManifestDepotsKey = "windowsManifestDepots";
    internal const string MetadataVisibleKey = "metadataVisible";
    internal const string PasswordRequiredKey = "passwordRequired";
    internal const string BuildIdKey = "buildId";
    internal const string DescriptionKey = "description";

    internal const string PasswordRequiredTrueToken = PasswordRequiredKey + "=true";
    internal const string ZeroWindowsManifestDepotsToken = WindowsManifestDepotsKey + "=0";
}
