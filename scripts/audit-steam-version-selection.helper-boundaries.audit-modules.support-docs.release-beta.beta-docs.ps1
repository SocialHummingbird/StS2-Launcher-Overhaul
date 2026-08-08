function Add-SteamVersionSelectionSupportDocsBetaDocBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.beta-integrity.evidence-docs.ps1" `
        "keeps beta branch tooling, checklist, and evidence-template checks focused" `
        @(
            "function Add-SteamVersionSelectionBetaIntegrityEvidenceDocChecks",
            "steam-version-selection-tooling.md",
            "steam-beta-integrity-runtime-checklist.md",
            "steam-version-selection-evidence-template.md",
            "new-steam-version-selection-evidence.ps1"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.beta-integrity.issue-template.ps1" `
        "keeps public beta-integrity issue-template checks behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionBetaIntegrityIssueTemplateChecks",
            "audit-steam-version-selection.beta-integrity.issue-template.hygiene.ps1",
            "audit-steam-version-selection.beta-integrity.issue-template.branch-cache.ps1",
            "audit-steam-version-selection.beta-integrity.issue-template.cloud-markers.ps1",
            "Add-SteamVersionSelectionBetaIntegrityIssueTemplateHygieneChecks",
            "Add-SteamVersionSelectionBetaIntegrityIssueTemplateBranchCacheChecks",
            "Add-SteamVersionSelectionBetaIntegrityIssueTemplateCloudMarkerChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.beta-integrity.issue-template.hygiene.ps1" `
        "keeps public beta-integrity issue-template hygiene checks focused" `
        @(
            "function Add-SteamVersionSelectionBetaIntegrityIssueTemplateHygieneChecks",
            "Public-vs-beta depot manifest integrity",
            "Public-vs-beta file inventory"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.beta-integrity.issue-template.branch-cache.ps1" `
        "keeps public beta-integrity issue-template branch/cache checks focused" `
        @(
            "function Add-SteamVersionSelectionBetaIntegrityIssueTemplateBranchCacheChecks",
            "Game version redownload marker filename",
            "Selected game branch marker depot manifest rows"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.beta-integrity.issue-template.cloud-markers.ps1" `
        "keeps public beta-integrity issue-template manual cloud marker checks focused" `
        @(
            "function Add-SteamVersionSelectionBetaIntegrityIssueTemplateCloudMarkerChecks",
            "Manual Push blocked before upload evidence recorded"
        )
}
