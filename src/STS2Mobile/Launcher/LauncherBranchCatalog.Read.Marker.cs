using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchCatalog
{
    private static BranchOption BranchOptionFromMarkerRow(SteamBranchAvailabilityMarkerRow row)
    {
        if (row.IsEmpty)
            return new BranchOption("");

        return new BranchOption(
            row.Branch,
            metadataVisible: row.MetadataVisible,
            windowsManifestDepotCount: row.WindowsManifestDepotCount,
            passwordRequired: row.PasswordRequired,
            buildId: row.BuildId,
            description: row.Description,
            source: "Steam app-info"
        );
    }
}
