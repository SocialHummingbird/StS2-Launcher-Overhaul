function Add-SteamVersionSelectionBetaIntegrityChecks {
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

    Add-Check `
        "scripts\capture-steam-beta-integrity-evidence.ps1" `
        "captures public versus selected branch inventories and marker evidence" `
        @(
            "android-shell-utils\.ps1",
            "evidence-path-utils\.ps1",
            "evidence-marker-utils\.ps1",
            "ConvertTo-AndroidShellSingleQuoted",
            "ConvertTo-AndroidShellPathSingleQuoted",
            "Resolve-EvidenceRepoPath",
            "ConvertTo-EvidenceSafeFileName",
            "public-files\.tsv",
            "public-cache-tree\.txt",
            "selected inventory",
            "cache-tree\.txt",
            "sha256sum",
            "public-vs-",
            "key-assets\.tsv",
            "Changed key asset rows",
            "Art/bundle-like files",
            "Public sharing warning:",
            "ReviewSummary",
            "FailOnNotReady",
            "Resolve-AndroidAdbPath",
            "review-beta-integrity-summary\.ps1",
            "Classification:",
            "Evidence readiness:",
            "Evidence missing/weak:",
            "Classification inputs:",
            "clean redownload not proven",
            "Public branch marker:",
            "Selected branch marker:",
            "Clean redownload marker:",
            "Clean redownload selected directories cleared:",
            "Branch availability marker:",
            "Branch availability matches investigated branch:",
            "Branch availability selected branch Windows depot manifests:",
            "likely Steam branch availability issue",
            "Focused logcat:",
            "Public branch depot manifest rows",
            "Selected branch depot manifest rows",
            "steam_branch\.txt",
            "last_steam_branch_availability\.txt",
            "public-inherited",
            "runtime remote/config",
            "Read-Inventory",
            "Write-InventoryComparison"
        )

    Add-Check `
        "scripts\review-beta-integrity-summary.ps1" `
        "reviews beta-integrity summary readiness without manually scanning the artifact" `
        @(
            "Evidence readiness:",
            "Evidence missing/weak:",
            "Public sharing warning present:",
            "Clean redownload matches investigated branch:",
            "Clean redownload selected directories cleared:",
            "Branch availability matches investigated branch:",
            "FailOnNotReady",
            "Exit code: 2",
            "Exit code: 3"
        )

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

    Add-Check `
        ".github\ISSUE_TEMPLATE\steam_version_selection_report.md" `
        "keeps public Steam version-selection reports free of secrets and identifiers" `
        @(
            "Release-readiness gate covered",
            "No silent fallback to public/default",
            "Public-vs-beta depot manifest integrity",
            "Public-vs-beta file inventory",
            "Did any game behavior, UI, or art asset look like public/mainline",
            "Was the beta slot clean-redownloaded",
            "Android/Samsung/password-manager suggestion behavior",
            "Public-share artifact hygiene reviewed",
            "Artifact hygiene",
            "Steam credentials",
            "refresh tokens",
            "shared preferences",
            "device identifiers",
            "local user paths",
            "Android credential provider model",
            "Launcher stores Steam password for credential providers",
            "SteamKit debug logs opt-in enabled",
            "SteamKit debug logs sanitized for credentials/tokens",
            "adb logcat",
            "redacting identifiers",
            "logcat-steam-version-focused-redacted\.txt",
            "avoid raw full logcat",
            "manually review it before posting",
            "Selected game version note",
            "Selected game version slot kind",
            "Selected game version slot directory",
            "Game version cache cleanup marker filename",
            "Game version cache cleanup marker path",
            "Game version cache cleanup marker present",
            "Game version cache cleanup marker UTC",
            "Game version cache cleanup marker selected branch",
            "Game version cache cleanup marker selected version",
            "Game version cache cleanup marker selected version slot kind",
            "Game version cache cleanup marker selected version slot directory",
            "Game version cache cleanup marker game_versions present",
            "Game version cache cleanup marker runtime_packs present",
            "Game version cache cleanup marker selected runtime pack directory",
            "Game version cache cleanup marker selected runtime pack present before cleanup",
            "Game version cache cleanup marker removed count",
            "Game version cache cleanup marker removed runtime pack count",
            "Game version cache cleanup marker selected cache preserved where applicable",
            "Game version cache cleanup marker selected runtime pack preserved where applicable",
            "Game version redownload marker filename",
            "Game version redownload marker path",
            "Game version redownload marker present",
            "Game version redownload marker selected branch",
            "Game version redownload marker selected version",
            "Game version redownload marker selected version slot kind",
            "Game version redownload marker selected version slot directory",
            "Selected game branch marker depots matching public",
            "Selected game branch marker depots differing from public",
            "Selected game branch marker depots without public comparison",
            "Selected game branch marker depots inherited from public",
            "Selected game branch marker depots missing selected branch manifest",
            "Selected game branch marker partial Steam branch evidence",
            "Selected game branch marker depot manifest rows",
            "manifestSource=<selected|public-inherited>",
            "manifestRequestBranch=<selected|public>",
            "Public-vs-beta file and art evidence",
            "Public-sharing warning reviewed",
            "SlayTheSpire2.pck",
            "Art/bundle-like files",
            "Branch switch marker filename",
            "Manual Pull evidence marker filename",
            "Manual Pull evidence marker path",
            "Manual Pull evidence UTC",
            "Manual Pull evidence UTC parseable",
            "Manual Pull evidence selected branch",
            "Manual Pull evidence selected version",
            "Manual Pull evidence selected version slot kind",
            "Manual Pull evidence selected version slot directory",
            "Manual Pull completed before Push",
            "Current important Android local save evidence count",
            "Current important Android local save evidence present",
            "Baseline manual Push prerequisites satisfied",
            "Manual Pull completed after branch switch",
            "Manual Push evidence marker filename",
            "Manual Push evidence marker path",
            "Manual Push evidence UTC",
            "Manual Push evidence UTC parseable",
            "Manual Push evidence selected branch",
            "Manual Push evidence selected branch selection kind",
            "Manual Push evidence selector mode",
            "Manual Push evidence selected version",
            "Manual Push evidence selected version slot kind",
            "Manual Push evidence selected version slot directory",
            "Manual Push evidence recorded important local save evidence count",
            "Manual Push evidence recorded baseline prerequisites satisfied",
            "Manual Push evidence recorded pre-Push backup evidence satisfied",
            "Manual Push completed after branch switch for selected version with backup evidence",
            "Manual Push blocked evidence marker filename",
            "Manual Push blocked evidence marker path",
            "Manual Push blocked evidence UTC",
            "Manual Push blocked evidence UTC parseable",
            "Manual Push blocked evidence selected branch",
            "Manual Push blocked evidence selected branch selection kind",
            "Manual Push blocked evidence selector mode",
            "Manual Push blocked evidence selected version",
            "Manual Push blocked evidence selected version slot kind",
            "Manual Push blocked evidence selected version slot directory",
            "Manual Push blocked evidence reason",
            "Manual Push blocked evidence recorded important local save evidence count",
            "Manual Push blocked evidence recorded baseline prerequisites satisfied",
            "Manual Push blocked evidence recorded prerequisites satisfied",
            "Manual Push blocked evidence recorded local backup count",
            "Manual Push blocked evidence recorded cloud backup count",
            "Manual Push blocked evidence recorded latest local backup UTC",
            "Manual Push blocked evidence recorded latest cloud backup UTC",
            "Manual Push blocked evidence recorded pre-Push backup evidence satisfied",
            "Manual Push blocked before upload evidence recorded",
            "Pre-Push local backup evidence count",
            "Pre-Push cloud backup evidence count",
            "Latest pre-Push local backup UTC",
            "Latest pre-Push cloud backup UTC",
            "Pre-Push local backup evidence after branch switch",
            "Pre-Push cloud backup evidence after branch switch",
            "Branch-switch pre-Push backup evidence satisfied",
            "Pull from Cloud"
        )

}
