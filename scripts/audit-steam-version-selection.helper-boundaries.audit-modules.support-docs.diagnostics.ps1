. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.drawer.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.reporting.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.branch-switch.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.evidence-tooling.ps1")

function Add-SteamVersionSelectionSupportDocsDiagnosticsBoundaryChecks {
    Add-SteamVersionSelectionSupportDocsDiagnosticsDrawerBoundaryChecks

    Add-SteamVersionSelectionSupportDocsDiagnosticsReportingBoundaryChecks

    Add-SteamVersionSelectionSupportDocsDiagnosticsBranchSwitchBoundaryChecks

    Add-SteamVersionSelectionSupportDocsEvidenceToolingBoundaryChecks
}
