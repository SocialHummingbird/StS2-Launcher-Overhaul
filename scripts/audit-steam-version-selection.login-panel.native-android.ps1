. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-panel.native-android.fields.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-panel.native-android.layout.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-panel.native-android.security.ps1")

function Add-SteamVersionSelectionLoginPanelNativeAndroidChecks {
    Add-SteamVersionSelectionLoginPanelNativeAndroidFieldChecks

    Add-SteamVersionSelectionLoginPanelNativeAndroidLayoutChecks

    Add-SteamVersionSelectionLoginPanelNativeAndroidSecurityChecks
}
