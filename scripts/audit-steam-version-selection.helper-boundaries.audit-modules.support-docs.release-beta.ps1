. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.release-docs.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.beta-core.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.beta-docs.ps1")

function Add-SteamVersionSelectionSupportDocsReleaseBetaBoundaryChecks {
    Add-SteamVersionSelectionSupportDocsReleaseDocsBoundaryChecks
    Add-SteamVersionSelectionSupportDocsBetaCoreBoundaryChecks
    Add-SteamVersionSelectionSupportDocsBetaDocBoundaryChecks
}
