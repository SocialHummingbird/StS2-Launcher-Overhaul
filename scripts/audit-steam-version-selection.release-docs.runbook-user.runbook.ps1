function Add-SteamVersionSelectionReleaseDocsRunbookChecks {
    Add-Check `
        "docs\steam-version-selection-runbook.md" `
        "orders destructive cloud validation behind Pull and backup gates" `
        @(
            "steam-version-selection-release-readiness\.md",
            "Cloud Pull gate",
            "Backup permission gate",
            "Pre-Push backup evidence",
            "Manual Push smoke test",
            "Optional auth diagnostics",
            "sts2_steamkit_debug_logs",
            "SteamKit debug logs sanitized for credentials/tokens"
        )
}
