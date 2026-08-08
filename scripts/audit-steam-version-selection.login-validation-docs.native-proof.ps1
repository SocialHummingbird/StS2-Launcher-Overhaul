. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-validation-docs.native-proof.core.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-validation-docs.native-proof.compact-status.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-validation-docs.native-proof.compact-actions.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-validation-docs.native-proof.task-header.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-validation-docs.native-proof.evidence-matrix.ps1")

function Add-SteamVersionSelectionLoginValidationDocsNativeProofChecks {
    Add-SteamVersionSelectionLoginValidationDocsNativeProofCoreChecks

    Add-SteamVersionSelectionLoginValidationDocsNativeProofCompactStatusChecks

    Add-SteamVersionSelectionLoginValidationDocsNativeProofCompactActionChecks

    Add-SteamVersionSelectionLoginValidationDocsNativeProofTaskHeaderChecks

    Add-SteamVersionSelectionLoginValidationDocsNativeProofEvidenceMatrixChecks
}
