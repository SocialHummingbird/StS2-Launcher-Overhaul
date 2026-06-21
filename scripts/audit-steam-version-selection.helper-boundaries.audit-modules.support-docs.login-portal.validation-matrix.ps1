function Add-SteamVersionSelectionSupportDocsLoginPortalValidationMatrixBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.ps1" `
        "keeps native credential, validation matrix, feature-support, and signoff evidence-template checks behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixChecks",
            "audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.native-credential.ps1",
            "audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.portal-workflow.ps1",
            "audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.compact-actions.ps1",
            "audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.signoff.ps1",
            "Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixNativeCredentialChecks",
            "Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixPortalWorkflowChecks",
            "Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixCompactActionChecks",
            "Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixSignoffChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.native-credential.ps1" `
        "keeps native credential and Steam Guard validation evidence-template checks focused" `
        @(
            "function Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixNativeCredentialChecks",
            "Native credential panel inline status configured",
            "Steam Guard one-shot code guidance supported",
            "Context-specific login recovery guidance supported"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.portal-workflow.ps1" `
        "keeps launcher portal status, workflow, and responsive layout validation evidence-template checks focused" `
        @(
            "function Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixPortalWorkflowChecks",
            "Launcher portal UX model",
            "Launcher compact sticky workflow step strip supported",
            "Launcher viewport-aware sticky task header reflow supported"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.compact-actions.ps1" `
        "keeps compact install, login action, cloud-safety, and support action validation evidence-template checks focused" `
        @(
            "function Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixCompactActionChecks",
            "Launcher compact selected-version headline supported",
            "Launcher compact ready-state cloud options below launch supported",
            "Launcher version-install/cloud-save separation guidance supported"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.signoff.ps1" `
        "keeps sanitized log and release signoff evidence-template checks focused" `
        @(
            "function Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixSignoffChecks",
            "SteamKit debug logs sanitized for credentials/tokens",
            "Release signoff is not valid"
        )
}
