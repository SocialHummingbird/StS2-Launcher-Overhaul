. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-core.construction.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-core.button-styles.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-core.ready-summary.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-core.layout.ps1")

function Add-SteamVersionSelectionActionCoreChecks {
    Add-SteamVersionSelectionActionCoreConstructionChecks

    Add-SteamVersionSelectionActionCoreButtonStyleChecks

    Add-SteamVersionSelectionActionCoreReadySummaryChecks

    Add-SteamVersionSelectionActionCoreLayoutChecks
}
