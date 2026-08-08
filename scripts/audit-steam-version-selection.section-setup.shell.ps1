function Add-SteamVersionSelectionSectionSetupShellChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\LauncherSectionSetup.cs" `
        "frames hidden launcher states through a small section setup entrypoint" `
        @(
            "ConfigureHiddenSection",
            "internal static partial class LauncherSectionSetup",
            "bool compact",
            "compactCue",
            "accent",
            "LauncherSectionMetrics\.CompactSectionSeparation",
            "LauncherSectionMetrics\.SectionSeparation",
            "section\.Visible = false",
            "BuildSectionHeader\(title, subtitle, scale, accent, compact, compactCue\)"
        )
}
