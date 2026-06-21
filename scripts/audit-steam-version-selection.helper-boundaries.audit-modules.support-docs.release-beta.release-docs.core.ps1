function Add-SteamVersionSelectionSupportDocsReleaseDocsCoreBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.release-docs.ps1" `
        "keeps Steam version-selection release/readiness documentation audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionReleaseDocsChecks",
            "audit-steam-version-selection.release-docs.overview.ps1",
            "audit-steam-version-selection.release-docs.completion.ps1",
            "audit-steam-version-selection.release-docs.runbook-user.ps1",
            "audit-steam-version-selection.release-docs.signoff.ps1",
            "Add-SteamVersionSelectionReleaseDocsOverviewChecks",
            "Add-SteamVersionSelectionReleaseDocsCompletionChecks",
            "Add-SteamVersionSelectionReleaseDocsRunbookUserChecks",
            "Add-SteamVersionSelectionReleaseDocsSignoffChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.release-docs.overview.ps1" `
        "keeps release validation, readiness, and architecture documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionReleaseDocsOverviewChecks",
            "steam-version-selection-validation.md",
            "steam-version-selection-release-readiness.md",
            "steam-version-selection-architecture.md"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.release-docs.completion.ps1" `
        "keeps completion-audit documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionReleaseDocsCompletionChecks",
            "steam-version-selection-completion-audit.md",
            "Add-ForbiddenCheck",
            "Do not mark Steam beta/version selection release-ready yet"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.release-docs.signoff.ps1" `
        "keeps save-compatibility, roadmap, and Android signoff documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionReleaseDocsSignoffChecks",
            "steam-version-selection-save-compatibility.md",
            "OVERHAUL_ROADMAP.md",
            "android-release-validation.md"
        )
}
