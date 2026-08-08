function Add-SteamVersionSelectionLoginPanelNativeAndroidSecurityChecks {
    Add-Check `
        "android\src\com\game\sts2launcher\GodotApp.java" `
        "keeps native Steam credential handoff short-lived and clears sensitive fields" `
        @(
            "cancelSteamLoginCredentialAutofillSession",
            "autofillManager\.cancel\(\)",
            "clearSteamLoginCredentialVisibleFieldText",
            "parent\.removeView\(steamLoginCredentialOverlay\)",
            "clearSteamLoginCredentialViewReferences",
            "setSteamLoginCredentialPasswordVisibilityState",
            "pendingSteamLoginCredentialUsername",
            "pendingSteamLoginCredentialPassword",
            "clearSteamLoginCredentialPanelSensitiveFields",
            'steamLoginCredentialUsernameField\.setText\(\"\"\)',
            'steamLoginCredentialPasswordField\.setText\(\"\"\)',
            "STEAM_LOGIN_CREDENTIAL_RESULT_TTL_MS",
            "pendingSteamLoginCredentialExpiresAtMs",
            "clearPendingSteamLoginCredentialsLocked",
            "Native Steam login credential panel shown",
            "Native Steam login credentials submitted to managed login flow"
        )
}
