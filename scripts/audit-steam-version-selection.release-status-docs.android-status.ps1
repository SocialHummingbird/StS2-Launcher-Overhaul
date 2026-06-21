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
        "advertises version selection as published but not release-candidate signed off" `
        @(
            "implemented for validation",
            "steam-version-selection-release-readiness\.md",
            "not release-candidate signed off",
            "discovery-led dropdown selector",
            "Refresh Game Versions",
            "public-inherited",
            "public-vs-beta integrity classification",
            "steam-beta-integrity-runtime-checklist\.md",
            "mixed beta/public behavior",
            "Autofill",
            "SteamKit debug logs are disabled by default",
            "Steam beta password entry",
            "Push backup evidence"
        )
}
