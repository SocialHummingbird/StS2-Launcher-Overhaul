. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.subcategories.portal-action.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.subcategories.support-docs.ps1")

function Add-SteamVersionSelectionAuditModuleSubcategoryBoundaryChecks {
    Add-SteamVersionSelectionPortalActionSubcategoryBoundaryChecks

    Add-SteamVersionSelectionSupportDocsSubcategoryBoundaryChecks
}
