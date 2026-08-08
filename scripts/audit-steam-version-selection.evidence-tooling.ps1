. (Join-Path $PSScriptRoot "audit-steam-version-selection.evidence-tooling.capture.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.evidence-tooling.scaffold.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.evidence-tooling.redaction.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.evidence-tooling.docs.ps1")

function Add-SteamVersionSelectionEvidenceToolingChecks {
    Add-SteamVersionSelectionEvidenceToolingCaptureChecks

    Add-SteamVersionSelectionEvidenceToolingScaffoldChecks

    Add-SteamVersionSelectionEvidenceToolingRedactionChecks

    Add-SteamVersionSelectionEvidenceToolingDocsChecks
}
