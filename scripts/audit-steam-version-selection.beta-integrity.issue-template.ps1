. (Join-Path $PSScriptRoot "audit-steam-version-selection.beta-integrity.issue-template.hygiene.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.beta-integrity.issue-template.branch-cache.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.beta-integrity.issue-template.cloud-markers.ps1")

function Add-SteamVersionSelectionBetaIntegrityIssueTemplateChecks {
    Add-SteamVersionSelectionBetaIntegrityIssueTemplateHygieneChecks
    Add-SteamVersionSelectionBetaIntegrityIssueTemplateBranchCacheChecks
    Add-SteamVersionSelectionBetaIntegrityIssueTemplateCloudMarkerChecks
}
