. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-reporting.shell.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-reporting.launcher-state.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-reporting.branch-switch.ps1")

function Add-SteamVersionSelectionDiagnosticsReportingChecks {
    Add-SteamVersionSelectionDiagnosticsReportingShellChecks

    Add-SteamVersionSelectionDiagnosticsReportingLauncherStateChecks

    Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchChecks
}
