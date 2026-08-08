function Add-SteamVersionSelectionBetaIntegrityIssueTemplateHygieneChecks {
    Add-Check `
        ".github\ISSUE_TEMPLATE\steam_version_selection_report.md" `
        "keeps public Steam version-selection reports free of secrets and identifiers" `
        @(
            "Release-readiness gate covered",
            "No silent fallback to public/default",
            "Public-vs-beta depot manifest integrity",
            "Public-vs-beta file inventory",
            "Did any game behavior, UI, or art asset look like public/mainline",
            "Was the beta slot clean-redownloaded",
            "Android/Samsung/password-manager suggestion behavior",
            "Public-share artifact hygiene reviewed",
            "Artifact hygiene",
            "Steam credentials",
            "refresh tokens",
            "shared preferences",
            "device identifiers",
            "local user paths",
            "Android credential provider model",
            "Launcher stores Steam password for credential providers",
            "SteamKit debug logs opt-in enabled",
            "SteamKit debug logs sanitized for credentials/tokens",
            "adb logcat",
            "redacting identifiers",
            "logcat-steam-version-focused-redacted\.txt",
            "avoid raw full logcat",
            "manually review it before posting"
        )
}
