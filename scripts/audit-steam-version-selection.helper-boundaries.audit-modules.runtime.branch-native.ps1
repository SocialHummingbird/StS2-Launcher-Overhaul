. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.branch-native.branch-runtime.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.branch-native.native-routing.ps1")

function Add-SteamVersionSelectionRuntimeBranchNativeBoundaryChecks {
    Add-SteamVersionSelectionRuntimeBranchBoundaryChecks

    Add-SteamVersionSelectionRuntimeNativeRoutingBoundaryChecks
}
