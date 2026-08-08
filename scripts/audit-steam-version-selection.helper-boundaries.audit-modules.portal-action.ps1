. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.ps1")

function Add-SteamVersionSelectionPortalActionAuditModuleBoundaryChecks {
    Add-SteamVersionSelectionPortalActionChromeWorkflowBoundaryChecks

    Add-SteamVersionSelectionPortalActionStartupBoundaryChecks

    Add-SteamVersionSelectionPortalActionReadyActionBoundaryChecks

    Add-SteamVersionSelectionPortalActionPortalUxBoundaryChecks
}
