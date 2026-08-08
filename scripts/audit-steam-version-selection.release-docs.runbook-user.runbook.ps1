function Add-SteamVersionSelectionReleaseDocsRunbookChecks {
    Add-Check `
        "docs\steam-version-selection-runbook.md" `
        "requires the shared transfer safety contract before destructive cloud validation" `
        @(
            "steam-version-selection-release-readiness\.md",
            "Upload eligibility",
            "Transfer-context isolation",
            "Transfer correctness",
            "Manual Upload smoke test",
            "Optional auth diagnostics",
            "sts2_steamkit_debug_logs",
            "SteamKit debug logs sanitized for credentials/tokens"
        )
}
