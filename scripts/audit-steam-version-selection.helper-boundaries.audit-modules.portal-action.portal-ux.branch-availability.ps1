function Add-SteamVersionSelectionPortalActionBranchAvailabilityBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.branch-availability.ps1" `
        "keeps Steam branch availability audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionBranchAvailabilityChecks",
            "audit-steam-version-selection.branch-availability.downloader.ps1",
            "audit-steam-version-selection.branch-availability.marker.ps1",
            "audit-steam-version-selection.branch-availability.launcher-status.ps1",
            "Add-SteamVersionSelectionBranchAvailabilityDownloaderChecks",
            "Add-SteamVersionSelectionBranchAvailabilityMarkerChecks",
            "Add-SteamVersionSelectionBranchAvailabilityLauncherStatusChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-availability.downloader.ps1" `
        "keeps Steam branch availability downloader audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchAvailabilityDownloaderChecks",
            "DepotDownloader.BranchAvailability.Builder.cs",
            "DepotDownloader.BranchAvailability.Report.cs",
            "DepotDownloader.BranchAvailability.Marker.cs",
            "DepotDownloader.BranchAvailability.Model.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-availability.marker.ps1" `
        "keeps Steam branch availability marker audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchAvailabilityMarkerChecks",
            "SteamBranchAvailabilityMarkerFields",
            "SteamBranchAvailabilityMarkerFile",
            "SteamBranchAvailabilityMarkerRow"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-availability.launcher-status.ps1" `
        "keeps Steam branch availability launcher-status audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchAvailabilityLauncherStatusChecks",
            "LauncherBranchAvailabilityStatus.cs",
            "LauncherBranchAvailabilityStatus.Fields.cs",
            "LauncherBranchAvailabilityStatus.Read.cs",
            "LauncherBranchAvailabilityStatus.Format.cs",
            "LauncherBranchAvailabilityStatus.Match.cs"
        )
}
