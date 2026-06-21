function Add-SteamVersionSelectionSafeFlowGuideStepChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.Steps.cs" `
        "declares typed compact quick-start safe-flow step rows" `
        @(
            "AddCompactSafeFlowSteps",
            "private readonly record struct CompactSafeFlowStepSpec",
            "CompactSafeFlowSteps",
            "foreach \(var step in CompactSafeFlowSteps\)",
            "BuildCompactSafeFlowStep\(scale, step\)",
            "CompactSafeFlowGuideStepHeight",
            "CompactSafeFlowGuideStepAccentWidth",
            "CompactSafeFlowGuideStepNumberWidth",
            "CompactSafeFlowGuideStepRadius",
            "CompactSafeFlowGuideStepHorizontalMargin",
            "CompactSafeFlowGuideStepVerticalMargin",
            "`"Sign in`"",
            "`"Steam account`"",
            "`"Get files`"",
            "`"Version on Android`"",
            "`"Get saves`"",
            "`"Steam to Android`"",
            "`"Play`"",
            "`"Ready version`"",
            "`"Upload locked`"",
            "Review before uploading"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.StepCard.cs" `
        "builds bounded compact quick-start safe-flow step card layout" `
        @(
            "BuildCompactSafeFlowStep",
            "BuildCompactSafeFlowStepText",
            "CompactSafeFlowStepSpec step",
            "step\.Title",
            "step\.Detail",
            "step\.Accent",
            "CustomMinimumSize = new Vector2"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.StepCard.Decor.cs" `
        "builds compact quick-start safe-flow step accent and marker decoration" `
        @(
            "BuildCompactSafeFlowStepAccent",
            "BuildCompactSafeFlowStepMarker",
            "Color accent",
            "step\.Marker",
            "step\.Accent",
            "CompactSafeFlowGuideStepAccentWidth",
            "CompactSafeFlowGuideStepNumberWidth"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.StepCard.Labels.cs" `
        "builds bounded compact quick-start safe-flow step title and detail labels" `
        @(
            "BuildCompactSafeFlowStepTitle",
            "BuildCompactSafeFlowStepDetail",
            "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
            "LauncherComponentTheme\.TextPrimary",
            "LauncherComponentTheme\.TextSecondary"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.StepStyle.cs" `
        "keeps bounded compact quick-start safe-flow step styling isolated" `
        @(
            "BuildCompactSafeFlowStepStyle",
            "Color accent",
            "CompactSafeFlowGuideStepRadius",
            "CompactSafeFlowGuideStepHorizontalMargin",
            "CompactSafeFlowGuideStepVerticalMargin",
            "LauncherStyleBoxes\.MakeFilled",
            "SetBorderWidthAll"
        )
}
