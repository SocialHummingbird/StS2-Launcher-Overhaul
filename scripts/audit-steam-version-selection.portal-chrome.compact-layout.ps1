function Add-SteamVersionSelectionPortalChromeCompactLayoutChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLayoutProfile.cs" `
        "uses the full available Android viewport for compact launcher layouts" `
        @(
            "mobileShell = OperatingSystem\.IsAndroid\(\)",
            "compact = mobileShell",
            "AndroidCompactTouchScaleFloor = 1\.06f",
            "mobileShell \? AndroidCompactTouchScaleFloor : CompactScaleFloor",
            "CompactStackedActionRowsWidth = 560f",
            "CompactStackedActionRows",
            "contentMaxWidth < MathF\.Round\(CompactStackedActionRowsWidth \* scale\)",
            "panelWidth = compact \? 1\.0f",
            "panelHeight = compact \? 1\.0f",
            "Math\.Min\(safeViewport\.X \* 0\.96f, 1600f\)",
            "Math\.Min\(safeViewport\.X \* 0\.84f, 1180f\)",
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
            "LauncherViewLayoutMetrics\.ScaleInt\(72, scale\)",
            "MouseFilterEnum\.Ignore"
        )
}
