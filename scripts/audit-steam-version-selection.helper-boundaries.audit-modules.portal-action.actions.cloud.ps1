. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.cloud.controls.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.cloud.safety.ps1")

function Add-SteamVersionSelectionPortalActionReadyActionCloudBoundaryChecks {
    Add-SteamVersionSelectionPortalActionReadyActionCloudControlBoundaryChecks

    Add-SteamVersionSelectionPortalActionReadyActionCloudSafetyBoundaryChecks
}
