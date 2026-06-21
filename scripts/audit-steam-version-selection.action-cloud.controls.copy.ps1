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
            "Confirm only after Pull/local saves are verified"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudPush.cs" `
        "keeps compact Push relock label direction-aware and structured after reset" `
        @(
            "CompactCloudPushToggleText",
            "SetCompactActionButtonText\(_cloudPushToggle, _compact"
        )
}
