. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.chrome-status.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.compact-install.ps1")

function Add-SteamVersionSelectionPortalActionChromeWorkflowBoundaryChecks {
    Add-SteamVersionSelectionPortalActionChromeStatusBoundaryChecks

    Add-SteamVersionSelectionPortalActionWorkflowBoundaryChecks

    Add-SteamVersionSelectionPortalActionCompactInstallBoundaryChecks
}
