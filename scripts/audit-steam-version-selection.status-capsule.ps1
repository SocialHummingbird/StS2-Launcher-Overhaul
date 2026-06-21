function Add-SteamVersionSelectionStatusCapsuleChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.cs" `
        "wraps launcher status in a readable portal status capsule" `
        @(
            "BuildStatusCapsule",
            "BuildCompactStatusCapsule",
            "new PanelContainer",
            "BuildStatusStyle\(scale, compact: false\)",
            "BuildStatusPhaseStyle\(scale, compact: false\)",
            "statusAccent\.CustomMinimumSize",
            "statusLabel\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.Styles.cs" `
        "keeps status capsule visual styles in focused helpers" `
        @(
            "BuildStatusStyle",
            "BuildStatusPhaseStyle",
            "BuildCompactStatusDetailButtonStyle",
            "SetBorderWidthAll"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.cs" `
        "centralizes compact status capsule sizing constants" `
        @(
            "CompactStatusBodySeparation = 5",
            "CompactStatusAccentHeight = 3",
            "CompactStatusHeadlineSeparation = 3",
            "CompactStatusHeadlineInlineSeparation = 6",
            "CompactStatusPhaseInlineWidth = 112",
            "CompactStatusPhaseHorizontalMargin = 7",
            "CompactStatusPhaseVerticalMargin = 3",
            "CompactStatusActionMinHeight = 24",
            "CompactStatusDetailHeight = 44",
            "CompactStatusDetailCueWidth = 62",
            "CompactStatusDetailCueFontSize = LauncherSectionMetrics\.CompactDetailLabelFontSize",
            "CompactStatusDetailHorizontalMargin = 8",
            "CompactStatusDetailVerticalMargin = 5",
            "CompactStatusDetailRowGap = 6",
            "CompactStatusDetailRadius = 7"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.Compact.cs" `
        "uses a low-profile compact status card with responsive headline and narrow stacked fallback" `
        @(
            "BuildCompactStatusCapsule",
            "profile\.CompactStackedActionRows",
            "ApplyCompactStatusHeadlineLayout",
            "GridContainer CompactHeadline",
            "PanelContainer CompactPhasePanel",
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactStatusBodySeparation, scale\)",
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactStatusAccentHeight, scale\)",
            "var headline = new GridContainer",
            "headline\.Columns = stacked \? 1 : 2",
            "LauncherViewLayoutMetrics\.ScaleInt\(\s*stacked\s*\?\s*CompactStatusHeadlineSeparation\s*:\s*CompactStatusHeadlineInlineSeparation",
            "LauncherViewLayoutMetrics\.ScaleInt\(stacked \? 0 : CompactStatusPhaseInlineWidth, scale\)",
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactStatusActionMinHeight, scale\)",
            "var stacked = profile\.CompactStackedActionRows",
            "phasePanel\.SizeFlagsHorizontal = stacked",
            "Control\.SizeFlags\.ShrinkBegin",
            "headline\.AddChild\(phasePanel\)",
            "BuildStatusPhaseStyle\(scale, compact: true\)",
            "statusActionLabel\.VerticalAlignment = VerticalAlignment\.Center",
            "statusActionLabel\.AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
            "statusActionLabel\.ClipText = false",
            "statusActionLabel\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
            "statusActionLabel\.CustomMinimumSize = new Vector2",
            "headline\.AddChild\(statusActionLabel\)",
            "body\.AddChild\(headline\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Behavior.Responsive.cs" `
        "reflows the compact status headline after viewport changes" `
        @(
            "private void UpdateCompactStatusHeadline\(Vector2 viewportSize\)",
            "_compactStatusHeadline",
            "_compactStatusPhasePanel",
            "LauncherLayoutProfile\.ForViewport\(viewportSize\)",
            "ApplyCompactStatusHeadlineLayout"
        )

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
