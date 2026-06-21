function Add-SteamVersionSelectionBranchAvailabilityDownloaderChecks {
    Add-Check `
        "src\STS2Mobile\Steam\DepotDownloader.BranchAvailability.Builder.cs" `
        "parses Steam branch metadata including beta password flags" `
        @(
            "BranchAvailabilityBuilder",
            "pwdrequired",
            "password_required"
        )

    Add-Check `
        "src\STS2Mobile\Steam\DepotDownloader.BranchAvailability.Report.cs" `
        "builds account-visible Steam branch availability from app info" `
        @(
            "BranchAvailabilityReport",
            "Visible Steam branches",
            "SteamBranchAvailabilityMarkerFields\.SelectedBranchVisibility",
            "SteamBranchAvailabilityMarkerFields\.SelectedBranchWindowsDepotManifests",
            "visible branches",
            "DepotIsWindowsCompatible"
        )

    Add-Check `
        "src\STS2Mobile\Steam\DepotDownloader.BranchAvailability.Marker.cs" `
        "persists Steam branch availability marker evidence" `
        @(
            "WriteMarker",
            "BranchAvailabilityMarkerPath",
            "MaxBranchAvailabilityMarkerBranches",
            "SteamBranchAvailabilityMarkerFields\.VisibleBranchOverflowCount",
            "SteamBranchAvailabilityMarkerFields\.SelectedBranchVisibility",
            "SteamBranchAvailabilityMarkerFields\.SelectedBranchWindowsDepotManifests"
        )

    Add-Check `
        "src\STS2Mobile\Steam\DepotDownloader.BranchAvailability.Model.cs" `
        "formats branch availability marker values safely" `
        @(
            "MaxBranchAvailabilityMarkerValueLength",
            "SafeMarkerValue",
            "SteamBranchAvailabilityMarkerFields\.PasswordRequiredKey",
            "DownloadabilityText",
            "password-protected",
            "!PasswordRequired\.Equals\(""true""",
            "SteamBranchAvailabilityMarkerFields\.WindowsManifestDepotsKey",
            "SteamBranchAvailabilityMarkerFields\.MetadataVisibleKey"
        )
}
