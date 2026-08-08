. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-ux-status.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-ux-flags.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-ux-narrative.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-ux-features.ps1")

function Add-SteamVersionSelectionPortalUxSupportChecks {
    Add-SteamVersionSelectionPortalUxStatusFormatterChecks

    Add-SteamVersionSelectionPortalUxFlagChecks

    Add-SteamVersionSelectionPortalUxNarrativeChecks

    Add-SteamVersionSelectionPortalUxFeatureChecks
}
