function Add-SteamVersionSelectionReleaseDocsUserGuideBetaIntegrityChecks {
    Add-Check `
        "docs\steam-version-selection-user-guide.md" `
        "keeps beta branch integrity evidence interpretation visible" `
        @(
            "steam_branch\.txt",
            "selectedBranchManifest",
            "publicManifest",
            "public-inherited",
            "manifestRequestBranch=public",
            "branch-integrity provenance",
            "Branch marker depots inherited from public",
            "Branch marker depots missing selected branch manifest",
            "Branch marker depot manifest rows",
            "Classification:",
            "Evidence readiness: not ready for final classification",
            "Clean redownload matches investigated branch: true",
            "Clean redownload selected directories cleared: true",
            "Public-vs-beta key asset comparison captured"
        )
}
