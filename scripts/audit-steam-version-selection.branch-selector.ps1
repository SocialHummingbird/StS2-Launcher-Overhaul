. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-selector.catalog.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-selector.storage.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-selector.download-section.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-selector.action-section.ps1")

function Add-SteamVersionSelectionBranchSelectorChecks {
    Add-SteamVersionSelectionBranchSelectorCatalogChecks

    Add-SteamVersionSelectionBranchSelectorStorageChecks

    Add-SteamVersionSelectionBranchSelectorDownloadSectionChecks

    Add-SteamVersionSelectionBranchSelectorActionSectionChecks
}
