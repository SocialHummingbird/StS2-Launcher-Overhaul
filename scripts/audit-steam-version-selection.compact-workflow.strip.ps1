. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-workflow.strip.shell.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-workflow.strip.cells.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-workflow.strip.style-navigation.ps1")

function Add-SteamVersionSelectionCompactWorkflowStripChecks {
    Add-SteamVersionSelectionCompactWorkflowStripShellChecks
    Add-SteamVersionSelectionCompactWorkflowStripCellChecks
    Add-SteamVersionSelectionCompactWorkflowStripStyleNavigationChecks
}
