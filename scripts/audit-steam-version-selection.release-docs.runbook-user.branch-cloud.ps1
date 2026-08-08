function Add-SteamVersionSelectionReleaseDocsUserGuideBranchCloudChecks {
    Add-Check `
        "docs\steam-version-selection-user-guide.md" `
        "keeps branch history and the current transfer safety contract visible" `
        @(
            "Branch switch marker filename",
            "Branch switch previous branch",
            "Branch switch selected branch",
            "Branch switch selected version",
            "Branch switch selected version slot kind",
            "Branch switch selected version slot directory",
            "Branch switch warning acknowledged",
            "Manual Pull outcome",
            "Manual Pull outcome detail",
            "Incomplete Pull marker present",
            "Selected save namespace",
            "Runtime compatibility / branch identity",
            "Mod-set fingerprint",
            "last_manual_cloud_push\.txt",
            "last_manual_cloud_push_blocked\.txt",
            "Latest manual Push evidence outcome",
            "Latest manual Push evidence reason",
            "Manual Push blocked evidence reason",
            "Manual Push blocked before upload evidence recorded",
            "Game version cache cleanup marker selected cache preserved where applicable",
            "Pull is a separate Cloud-to-Android operation",
            "Exact transfer context, destination backup, failure propagation, and verified read-back"
        )
}
