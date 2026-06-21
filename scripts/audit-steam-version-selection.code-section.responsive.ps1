function Add-SteamVersionSelectionCodeSectionResponsiveChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CodeSection.Layout.cs" `
        "reflows compact Steam Guard code controls after viewport changes" `
        @(
            "UpdateViewportProfile\(LauncherLayoutProfile profile\)",
            "GodotObject\.IsInstanceValid\(_compactCodeActionRow\)",
            "_compactStackedActionRows = profile\.Compact && profile\.CompactStackedActionRows",
            "ApplyCompactCodeActionRowLayout\(_compactCodeActionRow, profile\.Scale, _compactStackedActionRows\)",
            "row\.Columns = compactStackedActionRows \? 1 : 2",
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactCodeActionRowSeparation, scale\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Behavior.Responsive.cs" `
        "updates compact section responsive rows after viewport changes" `
        @(
            "private void UpdateCompactSectionResponsiveRows\(Vector2 viewportSize\)",
            "LauncherLayoutProfile\.ForViewport\(viewportSize\)",
            "Code\.UpdateViewportProfile\(profile\)"
        )
}
