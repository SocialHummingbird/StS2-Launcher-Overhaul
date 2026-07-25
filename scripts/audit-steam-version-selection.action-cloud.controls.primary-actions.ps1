function Add-SteamVersionSelectionActionCloudControlPrimaryActionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
        "uses a neutral Upload review before dangerous Upload actions" `
        @(
            "CompactCloudPullText\(\)",
            "CompactCloudPushToggleText",
            "Check requirements",
            "CompactCloudPushDangerText\(\)",
            "CompactCloudPushConfirmText\(\)",
            "SetCompactActionButtonText\(pullButton, pullButton\.Text\)",
            "SetCompactActionButtonText\(pushButton, pushButton\.Text\)",
            "SetCompactActionButtonText\(confirmPushButton, confirmPushButton\.Text\)",
            "BuildCloudOperationProgressControls",
            "operationProgress\.Group",
            "operationProgress\.PhaseLabel",
            "operationProgress\.DetailLabel",
            "operationProgress\.ProgressBar",
            "BuildCloudPushEligibilityLabel\(scale, compact\)",
            "pushPullRow\.AddChild\(pushEligibilityLabel\)",
            "ApplySupportAction\(cloudPushToggle, scale\)",
            "ApplyDangerAction\(pushButton, scale\)",
            "ApplyDangerAction\(confirmPushButton, scale\)",
            "BuildCloudPushConfirmationLabel\(scale, compact\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.Progress.cs" `
        "renders observable cloud-operation phase, detail, and progress controls" `
        @(
            "CloudOperationProgressControls",
            "BuildCloudOperationProgressControls",
            "VBoxContainer",
            "Visible = false",
            "StyledLabel",
            "phaseLabel",
            "detailLabel",
            "WordSmart",
            "StyledProgressBar",
            "ShowPercentage = false",
            "SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudOperationProgress.cs" `
        "keeps active and terminal Pull progress visible in the Saves workflow" `
        @(
            "SetCloudOperationState",
            "CloudOperationPresentation\.Create",
            "_cloudOperationProgressGroup\.Visible = presentation\.Visible",
            "_cloudOperationPhaseLabel\.Text = presentation\.PhaseText",
            "_cloudOperationDetailLabel\.Text = presentation\.DetailText",
            "_cloudOperationProgressBar\.Value = presentation\.ProgressValue",
            "presentation\.IsFailed",
            "presentation\.IsComplete",
            "ClearCloudOperationState"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
        "reserves danger styling for real Upload actions" `
        @(
            "ApplyDangerAction\(cloudPushToggle"
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
