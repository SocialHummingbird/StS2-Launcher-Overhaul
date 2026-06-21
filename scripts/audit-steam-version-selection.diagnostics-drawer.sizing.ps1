function Add-SteamVersionSelectionDiagnosticsDrawerSizingChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.LogColumn.Sizing.cs" `
        "bounds the compact diagnostics log viewport from the current launcher profile" `
        @(
            "CompactDiagnosticsLogViewportHeightRatio = 0\.28f",
            "CompactDiagnosticsLogMinHeight = 220",
            "CompactDiagnosticsLogMaxHeight = 340",
            "DiagnosticsLogHeight\(LauncherLayoutProfile profile\)",
            "profile\.ViewportSize\.Y \* CompactDiagnosticsLogViewportHeightRatio",
            "Math\.Clamp\(viewportHeight, minHeight, maxHeight\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Behavior.cs" `
        "refreshes diagnostics log viewport height after Android viewport changes" `
        @(
            "UpdateViewportSize\(Vector2 viewportSize\)",
            "_panel\.UpdateSizeFromViewport\(viewportSize, _profile\.PanelHeightRatio\)",
            "UpdateDiagnosticsLogViewport\(viewportSize\)",
            "UpdateKeyboardOffset\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.LogColumn.Sizing.cs" `
        "keeps open compact diagnostics readable after viewport resize" `
        @(
            "UpdateDiagnosticsLogViewport\(Vector2 viewportSize\)",
            "LauncherLayoutProfile\.ForViewport\(viewportSize\)",
            "Log\.CustomMinimumSize = new Vector2\(0, DiagnosticsLogHeight\(profile\)\)",
            "_profile\.Compact && DiagnosticsDrawer\.Visible",
            "ScrollCompactPrimaryTo\(DiagnosticsDrawer\)"
        )
}
