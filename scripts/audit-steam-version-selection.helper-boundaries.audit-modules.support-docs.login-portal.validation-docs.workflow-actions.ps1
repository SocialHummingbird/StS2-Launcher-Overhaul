function Add-SteamVersionSelectionSupportDocsLoginValidationWorkflowActionBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.login-validation-docs.portal-workflow.ps1" `
        "keeps portal status, workflow, and auth layout documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionLoginValidationDocsPortalWorkflowChecks",
            "Launcher portal UX model",
            "Launcher compact plain-language status copy supported",
            "Launcher compact sticky workflow step strip supported",
            "Launcher compact Android login primary CTA supported"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-validation-docs.compact-actions.ps1" `
        "keeps compact install, ready-state, cloud, and support action documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionLoginValidationDocsCompactActionChecks",
            "Launcher compact install primary detail label supported",
            "Launcher compact ready-version summary panel supported",
            "Launcher safer Pull-before-Push cloud ordering supported",
            "Launcher compact support tools grid supported"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-validation-docs.validation-boundary.ps1" `
        "keeps login validation boundary and remaining-risk documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionLoginValidationDocsValidationBoundaryChecks",
            "Launcher portal UX validation boundary",
            "SteamKit debug logs sanitized for credentials/tokens",
            "not complete until ARM64 evidence covers"
        )
}
