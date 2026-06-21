function Add-SteamVersionSelectionBetaIntegrityReleaseReadinessChecks {
    Add-Check `
        "docs\steam-version-selection-release-readiness.md" `
        "requires beta branch integrity evidence before release signoff" `
        @(
            "Beta branch integrity",
            "effective manifest",
            "selected-branch manifest",
            "public manifest",
            "manifest source",
            "inherits public",
            "file inventory",
            "key asset or PCK hashes",
            "Runtime patching now falls back to branch-local"
        )

}
