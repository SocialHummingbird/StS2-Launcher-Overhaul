. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-section-flow.visibility.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-section-flow.scrolling.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-section-flow.reanchor.ps1")

function Add-SteamVersionSelectionCompactSectionFlowChecks {
    Add-SteamVersionSelectionCompactSectionFlowVisibilityChecks
    Add-SteamVersionSelectionCompactSectionFlowScrollingChecks
    Add-SteamVersionSelectionCompactSectionFlowReanchorChecks
}
