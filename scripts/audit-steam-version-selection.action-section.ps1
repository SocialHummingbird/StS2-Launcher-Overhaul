. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-core.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-support.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-cloud.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-visibility.ps1")

function Add-SteamVersionSelectionActionSectionChecks {
    Add-SteamVersionSelectionActionCoreChecks

    Add-SteamVersionSelectionActionSupportChecks

    Add-SteamVersionSelectionActionCloudControlsChecks

    Add-SteamVersionSelectionActionVisibilityChecks

    Add-SteamVersionSelectionActionCloudSafetyChecks

    Add-SteamVersionSelectionActionSupportDiagnosticsChecks
}