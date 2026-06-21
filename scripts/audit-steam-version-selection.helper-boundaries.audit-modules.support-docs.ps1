. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.compact.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.ps1")

function Add-SteamVersionSelectionSupportDocsAuditModuleBoundaryChecks {
    Add-SteamVersionSelectionSupportDocsCompactBoundaryChecks

    Add-SteamVersionSelectionSupportDocsDiagnosticsBoundaryChecks

    Add-SteamVersionSelectionSupportDocsLoginPortalBoundaryChecks

    Add-SteamVersionSelectionSupportDocsReleaseBetaBoundaryChecks
}
