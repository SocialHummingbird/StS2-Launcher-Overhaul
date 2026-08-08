function Add-SteamVersionSelectionSupportDocsSubcategoryBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.compact.ps1" `
        "keeps compact support/docs boundary inventory behind focused label, section, and safe-flow modules" `
        @(
            "function Add-SteamVersionSelectionSupportDocsCompactBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.compact.labels.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.compact.section-setup.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.compact.safe-flow.ps1",
            "Add-SteamVersionSelectionSupportDocsCompactLabelBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsCompactSectionSetupBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsCompactSafeFlowBoundaryChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.ps1" `
        "keeps diagnostics support/docs boundary inventory behind focused drawer, reporting, branch-switch, and tooling modules" `
        @(
            "function Add-SteamVersionSelectionSupportDocsDiagnosticsBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.drawer.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.reporting.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.branch-switch.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.evidence-tooling.ps1",
            "Add-SteamVersionSelectionSupportDocsDiagnosticsDrawerBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsDiagnosticsReportingBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsDiagnosticsBranchSwitchBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsEvidenceToolingBoundaryChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.ps1" `
        "keeps login portal support/docs boundary inventory behind focused validation, evidence, and signoff modules" `
        @(
            "function Add-SteamVersionSelectionSupportDocsLoginPortalBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-docs.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.evidence-docs.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-matrix.ps1",
            "Add-SteamVersionSelectionSupportDocsLoginValidationBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsLoginPortalEvidenceBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsLoginPortalValidationMatrixBoundaryChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-docs.ps1" `
        "keeps login validation support/docs boundary inventory behind focused child boundary modules" `
        @(
            "function Add-SteamVersionSelectionSupportDocsLoginValidationBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-docs.shell.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-docs.native-proof.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-docs.workflow-actions.ps1",
            "Add-SteamVersionSelectionSupportDocsLoginValidationShellBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsLoginValidationNativeProofBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsLoginValidationWorkflowActionBoundaryChecks"
        )
}
