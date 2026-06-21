function Add-SteamVersionSelectionLoginPanelNativeAndroidFieldChecks {
    Add-Check `
        "android\src\com\game\sts2launcher\GodotApp.java" `
        "provides native Steam credential fields with Android credential-provider hints" `
        @(
            "showSteamLoginCredentialPanel",
            "hideSteamLoginCredentialPanel",
            "consumeSteamLoginCredentialResult",
            "SteamLoginCredentialEditText",
            "AUTOFILL_HINT_USERNAME",
            "AUTOFILL_HINT_PASSWORD",
            "setContentDescription",
            "setSaveEnabled\(false\)",
            "requestSteamLoginCredentialAutofill",
            "requestSteamLoginCredentialAutofillField",
            "setOnFocusChangeListener",
            "requestSteamLoginCredentialAutofillField\(steamLoginCredentialUsernameField\)",
            "requestSteamLoginCredentialAutofillField\(steamLoginCredentialPasswordField\)",
            "autofillManager\.requestAutofill\(field\)",
            "Sign in with Steam",
            "`"Next`"",
            "button\.setAllCaps\(false\)",
            "focusSteamLoginPasswordField",
            "setSteamLoginCredentialStatus",
            "Android password suggestions may appear",
            "Enter your Steam username to continue",
            "Enter your Steam password to continue",
            "Submitting to Steam",
            "IME_ACTION_NEXT",
            "IME_ACTION_DONE",
            "setWebDomain",
            "structure\.setHint",
            "store\.steampowered\.com",
            "Steam password is never stored by StS2 Mobile"
        )
}
