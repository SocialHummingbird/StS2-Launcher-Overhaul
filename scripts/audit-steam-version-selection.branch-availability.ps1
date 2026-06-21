. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-availability.downloader.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-availability.marker.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-availability.launcher-status.ps1")

function Add-SteamVersionSelectionBranchAvailabilityChecks {
    Add-SteamVersionSelectionBranchAvailabilityDownloaderChecks

    Add-SteamVersionSelectionBranchAvailabilityMarkerChecks

    Add-SteamVersionSelectionBranchAvailabilityLauncherStatusChecks
}
