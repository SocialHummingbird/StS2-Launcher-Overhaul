function Add-SteamVersionSelectionSessionAuthChecks {
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

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSteamSession.Connection.cs" `
        "keeps saved Steam connection retry policy centralized" `
        @(
            "SavedConnectionVerifyAttempts = 3"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSteamSession.Connection.SavedCredentials.cs" `
        "retries saved-credential login and verifies ownership before adopting a Steam connection" `
        @(
            "ConnectSavedCredentialsAndVerifyAsync",
            "SavedConnectionVerifyAttempts",
            "Retrying saved Steam connection",
            "UseConnectionAndVerifyOwnershipAsync",
            "CreateSavedCredentialConnection",
            "Saved Steam connection attempt",
            "Could not connect to Steam"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSteamSession.Connection.Ensure.cs" `
        "validates existing or saved Steam connections before depot operations" `
        @(
            "EnsureConnectedAsync",
            "EnsureExistingConnectionAsync",
            "AdoptSavedConnectionAfterAccessCheckAsync",
            "_connection != null",
            "_credentialStore\.TryCreateConnection",
            "No saved credentials",
            "EnsureAppAccessTokenNotDeniedAsync",
            "DropConnection",
            "Retrying Steam access check",
            "AdoptConnectionAfterVerificationAsync"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSteamSession.Connection.Adoption.cs" `
        "adopts verified Steam connections and disposes failed candidates" `
        @(
            "AdoptConnectionAfterVerificationAsync",
            "beforeAdoptAsync",
            "UseConnection\(connection\)",
            "adopted = true",
            "if \(!adopted\)",
            "connection\.Dispose\(\)",
            "DropConnection",
            "ReferenceEquals\(_connection, connection\)",
            "CreateSavedCredentialConnection",
            "throw new InvalidOperationException\(""No saved credentials""\)"
        )
}
