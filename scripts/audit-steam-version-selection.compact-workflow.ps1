. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-workflow.strip.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-workflow.sticky-header.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-workflow.state.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-workflow.sections.ps1")

function Add-SteamVersionSelectionCompactWorkflowChecks {
    Add-SteamVersionSelectionCompactWorkflowStripChecks

    Add-SteamVersionSelectionCompactWorkflowStickyHeaderChecks

    Add-SteamVersionSelectionCompactWorkflowStateChecks

    Add-SteamVersionSelectionCompactWorkflowSectionChecks
}
