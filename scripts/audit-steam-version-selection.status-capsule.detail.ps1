function Add-SteamVersionSelectionStatusCapsuleDetailChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.Detail.cs" `
        "keeps normal compact status details to a stable one-line row" `
        @(
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactStatusDetailHeight, scale\)",
            "statusLabel\.AutowrapMode = TextServer\.AutowrapMode\.Off",
            "statusLabel\.ClipText = true",
            "statusLabel\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Status.cs" `
        "uses short compact status details with tap-to-expand full details for touch devices" `
        @(
            "var fullMessage = LauncherPortalStatusFormatter\.MessageFor\(text\)",
            "_profile\.Compact",
            "LauncherPortalStatusFormatter\.CompactMessageFor\(text\)",
            "_statusLabel\.TooltipText = fullMessage",
            "_compactStatusDetailsButton",
            "_compactStatusDetailsCueLabel",
            "WireCompactStatusDetailToggle",
            "ToggleCompactStatusDetails",
            "ApplyCompactStatusDetailLayout",
            "ShouldAutoExpandCompactStatusDetails",
            "_compactStatusExpanded",
            "_compactStatusFullMessage",
            "_compactStatusShortMessage",
            "_compactStatusDetailsButton\.Pressed \+= ToggleCompactStatusDetails",
            "_compactStatusDetailsButton\.Disabled = !hasFullDetails",
            "_compactStatusDetailsButton\.MouseDefaultCursorShape = hasFullDetails",
            "_compactStatusDetailsCueLabel\.Visible = hasFullDetails",
            "_compactStatusDetailsCueLabel\.Text = expanded \? `"Hide`" : `"Details`"",
            "_compactStatusPhase",
            "`"Attention`"",
            "TextServer\.AutowrapMode\.WordSmart",
            "TextServer\.AutowrapMode\.Off",
            "_statusLabel\.ClipText = !expanded"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.Detail.cs" `
        "renders compact status details as a discoverable touch-safe Details cue" `
        @(
            "BuildCompactStatusDetailButton",
            "ApplyCompactStatusDetailButtonStyle",
            "TooltipText = `"Show full launcher status`"",
            "MouseDefaultCursorShape = Control\.CursorShape\.PointingHand",
            "detailCue",
            "`"Details`"",
            "LauncherComponentTheme\.CyanAccent"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.Styles.cs" `
        "styles compact status detail control for touch-safe affordance states" `
        @(
            "ApplyCompactStatusDetailButtonStyle",
            "BuildCompactStatusDetailButtonStyle",
            "CompactStatusDetailRadius",
            "CompactStatusDetailHorizontalMargin",
            "CompactStatusDetailVerticalMargin",
            "SetBorderWidthAll"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.Compact.cs" `
        "wires compact status detail toggle into the capsule layout" `
        @(
            "Button CompactDetailButton",
            "StyledLabel CompactDetailCue",
            "var detailButton = BuildCompactStatusDetailButton",
            "var detailRow = BuildCompactStatusDetailRow",
            "var detailCue = BuildCompactStatusDetailCue",
            "detailButton\.AddChild\(detailRow\)",
            "body\.AddChild\(detailButton\)",
            "return \(panel, headline, phasePanel, detailButton, detailCue\)"
        )
}
