function Add-SteamVersionSelectionReleaseDocsOverviewChecks {
    Add-Check `
        "docs\steam-version-selection-validation.md" `
        "keeps release blockers explicit" `
        @(
            "Release-readiness blockers",
            "Current release decision: not release-ready",
            "active install slot",
            "selected-cache-preserved aggregate",
            "Manual Pull completed before Push",
            "Current important Android local save evidence count",
            "Baseline manual Push prerequisites satisfied",
            "beta password",
            "save compatibility",
            "steam-version-selection-release-readiness\.md",
            "steam-version-selection-runbook\.md"
        )

    Add-Check `
        "docs\steam-version-selection-release-readiness.md" `
        "tracks implementation status versus release evidence requirements" `
        @(
            "Current status",
            "Evidence required before release-candidate signoff",
            "Known release blockers",
            "Release rule",
            "Public/default branch",
            "Branch selector",
            "No silent public fallback",
            "Side-by-side storage",
            "Native startup routing",
            "Steam Cloud safety",
            "Autofill",
            "Artifact hygiene",
            "ARM64",
            "Pull-before-Push",
            "not release-candidate signed off"
        )

    Add-Check `
        "docs\steam-version-selection-architecture.md" `
        "documents branch storage, readiness, cache, startup, and Push safety invariants" `
        @(
            "Steam branch dropdown",
            "SelectorInstallSlotHelpText",
            "active install slot",
            "Ready and download-required launcher status",
            "SteamBranchInfo\.selectorHelpText",
            "non-interactive helper text",
            "game_versions",
            "steam_branch\.txt",
            "Readiness rules",
            "Startup routing rules",
            "Selected game version note",
            "Selected game version slot kind",
            "Selected game version slot directory",
            "selected branch note",
            "Branch switch marker filename",
            "Cache cleanup rules",
            "Steam Cloud Push safety",
            "Release blockers"
        )
}
