function Add-SteamVersionSelectionSafeFlowGuideChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.cs" `
        "builds the quick-start safe-flow guide panel" `
        @(
            "BuildFirstRunGuide",
            "BuildFirstRunGuidePanel",
            "CompactSafeFlowGuideTitleHeight",
            "CompactSafeFlowGuideTitleFontSize",
            "BuildFirstRunGuideStyle\(scale, compact\)",
            "`"Quick start guide`"",
            "new PanelContainer",
            "AddCompactSafeFlowSteps\(body, scale\)",
            "choose a game version, get Steam saves, then start the game",
            "Upload stays locked until you deliberately open it after checking local saves"
        )

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

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.Toggle.cs" `
        "builds the collapsible compact quick-start guide toggle shell" `
        @(
            "BuildCollapsedFirstRunGuide",
            "BuildFirstRunGuidePanel\(scale, compact: true\)",
            "toggle\.Pressed \+= \(\) =>",
            "guide\.Visible = !guide\.Visible",
            "`"Quick Start`"",
            "`"Get saves first`"",
            "`"Hide Guide`"",
            "`"Safe order`"",
            "LauncherSectionMetrics\.CompactDrawerToggleHeight",
            "LauncherSectionMetrics\.CompactDetailButtonFontSize",
            "CompactButtonDetailLabelSpec",
            "CompactSafeFlowToggleLabels",
            "CompactSafeFlowToggleBodyName",
            "CompactSafeFlowToggleTitleName",
            "CompactSafeFlowToggleDetailName",
            "CompactButtonDetailLabelSpec\.Default"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.Toggle.Text.cs" `
        "sets compact quick-start guide toggle text through structured labels" `
        @(
            "SetCompactSafeFlowToggleText",
            "CompactButtonDetailLabels\.Apply",
            '\$"\{title\}\\n\{detail\}"',
            "enabled: true",
            "CompactSafeFlowToggleLabels"
        )
}
