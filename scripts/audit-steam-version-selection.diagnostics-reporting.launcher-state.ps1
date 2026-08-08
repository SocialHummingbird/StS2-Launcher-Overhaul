. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-reporting.launcher-state.preferences.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-reporting.launcher-state.branch-availability.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-reporting.launcher-state.cached-versions.ps1")

function Add-SteamVersionSelectionDiagnosticsReportingLauncherStateChecks {
    Add-SteamVersionSelectionDiagnosticsReportingLauncherPreferenceChecks

    Add-SteamVersionSelectionDiagnosticsReportingBranchAvailabilityChecks

    Add-SteamVersionSelectionDiagnosticsReportingCachedVersionChecks
}
