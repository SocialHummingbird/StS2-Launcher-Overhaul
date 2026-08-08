function Add-SteamVersionSelectionDiagnosticsDrawerShellChecks {
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
        "hosts diagnostics inside the stable Help destination" `
        @(
            "DiagnosticsDrawer",
            "DiagnosticsToggle",
            "BuildLogColumn\(profile, Actions\.HelpDiagnosticsHost",
            "Actions\.HelpDiagnosticsHost"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Destinations.cs" `
        "exposes diagnostics only while Help is selected" `
        @(
            "DiagnosticsToggle\.Visible = destination == LauncherDestination\.Help",
            "DiagnosticsDrawer\.Visible = false"
        )
}
