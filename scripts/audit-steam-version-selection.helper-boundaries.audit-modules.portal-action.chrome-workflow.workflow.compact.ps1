. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.compact.shell.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.compact.strip.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.compact.sticky.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.compact.state-sections.ps1")

function Add-SteamVersionSelectionPortalActionCompactWorkflowBoundaryChecks {
    Add-SteamVersionSelectionPortalActionCompactWorkflowShellBoundaryChecks
    Add-SteamVersionSelectionPortalActionCompactWorkflowStripBoundaryChecks
    Add-SteamVersionSelectionPortalActionCompactWorkflowStickyBoundaryChecks
    Add-SteamVersionSelectionPortalActionCompactWorkflowStateSectionBoundaryChecks
}
