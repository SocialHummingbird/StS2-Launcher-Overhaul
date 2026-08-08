function Add-SteamVersionSelectionAuthCloudLoginPanelBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.login-panel.ps1" `
        "keeps native credential panel and login-section audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionLoginPanelChecks",
            "audit-steam-version-selection.login-panel.support.ps1",
            "audit-steam-version-selection.login-panel.managed-section.ps1",
            "audit-steam-version-selection.login-panel.native-android.ps1",
            "Add-SteamVersionSelectionLoginPanelSupportChecks",
            "Add-SteamVersionSelectionLoginPanelManagedSectionChecks",
            "Add-SteamVersionSelectionLoginPanelNativeAndroidChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-panel.support.ps1" `
        "keeps native credential capability and password-storage boundary audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionLoginPanelSupportChecks",
            "LauncherCredentialEntrySupport.cs",
            "NativeIntegratedCredentialPanelSupported",
            "NativeCredentialHandoffPopupSupported",
            "AppStoresSteamPassword"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-panel.managed-section.ps1" `
        "keeps managed login-section credential handoff audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionLoginPanelManagedSectionChecks",
            "LoginSection.Submission.cs",
            "LoginSection.NativePanel.cs",
            "LoginSection.Help.cs",
            "LoginSection.State.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-panel.native-android.ps1" `
        "keeps native Android credential panel implementation audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionLoginPanelNativeAndroidChecks",
            "audit-steam-version-selection.login-panel.native-android.fields.ps1",
            "audit-steam-version-selection.login-panel.native-android.layout.ps1",
            "audit-steam-version-selection.login-panel.native-android.security.ps1",
            "Add-SteamVersionSelectionLoginPanelNativeAndroidFieldChecks",
            "Add-SteamVersionSelectionLoginPanelNativeAndroidLayoutChecks",
            "Add-SteamVersionSelectionLoginPanelNativeAndroidSecurityChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-panel.native-android.fields.ps1" `
        "keeps native Android credential field and autofill audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionLoginPanelNativeAndroidFieldChecks",
            "GodotApp.java",
            "SteamLoginCredentialEditText",
            "AUTOFILL_HINT_USERNAME",
            "AUTOFILL_HINT_PASSWORD"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-panel.native-android.layout.ps1" `
        "keeps native Android credential panel layout and input routing audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionLoginPanelNativeAndroidLayoutChecks",
            "GodotApp.java",
            "useSteamLoginCredentialWideLayout",
            "updateSteamLoginCredentialKeyboardInsets",
            "dismissSteamLoginCredentialPanelFromBack"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-panel.native-android.security.ps1" `
        "keeps native Android credential handoff and sensitive-field cleanup audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionLoginPanelNativeAndroidSecurityChecks",
            "GodotApp.java",
            "STEAM_LOGIN_CREDENTIAL_RESULT_TTL_MS",
            "clearSteamLoginCredentialPanelSensitiveFields"
        )
}
