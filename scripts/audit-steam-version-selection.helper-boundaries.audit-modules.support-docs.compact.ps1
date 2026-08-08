. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.compact.labels.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.compact.section-setup.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.compact.safe-flow.ps1")

function Add-SteamVersionSelectionSupportDocsCompactBoundaryChecks {
    Add-SteamVersionSelectionSupportDocsCompactLabelBoundaryChecks

    Add-SteamVersionSelectionSupportDocsCompactSectionSetupBoundaryChecks

    Add-SteamVersionSelectionSupportDocsCompactSafeFlowBoundaryChecks
}
