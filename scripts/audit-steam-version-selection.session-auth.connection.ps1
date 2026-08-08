function Add-SteamVersionSelectionSessionAuthConnectionChecks {
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
