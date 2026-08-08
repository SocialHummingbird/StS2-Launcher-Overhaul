. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-panel.support.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-panel.managed-section.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-panel.native-android.ps1")

function Add-SteamVersionSelectionLoginPanelChecks {
    Add-SteamVersionSelectionLoginPanelSupportChecks

    Add-SteamVersionSelectionLoginPanelManagedSectionChecks

    Add-SteamVersionSelectionLoginPanelNativeAndroidChecks
}
