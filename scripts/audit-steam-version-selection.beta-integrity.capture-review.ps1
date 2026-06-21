function Add-SteamVersionSelectionBetaIntegrityCaptureReviewChecks {
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

}
