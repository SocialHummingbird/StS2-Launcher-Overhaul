. (Join-Path $PSScriptRoot "audit-steam-version-selection.safe-flow-guide.panel.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.safe-flow-guide.steps.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.safe-flow-guide.toggle.ps1")

function Add-SteamVersionSelectionSafeFlowGuideChecks {
    Add-SteamVersionSelectionSafeFlowGuidePanelChecks
    Add-SteamVersionSelectionSafeFlowGuideStepChecks
    Add-SteamVersionSelectionSafeFlowGuideToggleChecks
}
