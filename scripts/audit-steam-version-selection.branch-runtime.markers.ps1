. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-runtime.markers.diagnostics.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-runtime.markers.fields.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-runtime.markers.readiness.ps1")

function Add-SteamVersionSelectionBranchRuntimeMarkerChecks {
    Add-SteamVersionSelectionBranchRuntimeMarkerDiagnosticsChecks
    Add-SteamVersionSelectionBranchRuntimeMarkerFieldChecks
    Add-SteamVersionSelectionBranchRuntimeMarkerReadinessChecks
}
