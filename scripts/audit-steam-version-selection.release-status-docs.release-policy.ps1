function Add-SteamVersionSelectionReleaseStatusDocsReleasePolicyChecks {
    Add-Check `
        "docs\launcher-loading-screen-staging.md" `
        "documents Android-readable shader warmup staging" `
        @(
            "Android shader warmup uses the launcher compact touch-scale floor",
            "mobile-width compact panel",
            "styled percentage progress bar",
            "Android game startup status now uses a framed mobile-width status card",
            "Successful startup cleanup now frees the whole Android startup status root container"
        )

    Add-Check `
        "docs\release-and-backport-policy.md" `
        "requires release notes to name branch/version limitations" `
        @(
            "Steam beta/version selection proof",
            "audit-steam-version-selection\.ps1",
            "audit-steam-branch-guidance-parity\.ps1",
            "beta password/private branch behavior",
            "save compatibility across branches"
        )
}
