. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.categories.runtime.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.categories.auth-cloud.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.categories.portal-action.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.categories.support-docs.ps1")

function Add-SteamVersionSelectionAuditModuleCategoryBoundaryChecks {
    Add-SteamVersionSelectionRuntimeCategoryBoundaryChecks

    Add-SteamVersionSelectionAuthCloudCategoryBoundaryChecks

    Add-SteamVersionSelectionPortalActionCategoryBoundaryChecks

    Add-SteamVersionSelectionSupportDocsCategoryBoundaryChecks
}
