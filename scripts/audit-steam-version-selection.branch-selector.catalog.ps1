. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-selector.catalog.capability.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-selector.catalog.options.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-selector.catalog.read.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-selector.catalog.dropdown.ps1")

function Add-SteamVersionSelectionBranchSelectorCatalogChecks {
    Add-SteamVersionSelectionBranchSelectorCatalogCapabilityChecks

    Add-SteamVersionSelectionBranchSelectorCatalogOptionChecks

    Add-SteamVersionSelectionBranchSelectorCatalogReadChecks

    Add-SteamVersionSelectionBranchSelectorCatalogDropdownChecks
}
