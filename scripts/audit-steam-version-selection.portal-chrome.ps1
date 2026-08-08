. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-chrome.shell-brand.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-chrome.compact-layout.ps1")

function Add-SteamVersionSelectionPortalChromeChecks {
    Add-SteamVersionSelectionPortalChromeShellBrandChecks
    Add-SteamVersionSelectionPortalChromeCompactLayoutChecks
}
