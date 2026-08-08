function Add-SteamVersionSelectionEvidenceToolingScaffoldChecks {
    Add-Check `
        "scripts\new-steam-version-selection-evidence.ps1" `
        "creates a structured artifact folder for ARM64 validation evidence" `
        @(
            "steam-version-selection",
            "branch-markers",
            "backup-evidence",
            "evidence-path-utils\.ps1",
            "evidence-redaction-utils\.ps1",
            "Resolve-EvidenceRepoPath",
            "steam-version-selection-evidence-template\.md",
            "steam-version-selection-release-readiness\.md",
            "ARTIFACT_HYGIENE\.txt",
            "PUBLIC_SHARE_MANIFEST\.txt",
            "PUBLIC_EVIDENCE_REDACTION_REVIEW\.txt",
            "review-public-evidence-redaction\.ps1",
            "Preferred public artifacts",
            "Local-only or manual-review artifacts",
            "Manual Push evidence marker filename",
            "Do not run manual Push"
        )

    Add-Check `
        "scripts\new-steam-version-selection-evidence.ps1" `
        "scaffolds non-secret device evidence review for version-selection validation" `
        @(
            "ARTIFACT_HYGIENE\.txt",
            "Raw logs and full launcher diagnostics are local-only",
            "PUBLIC_SHARE_MANIFEST\.txt",
            "PUBLIC_EVIDENCE_REDACTION_REVIEW\.txt",
            "Format-PublicEvidenceRedactionReviewFields",
            "preferred public artifacts",
            "steam_branch\.txt",
            "last_game_branch_switch\.txt",
            "last_manual_cloud_pull\.txt",
            "last_manual_cloud_push\.txt",
            "last_manual_cloud_push_blocked\.txt",
            "last_steam_branch_availability\.txt",
            "last_game_version_cache_cleanup\.txt",
            "last_game_version_redownload\.txt",
            "backup-evidence",
            "Resolve-EvidenceRepoPath",
            "branch-markers"
        )

    Add-Check `
        "scripts\audit-steam-branch-guidance-parity.ps1" `
        "checks managed/native selected-branch guidance phrase parity" `
        @(
            "SteamGameBranch\.cs",
            "SteamBranchInfo\.java",
            "Default/public Steam branch",
            "Failed downloads do not change Steam Cloud saves",
            "Save compatibility is unproven"
        )
}
