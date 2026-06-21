function Add-SteamVersionSelectionSupportDocsReleaseDocsRunbookUserBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.release-docs.runbook-user.ps1" `
        "keeps runbook and tester-facing user-guide documentation checks behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionReleaseDocsRunbookUserChecks",
            "audit-steam-version-selection.release-docs.runbook-user.runbook.ps1",
            "audit-steam-version-selection.release-docs.runbook-user.support.ps1",
            "audit-steam-version-selection.release-docs.runbook-user.branch-cloud.ps1",
            "audit-steam-version-selection.release-docs.runbook-user.beta-integrity.ps1",
            "audit-steam-version-selection.release-docs.runbook-user.artifact-hygiene.ps1",
            "Add-SteamVersionSelectionReleaseDocsRunbookChecks",
            "Add-SteamVersionSelectionReleaseDocsUserGuideSupportChecks",
            "Add-SteamVersionSelectionReleaseDocsUserGuideBranchCloudChecks",
            "Add-SteamVersionSelectionReleaseDocsUserGuideBetaIntegrityChecks",
            "Add-SteamVersionSelectionReleaseDocsUserGuideArtifactHygieneChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.release-docs.runbook-user.runbook.ps1" `
        "keeps release runbook documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionReleaseDocsRunbookChecks",
            "steam-version-selection-runbook.md",
            "Pre-Push backup evidence"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.release-docs.runbook-user.support.ps1" `
        "keeps tester-facing support and version setup documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionReleaseDocsUserGuideSupportChecks",
            "steam-version-selection-user-guide.md",
            "Android credential provider capability boundary"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.release-docs.runbook-user.branch-cloud.ps1" `
        "keeps tester-facing branch-switch and manual cloud marker documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionReleaseDocsUserGuideBranchCloudChecks",
            "steam-version-selection-user-guide.md",
            "Manual Push blocked before upload evidence recorded"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.release-docs.runbook-user.beta-integrity.ps1" `
        "keeps tester-facing beta integrity interpretation documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionReleaseDocsUserGuideBetaIntegrityChecks",
            "steam-version-selection-user-guide.md",
            "Public-vs-beta key asset comparison captured"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.release-docs.runbook-user.artifact-hygiene.ps1" `
        "keeps tester-facing artifact hygiene documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionReleaseDocsUserGuideArtifactHygieneChecks",
            "steam-version-selection-user-guide.md",
            "Steam credentials"
        )
}
