. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-workflow.sticky-header.placement.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-workflow.sticky-header.button.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-workflow.sticky-header.layout.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-workflow.sticky-header.style.ps1")

function Add-SteamVersionSelectionCompactWorkflowStickyHeaderChecks {
    Add-SteamVersionSelectionCompactWorkflowStickyHeaderPlacementChecks
    Add-SteamVersionSelectionCompactWorkflowStickyHeaderButtonChecks
    Add-SteamVersionSelectionCompactWorkflowStickyHeaderLayoutChecks
    Add-SteamVersionSelectionCompactWorkflowStickyHeaderStyleChecks
}
