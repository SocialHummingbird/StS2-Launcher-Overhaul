. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-cloud.controls.construction.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-cloud.controls.primary-actions.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-cloud.controls.copy.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-cloud.controls.layout.ps1")

function Add-SteamVersionSelectionActionCloudControlsChecks {
    Add-SteamVersionSelectionActionCloudControlConstructionChecks

    Add-SteamVersionSelectionActionCloudControlPrimaryActionChecks

    Add-SteamVersionSelectionActionCloudControlCopyChecks

    Add-SteamVersionSelectionActionCloudControlLayoutChecks
}
