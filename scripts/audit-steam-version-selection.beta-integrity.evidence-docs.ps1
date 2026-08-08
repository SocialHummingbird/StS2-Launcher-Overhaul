function Add-SteamVersionSelectionBetaIntegrityEvidenceDocChecks {
    Add-Check `
        "docs\steam-version-selection-tooling.md" `
        "documents beta-integrity evidence capture workflow" `
        @(
            "Capture beta branch integrity evidence",
            "capture-steam-beta-integrity-evidence\.ps1",
            "AdbPath",
            "review-beta-integrity-summary\.ps1",
            "ReviewSummary",
            "FailOnNotReady",
            "public-files\.tsv",
            "public-cache-tree\.txt",
            "<branch>-cache-tree\.txt",
            "public-vs-<branch>-comparison\.txt",
            "key-assets\.tsv",
            "Changed key asset rows",
            "manifestSource=selected",
            "public-inherited",
            "partial Steam branch",
            "Classification:",
            "Evidence readiness:",
            "review-beta-integrity-summary\.ps1",
            "clean-redownload proof",
            "public-sharing warning",
            "branch-availability evidence",
            "classification input metrics",
            "clean selected-branch redownload",
            "art assets look wrong"
        )

    Add-Check `
        "docs\steam-beta-integrity-runtime-checklist.md" `
        "documents remaining runtime pass for beta-integrity classification" `
        @(
            "capture-steam-beta-integrity-evidence\.ps1",
            "ReviewSummary",
            "FailOnNotReady",
            "Evidence readiness:",
            "Clean redownload matches investigated branch: true",
            "Clean redownload selected directories cleared: true",
            "Changed key asset rows",
            "likely Steam partial branch",
            "likely Steam branch availability issue",
            "Do not mark beta branch integrity complete"
        )

    Add-Check `
        "docs\steam-version-selection-evidence-template.md" `
        "captures beta-integrity evidence in validation packages" `
        @(
            "Public-vs-beta branch integrity",
            "Beta slot was clean-redownloaded",
            "clean-redownload fields",
            "branch-availability fields",
            "public/default and selected branch marker paths",
            "bounded public/default and selected branch depot manifest rows",
            "Focused beta-integrity logcat",
            "selectedBranchManifest",
            "publicManifest",
            "manifestSource",
            "manifestRequestBranch",
            "Selected beta cache tree captured",
            "Public-vs-beta inventory comparison captured",
            "public-vs-<branch>-key-assets\.tsv",
            "bounded changed key-asset rows",
            "SlayTheSpire2\.pck",
            "Affected art asset paths/hashes",
            "Classification:",
            "Evidence readiness:",
            "review-beta-integrity-summary\.ps1",
            "clean-redownload proof",
            "classification input metrics",
            "Steam partial branch",
            "runtime remote/config behavior",
            "Selected game branch marker depot manifest rows"
        )

    Add-Check `
        "scripts\new-steam-version-selection-evidence.ps1" `
        "scaffolds beta-integrity inventory evidence folder" `
        @(
            "inventories",
            "capture-steam-beta-integrity-evidence\.ps1",
            "review-beta-integrity-summary\.ps1",
            "Evidence readiness: not ready for final classification",
            "SHA-256 comparison summaries"
        )
}
