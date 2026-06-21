function Add-SteamVersionSelectionReleaseDocsSignoffChecks {

    Add-Check `
        "docs\steam-version-selection-save-compatibility.md" `
        "tracks public/beta save compatibility and Push safety evidence" `
        @(
            "Compatibility matrix",
            "Push safety matrix",
            "Public/default",
            "Beta",
            "Pull from Cloud after the branch switch",
            "last_manual_cloud_push\.txt",
            "successful Push marker evidence"
        )

    Add-Check `
        "OVERHAUL_ROADMAP.md" `
        "tracks Steam version selection as an active hardening phase" `
        @(
            "Steam version selection and branch cache hardening",
            "Refresh Game Versions",
            "Login credential providers",
            "SteamKit debug logs disabled by default",
            "sts2_steamkit_debug_logs=1",
            "branch marker/provenance",
            "wrapped selector guidance",
            "managed/native guidance parity",
            "missing/private/password",
            "Pull-after-switch",
            "last_manual_cloud_push\.txt",
            "aggregate successful post-switch Push evidence"
        )

    Add-Check `
        "docs\android-release-validation.md" `
        "keeps release signoff gated on branch/version evidence" `
        @(
            "Steam version-selection validation",
            "audit-steam-version-selection\.ps1",
            "audit-steam-branch-guidance-parity\.ps1",
            "steam_branch\.txt",
            "selected-PCK startup routing",
            "backup storage permission"
        )

}
