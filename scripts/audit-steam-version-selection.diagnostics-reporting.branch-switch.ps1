. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-reporting.branch-switch.shell.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-reporting.branch-switch.marker.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-reporting.branch-switch.pull.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-reporting.branch-switch.save-origin.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-reporting.branch-switch.push.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-reporting.branch-switch.backup.ps1")

function Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchChecks {
    Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchShellChecks

    Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchMarkerChecks

    Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchPullChecks

    Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchSaveOriginChecks

    Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchPushChecks

    Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchBackupChecks
}
