function Add-SteamVersionSelectionCompactWorkflowStickyHeaderStyleChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.CompactTaskHeader.Style.cs" `
        "wraps the compact sticky task header in a low-profile toolbar shell" `
        @(
            "WrapCompactStickyTaskHeader",
            "BuildCompactStickyTaskHeaderStyle",
            "new PanelContainer",
            "BuildCompactStickyTaskHeaderStyle\(scale\)",
            "LauncherComponentTheme\.Panel",
            "LauncherStyleBoxes\.MakeFilled",
            "CompactStickyTaskToolbarRadius",
            "SetBorderWidthAll",
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactStickyTaskToolbarHorizontalMargin, scale\)",
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactStickyTaskToolbarVerticalMargin, scale\)"
        )
}
