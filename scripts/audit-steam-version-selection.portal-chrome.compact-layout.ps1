function Add-SteamVersionSelectionPortalChromeCompactLayoutChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLayoutProfile.cs" `
        "separates touch sizing from phone, landscape, and foldable layout modes" `
        @(
            "ForViewport\(\s*Vector2 viewportSize,\s*bool touchOptimized",
            "ResolveMode\(safeViewport, shortEdge, aspect, touchOptimized\)",
            "compact = mode != LauncherLayoutMode\.Wide",
            "AndroidCompactTouchScaleFloor = 1\.06f",
            "ResolveAndroidScale\(viewportScale\)",
            "CompactStackedActionRowsWidth = 560f",
            "CompactStackedActionRows",
            "contentMaxWidth < MathF\.Round\(CompactStackedActionRowsWidth \* scale\)",
            "panelWidth = compact \? 1\.0f",
            "panelHeight = compact \? 1\.0f",
            "Math\.Min\(safeViewport\.X \* 0\.96f, 1600f\)",
            "foldableOrTablet = shortEdge >= 1280f && aspect <= 1\.7f",
            "LauncherLayoutMode\.PhonePortrait",
            "LauncherLayoutMode\.PhoneLandscape",
            "LauncherLayoutMode\.Wide",
            "CompactStackedActionRows=\{CompactStackedActionRows\}"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Components\StyledPanel.cs" `
        "reduces compact-mode panel padding and avoids a short fixed-height phone panel" `
        @(
            "MaxHeight = 2200f",
            "CompactPanelHorizontalMargin = 10",
            "CompactPanelTopMargin = 10",
            "CompactPanelBottomMargin = 12",
            "compact \? CompactPanelHorizontalMargin : LauncherComponentTheme\.PanelHorizontalMargin",
            "compact \? CompactPanelTopMargin : LauncherComponentTheme\.PanelTopMargin",
            "compact \? CompactPanelBottomMargin : LauncherComponentTheme\.PanelBottomMargin",
            "_compact\s*\?\s*vpSize\.Y \* heightRatio",
            "BuildStyle\(scale, compact\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.Support.cs" `
        "adds compact-only bottom scroll breathing room for phone gesture areas" `
        @(
            "BuildCompactBottomScrollSpacer",
            "CompactBottomScrollSpacerHeight = 180",
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactBottomScrollSpacerHeight, scale\)",
            "MouseFilterEnum\.Ignore"
        )
}
