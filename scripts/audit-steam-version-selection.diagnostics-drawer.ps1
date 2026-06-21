function Add-SteamVersionSelectionDiagnosticsDrawerChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.LogColumn.cs" `
        "keeps diagnostics hidden behind a clearly labeled Help & Reports drawer" `
        @(
            "Hidden by default",
            "Create a help report",
            "Problem details and help reports",
            "Review before sharing",
            "drawer\.Visible = false",
            "SetDiagnosticsToggleText",
            "LauncherSectionMetrics\.CompactDrawerToggleHeight",
            "BuildLogView\(profile\)",
            "return \(log, drawer, toggle\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.LogColumn.Toggle.cs" `
        "renders compact diagnostics drawer toggle labels as structured title/detail controls" `
        @(
            "Show Help & Reports",
            "Hide Help & Reports",
            "DiagnosticsToggleText",
            "SetDiagnosticsToggleText",
            "CompactButtonDetailLabels\.Apply",
            "CompactButtonDetailLabelSpec",
            "CompactDiagnosticsToggleLabels",
            "CompactDiagnosticsToggleBodyName",
            "CompactDiagnosticsToggleTitleName",
            "CompactDiagnosticsToggleDetailName",
            "CompactButtonDetailLabelSpec\.Default",
            "enabled: false",
            "enabled: true",
            "`"Help & Reports\\nPrivate until opened`"",
            "`"Hide Help\\nBack to launcher`""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.Result.cs" `
        "uses a typed primary-column layout result instead of a large positional tuple" `
        @(
            "LauncherViewPrimaryColumn",
            "CompactStatusDetailsButton",
            "WorkflowStepNumberLabels",
            "CompactStickyTaskHeader",
            "PrimaryScroll",
            "CompactDiagnosticsHost"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.StatusResult.cs" `
        "keeps primary status result fields isolated from full column layout result fields" `
        @(
            "LauncherViewPrimaryStatus",
            "StyledLabel Phase",
            "StyledLabel Action",
            "StyledLabel Message",
            "ColorRect Accent",
            "Control Capsule",
            "CompactDetailButton",
            "CompactHeadline",
            "CompactPhasePanel"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.BodyResult.cs" `
        "keeps primary body scroll result isolated from full column layout result fields" `
        @(
            "LauncherViewPrimaryBody",
            "ScrollContainer PrimaryScroll",
            "VBoxContainer Body"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
        "hosts compact diagnostics inside the primary scroll body instead of fixed root chrome" `
        @(
            "VBoxContainer compactDiagnosticsHost",
            "compactDiagnosticsHost = new VBoxContainer",
            "compactDiagnosticsHost\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
            "left\.AddChild\(compactDiagnosticsHost\)",
            "compactDiagnosticsHost"
        )

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

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Log.cs" `
        "renders compact diagnostics logs with readable Android text and padding" `
        @(
            "CompactDiagnosticsLogFontSize = 15",
            "CompactDiagnosticsLogMarginHorizontal = 12",
            "CompactDiagnosticsLogMarginVertical = 10",
            "BuildLogView\(LauncherLayoutProfile profile\)",
            "var compact = profile\.Compact",
            "compact \? CompactDiagnosticsLogFontSize : LauncherComponentTheme\.LogFontSize",
            "BuildLogStyle\(scale, compact\)",
            "compact\s*\?\s*CompactDiagnosticsLogMarginHorizontal",
            "compact\s*\?\s*CompactDiagnosticsLogMarginVertical",
            "SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.cs" `
        "hosts compact diagnostics drawer under the primary compact body" `
        @(
            "DiagnosticsDrawer",
            "DiagnosticsToggle",
            "var diagnosticsRoot = profile\.Compact",
            "primary\.CompactDiagnosticsHost"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Diagnostics.cs" `
        "can reveal the hidden Help & Reports drawer after explicit diagnostics actions" `
        @(
            "ShowDiagnosticsConsole",
            "DiagnosticsDrawer\.Visible = true",
            "SetDiagnosticsToggleText\(DiagnosticsToggle, _profile, visible: true\)",
            "ScrollCompactPrimaryTo\(DiagnosticsDrawer\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.Diagnostics.cs" `
        "opens Help & Reports when problem summary or launcher-log actions write output" `
        @(
            "ShowDiagnosticsSummary",
            "CopyRawLogToClipboard",
            "Last problem opened",
            "view\.ShowDiagnosticsConsole\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.Diagnostics.Export.cs" `
        "opens Help & Reports after manual help report export writes output" `
        @(
            "ShowDiagnosticsExportResult",
            "Help report ready",
            "Help report saved",
            "_view\.ShowDiagnosticsConsole\(\)"
        )

}
