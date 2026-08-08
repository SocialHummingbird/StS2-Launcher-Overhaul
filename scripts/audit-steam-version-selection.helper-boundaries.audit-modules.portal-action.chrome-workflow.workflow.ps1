. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.compact.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.code.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.section-flow.ps1")

function Add-SteamVersionSelectionPortalActionWorkflowBoundaryChecks {
    Add-SteamVersionSelectionPortalActionCompactWorkflowBoundaryChecks
    Add-SteamVersionSelectionPortalActionCodeSectionBoundaryChecks
    Add-SteamVersionSelectionPortalActionSectionFlowBoundaryChecks
}
