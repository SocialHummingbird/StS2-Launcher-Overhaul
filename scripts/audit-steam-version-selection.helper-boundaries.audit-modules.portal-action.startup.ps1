. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.shell.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.shader.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.status.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.recovery.ps1")

function Add-SteamVersionSelectionPortalActionStartupBoundaryChecks {
    Add-SteamVersionSelectionPortalActionStartupShellBoundaryChecks

    Add-SteamVersionSelectionPortalActionStartupShaderBoundaryChecks

    Add-SteamVersionSelectionPortalActionStartupStatusBoundaryChecks

    Add-SteamVersionSelectionPortalActionStartupRecoveryBoundaryChecks
}
