. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.native-credential.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.portal-workflow.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.compact-actions.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.signoff.ps1")

function Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixChecks {
    Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixNativeCredentialChecks

    Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixPortalWorkflowChecks

    Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixCompactActionChecks

    Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixSignoffChecks
}
