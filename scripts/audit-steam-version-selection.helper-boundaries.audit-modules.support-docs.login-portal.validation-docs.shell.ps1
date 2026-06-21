function Add-SteamVersionSelectionSupportDocsLoginValidationShellBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.login-validation-docs.ps1" `
        "keeps Android login validation documentation checks behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionLoginValidationDocsChecks",
            "audit-steam-version-selection.login-validation-docs.native-proof.ps1",
            "audit-steam-version-selection.login-validation-docs.portal-workflow.ps1",
            "audit-steam-version-selection.login-validation-docs.compact-actions.ps1",
            "audit-steam-version-selection.login-validation-docs.validation-boundary.ps1",
            "Add-SteamVersionSelectionLoginValidationDocsNativeProofChecks",
            "Add-SteamVersionSelectionLoginValidationDocsPortalWorkflowChecks",
            "Add-SteamVersionSelectionLoginValidationDocsCompactActionChecks",
            "Add-SteamVersionSelectionLoginValidationDocsValidationBoundaryChecks"
        )
}
