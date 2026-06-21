. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-behavior.debug-logs.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-behavior.compact-guidance.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-behavior.keyboard.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-behavior.chrome.ps1")

function Add-SteamVersionSelectionPortalBehaviorChecks {
    Add-SteamVersionSelectionPortalBehaviorDebugLogChecks
    Add-SteamVersionSelectionPortalBehaviorCompactGuidanceChecks
    Add-SteamVersionSelectionPortalBehaviorKeyboardChecks
    Add-SteamVersionSelectionPortalBehaviorChromeChecks
}
