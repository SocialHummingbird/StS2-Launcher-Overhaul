. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-docs.shell.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-docs.native-proof.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-docs.workflow-actions.ps1")

function Add-SteamVersionSelectionSupportDocsLoginValidationBoundaryChecks {
    Add-SteamVersionSelectionSupportDocsLoginValidationShellBoundaryChecks

    Add-SteamVersionSelectionSupportDocsLoginValidationNativeProofBoundaryChecks

    Add-SteamVersionSelectionSupportDocsLoginValidationWorkflowActionBoundaryChecks
}
