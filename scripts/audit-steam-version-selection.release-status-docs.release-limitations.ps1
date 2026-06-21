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
            "last_manual_cloud_push\.txt",
            "aggregate successful post-switch Push evidence",
            "bounded two-line Files for / Play version helper labels"
        )
}
