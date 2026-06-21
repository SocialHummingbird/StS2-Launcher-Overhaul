function Add-SteamVersionSelectionSupportDocsBetaCoreBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.beta-integrity.ps1" `
        "keeps beta branch integrity evidence audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionBetaIntegrityChecks",
            "audit-steam-version-selection.beta-integrity.release-readiness.ps1",
            "audit-steam-version-selection.beta-integrity.capture-review.ps1",
            "audit-steam-version-selection.beta-integrity.evidence-docs.ps1",
            "audit-steam-version-selection.beta-integrity.issue-template.ps1",
            "Add-SteamVersionSelectionBetaIntegrityReleaseReadinessChecks",
            "Add-SteamVersionSelectionBetaIntegrityCaptureReviewChecks",
            "Add-SteamVersionSelectionBetaIntegrityEvidenceDocChecks",
            "Add-SteamVersionSelectionBetaIntegrityIssueTemplateChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.beta-integrity.release-readiness.ps1" `
        "keeps beta branch release-readiness integrity blockers focused" `
        @(
            "function Add-SteamVersionSelectionBetaIntegrityReleaseReadinessChecks",
            "steam-version-selection-release-readiness.md",
            "Beta branch integrity",
            "Runtime patching now falls back to branch-local"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.beta-integrity.capture-review.ps1" `
        "keeps beta branch capture and summary-review tooling checks focused" `
        @(
            "function Add-SteamVersionSelectionBetaIntegrityCaptureReviewChecks",
            "capture-steam-beta-integrity-evidence.ps1",
            "review-beta-integrity-summary.ps1",
            "FailOnNotReady",
            "Classification inputs:"
        )
}
