function Add-SteamVersionSelectionActionCloudSafetyCueChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
        "keeps Pull and Push cloud actions beside the confirmation warning" `
        @(
            "cloudGroup.AddChild\(pushPullRow\)",
            "Pull Saves from Steam Cloud",
            "Push Locked",
            "CompactCloudPushConfirmText\(\)",
            "BuildCloudPushConfirmationLabel\(scale, compact\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.Safety.cs" `
        "shows cloud-save safety guidance beside Pull/Push actions" `
        @(
            "cloudSafetyLabel",
            "OrangeHot",
            "CompactCloudSafetyDetailHeight",
            "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
            "cloudGroup.AddChild\(cloudSafetyLabel\)",
            "CompactCloudSafetySummary\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.cs" `
        "keeps cloud safety, options, and guarded Pull/Push controls in one Saves workflow" `
        @(
            "BuildCloudPrimaryActionControls\(cloudGroup, scale, compact\)",
            "BuildCloudSafetyControls\(cloudGroup, scale, compact\)",
            "BuildCloudOptionControls\(cloudGroup, scale, compact\)",
            "primaryActions\.PushPullRow",
            "safetyControls\.CloudSafetyLabel",
            "optionControls\.CloudOptionsToggle"
        )
}
