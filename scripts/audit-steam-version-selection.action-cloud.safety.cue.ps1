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
        "src\STS2Mobile\Launcher\Sections\ActionSection.Layout.cs" `
        "moves compact cloud-safety cue before Pull/Push controls" `
        @(
            "MoveCompactCloudSafetyCueBeforeCloudActions",
            "_cloudGroup\.MoveChild\(_cloudSafetyToggle, 0\)",
            "MoveChildAfter\(_cloudGroup, _cloudSafetyLabel, _cloudSafetyToggle\)",
            "MoveChildAfter\(_cloudGroup, _pushPullRow, _cloudSafetyLabel\)"
        )
}
