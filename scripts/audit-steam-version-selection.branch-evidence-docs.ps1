. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-evidence-docs.selector.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-evidence-docs.cloud-push.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-evidence-docs.artifact-hygiene.ps1")

function Add-SteamVersionSelectionBranchEvidenceDocsChecks {
    Add-SteamVersionSelectionBranchEvidenceSelectorChecks

    Add-SteamVersionSelectionBranchEvidenceCloudPushChecks

    Add-SteamVersionSelectionBranchEvidenceArtifactHygieneChecks
}
