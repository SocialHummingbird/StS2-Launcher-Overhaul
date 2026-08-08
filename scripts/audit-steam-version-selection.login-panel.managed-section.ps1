function Add-SteamVersionSelectionLoginPanelManagedSectionChecks {
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
}
