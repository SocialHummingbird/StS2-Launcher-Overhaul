function Add-SteamVersionSelectionActionCloudSafetyPushFlowChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
        "uses explicit Steam Cloud direction and overwrite-risk wording in the portal" `
        @(
            "pushPullRow",
            "pushButton",
            "confirmPushButton",
            "pushConfirmationLabel",
            "Push Locked",
            "CompactCloudPushDangerText\(\)",
            "CompactCloudPushConfirmText\(\)",
            "Pull Saves from Steam Cloud",
            "BuildCloudPushConfirmationLabel\(scale, compact\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudPush.cs" `
        "keeps Steam Cloud Push behind an explicit arm and confirm flow" `
        @(
            "CloudPushArmRequested",
            "CloudPushArmRequested\?\.Invoke\(\) == false",
            "ArmCloudPush",
            "ConfirmCloudPush",
            "ResetCloudPushArm"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Branches.Text.cs" `
        "uses explicit Steam Cloud direction wording in ready/action state" `
        @(
            "_readyVersionSummaryLabel",
            "Ready version:",
            "Start Game and Pull/Push use this version",
            "Push stays locked until explicitly opened",
            "SteamGameInstallPaths\.VersionSlotKind",
            "Pull copies Steam Cloud saves to Android",
            "Push copies Android saves to Steam Cloud",
            "can overwrite remote saves",
            "Version/download actions affect local game files only",
            "Steam Cloud saves move only through Pull/Push"
        )
}
