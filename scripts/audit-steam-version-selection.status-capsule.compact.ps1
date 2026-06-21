function Add-SteamVersionSelectionStatusCapsuleCompactChecks {
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
}
