. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-drawer.shell.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-drawer.primary-column.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-drawer.sizing.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-drawer.actions.ps1")

function Add-SteamVersionSelectionDiagnosticsDrawerChecks {
    Add-SteamVersionSelectionDiagnosticsDrawerShellChecks

    Add-SteamVersionSelectionDiagnosticsDrawerPrimaryColumnChecks

    Add-SteamVersionSelectionDiagnosticsDrawerSizingChecks

    Add-SteamVersionSelectionDiagnosticsDrawerActionChecks
}
