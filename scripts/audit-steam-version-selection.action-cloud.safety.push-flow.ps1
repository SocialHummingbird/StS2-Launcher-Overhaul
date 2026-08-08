function Add-SteamVersionSelectionActionCloudSafetyPushFlowChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
        "uses neutral review and dangerous Upload actions with explicit direction" `
        @(
            "pushPullRow",
            "pushButton",
            "confirmPushButton",
            "pushEligibilityLabel",
            "pushConfirmationLabel",
            "Review Upload",
            "ApplySupportAction\(cloudPushToggle, scale\)",
            "ApplyDangerAction\(pushButton, scale\)",
            "ApplyDangerAction\(confirmPushButton, scale\)",
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
            "var eligibility = CloudPushArmRequested\?\.Invoke\(\)",
            "eligibility == null \|\| !eligibility\.IsEligible",
            "ReadAndApplyCloudPushEligibility",
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
            "Start Game, Pull, and Upload use this version",
            "Review Upload before overwriting Steam Cloud",
            "SteamGameInstallPaths\.VersionSlotKind",
            "Pull copies Steam Cloud saves to Android",
            "Push copies Android saves to Steam Cloud",
            "can overwrite remote saves",
            "Version/download actions affect local game files only",
            "Steam Cloud saves move only through Pull/Upload"
        )
}
