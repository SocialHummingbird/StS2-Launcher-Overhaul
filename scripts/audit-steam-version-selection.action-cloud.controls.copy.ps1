function Add-SteamVersionSelectionActionCloudControlCopyChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudSafety.cs" `
        "defines compact Pull label as an explicit title/detail Android download action" `
        @(
            "CompactCloudPullText",
            "Get Steam Saves",
            "Download to Android"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudSafety.cs" `
        "defines compact dangerous Push labels as explicit title/detail actions" `
        @(
            "CompactCloudPushDangerText",
            "Upload to Steam",
            "Overwrite cloud",
            "CompactCloudPushConfirmText",
            "Confirm Upload",
            "Overwrite cloud",
            "CompactCloudPushWarningText",
            "Steam Cloud overwrite",
            "Confirm the selected saves and version"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudSafety.cs" `
        "names the compact neutral drawer as an Upload review" `
        @(
            "CompactCloudPushToggleText",
            "Review Upload",
            "Close Upload Review",
            "string detail"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudPush.cs" `
        "keeps compact Upload review copy synced with live eligibility" `
        @(
            "UpdateCloudPushReviewButtonText",
            "SetCompactActionButtonText",
            "_cloudPushReviewDetail",
            "CompactCloudPushToggleText"
        )
}
