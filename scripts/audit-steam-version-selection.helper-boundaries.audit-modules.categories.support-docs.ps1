function Add-SteamVersionSelectionSupportDocsCategoryBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.ps1" `
        "keeps support/docs boundary inventory behind focused support documentation modules" `
        @(
            "function Add-SteamVersionSelectionSupportDocsAuditModuleBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.compact.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.ps1",
            "Add-SteamVersionSelectionSupportDocsCompactBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsDiagnosticsBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsLoginPortalBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsReleaseBetaBoundaryChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.release-docs.ps1" `
        "keeps release documentation support boundary checks delegated to focused child boundary modules" `
        @(
            "function Add-SteamVersionSelectionSupportDocsReleaseDocsBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.release-docs.core.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.release-docs.runbook-user.ps1",
            "Add-SteamVersionSelectionSupportDocsReleaseDocsCoreBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsReleaseDocsRunbookUserBoundaryChecks"
        )
}
