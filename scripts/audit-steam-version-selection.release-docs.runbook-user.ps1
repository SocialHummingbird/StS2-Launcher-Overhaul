. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-docs.runbook-user.runbook.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-docs.runbook-user.support.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-docs.runbook-user.branch-cloud.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-docs.runbook-user.beta-integrity.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-docs.runbook-user.artifact-hygiene.ps1")

function Add-SteamVersionSelectionReleaseDocsRunbookUserChecks {
    Add-SteamVersionSelectionReleaseDocsRunbookChecks
    Add-SteamVersionSelectionReleaseDocsUserGuideSupportChecks
    Add-SteamVersionSelectionReleaseDocsUserGuideBranchCloudChecks
    Add-SteamVersionSelectionReleaseDocsUserGuideBetaIntegrityChecks
    Add-SteamVersionSelectionReleaseDocsUserGuideArtifactHygieneChecks
}
