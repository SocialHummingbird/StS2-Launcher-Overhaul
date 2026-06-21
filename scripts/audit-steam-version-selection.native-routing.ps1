. (Join-Path $PSScriptRoot "audit-steam-version-selection.native-routing.startup-provenance.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.native-routing.branch-info.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.native-routing.fallback.ps1")

function Add-SteamVersionSelectionNativeRoutingChecks {
    Add-SteamVersionSelectionNativeRoutingStartupProvenanceChecks

    Add-SteamVersionSelectionNativeRoutingBranchInfoChecks

    Add-SteamVersionSelectionNativeRoutingFallbackChecks
}
