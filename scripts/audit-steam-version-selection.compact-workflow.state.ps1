function Add-SteamVersionSelectionCompactWorkflowStateChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.Data.cs" `
        "defines compact workflow step labels, details, and navigation enum" `
        @(
            "CompactWorkflowStepNames",
            "CompactWorkflowStepNumbers",
            "CompactWorkflowStepDetails",
            "CompactWorkflowStep",
            '"Sign in"',
            '"Verify"',
            '"Files"',
            '"Play"',
            '"1"',
            '"2"',
            '"3"',
            '"4"',
            "CompactWorkflowStepTooltips",
            "Sign in",
            "Steam Guard",
            "Game files",
            "Saves safe",
            "Open sign-in",
            "Open Steam Guard",
            "Open game files",
            "Open play and saves"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.State.cs" `
        "updates compact workflow active/completed step colors" `
        @(
            "SetCompactWorkflowStep",
            "_workflowStepNumberLabels",
            "_workflowStepNumberLabels\[i\]\.AddThemeColorOverride",
            "_workflowStepDetailLabels\[i\]\.AddThemeColorOverride",
            "LauncherComponentTheme\.OrangeHot",
            "LauncherComponentTheme\.CyanAccent",
            "LauncherComponentTheme\.CyanDim",
            "LauncherComponentTheme\.TextMuted"
    )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.Navigation.cs" `
        "wires the compact current-task jump button without invoking launcher actions directly" `
        @(
            "_compactCurrentTaskButton",
            "WireCompactCurrentTaskNavigation",
            "ScrollCompactPrimaryTo\(_compactCurrentTaskTarget\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.State.cs" `
        "tracks the compact current-task jump target without invoking launcher actions directly" `
        @(
            "_compactCurrentTaskButton",
            "_compactCurrentTaskTarget",
            "SetCompactCurrentTask",
            "SetCompactCurrentTaskButtonText",
            "string detail",
            "SetCompactCurrentTaskButtonText\(_compactCurrentTaskButton, _scale, text, detail\)"
        )
}
