. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.inventory.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.categories.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.subcategories.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.ps1")

function Add-SteamVersionSelectionAuditModuleBoundaryChecks {
    Add-SteamVersionSelectionAuditModuleInventoryBoundaryChecks

    Add-SteamVersionSelectionAuditModuleCategoryBoundaryChecks

    Add-SteamVersionSelectionAuditModuleSubcategoryBoundaryChecks

    Add-SteamVersionSelectionRuntimeAuditModuleBoundaryChecks

    Add-SteamVersionSelectionAuthCloudAuditModuleBoundaryChecks

    Add-SteamVersionSelectionSupportDocsAuditModuleBoundaryChecks

    Add-SteamVersionSelectionPortalActionAuditModuleBoundaryChecks
}
