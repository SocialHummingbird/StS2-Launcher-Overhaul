. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-selector.storage.paths.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-selector.storage.preferences.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-selector.storage.cloud.ps1")

function Add-SteamVersionSelectionBranchSelectorStorageChecks {
    Add-SteamVersionSelectionBranchSelectorStoragePathChecks

    Add-SteamVersionSelectionBranchSelectorStoragePreferenceChecks

    Add-SteamVersionSelectionBranchSelectorStorageCloudPreferenceChecks
}
