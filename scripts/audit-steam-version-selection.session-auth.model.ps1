function Add-SteamVersionSelectionSessionAuthModelChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.SessionAuth.cs" `
        "keeps launcher session authentication entry points together" `
        @(
            "ConnectAsync",
            "ConnectSavedCredentialsAndVerifyAsync",
            "SessionState\.Connecting",
            "LoginAsync\(string username, string password\)",
            "LoginWithTimeoutAsync",
            "SessionState\.Authenticating",
            "RunLoginAttemptAsync",
            "LoginAndVerifyAsync",
            "RaiseLogReceived",
            "RaiseCodeNeeded",
            "SubmitCode\(string code\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.SessionAuth.Result.cs" `
        "isolates session attempt result application and stale-failure logging" `
        @(
            "ConnectionAttemptResult",
            "Ignored stale session failure",
            "PatchHelper\.Log",
            "SessionState\.LoggedIn",
            "SessionState\.Failed",
            "_connectionResolved = Succeeded",
            "SetSessionState\(State, Failure\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.SessionAuth.Attempt.cs" `
        "centralizes session attempt identity and ownership-verification transition handling" `
        @(
            "RunConnectionAttemptAsync",
            "BeginSessionAttempt\(state\)",
            "CreateConnectionAttemptResult",
            "IsCurrentSessionAttempt\(attemptId\)",
            "ApplyConnectionAttemptResult",
            "BeginOwnershipVerification",
            "SessionState\.VerifyingOwnership"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.SessionAuth.Connection.cs" `
        "reuses an existing logged-in Steam connection before starting depot connection attempts" `
        @(
            "EnsureConnectedAsync",
            "IsLoggedIn",
            "_steamSession\.TryGetConnection",
            "RunConnectionAttemptAsync",
            "SessionState\.Connecting",
            "_steamSession\.EnsureConnectedAsync"
        )
}
