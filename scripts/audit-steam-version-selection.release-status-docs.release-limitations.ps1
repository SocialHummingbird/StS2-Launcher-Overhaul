function Add-SteamVersionSelectionReleaseStatusDocsReleaseLimitationChecks {
    Add-Check `
        "docs\steam-version-selection-release-note-snippet.md" `
        "prevents release notes from overclaiming branch/version readiness" `
        @(
            "validation-stage",
            "Known limitations",
            "Do not say yet",
            "Refresh Game Versions",
            "dropdown-first",
            "password-manager suggestion behavior",
            "SteamKit debug logs are disabled by default",
            "sts2_steamkit_debug_logs=1",
            "wrapped selector guidance",
            "managed/native selector-guidance parity",
            "audit-steam-version-selection\.ps1",
            "audit-steam-branch-guidance-parity\.ps1",
            "Password-protected beta branches",
            "Steam Cloud Push is safe",
            "Current-source Stage 2 is unreleased",
            "destination hash read-back before success",
            "A prior Pull, Steam installation, branch-switch history, shared-storage permission, and modded mode are not Upload prerequisites",
            "bounded two-line Files for / Play version helper labels"
        )
}
