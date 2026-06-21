function Add-SteamVersionSelectionLocalLoginChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.LocalLogin.cs" `
        "keeps local Steam credential handoff state and polling constants centralized" `
        @(
            "LocalLoginPollDelayMs = 500",
            "LocalLoginPollTimeout = TimeSpan\.FromSeconds\(180\)",
            "_localLoginHandoffStarted"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.LocalLogin.Start.cs" `
        "starts Android local credential handoff once and supports immediate credential consumption" `
        @(
            "StartLocalLoginHandoff",
            "TryStartImmediateLocalLoginHandoff",
            "OperatingSystem\.IsAndroid",
            "Interlocked\.CompareExchange",
            "Volatile\.Read",
            "ConsumeLocalSteamCredentials",
            "Volatile\.Write\(ref _localLoginHandoffStarted, 0\)",
            "Starting immediate local Steam credential handoff",
            "SessionState\.Authenticating",
            "StartObservedLocalLoginTask"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.LocalLogin.Handoff.cs" `
        "wraps local credential handoff execution with failure display and flag reset" `
        @(
            "RunLocalLoginHandoffAsync",
            "WatchLocalLoginHandoffAsync",
            "RunLocalLoginAsync",
            "LoginFormFailure\.LocalCredentialHandoff\(\)\.Show",
            "Volatile\.Write\(ref _localLoginHandoffStarted, 0\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.LocalLogin.Watch.cs" `
        "polls for local credential files only while the connection is pending" `
        @(
            "WatchLocalLoginHandoffAsync",
            "DateTime\.UtcNow \+ LocalLoginPollTimeout",
            "_model\.IsConnectionPending\(\)",
            "ConsumeLocalSteamCredentials",
            "Task\.Delay\(LocalLoginPollDelayMs\)",
            "Local Steam credential handoff watcher timed out",
            "connection no longer pending"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.LocalLogin.Run.cs" `
        "performs local Steam credential login through the normal model timeout path" `
        @(
            "RunLocalLoginAsync",
            "Consumed local Steam credential file",
            "SessionState\.Authenticating",
            "localLogin\.LoginAsync\(_model, StartConnectionTimeout\)"
        )
}
