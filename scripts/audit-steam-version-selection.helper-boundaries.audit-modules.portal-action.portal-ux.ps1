. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.support.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.flags.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.features.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.branch-availability.ps1")

function Add-SteamVersionSelectionPortalActionPortalUxBoundaryChecks {
    Add-SteamVersionSelectionPortalActionPortalUxSupportBoundaryChecks

    Add-SteamVersionSelectionPortalActionPortalUxFlagBoundaryChecks

    Add-SteamVersionSelectionPortalActionPortalUxFeatureBoundaryChecks

    Add-SteamVersionSelectionPortalActionBranchAvailabilityBoundaryChecks
}
