. (Join-Path $PSScriptRoot "audit-steam-version-selection.code-section.construction.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.code-section.controls.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.code-section.responsive.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.code-section.submission.ps1")

function Add-SteamVersionSelectionCodeSectionChecks {
    Add-SteamVersionSelectionCodeSectionConstructionChecks

    Add-SteamVersionSelectionCodeSectionControlChecks

    Add-SteamVersionSelectionCodeSectionResponsiveChecks

    Add-SteamVersionSelectionCodeSectionSubmissionChecks
}
