. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-validation-docs.native-proof.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-validation-docs.portal-workflow.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-validation-docs.compact-actions.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-validation-docs.validation-boundary.ps1")

function Add-SteamVersionSelectionLoginValidationDocsChecks {
    Add-SteamVersionSelectionLoginValidationDocsNativeProofChecks

    Add-SteamVersionSelectionLoginValidationDocsPortalWorkflowChecks

    Add-SteamVersionSelectionLoginValidationDocsCompactActionChecks

    Add-SteamVersionSelectionLoginValidationDocsValidationBoundaryChecks
}
