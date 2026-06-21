function Add-SteamVersionSelectionLoginPanelChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCredentialEntrySupport.cs" `
        "declares native Android credential panel support without app-owned password storage" `
        @(
            "AppStoresSteamPassword\s*=\s*false",
            "NativeCredentialHandoffPopupSupported\s*=\s*false",
            "NativeIntegratedCredentialPanelSupported\s*=\s*true",
            "NativeCredentialFieldsAutofillHintsConfigured\s*=\s*true",
            "SteamCredentialWebDomainConfigured\s*=\s*true",
            "NativeCredentialPanelInlineStatusConfigured\s*=\s*true",
            "NativeCredentialPanelKeyboardSafeLayoutConfigured\s*=\s*true",
            "NativeCredentialPanelImeInsetScrollSupported\s*=\s*true",
            "NativeCredentialPanelTouchTargetLayoutConfigured\s*=\s*true",
            "NativeCredentialPanelLargeFieldTargetsSupported\s*=\s*true",
            "NativeCredentialPanelRequestsBothAutofillFields\s*=\s*true",
            "NativeCredentialPanelFocusAutofillRequestsSupported\s*=\s*true",
            "NativeCredentialPanelTaskLedButtonsSupported\s*=\s*true",
            "NativeCredentialPanelResponsiveActionRowsSupported\s*=\s*true",
            "NativeCredentialPanelOrientationReflowSupported\s*=\s*true",
            "NativeCredentialPanelShortHeightCopySupported\s*=\s*true",
            "NativeCredentialPanelShortHeightReflowSupported\s*=\s*true",
            "NativeCredentialPanelImeHeightReflowSupported\s*=\s*true",
            "NativeCredentialPanelPasswordVisibilityToggleSupported\s*=\s*true",
            "NativeCredentialPanelPasswordFocusButtonSupported\s*=\s*true",
            "NativeCredentialPanelBackDismissSupported\s*=\s*true",
            "NativeCredentialPanelDismissRetrySupported\s*=\s*true",
            "NativeCredentialPanelDismissHidesKeyboardSupported\s*=\s*true",
            "NativeCredentialPanelSuppressesPreAuthSavePrompt\s*=\s*true",
            "SteamGuardOneShotCodeGuidanceSupported\s*=\s*true",
            "SteamGuardAlphanumericKeyboardSupported\s*=\s*true",
            "FailedLoginRetryGuidanceSupported\s*=\s*true",
            "ContextSpecificLoginRecoveryGuidanceSupported\s*=\s*true",
            "GodotFieldCredentialMetadataConfigured\s*=\s*true",
            "AndroidKeyboardCredentialHintsConfigured\s*=\s*true",
            "GodotFieldsAreNativeAndroidAutofillTargets\s*=\s*false",
            "PasswordManagerSuggestionsDeviceValidated\s*=\s*false",
            "NativeCredentialHandoffResultTtlSeconds",
            "NativeCredentialHandoffResultTtlSeconds\s*=\s*60",
            "Integrated native Android credential panel",
            "must not store or inject Steam passwords",
            "real username/password EditText fields",
            "Steam web-domain metadata",
            "inline status/error guidance",
            "explicit large styled credential-field touch targets",
            "keyboard-safe scrollable top-weighted layout with IME inset padding and focus scrolling",
            "branded task-led controls that stay full-width on portrait phones and switch to responsive credential/action rows on wide landscape Android viewports",
            "short-height landscape copy compression so credential fields and primary actions stay higher on phone screens",
            "short-height copy reflow when the landscape height class changes",
            "IME-visible height reflow when the keyboard reduces usable landscape height",
            "orientation and screen-size changes rebuild the native credential panel when the width class changes while clearing stale view fields",
            "manual password visibility toggle that resets to hidden",
            "one-shot Steam Guard code guidance with alphanumeric keyboard entry and uppercase/separator normalization",
            "context-specific failed-login and connection-recovery guidance",
            "keyboard/focus cleanup on native panel dismiss",
            "old user-facing native credential popup is disabled",
            "provider behavior is device/provider dependent",
            "Native username/password fields",
            "cleared after submit/cancel/expiry",
            "credential_hint",
            "credential_storage_owner",
            "android_credential_provider"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\LoginSection.Submission.cs" `
        "clears captured Steam password from Godot login UI before authentication handoff" `
        @(
            "var password = _passwordField\.Text",
            "_passwordField\.Text = """"",
            "LoginRequested\?\.Invoke\(username, password\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\LoginSection.cs" `
        "wires Godot fallback credential fields and the Android native panel entry point" `
        @(
            "ConfigureUsernameField",
            "ConfigurePasswordField",
            "VirtualKeyboardType\.EmailAddress",
            "VirtualKeyboardType\.Password",
            "Sign in with Steam",
            "credentialHelpLabel",
            "MoveChild\(_nativeLoginButton, credentialHelpLabel\.GetIndex\(\)\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\LoginSection.NativePanel.cs" `
        "uses integrated native Steam login panel instead of a separate credential popup on Android" `
        @(
            "ShowSteamLoginCredentialPanel",
            "TryConsumeSteamLoginCredentialResult",
            "IsSteamLoginCredentialPanelVisible",
            "StopNativeCredentialPolling\(hidePanel: false\)",
            "HideSteamLoginCredentialPanel",
            "OpenNativeCredentialPanel",
            "PollNativeCredentialResult",
            "LoginRequested"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\LoginSection.Help.cs" `
        "keeps native Steam login help explicit about integrated panel and password storage boundaries" `
        @(
            "integrated Steam login panel",
            "does not store your Steam password",
            "Password manager can appear\.",
            "Steam password is not stored\.",
            "LauncherSectionMetrics\.CompactCredentialHelpHeight"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\LoginSection.State.cs" `
        "clears and hides native credential panel when login state is disabled or password is cleared" `
        @(
            "ClearPassword",
            "_passwordField\.Text = """"",
            "StopNativeCredentialPolling\(hidePanel: true\)",
            "SetFormVisible",
            "OpenNativeCredentialPanel"
        )

    Add-Check `
        "android\src\com\game\sts2launcher\GodotApp.java" `
        "provides integrated native Steam credential panel with Android credential-provider hints" `
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
            "cancelSteamLoginCredentialAutofillSession",
            "autofillManager\.cancel\(\)",
            "Show password",
            "Hide password",
            "toggleSteamLoginCredentialPasswordVisibility",
            "TYPE_TEXT_VARIATION_VISIBLE_PASSWORD",
            "boolean wideCredentialLayout = useSteamLoginCredentialWideLayout\(\)",
            "steamLoginCredentialShortHeightLayout",
            "boolean shortHeightCredentialLayout = wideCredentialLayout && useSteamLoginCredentialShortHeightLayout\(\)",
            "useSteamLoginCredentialWideLayout",
            "width > height && width >= dp\(640\)",
            "useSteamLoginCredentialShortHeightLayout",
            "height < dp\(430\)",
            "steamLoginCredentialUsableHeightPixels",
            "getWindow\(\)",
            "getDecorView\(\)",
            "getWindowVisibleDisplayFrame\(visibleFrame\)",
            "visibleWideLayout != steamLoginCredentialWideLayout",
            "visibleShortHeightLayout != steamLoginCredentialShortHeightLayout",
            "shortHeightCredentialLayout == steamLoginCredentialShortHeightLayout",
            "translateSteamLoginCredentialStatusForLayout",
            "steamLoginCredentialDefaultStatusText\(boolean shortHeightLayout\)",
            "steamLoginCredentialShownStatusText\(boolean shortHeightLayout\)",
            "Use Android password suggestions here\. Credentials clear after one Steam handoff\.",
            "Not stored by StS2 Mobile\.",
            "steamLoginCredentialDefaultStatusText",
            "steamLoginCredentialShownStatusText",
            "steamLoginCredentialPanelWidth\(wideCredentialLayout\)",
            "createSteamLoginCredentialInputRow\(wideCredentialLayout\)",
            "credentialFieldLayoutParams\(wideCredentialLayout\)",
            "credentialInlineActionLayoutParams\(wideCredentialLayout, 4\)",
            "credentialInlineActionLayoutParams\(wideCredentialLayout, 6\)",
            "credentialSubmitButtonLayoutParams\(wideCredentialLayout\)",
            "credentialCancelButtonLayoutParams\(wideCredentialLayout\)",
            "new LinearLayout\.LayoutParams\(dp\(178\), LinearLayout\.LayoutParams\.WRAP_CONTENT\)",
            "onConfigurationChanged",
            "Configuration newConfig",
            "reflowSteamLoginCredentialPanelForCurrentWindow",
            "wideCredentialLayout == steamLoginCredentialWideLayout",
            "clearSteamLoginCredentialVisibleFieldText",
            "parent\.removeView\(steamLoginCredentialOverlay\)",
            "clearSteamLoginCredentialViewReferences",
            "setSteamLoginCredentialPasswordVisibilityState",
            "onBackPressed",
            "dispatchKeyEvent",
            "KEYCODE_BACK",
            "dismissSteamLoginCredentialPanelFromBack",
            "isSteamLoginCredentialPanelVisible",
            "public boolean isSteamLoginCredentialPanelVisible",
            "hideKeyboardForSteamLoginCredentialPanel",
            "focusedView\.clearFocus\(\)",
            "styleSteamLoginCredentialButton",
            "field\.setMinHeight\(dp\(56\)\)",
            "field\.setPadding\(dp\(14\), 0, dp\(14\), 0\)",
            "field\.setBackground\(background\)",
            "button\.setMinHeight\(dp\(primary \? 60 : 56\)\)",
            "ScrollView",
            "steamLoginCredentialScrollView",
            "updateSteamLoginCredentialKeyboardInsets",
            "scheduleSteamLoginCredentialFocusScroll",
            "Gravity\.TOP \| Gravity\.CENTER_HORIZONTAL",
            "scroll\.setClipToPadding\(false\)",
            "buttons\.setOrientation\(wideCredentialLayout \? LinearLayout\.HORIZONTAL : LinearLayout\.VERTICAL\)",
            "buttonLayoutParams",
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
            "Steam password is never stored by StS2 Mobile",
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
