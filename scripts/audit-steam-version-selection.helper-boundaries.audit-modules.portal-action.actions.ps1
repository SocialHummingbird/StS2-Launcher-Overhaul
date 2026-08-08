. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.core.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.support.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.cloud.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.visibility.ps1")

function Add-SteamVersionSelectionPortalActionReadyActionBoundaryChecks {
    Add-SteamVersionSelectionPortalActionReadyActionCoreBoundaryChecks

    Add-SteamVersionSelectionPortalActionReadyActionSupportBoundaryChecks

    Add-SteamVersionSelectionPortalActionReadyActionCloudBoundaryChecks

    Add-SteamVersionSelectionPortalActionReadyActionVisibilityBoundaryChecks
}
