. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.release-docs.core.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.release-docs.runbook-user.ps1")

function Add-SteamVersionSelectionSupportDocsReleaseDocsBoundaryChecks {
    Add-SteamVersionSelectionSupportDocsReleaseDocsCoreBoundaryChecks

    Add-SteamVersionSelectionSupportDocsReleaseDocsRunbookUserBoundaryChecks
}
