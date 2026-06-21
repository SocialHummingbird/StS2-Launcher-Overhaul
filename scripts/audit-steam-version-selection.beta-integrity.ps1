. (Join-Path $PSScriptRoot "audit-steam-version-selection.beta-integrity.release-readiness.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.beta-integrity.capture-review.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.beta-integrity.evidence-docs.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.beta-integrity.issue-template.ps1")

function Add-SteamVersionSelectionBetaIntegrityChecks {
    Add-SteamVersionSelectionBetaIntegrityReleaseReadinessChecks

    Add-SteamVersionSelectionBetaIntegrityCaptureReviewChecks

    Add-SteamVersionSelectionBetaIntegrityEvidenceDocChecks

    Add-SteamVersionSelectionBetaIntegrityIssueTemplateChecks
}
