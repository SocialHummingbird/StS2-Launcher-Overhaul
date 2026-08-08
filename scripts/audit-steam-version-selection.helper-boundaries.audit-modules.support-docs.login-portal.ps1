. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-docs.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.evidence-docs.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-matrix.ps1")

function Add-SteamVersionSelectionSupportDocsLoginPortalBoundaryChecks {
    Add-SteamVersionSelectionSupportDocsLoginValidationBoundaryChecks

    Add-SteamVersionSelectionSupportDocsLoginPortalEvidenceBoundaryChecks

    Add-SteamVersionSelectionSupportDocsLoginPortalValidationMatrixBoundaryChecks
}
