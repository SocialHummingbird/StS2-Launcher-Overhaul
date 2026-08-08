function Add-SteamVersionSelectionReleaseStatusDocsAndroidStatusChecks {
    Add-Check `
        "docs\current-android-status.md" `
        "keeps Android status current for version selection, credential providers, and credential-log hardening" `
        @(
            "Steam game version selection is in hardening",
            "steam-version-selection-release-readiness\.md",
            "android-steam-login-validation\.md",
            "discovery-led dropdown Steam branch selector",
            "password-manager login behavior",
            "does not store or inject Steam passwords",
            "SteamKit debug logs are disabled by default",
            "sts2_steamkit_debug_logs=1",
            "native fallback keeps verbose diagnostics collapsed until requested",
            "structured compact startup recovery actions",
            "ARM64 device validation"
        )

    Add-Check `
        "README.md" `
        "explains published tester scope and version-selection limitations in user-facing language" `
        @(
            "unofficial Android launcher",
            "You must own Slay the Spire 2 on Steam",
            "compatibility is not broad yet",
            "Steam version selection, beta branches, Workshop mods, and native modded-save compatibility remain experimental",
            "Refresh Game Versions",
            "Steam beta password entry is not implemented",
            "ARM64",
            "Current unreleased source keeps game saving local-only",
            "Launcher-owned automatic sync is enabled by default",
            "same verified transfer used by the manual recovery controls",
            "Published .v0\.2\.416. safe public trial checklist",
            "SteamKit debug logs are disabled by default",
            "does not store or inject Steam passwords"
        )
}
