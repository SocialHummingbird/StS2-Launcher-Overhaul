. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-runtime.cache-safety.redownload.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-runtime.cache-safety.cleanup-markers.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-runtime.cache-safety.runtime-packs.ps1")

function Add-SteamVersionSelectionBranchRuntimeCacheSafetyChecks {
    Add-SteamVersionSelectionBranchRuntimeRedownloadCleanupChecks

    Add-SteamVersionSelectionBranchRuntimeCacheCleanupMarkerChecks

    Add-SteamVersionSelectionBranchRuntimePackCleanupChecks
}
