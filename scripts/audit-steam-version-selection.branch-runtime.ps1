. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-runtime.depots.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-runtime.markers.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-runtime.cache-safety.ps1")

function Add-SteamVersionSelectionBranchRuntimeChecks {
    Add-SteamVersionSelectionBranchRuntimeDepotChecks

    Add-SteamVersionSelectionBranchRuntimeMarkerChecks

    Add-SteamVersionSelectionBranchRuntimeCacheSafetyChecks
}
