function Add-SteamVersionSelectionActionCloudSafetyCueChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
        "keeps Pull, Upload eligibility, and confirmation cues together" `
        @(
            "cloudGroup.AddChild\(pushPullRow\)",
            "Pull Saves from Steam Cloud",
            "Review Upload",
            "BuildCloudPushEligibilityLabel\(scale, compact\)",
            "pushPullRow\.AddChild\(pushEligibilityLabel\)",
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
