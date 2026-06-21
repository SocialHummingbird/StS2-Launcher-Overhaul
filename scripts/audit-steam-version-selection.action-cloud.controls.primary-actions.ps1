function Add-SteamVersionSelectionActionCloudControlPrimaryActionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
        "uses explicit compact cloud direction labels while keeping upload locked" `
        @(
            "CompactCloudPullText\(\)",
            "CompactCloudPushToggleText\(expanded: false\)",
            "CompactCloudPushDangerText\(\)",
            "CompactCloudPushConfirmText\(\)",
            "SetCompactActionButtonText\(pullButton, pullButton\.Text\)",
            "SetCompactActionButtonText\(pushButton, pushButton\.Text\)",
            "SetCompactActionButtonText\(confirmPushButton, confirmPushButton\.Text\)",
            "BuildCloudPushConfirmationLabel\(scale, compact\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PushConfirmation.cs" `
        "keeps compact cloud Push confirmation warning bounded and overwrite-explicit" `
        @(
            "BuildCloudPushConfirmationLabel",
            "CompactCloudPushWarningText\(\)",
            "Confirming Push uploads Android saves to Steam Cloud",
            "can overwrite remote Steam Cloud saves",
            "CompactCloudPushWarningFontSize",
            "CompactCloudPushWarningHeight",
            "pushConfirmationLabel\.ClipText = compact",
            "pushConfirmationLabel\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
            "pushConfirmationLabel\.CustomMinimumSize = new Vector2",
            "LauncherComponentTheme\.OrangeHot",
            "pushConfirmationLabel\.Visible = false"
        )
}
