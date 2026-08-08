. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.branch-switch.cache.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.branch-switch.marker.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.branch-switch.gates.ps1")

function Add-SteamVersionSelectionCloudSafetyBranchSwitchChecks {
    Add-SteamVersionSelectionCloudSafetyBranchSwitchCacheChecks

    Add-SteamVersionSelectionCloudSafetyBranchSwitchMarkerChecks

    Add-SteamVersionSelectionCloudSafetyBranchSwitchGateChecks
}
