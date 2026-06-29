function Add-SteamVersionSelectionLocalLoginChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSessionCoordinator.cs" `
        "keeps local Steam credential handoff state and polling constants centralized" `
        @(
            "LocalLoginPollDelayMs = 500",
            "LocalLoginPollTimeout = TimeSpan\.FromSeconds\(180\)",
            "_localLoginHandoffStarted"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSessionCoordinator.LocalLogin.cs" `
        "starts Android local credential handoff once and supports immediate credential consumption" `
        @(
            "StartLocalLoginHandoff",
            "TryStartImmediateLocalLoginHandoff",
            "OperatingSystem\.IsAndroid",
            "Interlocked\.CompareExchange",
            "Volatile\.Read",
            "LocalSteamCredentials\.Consume",
            "Volatile\.Write\(ref _localLoginHandoffStarted, 0\)",
            "Starting immediate local Steam credential handoff",
            "SessionState\.Authenticating",
            "StartObservedLocalLoginTask"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSessionCoordinator.LocalLogin.cs" `
        "wraps local credential handoff execution with failure display and flag reset" `
        @(
            "RunLocalLoginHandoffAsync",
            "WatchLocalLoginHandoffAsync",
            "RunLocalLoginAsync",
            "LoginFormFailure\.LocalCredentialHandoff\(\)\.Show",
            "Volatile\.Write\(ref _localLoginHandoffStarted, 0\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSessionCoordinator.LocalLogin.cs" `
        "polls for local credential files only while the connection is pending" `
        @(
            "WatchLocalLoginHandoffAsync",
            "DateTime\.UtcNow \+ LocalLoginPollTimeout",
            "_model\.IsConnectionPending\(\)",
            "LocalSteamCredentials\.Consume",
            "Task\.Delay\(LocalLoginPollDelayMs\)",
            "Local Steam credential handoff watcher timed out",
            "connection no longer pending"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSessionCoordinator.LocalLogin.cs" `
        "performs local Steam credential login through the normal model timeout path" `
        @(
            "RunLocalLoginAsync",
            "Consumed local Steam credential file",
            "SessionState\.Authenticating",
            "localLogin\.LoginAsync\(_model, StartConnectionTimeout\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LocalSteamCredentials.cs" `
        "keeps local Steam credential file decoding isolated from session orchestration" `
        @(
            "LocalSteamCredentialFileName = ""steam_login_credentials.txt""",
            "LauncherExternalFileInbox\.ConsumeLines",
            "TryDecodeBase64Line",
            "expected base64-encoded username and password",
            "model\.LoginWithTimeoutAsync\(Username, Password, startTimeout\)"
        )
}
