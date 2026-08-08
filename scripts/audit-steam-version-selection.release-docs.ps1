. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-docs.overview.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-docs.completion.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-docs.runbook-user.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-docs.signoff.ps1")

function Add-SteamVersionSelectionReleaseDocsChecks {
    Add-SteamVersionSelectionReleaseDocsOverviewChecks

    Add-SteamVersionSelectionReleaseDocsCompletionChecks

    Add-SteamVersionSelectionReleaseDocsRunbookUserChecks

    Add-SteamVersionSelectionReleaseDocsSignoffChecks
}
