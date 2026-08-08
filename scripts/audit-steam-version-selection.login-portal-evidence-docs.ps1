. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-portal-evidence-docs.auth-status.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-portal-evidence-docs.compact-workflow.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-portal-evidence-docs.install-cloud.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.ps1")

function Add-SteamVersionSelectionLoginPortalEvidenceDocsChecks {
    Add-SteamVersionSelectionLoginPortalEvidenceDocsAuthStatusChecks

    Add-SteamVersionSelectionLoginPortalEvidenceDocsCompactWorkflowChecks

    Add-SteamVersionSelectionLoginPortalEvidenceDocsInstallCloudChecks

    Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixChecks
}
